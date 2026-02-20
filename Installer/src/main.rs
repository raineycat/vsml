#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod app;
mod game_finder;

fn main() -> iced::Result {
    let env = env_logger::Env::default().default_filter_or("info");
    env_logger::Builder::from_env(env).init();

    #[cfg(target_os = "linux")]
    let window = iced::window::Settings {
        platform_specific: iced::window::settings::PlatformSpecific {
            application_id: "uk.reddust.vsml.installer".into(),
            override_redirect: false,
        },
        ..Default::default()
    };

    #[cfg(not(target_os = "linux"))]
    let window = iced::window::Settings::default();

    log::info!("Starting");
    iced::application(app::State::new, app::State::update, app::State::view)
        .title("VSML Installer")
        .theme(app::State::theme)
        .window(window)
        .window_size(iced::Size::new(600.0, 550.0))
        .run()
}
