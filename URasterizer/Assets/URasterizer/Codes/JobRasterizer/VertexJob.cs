using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace URasterizer
{
    [BurstCompile]
    public struct VertexShadingJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Vector3> positionData;        
        [ReadOnly]
        public NativeArray<Vector3> normalData;        

        public Matrix4x4 mvpMat;
        public Matrix4x4 modelMat;
        public Matrix4x4 normalMat;
        
        public NativeArray<VSOutBuf> result;

        public void Execute(int index)
        {            
            var vert = positionData[index];
            var normal = normalData[index];
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