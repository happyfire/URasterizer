
using UnityEngine;

namespace URasterizer
{
    public class CPURenderObjectData : IRenderObjectData
    {
        
        //缓存避免在draw loop中从mesh copy        
        public Vector3[] MeshVertices;        
        public Vector3[] MeshNormals;        
        public int[] MeshTriangles;        
        public Vector2[] MeshUVs;        
        public VSOutBuf[] vsOutputBuffer;

        public CPURenderObjectData(Mesh mesh)
        {
            //CPU Rasterizer使用的模型数据缓存
            MeshVertices = mesh.vertices;
            MeshNormals = mesh.normals;
            MeshTriangles = mesh.triangles;
            MeshUVs = mesh.uv;

            vsOutputBuffer = new VSOutBuf[mesh.vertexCount];
        }

        public void Release()
        {

        }

    }
}