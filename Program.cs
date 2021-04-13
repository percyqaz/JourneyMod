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
                string[] names = new[] { "Newtonsoft.Json", "RailSDK.Net", "Steamworks.NET", "Ionic.Zip.CF", "ReLogic" };
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
            new Thread(new ThreadStart(Journey)).Start();
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
                case "god":
                    Terraria.Main.LocalPlayer.SetImmuneTimeForAllTypes(50000);
                    item = Terraria.Main.LocalPlayer.inventory[Terraria.Main.LocalPlayer.selectedItem];
                    item.damage = item.OriginalDamage * 5;
                    break;
                case "usetime":
                case "ut":
                    item = Terraria.Main.LocalPlayer.inventory[Terraria.Main.LocalPlayer.selectedItem];
                    item.useAnimation = 100;
                    item.useTime = 0;
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
            return count > 0 && Terraria.Main.LocalPlayerCreativeTracker.ItemSacrifices.GetSacrificeCount(id) >= count;
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
                        if (Terraria.Main.mouseItem != null)
                        {
                            if (!ItemIsResearched(Terraria.Main.mouseItem.netID)) AutoResearch(Terraria.Main.mouseItem);
                        }
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
