using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using System.Text.Json;
using Terraria.GameContent.Creative;
using System.Security.Cryptography.X509Certificates;

namespace BlessWorld
{
    [ApiVersion(2, 1)]
    public class BlessWorld : TerrariaPlugin
    {

        public override string Author => "Onusai";
        public override string Description => "Converts all ores/bars in the world to their better variant";
        public override string Name => "BlessWorld";
        public override Version Version => new Version(1, 0, 0, 0);

        public class ConfigData
        {
            public bool BlessOnServerStart { get; set; } = false;
            public Dictionary<ushort, ushort> TileIds { get; set; } = new Dictionary<ushort, ushort>()
            {
                {7, 166},       // copper           -> tin
                {6, 167},       // iron             -> lead
                {9, 168},       // silver           -> tungsten
                {8, 169},       // gold             -> platinum
                {107, 221},     // cobalt           -> palladium
                {108, 222},     // mythril          -> orichalcum
                {111, 223}      // adamantite       -> titanium
            };

            public Dictionary<int, int> ItemIds { get; set; } = new Dictionary<int, int>()
            {
                {12, 699},      // copper ore       -> tin ore
                {11, 700},      // iron ore         -> lead ore
                {14, 701},      // silver ore       -> tungsten ore
                {13, 702},      // gold ore         -> platinum ore
                {364, 1104},    // cobalt ore       -> palladium ore
                {365, 1105},    // mythril ore      -> orichalcum ore
                {366, 1106},    // adamantite ore   -> titanium ore

                {19, 706},      // gold bar         -> platinum bar
                {20, 703},      // copper bar       -> tin bar
                {21, 705},      // silver bar       -> tungsten bar
                {22, 704},      // iron bar         -> lead bar
                {381, 1184},    // cobalt bar       -> palladium bar
                {382, 1191},    // mythril bar      -> orichalcum bar
                {391, 1198},    // adamantite bar   -> titanium bar

                {848, 857}      // pharaoh's mask   -> sandstorm in a bottle
            };
        }

        ConfigData configData;

        public BlessWorld(Main game) : base(game) { }

        public override void Initialize()
        {
            configData = PluginConfig.Load("BlessWorld");

            ServerApi.Hooks.GameInitialize.Register(this, OnGameLoad);
        }

        void OnGameLoad(EventArgs e)
        {
            RegisterCommand("blessworld", "tshock.admin", Bless, "Converts all ores/bars in the world to their better variant");
            ServerApi.Hooks.GamePostInitialize.Register(this, OnWorldLoaded);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameLoad);
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnWorldLoaded);
            }
            base.Dispose(disposing);
        }

        void RegisterCommand(string name, string perm, CommandDelegate handler, string helptext)
        {
            TShockAPI.Commands.ChatCommands.Add(new Command(perm, handler, name)
            { HelpText = helptext });
        }

        void OnWorldLoaded(EventArgs e)
        {
            if (configData.BlessOnServerStart) Bless();
        }

        void Bless(CommandArgs args = null)
        {
            int tilesUpdated = 0;
            int itemsUpdated = 0;

            for (int x = 0; x < Main.maxTilesX; x++)
            {
                for (int y = 0; y < Main.maxTilesY; y++)
                {
                    if (configData.TileIds.ContainsKey(Main.tile[x, y].type))
                    {
                        Main.tile[x, y].type = configData.TileIds[Main.tile[x, y].type];
                        tilesUpdated++;
                    }
                }
            }

            foreach (Chest chest in Main.chest)
            {
                if (chest == null) continue;

                foreach (Item item in chest.item)
                {
                    if (configData.ItemIds.ContainsKey(item.type))
                    {
                        var count = item.stack;
                        item.SetDefaults(configData.ItemIds[item.type]);
                        item.stack = count;
                        itemsUpdated++;
                    }
                }
            }

            if (tilesUpdated + itemsUpdated > 0) UpdateWorld();

            var msg = "The world has been blessed! t=" + tilesUpdated + " i=" + itemsUpdated;
            if (args != null) args.Player.SendMessage(msg, Color.Pink);
            else Console.WriteLine(msg);

        }

        void UpdateWorld()
        {
            foreach (RemoteClient sock in Netplay.Clients.Where(s => s.IsActive))
            {
                for (int i = Netplay.GetSectionX(0); i <= Netplay.GetSectionX(Main.maxTilesX); i++)
                {
                    for (int j = Netplay.GetSectionY(0); j <= Netplay.GetSectionY(Main.maxTilesY); j++)
                        sock.TileSections[i, j] = false;
                }
            }
        }

        public static class PluginConfig
        {
            public static string filePath;
            public static ConfigData Load(string Name)
            {
                filePath = String.Format("{0}/{1}.json", TShock.SavePath, Name);

                if (!File.Exists(filePath))
                {
                    var data = new ConfigData();
                    Save(data);
                    return data;
                }

                var jsonString = File.ReadAllText(filePath);
                var myObject = JsonSerializer.Deserialize<ConfigData>(jsonString);

                return myObject;
            }

            public static void Save(ConfigData myObject)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var jsonString = JsonSerializer.Serialize(myObject, options);

                File.WriteAllText(filePath, jsonString);
            }
        }

    }
}