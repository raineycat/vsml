# VSML - A mod loader for vivid/stasis

### Individual component documentation
- [Installer](Installer/README.md)
- [Injector](Injector/README.md)
- [Core modloader](ManagedLoader/README.md)
- [Mod contract](ModContract/README.md) - **Read if you want to make mods**
- [Chart loader mod](CustomChartLoader/README.md) - **Read if you want to make charts**
- [Script loader mod](DebuggingMod/README.md)

### Sister projects
- [charted](https://github.com/raineycat/charted)


## Features
- Loads mods written in C#
- Enables the [GameMaker debug overlay](https://manual.gamemaker.io/monthly/en/GameMaker_Language/GML_Reference/Debugging/The_Debug_Overlay.htm)
- Comes with custom chart support
- Comes with a GML script injector for debugging
- Caches installed mods and data for faster loading


## Goals
- Add utilities to make hooking code and adding assets easier
- Maintain accurate documentation on things like file formats and charting


## Installation

Download the latest release as a zip file, and extract it into the game folder, such that `version.dll` is in the same place as `vividstasis.exe`.

Alternatively, use the installer (also in the latest release).

> [!NOTE]
> 
> If you're running the game through Proton on Linux, you need to do a couple extra steps.
> 
> First, install the correct redists in your Wine prefix:
> ```shell
> protontricks 2093940 --force vcrun2022
> protontricks 2093940 dotnetdesktop9
> ```
> 
> Go into the game in your Steam library, open the properties dialog,  and set the launch options to:
> ```shell
> WINEDLLOVERRIDES="version=n,b" %command%
> ```


## Building

To build VSML from source, you need:
- [.NET 9](https://versionsof.net/core/9.0/9.0.9/)
- [Rust + Cargo](https://rustup.rs/)
- [Just](https://github.com/casey/just)

If you're building on Linux, you also need:
- [cargo-xwin](https://github.com/rust-cross/cargo-xwin)
- The `x86_64-pc-windows-msvc` Rust target

> [!IMPORTANT]
> 
> Make sure you cloned this repo recursively, otherwise dependencies will be missing.
> 
> If you didn't do this, then you can run:
> ```shell
> just fetch-submodules
> ```
> or
> ```shell
> git submodule update --init --recursive
> ```

VSML uses the Just command runner to make building easier. The most useful recipies are:
- `just make-zip` - Builds a ZIP file ready to be installed
- `just make-dist` - Builds everything and puts it into the correct folder structure for installation
- `just build-installer` - Builds the installer (as this is not included in the `make-*` recipies)
- `just clean-all` - Removes all generated build artifacts

The recipes will automatically handle cross compiling from Linux to Windows if needed.

To make a release build, use `just --set build_cfg Release [recipe]`. 
