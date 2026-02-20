# VSML Debugging Mod

This is a really simple mod made to inject debug scripts into the game.

Place GML files into `[game dir]/DebugScripts`, and this mod should patch them all into the data file.
They also get registered as dependencies, so VSML should repatch when you change them.

This is intended to be used for adding console commands, since any global function can be ran as one.
You can see examples in `ModifierCommands.gml`, which I made to edit modifier values from the console.
