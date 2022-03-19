using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace URasterizer
{
    //手动设置的mesh            
    //手动模型也使用左手系
    public class HandmakeMesh : MonoBehaviour
    {
        public Vector3[] Vertices = { 
                            new Vector3(1f, 0f, 2f), new Vector3(0f, 2f, 2f), new Vector3(-1f, 0f, 2f),
                            new Vector3(1.5f, 0.5f, 1.5f), new Vector3(0.5f, 2.5f, 1.5f), new Vector3(-0.5f, 0.5f, 1.5f)};

        public Vector2[] UVs = {
                            new Vector2(1f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0f),
                            new Vector2(1f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0f)};

        public int[] Indices =
        {
            0, 2, 1, 3, 5, 4
        };

        public VertexColors VertexColors;
        public Material MeshMaterial;

        private void Awake()
        {
            var _mesh = new Mesh
            {
                vertices = Vertices,
                triangles = Indices,                
            };
            _mesh.RecalculateNormals();

            if (VertexColors != null && VertexColors.Colors.Length > 0)
            {
                Color[] colors = new Color[_mesh.vertexCount];
                int colorCnts = VertexColors.Colors.Length;
                for (int i=0; i<_mesh.vertexCount; ++i)
                {
                    colors[i] = VertexColors.Colors[i % colorCnts];
                }
                _mesh.SetColors(colors);
            }

            _mesh.SetUVs(0, UVs);
            
            
            
            var ro = gameObject.AddComponent<RenderingObject>();
            ro.mesh = _mesh;
            gameObject.AddComponent<MeshFilter>().mesh = _mesh;
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MeshMaterial;
        }
    }
}