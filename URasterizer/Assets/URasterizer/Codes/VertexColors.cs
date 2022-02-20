using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "URasterizer/VertexColors")]
public class VertexColors : ScriptableObject
{    
    [Header("Vertex Color Setting")]
    public Color[] Colors;
}
