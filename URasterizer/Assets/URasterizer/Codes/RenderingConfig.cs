using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace URasterizer {
    [CreateAssetMenu(menuName = "URasterizer/RenderingConfig")]
    public class RenderingConfig : ScriptableObject
    {
        [Header("Common Setting")]        
        public Color ClearColor = Color.black;
        public Color AmbientColor = Color.black;        
        public RasterizerType RasterizerType;
        public bool UseUnityNativeRendering;

        [Header("CPU Rasterizer ONLY Setting")]
        public bool WireframeMode = false;
        public bool BackfaceCulling = true;
        public DisplayBufferType DisplayBuffer = DisplayBufferType.Color;
        public MSAALevel MSAA = MSAALevel.Disabled;
        public bool BilinearSample = true;
        public ShaderType FragmentShaderType = ShaderType.BlinnPhong;
                    
        public VertexColors VertexColors;

        [Header("GPU Driven Setting")]        
        public ComputeShader ComputeShader;        
    }

    public enum DisplayBufferType
    {
        Color,
        DepthRed,
        DepthGray
    }

    public enum MSAALevel
    {
        Disabled,
        X2 = 2,
        X4 = 4
    }

}