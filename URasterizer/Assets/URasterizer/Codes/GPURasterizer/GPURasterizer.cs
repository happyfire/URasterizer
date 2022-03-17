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

        RenderTexture _colorTexture;
        RenderTexture _depthTexture;        

        //Stats
        int _trianglesAll, _trianglesRendered;
        int _verticesAll;

        public OnRasterizerStatUpdate StatDelegate;

        //Compute shader
        ComputeShader computeShader;
        int kernelClearFrame;
        int kernelVertexProcess;
        int kernelTriangleProcess;        

        //ids of compute shader variables                
        int vertexBufferId;
        int normalBufferId;
        int uvBufferId;
        int triangleBufferId;
        int vertexOutBufferId;
        int frameColorTextureId;
        int frameDepthTextureId;        
        
        int clearColorId;
        int matMVPId;
        int matModelId;
        int frameBufferSizeId;

        int worldSpaceCameraPosId;
        int worldSpaceLightDirId;
        int lightColorId;
        int ambientColorId;

        int meshTextureId;

        public String Name { get=>"GPU Driven"; }

        public Texture ColorTexture { 
            get => _colorTexture;                            
        }


        public GPURasterizer(int w, int h, RenderingConfig config)
        {
            Debug.Log($"GPURasterizer screen size: {w}x{h}");

            _config = config;

            _width = w;
            _height = h;

            _colorTexture = new RenderTexture(w, h, 0);
            _colorTexture.enableRandomWrite = true;     
            _colorTexture.Create();       
            _colorTexture.filterMode = FilterMode.Point;

            _depthTexture = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat);
            _depthTexture.enableRandomWrite = true;
            _depthTexture.Create();
            _depthTexture.filterMode = FilterMode.Point;


            //init for compute shader
            computeShader = config.ComputeShader;
            kernelClearFrame = computeShader.FindKernel("ClearFrame");
            kernelVertexProcess = computeShader.FindKernel("VertexProcess");
            kernelTriangleProcess = computeShader.FindKernel("TriangleProcess");            
            
            vertexBufferId = Shader.PropertyToID("vertexBuffer");
            normalBufferId = Shader.PropertyToID("normalBuffer");
            uvBufferId = Shader.PropertyToID("uvBuffer");
            triangleBufferId = Shader.PropertyToID("triangleBuffer");            
            vertexOutBufferId = Shader.PropertyToID("vertexOutBuffer");
            frameColorTextureId = Shader.PropertyToID("frameColorTexture");
            frameDepthTextureId = Shader.PropertyToID("frameDepthTexture");

            clearColorId = Shader.PropertyToID("clearColor");
            matMVPId = Shader.PropertyToID("matMVP");
            matModelId = Shader.PropertyToID("matModel");
            frameBufferSizeId = Shader.PropertyToID("frameBufferSize");

            worldSpaceCameraPosId = Shader.PropertyToID("worldSpaceCameraPos");
            worldSpaceLightDirId = Shader.PropertyToID("worldSpaceLightDir");
            lightColorId = Shader.PropertyToID("lightColor");
            ambientColorId = Shader.PropertyToID("ambientColor");
            meshTextureId = Shader.PropertyToID("meshTexture");
        }

        public void Release()
        {
            _colorTexture = null;
            _depthTexture = null;
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
            ProfileManager.BeginSample("GPURasterizer.Clear");            
            var shader = _config.ComputeShader;                        
            shader.SetTexture(kernelClearFrame, frameColorTextureId, _colorTexture);   
            shader.SetTexture(kernelClearFrame, frameDepthTextureId, _depthTexture);   
            var clearColor = _config.ClearColor;
            shader.SetFloats(clearColorId, clearColor.r, clearColor.g, clearColor.b, clearColor.a);         
            
            int groupX = Mathf.CeilToInt(_colorTexture.width/32f);
            int groupY = Mathf.CeilToInt(_colorTexture.height/24f);                       
            shader.Dispatch(kernelClearFrame, groupX, groupY, 1); 
            
            _trianglesAll = _trianglesRendered = 0;
            _verticesAll = 0;

            ProfileManager.EndSample();            
        }       

        public void SetupUniforms(Camera camera, Light mainLight)
        {
            var shader = _config.ComputeShader;  

            var camPos = camera.transform.position;
            camPos.z *= -1;
            shader.SetFloats(worldSpaceCameraPosId, camPos.x, camPos.y, camPos.z);            

            var lightDir = mainLight.transform.forward;
            lightDir.z *= -1;
            shader.SetFloats(worldSpaceLightDirId, -lightDir.x, -lightDir.y, -lightDir.z);            
            
            var lightColor = mainLight.color * mainLight.intensity;
            shader.SetFloats(lightColorId, lightColor.r, lightColor.g, lightColor.b, lightColor.a);
            
            shader.SetFloats(ambientColorId, _config.AmbientColor.r, _config.AmbientColor.g, _config.AmbientColor.b, _config.AmbientColor.a);
                        

            TransformTool.SetupViewProjectionMatrix(camera, Aspect, out _matView, out _matProjection);
        }

        public void DrawObject(RenderingObject ro)
        {
            ProfileManager.BeginSample("GPURasterizer.Draw");

            Mesh mesh = ro.mesh;            

            _matModel = ro.GetModelMatrix();                      

            Matrix4x4 mvp = _matProjection * _matView * _matModel;
            Matrix4x4 normalMat = _matModel.inverse.transpose;

            int triangleCount = ro.cpuData.MeshTriangles.Length / 3;

            _verticesAll += mesh.vertexCount;
            _trianglesAll += triangleCount;                           
            
            ProfileManager.BeginSample("GPURasterizer.VertexProcess");                

            var shader = _config.ComputeShader;            
            shader.SetMatrix(matMVPId, mvp);
            shader.SetMatrix(matModelId, _matModel);
            shader.SetBuffer(kernelVertexProcess, vertexBufferId, ro.gpuData.VertexBuffer);
            shader.SetBuffer(kernelVertexProcess, normalBufferId, ro.gpuData.NormalBuffer);
            shader.SetBuffer(kernelVertexProcess, uvBufferId, ro.gpuData.UVBuffer);
            shader.SetBuffer(kernelVertexProcess, vertexOutBufferId, ro.gpuData.VertexOutBuffer);
            
            int groupCnt = Mathf.CeilToInt(mesh.vertexCount/512f);            
            shader.Dispatch(kernelVertexProcess, groupCnt, 1, 1);                          

            ProfileManager.EndSample();                                     
                                   
            ProfileManager.BeginSample("GPURasterizer.TriangleProcess");

            shader.SetInts(frameBufferSizeId, _colorTexture.width, _colorTexture.height);            
            shader.SetBuffer(kernelTriangleProcess, triangleBufferId, ro.gpuData.TriangleBuffer);            
            shader.SetBuffer(kernelTriangleProcess, vertexOutBufferId, ro.gpuData.VertexOutBuffer);            
            shader.SetTexture(kernelTriangleProcess, frameColorTextureId, _colorTexture);   
            shader.SetTexture(kernelTriangleProcess, frameDepthTextureId, _depthTexture);   
            shader.SetTexture(kernelTriangleProcess, meshTextureId, ro.texture);

            groupCnt = Mathf.CeilToInt(triangleCount/512f);            
            shader.Dispatch(kernelTriangleProcess, groupCnt, 1, 1);                        
            
            ProfileManager.EndSample();                                   
                            
        }        

        public void UpdateFrame()
        {                     
            if (StatDelegate != null)
            {
                StatDelegate(_verticesAll, _trianglesAll, _trianglesRendered);
            }            
        }


    }
}