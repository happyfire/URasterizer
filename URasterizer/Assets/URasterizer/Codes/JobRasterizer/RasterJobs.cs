using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace URasterizer
{ 
    public struct VertexShadingJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<RenderInputData> inputData;        

        public Matrix4x4 mvpMat;
        public Matrix4x4 modelMat;
        public Matrix4x4 normalMat;
        
        public NativeArray<VSOutBuf> result;

        public void Execute(int index)
        {
            RenderInputData input = inputData[index];
            var vert = input.vertex;
            var normal = input.normal;
            var output = result[index];

            var objVert = new Vector4(vert.x, vert.y, -vert.z, 1);
            output.clipPos = mvpMat * objVert;
            output.worldPos = modelMat * objVert;            
            var objNormal = new Vector3(normal.x, normal.y, -normal.z);
            output.objectNormal = objNormal;
            output.worldNormal = normalMat * objNormal;
            result[index] = output;
        }
    }
}