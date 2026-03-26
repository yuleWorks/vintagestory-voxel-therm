using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static Vintagestory.GameContent.BlockEntityMicroBlock;

namespace ThermoTesting.patches
{
    [HarmonyPatch(typeof(Vintagestory.GameContent.ItemWorkItem), "GenMesh")]
    public class PatchItemWorkItemGenMesh
    {
        public static bool Prefix(ICoreClientAPI capi, ItemStack workitemStack, byte[,,] voxels, out int textureId, ref MeshData __result)
        {
            bool CUSTOM_MESH_DATA_ACTIVE = true;

            byte[] materialsBytes = workitemStack.Attributes.GetBytes("addedMats");
            ExternalData.ThermoData? thermoInfo = ExternalData.TryGetTemperatureData(workitemStack);
            bool hasThermoInfo = false;
            if (thermoInfo != null) { hasThermoInfo = true; }
            float[,,] voxelTemps = null;

            if (hasThermoInfo)
            {
                voxelTemps = thermoInfo.voxelTemperatures;
            }


            textureId = 0;
            if (workitemStack == null)
            {
                __result = null;
                return false;
            }
            MeshData workItemMesh = new MeshData(24, 36, false, true, true, true);
            workItemMesh.CustomBytes = new CustomMeshDataPartByte
            {
                Conversion = DataConversion.NormalizedFloat,
                Count = workItemMesh.VerticesCount,
                InterleaveSizes = new int[]
                {
                    1
                },
                Instanced = false,
                InterleaveOffsets = new int[1],
                InterleaveStride = 1,
                Values = new byte[workItemMesh.VerticesCount]
            };

            if (hasThermoInfo && CUSTOM_MESH_DATA_ACTIVE) ///////////////////////////
            {
                workItemMesh.CustomInts = new CustomMeshDataPartInt
                {
                    Count = workItemMesh.VerticesCount,
                    InterleaveSizes = new int[]
                    {
                        1
                    },
                    Instanced = false,
                    InterleaveOffsets = new int[1],
                    InterleaveStride = 4,
                    Values = new int[workItemMesh.VerticesCount]
                };
            }



            List<TextureAtlasPosition> tposAddedMatList = new List<TextureAtlasPosition>();
            TextureAtlasPosition tposSlag;
            TextureAtlasPosition tposMetal;
            if (workitemStack.Collectible.FirstCodePart(0) == "ironbloom")
            {
                tposSlag = capi.BlockTextureAtlas.GetPosition(capi.World.GetBlock(new AssetLocation("anvil-copper")), "ironbloom", false);
                tposMetal = capi.BlockTextureAtlas.GetPosition(capi.World.GetBlock(new AssetLocation("ingotpile")), "iron", false);
            }
            else
            {
                tposMetal = capi.BlockTextureAtlas.GetPosition(capi.World.GetBlock(new AssetLocation("ingotpile")), workitemStack.Collectible.Variant["metal"], false);
                tposSlag = tposMetal;
            }

            if (materialsBytes != null)
            {
                for (int i = 0; i < materialsBytes.Length; i++)
                {
                    if (materialsBytes[i] == (byte)VoxelMaterials.Slag)
                    {
                        tposAddedMatList.Add(capi.BlockTextureAtlas.GetPosition(capi.World.GetBlock(new AssetLocation("anvil-copper")), "ironbloom", false));
                    }
                    else
                    {
                        tposAddedMatList.Add(capi.BlockTextureAtlas.GetPosition(capi.World.GetBlock(new AssetLocation("ingotpile")), Enum.GetName(typeof(VoxelMaterials), (VoxelMaterials)materialsBytes[i]).ToLower(), false));
                    }
                }
            }


            MeshData metalVoxelMesh = CubeMeshUtil.GetCubeOnlyScaleXyz(0.03125f, 0.03125f, new Vec3f(0.03125f, 0.03125f, 0.03125f));
            CubeMeshUtil.SetXyzFacesAndPacketNormals(metalVoxelMesh);
            metalVoxelMesh.CustomBytes = new CustomMeshDataPartByte
            {
                Conversion = DataConversion.NormalizedFloat,
                Count = metalVoxelMesh.VerticesCount,
                Values = new byte[metalVoxelMesh.VerticesCount]
            };
            if (hasThermoInfo && CUSTOM_MESH_DATA_ACTIVE) ///////////////////////////
            {
                metalVoxelMesh.CustomInts = new CustomMeshDataPartInt
                {
                    Count = metalVoxelMesh.VerticesCount,
                    Values = new int[metalVoxelMesh.VerticesCount]
                };
            }


            textureId = tposMetal.atlasTextureId;
            for (int i = 0; i < 6; i++)
            {
                metalVoxelMesh.AddTextureId(textureId);
            }
            metalVoxelMesh.XyzFaces = (byte[])CubeMeshUtil.CubeFaceIndices.Clone();
            metalVoxelMesh.XyzFacesCount = 6;
            metalVoxelMesh.Rgba.Fill(byte.MaxValue);
            MeshData slagVoxelMesh = metalVoxelMesh.Clone();

            List<MeshData> addedMatVoxelMeshes = new List<MeshData>();
            if (materialsBytes != null)
            {
                for (int i = 0; i < materialsBytes.Length; i++)
                {
                    addedMatVoxelMeshes.Add(metalVoxelMesh.Clone());
                }

                for (int j = 0; j < metalVoxelMesh.Uv.Length; j++)
                {
                    if (j % 2 > 0)
                    {
                        metalVoxelMesh.Uv[j] = tposMetal.y1 + metalVoxelMesh.Uv[j] * 2f / (float)capi.BlockTextureAtlas.Size.Height;
                        slagVoxelMesh.Uv[j] = tposSlag.y1 + slagVoxelMesh.Uv[j] * 2f / (float)capi.BlockTextureAtlas.Size.Height;
                        for (int i = 0; i < materialsBytes.Length; i++)
                        {
                            addedMatVoxelMeshes[i].Uv[j] = tposAddedMatList[i].y1 + addedMatVoxelMeshes[i].Uv[j] * 2f / (float)capi.BlockTextureAtlas.Size.Height;
                        }
                    }
                    else
                    {
                        metalVoxelMesh.Uv[j] = tposMetal.x1 + metalVoxelMesh.Uv[j] * 2f / (float)capi.BlockTextureAtlas.Size.Width;
                        slagVoxelMesh.Uv[j] = tposSlag.x1 + slagVoxelMesh.Uv[j] * 2f / (float)capi.BlockTextureAtlas.Size.Width;
                        for (int i = 0; i < materialsBytes.Length; i++)
                        {
                            addedMatVoxelMeshes[i].Uv[j] = tposAddedMatList[i].x1 + addedMatVoxelMeshes[i].Uv[j] * 2f / (float)capi.BlockTextureAtlas.Size.Width;
                        }
                    }
                }
            }
            else
            {
                for (int j = 0; j < metalVoxelMesh.Uv.Length; j++)
                {
                    if (j % 2 > 0)
                    {
                        metalVoxelMesh.Uv[j] = tposMetal.y1 + metalVoxelMesh.Uv[j] * 2f / (float)capi.BlockTextureAtlas.Size.Height;
                        slagVoxelMesh.Uv[j] = tposSlag.y1 + slagVoxelMesh.Uv[j] * 2f / (float)capi.BlockTextureAtlas.Size.Height;
                    }
                    else
                    {
                        metalVoxelMesh.Uv[j] = tposMetal.x1 + metalVoxelMesh.Uv[j] * 2f / (float)capi.BlockTextureAtlas.Size.Width;
                        slagVoxelMesh.Uv[j] = tposSlag.x1 + slagVoxelMesh.Uv[j] * 2f / (float)capi.BlockTextureAtlas.Size.Width;
                    }
                }
            }

            MeshData metVoxOffset = metalVoxelMesh.Clone();
            MeshData slagVoxOffset = slagVoxelMesh.Clone();
            List<MeshData> addedMatVoxOffsets = new List<MeshData>();
            int voxelCount = 0;
            int[] voxelIndexMap = new int[16 * 6 * 16];

            if (materialsBytes != null)
            {
                for (int i = 0; i < materialsBytes.Length; i++)
                {
                    addedMatVoxOffsets.Add(addedMatVoxelMeshes[i].Clone());
                }
                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 6; y++)
                    {
                        for (int z = 0; z < 16; z++)
                        {
                            VoxelMaterials mat = (VoxelMaterials)((voxels[x, y, z] > 1) ? materialsBytes[voxels[x, y, z] - 2] : voxels[x, y, z]);
                            if (mat != VoxelMaterials.Empty)
                            {
                                float px = (float)x / 16f;
                                float py = 0.625f + (float)y / 16f;
                                float pz = (float)z / 16f;
                                MeshData mesh = new MeshData();
                                MeshData meshVoxOffset = new MeshData();

                                if (mat == VoxelMaterials.Metal)
                                {
                                    mesh = metalVoxelMesh;
                                    meshVoxOffset = metVoxOffset;
                                }
                                else if (mat == VoxelMaterials.Slag)
                                {
                                    mesh = slagVoxelMesh;
                                    meshVoxOffset = slagVoxOffset;
                                }
                                else if ((byte)mat > 3)
                                {
                                    byte index = (byte)Array.IndexOf(materialsBytes, (byte)mat);

                                    mesh = addedMatVoxelMeshes[index];
                                    meshVoxOffset = addedMatVoxOffsets[index];
                                }

                                for (int k = 0; k < mesh.xyz.Length; k += 3)
                                {
                                    meshVoxOffset.xyz[k] = px + mesh.xyz[k];
                                    meshVoxOffset.xyz[k + 1] = py + mesh.xyz[k + 1];
                                    meshVoxOffset.xyz[k + 2] = pz + mesh.xyz[k + 2];
                                }
                                float textureSize = 32f / (float)capi.BlockTextureAtlas.Size.Width;
                                float offsetX = px * textureSize;
                                float offsetY = py * 32f / (float)capi.BlockTextureAtlas.Size.Width;
                                float offsetZ = pz * textureSize;
                                for (int l = 0; l < mesh.Uv.Length; l += 2)
                                {
                                    meshVoxOffset.Uv[l] = mesh.Uv[l] + GameMath.Mod(offsetX + offsetY, textureSize);
                                    meshVoxOffset.Uv[l + 1] = mesh.Uv[l + 1] + GameMath.Mod(offsetZ + offsetY, textureSize);
                                }
                                for (int m = 0; m < meshVoxOffset.CustomBytes.Values.Length; m++)
                                {
                                    byte glowSub = (byte)GameMath.Clamp(10 * (Math.Abs(x - 8) + Math.Abs(z - 8) + Math.Abs(y - 2)), 100, 250);
                                    meshVoxOffset.CustomBytes.Values[m] = (byte)((mat == VoxelMaterials.Metal || (byte)mat > 3) ? 0 : glowSub);
                                }

                                if ((hasThermoInfo && voxelTemps != null) && CUSTOM_MESH_DATA_ACTIVE)
                                {
                                    for (int n = 0; n < meshVoxOffset.CustomInts.Values.Length; n++)
                                    {
                                        meshVoxOffset.CustomInts.Values[n] = voxelCount;
                                    }
                                }
                                voxelCount += 1;
                                workItemMesh.AddMeshData(meshVoxOffset);
                            }
                        }
                    }
                }
            }
            else
            {
                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 6; y++)
                    {
                        for (int z = 0; z < 16; z++)
                        {
                            EnumVoxelMaterial mat = (EnumVoxelMaterial)voxels[x, y, z];
                            if (mat != EnumVoxelMaterial.Empty)
                            {
                                float px = (float)x / 16f;
                                float py = 0.625f + (float)y / 16f;
                                float pz = (float)z / 16f;
                                MeshData mesh = (mat == EnumVoxelMaterial.Metal) ? metalVoxelMesh : slagVoxelMesh;
                                MeshData meshVoxOffset = (mat == EnumVoxelMaterial.Metal) ? metVoxOffset : slagVoxOffset;
                                for (int k = 0; k < mesh.xyz.Length; k += 3)
                                {
                                    meshVoxOffset.xyz[k] = px + mesh.xyz[k];
                                    meshVoxOffset.xyz[k + 1] = py + mesh.xyz[k + 1];
                                    meshVoxOffset.xyz[k + 2] = pz + mesh.xyz[k + 2];
                                }
                                float textureSize = 32f / (float)capi.BlockTextureAtlas.Size.Width;
                                float offsetX = px * textureSize;
                                float offsetY = py * 32f / (float)capi.BlockTextureAtlas.Size.Width;
                                float offsetZ = pz * textureSize;
                                for (int l = 0; l < mesh.Uv.Length; l += 2)
                                {
                                    meshVoxOffset.Uv[l] = mesh.Uv[l] + GameMath.Mod(offsetX + offsetY, textureSize);
                                    meshVoxOffset.Uv[l + 1] = mesh.Uv[l + 1] + GameMath.Mod(offsetZ + offsetY, textureSize);
                                }
                                for (int m = 0; m < meshVoxOffset.CustomBytes.Values.Length; m++)
                                {
                                    byte glowSub = (byte)GameMath.Clamp(10 * (Math.Abs(x - 8) + Math.Abs(z - 8) + Math.Abs(y - 2)), 100, 250);
                                    meshVoxOffset.CustomBytes.Values[m] = (byte)(mat == EnumVoxelMaterial.Metal ? 0 : glowSub);
                                }

                                if ((hasThermoInfo && voxelTemps != null) && CUSTOM_MESH_DATA_ACTIVE)
                                {
                                    for (int n = 0; n < meshVoxOffset.CustomInts.Values.Length; n++)
                                    {
                                        meshVoxOffset.CustomInts.Values[n] = voxelCount;
                                    }
                                }
                                voxelCount += 1;
                                workItemMesh.AddMeshData(meshVoxOffset);
                            }
                        }
                    }
                }
            }


            __result = workItemMesh;
            return false;
        }
    }

}
