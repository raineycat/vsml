use std::{collections::HashMap, fs, path};

#[cfg(target_os = "windows")]
fn find_steam_dir() -> Option<path::PathBuf> {
    let hklm = winreg::RegKey::predef(winreg::enums::HKEY_LOCAL_MACHINE);
    let steam_key = hklm
        .open_subkey("SOFTWARE\\Valve\\Steam")
        .or(hklm.open_subkey("SOFTWARE\\WOW6432Node\\Valve\\Steam"))
        .ok()?;

    let install_dir: String = steam_key.get_value("InstallPath").ok()?;
    Some(path::PathBuf::from(install_dir))
}

#[cfg(not(target_os = "windows"))]
fn find_steam_dir() -> Option<path::PathBuf> {
    let home = std::env::var("HOME").ok()?;
    return Some(path::PathBuf::from(home).join(".steam").join("steam"));
}

pub fn find_app_dir(app_id: i32) -> Option<path::PathBuf> {
    let steam_dir = find_steam_dir()?;
    log::debug!("Found steam dir: {}", steam_dir.display());

    let library_folders_path = steam_dir.join("steamapps").join("libraryfolders.vdf");
    let library_folders_data = fs::read_to_string(library_folders_path).ok()?;

    let library_folders =
        match keyvalues_serde::from_str::<LibraryFoldersData>(&library_folders_data) {
            Ok(x) => x,
            Err(e) => {
                log::error!("Failed to parse libraryfolders.vdf: {}", e);
                return None;
            }
        };

    log::debug!("Loaded library folder data: {:#?}", library_folders);

    let app_lib_path = library_folders
        .0
        .iter()
        .find(|(_, lib)| lib.apps.contains_key(&app_id))
        .map(|(_, lib)| lib.path.as_str())?;
    log::debug!("App ID {} in lib {:?}", app_id, app_lib_path);

    let manifest_path = path::Path::new(app_lib_path)
        .join("steamapps")
        .join(format!("appmanifest_{}.acf", app_id));
    let manifest_data = fs::read_to_string(manifest_path).ok()?;

    let manifest = match keyvalues_serde::from_str::<AppState>(&manifest_data) {
        Ok(x) => x,
        Err(e) => {
            log::error!("Failed to parse appmanifest_x.acf: {}", e);
            return None;
        }
    };
    log::debug!("Loaded app manifest: {:#?}", manifest);

    let final_app_path = path::Path::new(app_lib_path)
        .join("steamapps")
        .join("common")
        .join(manifest.installdir);

    Some(final_app_path)
}

#[derive(Debug, serde::Deserialize)]
#[serde(rename = "libraryfolders")]
struct LibraryFoldersData(HashMap<usize, LibraryFolderEntry>);

#[derive(Debug, serde::Deserialize)]
struct LibraryFolderEntry {
    path: String,
    apps: HashMap<i32, usize>,
}

#[derive(Debug, serde::Deserialize)]
struct AppState {
    installdir: String,
}
