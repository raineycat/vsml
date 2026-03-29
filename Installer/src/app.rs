use std::{env, fmt, fs, io, path};

use iced::widget::{
    button, center, column, container, opaque, row, space, stack, text, text_input, tooltip,
};
use zip::read::root_dir_common_filter;

const VS_APP_ID: i32 = 2093940;

const VERSION_NAME_URL: &str =
    "https://raw.githubusercontent.com/raineycat/vsml/refs/heads/mistress/latest.txt";
const ZIP_FILE_URL: &str = "https://github.com/raineycat/vsml/releases/latest/download/vsml.zip";

const AUTO_DETECT_TOOLTIP: &str = r#"This scans your Steam library to try and find the game. 
It should work in the majority of situations, 
but you might need to manually locate it."#;

pub struct State {
    game_dir: Option<path::PathBuf>,
    latest_version: Option<String>,
    is_installing: bool,
    error_message: Option<String>,
}

#[derive(Clone)]
pub enum Message {
    StartInstall,
    EndInstall,

    SetGameDir(path::PathBuf),
    BrowseGameDir,
    DetectGameDir,

    SetLatestVersion(String),
    CheckLatestVersion,

    SetErrorMessage(String),
    ClearErrorMessage,
}

impl State {
    pub fn new() -> (Self, iced::Task<Message>) {
        let state = Self {
            game_dir: None,
            latest_version: None,
            is_installing: false,
            error_message: None,
        };
        let tasks =
            [Message::DetectGameDir, Message::CheckLatestVersion].map(|m| iced::Task::done(m));
        (state, iced::Task::batch(tasks))
    }

    pub fn theme(&self) -> iced::Theme {
        iced::Theme::KanagawaWave
    }

    pub fn update(&mut self, msg: Message) -> iced::Task<Message> {
        match msg {
            Message::StartInstall => {
                if !self.is_valid() {
                    log::error!("Install requirements not met!");
                    return iced::Task::none();
                }

                log::info!("Installing...");
                self.is_installing = true;
                install_task(self.game_dir.as_ref().unwrap().clone())
            }
            Message::EndInstall => {
                log::info!("Done!");
                self.is_installing = false;
                iced::Task::none()
            }

            Message::SetGameDir(d) => {
                self.game_dir = Some(d);
                iced::Task::none()
            }
            Message::BrowseGameDir => browse_dir_task(),
            Message::DetectGameDir => {
                self.game_dir = crate::game_finder::find_app_dir(VS_APP_ID);
                iced::Task::none()
            }

            Message::SetLatestVersion(ver) => {
                self.latest_version = Some(ver);
                iced::Task::none()
            }
            Message::CheckLatestVersion => get_version_task(),

            Message::SetErrorMessage(msg) => {
                self.error_message = Some(msg);
                iced::Task::none()
            }
            Message::ClearErrorMessage => {
                self.error_message = None;
                iced::Task::none()
            }
        }
    }

    pub fn view(&self) -> iced::Element<'_, Message> {
        let content = column![
            text("VSML Installer").size(30).center(),
            space(),
            text("Game directory").style(text::primary).size(20),
            text_input(
                "Select the game install directory",
                self.game_dir
                    .as_ref()
                    .and_then(|p| p.to_str())
                    .unwrap_or("")
            )
            .on_input(|s| Message::SetGameDir(path::PathBuf::from(s))),
            row![
                tooltip(
                    button("Auto-detect").on_press(Message::DetectGameDir),
                    container(AUTO_DETECT_TOOLTIP)
                        .padding(15)
                        .style(container::rounded_box),
                    tooltip::Position::Top
                )
                .delay(iced::time::Duration::from_millis(500)),
                button("Browse").on_press(Message::BrowseGameDir),
            ]
            .spacing(10),
            space(),
            text("Latest version").style(text::primary).size(20),
            row![
                text(
                    self.latest_version
                        .as_ref()
                        .map(|s| s.as_str())
                        .unwrap_or("???")
                )
                .size(20),
                space(),
                button("Check").on_press(Message::CheckLatestVersion)
            ]
            .spacing(10),
            space(),
            if self.is_valid() {
                button(
                    text("Install! :3")
                        .size(20)
                        .center()
                        .width(iced::Length::Fill),
                )
                .on_press(Message::StartInstall)
                .width(iced::Length::Fill)
                .into()
            } else {
                Into::<iced::Element<'_, Message>>::into(
                    text("The options currently set aren't valid!").style(text::warning),
                )
            },
            match self.error_message.as_ref() {
                Some(msg) => text!("{msg}").style(text::danger),
                None => text(""),
            },
        ]
        .padding(60)
        .spacing(15);

        let overlay: iced::Element<'_, Message> = if self.is_installing {
            center(opaque(
                text("Installing VSML...").size(30).style(text::primary),
            ))
            .style(translucent_bg_style)
            .into()
        } else {
            space().into()
        };

        stack![content, overlay].into()
    }

    fn is_valid(&self) -> bool {
        self.game_dir.is_some() && self.latest_version.is_some()
    }
}

fn translucent_bg_style(thene: &iced::Theme) -> container::Style {
    container::Style {
        background: Some(
            iced::Color {
                a: 0.95,
                ..thene.palette().background
            }
            .into(),
        ),
        ..Default::default()
    }
}

fn browse_dir_task() -> iced::Task<Message> {
    iced::Task::future(rfd::AsyncFileDialog::new().pick_folder()).then(|handle| match handle {
        Some(h) => iced::Task::done(Message::SetGameDir(h.path().to_path_buf())),
        None => iced::Task::none(),
    })
}

fn get_version_task() -> iced::Task<Message> {
    iced::Task::future(reqwest::get(VERSION_NAME_URL))
        .and_then(|res| iced::Task::done(res.error_for_status()))
        .and_then(|res| iced::Task::future(res.text()))
        .then(|res| match res {
            Ok(r) => iced::Task::done(Message::SetLatestVersion(r)),
            Err(e) => {
                log::error!("Failed to fetch latest version: {}", e);
                iced::Task::done(Message::SetErrorMessage(format!(
                    "Failed to fetch the latest version! - {e}"
                )))
            }
        })
}

fn install_task(target_dir: path::PathBuf) -> iced::Task<Message> {
    iced::Task::future(download_install_zip(target_dir)).then(|res| match res {
        Ok(_) => iced::Task::done(Message::EndInstall),
        Err(e) => {
            log::error!("Install failed: {}", e);
            let tasks = vec![
                iced::Task::done(Message::SetErrorMessage(format!(
                    "Failed to fetch install! - {e}"
                ))),
                iced::Task::done(Message::EndInstall),
            ];
            iced::Task::batch(tasks)
        }
    })
}

enum InstallError {
    RequestError(reqwest::Error),
    FileError(io::Error),
    ZipError(zip::result::ZipError),
}

impl fmt::Display for InstallError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            InstallError::RequestError(e) => write!(f, "Network request error: {}", e),
            InstallError::FileError(e) => write!(f, "File system error: {}", e),
            InstallError::ZipError(e) => write!(f, "ZIP file error: {}", e),
        }
    }
}

impl From<reqwest::Error> for InstallError {
    fn from(v: reqwest::Error) -> Self {
        Self::RequestError(v)
    }
}

impl From<io::Error> for InstallError {
    fn from(v: io::Error) -> Self {
        Self::FileError(v)
    }
}

impl From<zip::result::ZipError> for InstallError {
    fn from(v: zip::result::ZipError) -> Self {
        Self::ZipError(v)
    }
}

async fn download_install_zip(target_dir: path::PathBuf) -> Result<(), InstallError> {
    let temp_path = env::temp_dir().join("_vsml_download.zip");

    let resp = reqwest::get(ZIP_FILE_URL).await?.error_for_status()?;
    let buf = resp.bytes().await?;
    fs::write(&temp_path, buf)?;
    log::debug!("Saved zip to {}", temp_path.display());

    let file = fs::File::open(&temp_path)?;
    let mut zip = zip::ZipArchive::new(file)?;
    zip.extract_unwrapped_root_dir(target_dir, root_dir_common_filter)?;
    fs::remove_file(temp_path)?;

    Ok(())
}
