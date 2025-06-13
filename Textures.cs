using Silk.NET.OpenGL;

namespace SpaceSim
{
    public class Texture
    {
        private readonly GL _gl;
        private readonly uint _handle;

        public Texture(GL gl, uint handle)
        {
            _gl = gl;
            _handle = handle;
        }

        public void Use(uint slot = 0)
        {
            _gl.ActiveTexture(TextureUnit.Texture0 + (int)slot);
            _gl.BindTexture(TextureTarget.Texture2D, _handle);
        }

        public void Dispose()
        {
            _gl.DeleteTexture(_handle);
        }
    }
}