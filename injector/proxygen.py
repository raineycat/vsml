#!/usr/bin/env python3

from sys import argv

if len(argv) < 3:
    print("USAGE: proxygen.py <output.hpp> <output.def>")
    exit(1)

AUTO_GEN_HEADER = "I'M AUTO GENERATED - DON'T EDIT OR COMMIT ME"
PROXY_PREFIX = "_ProxyFnImpl_"

class ProxyFunc:
    def __init__(self, name: str, ret_type: str, args_str: str):
        self.name = name
        self.ret_type = ret_type

        self.args_string = args_str
        args = [x.split(" ") for x in args_str.split(", ")]

        self.arg_types = [a for a, b in args]
        self.arg_names = [b for a, b in args]
    
    def write_forwarder(self) -> str:
        return f"""{self.ret_type} {PROXY_PREFIX}{self.name}({self.args_string}) {{
            #ifdef HOOK_CB_{self.name}
                HOOK_CB_{self.name}();
            #endif
            return ForwardFunction<{self.ret_type}, {",".join(self.arg_types)}>("{self.name}", {",".join(self.arg_names)});
        }}\n\n"""

FUNCTIONS = [
    ProxyFunc("GetFileVersionInfoA", "BOOL", "LPCSTR lptstrFilename, DWORD dwHandle, DWORD dwLen, LPVOID lpData"),
    ProxyFunc("GetFileVersionInfoExA", "BOOL", "DWORD dwFlags, LPCSTR lptstrFilename, DWORD dwHandle, DWORD dwLen, LPVOID lpData"),
    ProxyFunc("GetFileVersionInfoSizeA", "DWORD", "LPCSTR lptstrFilename, LPDWORD lpdwHandle"),
    ProxyFunc("GetFileVersionInfoSizeExA", "DWORD", "DWORD dwFlags, LPCSTR lptstrFilename, LPDWORD lpdwHandle"),
    ProxyFunc("GetFileVersionInfoW", "BOOL", "LPCWSTR lpwstrFilename, DWORD dwHandle, DWORD dwLen, LPVOID lpData"),
    ProxyFunc("GetFileVersionInfoExW", "BOOL", "DWORD dwFlags, LPCWSTR lpwstrFilename, DWORD dwHandle, DWORD dwLen, LPVOID lpData"),
    ProxyFunc("GetFileVersionInfoSizeW", "DWORD", "LPCWSTR lpwstrFilename, LPDWORD lpdwHandle"),
    ProxyFunc("GetFileVersionInfoSizeExW", "DWORD", "DWORD dwFlags, LPCWSTR lpwstrFilename, LPDWORD lpdwHandle"),
    ProxyFunc("GetFileVersionInfoByHandle", "BOOL", "DWORD dwFlags, HANDLE hFile, LPVOID* lplpData, PDWORD pdwLen"),
    ProxyFunc("VerFindFileA", "DWORD", "DWORD uFlags, LPCSTR szFileName, LPCSTR szWinDir, LPCSTR szAppDir, LPSTR szCurDir, PUINT puCurDirLen, LPSTR szDestDir, PUINT puDestDirLen"),
    ProxyFunc("VerFindFileW", "DWORD", "DWORD uFlags, LPCWSTR szFileName, LPCWSTR szWinDir, LPCWSTR szAppDir, LPWSTR szCurDir, PUINT puCurDirLen, LPWSTR szDestDir, PUINT puDestDirLen"),
    ProxyFunc("VerInstallFileA", "DWORD", "DWORD uFlags, LPCSTR szSrcFileName, LPCSTR szDestFileName, LPCSTR szSrcDir, LPCSTR szDestDir, LPCSTR szCurDir, LPSTR szTmpFile, PUINT puTmpFileLen"),
    ProxyFunc("VerInstallFileW", "DWORD", "DWORD uFlags, LPCWSTR szSrcFileName, LPCWSTR szDestFileName, LPCWSTR szSrcDir, LPCWSTR szDestDir, LPCWSTR szCurDir, LPWSTR szTmpFile, PUINT puTmpFileLen"),
    ProxyFunc("VerLanguageNameA", "DWORD", "DWORD wLang, LPSTR szLang, DWORD cchLang"),
    ProxyFunc("VerLanguageNameW", "DWORD", "DWORD wLang, LPWSTR szLang, DWORD cchLang"),
    ProxyFunc("VerQueryValueA", "BOOL", "LPCVOID pBlock, LPCSTR lpSubBlock, LPVOID* lplpBuffer, PUINT puLen"),
    ProxyFunc("VerQueryValueW", "BOOL", "LPCVOID pBlock, LPCWSTR lpSubBlock, LPVOID* lplpBuffer, PUINT puLen")
]

with open(argv[1], "w") as f:
    f.write(f"/// {AUTO_GEN_HEADER} ///\n\n")
    f.write("#pragma once\n\n")
    for func in FUNCTIONS:
        f.write(func.write_forwarder())

with open(argv[2], "w") as f:
    f.write(f";;; {AUTO_GEN_HEADER} ;;;\n\n")
    f.write("LIBRARY version\n\n")
    f.write("EXPORTS\n")
    for func in FUNCTIONS:
        f.write(f"\t{func.name}={PROXY_PREFIX}{func.name}\n")