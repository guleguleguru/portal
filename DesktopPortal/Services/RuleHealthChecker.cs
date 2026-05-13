using DesktopPortal.Models;
using DesktopPortal.Utilities;

namespace DesktopPortal.Services;

public sealed class RuleHealthChecker
{
    public IReadOnlyList<RuleHealthIssue> CheckRules(IEnumerable<PortalRule> rules, bool pauseAllHotkeys)
    {
        if (pauseAllHotkeys)
        {
            return Array.Empty<RuleHealthIssue>();
        }

        var enabledRules = rules.Where(rule => rule.Enabled).ToList();
        var issues = new List<RuleHealthIssue>();
        var normalizedHotkeys = new Dictionary<string, List<PortalRule>>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in enabledRules)
        {
            if (!HotkeyParser.TryNormalize(rule.Hotkey, out var normalizedHotkey))
            {
                issues.Add(CreateIssue(rule, RuleHealthIssueKind.InvalidHotkey, "快捷键格式无效"));
            }
            else
            {
                if (!normalizedHotkeys.TryGetValue(normalizedHotkey, out var hotkeyRules))
                {
                    hotkeyRules = new List<PortalRule>();
                    normalizedHotkeys[normalizedHotkey] = hotkeyRules;
                }

                hotkeyRules.Add(rule);
            }

            var targetValidation = PathValidator.ValidateTarget(rule.TargetType, rule.Target);
            if (!targetValidation.IsValid)
            {
                issues.Add(CreateIssue(rule, RuleHealthIssueKind.InvalidTarget, $"目标无效：{targetValidation.Message}"));
            }
        }

        foreach (var duplicateGroup in normalizedHotkeys.Values.Where(group => group.Count > 1))
        {
            foreach (var rule in duplicateGroup)
            {
                issues.Add(CreateIssue(rule, RuleHealthIssueKind.DuplicateHotkey, "快捷键重复"));
            }
        }

        return issues;
    }

    public int Apply(IEnumerable<PortalRule> rules, bool pauseAllHotkeys)
    {
        var ruleList = rules.ToList();
        foreach (var rule in ruleList)
        {
            rule.HealthError = null;
        }

        var issues = CheckRules(ruleList, pauseAllHotkeys);
        foreach (var issueGroup in issues.GroupBy(issue => issue.RuleId))
        {
            var rule = ruleList.FirstOrDefault(candidate => candidate.Id == issueGroup.Key);
            if (rule is not null)
            {
                rule.HealthError = issueGroup.First().Message;
            }
        }

        return issues.Count;
    }

    private static RuleHealthIssue CreateIssue(PortalRule rule, RuleHealthIssueKind kind, string message)
    {
        return new RuleHealthIssue(rule.Id, rule.Name, kind, message);
    }
}

public sealed record RuleHealthIssue(
    string RuleId,
    string RuleName,
    RuleHealthIssueKind Kind,
    string Message);

public enum RuleHealthIssueKind
{
    InvalidHotkey,
    DuplicateHotkey,
    InvalidTarget
}
