# What's this?

Now that Terraria 1.4.1.2 has released and is supposedly the final patch for the game, I want to fully research every (obtainable) item in Terraria in Journey Mode.

The Journey Mode GUI is a bit cumbersome and it can take a long time just going through recipes, crafting them, researching them, etc etc and so I have made a tool to greatly assist in this process.

This also regears the game towards having to fully research an item before you can use it at all.

# How does it work?
- Terraria.exe is not obfuscated
- Run Terraria's main game thread from inside the program
- Run another thread alongside it
- Poke at the publicly exposed Terraria variables to achieve desired results from other thread

# Features
- Auto-deletion of any items you pick up that are already researched (can be turned off)
- Auto-research of any items you pick up that are not already researched (cannot be turned off to make it challenging/fun)
* When an item is auto-researched and this completes the research, the item stays in your inventory so you can check it out
- Click any item in your inventory to auto-research it if not already researched
- Favourited items become infinite stacks
## Additional powers via commands
- Tools to export item lists, recipe lists, your character's research data as text files if you want to spreadsheet that stuff
- If you have researched an item, all crafting recipes involving it behave as if you have infinite in your inventory
- If you have researched an item, all crafting recipes FOR it are disabled
- Ability to auto-research every recipe at your current crafting station(s) if all ingredients are researched

# How do I use it?
- Put the .exe in your terraria folder (Where Terraria.exe is)
- Run it (may have to try twice / run as administrator)

* For commands while ingame on a journey mode character, type `/[COMMAND]/` into the chat box to run a command
* todo: Document what commands are available - for now look at Program.cs
