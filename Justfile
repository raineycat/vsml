build_cfg := "Debug"
dist_dir := "vsml-dist"

git_branch := shell("git rev-parse --abbrev-ref HEAD")
git_commit := shell("git rev-parse --short HEAD")

cargo_args := if build_cfg == "Release" { "--release" } else { "" }
injector_bin := if os() == "linux" { "Injector" / "target" / "x86_64-pc-windows-msvc" / "release" } else { "Injector" / "target" / "release" }

dotnet_args := if os() == "linux" { "-p:EnableWindowsTargeting=true" } else { "" }
loader_bin := "ManagedLoader" / "bin" / build_cfg / "net9.0"
loadscreen_bin := "LoadingWindow" / "bin" / build_cfg / "net9.0-windows" / "win-x64"
chart_mod_bin := "CustomChartLoader" / "bin" / build_cfg / "net9.0"
debug_mod_bin := "DebuggingMod" / "bin" / build_cfg / "net9.0"

default:
    @echo "[ VSML - {{git_branch}}/{{git_commit}} ]"
    @just --list

[windows]
make-zip: make-dist
    tar -a -cf vsml.zip {{dist_dir}}
    rm -rf {{dist_dir}}

[linux]
make-zip: make-dist
    zip -r vsml {{dist_dir}}
    rm -rf {{dist_dir}}

make-dist: build-all
    # clean the dir if it exists
    rm -rf {{dist_dir}}

    # set up directory structure
    mkdir {{dist_dir}}
    mkdir {{dist_dir}}/Mods
    mkdir {{dist_dir}}/CustomCharts
    mkdir {{dist_dir}}/DebugScripts

    # write version file
    echo "{{git_branch}}/{{git_commit}}" > {{dist_dir}}/vsml.ver

    # copy injector
    cp {{injector_bin}}/version.dll {{dist_dir}}/

    # copy loader and dependencies
    cp {{loader_bin}}/ManagedLoader.{dll,runtimeconfig.json} {{dist_dir}}/Mods/
    cp {{loader_bin}}/ModContract.dll {{dist_dir}}/Mods/
    cp {{loader_bin}}/Serilog.dll {{dist_dir}}/Mods/
    cp {{loader_bin}}/Serilog.Sinks.File.dll {{dist_dir}}/Mods/
    cp {{loader_bin}}/UndertaleModLib.dll {{dist_dir}}/Mods/
    cp {{loader_bin}}/Underanalyzer.dll {{dist_dir}}/Mods/
    cp {{loader_bin}}/K4os.Hash.xxHash.dll {{dist_dir}}/Mods/

    # copy loading screen app
    cp {{loadscreen_bin}}/LoadingWindow.{exe,dll,runtimeconfig.json} {{dist_dir}}/Mods/

    # copy built in mods
    cp {{chart_mod_bin}}/CustomChartLoader.dll {{dist_dir}}/Mods/    
    cp {{debug_mod_bin}}/DebuggingMod.dll {{dist_dir}}/Mods/  
    
    # copy other assets
    cp ModifierCommands.gml {{dist_dir}}/DebugScripts/  

build-all: build-injector build-loader
clean-all: clean-injector clean-loader

[working-directory: 'Injector']
[windows]
build-injector:
    cargo build {{cargo_args}}

[working-directory: 'Injector']
[linux]
build-injector:
    cargo build-cross {{cargo_args}}

[working-directory: 'Injector']
clean-injector:
    cargo clean

build-loader:
    dotnet build -c:{{build_cfg}} {{dotnet_args}}

clean-loader:
    rm -rf */bin
    rm -rf */obj
