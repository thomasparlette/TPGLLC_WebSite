using TPGLLC_WebSite.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();
app.MapGet("/version", (IWebHostEnvironment env) =>
{
    var versionPath = Path.Combine(env.ContentRootPath, "version.json");

    if (!File.Exists(versionPath))
    {
        return Results.NotFound();
    }

    return Results.File(versionPath, "application/json");
});

app.MapGet("/health", () => Results.Text("OK", "text/plain"));

app.Run();
