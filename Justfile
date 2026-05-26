build_cfg := "Debug"
dist_dir := "vsml-dist"
set windows-shell := ["C:\\Program Files\\Git\\bin\\sh.exe", "-c"]

git_branch := shell("git rev-parse --abbrev-ref HEAD")
git_commit := shell("git rev-parse --short HEAD")

cargo_args := if build_cfg == "Release" { "--release" } else { "" }
injector_bin := if os() == "linux" { "Injector" / "target" / "x86_64-pc-windows-msvc" } else { "Injector" / "target" }

dotnet_args := if os() == "linux" { "-p:EnableWindowsTargeting=true" } else { "" }
loader_bin := "ManagedLoader" / "bin" / build_cfg / "net10.0"
loadscreen_bin := "LoadingWindow" / "bin" / build_cfg / "net10.0-windows" / "win-x64"
chart_mod_bin := "CustomChartLoader" / "bin" / build_cfg / "net10.0"
debug_mod_bin := "DebuggingMod" / "bin" / build_cfg / "net10.0"

default:
    @echo "[ VSML - {{git_branch}}/{{git_commit}} ]"
    @just --list

fetch-submodules:
    git submodule update --init --recursive

[windows]
make-zip: make-dist
    tar -a -cf vsml.zip {{dist_dir}}
    rm -rf {{dist_dir}}

[linux]
make-zip: make-dist
    zip -r vsml {{dist_dir}}
    rm -rf {{dist_dir}}

make-dist: build-installer make-dev

make-dev: build-runtime
    # clean the dir if it exists
    # rm -rf {{dist_dir}}

    # set up directory structure
    mkdir -p {{dist_dir}}
    mkdir -p {{dist_dir}}/VSML
    mkdir -p {{dist_dir}}/VSML/Core
    mkdir -p {{dist_dir}}/VSML/Logs
    mkdir -p {{dist_dir}}/VSML/Mods
    mkdir -p {{dist_dir}}/VSML/CustomCharts
    mkdir -p {{dist_dir}}/VSML/DebugScripts

    # write version file
    echo "{{git_branch}}/{{git_commit}}" > {{dist_dir}}/VSML/version.txt

    # copy injector
    cp {{injector_bin}}/{{lowercase(build_cfg)}}/version.dll {{dist_dir}}/

    # copy loader and dependencies
    cp {{loader_bin}}/ManagedLoader.{dll,pdb,runtimeconfig.json} {{dist_dir}}/VSML/Core/
    cp {{loader_bin}}/ModContract.dll {{dist_dir}}/VSML/Core/
    cp {{loader_bin}}/Serilog.dll {{dist_dir}}/VSML/Core/
    cp {{loader_bin}}/Serilog.Sinks.Console.dll {{dist_dir}}/VSML/Core/
    cp {{loader_bin}}/Serilog.Sinks.File.dll {{dist_dir}}/VSML/Core/
    cp {{loader_bin}}/UndertaleModLib.dll {{dist_dir}}/VSML/Core/
    cp {{loader_bin}}/Underanalyzer.dll {{dist_dir}}/VSML/Core/
    cp {{loader_bin}}/K4os.Hash.xxHash.dll {{dist_dir}}/VSML/Core/

    # copy loading screen app
    cp {{loadscreen_bin}}/LoadingWindow.{exe,dll,runtimeconfig.json} {{dist_dir}}/VSML/Core/

    # copy built in mods
    # FIXME: chart mod is broken (and probably should get remade anyway) so i'm removing it for now
    # cp {{chart_mod_bin}}/CustomChartLoader.dll {{dist_dir}}/VSML/Mods/    
    cp {{debug_mod_bin}}/DebuggingMod.dll {{dist_dir}}/VSML/Mods/  
    
    # copy other assets
    cp ModifierCommands.gml {{dist_dir}}/VSML/DebugScripts/  

build-all: build-runtime build-installer
build-runtime: build-injector build-loader
clean-all: clean-injector clean-loader clean-installer

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

[working-directory: 'Installer']
build-installer:
    cargo build {{cargo_args}}

[working-directory: 'Installer']
clean-installer:
    cargo clean
