using UnityEngine;

namespace URasterizer
{
    public enum RasterizerType
    {
        CPU,
        CPUJobs,
        GPUDriven
    }
    
    public enum BufferMask
    {
        Color = 1,
        Depth = 2
    }   

    public struct VSOutBuf
    {
        public Vector4 clipPos; //clip space vertices
        public Vector3 worldPos; //world space vertices
        public Vector3 objectNormal; //obj space normals
        public Vector3 worldNormal; //world space normals
    } 

    public delegate void OnRasterizerStatUpdate(int verticesAll, int trianglesAll, int trianglesRendered);
}
