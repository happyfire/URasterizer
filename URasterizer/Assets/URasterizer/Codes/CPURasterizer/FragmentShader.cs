using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace URasterizer
{
    //Shader在世界空间下计算

    public struct URColor24
    {
        public byte r;
        public byte g;
        public byte b;

        public static implicit operator Color(URColor24 c)
        {
            float s = 1/255f;
            return new Color(c.r * s, c.g * s, c.b * s, 1);            
        }
    }

    public struct FragmentShaderInputData
    {
        public Vector3 WorldPos;
        public Vector3 WorldNormal;
        public Vector3 LocalNormal;
        public Color Color;
        public Vector2 UV;        
        public NativeArray<URColor24> TextureData; //因为我们的纹理都是RGB格式的(24位)，所以不能用Color32(32位)
        public int TextureWidth;
        public int TextureHeight;
    }

    public struct ShaderUniforms
    {        
        public Vector3 WorldSpaceCameraPos;
        public Vector3 WorldSpaceLightDir;
        public Color LightColor;
        public Color AmbientColor;
    }

    public delegate Color FragmentShader(FragmentShaderInputData input);

    public enum ShaderType
    {
        VertexColor,
        BlinnPhong,
        NormalVisual
    }

    public class ShaderContext
    {
        public static ShaderUniforms Uniforms;
        public static RenderingConfig Config;

        public static Color FSVertexColor(FragmentShaderInputData input)
        {
            return input.Color;
        }

        public static Color FSBlinnPhong(FragmentShaderInputData input)
        {
            
            int w = input.TextureWidth;
            int h = input.TextureHeight;
            int x = (int)(w * input.UV.x);
            int y = (int)(h * input.UV.y);
            int tidx = y*w + x;
                                    
            URColor24 c = input.TextureData[tidx];            
            Color textureColor = (URColor24)c;
            // if (input.Texture != null)
            // {
            //     int w = input.Texture.width;
            //     int h = input.Texture.height;
            //     if (Config.BilinearSample)
            //     {
            //         textureColor = input.Texture.GetPixelBilinear(input.UV.x, input.UV.y);
            //     }
            //     else
            //     {
            //         textureColor = input.Texture.GetPixel((int)(w * input.UV.x), (int)(h * input.UV.y));
            //     }
                
            // }
                        
            Color ambient = Uniforms.AmbientColor;

            Color ks = new Color(0.7937f, 0.7937f, 0.7937f);

            float ndotl = Vector3.Dot(input.WorldNormal, Uniforms.WorldSpaceLightDir);
            Color diffuse = textureColor * Uniforms.LightColor * Mathf.Max(0f, ndotl);

            Vector3 viewDir = Uniforms.WorldSpaceCameraPos - input.WorldPos;
            viewDir.Normalize();
            Vector3 halfDir = (viewDir + Uniforms.WorldSpaceLightDir);
            halfDir.Normalize();
            float hdotn = Vector3.Dot(halfDir, input.WorldNormal);
            Color specular = ks * Uniforms.LightColor * Mathf.Pow(Mathf.Max(0f, hdotn), 150);
            


            return ambient + diffuse + specular;
        }

        public static Color FSNormalVisual(FragmentShaderInputData input)
        {
            Vector3 tmp = input.LocalNormal * 0.5f + new Vector3(0.5f,0.5f,0.5f);
            return new Color(tmp.x, tmp.y, tmp.z);
        }


    }

    
}
