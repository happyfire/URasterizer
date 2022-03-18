using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace URasterizer
{ 
    public struct TriangleJob : IJobParallelFor
    {                
        [ReadOnly]
        public NativeArray<Vector3Int> trianglesData;  
        [ReadOnly]
        public NativeArray<Vector2> uvData; 
        [ReadOnly]
        public NativeArray<VSOutBuf> vsOutput;

        [NativeDisableParallelForRestriction]
        public NativeArray<Color> frameBuffer;
        [NativeDisableParallelForRestriction]
        public NativeArray<float> depthBuffer;

        public int screenWidth;
        public int screenHeight; 

        [ReadOnly]
        public NativeArray<URColor24> TextureData;   
        public int TextureWidth;
        public int TextureHeight;  

        public bool UseBilinear;   

        public ShaderType fsType;                 
                

        public void Execute(int index)
        {            
            Vector3Int triangle = trianglesData[index];
            int idx0 = triangle.x;
            int idx1 = triangle.y;
            int idx2 = triangle.z;

            var v0 = vsOutput[idx0].clipPos;
            var v1 = vsOutput[idx1].clipPos;
            var v2 = vsOutput[idx2].clipPos;                                  
                
            // ------ Clipping -------
            if (Clipped(v0, v1, v2))
            {
                return;
            }                

            // ------- Perspective division --------
            //clip space to NDC
            v0.x /= v0.w;
            v0.y /= v0.w;
            v0.z /= v0.w;
            v1.x /= v1.w;
            v1.y /= v1.w;
            v1.z /= v1.w;
            v2.x /= v2.w;
            v2.y /= v2.w;
            v2.z /= v2.w;                                            

            //backface culling                
            {
                Vector3 t0 = new Vector3(v0.x, v0.y, v0.z);
                Vector3 t1 = new Vector3(v1.x, v1.y, v1.z);
                Vector3 t2 = new Vector3(v2.x, v2.y, v2.z);
                Vector3 e01 = t1 - t0;
                Vector3 e02 = t2 - t0;
                Vector3 cross = Vector3.Cross(e01, e02);
                if (cross.z < 0)
                {
                    return;
                }
            }

            // ------- Viewport Transform ----------
            //NDC to screen space            
            {

                v0.x = 0.5f * screenWidth * (v0.x + 1.0f);
                v0.y = 0.5f * screenHeight * (v0.y + 1.0f);                
                v0.z = v0.z * 0.5f + 0.5f; 

                v1.x = 0.5f * screenWidth * (v1.x + 1.0f);
                v1.y = 0.5f * screenHeight * (v1.y + 1.0f);                
                v1.z = v1.z * 0.5f + 0.5f; 

                v2.x = 0.5f * screenWidth * (v2.x + 1.0f);
                v2.y = 0.5f * screenHeight * (v2.y + 1.0f);                
                v2.z = v2.z * 0.5f + 0.5f; 
            }

            Triangle t = new Triangle();
            t.Vertex0.Position = v0;
            t.Vertex1.Position = v1;
            t.Vertex2.Position = v2;                

            //set obj normal
            t.Vertex0.Normal = vsOutput[idx0].objectNormal;
            t.Vertex1.Normal = vsOutput[idx1].objectNormal;
            t.Vertex2.Normal = vsOutput[idx2].objectNormal;                
                                
            t.Vertex0.Texcoord = uvData[idx0];
            t.Vertex1.Texcoord = uvData[idx1];
            t.Vertex2.Texcoord = uvData[idx2];                    
                                            
            t.Vertex0.Color = Color.white;
            t.Vertex1.Color = Color.white;
            t.Vertex2.Color = Color.white;
            

            //set world space pos & normal
            t.Vertex0.WorldPos = vsOutput[idx0].worldPos;
            t.Vertex1.WorldPos = vsOutput[idx1].worldPos;
            t.Vertex2.WorldPos = vsOutput[idx2].worldPos;
            t.Vertex0.WorldNormal = vsOutput[idx0].worldNormal;
            t.Vertex1.WorldNormal = vsOutput[idx1].worldNormal;
            t.Vertex2.WorldNormal = vsOutput[idx2].worldNormal;

            RasterizeTriangle(t);
            
        }

        bool Clipped(Vector4 v0, Vector4 v1, Vector4 v2)
        {            
            //分别检查视锥体的六个面，如果三角形所有三个顶点都在某个面之外，则该三角形在视锥外，剔除  
            //由于NDC中总是满足-1<=Zndc<=1, 而当 w < 0 时，-w >= Zclip = Zndc*w >= w。所以此时clip space的坐标范围是[w,-w], 为了比较时更明确，将w取正      
            
            var w0 = v0.w >=0 ? v0.w : -v0.w;            
            var w1 = v1.w >=0 ? v1.w : -v1.w;            
            var w2 = v2.w >=0 ? v2.w : -v2.w;
            
            //left
            if(v0.x < -w0 && v1.x < -w1 && v2.x < -w2){
                return true;
            }
            //right
            if(v0.x > w0 && v1.x > w1 && v2.x > w2){
                return true;
            }
            //bottom
            if(v0.y < -w0 && v1.y < -w1 && v2.y < -w2){
                return true;
            }
            //top
            if(v0.y > w0 && v1.y > w1 && v2.y > w2){
                return true;
            }
            //near
            if(v0.z < -w0 && v1.z < -w1 && v2.z < -w2){
                return true;
            }
            //far
            if(v0.z > w0 && v1.z > w1 && v2.z > w2){
                return true;
            }
            return false;       
        }
        
        Vector3 ComputeBarycentric2D(float x, float y, Triangle t)
        {                      
            var v0 = t.Vertex0.Position;
            var v1 = t.Vertex1.Position;
            var v2 = t.Vertex2.Position;
            
            float c1 = (x * (v1.y - v2.y) + (v2.x - v1.x) * y + v1.x * v2.y - v2.x * v1.y) / (v0.x * (v1.y - v2.y) + (v2.x - v1.x) * v0.y + v1.x * v2.y - v2.x * v1.y);
            float c2 = (x * (v2.y - v0.y) + (v0.x - v2.x) * y + v2.x * v0.y - v0.x * v2.y) / (v1.x * (v2.y - v0.y) + (v0.x - v2.x) * v1.y + v2.x * v0.y - v0.x * v2.y);
            float c3 = (x * (v0.y - v1.y) + (v1.x - v0.x) * y + v0.x * v1.y - v1.x * v0.y) / (v2.x * (v0.y - v1.y) + (v1.x - v0.x) * v2.y + v0.x * v1.y - v1.x * v0.y);
            return new Vector3(c1, c2, c3);
        }
        
        public int GetIndex(int x, int y)
        {
            return y * screenWidth + x;
        }

        void RasterizeTriangle(Triangle t)
        {                        
            var v0 = t.Vertex0.Position;
            var v1 = t.Vertex1.Position;
            var v2 = t.Vertex2.Position;            
            
            //Find out the bounding box of current triangle.
            float minX = (v0.x < v1.x) ? ((v0.x < v2.x)?v0.x : (v1.x < v2.x)?v1.x:v2.x) : ((v1.x < v2.x)?v1.x:(v0.x < v2.x)?v0.x:v2.x);
            float maxX = (v0.x > v1.x) ? ((v0.x > v2.x)?v0.x : (v1.x > v2.x)?v1.x:v2.x) : ((v1.x > v2.x)?v1.x:(v0.x > v2.x)?v0.x:v2.x);
            float minY = (v0.y < v1.y) ? ((v0.y < v2.y)?v0.y : (v1.y < v2.y)?v1.y:v2.y) : ((v1.y < v2.y)?v1.y:(v0.y < v2.y)?v0.y:v2.y);
            float maxY = (v0.y > v1.y) ? ((v0.y > v2.y)?v0.y : (v1.y > v2.y)?v1.y:v2.y) : ((v1.y > v2.y)?v1.y:(v0.y > v2.y)?v0.y:v2.y);                        
                                        

            int minPX = Mathf.FloorToInt(minX);
            minPX = minPX < 0 ? 0 : minPX;
            int maxPX = Mathf.CeilToInt(maxX);
            maxPX = maxPX > screenWidth ? screenWidth : maxPX;
            int minPY = Mathf.FloorToInt(minY);
            minPY = minPY < 0 ? 0 : minPY;
            int maxPY = Mathf.CeilToInt(maxY);
            maxPY = maxPY > screenHeight ? screenHeight : maxPY;

            
                            
            // 遍历当前三角形包围中的所有像素，判断当前像素是否在三角形中
            // 对于在三角形中的像素，使用重心坐标插值得到深度值，并使用z buffer进行深度测试和写入
            for(int y = minPY; y < maxPY; ++y)
            {
                for(int x = minPX; x < maxPX; ++x)
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
                    float z = 1.0f / (alpha / v0.w + beta / v1.w + gamma / v2.w);
                    //zp为透视校正插值后的screen space z值
                    float zp = (alpha * v0.z / v0.w + beta * v1.z / v1.w + gamma * v2.z / v2.w) * z;
                    
                    //深度测试(注意我们这儿的z值越大越靠近near plane，因此大值通过测试）
                    int index = GetIndex(x, y);
                    if(zp >= depthBuffer[index])
                    {
                        depthBuffer[index] = zp;
                        
                        //透视校正插值                            
                        Color color_p = (alpha * t.Vertex0.Color / v0.w + beta * t.Vertex1.Color / v1.w + gamma * t.Vertex2.Color / v2.w) * z;
                        Vector2 uv_p = (alpha * t.Vertex0.Texcoord / v0.w + beta * t.Vertex1.Texcoord / v1.w + gamma * t.Vertex2.Texcoord / v2.w) * z;
                        Vector3 normal_p = (alpha * t.Vertex0.Normal / v0.w + beta * t.Vertex1.Normal  / v1.w + gamma * t.Vertex2.Normal  / v2.w) * z;
                        Vector3 worldPos_p = (alpha * t.Vertex0.WorldPos / v0.w + beta * t.Vertex1.WorldPos / v1.w + gamma * t.Vertex2.WorldPos / v2.w) * z;
                        Vector3 worldNormal_p = (alpha * t.Vertex0.WorldNormal / v0.w + beta * t.Vertex1.WorldNormal / v1.w + gamma * t.Vertex2.WorldNormal / v2.w) * z;                            
                        
                        FragmentShaderInputData input = new FragmentShaderInputData();
                        input.Color = color_p;
                        input.UV = uv_p;
                        input.TextureData = TextureData;
                        input.TextureWidth = TextureWidth;
                        input.TextureHeight = TextureHeight;
                        input.UseBilinear = UseBilinear;
                        input.LocalNormal = normal_p;
                        input.WorldPos = worldPos_p;
                        input.WorldNormal = worldNormal_p;

                        switch(fsType){
                            case ShaderType.BlinnPhong:
                                frameBuffer[index] = ShaderContext.FSBlinnPhong(input);
                                break;
                            case ShaderType.NormalVisual:
                                frameBuffer[index] = ShaderContext.FSNormalVisual(input);
                                break;
                            case ShaderType.VertexColor:
                                frameBuffer[index] = ShaderContext.FSVertexColor(input);
                                break;
                        }                                                                                             
                        
                    }                                            
                }
            }                                    
        }

    }
}