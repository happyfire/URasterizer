
using UnityEngine;

namespace URasterizer
{
    public class GPURenderObjectData : IRenderObjectData
    {
        public ComputeBuffer VertexBuffer;
        public ComputeBuffer NormalBuffer;
        public ComputeBuffer UVBuffer;
        public ComputeBuffer TriangleBuffer;
        public ComputeBuffer VertexOutBuffer; //for vertex shader output
        
        public ComputeBuffer RenderTriangleBuffer;

        public GPURenderObjectData(Mesh mesh)
        {                        
            int vertexCnt = mesh.vertexCount;
            VertexBuffer = new ComputeBuffer(vertexCnt, 3*sizeof(float));            
            VertexBuffer.SetData(mesh.vertices);
            
            NormalBuffer = new ComputeBuffer(vertexCnt, 3*sizeof(float));            
            NormalBuffer.SetData(mesh.normals);

            UVBuffer = new ComputeBuffer(vertexCnt, 2*sizeof(float));
            UVBuffer.SetData(mesh.uv);

            //初始化三角形数组，每个三角形包含3个索引值
            //注意这儿对调了v0和v1的索引，因为原来的 0,1,2是顺时针的，对调后是 1,0,2是逆时针的
            //Unity Quard模型的两个三角形索引分别是 0,3,1,3,0,2 转换后为 3,0,1,0,3,2
            var mesh_triangles = mesh.triangles;
            int triCnt = mesh_triangles.Length/3;
            Vector3Int[] triangles = new Vector3Int[triCnt];
            for(int i=0; i < triCnt; ++i){
                int j = i * 3;
                triangles[i].x = mesh_triangles[j+1];
                triangles[i].y = mesh_triangles[j];
                triangles[i].z = mesh_triangles[j+2];
            }

            TriangleBuffer = new ComputeBuffer(triangles.Length, 3*sizeof(uint));
            TriangleBuffer.SetData(triangles);
            
            VertexOutBuffer = new ComputeBuffer(vertexCnt, 15*sizeof(float)); 
        }

        public void Release()
        {
            VertexBuffer.Release();
            VertexBuffer = null;
            NormalBuffer.Release();
            NormalBuffer = null;
            UVBuffer.Release();
            UVBuffer = null;
            TriangleBuffer.Release();
            TriangleBuffer = null;
            VertexOutBuffer.Release();
            VertexOutBuffer = null;   
        }

    }
}