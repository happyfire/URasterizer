
using UnityEngine;

namespace URasterizer
{
    public class RenderingObject: MonoBehaviour
    {        
        public Mesh mesh;
        public bool DoubleSideRendering;
        [HideInInspector, System.NonSerialized]
        public Texture2D texture;

#region CPU Rasterizer Use
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
#endregion        

#region  GPU Rasterizer Use
        public ComputeBuffer VertexBuffer;
        public ComputeBuffer NormalBuffer;
        public ComputeBuffer UVBuffer;
        public ComputeBuffer TriangleBuffer;
        public ComputeBuffer VertexOutBuffer; //for vertex shader output
        
        public ComputeBuffer RenderTriangleBuffer;
#endregion

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
            if(texture==null){
                texture = Texture2D.whiteTexture;
            }

            //CPU Rasterizer使用的模型数据缓存
            MeshVertices = mesh.vertices;
            MeshNormals = mesh.normals;
            MeshTriangles = mesh.triangles;
            MeshUVs = mesh.uv;

            //为了能在运行时动态切换是否使用 GPU Driven Rasterizer, 这里同时把GPU使用的Compute Buffer创建出来
            int vertexCnt = mesh.vertexCount;
            VertexBuffer = new ComputeBuffer(vertexCnt, 3*sizeof(float));            
            VertexBuffer.SetData(MeshVertices);
            
            NormalBuffer = new ComputeBuffer(vertexCnt, 3*sizeof(float));            
            NormalBuffer.SetData(MeshNormals);

            UVBuffer = new ComputeBuffer(vertexCnt, 2*sizeof(float));
            UVBuffer.SetData(MeshUVs);

            //初始化三角形数组，每个三角形包含3个索引值
            //注意这儿对调了v0和v1的索引，因为原来的 0,1,2是顺时针的，对调后是 1,0,2是逆时针的
            //Unity Quard模型的两个三角形索引分别是 0,3,1,3,0,2 转换后为 3,0,1,0,3,2
            int triCnt = MeshTriangles.Length/3;
            Vector3Int[] triangles = new Vector3Int[triCnt];
            for(int i=0; i < triCnt; ++i){
                int j = i * 3;
                triangles[i].x = MeshTriangles[j+1];
                triangles[i].y = MeshTriangles[j];
                triangles[i].z = MeshTriangles[j+2];
            }

            TriangleBuffer = new ComputeBuffer(triangles.Length, 3*sizeof(uint));
            TriangleBuffer.SetData(triangles);
            
            VertexOutBuffer = new ComputeBuffer(vertexCnt, 15*sizeof(float));                        
        }

        void OnDestroy()
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
