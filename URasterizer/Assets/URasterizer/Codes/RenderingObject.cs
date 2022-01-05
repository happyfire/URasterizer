
using UnityEngine;

namespace URasterizer
{
    public class RenderingObject
    {
        public Transform transform;
        public Mesh mesh;


        public RenderingObject(Transform node)
        {
            transform = node;
            mesh = node.GetComponent<MeshFilter>().mesh;
        }

        public RenderingObject(Mesh mesh)
        {
            this.mesh = mesh;
        }
    }
}
