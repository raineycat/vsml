#include "dotnet_launcher.hpp"
#include "config.hpp"
#include <cstdio>
#include <Windows.h>

#define NETHOST_USE_AS_STATIC
#include <nethost.h>
#include <hostfxr.h>
#include <coreclr_delegates.h>

namespace DotNet {
    static HANDLE mutex = NULL;
    static HMODULE library = NULL;
    static hostfxr_initialize_for_runtime_config_fn init_for_config_fptr = NULL;
    static hostfxr_get_runtime_delegate_fn get_delegate_fptr = NULL;
    static hostfxr_close_fn close_fptr = NULL;
}

bool DotNet::Init(void* arg) {
    if (DotNet::mutex != NULL) {
        return true;
    }

    DotNet::mutex = CreateMutexA(
        nullptr,
        false,
        "_VSMLDotNetInjectorMutex");

    if (DotNet::mutex == NULL) {
        return false;
    }

    // check if the mutex has already been created (already initialised)
    if (GetLastError() == ERROR_ALREADY_EXISTS) {
        MessageBoxA(NULL, "Mutex already exists", "E", MB_ICONINFORMATION);
        CloseHandle(DotNet::mutex);
        return true;
    }

    WCHAR* assemblyDir = nullptr;
    get_hostfxr_parameters params {
        sizeof(get_hostfxr_parameters),
        assemblyDir,
        nullptr
    };

    WCHAR hostfxrPath[MAX_PATH];
    size_t hostfxrPathLen = MAX_PATH;
    int result = get_hostfxr_path(hostfxrPath, &hostfxrPathLen, &params);

    if (result != 0) {
        MessageBoxA(nullptr,
                "Failed to find hostfxr.dll!\n\nTry making sure a .NET runtime is installed.",
                "DotNet Error",
                MB_ICONERROR);
        return false;
    }

    DotNet::library = LoadLibraryW(hostfxrPath);
    if (!DotNet::library) {
        MessageBoxA(nullptr,
                "Failed to load the .NET runtime!",
                "DotNet Error",
                MB_ICONERROR);
        return false;
    }

    DotNet::init_for_config_fptr = reinterpret_cast<hostfxr_initialize_for_runtime_config_fn>(
        GetProcAddress(DotNet::library, "hostfxr_initialize_for_runtime_config"));
    DotNet::get_delegate_fptr = reinterpret_cast<hostfxr_get_runtime_delegate_fn>(
        GetProcAddress(DotNet::library, "hostfxr_get_runtime_delegate"));
    DotNet::close_fptr = reinterpret_cast<hostfxr_close_fn>(
        GetProcAddress(DotNet::library, "hostfxr_close"));

    if (!(DotNet::init_for_config_fptr && DotNet::get_delegate_fptr && DotNet::close_fptr)) {
        MessageBoxA(nullptr,
                "Failed to get .NET runtime functions!",
                "DotNet Error",
                MB_ICONERROR);
        return false;
    }

    WCHAR configPath[MAX_PATH];
    GetCurrentDirectoryW(MAX_PATH, configPath);
    wcscat_s(configPath, VSML_RUNTIME_CONFIG);

    hostfxr_handle fxr = nullptr;
    result = DotNet::init_for_config_fptr(configPath, nullptr, &fxr);
    if (result != 0 || fxr == nullptr) {
        MessageBoxA(nullptr,
                "Failed to get a hostfxr handle!",
                "DotNet Error",
                MB_ICONERROR);
        return false;
    }

    load_assembly_fn loadAssemblyPtr = nullptr;
    result = DotNet::get_delegate_fptr(fxr, hdt_load_assembly, reinterpret_cast<void**>(&loadAssemblyPtr));
    if (result != 0 || loadAssemblyPtr == nullptr) {
        MessageBoxA(nullptr,
                "Failed to get hdt_load_assembly!",
                "DotNet Error",
                MB_ICONERROR);
        return false;
    }

    get_function_pointer_fn getFunctionPointerPtr = nullptr;
    result = DotNet::get_delegate_fptr(fxr, hdt_get_function_pointer, reinterpret_cast<void**>(&getFunctionPointerPtr));
    if (result != 0 || getFunctionPointerPtr == nullptr) {
        MessageBoxA(nullptr,
                "Failed to get hdt_get_function_pointer!",
                "DotNet Error",
                MB_ICONERROR);
        return false;
    }

    DotNet::close_fptr(fxr);

    WCHAR loaderPath[MAX_PATH];
    GetCurrentDirectoryW(MAX_PATH, loaderPath);
    wcscat_s(loaderPath, VSML_MANAGED_DLL);

    result = loadAssemblyPtr(loaderPath, nullptr, nullptr);
    if (result != 0) {
        MessageBoxA(nullptr,
                "Failed to load the managed DLL!",
                "DotNet Error",
                MB_ICONERROR);
        return false;
    }

    component_entry_point_fn entryPoint = nullptr;
    result = getFunctionPointerPtr(
        VSML_ENTRYPOINT_TYPE,
        VSML_ENTRYPOINT_METHOD,
        UNMANAGEDCALLERSONLY_METHOD,
        nullptr,
        nullptr,
        reinterpret_cast<void**>(&entryPoint));

    if (result != 0 || entryPoint == nullptr) {
        MessageBoxA(nullptr,
                "Failed to get the managed entry point!",
                "DotNet Error",
                MB_ICONERROR);
        return false;
    }

    result = entryPoint(arg, sizeof(void*));
    if (result != 0) {
        MessageBoxA(nullptr,
                "The managed loader encountered an error!",
                "DotNet Error",
                MB_ICONERROR);
        return false;
    }

    return true;
}

void DotNet::Cleanup() {
    if (DotNet::mutex == NULL) {
        return;
    }

    FreeLibrary(DotNet::library);
    DotNet::init_for_config_fptr = nullptr;
    DotNet::get_delegate_fptr = nullptr;
    DotNet::close_fptr = nullptr;

    CloseHandle(DotNet::mutex);
    DotNet::mutex = NULL;
}
