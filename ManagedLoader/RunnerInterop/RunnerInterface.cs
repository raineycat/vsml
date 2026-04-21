using System.Runtime.InteropServices;

namespace ManagedLoader.RunnerInterop;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct RunnerInterface
{
    #region User interaction
    public delegate* unmanaged<string, RuntimeArgumentHandle, void> DebugConsoleOutput;
    public delegate* unmanaged<string, RuntimeArgumentHandle, void> ReleaseConsoleOutput;
    public delegate* unmanaged<string, void> ShowMessage;
    public delegate* unmanaged<string, RuntimeArgumentHandle, void> YYError;
    #endregion
    
    #region Memory management
    public delegate* unmanaged<int, IntPtr> YYAlloc;
    public delegate* unmanaged<IntPtr, int, IntPtr> YYReAlloc;
    public delegate* unmanaged<IntPtr, void> YYFree;
    public delegate* unmanaged<string, char*> YYStrDup;
    #endregion
    
    #region Argument parsing
    public delegate* unmanaged<IntPtr, int, bool> YYGetBool;
    public delegate* unmanaged<IntPtr, int, float> YYGetFloat;
    public delegate* unmanaged<IntPtr, int, double> YYGetReal;
    public delegate* unmanaged<IntPtr, int, int> YYGetInt32;
    public delegate* unmanaged<IntPtr, int, uint> YYGetUInt32;
    public delegate* unmanaged<IntPtr, int, long> YYGetInt64;
    public delegate* unmanaged<IntPtr, int, IntPtr> YYGetPtr;
    public delegate* unmanaged<IntPtr, int, IntPtr> YYGetPtrOrInt;
    public delegate* unmanaged<IntPtr, int, string> YYGetString;
    #endregion

    #region RValue parsing
    public delegate* unmanaged<IntPtr, bool> BoolRValue;
    public delegate* unmanaged<IntPtr, double> RealRValue;
    public delegate* unmanaged<IntPtr, IntPtr> PtrRValue;
    public delegate* unmanaged<IntPtr, long> Int64RValue;
    public delegate* unmanaged<IntPtr, int> Int32RValue;
    #endregion

    #region Hashing
    public delegate* unmanaged<IntPtr, int> HashRValue;
    #endregion

    #region RValue operations
    public delegate* unmanaged<IntPtr, IntPtr, IntPtr, int, void> SetRValue;
    public delegate* unmanaged<IntPtr, IntPtr, IntPtr, int, bool, bool, void> GetRValue;
    public delegate* unmanaged<IntPtr, IntPtr, void> CopyRValue;
    public delegate* unmanaged<IntPtr, int> KindRValue;
    public delegate* unmanaged<IntPtr, void> FreeRValue;
    
    public delegate* unmanaged<IntPtr, string, void> YYCreateString;
    public delegate* unmanaged<IntPtr, int, double*, void> YYCreateArray;
    
    public delegate* unmanaged<string, int> ScriptFindId;
    public delegate* unmanaged<int, IntPtr, IntPtr, int, IntPtr, IntPtr, bool> ScriptPerform;
    public delegate* unmanaged<string, int*, bool> CodeFunctionFind;
    #endregion

    #region HTTP requests
    public delegate* unmanaged<string, IntPtr, IntPtr, IntPtr, void> HTTPGet;
    public delegate* unmanaged<string, string, IntPtr, IntPtr, IntPtr, void> HTTPPost;
    public delegate* unmanaged<string, string, string, string, IntPtr, IntPtr, IntPtr, int, void> HTTPRequest;
    #endregion

    #region Sprites
    public delegate* unmanaged<IntPtr, IntPtr, int*, int> AsyncFuncSpriteAdd;
    public delegate* unmanaged<IntPtr, void> AsyncFuncSpriteCleanup;
    public delegate* unmanaged<int*, int, int, int, int, IntPtr> CreateSpriteAsync;
    #endregion

    #region Timing
    public delegate* unmanaged<long> TimingTime;
    public delegate* unmanaged<long, bool, void> TimingSleep;
    #endregion

    #region Mutexes
    public delegate* unmanaged<string, IntPtr> YYMutexCreate;
    public delegate* unmanaged<IntPtr, void> YYMutexDestroy;
    public delegate* unmanaged<IntPtr, void> YYMutexLock;
    public delegate* unmanaged<IntPtr, void> YYMutexUnlock;
    #endregion

    #region Async events
    public delegate* unmanaged<int, int, void> CreateAsyncEventWithDSMap;
    public delegate* unmanaged<int, int, int, void> CreateAsyncEventWithDSMapAndBuffer;
    #endregion

    #region DS Map Manipulation
    public delegate* unmanaged<int, RuntimeArgumentHandle, int> CreateDsMap;
    public delegate* unmanaged<int, string, double, bool> DsMapAddDouble;
    public delegate* unmanaged<int, string, string, bool> DsMapAddString;
    public delegate* unmanaged<int, string, long, bool> DsMapAddInt64;
    #endregion

    #region Buffer access
    public delegate* unmanaged<int, void**, int*, bool> BufferGetContent;
    public delegate* unmanaged<int, int, void*, int, bool, bool, int> BufferWriteContent;
    public delegate* unmanaged<int, int, int, int> CreateBuffer;
    #endregion

    #region Variables
    public bool* pLiveConnection;
    public int* pHttpId;
    #endregion

    #region DS List and DS Map Manipulation
    public delegate* unmanaged<int> DsListCreate;
    public delegate* unmanaged<int, string, int, void> DsMapAddList;
    public delegate* unmanaged<int, int, void> DsListAddMap;
    public delegate* unmanaged<int, void> DsMapClear;
    public delegate* unmanaged<int, void> DsListClear;
    #endregion

    #region Files
    public delegate* unmanaged<string, bool> BundleFileExists;
    public delegate* unmanaged<char*, int, string, bool> BundleFileName;
    public delegate* unmanaged<string, bool> SaveFileExists;
    public delegate* unmanaged<char*, int, string, bool> SaveFileName;
    #endregion

    #region Base64
    public delegate* unmanaged<void*, ulong, void*, ulong> Base64Encode;
    #endregion

    #region DS List Manipulation
    public delegate* unmanaged<int, long, void> DsListAddInt64;
    #endregion

    #region File/directory whitelisting
    public delegate* unmanaged<string, void> AddDirectoryToBundleWhitelist;
    public delegate* unmanaged<string, void> AddFileToBundleWhitelist;
    public delegate* unmanaged<string, void> AddDirectoryToSaveWhitelist;
    public delegate* unmanaged<string, void> AddFileToSaveWhitelist;
    #endregion

    #region Utilities
    public delegate* unmanaged<IntPtr, string> KindNameRValue;
    #endregion

    #region DS Map Manipulation (again)
    public delegate* unmanaged<int, string, bool, void> DsMapAddBool;
    public delegate* unmanaged<int, string, IntPtr, void> DsMapAddRValue;
    public delegate* unmanaged<int, void> DestroyDsMap;
    #endregion

    #region Struct Manipulation
    public delegate* unmanaged<IntPtr, void> StructCreate;
    public delegate* unmanaged<IntPtr, string, bool, void> StructAddBool;
    public delegate* unmanaged<IntPtr, string, double, void> StructAddDouble;
    public delegate* unmanaged<IntPtr, string, int, void> StructAddInt;
    public delegate* unmanaged<IntPtr, string, IntPtr, void> StructAddRValue;
    public delegate* unmanaged<IntPtr, string, string, void> StructAddString;
    #endregion

    #region Directory Manipulation
    public delegate* unmanaged<string, bool> WhitelistIsDirectoryIn;
    public delegate* unmanaged<string, bool> WhitelistIsFilenameIn;
    public delegate* unmanaged<string, bool, void> WhitelistAddTo;
    public delegate* unmanaged<string, bool> DirExists;
    #endregion

    #region Advanced buffer access
    public delegate* unmanaged<int, IntPtr> BufferGetFromGML;
    public delegate* unmanaged<IntPtr, int> BufferTell;
    public delegate* unmanaged<IntPtr, IntPtr> BufferGet;
    public delegate* unmanaged<string> FilePrePend;
    #endregion
    
    #region Struct Manipulation (again)
    public delegate* unmanaged<IntPtr, string, int, void> StructAddInt32;
    public delegate* unmanaged<IntPtr, string, long, void> StructAddInt64;
    public delegate* unmanaged<IntPtr, string, IntPtr> StructGetMember;
    public delegate* unmanaged<IntPtr, char**, int*, int> StructGetKeys;
    public delegate* unmanaged<IntPtr, int, IntPtr> YYGetStruct;
    #endregion

    #region Extension options
    public delegate* unmanaged<IntPtr, string, string, void> ExtOptGetRValue;
    public delegate* unmanaged<string, string, string> ExtOptGetString;
    public delegate* unmanaged<string, string, double> ExtOptGetReal;
    #endregion

    #region Utilities (again)
    public delegate* unmanaged<bool> IsRunningFromIDE;
    public delegate* unmanaged<IntPtr, int> YYArrayGetLength;
    #endregion

    #region £xtensions
    public delegate* unmanaged<string, string> ExtGetVersion;
    #endregion
}