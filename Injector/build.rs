use windows_exe_info::versioninfo::*;

fn main() {
    VersionInfo {
        file_version: Version(0, 1, 0, 0),
        product_version: Version(0, 1, 0, 0),
        file_flag_mask: FileFlagMask::Win16,
        file_flags: FileFlags {
            debug: false,
            patched: false,
            prerelease: false,
            privatebuild: false,
            infoinferred: false,
            specialbuild: false,
        },
        file_os: FileOS::Windows32,
        file_type: FileType::App,
        file_info: vec![FileInfo {
            lang: Language::UKEnglish,
            charset: CharacterSet::Multilingual,
            comment: None,
            company_name: "raineycat (https://reddust.uk)".into(),
            file_description: "VSML injector DLL".into(),
            file_version: "0.1.0.0".into(),
            internal_name: "injector-rs".into(),
            legal_copyright: None,
            legal_trademarks: None,
            original_filename: "injector.dll".into(),
            product_name: "VSML".into(),
            product_version: "0.1.0.0".into(),
            private_build: None,
            special_build: None,
        }],
    }
    .link()
    .unwrap();
}
