
using UnityEngine;
using Unity.Collections;
using System.Runtime.CompilerServices;

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
        public bool UseBilinear;
    }

    public struct ShaderUniforms
    {        
        public Vector3 WorldSpaceCameraPos;
        public Vector3 WorldSpaceLightDir;
        public Color LightColor;
        public Color AmbientColor;
    }    

    public enum ShaderType
    {
        VertexColor,
        BlinnPhong,
        NormalVisual
    }

    public class ShaderContext
    {        
        public static RenderingConfig Config;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color FSVertexColor(FragmentShaderInputData input)
        {
            return input.Color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Color GetTextureColor(NativeArray<URColor24> textureData, int w, int h, int x, int y)
        {      
            int tidx = y*w + x;                                    
            URColor24 c = textureData[tidx];            
            return (Color)c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color FSBlinnPhong(FragmentShaderInputData input, ShaderUniforms Uniforms)
        {
            Color textureColor;
            int w = input.TextureWidth;
            int h = input.TextureHeight;
                        
            if(input.UseBilinear)
            {
                float u_img = input.UV.x * (w-1);
                int u_img_i = (int)(u_img);
                int u0 = u_img < u_img_i + 0.5 ? u_img_i - 1 : u_img_i;
                if(u0<0) u0 = 0;
                int u1 = u0 + 1;
                float s = u_img - (u0 + 0.5f);

                float v_img = input.UV.y * (h-1);
                int v_img_i = (int)(v_img);        
                int v0 = v_img < v_img_i + 0.5 ? v_img_i-1 : v_img_i;
                if(v0<0) v0 = 0;
                int v1 = v0 + 1;
                float t = v_img - (v0 + 0.5f); 

                var color_00 = GetTextureColor(input.TextureData, w, h, u0, v0);
                var color_10 = GetTextureColor(input.TextureData, w, h, u1, v0);
                var color_0 = Color.Lerp(color_00, color_10, s);

                var color_01 = GetTextureColor(input.TextureData, w, h, u0, v1);
                var color_11 = GetTextureColor(input.TextureData, w, h, u1, v1);
                var color_1 = Color.Lerp(color_01, color_11, s);

                textureColor = Color.Lerp(color_0, color_1, t);

            }
            else
            {
                int x = (int)((w-1) * input.UV.x);
                int y = (int)((h-1) * input.UV.y);                            
                textureColor = GetTextureColor(input.TextureData, w, h, x, y);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color FSNormalVisual(FragmentShaderInputData input)
        {
            Vector3 tmp = input.LocalNormal * 0.5f + new Vector3(0.5f,0.5f,0.5f);
            return new Color(tmp.x, tmp.y, tmp.z);
        }


    }

    
}
