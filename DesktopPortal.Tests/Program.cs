using DesktopPortal.Models;
using DesktopPortal.Services;
using DesktopPortal.Utilities;
using System.Windows.Input;

namespace DesktopPortal.Tests;

[TestClass]
public sealed class DesktopPortalTests
{
    [TestMethod]
    public void HotkeyParserAcceptsSupportedHotkeysAndNormalizesThem()
    {
        Assert.IsTrue(HotkeyParser.TryParse("F8", out var f8), "F8 should parse");
        Assert.AreEqual("F8", f8.ToString());

        Assert.IsTrue(HotkeyParser.TryParse("Ctrl+Alt+Q", out var ctrlAltQ), "Ctrl+Alt+Q should parse");
        Assert.AreEqual(HotkeyModifiers.Control | HotkeyModifiers.Alt, ctrlAltQ.Modifiers);
        Assert.AreEqual("Q", ctrlAltQ.Key);
        Assert.AreEqual("Ctrl+Alt+Q", ctrlAltQ.ToString());

        Assert.IsTrue(HotkeyParser.TryParse("Ctrl+Shift+1", out var ctrlShift1), "Ctrl+Shift+1 should parse");
        Assert.AreEqual("Ctrl+Shift+1", ctrlShift1.ToString());
    }

    [TestMethod]
    public void HotkeyParserAcceptsSingleDigitAndMouseButtonTriggers()
    {
        Assert.IsTrue(HotkeyParser.TryParse("1", out var digit), "single digit should parse");
        Assert.AreEqual(HotkeyTriggerKind.Keyboard, digit.Kind);
        Assert.AreEqual(HotkeyModifiers.None, digit.Modifiers);
        Assert.AreEqual("1", digit.ToString());

        Assert.IsTrue(HotkeyParser.TryParse("MouseMiddle", out var middle), "middle mouse should parse");
        Assert.AreEqual(HotkeyTriggerKind.Mouse, middle.Kind);
        Assert.AreEqual("MouseMiddle", middle.ToString());

        Assert.IsTrue(HotkeyParser.TryParse("MouseBack", out var back), "mouse back side button should parse");
        Assert.AreEqual("MouseBack", back.ToString());

        Assert.IsTrue(HotkeyParser.TryParse("MouseForward", out var forward), "mouse forward side button should parse");
        Assert.AreEqual("MouseForward", forward.ToString());
    }

    [TestMethod]
    public void HotkeyParserCapturesFunctionKeysAsSingleKeyShortcuts()
    {
        Assert.IsTrue(HotkeyParser.TryCreateFromKey(Key.F1, ModifierKeys.None, out var normalized, out _), "F1 should be capturable");
        Assert.AreEqual("F1", normalized);

        Assert.IsTrue(HotkeyParser.TryCreateFromKey(Key.F12, ModifierKeys.Control | ModifierKeys.Alt, out var normalizedWithNoisyModifiers, out _), "function keys should be captured as single keys");
        Assert.AreEqual("F12", normalizedWithNoisyModifiers);
    }

    [TestMethod]
    public void HotkeyParserRejectsDangerousAndUnsupportedHotkeys()
    {
        Assert.IsFalse(HotkeyParser.TryParse("Alt+Tab", out _), "Alt+Tab should be rejected");
        Assert.IsFalse(HotkeyParser.TryParse("Ctrl+Alt+Delete", out _), "Ctrl+Alt+Delete should be rejected");
        Assert.IsFalse(HotkeyParser.TryParse("Win+D", out _), "Win+D should be rejected");
        Assert.IsFalse(HotkeyParser.TryParse("Esc", out _), "Esc should be rejected");
        Assert.IsFalse(HotkeyParser.TryParse("Enter", out _), "Enter should be rejected");
        Assert.IsFalse(HotkeyParser.TryParse("Ctrl+Alt+Shift+Q", out _), "unsupported modifier combinations should be rejected");
        Assert.IsFalse(HotkeyParser.TryParse("A", out _), "letter single-key shortcuts should be rejected");
        Assert.IsFalse(HotkeyParser.TryParse("Ctrl+MouseBack", out _), "mouse buttons should not support modifiers");
    }

    [TestMethod]
    public void DragDropTargetResolverDetectsDroppedFilesFoldersAndExecutables()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "DesktopPortalDropTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var documentPath = Path.Combine(tempRoot, "document.pdf");
            File.WriteAllText(documentPath, "test");
            var exePath = Path.Combine(tempRoot, "tool.exe");
            File.WriteAllText(exePath, "test");

            Assert.IsTrue(DragDropTargetResolver.TryResolve(new[] { documentPath }, out var fileTarget), "file drop should resolve");
            Assert.AreEqual(TargetType.File, fileTarget.TargetType);
            Assert.AreEqual(documentPath, fileTarget.Target);

            Assert.IsTrue(DragDropTargetResolver.TryResolve(new[] { exePath }, out var exeTarget), "exe drop should resolve");
            Assert.AreEqual(TargetType.Exe, exeTarget.TargetType);
            Assert.AreEqual(exePath, exeTarget.Target);

            Assert.IsTrue(DragDropTargetResolver.TryResolve(new[] { tempRoot }, out var folderTarget), "folder drop should resolve");
            Assert.AreEqual(TargetType.Folder, folderTarget.TargetType);
            Assert.AreEqual(tempRoot, folderTarget.Target);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [TestMethod]
    public void PathValidatorValidatesUrlsAndLocalTargets()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "DesktopPortalTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var documentPath = Path.Combine(tempRoot, "note.txt");
            File.WriteAllText(documentPath, "test");
            var exePath = Path.Combine(tempRoot, "tool.exe");
            File.WriteAllText(exePath, "fake exe for path validation");

            Assert.IsTrue(PathValidator.ValidateTarget(TargetType.Url, "https://chatgpt.com").IsValid, "valid url should pass");
            Assert.IsFalse(PathValidator.ValidateTarget(TargetType.Url, "not a url").IsValid, "invalid url should fail");
            Assert.IsTrue(PathValidator.ValidateTarget(TargetType.File, documentPath).IsValid, "existing file should pass");
            Assert.IsTrue(PathValidator.ValidateTarget(TargetType.Folder, tempRoot).IsValid, "existing folder should pass");
            Assert.IsTrue(PathValidator.ValidateTarget(TargetType.Exe, exePath).IsValid, "existing exe path should pass");
            Assert.IsFalse(PathValidator.ValidateTarget(TargetType.Exe, documentPath).IsValid, "non-exe file should not pass as exe");
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [TestMethod]
    public void PortalRuleStatusTextUsesUserFacingEnabledWording()
    {
        var rule = new PortalRule { Enabled = true };

        rule.IsRegistered = true;
        Assert.AreEqual("已启用", rule.StatusText);

        rule.IsRegistered = false;
        Assert.AreEqual("未启用", rule.StatusText);

        rule.Enabled = false;
        Assert.AreEqual("已禁用", rule.StatusText);
    }

    [TestMethod]
    public void WindowMatchPlanBuildsRobustFileTitleCandidates()
    {
        var candidates = WindowMatchPlan.BuildFileTitleCandidates(@"C:\Docs\Quarter Plan.xlsx", "Finance Review");

        CollectionAssert.Contains(candidates.ToList(), "Finance Review");
        CollectionAssert.Contains(candidates.ToList(), "Quarter Plan.xlsx");
        CollectionAssert.Contains(candidates.ToList(), "Quarter Plan");
        Assert.IsTrue(WindowMatchPlan.TitleMatches("Quarter Plan.xlsx - Excel", candidates), "full filename should match Office-style titles");
        Assert.IsTrue(WindowMatchPlan.TitleMatches("Quarter Plan - Excel", candidates), "stem should match Office-style titles");
    }

    [TestMethod]
    public void WindowMatchPlanAvoidsBrowserMatchesForFileTargets()
    {
        var candidates = WindowMatchPlan.BuildFileTitleCandidates(@"C:\Docs\Quarter Plan.xlsx", null);

        Assert.IsTrue(WindowMatchPlan.FileWindowMatches("Quarter Plan.xlsx - Excel", "EXCEL", candidates));
        Assert.IsTrue(WindowMatchPlan.FileWindowMatches("Quarter Plan.pdf - Adobe Acrobat", "AcroRd32", candidates));
        Assert.IsFalse(WindowMatchPlan.FileWindowMatches("Quarter Plan - Search - Google Chrome", "chrome", candidates));
        Assert.IsFalse(WindowMatchPlan.FileWindowMatches("Quarter Plan - Microsoft Edge", "msedge", candidates));
    }

    [TestMethod]
    public void WindowMatchPlanBuildsUrlAppCandidates()
    {
        var candidates = WindowMatchPlan.BuildUrlTitleCandidates("https://chatgpt.com/codex", "ChatGPT");

        CollectionAssert.Contains(candidates.ToList(), "ChatGPT");
        CollectionAssert.Contains(candidates.ToList(), "chatgpt.com");
        Assert.IsTrue(WindowMatchPlan.TitleMatches("ChatGPT - Google Chrome", candidates), "title hint should match browser app window");
        Assert.IsTrue(WindowMatchPlan.TitleMatches("chatgpt.com", candidates), "host should match browser app window");
    }

    [TestMethod]
    public void WindowMatchPlanNormalizesPathsForStableIdentity()
    {
        Assert.IsTrue(
            WindowMatchPlan.PathsEqual(@"C:\Users\Public\Documents\", @"c:/users/public/documents"),
            "path comparison should ignore slash, trailing separator, and casing differences");
        Assert.IsTrue(
            WindowMatchPlan.PathsEqual(@"C:\Tools\App.exe", @"c:\tools\app.exe"),
            "executable identity should be based on normalized full path");
        Assert.IsFalse(
            WindowMatchPlan.PathsEqual(@"C:\Tools\App.exe", @"C:\Tools\Other.exe"),
            "different paths should not match");
    }

    [TestMethod]
    public void RuleContextMenuPolicyExposesUsefulRowGridAndColumnActions()
    {
        var enabledRule = new PortalRule { Name = "Docs", Enabled = true, Target = @"C:\Docs\plan.pdf", Hotkey = "F8" };
        var rowActions = RuleContextMenuPolicy.GetRowActions(enabledRule);

        CollectionAssert.Contains(rowActions.ToList(), RuleContextAction.TestRule);
        CollectionAssert.Contains(rowActions.ToList(), RuleContextAction.EditRule);
        CollectionAssert.Contains(rowActions.ToList(), RuleContextAction.DuplicateRule);
        CollectionAssert.Contains(rowActions.ToList(), RuleContextAction.DisableRule);
        CollectionAssert.Contains(rowActions.ToList(), RuleContextAction.CopyTarget);
        CollectionAssert.Contains(rowActions.ToList(), RuleContextAction.OpenTargetLocation);
        CollectionAssert.Contains(rowActions.ToList(), RuleContextAction.DeleteRule);

        var disabledRule = enabledRule.Clone();
        disabledRule.Enabled = false;
        CollectionAssert.Contains(RuleContextMenuPolicy.GetRowActions(disabledRule).ToList(), RuleContextAction.EnableRule);

        CollectionAssert.Contains(RuleContextMenuPolicy.GetGridActions(pauseAllHotkeys: false).ToList(), RuleContextAction.AddRule);
        CollectionAssert.Contains(RuleContextMenuPolicy.GetGridActions(pauseAllHotkeys: false).ToList(), RuleContextAction.PauseAllHotkeys);
        CollectionAssert.Contains(RuleContextMenuPolicy.GetGridActions(pauseAllHotkeys: true).ToList(), RuleContextAction.ResumeHotkeys);
        CollectionAssert.Contains(RuleContextMenuPolicy.GetColumnActions(canHideColumn: true).ToList(), RuleContextAction.HideColumn);
        CollectionAssert.Contains(RuleContextMenuPolicy.GetColumnActions(canHideColumn: true).ToList(), RuleContextAction.ShowAllColumns);
    }

    [TestMethod]
    public void ConfigServiceCreatesDefaultsAndRecoversBrokenJson()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "DesktopPortalConfigTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var service = new ConfigService(tempRoot);
            var created = service.Load();

            Assert.IsFalse(created.StartWithWindows, "default startWithWindows should be false");
            Assert.IsFalse(created.PauseAllHotkeys, "default pauseAllHotkeys should be false");
            Assert.IsTrue(File.Exists(Path.Combine(tempRoot, "config.json")), "missing config should be created");

            File.WriteAllText(Path.Combine(tempRoot, "config.json"), "{broken json");
            var recovered = service.Load();

            Assert.IsFalse(recovered.PauseAllHotkeys, "broken config should recover to defaults");
            Assert.HasCount(1, Directory.GetFiles(tempRoot, "config.broken.*.json"), "broken config should be backed up");
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [TestMethod]
    public void ConfigServiceTrySaveReportsFailureWithoutThrowing()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "DesktopPortalConfigBlocked", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            Directory.CreateDirectory(Path.Combine(tempRoot, "config.json"));
            var service = new ConfigService(tempRoot);

            var result = service.TrySave(ConfigService.CreateDefaultConfig());

            Assert.IsFalse(result.Success);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [TestMethod]
    public void LoggerRotatesLargeLogFiles()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "DesktopPortalLogTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            Logger.Initialize(tempRoot, maxLogBytes: 180, maxArchiveFiles: 2);

            Logger.Info(new string('a', 220));
            Logger.Info("after rotation");

            var logDirectory = Path.Combine(tempRoot, "logs");
            Assert.IsTrue(File.Exists(Path.Combine(logDirectory, "app.log")));
            Assert.IsGreaterThanOrEqualTo(Directory.GetFiles(logDirectory, "app.*.log").Length, 1, "rotated log should be archived");
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [TestMethod]
    public void RuleHealthCheckerMarksInvalidTargetsAndDuplicateHotkeys()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "DesktopPortalHealthTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var existingFile = Path.Combine(tempRoot, "existing.pdf");
            File.WriteAllText(existingFile, "test");

            var firstDuplicate = new PortalRule
            {
                Name = "First",
                Hotkey = "F8",
                TargetType = TargetType.File,
                Target = existingFile,
                Enabled = true
            };
            var secondDuplicate = new PortalRule
            {
                Name = "Second",
                Hotkey = "F8",
                TargetType = TargetType.Url,
                Target = "https://example.com",
                Enabled = true
            };
            var missingTarget = new PortalRule
            {
                Name = "Missing",
                Hotkey = "F9",
                TargetType = TargetType.File,
                Target = Path.Combine(tempRoot, "missing.pdf"),
                Enabled = true
            };

            var checker = new RuleHealthChecker();
            var issues = checker.CheckRules(
                new[] { firstDuplicate, secondDuplicate, missingTarget },
                pauseAllHotkeys: false);

            Assert.IsTrue(issues.Any(issue => issue.Kind == RuleHealthIssueKind.DuplicateHotkey && issue.RuleId == firstDuplicate.Id));
            Assert.IsTrue(issues.Any(issue => issue.Kind == RuleHealthIssueKind.DuplicateHotkey && issue.RuleId == secondDuplicate.Id));
            Assert.IsTrue(issues.Any(issue => issue.Kind == RuleHealthIssueKind.InvalidTarget && issue.RuleId == missingTarget.Id));

            var appliedCount = checker.Apply(new[] { firstDuplicate, secondDuplicate, missingTarget }, pauseAllHotkeys: false);
            Assert.AreEqual(3, appliedCount);
            Assert.IsTrue(firstDuplicate.HasIssue);
            Assert.IsTrue(secondDuplicate.HasIssue);
            Assert.IsTrue(missingTarget.HasIssue);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [TestMethod]
    public void ConfigServiceCreatesRetainedBackupsBeforeReplacingExistingConfig()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "DesktopPortalBackupTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var service = new ConfigService(tempRoot);
            var config = ConfigService.CreateDefaultConfig();
            Assert.IsTrue(service.TrySave(config).Success);

            for (var i = 0; i < ConfigService.DefaultConfigBackupRetention + 2; i++)
            {
                config.Rules = new List<PortalRule>
                {
                    new()
                    {
                        Name = $"Rule {i}",
                        Hotkey = $"F{Math.Min(i + 1, 12)}",
                        TargetType = TargetType.Url,
                        Target = $"https://example.com/{i}",
                        Enabled = true
                    }
                };

                Assert.IsTrue(service.TrySave(config).Success);
                Thread.Sleep(2);
            }

            var backupDirectory = Path.Combine(tempRoot, "backups");
            var backups = Directory.GetFiles(backupDirectory, "config.backup.*.json");
            Assert.HasCount(ConfigService.DefaultConfigBackupRetention, backups);
            Assert.IsTrue(backups.All(path => new FileInfo(path).Length > 0));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [TestMethod]
    public void CrashReporterFormatsUnhandledExceptionsForFallbackLogging()
    {
        var exceptionText = CrashReporter.FormatUnhandledException(
            "TaskScheduler.UnobservedTaskException",
            new InvalidOperationException("boom"));

        StringAssert.Contains(exceptionText, "TaskScheduler.UnobservedTaskException");
        StringAssert.Contains(exceptionText, nameof(InvalidOperationException));
        StringAssert.Contains(exceptionText, "boom");

        var objectText = CrashReporter.FormatUnhandledException("AppDomain.UnhandledException", "plain failure");

        StringAssert.Contains(objectText, "AppDomain.UnhandledException");
        StringAssert.Contains(objectText, "plain failure");
    }

    [TestMethod]
    public void SingleInstanceNamesAreStableAndScoped()
    {
        var names = SingleInstanceService.CreateNames("DesktopPortal.ReleaseTest");

        Assert.AreEqual(@"Local\DesktopPortal.ReleaseTest.Mutex", names.MutexName);
        Assert.AreEqual(@"Local\DesktopPortal.ReleaseTest.Activate", names.ActivationEventName);
    }

    [TestMethod]
    public void ReleasePublishScriptUsesSelfContainedWinX64Release()
    {
        var scriptPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "scripts", "publish-release.ps1"));

        Assert.IsTrue(File.Exists(scriptPath), "release publish script should exist");
        var script = File.ReadAllText(scriptPath);

        StringAssert.Contains(script, "dotnet publish");
        StringAssert.Contains(script, "-c Release");
        StringAssert.Contains(script, "-r win-x64");
        StringAssert.Contains(script, "--self-contained true");
        StringAssert.Contains(script, "DesktopPortal-$Version-$Runtime");
        StringAssert.Contains(script, "Compress-Archive");
    }

    [TestMethod]
    public void ProjectHasReleaseVersionMetadata()
    {
        var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DesktopPortal", "DesktopPortal.csproj"));
        var project = File.ReadAllText(projectPath);

        StringAssert.Contains(project, "<Version>0.1.0-beta</Version>");
        StringAssert.Contains(project, "<FileVersion>0.1.0.0</FileVersion>");
        StringAssert.Contains(project, "<Product>Desktop Portal</Product>");
        StringAssert.Contains(project, "<Description>");
    }

    [TestMethod]
    public void ReleaseDocumentationExistsAndContainsRequiredSections()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var requiredFiles = new[]
        {
            "README.md",
            "docs\\privacy.md",
            "docs\\qa-checklist.md",
            "docs\\release-notes\\v0.1.0-beta.md",
            "installer\\DesktopPortal.iss"
        };

        foreach (var relativePath in requiredFiles)
        {
            Assert.IsTrue(File.Exists(Path.Combine(root, relativePath)), $"{relativePath} should exist");
        }

        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        StringAssert.Contains(readme, "Desktop Portal");
        StringAssert.Contains(readme, "不联网");
        StringAssert.Contains(readme, "%AppData%\\DesktopPortal\\config.json");

        var privacy = File.ReadAllText(Path.Combine(root, "docs", "privacy.md"));
        StringAssert.Contains(privacy, "不采集");
        StringAssert.Contains(privacy, "不联网");

        var qa = File.ReadAllText(Path.Combine(root, "docs", "qa-checklist.md"));
        StringAssert.Contains(qa, "快捷键冲突");
        StringAssert.Contains(qa, "托盘");
        StringAssert.Contains(qa, "开机自启");

        var installer = File.ReadAllText(Path.Combine(root, "installer", "DesktopPortal.iss"));
        StringAssert.Contains(installer, "AppVersion=0.1.0-beta");
        StringAssert.Contains(installer, "DesktopPortal.exe");
    }

    [TestMethod]
    public void ProjectDesignDocumentCapturesDownloadedDesignReference()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var designPath = Path.Combine(root, "DESIGN.md");

        Assert.IsTrue(File.Exists(designPath), "DESIGN.md should document the Desktop Portal visual system");
        var design = File.ReadAllText(designPath);

        StringAssert.Contains(design, "Desktop Portal");
        StringAssert.Contains(design, "awesome-design-md");
        StringAssert.Contains(design, "Raycast");
        StringAssert.Contains(design, "PortalShell");
        StringAssert.Contains(design, "Keycap");
    }

    [TestMethod]
    public void MainWindowUsesCommandCenterDesignTokens()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var xaml = File.ReadAllText(Path.Combine(root, "DesktopPortal", "MainWindow.xaml"));

        StringAssert.Contains(xaml, "PortalShellBrush");
        StringAssert.Contains(xaml, "CommandButtonStyle");
        StringAssert.Contains(xaml, "KeycapBadgeStyle");
        StringAssert.Contains(xaml, "StatusPillStyle");
        StringAssert.Contains(xaml, "规则命令中心");
    }
}
