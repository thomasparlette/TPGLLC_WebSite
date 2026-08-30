using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TPGLLC.Data;
using TPGLLC.Shared.Identity;
using TPGLLC.Web.Components;

namespace TPGLLC.Web.Infrastructure;

public static class TpgllcApplicationBuilderExtensions
{
    public static WebApplication UseTpgllcPipeline(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        if (app.Configuration.GetValue<bool?>("HttpsRedirection:Enabled") != false)
        {
            app.UseHttpsRedirection();
        }
        app.UseRouting();

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.MapRazorPages();
        app.MapStaticAssets();

        app.MapRazorComponents<App>()
           .AddInteractiveServerRenderMode();

        app.MapGet("/logout", async (HttpContext context, SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();

            context.Response.Cookies.Delete(".AspNetCore.Identity.Application");
            context.Response.Cookies.Delete("TPGLLC.Identity");

            return Results.LocalRedirect("/");
        }).AllowAnonymous();

        app.MapGet("/health", () => Results.Text("OK", "text/plain"))
           .AllowAnonymous();

        app.MapGet("/version", (IWebHostEnvironment env) =>
        {
            var versionPath = Path.Combine(env.ContentRootPath, "version.json");
            return File.Exists(versionPath)
                ? Results.File(versionPath, "application/json")
                : Results.NotFound();
        }).AllowAnonymous();

        app.MapGet("/db", (TPGLLCDbContext db) =>
        {
            return db.Database.GetConnectionString();
        });



        return app;
    }
}
