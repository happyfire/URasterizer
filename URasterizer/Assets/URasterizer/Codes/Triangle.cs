using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace URasterizer
{
    public struct TriangleVertex
    {
        public Vector4 Position;
        public Vector3 Normal;
        public Color Color;
        public Vector2 Texcoord;
        public Vector3 WorldPos;
        public Vector3 WorldNormal;
    }

    public struct Triangle
    {
        public TriangleVertex Vertex0;
        public TriangleVertex Vertex1;
        public TriangleVertex Vertex2;                
    }
}