using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="URasterizer/RenderingConfig")]
public class RenderingConfig : ScriptableObject
{
    public Color ClearColor = Color.black;
    public bool WireframeMode = false;
    public bool BackfaceCulling = true;
    public DisplayBufferType DisplayBuffer = DisplayBufferType.Color;
    
    
    [Header("Vertex Color Setting")]    
    public Color[] VertexColors;
}

public enum DisplayBufferType
{
    Color,
    DepthRed,
    DepthGray
}
