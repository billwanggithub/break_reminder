# Rust Break Reminder MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a single-threaded Rust + Slint port of the WPF break reminder MVP (tray icon, fixed-interval timer, settings window, reminder window, JSON persistence) in a new repo at `d:\github\break_reminder_rs\`.

**Architecture:** One Slint event loop owns everything. `AppState` is held via `Rc<RefCell<...>>` and shared across UI / timer / tray callbacks. No background threads, no channels, no async runtime. Settings persist as JSON in `%LocalAppData%\BreakReminderRs\`.

**Tech Stack:** Rust (stable-msvc), Slint 1.8, tray-icon 0.19, serde + serde_json, dirs 5, winres 0.1 (build-dep), winit (transitive via Slint).

**Spec:** [`d:\github\break_reminder\docs\superpowers\specs\2026-05-02-rust-break-reminder-mvp-design.md`](../specs/2026-05-02-rust-break-reminder-mvp-design.md)

**Note on testing:** Per the spec, this MVP follows the WPF version's precedent and ships without automated tests. Each task ends with a `cargo build` / `cargo run` manual verification step instead of a test step. Verification is by the manual checklist in the spec, run after Task 9.

---

## Task 1: Bootstrap the Cargo project

**Files:**
- Create: `d:\github\break_reminder_rs\Cargo.toml`
- Create: `d:\github\break_reminder_rs\src\main.rs`
- Create: `d:\github\break_reminder_rs\.gitignore`

- [ ] **Step 1: Verify parent directory and that target does not yet exist**

Run from any directory:
```powershell
Test-Path d:\github
Test-Path d:\github\break_reminder_rs
```
Expected: `True` then `False`. If the second is `True`, stop and ask the user — there is unexpected state.

- [ ] **Step 2: Create the project with cargo**

Run:
```powershell
cargo new --bin d:\github\break_reminder_rs --name break_reminder_rs
```
Expected output: `Creating binary (application) `break_reminder_rs` package`. This produces `Cargo.toml`, `src/main.rs`, and `.gitignore`.

- [ ] **Step 3: Replace `Cargo.toml` with the full dependency set**

Overwrite `d:\github\break_reminder_rs\Cargo.toml`:
```toml
[package]
name = "break_reminder_rs"
version = "0.1.0"
edition = "2021"

[dependencies]
slint = "1.8"
tray-icon = "0.19"
serde = { version = "1", features = ["derive"] }
serde_json = "1"
dirs = "5"

[build-dependencies]
slint-build = "1.8"
winres = "0.1"

[profile.release]
strip = true
lto = true
codegen-units = 1
```

- [ ] **Step 4: Replace `src/main.rs` with a placeholder that compiles**

Overwrite `d:\github\break_reminder_rs\src\main.rs`:
```rust
fn main() {
    println!("break_reminder_rs: bootstrap OK");
}
```

- [ ] **Step 5: Build and run to confirm the toolchain is set up**

Run from `d:\github\break_reminder_rs`:
```powershell
cargo build
cargo run
```
Expected: build succeeds (this is slow the first time — Slint pulls a lot of crates), and `cargo run` prints `break_reminder_rs: bootstrap OK`. If `cargo build` errors with "linker `link.exe` not found", the user needs Visual Studio Build Tools with the C++ workload — stop and report that.

- [ ] **Step 6: Initialize git and commit**

Run from `d:\github\break_reminder_rs`:
```powershell
git init
git add .
git commit -m "chore: bootstrap Rust + Slint break reminder project"
```

---

## Task 2: Wire up the Slint build script and an empty MainWindow

**Files:**
- Create: `d:\github\break_reminder_rs\build.rs`
- Create: `d:\github\break_reminder_rs\ui\MainWindow.slint`
- Modify: `d:\github\break_reminder_rs\src\main.rs`

- [ ] **Step 1: Create the Slint UI directory and an empty settings window**

Create `d:\github\break_reminder_rs\ui\MainWindow.slint`:
```slint
import { SpinBox, Button, VerticalBox } from "std-widgets.slint";

export component MainWindow inherits Window {
    title: "設定 - 休息提醒小幫手";
    width: 360px;
    height: 180px;

    in-out property <int> interval-minutes: 45;
    callback save-clicked();

    VerticalBox {
        padding: 20px;
        spacing: 12px;

        HorizontalLayout {
            spacing: 8px;
            alignment: center;
            Text { text: "休息間隔："; vertical-alignment: center; font-size: 14px; }
            SpinBox {
                minimum: 1;
                maximum: 240;
                value <=> root.interval-minutes;
            }
            Text { text: "分鐘"; vertical-alignment: center; font-size: 14px; }
        }

        Button {
            text: "儲存並隱藏";
            clicked => { root.save-clicked(); }
        }
    }
}
```

- [ ] **Step 2: Add `build.rs` to compile the Slint file**

Create `d:\github\break_reminder_rs\build.rs`:
```rust
fn main() {
    slint_build::compile("ui/MainWindow.slint").unwrap();
}
```

- [ ] **Step 3: Replace `src/main.rs` to show the window once**

Overwrite `d:\github\break_reminder_rs\src\main.rs`:
```rust
slint::include_modules!();

fn main() -> Result<(), slint::PlatformError> {
    let window = MainWindow::new()?;
    window.on_save_clicked({
        let weak = window.as_weak();
        move || {
            if let Some(w) = weak.upgrade() {
                w.hide().ok();
            }
        }
    });
    window.run()
}
```

- [ ] **Step 4: Run and visually verify the window**

Run from `d:\github\break_reminder_rs`:
```powershell
cargo run
```
Expected: a window titled "設定 - 休息提醒小幫手" appears with a SpinBox preset to 45 and a "儲存並隱藏" button. Clicking the button closes the window and the process exits. Close it manually if needed.

- [ ] **Step 5: Verify VS Code Slint preview**

Open `d:\github\break_reminder_rs\ui\MainWindow.slint` in VS Code (Slint extension must be installed). Run command palette → "Slint: Show Preview". Expected: preview pane renders the same window. This validates the spec's "VS Code preview" goal. If the extension is missing, install `Slint.slint` from the marketplace and retry.

- [ ] **Step 6: Commit**

Run from `d:\github\break_reminder_rs`:
```powershell
git add .
git commit -m "feat: add empty Slint settings window with build script"
```

---

## Task 3: Add the settings module and wire the SpinBox to a real value

**Files:**
- Create: `d:\github\break_reminder_rs\src\settings.rs`
- Modify: `d:\github\break_reminder_rs\src\main.rs`

- [ ] **Step 1: Create `src/settings.rs`**

Create `d:\github\break_reminder_rs\src\settings.rs`:
```rust
use std::path::{Path, PathBuf};
use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct Settings {
    #[serde(rename = "IntervalMinutes")]
    pub interval_minutes: u32,
}

impl Default for Settings {
    fn default() -> Self {
        Self { interval_minutes: 45 }
    }
}

pub fn settings_path() -> PathBuf {
    let base = dirs::data_local_dir()
        .expect("LocalAppData unavailable")
        .join("BreakReminderRs");
    let _ = std::fs::create_dir_all(&base);
    base.join("settings.json")
}

pub fn load(path: &Path) -> Settings {
    match std::fs::read_to_string(path) {
        Ok(json) => serde_json::from_str(&json).unwrap_or_default(),
        Err(_) => Settings::default(),
    }
}

pub fn save(path: &Path, settings: &Settings) {
    match serde_json::to_string_pretty(settings) {
        Ok(json) => {
            if let Err(e) = std::fs::write(path, json) {
                eprintln!("[break_reminder_rs] failed to write settings: {e}");
            }
        }
        Err(e) => eprintln!("[break_reminder_rs] failed to serialize settings: {e}"),
    }
}
```

- [ ] **Step 2: Use the settings module in `main.rs`**

Overwrite `d:\github\break_reminder_rs\src\main.rs`:
```rust
mod settings;

slint::include_modules!();

fn main() -> Result<(), slint::PlatformError> {
    let path = settings::settings_path();
    let mut current = settings::load(&path);

    let window = MainWindow::new()?;
    window.set_interval_minutes(current.interval_minutes as i32);

    window.on_save_clicked({
        let weak = window.as_weak();
        let path = path.clone();
        move || {
            let Some(w) = weak.upgrade() else { return };
            current_set_and_save(w.get_interval_minutes(), &path, &mut current.clone());
            w.hide().ok();
        }
    });
    window.run()
}

fn current_set_and_save(value: i32, path: &std::path::Path, current: &mut settings::Settings) {
    current.interval_minutes = value.max(1) as u32;
    settings::save(path, current);
}
```

Note: this still does not persist `current` between callbacks across the closure boundary — that's intentional, the next task introduces `AppState` to hold mutable shared state cleanly. This task only wires the load/save round-trip.

- [ ] **Step 3: Run and verify load/save**

Run:
```powershell
cargo run
```
Expected: window opens with SpinBox at 45 (or whatever was previously saved). Change to 30, click save, window closes, process exits.

Then verify the file was written:
```powershell
Get-Content "$env:LOCALAPPDATA\BreakReminderRs\settings.json"
```
Expected: `{ "IntervalMinutes": 30 }` (or similar formatted JSON).

Run `cargo run` again. Expected: SpinBox now shows 30.

- [ ] **Step 4: Commit**

```powershell
git add .
git commit -m "feat: add settings module with JSON load/save"
```

---

## Task 4: Introduce AppState and the Slint Timer

**Files:**
- Create: `d:\github\break_reminder_rs\src\state.rs`
- Modify: `d:\github\break_reminder_rs\src\main.rs`

- [ ] **Step 1: Create `src/state.rs`**

Create `d:\github\break_reminder_rs\src\state.rs`:
```rust
use std::cell::RefCell;
use std::path::PathBuf;
use std::rc::Rc;
use std::time::Duration;

use slint::{ComponentHandle, Timer, TimerMode, Weak};

use crate::settings::{self, Settings};
use crate::{MainWindow, ReminderWindow};

pub struct AppState {
    pub settings: Settings,
    pub settings_path: PathBuf,
    pub settings_window: Weak<MainWindow>,
    pub reminder_window: Option<ReminderWindow>,
    pub timer: Timer,
}

impl AppState {
    pub fn new(settings_path: PathBuf, settings: Settings, settings_window: Weak<MainWindow>) -> Rc<RefCell<Self>> {
        Rc::new(RefCell::new(Self {
            settings,
            settings_path,
            settings_window,
            reminder_window: None,
            timer: Timer::default(),
        }))
    }

    pub fn restart_timer(state: &Rc<RefCell<Self>>) {
        let interval_minutes = state.borrow().settings.interval_minutes;
        let weak_state = Rc::downgrade(state);
        state.borrow().timer.start(
            TimerMode::Repeated,
            Duration::from_secs(interval_minutes as u64 * 60),
            move || {
                if let Some(s) = weak_state.upgrade() {
                    show_reminder(&s);
                }
            },
        );
    }

    pub fn stop_timer(&self) {
        self.timer.stop();
    }

    pub fn save(&self) {
        settings::save(&self.settings_path, &self.settings);
    }
}

pub fn show_settings(state: &Rc<RefCell<AppState>>) {
    let Some(window) = state.borrow().settings_window.upgrade() else { return };
    window.set_interval_minutes(state.borrow().settings.interval_minutes as i32);
    window.show().ok();
}

pub fn show_reminder(state: &Rc<RefCell<AppState>>) {
    if state.borrow().reminder_window.is_some() {
        if let Some(w) = state.borrow().reminder_window.as_ref().map(|w| w.as_weak()) {
            if let Some(w) = w.upgrade() {
                w.show().ok();
            }
        }
        return;
    }

    state.borrow().stop_timer();

    let window = match ReminderWindow::new() {
        Ok(w) => w,
        Err(e) => {
            eprintln!("[break_reminder_rs] failed to create reminder window: {e}");
            AppState::restart_timer(state);
            return;
        }
    };

    window.on_dismissed({
        let weak_state = Rc::downgrade(state);
        let weak_window = window.as_weak();
        move || {
            if let Some(w) = weak_window.upgrade() {
                w.hide().ok();
            }
            if let Some(state) = weak_state.upgrade() {
                state.borrow_mut().reminder_window = None;
                AppState::restart_timer(&state);
            }
        }
    });

    window.show().ok();
    state.borrow_mut().reminder_window = Some(window);
}
```

- [ ] **Step 2: Add a stub ReminderWindow.slint so `include_modules!` resolves**

Create `d:\github\break_reminder_rs\ui\ReminderWindow.slint`:
```slint
import { Button, VerticalBox } from "std-widgets.slint";

export component ReminderWindow inherits Window {
    title: "該休息囉！";
    width: 500px;
    height: 300px;
    always-on-top: true;
    no-frame: false;

    callback dismissed();

    VerticalBox {
        padding: 20px;
        alignment: center;

        Text {
            text: "⏰ 時間到！";
            font-size: 48px;
            horizontal-alignment: center;
        }
        Text {
            text: "現在立刻離開座位，喝杯水，讓眼睛過濾一下吧！";
            font-size: 18px;
            horizontal-alignment: center;
            wrap: word-wrap;
        }
        Button {
            text: "我知道了";
            height: 50px;
            clicked => { root.dismissed(); }
        }
    }
}
```

- [ ] **Step 3: Update `build.rs` to compile both Slint files**

Overwrite `d:\github\break_reminder_rs\build.rs`:
```rust
fn main() {
    slint_build::compile("ui/MainWindow.slint").unwrap();
    slint_build::compile("ui/ReminderWindow.slint").unwrap();
}
```

- [ ] **Step 4: Rewire `main.rs` to use AppState**

Overwrite `d:\github\break_reminder_rs\src\main.rs`:
```rust
mod settings;
mod state;

use std::time::Duration;

use slint::{ComponentHandle, Timer, TimerMode};
use state::{show_reminder, AppState};

slint::include_modules!();

fn main() -> Result<(), slint::PlatformError> {
    let path = settings::settings_path();
    let loaded = settings::load(&path);

    let settings_window = MainWindow::new()?;
    let app_state = AppState::new(path, loaded, settings_window.as_weak());

    settings_window.set_interval_minutes(app_state.borrow().settings.interval_minutes as i32);

    settings_window.on_save_clicked({
        let weak_state = std::rc::Rc::downgrade(&app_state);
        let weak_window = settings_window.as_weak();
        move || {
            let Some(state) = weak_state.upgrade() else { return };
            let Some(window) = weak_window.upgrade() else { return };
            let new_value = window.get_interval_minutes().max(1) as u32;
            state.borrow_mut().settings.interval_minutes = new_value;
            state.borrow().save();
            AppState::restart_timer(&state);
            window.hide().ok();
        }
    });

    AppState::restart_timer(&app_state);

    // Temporary: open settings window for development. Tray will replace this in Task 5.
    settings_window.show()?;

    slint::run_event_loop()
}
```

- [ ] **Step 5: Build and run to verify nothing regressed**

Run:
```powershell
cargo build
cargo run
```
Expected: settings window opens with the previously saved interval. Edit value, save, window hides — but the process keeps running because of `run_event_loop`. After `interval_minutes` minutes the reminder window appears (you can lower the interval to 1 to verify quickly without waiting). Press "我知道了" — reminder hides, timer continues. Stop the process with Ctrl+C in the terminal (or close all windows after Task 5 adds tray quit).

- [ ] **Step 6: Commit**

```powershell
git add .
git commit -m "feat: add AppState, Slint Timer, and reminder window"
```

---

## Task 5: Add the system tray icon and menu

**Files:**
- Create: `d:\github\break_reminder_rs\assets\app.ico` (copied)
- Create: `d:\github\break_reminder_rs\src\tray.rs`
- Modify: `d:\github\break_reminder_rs\src\main.rs`

- [ ] **Step 1: Copy the icon from the WPF repo**

Run:
```powershell
New-Item -ItemType Directory -Force d:\github\break_reminder_rs\assets | Out-Null
Copy-Item d:\github\break_reminder\app.ico d:\github\break_reminder_rs\assets\app.ico
Test-Path d:\github\break_reminder_rs\assets\app.ico
```
Expected: `True`.

- [ ] **Step 2: Create `src/tray.rs`**

Create `d:\github\break_reminder_rs\src\tray.rs`:
```rust
use std::cell::RefCell;
use std::rc::Rc;

use tray_icon::{
    menu::{Menu, MenuEvent, MenuItem, PredefinedMenuItem},
    Icon, MouseButton, MouseButtonState, TrayIcon, TrayIconBuilder, TrayIconEvent,
};

use crate::state::{show_reminder, show_settings, AppState};

pub struct Tray {
    _icon: TrayIcon,
}

pub fn build(state: Rc<RefCell<AppState>>) -> Tray {
    let icon = load_icon();

    let menu = Menu::new();
    let settings_item = MenuItem::new("設定 (Settings)", true, None);
    let break_item = MenuItem::new("立刻休息 (Break Now)", true, None);
    let exit_item = MenuItem::new("離開 (Exit)", true, None);
    menu.append(&settings_item).unwrap();
    menu.append(&break_item).unwrap();
    menu.append(&PredefinedMenuItem::separator()).unwrap();
    menu.append(&exit_item).unwrap();

    let tray = TrayIconBuilder::new()
        .with_menu(Box::new(menu))
        .with_tooltip("休息提醒小幫手 (雙擊開啟設定)")
        .with_icon(icon)
        .build()
        .expect("failed to build tray icon");

    let settings_id = settings_item.id().clone();
    let break_id = break_item.id().clone();
    let exit_id = exit_item.id().clone();
    let tray_id = tray.id().clone();

    register_handlers(state, settings_id, break_id, exit_id, tray_id);

    Tray { _icon: tray }
}

fn register_handlers(
    state: Rc<RefCell<AppState>>,
    settings_id: tray_icon::menu::MenuId,
    break_id: tray_icon::menu::MenuId,
    exit_id: tray_icon::menu::MenuId,
    tray_id: tray_icon::TrayIconId,
) {
    let menu_state = state.clone();
    let menu_settings = settings_id.clone();
    let menu_break = break_id.clone();
    let menu_exit = exit_id.clone();
    MenuEvent::set_event_handler(Some(move |event: MenuEvent| {
        let menu_state = menu_state.clone();
        let menu_settings = menu_settings.clone();
        let menu_break = menu_break.clone();
        let menu_exit = menu_exit.clone();
        slint::invoke_from_event_loop(move || {
            if event.id == menu_settings {
                show_settings(&menu_state);
            } else if event.id == menu_break {
                show_reminder(&menu_state);
            } else if event.id == menu_exit {
                slint::quit_event_loop().ok();
            }
        }).ok();
    }));

    let tray_state = state;
    TrayIconEvent::set_event_handler(Some(move |event: TrayIconEvent| {
        let TrayIconEvent::DoubleClick { id, button: MouseButton::Left, .. } = event else { return };
        if id != tray_id { return };
        let _ = (settings_id.clone(), break_id.clone(), exit_id.clone()); // silence move warnings
        let s = tray_state.clone();
        slint::invoke_from_event_loop(move || {
            show_settings(&s);
        }).ok();
    }));
}

fn load_icon() -> Icon {
    let bytes = include_bytes!("../assets/app.ico");
    let image = image::load_from_memory(bytes)
        .expect("failed to decode app.ico")
        .into_rgba8();
    let (w, h) = image.dimensions();
    Icon::from_rgba(image.into_raw(), w, h).expect("failed to build tray icon")
}
```

- [ ] **Step 3: Add `image` crate dependency**

Edit `d:\github\break_reminder_rs\Cargo.toml`, in the `[dependencies]` block append:
```toml
image = { version = "0.25", default-features = false, features = ["ico"] }
```
The `image` crate is needed because `tray-icon::Icon::from_rgba` wants raw pixels, and decoding the `.ico` is the most portable way to get them.

- [ ] **Step 4: Wire tray into `main.rs` and remove the temporary `show()` call**

Overwrite `d:\github\break_reminder_rs\src\main.rs`:
```rust
mod settings;
mod state;
mod tray;

use slint::ComponentHandle;
use state::AppState;

slint::include_modules!();

fn main() -> Result<(), slint::PlatformError> {
    let path = settings::settings_path();
    let loaded = settings::load(&path);

    let settings_window = MainWindow::new()?;
    let app_state = AppState::new(path, loaded, settings_window.as_weak());

    settings_window.set_interval_minutes(app_state.borrow().settings.interval_minutes as i32);

    settings_window.on_save_clicked({
        let weak_state = std::rc::Rc::downgrade(&app_state);
        let weak_window = settings_window.as_weak();
        move || {
            let Some(state) = weak_state.upgrade() else { return };
            let Some(window) = weak_window.upgrade() else { return };
            let new_value = window.get_interval_minutes().max(1) as u32;
            state.borrow_mut().settings.interval_minutes = new_value;
            state.borrow().save();
            AppState::restart_timer(&state);
            window.hide().ok();
        }
    });

    let _tray = tray::build(app_state.clone());

    AppState::restart_timer(&app_state);

    slint::run_event_loop()
}
```

- [ ] **Step 5: Build and run, then test all tray paths manually**

Run:
```powershell
cargo run
```
Expected:
- No window shows on startup. A tray icon appears in the Windows notification area.
- Right-click tray → menu with 設定 / 立刻休息 / (separator) / 離開.
- Click 設定 → settings window opens.
- Double-click tray → settings window opens (same effect).
- Click 立刻休息 → reminder window appears immediately and the timer resets.
- Click 離開 → process exits.

If the tray icon does not appear, check that `assets/app.ico` is a valid `.ico` file (`Test-Path` from Step 1).

- [ ] **Step 6: Commit**

```powershell
git add .
git commit -m "feat: add system tray with Settings / Break Now / Exit menu"
```

---

## Task 6: Embed the icon into the .exe via winres

**Files:**
- Modify: `d:\github\break_reminder_rs\build.rs`

- [ ] **Step 1: Update `build.rs` to embed the icon on Windows release builds**

Overwrite `d:\github\break_reminder_rs\build.rs`:
```rust
fn main() {
    slint_build::compile("ui/MainWindow.slint").unwrap();
    slint_build::compile("ui/ReminderWindow.slint").unwrap();

    #[cfg(target_os = "windows")]
    {
        let mut res = winres::WindowsResource::new();
        res.set_icon("assets/app.ico");
        if let Err(e) = res.compile() {
            eprintln!("warning: winres failed to embed icon: {e}");
        }
    }
}
```

- [ ] **Step 2: Rebuild and verify the exe icon**

Run:
```powershell
cargo build --release
```
Expected: build succeeds. Then verify the icon shows up on the binary in Explorer:
```powershell
Start-Process explorer.exe d:\github\break_reminder_rs\target\release\
```
Expected: `break_reminder_rs.exe` shows the same icon as the WPF app's `BreakReminder.exe`.

- [ ] **Step 3: Commit**

```powershell
git add .
git commit -m "build: embed app icon into Windows binary via winres"
```

---

## Task 7: Hide the console window in release builds

**Files:**
- Modify: `d:\github\break_reminder_rs\src\main.rs`

- [ ] **Step 1: Add the `windows_subsystem` attribute at the top of `main.rs`**

Edit `d:\github\break_reminder_rs\src\main.rs` and add this as the very first line (above `mod settings;`):
```rust
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]
```

- [ ] **Step 2: Verify debug build still shows console**

Run:
```powershell
cargo run
```
Expected: a console window appears alongside the tray icon (debug build). `eprintln!` from settings save errors will be visible here. Click 離開 to exit.

- [ ] **Step 3: Verify release build does NOT show a console**

Run:
```powershell
cargo build --release
Start-Process d:\github\break_reminder_rs\target\release\break_reminder_rs.exe
```
Expected: no console window appears. The tray icon shows up. Click 離開 from the tray menu to exit.

- [ ] **Step 4: Commit**

```powershell
git add .
git commit -m "chore: hide console window in release builds"
```

---

## Task 8: Polish — center the reminder window and tighten window styling

**Files:**
- Modify: `d:\github\break_reminder_rs\ui\ReminderWindow.slint`
- Modify: `d:\github\break_reminder_rs\ui\MainWindow.slint`

- [ ] **Step 1: Tighten the reminder window to match the WPF layout**

Overwrite `d:\github\break_reminder_rs\ui\ReminderWindow.slint`:
```slint
import { Button, VerticalBox } from "std-widgets.slint";

export component ReminderWindow inherits Window {
    title: "該休息囉！";
    width: 500px;
    height: 300px;
    always-on-top: true;

    callback dismissed();

    VerticalBox {
        padding: 20px;
        spacing: 16px;

        VerticalLayout {
            alignment: center;
            spacing: 20px;
            Text {
                text: "⏰ 時間到！";
                font-size: 48px;
                horizontal-alignment: center;
            }
            Text {
                text: "現在立刻離開座位，喝杯水，讓眼睛過濾一下吧！";
                font-size: 18px;
                horizontal-alignment: center;
                wrap: word-wrap;
            }
        }

        Button {
            text: "我知道了";
            height: 50px;
            clicked => { root.dismissed(); }
        }
    }
}
```

- [ ] **Step 2: Run and visually compare to WPF version**

Run from `d:\github\break_reminder_rs`:
```powershell
cargo run
```
Click 立刻休息 from the tray. Compare the layout to the WPF `ReminderWindow.xaml`: the headline should be large, the message smaller and below, and the button at the bottom. If the spacing is off, adjust `spacing` and `padding` values; otherwise leave it alone.

- [ ] **Step 3: Commit**

```powershell
git add .
git commit -m "feat: tighten reminder window layout to match WPF version"
```

---

## Task 9: Run the full manual verification checklist

**Files:** none (verification only)

- [ ] **Step 1: Build a fresh release binary**

Run:
```powershell
cargo build --release
```
Expected: build succeeds with no warnings. If there are warnings, decide whether to fix them now or open follow-up issues.

- [ ] **Step 2: Run through the spec's manual checklist**

Start the release binary:
```powershell
Start-Process d:\github\break_reminder_rs\target\release\break_reminder_rs.exe
```

Check every item from `docs/superpowers/specs/2026-05-02-rust-break-reminder-mvp-design.md` "Manual verification checklist". For convenience, repeated here:
- [ ] `cargo run` shows tray icon (done above)
- [ ] Double-click tray opens settings window
- [ ] Edit interval, click save → window hides
- [ ] Restart app → new interval persisted
- [ ] After interval elapses, reminder window appears on top, centered (test with interval=1 to make this fast; remember to set it back afterwards)
- [ ] Click "我知道了" → window closes, timer continues
- [ ] "立刻休息" menu item shows reminder immediately and resets the timer
- [ ] "離開" menu item terminates the process
- [ ] Open `ui/MainWindow.slint` in VS Code; Slint preview renders the window

If any item fails, open a follow-up issue and document it; do not fix on the fly unless the failure indicates a clear regression in this MVP.

- [ ] **Step 3: Copy the spec into the new repo**

The spec was written in the WPF repo because the Rust repo did not exist yet. Now copy it over:
```powershell
New-Item -ItemType Directory -Force d:\github\break_reminder_rs\docs\specs | Out-Null
Copy-Item d:\github\break_reminder\docs\superpowers\specs\2026-05-02-rust-break-reminder-mvp-design.md d:\github\break_reminder_rs\docs\specs\
```

- [ ] **Step 4: Final commit**

```powershell
git add .
git commit -m "docs: copy MVP design spec into Rust repo"
```

- [ ] **Step 5: Tag the MVP**

```powershell
git tag -a v0.1.0-mvp -m "MVP: tray + interval timer + settings + reminder window"
git log --oneline
```
Expected: a clean linear history of ~9 commits, ending with the v0.1.0-mvp tag.
