using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace URasterizer
{
    public class Triangle
    {
        public Vector4[] Positions = new Vector4[3];
        public Vector3[] Normals = new Vector3[3];
        public Color[] Colors = new Color[3];
        public Vector2[] TexCoords = new Vector2[3];

        public Vector3[] WorldPoses = new Vector3[3];
        public Vector3[] WorldNormals = new Vector3[3];
        

        public Triangle()
        {

        }

        public void SetPosition(int idx, Vector4 vertex)
        {
            Positions[idx] = vertex;
        }
        public void SetNormal(int idx, Vector3 normal)
        {
            Normals[idx] = normal;
        }

        public void SetColor(int idx, Color color)
        {
            Colors[idx] = color;
        }

        public void SetTexCoord(int idx, Vector2 uv)
        {
            TexCoords[idx] = uv;
        }

        public void SetWorldPos(int idx, Vector3 pos)
        {
            WorldPoses[idx] = pos;
        }

        public void SetWorldNormal(int idx, Vector3 normal)
        {
            WorldNormals[idx] = normal;
        }
    }
}