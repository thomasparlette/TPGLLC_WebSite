namespace TPGLLC.Web.Services.Portal;

public sealed class BuildEnvironmentService : IBuildEnvironmentService
{
    private readonly bool _isBuildEnvironment;

    public BuildEnvironmentService(IWebHostEnvironment environment)
    {
        _isBuildEnvironment = environment.IsEnvironment("Build");
    }

    public bool IsBuildEnvironment => _isBuildEnvironment;
}