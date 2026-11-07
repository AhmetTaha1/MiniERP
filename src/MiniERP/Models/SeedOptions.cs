namespace MiniERP.Models;

/// <summary>Bound from the "Seed" configuration section.</summary>
public class SeedOptions
{
    public const string SectionName = "Seed";

    public bool DemoData { get; set; } = true;
    public bool DemoUsers { get; set; } = true;
    public string AdminEmail { get; set; } = "admin@minierp.com";

    /// <summary>When empty, the admin account is not created (set it via user-secrets or environment variables).</summary>
    public string AdminPassword { get; set; } = string.Empty;

    public string DemoUserPassword { get; set; } = string.Empty;

    /// <summary>Shows the demo credentials on the login page (development only).</summary>
    public bool ShowDemoCredentials { get; set; }
}
