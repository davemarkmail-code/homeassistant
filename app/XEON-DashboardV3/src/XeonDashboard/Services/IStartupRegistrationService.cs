namespace XeonDashboard.Services;

/// <summary>
/// Manages "launch at sign-in" via a shortcut in the user's Startup folder.
/// No administrator rights required, which keeps the appliance easy to deploy.
/// </summary>
public interface IStartupRegistrationService
{
    /// <summary>True when the Startup-folder shortcut currently exists.</summary>
    bool IsEnabled();

    /// <summary>Creates or removes the Startup-folder shortcut.</summary>
    void SetEnabled(bool enabled);
}
