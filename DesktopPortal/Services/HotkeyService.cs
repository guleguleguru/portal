using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DesktopPortal.Models;
using DesktopPortal.Utilities;

namespace DesktopPortal.Services;

public sealed class HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int WhMouseLl = 14;
    private const int WmMbuttonDown = 0x0207;
    private const int WmXbuttonDown = 0x020B;
    private const int Xbutton1 = 0x0001;
    private const int Xbutton2 = 0x0002;
    private const uint ModNoRepeat = 0x4000;

    private readonly Dictionary<int, string> _idToRuleId = new();
    private readonly Dictionary<string, string> _mouseKeyToRuleId = new(StringComparer.OrdinalIgnoreCase);
    private readonly LowLevelMouseProc _mouseProc;
    private SynchronizationContext? _callbackContext;
    private HwndSource? _source;
    private IntPtr _windowHandle;
    private IntPtr _mouseHookHandle;
    private int _nextHotkeyId = 1000;
    private int _registeredCount;
    private int _conflictCount;

    public HotkeyService()
    {
        _mouseProc = MouseHookCallback;
    }

    public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

    public int RegisteredCount => _registeredCount;

    public int ConflictCount => _conflictCount;

    public void Attach(Window window)
    {
        if (_source is not null)
        {
            return;
        }

        _windowHandle = new WindowInteropHelper(window).EnsureHandle();
        _callbackContext = SynchronizationContext.Current;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(WndProc);
    }

    public void RegisterRules(IEnumerable<PortalRule> rules, bool pauseAllHotkeys)
    {
        UnregisterAll();
        _registeredCount = 0;
        _conflictCount = 0;

        foreach (var rule in rules)
        {
            rule.IsRegistered = false;
            rule.RegistrationError = null;
        }

        if (pauseAllHotkeys)
        {
            Logger.Info("All hotkeys paused.");
            return;
        }

        var normalizedToRule = new Dictionary<string, PortalRule>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules.Where(r => r.Enabled))
        {
            if (!HotkeyParser.TryParse(rule.Hotkey, out var hotkey))
            {
                MarkConflict(rule, "快捷键格式无效");
                continue;
            }

            rule.Hotkey = hotkey.ToString();
            if (normalizedToRule.TryGetValue(rule.Hotkey, out var existing))
            {
                MarkConflict(rule, "快捷键重复");
                MarkConflict(existing, "快捷键重复");
                continue;
            }

            normalizedToRule[rule.Hotkey] = rule;
        }

        foreach (var pair in normalizedToRule)
        {
            var rule = pair.Value;
            if (rule.HasConflict || !HotkeyParser.TryParse(rule.Hotkey, out var hotkey))
            {
                continue;
            }

            if (hotkey.IsMouse)
            {
                _mouseKeyToRuleId[hotkey.Key] = rule.Id;
                rule.IsRegistered = true;
                rule.RegistrationError = null;
                _registeredCount++;
                Logger.Info($"Mouse hotkey registered: {rule.Name} {rule.Hotkey}");
                continue;
            }

            var id = _nextHotkeyId++;
            var modifiers = (uint)hotkey.Modifiers | ModNoRepeat;
            if (!RegisterHotKey(_windowHandle, id, modifiers, hotkey.VirtualKey))
            {
                var error = new Win32Exception(Marshal.GetLastWin32Error());
                MarkConflict(rule, "该快捷键可能已被系统或其他程序占用");
                Logger.Warn($"Hotkey register failed: {rule.Name} {rule.Hotkey}. {error.Message}");
                continue;
            }

            _idToRuleId[id] = rule.Id;
            rule.IsRegistered = true;
            rule.RegistrationError = null;
            _registeredCount++;
            Logger.Info($"Hotkey registered: {rule.Name} {rule.Hotkey} id={id}");
        }

        if (_mouseKeyToRuleId.Count > 0 && !InstallMouseHook())
        {
            foreach (var rule in normalizedToRule.Values.Where(r => HotkeyParser.TryParse(r.Hotkey, out var hotkey) && hotkey.IsMouse))
            {
                MarkConflict(rule, "鼠标快捷键注册失败");
                if (_registeredCount > 0)
                {
                    _registeredCount--;
                }
            }

            _mouseKeyToRuleId.Clear();
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _idToRuleId.Keys.ToList())
        {
            try
            {
                UnregisterHotKey(_windowHandle, id);
                Logger.Info($"Hotkey unregistered: id={id}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to unregister hotkey id={id}", ex);
            }
        }

        _idToRuleId.Clear();
        _mouseKeyToRuleId.Clear();
        UninstallMouseHook();
        _registeredCount = 0;
    }

    public void Dispose()
    {
        UnregisterAll();
        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
            _source = null;
        }
    }

    private void MarkConflict(PortalRule rule, string message)
    {
        if (!rule.HasConflict)
        {
            _conflictCount++;
        }

        rule.IsRegistered = false;
        rule.RegistrationError = message;
        Logger.Warn($"Hotkey conflict: {rule.Name} {rule.Hotkey} {message}");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey)
        {
            var id = wParam.ToInt32();
            if (_idToRuleId.TryGetValue(id, out var ruleId))
            {
                handled = true;
                Logger.Info($"Hotkey triggered: id={id} rule={ruleId}");
                RaiseHotkeyPressed(ruleId);
            }
        }

        return IntPtr.Zero;
    }

    private bool InstallMouseHook()
    {
        if (_mouseHookHandle != IntPtr.Zero)
        {
            return true;
        }

        try
        {
            using var currentProcess = Process.GetCurrentProcess();
            using var currentModule = currentProcess.MainModule;
            var moduleHandle = currentModule?.ModuleName is null ? IntPtr.Zero : GetModuleHandle(currentModule.ModuleName);
            _mouseHookHandle = SetWindowsHookEx(WhMouseLl, _mouseProc, moduleHandle, 0);
            if (_mouseHookHandle == IntPtr.Zero)
            {
                var error = new Win32Exception(Marshal.GetLastWin32Error());
                Logger.Warn($"Mouse hook install failed: {error.Message}");
                return false;
            }

            Logger.Info("Mouse hook installed.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Mouse hook install exception.", ex);
            return false;
        }
    }

    private void UninstallMouseHook()
    {
        if (_mouseHookHandle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            UnhookWindowsHookEx(_mouseHookHandle);
            Logger.Info("Mouse hook uninstalled.");
        }
        catch (Exception ex)
        {
            Logger.Error("Mouse hook uninstall failed.", ex);
        }
        finally
        {
            _mouseHookHandle = IntPtr.Zero;
        }
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && TryGetMouseHotkeyKey(wParam, lParam, out var mouseKey) && _mouseKeyToRuleId.TryGetValue(mouseKey, out var ruleId))
        {
            Logger.Info($"Mouse hotkey triggered: {mouseKey} rule={ruleId}");
            PostHotkeyPressed(ruleId);
            return new IntPtr(1);
        }

        return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
    }

    private void RaiseHotkeyPressed(string ruleId)
    {
        HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(ruleId));
    }

    private void PostHotkeyPressed(string ruleId)
    {
        var context = _callbackContext;
        if (context is null)
        {
            ThreadPool.QueueUserWorkItem(_ => RaiseHotkeyPressed(ruleId));
            return;
        }

        context.Post(_ => RaiseHotkeyPressed(ruleId), null);
    }

    private static bool TryGetMouseHotkeyKey(IntPtr wParam, IntPtr lParam, out string mouseKey)
    {
        mouseKey = string.Empty;
        var message = wParam.ToInt32();
        if (message == WmMbuttonDown)
        {
            mouseKey = "MouseMiddle";
            return true;
        }

        if (message != WmXbuttonDown)
        {
            return false;
        }

        var hookStruct = Marshal.PtrToStructure<Msllhookstruct>(lParam);
        var xButton = (hookStruct.MouseData >> 16) & 0xffff;
        mouseKey = xButton switch
        {
            Xbutton1 => "MouseBack",
            Xbutton2 => "MouseForward",
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(mouseKey);
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msllhookstruct
    {
        public Point Pt;
        public int MouseData;
        public int Flags;
        public int Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
