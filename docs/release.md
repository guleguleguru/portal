# Desktop Portal Release Notes

## Build

Run from the repository root:

```powershell
.\scripts\publish-release.ps1
```

The script creates:

- `artifacts\DesktopPortal-0.1.0-beta-win-x64\DesktopPortal.exe`
- `artifacts\DesktopPortal-0.1.0-beta-win-x64.zip`

The package is a self-contained `win-x64` Release build, so the target machine does not need a separate .NET runtime installation.

## Smoke Test

1. Run `DesktopPortal.exe` from `artifacts\DesktopPortal-0.1.0-beta-win-x64`.
2. Launch it a second time and confirm the existing window is brought forward instead of creating a second tray icon.
3. Confirm the tray icon uses `Assets\DesktopPortal.ico`.
4. Confirm `%AppData%\DesktopPortal\logs\app.log` rotates after it grows past the configured size.
5. Create one duplicate hotkey and one missing file target, then run Health Check and confirm the rule status column reports the issues.
6. Edit and save a rule, then confirm `%AppData%\DesktopPortal\backups\config.backup.*.json` keeps recent config snapshots.

## Installer

An Inno Setup script is available at:

- `installer\DesktopPortal.iss`

Build the release files first, then compile the installer script with Inno Setup. The expected installer output is:

- `artifacts\installer\DesktopPortal-0.1.0-beta-win-x64-setup.exe`
