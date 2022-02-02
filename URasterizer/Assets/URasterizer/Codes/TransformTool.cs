using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace URasterizer
{
    //计算变换矩阵
    //约定如下：
    //1. Modle, View使用右手坐标系。NDC也是右手坐标系(GAMES101规范），NDC范围[-1,1]，注意：f为-1，n为+1
    //2. 矩阵使用Column Major
    //3. ViewSpace中, camera看向负Z轴方向
    //4. 参数约定见函数注释

    public class TransformTool
    {
        const float MY_PI = 3.1415926f;
        const float D2R = MY_PI / 180.0f;

        public static Matrix4x4 GetViewMatrix(Vector3 eye_pos, Vector3 lookAtDir, Vector3 upDir)
        {
            //这儿lookAtDir取反是因为我们使用的view space默认camrea看向(0,0,-1)，因此lookAt会被对应到(0,0,-1)
            //那么-lookAt就对应到(0,0,1)
            //我们构造的旋转矩阵是将(0,0,1)变换到-lookAt，其逆矩阵就是将-lookAt变换到(0,0,1)
            Vector3 camZ = -lookAtDir.normalized;
            Vector3 camY = upDir.normalized;
            Vector3 camX = Vector3.Cross(camY, camZ);
            camY = Vector3.Cross(camZ, camX);
            Matrix4x4 matRot = Matrix4x4.identity;
            matRot.SetColumn(0, camX);
            matRot.SetColumn(1, camY);
            matRot.SetColumn(2, camZ);
                        
            Matrix4x4 translate = Matrix4x4.identity;
            translate.SetColumn(3, new Vector4(-eye_pos.x, -eye_pos.y, -eye_pos.z, 1f));

            Matrix4x4 view = matRot.transpose * translate;
            return view;
        }

        //Get the result of rotate a vector around axis with angle radius.
        //axis must be normalized.
        public static Vector3 RotateVector(Vector3 axis, Vector3 v, float radius)
        {            
            Vector3 v_parallel = Vector3.Dot(axis, v) * axis;
            Vector3 v_vertical = v - v_parallel;
            float v_vertical_len = v_vertical.magnitude;

            Vector3 a = axis;
            Vector3 b = v_vertical.normalized;
            Vector3 c = Vector3.Cross(a, b);
            
            Vector3 v_vertical_rot = v_vertical_len * (Mathf.Cos(radius) * b + Mathf.Sin(radius) * c);
            return v_parallel + v_vertical_rot;
        }

        public static Matrix4x4 GetRotationMatrix(Vector3 axis, float angle)
        {
            Vector3 vx = new Vector3(1, 0, 0);
            Vector3 vy = new Vector3(0, 1, 0);
            Vector3 vz = new Vector3(0, 0, 1);            

            axis.Normalize();
            float radius = angle * D2R;

            var tx = RotateVector(axis, vx, radius);
            var ty = RotateVector(axis, vy, radius);
            var tz = RotateVector(axis, vz, radius);

            Matrix4x4 rotMat = Matrix4x4.identity;
            rotMat.SetColumn(0, tx);
            rotMat.SetColumn(1, ty);
            rotMat.SetColumn(2, tz);
            return rotMat;
        }

        public static Matrix4x4 GetTranslationMatrix(Vector3 translate)
        {
            Matrix4x4 translationMat = Matrix4x4.identity;
            translationMat.SetColumn(3, new Vector4(translate.x, translate.y, -translate.z, 1));
            return translationMat;
        }

        public static Matrix4x4 GetScaleMatrix(Vector3 scale)
        {
            Matrix4x4 scaleMat = Matrix4x4.identity;
            scaleMat[0, 0] = scale.x;
            scaleMat[1, 1] = scale.y;
            scaleMat[2, 2] = scale.z;
            return scaleMat;
        }

        public static Matrix4x4 GetRotZMatrix(float rotation_angle)
        {
            Matrix4x4 model = Matrix4x4.identity;

            float cs = Mathf.Cos(rotation_angle * D2R);
            float si = Mathf.Sin(rotation_angle * D2R);

            model.m00 = cs;
            model.m01 = -si;
            model.m10 = si;
            model.m11 = cs;
            return model;
        }

        //根据ViewSpace下的frustum剪裁面坐标计算正交投影矩阵。
        //ViewSpace使用右手坐标系，camera看向-Z轴。
        //所有参数都是坐标值。因此f和n都是负数，且 f < n
        public static Matrix4x4 GetOrthographicProjectionMatrix(float l, float r, float b, float t, float f, float n)
        {
            Matrix4x4 translate = Matrix4x4.identity;
            translate.SetColumn(3, new Vector4(-(r + l) * 0.5f, -(t + b) * 0.5f, -(n + f) * 0.5f, 1f));
            Matrix4x4 scale = Matrix4x4.identity;
            scale.m00 = 2f / (r - l);
            scale.m11 = 2f / (t - b);
            scale.m22 = 2f / (n - f);
            return scale * translate;
        }

        //根据ViewSpace下的frustum剪裁面坐标计算透视投影矩阵。
        //ViewSpace使用右手坐标系，camera看向-Z轴。
        //所有参数都是坐标值。因此f和n都是负数，且 f < n        
        public static Matrix4x4 GetPerspectiveProjectionMatrix(float l, float r, float b, float t, float f, float n)
        {
            Matrix4x4 perspToOrtho = Matrix4x4.identity;
            perspToOrtho.m00 = n;
            perspToOrtho.m11 = n;
            perspToOrtho.m22 = n + f;
            perspToOrtho.m23 = -n * f;
            perspToOrtho.m32 = 1;
            perspToOrtho.m33 = 0;
            var orthoProj = GetOrthographicProjectionMatrix(l, r, b, t, f, n);
            return orthoProj * perspToOrtho;
        }

        //根据FOV等参数计算透视投影矩阵。fov为fov y, aspect_ratio为宽/高，zNear,zFar为距离值（正数）
        public static Matrix4x4 GetPerspectiveProjectionMatrix(float eye_fov, float aspect_ratio, float zNear, float zFar)
        {
            float t = zNear * Mathf.Tan(eye_fov * D2R * 0.5f);
            float b = -t;
            float r = t * aspect_ratio;
            float l = -r;
            float n = -zNear;
            float f = -zFar;
            return GetPerspectiveProjectionMatrix(l, r, b, t, f, n);
        }
        
    }
}