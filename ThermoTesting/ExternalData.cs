using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;



namespace ThermoTesting.patches
{
    static class ExternalData
    {

        public class ThermoData
        {
            public float[,,] voxelTemperatures;
            public float[] flatTemps;

            public int tempTexId = 0;
            public int tempTexWidth = 0;


            public bool isMeshDirty = true;

            public ThermoData() { }
            public ThermoData(float[,,] in_voxelTemperatures)
            {
                voxelTemperatures = in_voxelTemperatures;
            }
            public ThermoData(float[,,] in_voxelTemperatures, bool in_isMeshDirty)
            {
                voxelTemperatures = in_voxelTemperatures;
                isMeshDirty = in_isMeshDirty;
            }
            public ThermoData(float[,,] in_voxelTemperatures, float[] in_voxelTemperaturesFlat, int in_tempTexId, int in_tempTexWidth, bool in_isMeshDirty)
            {
                voxelTemperatures = in_voxelTemperatures;
                flatTemps = in_voxelTemperaturesFlat;
                isMeshDirty = in_isMeshDirty;
                tempTexId = in_tempTexId;
                tempTexWidth = in_tempTexWidth;

                if (isMeshDirty)
                {
                    //floatLookupTexture = new TemperatureTexture();
                    //floatLookupTexture.UploadTempTex(flatTemps);
                    //isMeshDirty = false;
                }
            }
        }

        //static ConditionalWeakTable<ItemStack, TemperatureData> table = new ConditionalWeakTable<ItemStack, TemperatureData>();

        public static readonly ConcurrentDictionary<Guid, ThermoData> table = new ConcurrentDictionary<Guid, ThermoData>();

        public static void InitializeTable(byte[] data)
        {
            if (data == null) return;
            Dictionary<Guid, ThermoData> dict = SerializerUtil.Deserialize<Dictionary<Guid, ThermoData>>(data);
            table.Clear();
            foreach (var entry in dict) {
                table[entry.Key] = entry.Value;
            }
        }

        private static bool TryGetAttributeGuid(ItemStack obj, out Guid result)
        {
            string str_voxTempGuid = obj.Attributes.GetString("voxTemp", null);
            if (str_voxTempGuid == null)
            {
                result = Guid.Empty;
                return false;
            }

            if (!Guid.TryParseExact(str_voxTempGuid, "N", out var voxTempGuid))
            {
                //
                //parsing failed
                throw new Exception("Voxel Temp Parse Failed: {str_voxTempGuid}");
            }

            result = voxTempGuid;
            return true;
        }

        public static ThermoData? TryGetTemperatureData(ItemStack obj)
        {
            if(TryGetAttributeGuid(obj, out var voxTempGuid))
            {
                table.TryGetValue(voxTempGuid, out var thermoData);
                return thermoData;
            }
            return null;
        }

        public static void AddOrUpdateTemperatureData(ItemStack obj, float[,,] value) {
            Guid guid = new Guid();
            if (!TryGetAttributeGuid(obj, out guid))
            {
                guid = Guid.NewGuid();
                string str_guid = guid.ToString("N");
                obj.Attributes.SetString("voxTemp", str_guid);
            }


            ThermoData? oldData = null; // will stay null if this is an add
            table.AddOrUpdate(
                guid,
                // ADD: key wasn't present
                addValueFactory: _ => new ThermoData(value),

                // UPDATE: key was present; oldData is the previous value
                updateValueFactory: (_, existing) =>
                {
                    oldData = existing;        // capture old value
                    return new ThermoData(value);  // overwrite with new grid
                }
            );
        }

        // @Todo: make separate temperature construction functions for if the mesh is new vs if only temps are updated
        public static void AddOrUpdateTemperatureDataDual(ItemStack obj, float[,,] temps, float[] flatTemps)
        {
            Guid guid = new Guid();
            if (!TryGetAttributeGuid(obj, out guid))
            {
                guid = Guid.NewGuid();
                string str_guid = guid.ToString("N");
                obj.Attributes.SetString("voxTemp", str_guid);


                table.AddOrUpdate(
                guid,
                // ADD: key wasn't present
                addValueFactory: _ => new ThermoData(temps, true),

                // UPDATE: key was present; oldData is the previous value
                updateValueFactory: (_, existing) =>
                {
                    return new ThermoData(temps, true);  // overwrite with new grid
                }               
                );
                return;
            }

            ThermoData? oldData = null; // will stay null if this is an add
            table.AddOrUpdate(
                guid,
                // ADD: key wasn't present
                addValueFactory: _ => new ThermoData(temps, flatTemps, oldData.tempTexId, oldData.tempTexWidth, oldData.isMeshDirty),

                // UPDATE: key was present; oldData is the previous value
                updateValueFactory: (_, existing) =>
                {
                    oldData = existing;        // capture old value
                    return new ThermoData(temps, flatTemps, oldData.tempTexId, oldData.tempTexWidth, oldData.isMeshDirty);  // overwrite with new grid
                }
            );


        }

        public static float[,,] GetHomogenousTemperatureArrayFromMask(byte[,,] voxels, float temperature)
        {
            float[,,] temperatures = new float[16, 6, 16];
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        if (voxels[x, y, z] != 0)
                        {
                            temperatures[x, y, z] = temperature;
                        } else
                        {
                            temperatures[x, y, z] = 0;
                        }
                    }
                }
            }
            return temperatures;
        }

        public static float[,,] GetRandomTemperatureArrayFromMask(byte[,,] voxels)
        {
            float[,,] temperatures = new float[16, 6, 16];
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        if (voxels[x, y, z] != 0)
                        {
                            temperatures[x, y, z] = Random.Shared.NextSingle();
                        }
                        else
                        {
                            temperatures[x, y, z] = 0;
                        }
                    }
                }
            }
            return temperatures;
        }
        public static void GetRandomTemperatureArrayFromMaskDualReturn(byte[,,] voxels, out float[,,] out_temps, out float[] out_flatTemps)
        {
            Random rnd = new Random();
            float[,,] temperatures = new float[16, 6, 16];
            float[] flatTemperatures = new float[16 * 6 * 16];
            int voxelCount = 0;
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        if (voxels[x, y, z] != 0)
                        {
                            float randTemp = rnd.Next(500, 1100);
                            temperatures[x, y, z] = randTemp;
                            flatTemperatures[voxelCount++] = randTemp;
                        }
                        else
                        {
                            temperatures[x, y, z] = 0;
                        }
                    }
                }
            }
            out_temps = temperatures;
            Array.Resize(ref flatTemperatures, voxelCount);
            out_flatTemps = flatTemperatures;
        }        
    }
}
