using UnityEngine;

namespace URasterizer
{
    public interface IRasterizer
    {
        void Clear(BufferMask mask);
        void Draw(RenderingObject ro, Camera camera);

        void UpdateFrame();

        FragmentShader CurrentFragmentShader { get; set; }
    }
}