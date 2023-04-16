# What's this?

A tool to streamline journey mode item research

Now that Terraria ~~1.4.1.2~~ ~~1.4.2.3~~ ~~1.4.3.6~~ ~~1.4.4.2~~ 1.4.4.9 has released and is ***supposedly*** the final patch for the game, I want to fully research every (obtainable) item in Terraria in Journey Mode.

## Features
- Items you've researched are always treated as "in your inventory" for the purpose of item crafting
- Crafting recipes for items you've already researched are hidden
- Favourited stacks are infinite stacks
- Automatically research anything you pick up, craft or purchase
- Automatically delete anything you pick up that is already researched (this is a toggle)
* When an item is auto-researched and this completes the research, the item stays in your inventory so you can check it out
- Automatically craft + research anything you have researched the materials for
- Export item lists, recipe lists and research lists as CSV files
- Export and import research to clipboard - You can send the clipboard text to friends to share/sync your research items!

Note: You must now fully research an item before you have any of that item which poses its own (fun) challenge

## Install instructions
TModloader isn't always up to date with the latest Terraria version (which keeps changing) and I'm too lazy to figure it out
So, I'm manually patching the .exe myself using the code in JourneyMod.cs

This means I don't have a good way to get this into the hands of users (I cannot distribute the modded .exe as-is)

## Ingame commands

* For commands while ingame on a journey mode character, type `/[COMMAND]/` into the chat box to run a command
* todo: Document what commands are available - for now look at the Command method in JourneyMod.cs

**If you find this repository and it looks useful but you can't get it working/want to know how the commands work, you're welcome to open an issue and I will write the documentation properly**