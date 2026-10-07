namespace SMSNotificationSystem.ViewModels
{
    /// <summary>Model for Views/Shared/_NavLink.cshtml.</summary>
    public record NavLinkModel(string Controller, string Action, string Icon, string Text);

    /// <summary>Model for Views/Shared/_EmptyState.cshtml.</summary>
    public record EmptyStateModel(string Icon, string Title, string Text);
}
