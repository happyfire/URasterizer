
using UnityEngine;

namespace URasterizer
{
    public class RenderingObject: MonoBehaviour
    {        
        public Mesh mesh;
        public bool DoubleSideRendering;
        public Texture2D texture;

        //缓存避免在draw loop中从mesh copy
        [HideInInspector, System.NonSerialized]
        public Vector3[] MeshVertices;
        [HideInInspector, System.NonSerialized]
        public Vector3[] MeshNormals;
        [HideInInspector, System.NonSerialized]
        public int[] MeshTriangles;
        [HideInInspector, System.NonSerialized]
        public Vector2[] MeshUVs;
        [HideInInspector, System.NonSerialized]
        public VSOutBuf[] vsOutputBuffer;

        public ComputeBuffer VertexBuffer;
        public ComputeBuffer NormalBuffer;
        public ComputeBuffer OutBuffer;


        private void Start()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if(meshFilter != null)
            {
                mesh = meshFilter.mesh;                  
                vsOutputBuffer = new VSOutBuf[mesh.vertexCount];            
            }
            var meshRenderer = GetComponent<MeshRenderer>();             
            if (meshRenderer != null && meshRenderer.sharedMaterial!=null)
            {
                texture = meshRenderer.sharedMaterial.mainTexture as Texture2D;
            }

            MeshVertices = mesh.vertices;
            MeshNormals = mesh.normals;
            MeshTriangles = mesh.triangles;
            MeshUVs = mesh.uv;

            //为了能在运行时动态切换是否使用 GPU Driven Rasterizer, 这里同时把GPU使用的Compute Buffer创建出来
            int vertexCnt = mesh.vertexCount;
            VertexBuffer = new ComputeBuffer(vertexCnt, 3*sizeof(float));
            VertexBuffer.name = "VertexBuffer";
            VertexBuffer.SetData(MeshVertices);
            
            NormalBuffer = new ComputeBuffer(vertexCnt, 3*sizeof(float));
            NormalBuffer.name = "NormalBuffer";
            NormalBuffer.SetData(MeshNormals);
            
            OutBuffer = new ComputeBuffer(vertexCnt, 13*sizeof(float));
            OutBuffer.name = "OutBuffer";
        }

        void OnDestroy()
        {
            VertexBuffer.Release();
            NormalBuffer.Release();
            OutBuffer.Release();
        }



        // TRS
        public Matrix4x4 GetModelMatrix()
        {
            if(transform == null)
            {
                return TransformTool.GetRotZMatrix(0);
            }

            var matScale = TransformTool.GetScaleMatrix(transform.lossyScale);

            var rotation = transform.rotation.eulerAngles;
            var rotX = TransformTool.GetRotationMatrix(Vector3.right, -rotation.x);
            var rotY = TransformTool.GetRotationMatrix(Vector3.up, -rotation.y);
            var rotZ = TransformTool.GetRotationMatrix(Vector3.forward, rotation.z);
            var matRot = rotY * rotX * rotZ; // rotation apply order: z(roll), x(pitch), y(yaw) 

            var matTranslation = TransformTool.GetTranslationMatrix(transform.position);

            return matTranslation * matRot * matScale;
        }
    }
}
