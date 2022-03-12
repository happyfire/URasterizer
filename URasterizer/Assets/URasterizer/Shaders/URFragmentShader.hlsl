
Texture2D<float4> meshTexture;
SamplerState samplermeshTexture; 

float4 FSBlinnPhong(float3 worldPos, float3 worldNormal, float2 uv)
{
    float4 textureColor = meshTexture.SampleLevel(samplermeshTexture, uv, 0);  //Compute shader中不能使用Sample                    

    float4 ks = float4(0.7937f, 0.7937f, 0.7937f, 1.0f);

    float ndotl = dot(worldNormal, worldSpaceLightDir);
    float4 diffuse = textureColor * lightColor * saturate(ndotl);

    float3 viewDir = normalize(worldSpaceCameraPos - worldPos);    
    float3 halfDir = normalize(viewDir + worldSpaceLightDir);
    
    float hdotn = dot(halfDir, worldNormal);
    float4 specular = ks * lightColor * pow(saturate(hdotn), 150);    

    return ambientColor + diffuse + specular;
}
