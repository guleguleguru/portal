# Microsoft Store Release Implementation Plan

> **For Codex:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Prepare Desktop Portal for Microsoft Store submission through an MSIX-based package.

**Architecture:** Keep the existing WPF application as the desktop entry point and add a Windows Application Packaging Project for Store packaging. Store-specific behavior must be isolated behind services so the unpackaged direct-download build can keep using current HKCU startup behavior while the packaged Store build uses packaged app APIs and manifest extensions.

**Tech Stack:** C# / .NET / WPF, Windows Application Packaging Project, MSIX, Partner Center, Windows App Certification Kit.

---

## Current Readiness Snapshot

Date checked: 2026-05-14.

- Repository: clean, `main` tracks `origin/main`.
- App project: `DesktopPortal/DesktopPortal.csproj`, WPF `WinExe`, `net10.0-windows`.
- Current release artifacts: self-contained win-x64 zip/exe only.
- Missing Store artifacts: no `.msix`, `.msixupload`, `.appxupload`, `.pfx`, or `.cer`.
- Missing local tooling: `makeappx`, `signtool`, and `appcert` are not currently available in PATH or the usual Windows SDK folders.
- Recommended path: MSIX via Windows Application Packaging Project, not MSI/EXE.

## Official Guidance Used

- Microsoft Store is recommended for most apps and handles signing, updates, and discovery. WPF/WinForms do not produce MSIX by default; Microsoft recommends adding a Windows Application Packaging Project for Store submission: <https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/publish-first-app>
- Windows Application Packaging Project is the supported Visual Studio route for WPF desktop apps: <https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-packaging-dot-net>
- Store submissions should upload `.msixupload` / `.appxupload` when available: <https://learn.microsoft.com/en-us/windows/msix/package/packaging-uwp-apps>
- MSIX packages for Store should be tested with Windows App Certification Kit: <https://learn.microsoft.com/en-us/windows/uwp/debug-test-perf/windows-app-certification-kit>
- MSI/EXE Store submission requires CA code signing, a versioned HTTPS URL, and silent install; this is a fallback, not the recommended path for this project: <https://learn.microsoft.com/en-gb/windows/apps/publish/publish-your-app/msi/app-package-requirements>

## Release Strategy

Use two distribution tracks:

- **Store track:** packaged MSIX, Partner Center, Microsoft-managed signing and updates.
- **Direct track:** existing self-contained zip / future installer for testers and GitHub Releases.

Do not force the direct track to inherit Store-only constraints. Do not let Store-only APIs break the unpackaged build.

## Major Certification Risks To Resolve

- **Startup registration:** current `StartupService` writes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. Packaged apps should use a manifest `desktop:StartupTask` and packaged startup APIs instead.
- **AppData path behavior:** MSIX redirects app data and registry writes. Verify `%AppData%\DesktopPortal\config.json` remains acceptable or migrate packaged builds to `ApplicationData.Current.LocalFolder`.
- **Windows S compatibility:** Microsoft MSIX guidance says Store apps must work on Windows 10 S. Desktop Portal launches external files, folders, browsers, and arbitrary exe targets, so this must be tested and may require a Store-mode limitation or certification note.
- **Global hooks/hotkeys:** `RegisterHotKey` and low-level mouse hooks must be tested in packaged context and WACK.
- **Package identity:** Partner Center app identity values are required for the manifest and should be associated through Visual Studio after the app name is reserved.

---

### Task 1: Install Store Packaging Toolchain

**Files:**
- No repo files required.

**Step 1: Install prerequisites**

Install Visual Studio 2022 or newer with:

- `.NET desktop development`
- `Universal Windows Platform development`, or the optional `MSIX Packaging Tools` component under `.NET desktop development`
- Windows 10/11 SDK including Windows App Certification Kit

**Step 2: Verify tools**

Run:

```powershell
where.exe makeappx
where.exe signtool
where.exe appcert
```

Expected: each command prints at least one path.

**Step 3: Commit**

No commit unless docs or scripts are changed.

### Task 2: Reserve App Name And Capture Package Identity

**Files:**
- Modify: `docs/store/microsoft-store-submission.md`

**Step 1: Reserve name**

In Partner Center, reserve the final public app name. Recommended first choice:

```text
Desktop Portal
```

Fallback if unavailable:

```text
Desktop Portal - Hotkey Launcher
```

**Step 2: Record identity**

Copy Partner Center package identity fields into `docs/store/microsoft-store-submission.md` under "Partner Center identity".

Expected fields:

- Package/Identity/Name
- Publisher
- PublisherDisplayName
- Reserved app name

**Step 3: Commit**

```powershell
git add docs/store/microsoft-store-submission.md
git commit -m "docs: record Microsoft Store app identity"
```

### Task 3: Add Windows Application Packaging Project

**Files:**
- Create: `DesktopPortal.Package/`
- Modify: `DesktopPortal.slnx`
- Modify: packaging manifest inside `DesktopPortal.Package`

**Step 1: Create packaging project**

Use Visual Studio:

1. Open `DesktopPortal.slnx`.
2. Add a Windows Application Packaging Project named `DesktopPortal.Package`.
3. Set minimum version to Windows 10 Anniversary Update or later; prefer Windows 10 19041+ if certification testing confirms it.
4. Add a project reference from the packaging project to `DesktopPortal`.
5. Set `DesktopPortal` as the package entry point.

**Step 2: Configure visual assets**

Use `DesktopPortal/Assets/DesktopPortal.ico` and generate Store package images from the existing PNG source assets:

- `DesktopPortal/Assets/DesktopPortal.icon.png`
- `DesktopPortal/Assets/DesktopPortal.icon-source.png`

**Step 3: Build package project**

Run from Visual Studio first. Then verify from command line if MSBuild supports the project:

```powershell
msbuild .\DesktopPortal.Package\DesktopPortal.Package.wapproj /p:Configuration=Release /p:Platform=x64
```

Expected: package project builds without errors.

**Step 4: Commit**

```powershell
git add DesktopPortal.Package DesktopPortal.slnx
git commit -m "build: add MSIX packaging project"
```

### Task 4: Split StartupService For Packaged And Unpackaged Builds

**Files:**
- Modify: `DesktopPortal/Services/StartupService.cs`
- Modify: `DesktopPortal/Views/SettingsWindow.xaml.cs`
- Modify: package manifest for startup task declaration
- Test: `DesktopPortal.Tests/Program.cs`

**Step 1: Write failing tests**

Add tests for service selection:

```csharp
[TestMethod]
public void StartupServiceFactoryUsesPackagedServiceWhenPackageIdentityExists()
{
    // Use an injected identity detector so this can be tested without MSIX.
}
```

Expected: fails until a factory / detector abstraction exists.

**Step 2: Implement abstraction**

Create:

- `IStartupService`
- `RegistryStartupService` for unpackaged builds
- `PackagedStartupService` using packaged startup task APIs
- `StartupServiceFactory`

Keep the UI using the interface.

**Step 3: Add manifest extension**

Add `desktop:StartupTask` for Desktop Portal to the package manifest.

**Step 4: Verify**

Run:

```powershell
dotnet test .\DesktopPortal.Tests\DesktopPortal.Tests.csproj -c Release
dotnet build .\DesktopPortal\DesktopPortal.csproj -c Release
```

Expected: tests and build pass.

**Step 5: Commit**

```powershell
git add DesktopPortal DesktopPortal.Tests DesktopPortal.Package
git commit -m "feat: support packaged startup registration"
```

### Task 5: Decide Store Data Path

**Files:**
- Modify: `DesktopPortal/Services/ConfigService.cs`
- Modify: `docs/privacy.md`
- Test: `DesktopPortal.Tests/Program.cs`

**Step 1: Investigate packaged path**

Install the test MSIX locally and inspect where config/logs are written.

**Step 2: If needed, add path provider**

Create a path provider abstraction:

- unpackaged: `%AppData%\DesktopPortal`
- packaged: package local app data folder

**Step 3: Update privacy docs**

Document both packaged and unpackaged local data locations.

**Step 4: Verify**

Run tests and manually install the package to verify config persistence across relaunch.

**Step 5: Commit**

```powershell
git add DesktopPortal DesktopPortal.Tests docs/privacy.md
git commit -m "feat: support packaged app data paths"
```

### Task 6: Package And Run Local MSIX Smoke Tests

**Files:**
- Modify: `docs/qa-checklist.md`
- Optionally create: `docs/store/msix-smoke-test-results.md`

**Step 1: Generate local package**

Use Visual Studio Create App Packages wizard or MSBuild after project setup.

Expected output:

```text
*.msix
*.msixupload
```

**Step 2: Install locally**

Install using App Installer or PowerShell.

**Step 3: Smoke test**

Verify:

- Launch from Start menu.
- Add/edit/delete rule.
- Register `F8` or `Alt+1`.
- Trigger URL target.
- Trigger file/folder target.
- Trigger mouse hotkey.
- Pause/resume all hotkeys.
- Tray menu works.
- Settings opens.
- Packaged startup option behaves as expected.

**Step 4: Record results**

Record the package path, version, OS version, and failed/passed cases in `docs/store/msix-smoke-test-results.md`.

**Step 5: Commit**

```powershell
git add docs/qa-checklist.md docs/store/msix-smoke-test-results.md
git commit -m "test: document MSIX smoke test results"
```

### Task 7: Run Windows App Certification Kit

**Files:**
- Create or modify: `docs/store/wack-results.md`

**Step 1: Run WACK**

Run Windows App Certification Kit against the installed packaged app.

Expected: pass, or failures with actionable details.

**Step 2: Record report**

Record:

- WACK version
- Package version
- OS build
- Pass/fail summary
- Failure report path

**Step 3: Fix failures using TDD where possible**

For every app-code failure, write a failing test first, then fix.

**Step 4: Commit**

```powershell
git add docs/store/wack-results.md DesktopPortal DesktopPortal.Tests
git commit -m "test: record Windows App Certification Kit results"
```

### Task 8: Prepare Store Listing Assets

**Files:**
- Modify: `docs/store/microsoft-store-submission.md`
- Create: `store-assets/` or another ignored/export folder for final screenshots if desired

**Step 1: Capture screenshots**

Provide at least one screenshot; target four or more:

- Main rules command center
- Rule editor with drag-drop target field
- Hotkey capture state
- Settings / tray story if applicable

**Step 2: Finalize listing text**

Use `docs/store/microsoft-store-submission.md` as the source for:

- Description
- Short description
- Feature bullets
- Keywords
- Certification notes

**Step 3: Publish privacy policy URL**

Use a stable public URL, preferably GitHub Pages or a project website.

**Step 4: Commit**

```powershell
git add docs/store/microsoft-store-submission.md store-assets
git commit -m "docs: prepare Microsoft Store listing assets"
```

### Task 9: Submit Draft In Partner Center

**Files:**
- Modify: `docs/store/microsoft-store-submission.md`

**Step 1: Create submission**

Complete:

- Pricing and availability
- Properties
- Age ratings
- Packages
- Store listings
- Submission options

**Step 2: Certification notes**

Include:

- Desktop Portal is local-only.
- It does not collect or transmit user data.
- It uses global hotkeys and optional startup registration for user-configured shortcut switching.
- It may launch user-selected files, folders, URLs, and apps.
- It does not require administrator rights.

**Step 3: Commit final notes**

```powershell
git add docs/store/microsoft-store-submission.md
git commit -m "docs: finalize Microsoft Store submission notes"
```

### Task 10: Submit For Certification

**Files:**
- Modify: `docs/store/microsoft-store-submission.md`

**Step 1: Submit**

Submit the Partner Center draft for certification.

**Step 2: Track result**

Record certification status, date, and any reviewer feedback in `docs/store/microsoft-store-submission.md`.

**Step 3: Fix any certification failures**

Use `superpowers:systematic-debugging` and `superpowers:test-driven-development` before changing code.

**Step 4: Commit**

```powershell
git add docs/store/microsoft-store-submission.md
git commit -m "docs: record Microsoft Store certification status"
```

## Go / No-Go Criteria

Go only when:

- Packaging project builds Release x64 MSIX.
- `.msixupload` exists.
- Local install works without developer-only manual hacks beyond test certificate trust.
- WACK passes.
- Startup setting works in packaged mode or is disabled/hidden with clear UI.
- Config and logs persist across relaunch.
- Store listing has at least one screenshot and a stable privacy policy URL.
- Partner Center app identity is reflected in the package manifest.

No-Go if:

- Packaged app cannot register hotkeys.
- Mouse hook fails in packaged context.
- App fails Windows S / certification checks due to external process launching.
- Startup registration cannot be made compliant.
- WACK reports unresolved failures.
