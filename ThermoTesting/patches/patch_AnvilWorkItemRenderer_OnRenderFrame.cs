using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using OpenTK.Graphics.OpenGL;

namespace ThermoTesting.patches
{
    [HarmonyPatch(typeof(Vintagestory.GameContent.AnvilWorkItemRenderer), "OnRenderFrame")]
    public class PatchAnvilWorkItemRendererOnRenderFrame
    {
        static readonly MethodInfo PrivateHelper = AccessTools.Method(typeof(AnvilWorkItemRenderer), "RenderRecipeOutLine", new Type[] { });
        
        public static bool Prefix(float deltaTime, EnumRenderStage stage, AnvilWorkItemRenderer __instance)
        {
            var field_workItemMeshRef = AccessTools.Field(typeof(AnvilWorkItemRenderer), "workItemMeshRef");
            var field_api = AccessTools.Field(typeof(AnvilWorkItemRenderer), "api");
            var field_ingot = AccessTools.Field(typeof(AnvilWorkItemRenderer), "ingot");
            var field_pos = AccessTools.Field(typeof(AnvilWorkItemRenderer), "pos");
            var field_glowRgb = AccessTools.Field(typeof(AnvilWorkItemRenderer), "glowRgb");
            var field_texId = AccessTools.Field(typeof(AnvilWorkItemRenderer), "texId");
            var field_coreMod = AccessTools.Field(typeof(AnvilWorkItemRenderer), "coreMod");
            var field_ModelMat = AccessTools.Field(typeof(AnvilWorkItemRenderer), "ModelMat");

            //var method_RenderRecipeOutLine = PrivateHelper.Invoke(__instance, new object[] { });

            MeshRef _workItemMeshRef = (MeshRef)field_workItemMeshRef.GetValue(__instance);
            ICoreClientAPI _api = (ICoreClientAPI)field_api.GetValue(__instance);
            ItemStack _ingot = (ItemStack)field_ingot.GetValue(__instance);
            BlockPos _pos = (BlockPos)field_pos.GetValue(__instance);
            Vec4f _glowRgb = (Vec4f)field_glowRgb.GetValue(__instance);
            int _texId = (int)field_texId.GetValue(__instance);
            SurvivalCoreSystem _coreMod = (SurvivalCoreSystem)field_coreMod.GetValue(__instance);
            Matrixf _ModelMat = (Matrixf)field_ModelMat.GetValue(__instance);

            if (_workItemMeshRef == null)
            {
                return false;
            }
            if (stage == EnumRenderStage.AfterFinalComposition)
            {
                IClientPlayer player = _api.World.Player;
                object obj;
                if (player == null)
                {
                    obj = null;
                }
                else
                {
                    IPlayerInventoryManager inventoryManager = player.InventoryManager;
                    if (inventoryManager == null)
                    {
                        obj = null;
                    }
                    else
                    {
                        ItemSlot activeHotbarSlot = inventoryManager.ActiveHotbarSlot;
                        if (activeHotbarSlot == null)
                        {
                            obj = null;
                        }
                        else
                        {
                            ItemStack itemstack = activeHotbarSlot.Itemstack;
                            obj = ((itemstack != null) ? itemstack.Collectible : null);
                        }
                    }
                }
                if (obj is ItemHammer)
                {
                    //__instance.RenderRecipeOutLine();
                    PrivateHelper.Invoke(__instance, new object[] { });

                }
                return false;
            }
            IRenderAPI rpi = _api.Render;
            IClientWorldAccessor worldAccess = _api.World;
            Vec3d camPos = worldAccess.Player.Entity.CameraPos;
            Vec4f lightrgbs = worldAccess.BlockAccessor.GetLightRGBs(_pos.X, _pos.Y, _pos.Z);

            int num = (int)_ingot.Collectible.GetTemperature(_api.World, _ingot);        
            
            int extraGlow = GameMath.Clamp((num - 550) / 2, 0, 255);
            float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(num);
            _glowRgb.R = glowColor[0];
            _glowRgb.G = glowColor[1];
            _glowRgb.B = glowColor[2];
            _glowRgb.A = (float)extraGlow / 255f;
            // need to move the glowRGB calculations to the GPU using the passed temps now. 




            rpi.GlDisableCullFace();
            //IShaderProgram anvilShaderProg = _coreMod.anvilShaderProg;
            IShaderProgram anvilShaderProg = _api.Shader.GetProgramByName("anvilworkitempatch");
            anvilShaderProg.Use();
            rpi.BindTexture2d(_texId);

            //anvilShaderProg.BindTexture2D("uTempTex", 0, 1);
            //anvilShaderProg.Uniform("uTempTex", 1);
            int prevActive;
            GL.GetInteger(GetPName.ActiveTexture, out prevActive);


            int prevTex;
            GL.GetInteger(GetPName.TextureBinding2D, out prevTex);


            int prevAlign;
            GL.GetInteger(GetPName.UnpackAlignment, out prevAlign);

            ExternalData.ThermoData? thermoData = null;
            if (((thermoData = ExternalData.TryGetTemperatureData(_ingot)) != null) && true)
            {
                if(thermoData.isMeshDirty)
                {
                    int width = thermoData.flatTemps.Length;
                    if (!(thermoData.tempTexId != 0 && thermoData.tempTexWidth == width))
                    {
                        
                        if (thermoData.tempTexId != 0) GL.DeleteTexture(thermoData.tempTexId);
                        thermoData.tempTexId = GL.GenTexture();
                        thermoData.tempTexWidth = width;
                        GL.ActiveTexture(TextureUnit.Texture0);
                        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);


                        GL.BindTexture(TextureTarget.Texture2D, thermoData.tempTexId);

                        // bind ONLY inside this function
                        GL.BindTexture(TextureTarget.Texture2D, thermoData.tempTexId);
                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f,
                        width, 1, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);


                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);


                        GL.BindTexture(TextureTarget.Texture2D, 0); // unbind = “quiet”
                        thermoData.isMeshDirty = false;
                    }
                }
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);


                GL.BindTexture(TextureTarget.Texture2D, thermoData.tempTexId);
                GL.BindTexture(TextureTarget.Texture2D, thermoData.tempTexId);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, thermoData.tempTexWidth, 1,
                PixelFormat.Red, PixelType.Float, thermoData.flatTemps);
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
            GL.BindTexture(TextureTarget.Texture2D, prevTex);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, prevAlign);
            GL.ActiveTexture((TextureUnit)prevActive);



            anvilShaderProg.Uniform("rgbaAmbientIn", rpi.AmbientColor);
            anvilShaderProg.Uniform("rgbaFogIn", rpi.FogColor);
            anvilShaderProg.Uniform("fogMinIn", rpi.FogMin);
            anvilShaderProg.Uniform("dontWarpVertices", 0);
            anvilShaderProg.Uniform("addRenderFlags", 0);
            anvilShaderProg.Uniform("fogDensityIn", rpi.FogDensity);
            anvilShaderProg.Uniform("rgbaTint", ColorUtil.WhiteArgbVec);
            anvilShaderProg.Uniform("rgbaLightIn", lightrgbs);
                       
            //anvilShaderProg.Uniform("rgbaGlowIn", _glowRgb);
            //anvilShaderProg.Uniform("extraGlow", extraGlow);

            if(thermoData != null)
            {
                anvilShaderProg.BindTexture2D("uTempTex", thermoData.tempTexId, 1);
                anvilShaderProg.Uniform("uTempTex", 1);
                anvilShaderProg.Uniform("uTempTexWidth", thermoData.tempTexWidth);
            }            

            anvilShaderProg.UniformMatrix("modelMatrix", _ModelMat.Identity().Translate((double)_pos.X - camPos.X, (double)_pos.Y - camPos.Y, (double)_pos.Z - camPos.Z).Values);
            anvilShaderProg.UniformMatrix("viewMatrix", rpi.CameraMatrixOriginf);
            anvilShaderProg.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
            rpi.RenderMesh(_workItemMeshRef);
            anvilShaderProg.UniformMatrix("modelMatrix", rpi.CurrentModelviewMatrix);
            anvilShaderProg.Stop();

            return false;
        }
    }
}