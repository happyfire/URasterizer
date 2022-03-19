struct VertexOutBuf
{
    float4 clipPos;
    float3 worldPos;
    float3 objectNormal;
    float3 worldNormal;
    float2 uv;
};

float4 clearColor;
int2 frameBufferSize;

float4x4 matMVP;
float4x4 matModel;

float3 worldSpaceCameraPos;
float3 worldSpaceLightDir;
float4 lightColor;
float4 ambientColor;

StructuredBuffer<float3> vertexBuffer;
StructuredBuffer<float3> normalBuffer;
StructuredBuffer<float2> uvBuffer;
StructuredBuffer<uint3> triangleBuffer; // All triangles of the mesh

RWStructuredBuffer<VertexOutBuf> vertexOutBuffer;
RWTexture2D<float4> frameColorTexture;
RWTexture2D<uint> frameDepthTexture;

