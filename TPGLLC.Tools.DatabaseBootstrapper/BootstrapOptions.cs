namespace TPGLLC.Tools.DatabaseBootstrapper;

public sealed class BootstrapOptions
{
    public string AdminEmail { get; set; } = "tomparlette@tomparlettegarage.org";

    public string AdminPassword { get; set; } = "Admin12345!";

    public string AdminRole { get; set; } = "Administrator";

    public List<string> Roles { get; set; } =
    [
        "Administrator",
        "Owner",
        "ServiceAdvisor",
        "Technician",
        "Finance",
        "Customer"
    ];
}
