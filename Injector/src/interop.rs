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

#[unsafe(export_name = "?interop_call@@YAXAEAURValue@@PEAVCInstance@@1HPEAU1@@Z")]
pub extern "system" fn interop_call(
    result: *const c_void,
    self_inst: *const c_void,
    other_inst: *const c_void,
    argc: i32,
    arg: *const c_void,
) {
    log::info!("interop_call({result:?}, {self_inst:?}, {other_inst:?}, {argc}, {arg:?})");
}
