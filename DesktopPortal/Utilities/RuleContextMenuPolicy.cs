using DesktopPortal.Models;

namespace DesktopPortal.Utilities;

public static class RuleContextMenuPolicy
{
    public static IReadOnlyList<RuleContextAction> GetRowActions(PortalRule rule)
    {
        var toggleAction = rule.Enabled ? RuleContextAction.DisableRule : RuleContextAction.EnableRule;
        return
        [
            RuleContextAction.TestRule,
            RuleContextAction.EditRule,
            RuleContextAction.DuplicateRule,
            toggleAction,
            RuleContextAction.CopyTarget,
            RuleContextAction.CopyHotkey,
            RuleContextAction.OpenTargetLocation,
            RuleContextAction.DeleteRule
        ];
    }

    public static IReadOnlyList<RuleContextAction> GetGridActions(bool pauseAllHotkeys)
    {
        return
        [
            RuleContextAction.AddRule,
            pauseAllHotkeys ? RuleContextAction.ResumeHotkeys : RuleContextAction.PauseAllHotkeys,
            RuleContextAction.ReloadConfig
        ];
    }

    public static IReadOnlyList<RuleContextAction> GetColumnActions(bool canHideColumn)
    {
        return canHideColumn
            ? [RuleContextAction.HideColumn, RuleContextAction.ShowAllColumns]
            : [RuleContextAction.ShowAllColumns];
    }
}
