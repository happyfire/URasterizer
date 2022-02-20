using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace URasterizer
{
    //Shader在世界空间下计算

    public struct FragmentShaderInputData
    {
        public Vector3 WorldPos;
        public Vector3 WorldNormal;
        public Vector3 LocalNormal;
        public Color Color;
        public Vector2 UV;
        public Texture2D Texture;
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
            Color textureColor = Color.white;
            if (input.Texture != null)
            {
                int w = input.Texture.width;
                int h = input.Texture.height;
                if (Config.BilinearSample)
                {
                    textureColor = input.Texture.GetPixelBilinear(input.UV.x, input.UV.y);
                }
                else
                {
                    textureColor = input.Texture.GetPixel((int)(w * input.UV.x), (int)(h * input.UV.y));
                }
                
            }
                        
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
