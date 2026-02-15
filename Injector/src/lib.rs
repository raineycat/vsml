use ftail::Ftail;
use lazy_static::lazy_static;
use libloading::{Library, Symbol};
use log::LevelFilter;
use netcorehost::bindings::hostfxr::{hostfxr_delegate_type, load_assembly_fn};
use netcorehost::error::HostingResult;
use netcorehost::hostfxr::{HostfxrContext, InitializedForRuntimeConfig};
use netcorehost::pdcstring::PdCStr;
use netcorehost::{nethost, pdcstr};
use retour::GenericDetour;
use std::cell::OnceCell;
use std::ffi::c_void;
use std::path::PathBuf;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Arc, Mutex};
use std::{env, mem, ptr};
use windows::Win32::Foundation::HINSTANCE;
use windows::Win32::System::SystemServices::{DLL_PROCESS_ATTACH, DLL_PROCESS_DETACH};

mod proxy;

lazy_static! {
    static ref DOTNET_INIT: AtomicBool = AtomicBool::new(false);
    static ref KERNEL32: Library = load_kernel32();
    static ref CREATE_FILE_W_HOOK: GenericDetour<CreateFileWFn> = create_hook();
    static ref HOSTFXR_CTX: Arc<Mutex<OnceCell<HostfxrContext<InitializedForRuntimeConfig>>>> =
        Arc::new(Mutex::new(OnceCell::new()));
}

#[unsafe(no_mangle)]
pub extern "system" fn DllMain(
    _dll_instance: HINSTANCE,
    reason_for_call: u32,
    _reserved: *const c_void,
) -> i32 {
    match reason_for_call {
        DLL_PROCESS_ATTACH => init_dll(),
        DLL_PROCESS_DETACH => cleanup_dll(),
        _ => Ok(()),
    }
    .inspect_err(|e| log::error!("DLL handler error: {:?}", e))
    .unwrap();

    1
}

fn init_dll() -> anyhow::Result<()> {
    let log_file = PathBuf::from("injector.log");
    let log_level = if cfg!(debug_assertions) {
        LevelFilter::Debug
    } else {
        LevelFilter::Warn
    };
    Ftail::new()
        .single_file(&log_file, false, log_level)
        .init()?;

    log::info!("DLL init!");

    unsafe { CREATE_FILE_W_HOOK.enable() }?;
    Ok(())
}

fn cleanup_dll() -> anyhow::Result<()> {
    log::info!("DLL cleanup");

    let _ctx = HOSTFXR_CTX
        .lock()
        .map_err(|_| anyhow::format_err!("Failed to lock HOSTFXR_CTX for cleanup"))?;

    unsafe { CREATE_FILE_W_HOOK.disable() }?;

    Ok(())
}

fn load_kernel32() -> Library {
    match unsafe { Library::new("kernel32.dll") } {
        Ok(l) => l,
        Err(e) => {
            log::error!("Failed to load kernel32 for hooking! {:?}", e);
            panic!("kernel32.dll didn't load!");
        }
    }
}

fn create_hook() -> GenericDetour<CreateFileWFn> {
    unsafe {
        let sym: Symbol<CreateFileWFn> = KERNEL32.get("CreateFileW").unwrap();
        let func: CreateFileWFn = mem::transmute(sym.try_as_raw_ptr().unwrap());
        GenericDetour::new(func, create_file_w_hook).unwrap()
    }
}

type CreateFileWFn = extern "system" fn(
    *const libc::wchar_t,
    u32,
    u32,
    *const c_void,
    u32,
    u32,
    *const c_void,
) -> *const c_void;

extern "system" fn create_file_w_hook(
    filename: *const libc::wchar_t,
    access: u32,
    share: u32,
    security_attrs: *const c_void,
    disposition: u32,
    flags_and_attrs: u32,
    template: *const c_void,
) -> *const c_void {
    log::debug!(
        "CreateFileW({:?}, {}, {}, {:?}, {}, {}, {:?})",
        filename,
        access,
        share,
        security_attrs,
        disposition,
        flags_and_attrs,
        template
    );

    let path_slice = unsafe {
        let path_len = libc::wcslen(filename);
        std::slice::from_raw_parts(filename, path_len)
    };

    let mut str_path = match String::from_utf16(path_slice) {
        Ok(p) => PathBuf::from(p),
        Err(e) => {
            log::error!("Invalid UTF-16 filename in hook! {:?}", e);
            return ptr::null();
        }
    };

    log::debug!("Landed in hook function for: {:?}", str_path);

    if let Some(name) = str_path.file_name().and_then(|s| s.to_str()) {
        match name {
            "data.win" => {
                match try_init_dotnet() {
                    Ok(_) => log::info!("Finished .NET init"),
                    Err(e) => log::error!(".NET init failed! {:#?}", e),
                };

                log::info!("Swizzling data.win access");
                str_path.set_file_name("shadow.win");
            }
            "shadow.win" => {
                log::info!("Swizzling shadow.win access");
                str_path.set_file_name("data.win");
            }
            _ => {}
        }
    }

    let mut new_filename: Vec<u16> = str_path
        .to_str()
        .map(|s| s.encode_utf16().collect())
        .unwrap();
    new_filename.push(0);

    log::debug!("New path: {:?}", String::from_utf16(&new_filename));

    let result = CREATE_FILE_W_HOOK.call(
        new_filename.as_ptr(),
        access,
        share,
        security_attrs,
        disposition,
        flags_and_attrs,
        template,
    );

    log::debug!("Result: {:?}", result);
    result
}

fn try_init_dotnet() -> anyhow::Result<()> {
    if DOTNET_INIT.load(Ordering::Relaxed) {
        return Ok(());
    }

    DOTNET_INIT.store(true, Ordering::Relaxed);

    let ctx = HOSTFXR_CTX
        .try_lock() // don't block here in case of deadlocks
        // (they would happen previously; not sure if it's still an issue)
        .map_err(|_| anyhow::format_err!("Failed to lock HOSTFXR_CTX for init checks"))?;

    if ctx.get().is_some() {
        return Ok(());
    }

    log::info!("Starting hostfxr init");

    let hostfxr = nethost::load_hostfxr()?;
    log::debug!("Found runtime: {:?}", hostfxr.get_dotnet_exe());

    let managed_dir = env::current_dir()?.join("Mods");

    let config_path: Vec<u16> = managed_dir
        .join("ManagedLoader.runtimeconfig.json")
        .to_str()
        .ok_or(anyhow::format_err!("Failed to convert config_path"))?
        .encode_utf16()
        .chain(std::iter::once(0))
        .collect();
    let context =
        hostfxr.initialize_for_runtime_config(PdCStr::from_slice_with_nul(&config_path)?)?;
    log::debug!("Created context with runtimeconfig");

    let loader = context.get_delegate_loader()?;
    let load_asm: load_assembly_fn = context
        .get_runtime_delegate(hostfxr_delegate_type::hdt_load_assembly)
        .map(|p| unsafe { mem::transmute(p) })?;
    log::debug!("Got hdt_load_assembly: {:?}", load_asm);

    // we have to do it this way so it's not stuck in an isolated load context
    let dll_path: Vec<u16> = managed_dir
        .join("ManagedLoader.dll")
        .to_str()
        .ok_or(anyhow::format_err!("Failed to convert config_path"))?
        .encode_utf16()
        .chain(std::iter::once(0))
        .collect();
    HostingResult::from_status_code(
        unsafe { load_asm(dll_path.as_ptr(), ptr::null(), ptr::null()) } as u32,
    )
    .0?;
    log::debug!("Loaded assembly from disk");

    type ManagedEntryPoint = fn(*const c_void, i32) -> i32;
    let entry_point = loader.get_function_with_unmanaged_callers_only::<ManagedEntryPoint>(
        pdcstr!("ManagedLoader.NativeEntryPoint, ManagedLoader"),
        pdcstr!("LoaderMain"),
    )?;

    log::info!("Finished loading managed code");

    let result = entry_point(ptr::null(), 0);
    if result != 0 {
        anyhow::bail!("Managed entrypoint failed! {}", result);
    }

    log::info!("Returned from managed loader");

    ctx.set(context)
        .map_err(|_| anyhow::format_err!("HOSTFXR_CTX failed assignment"))?;

    Ok(())
}
