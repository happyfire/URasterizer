bool Clipped(float4 v[3])
{
    //分别检查视锥体的六个面，如果三角形所有三个顶点都在某个面之外，则该三角形在视锥外，剔除  
    //由于NDC中总是满足-1<=Zndc<=1, 而当 w < 0 时，-w >= Zclip = Zndc*w >= w。所以此时clip space的坐标范围是[w,-w], 为了比较时更明确，将w取正      
    float4 v0 = v[0];
    float w0 = v0.w >=0 ? v0.w : -v0.w;
    float4 v1 = v[1];
    float w1 = v1.w >=0 ? v1.w : -v1.w;
    float4 v2 = v[2];
    float w2 = v2.w >=0 ? v2.w : -v2.w;
    
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



float3 ComputeBarycentric2D(float x, float y, float4 v[3])
{                        
    float c1 = (x * (v[1].y - v[2].y) + (v[2].x - v[1].x) * y + v[1].x * v[2].y - v[2].x * v[1].y) / (v[0].x * (v[1].y - v[2].y) + (v[2].x - v[1].x) * v[0].y + v[1].x * v[2].y - v[2].x * v[1].y);
    float c2 = (x * (v[2].y - v[0].y) + (v[0].x - v[2].x) * y + v[2].x * v[0].y - v[0].x * v[2].y) / (v[1].x * (v[2].y - v[0].y) + (v[0].x - v[2].x) * v[1].y + v[2].x * v[0].y - v[0].x * v[2].y);
    float c3 = (x * (v[0].y - v[1].y) + (v[1].x - v[0].x) * y + v[0].x * v[1].y - v[1].x * v[0].y) / (v[2].x * (v[0].y - v[1].y) + (v[1].x - v[0].x) * v[2].y + v[0].x * v[1].y - v[1].x * v[0].y);                        
    return float3(c1, c2, c3);
}

//v: screen space vertex coordinates
void RasterizeTriangle(int idx0, int idx1, int idx2, float4 v[3])
{        
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

    int minPX = floor(minX);
    minPX = minPX < 0 ? 0 : minPX;
    int maxPX = ceil(maxX);
    maxPX = maxPX > frameBufferSize.x ? frameBufferSize.x : maxPX;
    int minPY = floor(minY);
    minPY = minPY < 0 ? 0 : minPY;
    int maxPY = ceil(maxY);
    maxPY = maxPY > frameBufferSize.y ? frameBufferSize.y : maxPY;

    VertexOutBuf vertex0 = vertexOutBuffer[idx0];
    VertexOutBuf vertex1 = vertexOutBuffer[idx1];
    VertexOutBuf vertex2 = vertexOutBuffer[idx2];

    const float tolerant = -0.0005;

    for(int y = minPY; y < maxPY; ++y)
    {
        for(int x = minPX; x < maxPX; ++x)
        {
            //计算重心坐标
            float3 c = ComputeBarycentric2D(x, y, v);
            float alpha = c.x;
            float beta = c.y;
            float gamma = c.z;
            
            if(alpha < tolerant || beta < tolerant || gamma < tolerant){
                continue;
            }            

            //透视校正插值，z为透视校正插值后的view space z值
            float z = 1.0f / (alpha / v[0].w + beta / v[1].w + gamma / v[2].w);
            //zp为透视校正插值后的screen space z值
            float zp = (alpha * v[0].z / v[0].w + beta * v[1].z / v[1].w + gamma * v[2].z / v[2].w) * z;
            
            //深度测试(注意我们这儿的z值越大越靠近near plane，因此大值通过测试）
            uint PrevDepth;
            uint DepthInt = asuint(zp);
            InterlockedMax(frameDepthTexture[uint2(x, y)], DepthInt, PrevDepth);
            if(DepthInt > PrevDepth)
            {                                                            
                //透视校正插值
                
                float2 uv_p = (alpha * vertex0.uv / v[0].w + beta * vertex1.uv / v[1].w + gamma * vertex2.uv / v[2].w) * z;
                float3 normal_p = (alpha * vertex0.objectNormal / v[0].w + beta * vertex1.objectNormal  / v[1].w + gamma * vertex2.objectNormal  / v[2].w) * z;
                float3 worldPos_p = (alpha * vertex0.worldPos / v[0].w + beta * vertex1.worldPos / v[1].w + gamma * vertex2.worldPos / v[2].w) * z;
                float3 worldNormal_p = (alpha * vertex0.worldNormal / v[0].w + beta * vertex1.worldNormal / v[1].w + gamma * vertex2.worldNormal / v[2].w) * z;
                
                frameColorTexture[uint2(x,y)] = FSBlinnPhong(worldPos_p, worldNormal_p, uv_p);                
            }
        }
    }


    

}
