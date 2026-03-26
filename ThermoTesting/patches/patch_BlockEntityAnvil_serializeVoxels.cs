using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThermoTesting.patches
{
    [HarmonyPatch(typeof(Vintagestory.GameContent.BlockEntityAnvil), "serializeVoxels")]
    internal class PatchBEAnvilSerializeVoxels
    {
        public static bool Prefix(byte[,,] voxels, ref byte[] __result)
        {
            byte[] data = new byte[576];
            int arrayPos = 0;
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        byte[] array = data;
                        array[arrayPos] |= (byte)((voxels[x, y, z * 8 + 0] & 0b00000111) << 5);

                        array[arrayPos] |= (byte)((voxels[x, y, z * 8 + 1] & 0b00000111) << 2);

                        array[arrayPos] |= (byte)((voxels[x, y, z * 8 + 2] & 0b00000110) >> 1);
                        array[arrayPos + 1] |= (byte)((voxels[x, y, z * 8 + 2] & 0b00000001) << 7);

                        array[arrayPos + 1] |= (byte)((voxels[x, y, z * 8 + 3] & 0b00000111) << 4);

                        array[arrayPos + 1] |= (byte)((voxels[x, y, z * 8 + 4] & 0b00000111) << 1);

                        array[arrayPos + 1] |= (byte)((voxels[x, y, z * 8 + 5] & 0b00000100) >> 2);
                        array[arrayPos + 2] |= (byte)((voxels[x, y, z * 8 + 5] & 0b00000011) << 6);

                        array[arrayPos + 2] |= (byte)((voxels[x, y, z * 8 + 6] & 0b00000111) << 3);

                        array[arrayPos + 2] |= (byte)((voxels[x, y, z * 8 + 7] & 0b00000111));

                        arrayPos += 3;
                    }
                }
            }
            __result = data;
            return false;
        }
    }
}
