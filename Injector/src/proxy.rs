use lazy_static::lazy_static;
use libloading::Library;
use libloading::Symbol;
use std::ffi::{c_char, c_void};
use std::fmt::Write;
use std::path;
use windows::Win32;

fn load_version_lib() -> Option<Library> {
    let mut buffer: [u8; 512] = [0; 512];
    let count = unsafe { Win32::System::SystemInformation::GetSystemDirectoryA(Some(&mut buffer)) };

    if count == 0 {
        log::error!("Invalid system dir!");
        return None;
    }

    let count = count as usize;
    let sys_dir = match String::from_utf8(buffer[0..count].to_vec()) {
        Ok(s) => s,
        Err(e) => {
            log::error!("GetSystemDirectoryA returned an invalid string! {:?}", e);
            return None;
        }
    };

    let mut dll_path = path::PathBuf::from(sys_dir);
    dll_path.push("version.dll");

    unsafe {
        match Library::new(dll_path) {
            Ok(lib) => Some(lib),
            Err(e) => {
                log::error!("Failed to load system lib to proxy: {:?}", e);
                None
            }
        }
    }
}

lazy_static! {
    static ref VERSION_DLL: Library = load_version_lib().unwrap();
}

macro_rules! proxy_call {
    ($func: ident, $ret_type: ty, $($param_names: ident: $param_types: ty),*) => {
        #[unsafe(no_mangle)]
        pub extern "system" fn $func($($param_names: $param_types),+) -> $ret_type {
            type ProxyType = unsafe extern "system" fn($($param_types),*) -> $ret_type;

            let mut trace_text = String::new();
            $(
                let _ = write!(&mut trace_text, "{:?} = {:?}, ", stringify!($param_names), $param_names);
            )+
            log::debug!("Proxying call: {}({})", stringify!($func), &trace_text[..trace_text.len()-2]);

            unsafe {
                let sym: Symbol<ProxyType> =
                    match VERSION_DLL.get(stringify!($func)){
                      Ok(sym) => sym,
                      Err(e) => {
                          log::error!("Failed to find symbol '{}': {:?}", stringify!(func), e);
                          panic!()
                      }
                    };

                sym($($param_names),+)
            }
        }
    };
}

proxy_call!(GetFileVersionInfoA, i32, lptstr_filename: *const c_char, dw_handle: u32, dw_len: u32, lp_data: *mut c_void);
proxy_call!(GetFileVersionInfoExA, i32, dw_flags: u32, lptstr_filename: *const c_char, dw_handle: u32, dw_len: u32, lp_data: *mut c_void);

proxy_call!(GetFileVersionInfoSizeA, u32, lptstr_filename: *const c_char, lpdw_handle: *mut u32);
proxy_call!(GetFileVersionInfoSizeExA, u32, dw_flags: u32, lptstr_filename: *const c_char, lpdw_handle: *mut u32);

proxy_call!(GetFileVersionInfoW, i32, lpwstr_filename: *const u16, dw_handle: u32, dw_len: u32, lp_data: *mut c_void);
proxy_call!(GetFileVersionInfoExW, i32, dw_flags: u32, lpwstr_filename: *const u16, dw_handle: u32, dw_len: u32, lp_data: *mut c_void);

proxy_call!(GetFileVersionInfoSizeW, u32, lpwstr_filename: *const u16, lpdw_handle: *mut u32);
proxy_call!(GetFileVersionInfoSizeExW, u32, dw_flags: u32, lpwstr_filename: *const u16, lpdw_handle: *mut u32);

proxy_call!(GetFileVersionInfoByHandle, i32, dw_flags: u32, h_file: *const c_void, lplp_data: *mut *mut c_void, pdw_len: *mut u32);

proxy_call!(VerFindFileA, u32, u_flags: u32, sz_file_name: *const c_char, sz_win_dir: *const c_char, sz_app_dir: *const c_char, sz_cur_dir: *mut c_char, pu_cur_dir_len: *mut u32, sz_dest_dir: *mut c_char, pu_dest_dir_len: *mut u32);
proxy_call!(VerFindFileW, u32, u_flags: u32, sz_file_name: *const u16, sz_win_dir: *const u16, sz_app_dir: *const u16, sz_cur_dir: *mut u16, pu_cur_dir_len: *mut u32, sz_dest_dir: *mut u16, pu_dest_dir_len: *mut u32);

proxy_call!(VerInstallFileA, u32, u_flags: u32, sz_src_file_name: *const c_char, sz_dest_file_name: *const c_char, sz_src_dir: *const c_char, sz_dest_dir: *const c_char, sz_cur_dir: *const c_char, sz_tmp_file: *mut c_char, pu_tmp_file_len: *mut u32);
proxy_call!(VerInstallFileW, u32, u_flags: u32, sz_src_file_name: *const u16, sz_dest_file_name: *const u16, sz_src_dir: *const u16, sz_dest_dir: *const u16, sz_cur_dir: *const u16, sz_tmp_file: *mut u16, pu_tmp_file_len: *mut u32);

proxy_call!(VerLanguageNameA, u32, w_lang: u32, sz_lang: *mut c_char, cch_lang: u32);
proxy_call!(VerLanguageNameW, u32, w_lang: u32, sz_lang: *mut u16, cch_lang: u32);

proxy_call!(VerQueryValueA, i32, p_block: *const c_void, lp_sub_block: *const c_char, lplp_buffer: *mut *mut c_void, pu_len: *mut u32);
proxy_call!(VerQueryValueW, i32, p_block: *const c_void, lp_sub_block: *const u16, lplp_buffer: *mut *mut c_void, pu_len: *mut u32);
