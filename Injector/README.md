# VSML Injector

This is a simple little injectable DLL written in Rust. It proxies the windows `version.dll`, but hooks the `CreateFileW` function to use as an entrypoint.

We use the `netcorehost` crate to spin up a .NET runtime, and run the rest of the modloader from there.
This DLL also silently switches the filename passed to the real `CreateFileW` function, such that when the game tries to read `data.win` it actually reads a file named `shadow.win` instead, and vice versa.

This allows us to force the game to load our patched data file, but in a way that leaves no permanent modifications to files on disk. (the main reason I wanted to make this)

## Building

Building this is handled by Just, so all you need to do is run `just build-injector` from the repo root.

If you want to do it manually, on windows, it should be as simple as `cargo build`.

On linux, you need to have [cargo-xwin](https://github.com/rust-cross/cargo-xwin) installed. Then, you should be able to run `cargo build-cross`.
