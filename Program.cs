using System;
using System.IO;
using System.Threading;
using System.Reflection;
using Terraria;
using System.Collections.Generic;
using System.Text;

namespace JourneyMod
{
    class Program
    {
        static bool running = false;
        static bool autodelete = false;

        static void Main(string[] args)
        {
            try
            {
                string[] names = new[] { "Newtonsoft.Json", "RailSDK.Net", "Steamworks.NET", "Ionic.Zip.CF", "ReLogic", "CsvHelper", "MP3Sharp", "NVorbis", "System.ValueTuple" };
                var terrariaAssembly = Assembly.GetAssembly(typeof(Item));
                foreach (string lib in terrariaAssembly.GetManifestResourceNames())
                {
                    if (lib.StartsWith("Terraria.Libraries"))
                    {
                        foreach (string name in names)
                        {
                            if (lib.EndsWith(name + ".dll"))
                            {
                                var s = terrariaAssembly.GetManifestResourceStream(lib);
                                using (BinaryReader br = new BinaryReader(s))
                                {
                                    File.WriteAllBytes(name + ".dll", br.ReadBytes(1000000));
                                }
                                break;
                            }
                        }
                    }
                }
                Log("Extracted libraries.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Log("Launching Terraria ...");
            running = true;
            var thread = new Thread(new ThreadStart(Journey));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Terraria.Program.LaunchGame(args);
            running = false;
        }

        static void Command(string cmd)
        {
            var sb = new StringBuilder();
            Item item;
            switch (cmd)
            {
                case "research":
                    foreach (var kvp in Terraria.Main.LocalPlayerCreativeTracker.ItemSacrifices.SacrificesCountByItemIdCache)
                    {
                        sb.AppendLine(kvp.Key.ToString() + "," + kvp.Value.ToString());
                    }
                    File.WriteAllText("research.csv", sb.ToString());
                    break;
                case "items":
                    item = new Item();
                    var count = 0;
                    for (int i = 0; i < Terraria.ID.ItemID.Count; i++)
                    {
                        item.SetDefaults(i);
                        Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(i, out count);
                        sb.AppendLine(i.ToString() + "," + item.Name + "," + count.ToString());
                    }
                    File.WriteAllText("items.csv", sb.ToString());
                    break;
                case "recipes":
                    foreach (var recipe in Terraria.Main.recipe)
                    {
                        sb.Append(recipe.createItem.netID.ToString() + ",");
                        foreach (Item i in recipe.requiredItem)
                        {
                            sb.Append(i.netID + "|");
                        }
                        sb.AppendLine();
                    }
                    File.WriteAllText("recipes.csv", sb.ToString());
                    break;
                case "craft":
                case "c":
                    AutoCraft();
                    break;
                case "share":
                    string code = GetShareCode();
                    Log("Share code: " + code);
                    /*
                    Log("Waiting to edit sign");
                    Message("Edit a sign to share!");
                    while (!Terraria.Main.editSign)
                    {
                        Thread.Sleep(100);
                    }
                    Terraria.Main.npcChatText = "SHARE/" + code;
                    Terraria.Main.SubmitSignText();*/
                    System.Windows.Clipboard.SetText("SHARE/" + code);
                    break;
                case "get":
                    var text = System.Windows.Clipboard.GetText();
                    if (text.StartsWith("SHARE/"))
                    {
                        try
                        {
                            LoadShareCode(text.Substring(6));
                            Message("Import success!");
                        }
                        catch { Message("Import failed."); }
                    }
                    break;
                case "toggledelete":
                case "td":
                    autodelete = !autodelete;
                    Message("Auto Delete: " + (autodelete ? "ON" : "OFF"));
                    break;
                default:
                    Message("Invalid Command \'" + cmd + "\'!"); break;
            }
        }

        static bool ItemIsResearched(int id)
        {
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(id, out int count);
            return (count > 0 && Terraria.Main.LocalPlayerCreativeTracker.ItemSacrifices.GetSacrificeCount(id) >= count)
                || (id == 704 && ItemIsResearched(22));
        }

        static void ResearchItem(int id)
        {
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(id, out int count);
            if (count > 0)
            {
                var current = Terraria.Main.LocalPlayerCreativeTracker.ItemSacrifices.GetSacrificeCount(id);
                Terraria.Main.LocalPlayerCreativeTracker.ItemSacrifices.RegisterItemSacrifice(id, count - current);
            }
        }

        static void Message(string msg)
        {
            Terraria.Chat.ChatHelper.BroadcastChatMessage(Terraria.Localization.NetworkText.FromLiteral(msg), Microsoft.Xna.Framework.Color.White);
        }

        static void JourneyRecipe()
        {
            List<int> items = new List<int>();
            foreach (var recipe in Terraria.Main.recipe)
            {
                //make it inaccessible to craft already researched items
                if (ItemIsResearched(recipe.createItem.netID)) { recipe.needGraveyardBiome = true; recipe.needLava = true; recipe.needWater = true; recipe.needHoney = true; recipe.needSnowBiome = true; }
                else
                {
                    items.Clear();
                    foreach (Item i in recipe.requiredItem)
                    {
                        items.Add(i.netID);
                        if (i.netID != 0 && !ItemIsResearched(i.netID)) items.Add(i.stack);
                        else items.Add(0);
                    }
                    recipe.SetIngridients(items.ToArray());
                }
            }
        }

        static void AutoCraft()
        {
            foreach (var index in Terraria.Main.availableRecipe)
            {
                var recipe = Terraria.Main.recipe[index];
                if (!ItemIsResearched(recipe.createItem.netID))
                {
                    var flag = true;
                    foreach (Item i in recipe.requiredItem)
                    {
                        if (i.netID != 0 && !ItemIsResearched(i.netID)) flag = false;
                    }
                    if (flag) { ResearchItem(recipe.createItem.netID); Message("Crafted " + recipe.createItem.Name + " ([i:" + recipe.createItem.netID.ToString() + "])"); }
                }
            }
            JourneyRecipe();
        }

        static void AutoResearch(Item item)
        {
            Terraria.Main.LocalPlayerCreativeTracker.ItemSacrifices.RegisterItemSacrifice(item.netID, item.stack);
            if (ItemIsResearched(item.netID)) { Log("Researched: " + item.Name); Message("Unlocked " + item.Name + " ([i:" + item.netID.ToString() + "])"); }
            else item.TurnToAir();
        }

        static string GetShareCode()
        {
            byte[] data = new byte[Terraria.ID.ItemID.Count / 8];
            for (int b = 0; b < data.Length; b++)
            {
                for (int i = b * 8; i < (b + 1) * 8; i++)
                {
                    data[b] <<= 1;
                    try
                    {
                        if (ItemIsResearched(i)) data[b] += 1;
                    }
                    catch { }
                }
            }
            return Convert.ToBase64String(data);
        }

        static string LoadShareCode(string code)
        {
            var item = new Item();
            byte[] data = Convert.FromBase64String(code);
            for (int b = 0; b < data.Length; b++)
            {
                for (int i = b * 8 + 7; i >= b * 8; i--)
                {
                    if ((data[b] & 1) > 0)
                    {
                        item.SetDefaults(i);
                        Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(i, out int count);
                        int n = count - Terraria.Main.LocalPlayerCreativeTracker.ItemSacrifices.GetSacrificeCount(i);
                        if (n > 0)
                        {
                            Terraria.Main.LocalPlayerCreativeTracker.ItemSacrifices.RegisterItemSacrifice(i, n);
                            Message("Received " + item.Name + " ([i:" + item.netID.ToString() + "])");
                        }
                    }
                    data[b] >>= 1;
                }
            }
            return Convert.ToBase64String(data);
        }

        static void Journey()
        {
            while (!Terraria.Program.LoadedEverything) { }
            Log("Started JourneyMod.");
            try
            {
                while (running && (!Terraria.Main.GameModeInfo.IsJourneyMode || Terraria.Main.gameMenu)) { Thread.Sleep(100); }
                while (running)
                {
                    Log("Found Journey Mode character.");
                    JourneyRecipe();
                    while (running && (Terraria.Main.GameModeInfo.IsJourneyMode && !Terraria.Main.gameMenu))
                    {
                        Thread.Sleep(100);

                        // Auto-re new items, infinite stack favourites
                        foreach (Item item in Terraria.Main.LocalPlayer.inventory)
                        {
                            if (item.newAndShiny)
                            {
                                item.newAndShiny = false;
                                if (ItemIsResearched(item.netID)) { if (autodelete) item.TurnToAir(); }
                                else AutoResearch(item);
                            }
                            else if (item.favorited && ItemIsResearched(item.netID)) item.stack = item.maxStack;
                        }

                        // Auto-re mouse item
                        if (Terraria.Main.mouseItem != null)
                        {
                            if (!ItemIsResearched(Terraria.Main.mouseItem.netID)) AutoResearch(Terraria.Main.mouseItem);
                        }

                        // Command handling
                        if (Terraria.Main.chatText.Length > 1 && Terraria.Main.chatText.StartsWith("/") && Terraria.Main.chatText.EndsWith("/"))
                        {
                            Command(Terraria.Main.chatText.Substring(1, Terraria.Main.chatText.Length - 2)); Terraria.Main.chatText = "";
                        }
                    }
                    Log("Waiting for Journey Mode character...");
                    while (running && (!Terraria.Main.GameModeInfo.IsJourneyMode || Terraria.Main.gameMenu)) { Thread.Sleep(100); }
                }
            }
            catch (Exception e)
            {
                Log("Error in journey thread: " + e.ToString());
            }
            Log("JourneyMod thread stopped.");
        }

        static void Log(string text)
        {
            Console.WriteLine("[JMOD] " + text);
        }
    }
}
