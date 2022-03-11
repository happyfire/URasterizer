using System;
using System.Collections.Generic;
using UnityEngine;

namespace URasterizer
{    
    public class GPURasterizer : IRasterizer
    {
        int _width;
        int _height;

        RenderingConfig _config;

        Matrix4x4 _matModel;
        Matrix4x4 _matView;
        Matrix4x4 _matProjection;

        public Matrix4x4 ModelMatrix
        {
            get => _matModel;           
            set => _matModel = value;            
        }

        public Matrix4x4 ViewMatrix
        {
            get => _matView;
            set => _matView = value;
        }

        public Matrix4x4 ProjectionMatrix
        {
            get => _matProjection;
            set => _matProjection = value;
        }

        Color[] frame_buf;
        float[] depth_buf;
        Color[] temp_buf;

        Color[] samplers_color_MSAA;
        bool[] samplers_mask_MSAA;
        float[] samplers_depth_MSAA;

        RenderTexture _colorTexture;

        public FragmentShader CurrentFragmentShader {get; set;}

        //Stats
        int _trianglesAll, _trianglesRendered;
        int _verticesAll;

        public OnRasterizerStatUpdate StatDelegate;

        //优化GC
        Vector4[] _tmpVector4s = new Vector4[3];
        Vector3[] _tmpVector3s = new Vector3[3];

        //Compute shader
        ComputeShader computeShader;
        int kernelClearFrame;
        int kernelVertexProcess;
        int kernelTriangleSetup;
        int kernelRasterizeTriangle;

        //ids of compute shader variables
        int frameColorTextureId;
        
        int vertexBufferId;
        int normalBufferId;
        int vertexOutBufferId;
        int triangleBufferId;
        int renderTriangleBufferId;
        int matMVPId;
        int matModelId;

        public String Name { get=>"GPU"; }

        public Texture ColorTexture { get=>_colorTexture; }


        public GPURasterizer(int w, int h, RenderingConfig config)
        {
            Debug.Log($"GPURasterizer screen size: {w}x{h}");

            _config = config;

            _width = w;
            _height = h;

            frame_buf = new Color[w * h];
            depth_buf = new float[w * h];
            temp_buf = new Color[w * h];

            _colorTexture = new RenderTexture(w, h, 0);
            _colorTexture.enableRandomWrite = true;     
            _colorTexture.Create();       
            _colorTexture.filterMode = FilterMode.Point;


            //init for compute shader
            computeShader = config.ComputeShader;
            kernelClearFrame = computeShader.FindKernel("ClearFrame");
            kernelVertexProcess = computeShader.FindKernel("VertexProcess");
            kernelTriangleSetup = computeShader.FindKernel("TriangleSetup");
            kernelRasterizeTriangle = computeShader.FindKernel("RasterizeTriangle");

            frameColorTextureId = Shader.PropertyToID("frameColorTexture");
            vertexBufferId = Shader.PropertyToID("vertexBuffer");
            normalBufferId = Shader.PropertyToID("normalBuffer");
            vertexOutBufferId = Shader.PropertyToID("vertexOutBuffer");
            triangleBufferId = Shader.PropertyToID("triangleBuffer");
            renderTriangleBufferId = Shader.PropertyToID("renderTriangleBuffer");

            matMVPId = Shader.PropertyToID("matMVP");
            matModelId = Shader.PropertyToID("matModel");
        }
        

        public float Aspect
        {
            get
            {
                return (float)_width / _height;
            }
        }

        public void Clear(BufferMask mask)
        {
            ProfileManager.BeginSample("Rasterizer.Clear GPU");            
            var shader = _config.ComputeShader;                        
            shader.SetTexture(kernelClearFrame, frameColorTextureId, _colorTexture);   
            var clearColor = _config.ClearColor;
            shader.SetFloats("ClearColor", clearColor.r, clearColor.g, clearColor.b, clearColor.a);         
            
            int groupX = Mathf.CeilToInt(_colorTexture.width/32f);
            int groupY = Mathf.CeilToInt(_colorTexture.height/24f);
            groupX = groupX==0? 1 : groupX;
            groupY = groupY==0? 1 : groupY;            
            shader.Dispatch(kernelClearFrame, groupX, groupY, 1); 
            
            _trianglesAll = _trianglesRendered = 0;
            _verticesAll = 0;

            ProfileManager.EndSample();            
        }

        public void SetupViewProjectionMatrix(Camera camera)
        {
            //左手坐标系转右手坐标系,以下坐标和向量z取反
            var camPos = camera.transform.position;
            camPos.z *= -1; 
            var lookAt = camera.transform.forward;
            lookAt.z *= -1;
            var up = camera.transform.up;
            up.z *= -1;
            
            ViewMatrix = TransformTool.GetViewMatrix(camPos, lookAt, up);

            if (camera.orthographic)
            {
                float halfOrthHeight = camera.orthographicSize;
                float halfOrthWidth = halfOrthHeight * Aspect;
                float f = -camera.farClipPlane;
                float n = -camera.nearClipPlane;
                ProjectionMatrix = TransformTool.GetOrthographicProjectionMatrix(-halfOrthWidth, halfOrthWidth, -halfOrthHeight, halfOrthHeight, f, n);
            }
            else
            {
                ProjectionMatrix = TransformTool.GetPerspectiveProjectionMatrix(camera.fieldOfView, Aspect, camera.nearClipPlane, camera.farClipPlane);
            }
        }

        public void Draw(RenderingObject ro, Camera camera)
        {
            ProfileManager.BeginSample("Rasterizer.Draw");

            Mesh mesh = ro.mesh;

            SetupViewProjectionMatrix(camera);

            ModelMatrix = ro.GetModelMatrix();                      

            Matrix4x4 mvp = _matProjection * _matView * _matModel;
            Matrix4x4 normalMat = _matModel.inverse.transpose;

            _verticesAll += mesh.vertexCount;
            _trianglesAll += ro.MeshTriangles.Length / 3;                           
            
            ProfileManager.BeginSample("GPURasterizer.VertexProcess");                

            var shader = _config.ComputeShader;            
            shader.SetMatrix(matMVPId, mvp);
            shader.SetMatrix(matModelId, _matModel);
            shader.SetBuffer(kernelVertexProcess, vertexBufferId, ro.VertexBuffer);
            shader.SetBuffer(kernelVertexProcess, normalBufferId, ro.NormalBuffer);
            shader.SetBuffer(kernelVertexProcess, vertexOutBufferId, ro.VertexOutBuffer);
            
            int groupCnt = Mathf.CeilToInt(mesh.vertexCount/768f);
            groupCnt = groupCnt==0? 1: groupCnt;
            shader.Dispatch(kernelVertexProcess, groupCnt, 1, 1);                          

            ProfileManager.EndSample();                                     
                                   
            ProfileManager.BeginSample("GPURasterizer.TriangleSetup");

            shader.SetInts("FrameBufferSize", _colorTexture.width, _colorTexture.height);
            ro.RenderTriangleBuffer.SetCounterValue(0);
            shader.SetBuffer(kernelTriangleSetup, triangleBufferId, ro.TriangleBuffer);
            shader.SetBuffer(kernelTriangleSetup, renderTriangleBufferId, ro.RenderTriangleBuffer);
            shader.SetBuffer(kernelTriangleSetup, vertexOutBufferId, ro.VertexOutBuffer);

            groupCnt = Mathf.CeilToInt((ro.MeshTriangles.Length/3)/768f);
            groupCnt = groupCnt==0? 1: groupCnt;
            shader.Dispatch(kernelTriangleSetup, groupCnt, 1, 1); 
            
            ComputeBuffer tmpBuf = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.IndirectArguments);

            ComputeBuffer.CopyCount(ro.RenderTriangleBuffer, tmpBuf, 0);
            uint[] tmpData = new uint[1];
            tmpBuf.GetData(tmpData);
            _trianglesRendered = (int)tmpData[0];
            tmpBuf.Release();
            
            ProfileManager.EndSample();

            //对每个triangle进行光栅化
            for(int i=0; i < _trianglesRendered; ++i){
                shader.SetBuffer(kernelRasterizeTriangle, "renderCosumeBuffer", ro.RenderTriangleBuffer);                
                shader.SetBuffer(kernelRasterizeTriangle, vertexOutBufferId, ro.VertexOutBuffer);
                shader.SetTexture(kernelRasterizeTriangle, frameColorTextureId, _colorTexture);   

                int groupX = Mathf.CeilToInt(_colorTexture.width/32f);
                int groupY = Mathf.CeilToInt(_colorTexture.height/24f);
                groupX = groupX==0? 1 : groupX;
                groupY = groupY==0? 1 : groupY;            
                shader.Dispatch(kernelRasterizeTriangle, groupX, groupY, 1); 
            }


            
           

            

            //Resolve AA
            if(_config.MSAA != MSAALevel.Disabled && !_config.WireframeMode)
            {
                int MSAALevel = (int)_config.MSAA;
                int SamplersPerPixel = MSAALevel * MSAALevel;

                // for (int y=0; y < _height; ++y)
                // {
                //     for(int x=0; x < _width; ++x)
                //     {
                //         int index = GetIndex(x, y);
                //         Color color = Color.clear;
                //         float a = 0.0f;
                //         for(int si=0; si < MSAALevel; ++si)
                //         {
                //             for(int sj=0; sj < MSAALevel; ++sj)
                //             {
                //                 int xi = x * MSAALevel + si;
                //                 int yi = y * MSAALevel + sj;
                //                 int indexSamper = yi * _width * MSAALevel + xi;
                //                 if (samplers_mask_MSAA[indexSamper])
                //                 {
                //                     color += samplers_color_MSAA[indexSamper];
                //                     a += 1.0f;
                //                 }
                //             }
                //         }
                //         if(a > 0.0f)
                //         {
                //             frame_buf[index] = color / SamplersPerPixel;
                //         }
                //     }
                // }
            }

            ProfileManager.EndSample();
        }        

        
        

        

        //Screen space  rasterization
        void RasterizeTriangle(Triangle t, RenderingObject ro)
        {
            ProfileManager.BeginSample("Rasterizer.RasterizeTriangle");
            var v = _tmpVector4s;
            v[0] = t.Vertex0.Position;
            v[1] = t.Vertex1.Position;
            v[2] = t.Vertex2.Position;            
            
            //Find out the bounding box of current triangle.
            float minX = v[0].x;
            float maxX = minX;
            float minY = v[0].y;
            float maxY = minY;

            for(int i=1; i<3; ++i)
            {
                float x = v[i].x;
                if(x < minX)
                {
                    minX = x;
                } else if(x > maxX)
                {
                    maxX = x;
                }
                float y = v[i].y;
                if(y < minY)
                {
                    minY = y;
                }else if(y > maxY)
                {
                    maxY = y;
                }
            }

            int minPX = Mathf.FloorToInt(minX);
            minPX = minPX < 0 ? 0 : minPX;
            int maxPX = Mathf.CeilToInt(maxX);
            maxPX = maxPX > _width ? _width : maxPX;
            int minPY = Mathf.FloorToInt(minY);
            minPY = minPY < 0 ? 0 : minPY;
            int maxPY = Mathf.CeilToInt(maxY);
            maxPY = maxPY > _height ? _height : maxPY;

            if(_config.MSAA == MSAALevel.Disabled)
            {                
                // 遍历当前三角形包围中的所有像素，判断当前像素是否在三角形中
                // 对于在三角形中的像素，使用重心坐标插值得到深度值，并使用z buffer进行深度测试和写入
                for(int y = minPY; y < maxPY; ++y)
                {
                    for(int x = minPX; x < maxPX; ++x)
                    {
                        //if(IsInsideTriangle(x, y, t)) //-->检测是否在三角形内比使用重心坐标检测要慢，因此先计算重心坐标，再检查3个坐标是否有小于0
                        {
                            //计算重心坐标
                            var c = ComputeBarycentric2D(x, y, t);
                            float alpha = c.x;
                            float beta = c.y;
                            float gamma = c.z;
                            if(alpha < 0 || beta < 0 || gamma < 0){                                
                                continue;
                            }
                            //透视校正插值，z为透视校正插值后的view space z值
                            float z = 1.0f / (alpha / v[0].w + beta / v[1].w + gamma / v[2].w);
                            //zp为透视校正插值后的screen space z值
                            float zp = (alpha * v[0].z / v[0].w + beta * v[1].z / v[1].w + gamma * v[2].z / v[2].w) * z;
                            
                            //深度测试(注意我们这儿的z值越大越靠近near plane，因此大值通过测试）
                            int index = GetIndex(x, y);
                            if(zp >= depth_buf[index])
                            {
                                depth_buf[index] = zp;
                                
                                //透视校正插值
                                ProfileManager.BeginSample("Rasterizer.RasterizeTriangle.AttributeInterpolation");
                                Color color_p = (alpha * t.Vertex0.Color / v[0].w + beta * t.Vertex1.Color / v[1].w + gamma * t.Vertex2.Color / v[2].w) * z;
                                Vector2 uv_p = (alpha * t.Vertex0.Texcoord / v[0].w + beta * t.Vertex1.Texcoord / v[1].w + gamma * t.Vertex2.Texcoord / v[2].w) * z;
                                Vector3 normal_p = (alpha * t.Vertex0.Normal / v[0].w + beta * t.Vertex1.Normal  / v[1].w + gamma * t.Vertex2.Normal  / v[2].w) * z;
                                Vector3 worldPos_p = (alpha * t.Vertex0.WorldPos / v[0].w + beta * t.Vertex1.WorldPos / v[1].w + gamma * t.Vertex2.WorldPos / v[2].w) * z;
                                Vector3 worldNormal_p = (alpha * t.Vertex0.WorldNormal / v[0].w + beta * t.Vertex1.WorldNormal / v[1].w + gamma * t.Vertex2.WorldNormal / v[2].w) * z;
                                ProfileManager.EndSample();

                                if (CurrentFragmentShader != null)
                                {
                                    FragmentShaderInputData input = new FragmentShaderInputData();
                                    input.Color = color_p;
                                    input.UV = uv_p;
                                    input.Texture = ro.texture;
                                    input.LocalNormal = normal_p;
                                    input.WorldPos = worldPos_p;
                                    input.WorldNormal = worldNormal_p;

                                    ProfileManager.BeginSample("Rasterizer.RasterizeTriangle.FragmentShader");
                                    frame_buf[index] = CurrentFragmentShader(input);
                                    ProfileManager.EndSample();
                                }
                                

                                
                            }
                        }                        
                    }
                }
            }
            
            ProfileManager.EndSample();
        }

        

        Vector3 ComputeBarycentric2D(float x, float y, Triangle t)
        {
            ProfileManager.BeginSample("Rasterizer.ComputeBarycentric2D");
            var v = _tmpVector4s;            
            v[0] = t.Vertex0.Position;
            v[1] = t.Vertex1.Position;
            v[2] = t.Vertex2.Position;
            
            float c1 = (x * (v[1].y - v[2].y) + (v[2].x - v[1].x) * y + v[1].x * v[2].y - v[2].x * v[1].y) / (v[0].x * (v[1].y - v[2].y) + (v[2].x - v[1].x) * v[0].y + v[1].x * v[2].y - v[2].x * v[1].y);
            float c2 = (x * (v[2].y - v[0].y) + (v[0].x - v[2].x) * y + v[2].x * v[0].y - v[0].x * v[2].y) / (v[1].x * (v[2].y - v[0].y) + (v[0].x - v[2].x) * v[1].y + v[2].x * v[0].y - v[0].x * v[2].y);
            float c3 = (x * (v[0].y - v[1].y) + (v[1].x - v[0].x) * y + v[0].x * v[1].y - v[1].x * v[0].y) / (v[2].x * (v[0].y - v[1].y) + (v[1].x - v[0].x) * v[2].y + v[0].x * v[1].y - v[1].x * v[0].y);
            
            ProfileManager.EndSample();
            return new Vector3(c1, c2, c3);
        }

        public int GetIndex(int x, int y)
        {
            return y * _width + x;
        }



        public void UpdateFrame()
        {
            ProfileManager.BeginSample("CameraRenderer.UpdateFrame");

            // switch (_config.DisplayBuffer)
            // {
            //     case DisplayBufferType.Color:
            //         texture.SetPixels(frame_buf);
            //         break;
            //     case DisplayBufferType.DepthRed:
            //     case DisplayBufferType.DepthGray:
            //         for (int i = 0; i < depth_buf.Length; ++i)
            //         {
            //             //depth_buf中的值范围是[0,1]，且最近处为1，最远处为0。因此可视化后背景是黑色
            //             float c = depth_buf[i]; 
            //             if(_config.DisplayBuffer == DisplayBufferType.DepthRed)
            //             {
            //                 temp_buf[i] = new Color(c, 0, 0);
            //             }
            //             else
            //             {
            //                 temp_buf[i] = new Color(c, c, c);
            //             }                        
            //         }
            //         texture.SetPixels(temp_buf);
            //         break;
            // }                                
            
            // texture.Apply();

            if (StatDelegate != null)
            {
                StatDelegate(_verticesAll, _trianglesAll, _trianglesRendered);
            }

            ProfileManager.EndSample();
        }


    }
}