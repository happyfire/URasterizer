
using UnityEngine;

namespace URasterizer
{
    public class RenderingObject
    {
        public Transform transform;
        public Mesh mesh;


        public RenderingObject(Transform node)
        {
            transform = node;
            mesh = node.GetComponent<MeshFilter>().mesh;
        }

        public RenderingObject(Mesh mesh)
        {
            this.mesh = mesh;            
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
