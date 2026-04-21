use std::ffi::c_void;

#[unsafe(export_name = "?YYExtensionInitialise@@YAXPEBUYYRunnerInterface@@_K@Z")]
pub extern "system" fn extension_init(runner: *const c_void, runner_size: usize) {
    log::debug!("YYExtensionInitialise called! {runner:?} ({runner_size} bytes)");

    let callable = match crate::MANAGED_ENTRY_POINT.get() {
        Some(c) => c,
        None => {
            log::error!("Failed to get MANAGED_ENTRY_POINT for second call");
            return;
        }
    };

    let result = callable(runner, runner_size as i32);
    if result != 0 {
        log::error!("Second managed call failed! {}", result);
        return;
    }

    log::info!("Sent runner interop to managed code");
}

#[unsafe(no_mangle)]
pub extern "system" fn vsml_interop_test() -> *const u8 {
    "meow!".as_ptr()
}
