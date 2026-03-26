using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThermoTesting.patches
{
    [HarmonyPatch(typeof(Vintagestory.GameContent.BlockEntityAnvil), "deserializeVoxels")]
    public class PatchBEAnvilDeserializeVoxels
    {
        public static bool Prefix(byte[] data, ref byte[,,] __result)
        {
            byte[,,] voxels = new byte[16, 6, 16];

            if (data == null || data.Length < 576)
            {
                __result = voxels;
                return false;
            }
            int arrayPos = 0;
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        voxels[x, y, z * 8] = (byte)((data[arrayPos] & 0b11100000) >> 5);
                        voxels[x, y, z * 8 + 1] = (byte)((data[arrayPos] & 0b00011100) >> 2);
                        voxels[x, y, z * 8 + 2] = (byte)((data[arrayPos] & 0b00000011) << 1 | (data[arrayPos + 1] & 0b10000000) >> 7);
                        voxels[x, y, z * 8 + 3] = (byte)((data[arrayPos + 1] & 0b01110000) >> 4);
                        voxels[x, y, z * 8 + 4] = (byte)((data[arrayPos + 1] & 0b00001110) >> 1);
                        voxels[x, y, z * 8 + 5] = (byte)((data[arrayPos + 1] & 0b00000001) | (data[arrayPos + 2] & 0b11000000) >> 6);
                        voxels[x, y, z * 8 + 6] = (byte)((data[arrayPos + 2] & 0b00111000) >> 3);
                        voxels[x, y, z * 8 + 7] = (byte)((data[arrayPos + 2] & 0b00000111));
                        arrayPos += 3;
                    }
                }
            }
            __result = voxels;
            return false;
        }
    }
}
