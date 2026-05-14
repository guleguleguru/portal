# Microsoft Store Submission Draft

Status: planning

## Partner Center Identity

Fill after reserving the app name in Partner Center.

- Reserved app name: TBD
- Package identity name: TBD
- Publisher: TBD
- Publisher display name: TBD

## Recommended Distribution Route

Use MSIX / `.msixupload` through Partner Center.

Do not use the current self-contained `.zip` / `.exe` as the Store submission artifact. The direct zip remains useful for GitHub Releases and testing.

## Store Listing Draft

### App Name

Desktop Portal

Fallback if unavailable:

Desktop Portal - Hotkey Launcher

### Category

Productivity

### Short Description

Switch to files, folders, apps, and websites with local global hotkeys.

### Full Description

Desktop Portal is a lightweight Windows productivity tool for quickly opening or switching to the files, folders, applications, and websites you use most.

Create local rules that map a global shortcut to a target. Press the shortcut from anywhere in Windows and Desktop Portal will bring the matching window to the front, or open the target if it is not already running.

Desktop Portal is local-first: no account, no cloud sync, no analytics, and no background network service.

### Feature Bullets

- Configure hotkeys for URLs, files, folders, and applications.
- Use function keys, number keys, mouse middle/side buttons, or common modifier combinations.
- Switch to an existing target window when possible instead of opening duplicates.
- Open URLs in the default browser or Chrome/Edge app-window mode.
- Pause or resume all hotkeys from the main window or system tray.
- Validate rules with health checks for duplicate shortcuts and missing targets.
- Store configuration as local JSON.

### Keywords

hotkey, launcher, productivity, shortcut, window switcher, file launcher, desktop utility, local tool

### What's New

Initial Microsoft Store release.

### Support URL

TBD. Recommended:

<https://github.com/guleguleguru/portal/issues>

### Privacy Policy URL

TBD. Recommended stable option:

<https://github.com/guleguleguru/portal/blob/main/docs/privacy.md>

Prefer a GitHub Pages URL before final submission if available.

## Privacy Statement Summary

Desktop Portal does not collect, sell, transmit, or upload personal data.

Local files:

- Configuration: local app data / Desktop Portal config directory.
- Logs: local app data / Desktop Portal logs directory.

Logs are for local diagnostics and may contain rule names, target paths, hotkeys, and exception messages. Logs remain on the user's device unless the user explicitly shares them.

## Certification Notes Draft

Desktop Portal is a local Windows utility. It uses Win32 global hotkeys and optional mouse hotkeys to switch to user-configured local targets. The app can launch user-selected URLs, files, folders, and executable paths. It does not require administrator rights and does not run a network service.

The startup option is user-controlled and should use packaged app startup registration in the Store build.

## Screenshots Needed

- Main command center with a few sample rules.
- Rule editor showing target path and hotkey capture controls.
- Health check or conflict state in the status column.
- Settings window showing local startup option.

## Package Checklist

- [ ] Partner Center app name reserved.
- [ ] Package identity copied into the package manifest.
- [ ] Windows Application Packaging Project exists.
- [ ] MSIX Release x64 builds.
- [ ] `.msixupload` generated.
- [ ] Local install succeeds.
- [ ] Windows App Certification Kit passes.
- [ ] Startup behavior is compliant in packaged mode.
- [ ] Config/log paths are documented and tested.
- [ ] Screenshots captured.
- [ ] Privacy policy URL is public and stable.

## Known Risks

- The Store build may need a different startup implementation than the current HKCU Run registry value.
- Arbitrary exe launching must be tested under packaged app and Windows S compatibility expectations.
- Low-level mouse hooks and global hotkeys must be validated under MSIX and Windows App Certification Kit.
