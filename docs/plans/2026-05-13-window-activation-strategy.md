# Window Activation Strategy Implementation Plan

> **For Codex:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development to implement this plan task-by-task.

**Goal:** Make every rule type switch to an already-open target more reliably before opening a new instance.

**Architecture:** Keep the Win32 code inside `WindowActivator`, add pure match planning helpers that can be unit-tested without GUI automation, and let `TargetExecutor` call target-specific activation methods. Cache the last successful window handle per rule in memory for the fastest repeat path.

**Tech Stack:** C#/.NET/WPF, Win32 `user32.dll`, Shell COM for Explorer folder path inspection, local console smoke tests.

---

### Task 1: Add Matching Regression Tests

**Files:**
- Modify: `DesktopPortal.Tests/Program.cs`
- Create: `DesktopPortal/Services/WindowMatchPlan.cs`

**Steps:**
1. Add tests for file title candidates, URL app candidates, executable path identity, and folder path normalization.
2. Run `dotnet run --project DesktopPortal.Tests\DesktopPortal.Tests.csproj` and verify the new tests fail because `WindowMatchPlan` does not exist yet.

### Task 2: Implement Pure Match Planning

**Files:**
- Create: `DesktopPortal/Services/WindowMatchPlan.cs`
- Modify: `DesktopPortal/Services/WindowActivator.cs`

**Steps:**
1. Implement title candidate generation and normalized path comparison.
2. Expose activation methods for rule id + file/url/folder/exe that use the match plan.
3. Keep all Win32 enumeration inside `WindowActivator`.

### Task 3: Add Cached Handles and Stronger Foreground Activation

**Files:**
- Modify: `DesktopPortal/Services/WindowActivator.cs`

**Steps:**
1. Cache the successful `HWND` by rule id.
2. Before scanning all windows, try the cached handle if it is still a valid visible window and still matches the target.
3. Improve `BringToFront` with restore, `AttachThreadInput`, and foreground verification.

### Task 4: Wire Target Execution

**Files:**
- Modify: `DesktopPortal/Services/TargetExecutor.cs`

**Steps:**
1. Use the new target-specific activation methods for URL app, file, folder, and exe.
2. Log activation miss versus open fallback clearly.
3. Preserve existing validation and fallback opening behavior.

### Task 5: Verify

**Commands:**
- `dotnet run --project DesktopPortal.Tests\DesktopPortal.Tests.csproj`
- `dotnet build DesktopPortal.slnx`
- Launch and stop the WPF app as a smoke test.
