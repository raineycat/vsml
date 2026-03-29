# VSML Managed Loader

## About
This is the actual mod loader, written in C#, that handles loading mod files and patching the game's data file.

This is different to traditional modding frameworks because your mod code and the game's code never run at the same time.
Your mod code only runs once, before the game loads, and after that it never runs again. 
However, the .NET environment does stay alive through the whole lifetime of the game.

## Usage
- Put the injector DLL into the game folder
- Add `ManagedLoader.dll` and its dependencies into the `VSML/Core` folder
- Add any mod DLLs into the `VSML/Mods` folder too
- Run the game
- Edit the config file (`VSML/config.json`) if needed
