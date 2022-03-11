using UnityEngine;

namespace URasterizer
{
    public interface IRasterizer
    {
        string Name { get; }
        void Clear(BufferMask mask);
        void Draw(RenderingObject ro, Camera camera);

        Texture ColorTexture { get; }

        void UpdateFrame();

        FragmentShader CurrentFragmentShader { get; set; }
    }
}