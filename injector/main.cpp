#include <Windows.h>
#include <MinHook.h>
#include <cstdio>
#include "dotnet_launcher.hpp"
#include <cassert>

typedef HANDLE(*CreateFileWPtr)(LPCWSTR, DWORD, DWORD, LPSECURITY_ATTRIBUTES, DWORD, DWORD, HANDLE);
CreateFileWPtr createFileW;
CreateFileWPtr originalCreateFileW;

HANDLE HookedCreateFileW(
    LPCWSTR lpFileName,
    DWORD dwAccess,
    DWORD dwShare,
    LPSECURITY_ATTRIBUTES lpSecurityAttrs,
    DWORD dwCreationDisposition,
    DWORD dwFlagsAndAttributes,
    HANDLE hTemplateFile) {
    WCHAR actualPath[MAX_PATH];
    wcscpy_s(actualPath, lpFileName);

    WCHAR* dataWinPtr = wcsstr(actualPath, L"data.win");
    if (dataWinPtr != nullptr) {
        *dataWinPtr = 0;
        wcscat_s(actualPath, L"shadow.win");

        if (!DotNet::Init(nullptr)) {
            ExitProcess(1);
        }
    }

    WCHAR* shadowWinPtr = wcsstr(actualPath, L"shadow.win");
    if (dataWinPtr == nullptr && shadowWinPtr != nullptr) {
        *shadowWinPtr = 0;
        wcscat_s(actualPath, L"data.win");
    }

    return originalCreateFileW(
        actualPath,
        dwAccess,
        dwShare,
        lpSecurityAttrs,
        dwCreationDisposition,
        dwFlagsAndAttributes,
        hTemplateFile);
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved) {
    if (fdwReason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hinstDLL);

        assert(MH_Initialize() == MH_OK);

        HINSTANCE hK32 = LoadLibraryA("kernel32.dll");
        assert(hK32 != nullptr);

        createFileW = reinterpret_cast<CreateFileWPtr>(GetProcAddress(hK32, "CreateFileW"));
        assert(createFileW != nullptr);

        assert(MH_CreateHook(
            reinterpret_cast<void*>(createFileW),
            reinterpret_cast<void*>(HookedCreateFileW),
            reinterpret_cast<void**>(&originalCreateFileW)) == MH_OK);
        assert(MH_EnableHook(reinterpret_cast<void*>(createFileW)) == MH_OK);

        FreeLibrary(hK32);
    }
    else if (fdwReason == DLL_PROCESS_DETACH) {
        DotNet::Cleanup();
        assert(MH_Uninitialize() == MH_OK);
    }

    return TRUE;
}