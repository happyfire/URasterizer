void VertexTransform (uint vertexIdx)
{
    float4 pos = float4(vertexBuffer[vertexIdx].x, vertexBuffer[vertexIdx].y, -vertexBuffer[vertexIdx].z, 1.0f);
    float3 normal = float3(normalBuffer[vertexIdx].x, normalBuffer[vertexIdx].y, -normalBuffer[vertexIdx].z);
    
    vertexOutBuffer[vertexIdx].clipPos = mul(matMVP, pos);      
    vertexOutBuffer[vertexIdx].worldPos = mul(matModel, pos).xyz;      
    vertexOutBuffer[vertexIdx].objectNormal = normal;    
    vertexOutBuffer[vertexIdx].worldNormal = mul( (float3x3)matModel , normal);      
    vertexOutBuffer[vertexIdx].uv = uvBuffer[vertexIdx];
}