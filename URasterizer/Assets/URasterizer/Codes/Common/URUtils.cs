using UnityEngine;
using System;

namespace URasterizer
{
    public class URUtils
    {
        static Vector4[] s_tmp8Vector4s = new Vector4[8];

        public static void FillArray<T>(T[] arr, T value)
        {
            int length = arr.Length;            
            if(length == 0){
                return;
            }
            arr[0] = value;

            int arrayHalfLen = length/2;
            int copyLength;
            for(copyLength = 1; copyLength <= arrayHalfLen; copyLength<<=1){
                Array.Copy(arr, 0, arr, copyLength, copyLength);
            }
            Array.Copy(arr, 0, arr, copyLength, length-copyLength);            
        }

        //在ClipSpace中，判断OBB顶点是否在视锥外部。输入v为clip space下OBB的8个顶点
        //该函数使用的是保守视锥剔除算法，对于每个裁剪面都检查OBB的所有顶点是否在它外面，如果都在则剔除
        //该算法是保守的，不会错误剔除掉不该剔除的OBB，但是可能有一些特殊位置的OBB剔除不掉，
        //比如当OBB比较大且在视锥外，但是同时和左右近3个面相交，这样这3个面就无法成功判断剔除。
        //这种情况一般是离镜头很近的大的墙体，可能会剔除不掉，造成性能问题。
        //可参考：https://www.iquilezles.org/www/articles/frustumcorrect/frustumcorrect.htm        
        public static bool CheckVerticesOutFrustumClipSpace(Vector4[] v)
        {           
            //left
            int cnt = 0;
            for(int i=0; i < 8; ++i){                
                var w = v[i].w >=0 ? v[i].w : -v[i].w;
                if(v[i].x < -w){
                    ++cnt;
                }
                if(cnt==8){
                    return true;
                }
            }            
            //right
            cnt = 0;
            for(int i=0; i < 8; ++i){
                var w = v[i].w >=0 ? v[i].w : -v[i].w;
                if(v[i].x > w){
                    ++cnt;
                }
                if(cnt==8){
                    return true;
                }
            }    
            //bottom
            cnt = 0;
            for(int i=0; i < 8; ++i){
                var w = v[i].w >=0 ? v[i].w : -v[i].w;
                if(v[i].y < -w){
                    ++cnt;
                }
                if(cnt==8){
                    return true;
                }
            }    
            //top
            cnt = 0;
            for(int i=0; i < 8; ++i){                
                var w = v[i].w >=0 ? v[i].w : -v[i].w;
                if(v[i].y > w){
                    ++cnt;
                }
                if(cnt==8){
                    return true;
                }
            }    
            //near
            cnt = 0;
            for(int i=0; i < 8; ++i){
                var w = v[i].w >=0 ? v[i].w : -v[i].w;
                if(v[i].z < -w){
                    ++cnt;
                }
                if(cnt==8){
                    return true;
                }
            }    
            //far
            cnt = 0;
            for(int i=0; i < 8; ++i){
                var w = v[i].w >=0 ? v[i].w : -v[i].w;
                if(v[i].z > w){
                    ++cnt;
                }
                if(cnt==8){
                    return true;
                }
            }    
            return false;                       
        }

        public static bool FrustumCulling(Bounds localAABB, Matrix4x4 mvp)
        {
            var v = s_tmp8Vector4s;
            var min = localAABB.min; min.z = -min.z;
            var max = localAABB.max; max.z = -max.z;
            v[0] = mvp * new Vector4(min.x, min.y, min.z, 1.0f);
            v[1] = mvp * new Vector4(min.x, min.y, max.z, 1.0f);
            v[2] = mvp * new Vector4(min.x, max.y, min.z, 1.0f);
            v[3] = mvp * new Vector4(min.x, max.y, max.z, 1.0f);
            v[4] = mvp * new Vector4(max.x, min.y, min.z, 1.0f);
            v[5] = mvp * new Vector4(max.x, min.y, max.z, 1.0f);
            v[6] = mvp * new Vector4(max.x, max.y, min.z, 1.0f);
            v[7] = mvp * new Vector4(max.x, max.y, max.z, 1.0f);
            
            return CheckVerticesOutFrustumClipSpace(v);
        }
    }
}
