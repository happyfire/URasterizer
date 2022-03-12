using UnityEngine;

namespace URasterizer
{
    public interface IRasterizer
    {
        string Name { get; }
        void Clear(BufferMask mask);

        void SetupUniforms(Camera camera, Light mainLight);

        void DrawObject(RenderingObject ro);

        Texture ColorTexture { get; }

        void UpdateFrame();        
    }
}