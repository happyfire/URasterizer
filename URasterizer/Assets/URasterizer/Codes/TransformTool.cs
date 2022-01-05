using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace URasterizer
{
    //计算变换矩阵
    //约定如下：
    //1. Modle, View使用右手坐标系。Projection使用OpenGL约定(左手坐标系，NDC范围[-1,1])
    //2. 矩阵使用Column Major
    //3. ViewSpace中, camera看向负Z轴方向
    //4. 参数约定见函数注释

    public class TransformTool
    {


        const float MY_PI = 3.1415926f;
        const float D2R = MY_PI / 180.0f;

        public static Matrix4x4 GetViewMatrix(Vector3 eye_pos)
        {
            Matrix4x4 view = Matrix4x4.identity;
            Matrix4x4 translate = Matrix4x4.identity;
            translate.SetColumn(3, new Vector4(-eye_pos.x, -eye_pos.y, -eye_pos.z, 1f));

            view = translate * view;
            return view;
        }

        public static Matrix4x4 GetModelMatrix(float rotation_angle)
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
        //ViewSpace使用左手坐标系，camera看向-Z轴。
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
        //ViewSpace使用左手坐标系，camera看向-Z轴。
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
        public static Matrix4x4 GetProjectionMatrix(float eye_fov, float aspect_ratio, float zNear, float zFar)
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