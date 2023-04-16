using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Terraria
{
    public static class JourneyMod
    {
        static bool autodelete = false;
        static bool welcome = false;

        static void Command(string cmd)
        {
            var sb = new StringBuilder();
            Item item;
            switch (cmd)
            {
                //case "research":
                //    foreach (var kvp in Terraria.Main.LocalPlayerCreativeTracker.ItemSacrifices.)
                //    {
                //        sb.AppendLine(kvp.Key.ToString() + "," + kvp.Value.ToString());
                //    }
                //    File.WriteAllText("research.csv", sb.ToString());
                //    break;
                case "items":
                    item = new Item();
                    int count;
                    for (int i = 0; i < ID.ItemID.Count; i++)
                    {
                        item.SetDefaults(i);
                        GameContent.Creative.CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(i, out count);
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
                case "export":
                    string code = GetShareCode();
                    Message("Research copied to clipboard. Share it with a friend!");
                    System.Windows.Forms.Clipboard.SetText("SHARE/" + code);
                    break;
                case "import":
                    var text = System.Windows.Forms.Clipboard.GetText();
                    if (text.StartsWith("SHARE/"))
                    {
                        try
                        {
                            LoadShareCode(text.Substring(6));
                            Message("Import from clipboard success!");
                        }
                        catch { Message("Import from clipboard failed."); }
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
            GameContent.Creative.CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(id, out int count);
            return (count > 0 && Main.LocalPlayerCreativeTracker.ItemSacrifices.GetSacrificeCount(id) >= count)
                || (id == 704 && ItemIsResearched(22));
        }

        static void ResearchItem(int id)
        {
            GameContent.Creative.CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(id, out int count);
            if (count > 0)
            {
                var current = Main.LocalPlayerCreativeTracker.ItemSacrifices.GetSacrificeCount(id);
                Terraria.Main.LocalPlayerCreativeTracker.ItemSacrifices.RegisterItemSacrifice(id, count - current);
            }
        }

        static void Message(string msg)
        {
            Chat.ChatHelper.BroadcastChatMessage(Localization.NetworkText.FromLiteral(msg), Microsoft.Xna.Framework.Color.White);
        }

        static void JourneyRecipe()
        {
            List<int> items = new List<int>();
            for (int r = 0; r < Recipe.numRecipes; r++) {

                var recipe = Terraria.Main.recipe[r];
                if (recipe is null) continue;

                // make it inaccessible to craft already researched items
                if (ItemIsResearched(recipe.createItem.netID)) 
                { 
                    recipe.needGraveyardBiome = true; 
                    recipe.needLava = true;
                    recipe.needWater = true;
                    recipe.needHoney = true;
                    recipe.needSnowBiome = true;
                }
                else
                {
                    var filler = 0;
                    items.Clear();
                    foreach (Item i in recipe.requiredItem)
                    {
                        if (i.netID != 0 && !ItemIsResearched(i.netID))
                        {
                            items.Add(i.netID);
                            items.Add(i.stack);
                        }
                        else
                        {
                            filler += 1;
                        }
                    }

                    for (int i = 0; i < filler; i++)
                    {
                        items.Add(0);
                        items.Add(0);
                    }
                }

                recipe.SetIngredients(items.ToArray());
            }
        }

        static void AutoCraft()
        {
            foreach (var index in Main.availableRecipe)
            {
                var recipe = Main.recipe[index];
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
            Main.LocalPlayerCreativeTracker.ItemSacrifices.RegisterItemSacrifice(item.netID, item.stack);
            if (ItemIsResearched(item.netID)) { Message("Unlocked " + item.Name + " ([i:" + item.netID.ToString() + "])"); }
            else item.TurnToAir();
        }

        static string GetShareCode()
        {
            byte[] data = new byte[ID.ItemID.Count / 8];
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
                        GameContent.Creative.CreativeItemSacrificesCatalog.Instance.TryGetSacrificeCountCapToUnlockInfiniteItems(i, out int count);
                        int n = count - Main.LocalPlayerCreativeTracker.ItemSacrifices.GetSacrificeCount(i);
                        if (n > 0)
                        {
                            Main.LocalPlayerCreativeTracker.ItemSacrifices.RegisterItemSacrifice(i, n);
                            Message("Received " + item.Name + " ([i:" + item.netID.ToString() + "])");
                        }
                    }
                    data[b] >>= 1;
                }
            }
            return Convert.ToBase64String(data);
        }

        public static void MainLoop()
        {
            if (!Main.GameModeInfo.IsJourneyMode || Main.gameMenu) return;

            if (!welcome) { Message("Welcome to JourneyMod!"); welcome = true; }

            // Auto-re new items, infinite stack favourites
            foreach (Item item in Main.LocalPlayer.inventory)
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
            if (Main.mouseItem != null)
            {
                if (!ItemIsResearched(Main.mouseItem.netID)) AutoResearch(Main.mouseItem);
            }

            // Command handling
            if (Main.chatText.Length > 1 && Main.chatText.StartsWith("/") && Main.chatText.EndsWith("/"))
            {
                Command(Main.chatText.Substring(1, Main.chatText.Length - 2)); Main.chatText = "";
            }
        }
    }

    // patch this class into the assembly
    // patch to use old crafting
    // patch MainLoop into actual main loop
}
