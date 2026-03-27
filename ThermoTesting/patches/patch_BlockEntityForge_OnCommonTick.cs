using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ThermoTesting.patches;

[HarmonyPatch(typeof(Vintagestory.GameContent.BlockEntityForge), "OnCommonTick")]
public class PatchBEForgeOnCommonTick
{
    static readonly AccessTools.FieldRef<BlockEntityForge, ItemStack> contentsRef = AccessTools.FieldRefAccess<BlockEntityForge, ItemStack>("contents");

    public static void Postfix(float dt, BlockEntityForge __instance)
    {
        ItemStack contents = contentsRef(__instance);
        

        //need to add a lookup to see if the contents exist within the lookup table already
        if (contents != null)
        {
            int hash = contents.GetHashCode();
            byte[,,] voxels = BlockEntityAnvil.deserializeVoxels(contents.Attributes.GetBytes("voxels", null));
            if (voxels == null)
            {
                //Ripped from ItemIngot.CreateVoxelsFromIngot to remove blister steel option
                //This is just to create temperature voxels for ingots placed within forge, since they have not
                //  yet been turned into workitems and do not have voxels yet. This pregenerates a voxel structure 
                //  that temperatures can be assigned to.
                voxels = new byte[16, 6, 16];
                for (int x = 0; x < 7; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int z = 0; z < 3; z++)
                        {
                            voxels[4 + x, y, 6 + z] = 1;
                        }
                    }
                }
            }

            //ExternalData.SetTemperatureData(contents, ExternalData.GetHomogenousTemperatureArrayFromMask(voxels, 69.0f));
            float[,,] temperatures;
            float[] flatTemperatures;

            ExternalData.GetRandomTemperatureArrayFromMaskDualReturn(voxels, out temperatures, out flatTemperatures);
            ExternalData.AddOrUpdateTemperatureDataDual(contents, temperatures, flatTemperatures);

            //ExternalData.AddOrUpdateTemperatureData(contents, ExternalData.GetRandomTemperatureArrayFromMask(voxels));
        }
        //tree.SetBytes("temperatureVoxels", );
    }
}
