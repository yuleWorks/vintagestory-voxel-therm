using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace ThermoTesting.patches
{
    //[HarmonyPatch(typeof(Vintagestory.Client.NoObf.InventoryItemRenderer), "RenderItemStackToGui")]
    public class PatchInventoryItemRendererRenderItemStackToGui
    {
        public static bool Prefix(ItemSlot inSlot, double posX, double posY, double posZ, float size, int color, float dt, InventoryItemRenderer __instance, bool shading = true, bool origRotate = false, bool showStackSize = true)
        {
            var field_clearPixels = AccessTools.Field(typeof(InventoryItemRenderer), "clearPixels");
            var field_game = AccessTools.Field(typeof(InventoryItemRenderer), "game");
            ClientMain _game = (ClientMain)field_game.GetValue(__instance);
            var field_api = AccessTools.Field(_game.GetType(), "api");
            ClientCoreAPI _api = (ClientCoreAPI)field_api.GetValue(__instance);
            var field_eventApi = AccessTools.Field(_api.GetType(), "eventapi");
            ClientEventAPI _eventapi = (ClientEventAPI)field_eventApi.GetValue(__instance);
            var field_renderapi = AccessTools.Field(_api.GetType(), "renderapi");
            RenderAPIGame _renderapi = (RenderAPIGame)field_renderapi.GetValue(__instance);


            var field_modelMat = AccessTools.Field(typeof(InventoryItemRenderer), "modelMat");
            var field_perAtlasQueues = AccessTools.Field(typeof(InventoryItemRenderer), "perAtlasQueues");
            var field_quadModelRef = AccessTools.Field(typeof(InventoryItemRenderer), "quadModelRef");
            var field_queue = AccessTools.Field(typeof(InventoryItemRenderer), "queue");
            var field_stackSizeFont = AccessTools.Field(typeof(InventoryItemRenderer), "stackSizeFont");
            var field_StackSizeTextures = AccessTools.Field(typeof(InventoryItemRenderer), "StackSizeTextures");
            var field_taskCompletesdQueue = AccessTools.Field(typeof(InventoryItemRenderer), "taskCompletedQueue");

            var field = AccessTools.Field(_eventapi.GetType(), "itemStackRenderersByTarget");
            var arr = (Dictionary<int, ItemRenderDelegate>[][])field.GetValue(_eventapi);


            var method_GenStackSizeTexture = AccessTools.Method(__instance.GetType(), "GenStackSizeTexture", new Type[] { typeof(int), typeof(float) });


            //var field_api = AccessTools.Field(typeof(InventoryItemRenderer), "")

            //var method_RenderRecipeOutLine = PrivateHelper.Invoke(__instance, new object[] { });

            int[] _clearPixels = (int[])field_clearPixels.GetValue(__instance);
            
            Matrixf _modelMat = (Matrixf)field_modelMat.GetValue(__instance);
            Dictionary<int, Queue<AtlasRenderTask>> _perAtlasQueues = (Dictionary<int, Queue<AtlasRenderTask>>)field_perAtlasQueues.GetValue(__instance);
            MeshRef _quadModelRef = (MeshRef)field_quadModelRef.GetValue(__instance);
            Queue<AtlasRenderTask> _queue = (Queue<AtlasRenderTask>)field_queue.GetValue(__instance);
            CairoFont _stackSizeFont = (CairoFont)field_stackSizeFont.GetValue(__instance);
            Dictionary<string, LoadedTexture> _StackSizeTextures = (Dictionary<string, LoadedTexture>)field_StackSizeTextures.GetValue(__instance);
            Queue<AtlasRenderTask> _taskCompletesdQueue = (Queue<AtlasRenderTask>)field_taskCompletesdQueue.GetValue(__instance);


            try
            {
                ItemStack itemstack = inSlot.Itemstack;
                ItemRenderInfo renderInfo = InventoryItemRenderer.GetItemStackRenderInfo(_game, inSlot, EnumItemRenderTarget.Gui, dt);
                if (renderInfo.ModelRef != null)
                {
                    itemstack.Collectible.InGuiIdle(_game, itemstack);
                    ModelTransform transform = renderInfo.Transform;
                    if (transform != null)
                    {
                        bool upsidedown = itemstack.Class == EnumItemClass.Block;
                        bool rotate = origRotate && renderInfo.Transform.Rotate;
                        Matrixf modelMat = _modelMat;
                        modelMat.Identity();
                        modelMat.Translate((float)((int)posX - ((itemstack.Class == EnumItemClass.Item) ? 3 : 0)), (float)((int)posY - ((itemstack.Class == EnumItemClass.Item) ? 1 : 0)), (float)posZ);
                        modelMat.Translate((double)transform.Origin.X + GuiElement.scaled((double)transform.Translation.X), (double)transform.Origin.Y + GuiElement.scaled((double)transform.Translation.Y), (double)(transform.Origin.Z * size) + GuiElement.scaled((double)transform.Translation.Z));
                        modelMat.Scale(size * transform.ScaleXYZ.X, size * transform.ScaleXYZ.Y, size * transform.ScaleXYZ.Z);
                        modelMat.RotateXDeg(transform.Rotation.X + (upsidedown ? 180f : 0f));
                        modelMat.RotateYDeg(transform.Rotation.Y - (float)(upsidedown ? -1 : 1) * (rotate ? ((float)_game.Platform.EllapsedMs / 50f) : 0f));
                        modelMat.RotateZDeg(transform.Rotation.Z);
                        modelMat.Translate(-transform.Origin.X, -transform.Origin.Y, -transform.Origin.Z);
                        int num = (int)itemstack.Collectible.GetTemperature(_game, itemstack);
                        float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(num);
                        float[] drawcolor = ColorUtil.ToRGBAFloats(color);
                        int extraGlow = GameMath.Clamp((num - 550) / 2, 0, 255);
                        bool tempGlowMode = itemstack.Attributes.HasAttribute("temperature");
                        ShaderProgramGui guiShaderProg = _game.guiShaderProg;
                        guiShaderProg.NormalShaded = ((renderInfo.NormalShaded && shading) ? 1 : 0);
                        guiShaderProg.RgbaIn = new Vec4f(drawcolor[0], drawcolor[1], drawcolor[2], drawcolor[3]);
                        guiShaderProg.ExtraGlow = extraGlow;
                        guiShaderProg.TempGlowMode = ((tempGlowMode != false) ? 1 : 0);
                        guiShaderProg.RgbaGlowIn = (tempGlowMode ? new Vec4f(glowColor[0], glowColor[1], glowColor[2], (float)extraGlow / 255f) : new Vec4f(1f, 1f, 1f, (float)extraGlow / 255f));
                        guiShaderProg.ApplyColor = ((renderInfo.ApplyColor != false) ? 1 : 0);
                        guiShaderProg.AlphaTest = renderInfo.AlphaTest;
                        guiShaderProg.OverlayOpacity = renderInfo.OverlayOpacity;
                        if (renderInfo.OverlayTexture != null && renderInfo.OverlayOpacity > 0f)
                        {
                            guiShaderProg.Tex2dOverlay2D = renderInfo.OverlayTexture.TextureId;
                            guiShaderProg.OverlayTextureSize = new Vec2f((float)renderInfo.OverlayTexture.Width, (float)renderInfo.OverlayTexture.Height);
                            guiShaderProg.BaseTextureSize = new Vec2f((float)renderInfo.TextureSize.Width, (float)renderInfo.TextureSize.Height);
                            TextureAtlasPosition texPos = InventoryItemRenderer.GetTextureAtlasPosition(_game, itemstack);
                            guiShaderProg.BaseUvOrigin = new Vec2f(texPos.x1, texPos.y1);
                        }
                        guiShaderProg.ModelMatrix = modelMat.Values;
                        guiShaderProg.ProjectionMatrix = _game.CurrentProjectionMatrix;
                        guiShaderProg.ModelViewMatrix = modelMat.ReverseMul(_game.CurrentModelViewMatrix).Values;
                        guiShaderProg.ApplyModelMat = 1;
                        ItemRenderDelegate renderer;
                        if (arr[(int)itemstack.Collectible.ItemClass][0].TryGetValue(itemstack.Collectible.Id, out renderer))
                        {
                            renderer(inSlot, renderInfo, modelMat, posX, posY, posZ, size, color, origRotate, showStackSize);
                            guiShaderProg.ApplyModelMat = 0;
                            guiShaderProg.NormalShaded = 0;
                            guiShaderProg.RgbaGlowIn = new Vec4f(0f, 0f, 0f, 0f);
                            guiShaderProg.AlphaTest = 0f;
                        }
                        else
                        {
                            guiShaderProg.DamageEffect = renderInfo.DamageEffect;
                            _renderapi.RenderMultiTextureMesh(renderInfo.ModelRef, "tex2d", 0);
                            
                            guiShaderProg.ApplyModelMat = 0;
                            guiShaderProg.NormalShaded = 0;
                            guiShaderProg.TempGlowMode = 0;
                            guiShaderProg.DamageEffect = 0f;
                            LoadedTexture stackSizeTexture = null;
                            if (itemstack.StackSize != 1 && showStackSize)
                            {
                                float mul = size / (float)GuiElement.scaled(25.600000381469727);
                                string key = itemstack.StackSize.ToString() + "-" + ((int)(mul * 100f)).ToString();
                                if (!_StackSizeTextures.TryGetValue(key, out stackSizeTexture))
                                {
                                    stackSizeTexture = (_StackSizeTextures[key] = (LoadedTexture)method_GenStackSizeTexture.Invoke(__instance, new object[] { itemstack.StackSize, mul }));

                                }
                            }
                            if (stackSizeTexture != null)
                            {
                                float mul2 = size / (float)GuiElement.scaled(25.600000381469727);
                                _game.Platform.GlToggleBlend(true, EnumBlendMode.PremultipliedAlpha);
                                _game.Render2DLoadedTexture(stackSizeTexture, (float)((int)(posX + (double)size + 1.0 - (double)stackSizeTexture.Width)), (float)((int)(posY + (double)mul2 * GuiElement.scaled(3.0) - GuiElement.scaled(4.0))), (float)((int)posZ + 100), null);
                                _game.Platform.GlToggleBlend(true, EnumBlendMode.Standard);
                            }
                            guiShaderProg.AlphaTest = 0f;
                            guiShaderProg.RgbaGlowIn = new Vec4f(0f, 0f, 0f, 0f);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error while rendering item in slot " + ((inSlot != null) ? inSlot.ToString() : null), e);
            }


            return false;
        }
    }
}
