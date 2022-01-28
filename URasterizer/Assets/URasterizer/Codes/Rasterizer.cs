using System;
using System.Collections.Generic;
using UnityEngine;

namespace URasterizer
{
    public enum BufferMask
    {
        Color = 1,
        Depth = 2
    }

    public class Rasterizer
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

        public Texture2D texture;        

        //Stats
        public int Stats_Triangles;
        public int Stats_Vertices;

        public Rasterizer(int w, int h, RenderingConfig config)
        {
            Debug.Log($"Rasterizer screen size: {w}x{h}");

            _config = config;

            _width = w;
            _height = h;

            frame_buf = new Color[w * h];
            depth_buf = new float[w * h];

            texture = new Texture2D(w, h);
            texture.filterMode = FilterMode.Point;
        }

        public float Aspect
        {
            get
            {
                return (float)_width / _height;
            }
        }

        void FillArray<T>(T[] arr, T value)
        {
            int i = 0;
            if(arr.Length > 16)
            {
                do
                {
                    arr[i++] = value;
                } while (i < arr.Length);

                while( i + 16 < arr.Length)
                {
                    Array.Copy(arr, 0, arr, i, 16);
                    i += 16;
                }
            }
            while (i < arr.Length)
            {
                arr[i++] = value;
            }
        }

        public void Clear(BufferMask mask)
        {
            if((mask & BufferMask.Color) == BufferMask.Color)
            {                
                FillArray(frame_buf, _config.ClearColor);
            }
            if((mask & BufferMask.Depth) == BufferMask.Depth)
            {
                FillArray(depth_buf, float.MaxValue);
            }
        }

        public void SetupViewProjectionMatrix(Camera camera)
        {
            var camPos = camera.transform.position;
            camPos.z *= -1;
            ViewMatrix = TransformTool.GetViewMatrix(camPos);

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
            Mesh mesh = ro.mesh;
            SetupViewProjectionMatrix(camera);

            ModelMatrix = ro.GetModelMatrix();                      

            Matrix4x4 mvp = _matProjection * _matView * _matModel;


            var indices = mesh.triangles;
            for(int i=0; i< indices.Length; i+=3)
            {
                //Unity模型本地坐标系也是左手系，需要转成我们使用的右手系
                //1. z轴反转
                //2. 三角形顶点环绕方向从顺时针改成逆时针

                //注意这儿对调了v0和v1的索引，因为原来的 0,1,2是顺时针的，对调后是 1,0,2是逆时针的
                //Unity Quard模型的两个三角形索引分别是 0,3,1,3,0,2 转换后为 3,0,1,0,3,2
                int idx0 = indices[i+1];
                int idx1 = indices[i]; 
                int idx2 = indices[i+2];
                                
                //vertex shader

                //world to clip space
                Vector4[] v =
                {
                    mvp * new Vector4(mesh.vertices[idx0].x, mesh.vertices[idx0].y, -mesh.vertices[idx0].z, 1), //注意这儿反转了z坐标
                    mvp * new Vector4(mesh.vertices[idx1].x, mesh.vertices[idx1].y, -mesh.vertices[idx1].z, 1),
                    mvp * new Vector4(mesh.vertices[idx2].x, mesh.vertices[idx2].y, -mesh.vertices[idx2].z, 1),
                };
                

                //do clipping
                if (Clipped(v))
                {
                    continue;
                }               
                
                //clip space to NDC (Perspective division)                 
                for(int k=0; k<3; k++)
                {
                    v[k].x /= v[k].w;
                    v[k].y /= v[k].w;
                    v[k].z /= v[k].w;                  
                }

                //backface culling
                if (_config.BackfaceCulling && !ro.DoubleSideRendering)
                {
                    Vector3 v0 = new Vector3(v[0].x, v[0].y, v[0].z);
                    Vector3 v1 = new Vector3(v[1].x, v[1].y, v[1].z);
                    Vector3 v2 = new Vector3(v[2].x, v[2].y, v[2].z);
                    Vector3 e01 = v1 - v0;
                    Vector3 e02 = v2 - v0;
                    Vector3 cross = Vector3.Cross(e01, e02);
                    if (cross.z < 0)
                    {
                        continue;
                    }
                }


                //NDC to screen space, viewport transform
                for (int k = 0; k < 3; k++)
                {
                    var vec = v[k];
                    vec.x = 0.5f * _width * (vec.x + 1.0f);
                    vec.y = 0.5f * _height * (vec.y + 1.0f);

                    //GAMES101约定的NDC是右手坐标系，z值范围是[-1,1]，但n为1，f为-1，因此值越大越靠近n。
                    //为了符合深度值越小离camera越近的惯例，将View space的Z值反转一下，这样后面深度测试的时候就可以使用LESS_EQUAL测试了
                    vec.z = -vec.z; 

                    v[k] = vec;
                }

             
                Triangle t = new Triangle();
                for(int k=0; k<3; k++)
                {
                    t.SetPosition(k, v[k]);
                }

                //设置顶点色,使用config中的颜色数组循环设置
                int vertexColorCount = _config.VertexColors.Length;
                if(vertexColorCount > 0)
                {
                    t.SetColor(0, _config.VertexColors[idx0 % vertexColorCount]);
                    t.SetColor(1, _config.VertexColors[idx1 % vertexColorCount]);
                    t.SetColor(2, _config.VertexColors[idx2 % vertexColorCount]);
                }
                else
                {
                    t.SetColor(0, Color.white);
                    t.SetColor(1, Color.white);
                    t.SetColor(2, Color.white);
                }                             

                //Rasterization
                if (_config.WireframeMode)
                {
                    RasterizeWireframe(t);
                }
                else
                {
                    RasterizeTriangle(t);
                }
                
            }
        }

        bool Clipped(Vector4[] v)
        {
            //Clip space使用GAMES101规范，右手坐标系，n为+1， f为-1
            //裁剪（仅整体剔除）     
            //实际的硬件是在Clip space裁剪，所以此处我们也使用clip space （当然由于我们不真正的裁剪，只是整体剔除，所以其实在NDC操作更方便）
            for (int i = 0; i < 3; ++i)
            {
                var vertex = v[i];
                var w = vertex.w;
                w = w >= 0 ? w : -w; //由于NDC中总是满足-1<=Zndc<=1, 而当 w < 0 时，-w >= Zclip = Zndc*w >= w。所以此时clip space的坐标范围是[w,-w], 为了比较时更明确，将w取正
                //Debug.LogError("w=" + w);
                bool inside = (vertex.x <= w && vertex.x >= -w
                    && vertex.y <= w && vertex.y >= -w
                    && vertex.z <= w && vertex.z >= -w);
                if (inside)
                {             
                    //不裁剪三角形，只要有任意一点在clip space中则三角形整体保留
                    return false;
                }
            }

            //三个顶点都不在三角形中则剔除
            return true;
        }

        #region Wireframe mode
        private void DrawLine(Vector3 begin, Vector3 end, Color colorBegin, Color colorEnd)
        {            
            int x1 = Mathf.FloorToInt(begin.x);
            int y1 = Mathf.FloorToInt(begin.y);
            int x2 = Mathf.FloorToInt(end.x);
            int y2 = Mathf.FloorToInt(end.y);            

            int x, y, dx, dy, dx1, dy1, px, py, xe, ye, i;

            dx = x2 - x1;
            dy = y2 - y1;
            dx1 = Math.Abs(dx);
            dy1 = Math.Abs(dy);
            px = 2 * dy1 - dx1;
            py = 2 * dx1 - dy1;            

            if (dy1 <= dx1)
            {
                if (dx >= 0)
                {
                    x = x1;
                    y = y1;
                    xe = x2;
                }
                else
                {
                    x = x2;
                    y = y2;
                    xe = x1;
                }
                Vector3 point = new Vector3(x, y, 1.0f);
                Color line_color = dx >= 0 ? colorBegin : colorEnd;
                SetPixel(point, line_color);
                for (i = 0; x < xe; i++)
                {
                    x++;
                    if (px < 0)
                    {
                        px += 2 * dy1;
                    }
                    else
                    {
                        if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0))
                        {
                            y++;
                        }
                        else
                        {
                            y--;
                        }
                        px +=  2 * (dy1 - dx1);
                    }
                    
                    Vector3 pt = new Vector3(x, y, 1.0f);
                    float t = (float)(xe - x) / dx1;
                    line_color = Color.Lerp(colorBegin, colorEnd, t);                    
                    SetPixel(pt, line_color);
                }
            }
            else
            {
                if (dy >= 0)
                {
                    x = x1;
                    y = y1;
                    ye = y2;
                }
                else
                {
                    x = x2;
                    y = y2;
                    ye = y1;
                }
                Vector3 point = new Vector3(x, y, 1.0f);
                Color line_color = dy >= 0 ? colorBegin : colorEnd;
                SetPixel(point, line_color);
                
                for (i = 0; y < ye; i++)
                {
                    y++;
                    if (py <= 0)
                    {
                        py += 2 * dx1;
                    }
                    else
                    {
                        if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0))
                        {
                            x++;
                        }
                        else
                        {
                            x--;
                        }
                        py += 2 * (dx1 - dy1);
                    }
                    Vector3 pt = new Vector3(x, y, 1.0f);
                    float t = (float)(ye - y) / dy1;
                    line_color = Color.Lerp(colorBegin, colorEnd, t);
                    SetPixel(pt, line_color);
                }
            }
        }

        private void RasterizeWireframe(Triangle t)
        {
            DrawLine(t.Positions[0], t.Positions[1], t.Colors[0], t.Colors[1]);
            DrawLine(t.Positions[1], t.Positions[2], t.Colors[1], t.Colors[2]);
            DrawLine(t.Positions[2], t.Positions[0], t.Colors[2], t.Colors[0]);
        }

        #endregion

        

        //Screen space  rasterization
        void RasterizeTriangle(Triangle t)
        {
            var v = t.Positions;
            
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

  
            {                
                // 遍历当前三角形包围中的所有像素，判断当前像素是否在三角形中
                // 对于在三角形中的像素，使用重心坐标插值得到深度值，并使用z buffer进行深度测试和写入
                for(int y = minPY; y < maxPY; ++y)
                {
                    for(int x = minPX; x < maxPX; ++x)
                    {
                        if(IsInsideTriangle(x, y, t))
                        {
                            //计算重心坐标
                            var c = ComputeBarycentric2D(x, y, t);
                            float alpha = c.x;
                            float beta = c.y;
                            float gamma = c.z;
                            //透视校正插值
                            float w_reciprocal = 1.0f / (alpha / v[0].w + beta / v[1].w + gamma / v[2].w);
                            float z_interpolated = alpha * v[0].z / v[0].w + beta * v[1].z / v[1].w + gamma * v[2].z / v[2].w;
                            z_interpolated *= w_reciprocal;
                            //深度测试
                            int index = GetIndex(x, y);
                            if(z_interpolated <= depth_buf[index])
                            {
                                depth_buf[index] = z_interpolated;
                                
                                Color color_interpolated = alpha * t.Colors[0] / v[0].w + beta * t.Colors[1] / v[1].w + gamma * t.Colors[2] / v[2].w;
                                color_interpolated *= w_reciprocal;
                                frame_buf[index] = color_interpolated;
                            }
                        }                        
                    }
                }
            }
        }

        bool IsInsideTriangle(int x, int y, Triangle t, float offsetX=0.5f, float offsetY=0.5f)
        {
            Vector3[] v = new Vector3[3];
            for(int i=0; i<3; ++i)
            {
                v[i] = new Vector3(t.Positions[i].x, t.Positions[i].y, t.Positions[i].z);
            }

            //当前像素中心位置p
            Vector3 p = new Vector3(x + offsetX, y + offsetY, 0);            
            
            Vector3 v0p = p - v[0]; v0p[2] = 0;
            Vector3 v01 = v[1] - v[0]; v01[2] = 0;
            Vector3 cross0p = Vector3.Cross(v0p, v01);

            Vector3 v1p = p - v[1]; v1p[2] = 0;
            Vector3 v12 = v[2] - v[1]; v12[2] = 0;
            Vector3 cross1p = Vector3.Cross(v1p, v12);

            if(cross0p.z * cross1p.z > 0)
            {
                Vector3 v2p = p - v[2]; v2p[2] = 0;
                Vector3 v20 = v[0] - v[2]; v20[2] = 0;
                Vector3 cross2p = Vector3.Cross(v2p, v20);
                if(cross2p.z * cross1p.z > 0)
                {
                    return true;
                }
            }

            return false;
        }

        Vector3 ComputeBarycentric2D(float x, float y, Triangle t)
        {
            var v = t.Positions;
            float c1 = (x * (v[1].y - v[2].y) + (v[2].x - v[1].x) * y + v[1].x * v[2].y - v[2].x * v[1].y) / (v[0].x * (v[1].y - v[2].y) + (v[2].x - v[1].x) * v[0].y + v[1].x * v[2].y - v[2].x * v[1].y);
            float c2 = (x * (v[2].y - v[0].y) + (v[0].x - v[2].x) * y + v[2].x * v[0].y - v[0].x * v[2].y) / (v[1].x * (v[2].y - v[0].y) + (v[0].x - v[2].x) * v[1].y + v[2].x * v[0].y - v[0].x * v[2].y);
            float c3 = (x * (v[0].y - v[1].y) + (v[1].x - v[0].x) * y + v[0].x * v[1].y - v[1].x * v[0].y) / (v[2].x * (v[0].y - v[1].y) + (v[1].x - v[0].x) * v[2].y + v[0].x * v[1].y - v[1].x * v[0].y);
            return new Vector3(c1, c2, c3);
        }

        public int GetIndex(int x, int y)
        {
            return y * _width + x;
        }

        public void SetPixel(Vector3 point, Color color)
        {
            if(point.x < 0 || point.x >= _width || point.y < 0 || point.y >= _height)
            {
                return;
            }

            int idx = (int)point.y * _width + (int)point.x;
            frame_buf[idx] = color;
        }

        public void UpdateFrame()
        {
            texture.SetPixels(frame_buf);
            texture.Apply();
        }


    }
}