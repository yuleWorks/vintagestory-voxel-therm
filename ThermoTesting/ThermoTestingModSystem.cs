using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ThermoTesting.patches;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;


namespace ThermoTesting;
    public enum VoxelMaterials
    {
        /* A */ Empty,
        /* B */ Metal, //TO BE REPLACED WITH ALL METALS IN HIGHER RANGES
        /* C */ Slag,
        /* D */ Placeholder, //NOT USED
        /* E */ copper,
        /* F */ silversolder,
        /* G */ bismuth,
        /* H */ brass,
        /* I */ blackbronze,
        /* J */ tinbronze,
        /* K */ bismuthbronze,
        /* L */ iron,
        /* M */ lead,
        /* N */ chromium,
        /* O */ meteoriciron,
        /* P */ nickel,
        /* W */ stainlesssteel,
        /* R */ steel,
        /* S */ titanium,
        /* T */ zinc,
        /* U */ molybdochalkos,
        /* V */ silver,
        /* W */ gold,
        /* X */ cupronickel
    }
public class ThermoTestingModSystem : ModSystem
{
    ICoreServerAPI serverAPI;
    ICoreClientAPI clientAPI;

    public IShaderProgram anvilShaderProjPatch;

    //private Harmony harmony;

    // Called on server and client
    // Useful for registering block/entity classes on both sides
    public override void Start(ICoreAPI api)
    {
        Mod.Logger.Notification("Hello from template mod: " + api.Side);
        
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("thermotesting:hello"));
        var harmony = new Harmony(Mod.Info.ModID);
        harmony.PatchAll();

        serverAPI = api;
        api.Event.SaveGameLoaded += OnSaveGameLoading;
        api.Event.GameWorldSave += OnSaveGameSaving;


        var parsers = api.ChatCommands.Parsers;
        AssetLocation sound = new AssetLocation("game", "sounds/tutorialstepsuccess");
        api.ChatCommands.Create("wi")
        .WithDescription("gives a half-copper, half-blackbronze demo workitem")
        .RequiresPrivilege(Privilege.chat)
        .RequiresPlayer()
        .HandleWith((args) =>
        {
            ItemStack stack = new ItemStack();
            int recipeId = (from r in api.GetSmithingRecipes()
                            where r.Ingredient.Code.EndVariant() == "copper" && r.Name.ToShortString() == "falx"
                            orderby r.Output.ResolvedItemstack.Collectible.Code
                            select r).ToList()[0].RecipeId;

            List<byte> materials = new List<byte>();
            materials.Add(0b00000001); // Metal (base workitem material, which is copper in this case)
            materials.Add(0b00001000); // Blackbronze (index 8 in the VoxelMaterials enum)

            byte[,,] voxels = new byte[16, 6, 16];
            for (int x = 0; x < 7; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        if (x < 3) voxels[4 + x, y, 6 + z] = 1;
                        else voxels[4 + x, y, 6 + z] = 3; // Artifact of how the added material indexing system works, this should be streamlined.
                    }
                }
            }

            ItemStack wiStack = new ItemStack(api.World.GetItem(new AssetLocation("game:workitem-copper")), 1);
            //wiStack.Collectible.SetTemperature(api.World, stack, 30.0f, true);
            wiStack.Attributes.SetBytes("voxels", BlockEntityAnvil.serializeVoxels(voxels));
            wiStack.Attributes.SetInt("selectedRecipeId", recipeId);
            wiStack.Attributes.SetInt("rotation", 270);
            wiStack.Attributes.SetBytes("addedMats", materials.ToArray());
            stack = wiStack;

            if (args.Caller.Player.InventoryManager.TryGiveItemstack(stack))
            {
                var byEntity = args.Caller.Entity;
                byEntity.World.PlaySoundAt(sound, byEntity);
                return TextCommandResult.Success("workitem given");
            }
            return TextCommandResult.Error("no free slot");
        });

        //harmonyServer.CreateClassProcessor(typeof(PatchBEForgeOnCommonTick)).Patch();
        //harmonyServer.CreateClassProcessor(typeof(PatchBEAnvilToTreeAttributes)).Patch();
        //harmonyServer.CreateClassProcessor(typeof(PatchBEAnvilFromTreeAttributes)).Patch();

    }
    public override void StartClientSide(ICoreClientAPI api)
    {
        Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("thermotesting:hello"));
        //var harmonyClient = new Harmony(Mod.Info.ModID);

        //harmonyClient.CreateClassProcessor(typeof(PatchInventoryItemRendererRenderItemStackToGui)).Patch();
        //harmonyClient.CreateClassProcessor(typeof(PatchAnvilWorkItemRendererOnRenderFrame)).Patch();

        clientAPI = api;

        clientAPI.Event.ReloadShader += LoadShader;
        LoadShader();
    }

    private void OnSaveGameLoading()
    {
        byte[] data = serverAPI.WorldManager.SaveGame.GetData("thermoData");
        if (data == null || data.Length == 0) return;

        ThermoSave save = SerializerUtil.Deserialize<ThermoSave>(data);
        ExternalData.table.Clear();

        foreach (var entry in save.Entries)
        {
            if (!Guid.TryParse(entry.Id, out var guid)) continue;

            var d = entry.Data;
            var temperatures = ExternalDataSave.Unflatten(d.Temps, d.SizeX, d.SizeY, d.SizeZ);
            var flatTemperatures = d.FlatTemps;
            ExternalData.table[guid] = new ExternalData.ThermoData
            {
                voxelTemperatures = temperatures,
                flatTemps = flatTemperatures,
                isMeshDirty = true
            };
        }
    }

    private void OnSaveGameSaving()
    {
        ThermoSave save = new ThermoSave();

        foreach (var kv in ExternalData.table)
        {
            ExternalData.ThermoData thermoData = kv.Value;
            float[,,] temperatures = thermoData.voxelTemperatures;
            int sx = temperatures.GetLength(0), sy = temperatures.GetLength(1), sz = temperatures.GetLength(2);

            save.Entries.Add(new ThermoEntry
            {
                Id = kv.Key.ToString(),
                Data = new ThermoDataSave
                {
                    SizeX = sx,
                    SizeY = sy,
                    SizeZ = sz,
                    Temps = ExternalDataSave.Flatten(temperatures),
                    FlatTemps = thermoData.flatTemps
                }
            });
        }
        serverAPI.WorldManager.SaveGame.StoreData("thermoData", SerializerUtil.Serialize(save));
    }

    private bool LoadShader()
    {
        anvilShaderProjPatch = clientAPI.Shader.NewShaderProgram();
        anvilShaderProjPatch.AssetDomain = Mod.Info.ModID;
        anvilShaderProjPatch.VertexShader = clientAPI.Shader.NewShader(EnumShaderType.VertexShader);
        anvilShaderProjPatch.FragmentShader = clientAPI.Shader.NewShader(EnumShaderType.FragmentShader);
        clientAPI.Shader.RegisterFileShaderProgram("anvilworkitempatch", anvilShaderProjPatch);
        return anvilShaderProjPatch.Compile();
    }
}
