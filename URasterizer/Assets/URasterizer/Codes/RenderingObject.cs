
using UnityEngine;

namespace URasterizer
{
    public class RenderingObject: MonoBehaviour
    {        
        public Mesh mesh;
        public bool DoubleSideRendering;
        public Color Color0 = Color.red;
        public Color Color1 = Color.green;
        public Color Color2 = Color.blue;

        private void Awake()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if(meshFilter != null)
            {
                mesh = meshFilter.mesh;
            }
        }    

        // TRS
        public Matrix4x4 GetModelMatrix()
        {
            if(transform == null)
            {
                return TransformTool.GetRotZMatrix(0);
            }

            var matScale = TransformTool.GetScaleMatrix(transform.lossyScale);

            var rotation = transform.localRotation.eulerAngles;
            var rotX = TransformTool.GetRotationMatrix(Vector3.right, -rotation.x);
            var rotY = TransformTool.GetRotationMatrix(Vector3.up, -rotation.y);
            var rotZ = TransformTool.GetRotationMatrix(Vector3.forward, rotation.z);
            var matRot = rotY * rotX * rotZ; // rotation apply order: z(roll), x(pitch), y(yaw) 

            var matTranslation = TransformTool.GetTranslationMatrix(transform.position);

            return matTranslation * matRot * matScale;
        }
    }
}
