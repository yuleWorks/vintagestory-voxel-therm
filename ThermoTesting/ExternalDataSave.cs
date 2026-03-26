using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThermoTesting
{
    public static class ExternalDataSave
    {
        public static float[] Flatten(float[,,] src)
        {
            int sx = src.GetLength(0), sy = src.GetLength(1), sz = src.GetLength(2);
            var dst = new float[sx * sy * sz];
            int i = 0;
            for (int x = 0; x < sx; x++)
                for (int y = 0; y < sy; y++)
                    for (int z = 0; z < sz; z++)
                        dst[i++] = src[x, y, z];
            return dst;
        }

        public static float[,,] Unflatten(float[] src, int sx, int sy, int sz)
        {
            var dst = new float[sx, sy, sz];
            int i = 0;
            for (int x = 0; x < sx; x++)
                for (int y = 0; y < sy; y++)
                    for (int z = 0; z < sz; z++)
                        dst[x, y, z] = src[i++];
            return dst;
        }
    }

    [ProtoContract]
    public class ThermoSave
    {
        // protobuf-net often doesn't like Guid as a key.
        // Store as string or byte[].
        [ProtoMember(1)]
        public List<ThermoEntry> Entries { get; set; } = new();
    }

    [ProtoContract]
    public class ThermoEntry
    {
        [ProtoMember(1)]
        public string Id { get; set; } = "";   // Guid as string

        [ProtoMember(2)]
        public ThermoDataSave Data { get; set; } = new();
    }

    [ProtoContract]
    public class ThermoDataSave
    {
        [ProtoMember(1)] public int SizeX { get; set; }
        [ProtoMember(2)] public int SizeY { get; set; }
        [ProtoMember(3)] public int SizeZ { get; set; }

        [ProtoMember(4)] public float[] Temps { get; set; } = Array.Empty<float>();
        [ProtoMember(5)] public float[] FlatTemps { get; set; } = Array.Empty<float>();

        // add other simple fields you need, each with ProtoMember
    }


}
