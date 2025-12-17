#pragma once

#include <Windows.h>
#include <cassert>

inline HMODULE LoadOriginalLib() {
    static HMODULE lib = nullptr;

    if (lib == nullptr) {
        char libPath[MAX_PATH];
        GetSystemDirectoryA(libPath, MAX_PATH);
        strcat_s(libPath, "\\version.dll");

        lib = LoadLibraryA(libPath);
        assert(lib != nullptr);
    }
    return lib;
}

template <typename TRet, typename ...TArgs>
TRet ForwardFunction(const char* name, TArgs&... args) {
    using TFunc = TRet(*)(TArgs...);

    FARPROC proc = GetProcAddress(LoadOriginalLib(), name);
    assert(proc != nullptr);

    auto f = reinterpret_cast<TFunc>(proc);
    return f(args...);
}
