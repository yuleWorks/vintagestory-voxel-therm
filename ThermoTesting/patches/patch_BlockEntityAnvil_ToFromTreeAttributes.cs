using HarmonyLib;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ThermoTesting.patches
{
    [HarmonyPatch(typeof(Vintagestory.GameContent.BlockEntityAnvil), "ToTreeAttributes")]
    public class PatchBEAnvilToTreeAttributes
    {
        static readonly AccessTools.FieldRef<BlockEntityAnvil, ItemStack> _workItemStackRef = AccessTools.FieldRefAccess<BlockEntityAnvil, ItemStack>("_workItemStack");
        public static void Postfix(ITreeAttribute tree, BlockEntityAnvil __instance)
        {
            //tree.SetBytes("temperatureVoxels", );
        }
    }

    [HarmonyPatch(typeof(Vintagestory.GameContent.BlockEntityAnvil), "FromTreeAttributes")]
    public class PatchBEAnvilFromTreeAttributes
    {
        public static void Postfix(ITreeAttribute tree, IWorldAccessor worldForResolving, BlockEntityAnvil __instance)
        {
            
        }
    }


}
