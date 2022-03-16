
using UnityEngine;
using Unity.Collections;

namespace URasterizer
{
    public struct RenderInputData
    {
        public Vector3 vertex;
        public Vector3 normal;

        public RenderInputData(Vector3 v, Vector3 n){
            vertex = v;
            normal = n;
        }
    }

    public class JobRenderObjectData : IRenderObjectData
    {
        [ReadOnly]
        public NativeArray<RenderInputData> inputData;        
        

        public JobRenderObjectData(Mesh mesh)
        {            
            inputData = new NativeArray<RenderInputData>(mesh.vertexCount, Allocator.Persistent);            

            var meshVertices = mesh.vertices;
            var meshNormals = mesh.normals;

            for(int i=0; i<mesh.vertexCount; ++i){
                inputData[i] = new RenderInputData(meshVertices[i], meshNormals[i]);
            }                        
        }

        public void Release()
        {
            inputData.Dispose();            
        }

    }
}