build_cfg := "Debug"
meson_build_dir := ".buildDir"
dist_dir := "vsml-dist"

git_branch := shell("git rev-parse --abbrev-ref HEAD")
git_commit := shell("git rev-parse --short HEAD")

injector_bin := "injector" / meson_build_dir
loader_bin := "ManagedLoader" / "bin" / build_cfg / "net9.0"
loadscreen_bin := "LoadingWindow" / "bin" / build_cfg / "net9.0-windows"
chart_mod_bin := "CustomChartLoader" / "bin" / build_cfg / "net9.0"

default:
    @echo "[ VSML - {{git_branch}}/{{git_commit}} ]"
    @just --list

package: build-all
    # clean the dir if it exists
    rm -rf {{dist_dir}}

    # set up directory structure
    mkdir {{dist_dir}}
    mkdir {{dist_dir}}/Mods
    mkdir {{dist_dir}}/CustomCharts

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
    cp {{loadscreen_bin}}/LoadingWindow.exe {{dist_dir}}/Mods/
    cp {{loadscreen_bin}}/LoadingWindow.dll {{dist_dir}}/Mods/
    cp {{loadscreen_bin}}/LoadingWindow.runtimeconfig.json {{dist_dir}}/Mods/

    # copy chart mod
    cp {{chart_mod_bin}}/CustomChartLoader.dll {{dist_dir}}/Mods/

    # make zip file
    tar -a -cf vsml.zip {{dist_dir}}

    # clean staging dir
    rm -rf {{dist_dir}}

build-all: build-injector build-loader
clean-all: clean-injector clean-loader

[working-directory: 'injector']
build-injector:
    meson setup {{meson_build_dir}} --buildtype={{lowercase(build_cfg)}}
    meson compile -C {{meson_build_dir}}

[working-directory: 'injector']
clean-injector:
    rm -rf {{meson_build_dir}}

build-loader:
    dotnet build -c:{{build_cfg}}

clean-loader:
    rm -rf */bin
    rm -rf */obj
