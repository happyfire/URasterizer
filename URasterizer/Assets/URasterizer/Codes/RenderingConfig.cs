using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace URasterizer {
    [CreateAssetMenu(menuName = "URasterizer/RenderingConfig")]
    public class RenderingConfig : ScriptableObject
    {
        public Color ClearColor = Color.black;
        public bool WireframeMode = false;
        public bool BackfaceCulling = true;
        public DisplayBufferType DisplayBuffer = DisplayBufferType.Color;
        public MSAALevel MSAA = MSAALevel.Disabled;
        public bool BilinearSample = true;
        public ShaderType FragmentShaderType = ShaderType.BlinnPhong;
        public Color AmbientColor = Color.black;
    
        [Header("Vertex Color Setting")]
        public VertexColors VertexColors;

        [Header("Compute Shaders")]
        public bool UseComputeShader;
        public ComputeShader VertexShader;
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