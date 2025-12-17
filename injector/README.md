# VSML-Injector

## About
This is a simple DLL search path based injector for VSML. 
It proxies `version.dll` and forwards all calls through to it.

The DLL hooks the `CreateFileW` function from WinAPI, and looks for two filenames.
- If it sees `data.win`, it will replace the filename with `shadow.win`, and also initialise the .NET host if it hasn't already done so.
- If it sees `shadow.win`, it will replace the filename with `data.win`.

This allows us to patch the game's data file while leaving the original one intact and in the same place, as well as providing an early enough injection point to modify the data before it gets loaded.

## Dependencies
The DLL uses libnethost to find the system's .NET runtime, so one (>=9.0) needs to be installed for this to work.

MinHook is also used for the purposes specified above, however this is linked statically, so everything is contained within the proxy DLL.

> TODO: Git submodule / meson wrap so we don't have binaries in the repo?

## Usage
### Users
If you're using this as part of VSML, just add it to the game's root directory and make sure the mod loader is installed in the correct directory.

### Developers
If you want to use this for your own purposes, you should modify the definitions in `config.hpp`.

You should modify `VSML_MANAGED_DLL` and `VSML_RUNTIME_CONFIG` to point to your entrypoint DLL and its runtime config file respectively. These are appended to the current working directory, and must have a path separator before them.

> Note: by default, runtime config files are not generated for .NET class libraries (DLLs), but you can enable this by adding `<GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>` to your project file.

You should then set `VSML_ENTRYPOINT_TYPE` to the name of your entrypoint class in the format `Namespace.TypeName, AssemblyName`, and set `VSML_ENTRYPOINT_METHOD` to the actual method name.

Your entrypoint must have the following definition:
```cs
[UnmanagedCallersOnly]
public static int MyEntrypointFunction(IntPtr argument, int argumentSizeInBytes);
```

Currently, `argument` and its size will both be zero. However, you can pass a pointer here from native code in the `DotNet::Init` function.