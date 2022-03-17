
using UnityEngine;
using Unity.Collections;

namespace URasterizer
{
    public class JobRenderObjectData : IRenderObjectData
    {        
        public NativeArray<Vector3> positionData;        
        public NativeArray<Vector3> normalData;        
        public NativeArray<Vector2> uvData;
        public NativeArray<Vector3Int> trianglesData;             
        

        public JobRenderObjectData(Mesh mesh)
        {            
            positionData = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Persistent);            
            positionData.CopyFrom(mesh.vertices);

            normalData = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Persistent);   
            normalData.CopyFrom(mesh.normals);         
            
            uvData = new NativeArray<Vector2>(mesh.vertexCount, Allocator.Persistent);
            uvData.CopyFrom(mesh.uv);

            //初始化三角形数组，每个三角形包含3个索引值
            //注意这儿对调了v0和v1的索引，因为原来的 0,1,2是顺时针的，对调后是 1,0,2是逆时针的
            //Unity Quard模型的两个三角形索引分别是 0,3,1,3,0,2 转换后为 3,0,1,0,3,2
            var mesh_triangles = mesh.triangles;
            int triCnt = mesh_triangles.Length/3;
            trianglesData = new NativeArray<Vector3Int>(triCnt, Allocator.Persistent);
            for(int i=0; i < triCnt; ++i){
                int j = i * 3;
                trianglesData[i] = new Vector3Int(mesh_triangles[j+1], mesh_triangles[j], mesh_triangles[j+2]);
            }                      
        }

        public void Release()
        {
            positionData.Dispose();  
            normalData.Dispose();
            uvData.Dispose();
            trianglesData.Dispose();          
        }

    }
}