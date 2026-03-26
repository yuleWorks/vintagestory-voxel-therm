using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace ThermoTesting
{
    public class TemperatureTexture
    {
        int tempTexId = 0;
        int tempTexWidth = 0;
        //static bool tempDirty = true;

        public void EnsureTempTex(int width)
        {
            if (tempTexId != 0 && tempTexWidth == width) return;


            if (tempTexId != 0) GL.DeleteTexture(tempTexId);


            tempTexId = GL.GenTexture();
            tempTexWidth = width;


            // bind ONLY inside this function
            GL.BindTexture(TextureTarget.Texture2D, tempTexId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f,
            width, 1, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);


            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);


            GL.BindTexture(TextureTarget.Texture2D, 0); // unbind = “quiet”
        }
        public void UploadTempTex(float[] temps)
        {
            EnsureTempTex(temps.Length);


            GL.BindTexture(TextureTarget.Texture2D, tempTexId);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, tempTexWidth, 1,
            PixelFormat.Red, PixelType.Float, temps);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
}
