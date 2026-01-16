namespace Outsource.Modules.AI.Scripts
{
    using System;

    public static class IssueEventBus
    {
        public static event Action<BehaviourTree, IssueType> OnIssueTriggered;
        public static event Action<BehaviourTree, IssueType>   OnResolveIssue;

        public static void Publish(BehaviourTree sender, IssueType type)
        {
            OnIssueTriggered?.Invoke(sender, type);
        }

        public static void ResolveIssue(BehaviourTree sender, IssueType issueType)
        {
            OnResolveIssue?.Invoke(sender, issueType);
        }
    }
}