using TPGLLC_WebSite.Components;
using TPGLLC_WebSite.Models;
using TPGLLC_WebSite.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

builder.Services.Configure<GmailOptions>(builder.Configuration.GetSection("Gmail"));
builder.Services.AddTransient<IEmailService, SmtpEmailService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.MapPost("/contact/send", async (HttpRequest request, IEmailService emailService) =>
{
    var form = await request.ReadFormAsync();

    var message = new ContactMessage
    {
        Name = form["Name"],
        Phone = form["Phone"],
        Email = form["Email"],
        Body = form["Body"],
        Company = form["Company"]
    };

    // Honeypot: quietly ignore spam bots.
    if (!string.IsNullOrWhiteSpace(message.Company))
    {
        return Results.Redirect("/?contact=sent#contact");
    }

    if (string.IsNullOrWhiteSpace(message.Name) ||
        string.IsNullOrWhiteSpace(message.Phone) ||
        string.IsNullOrWhiteSpace(message.Email) ||
        string.IsNullOrWhiteSpace(message.Body))
    {
        return Results.Redirect("/?contact=missing#contact");
    }

    await emailService.SendContactMessageAsync(message);
    return Results.Redirect("/?contact=sent#contact");
})
.DisableAntiforgery();

app.MapGet("/version", (IWebHostEnvironment env) =>
{
    var versionPath = Path.Combine(env.ContentRootPath, "version.json");
    return File.Exists(versionPath)
        ? Results.File(versionPath, "application/json")
        : Results.NotFound();
});

app.MapGet("/health", () => Results.Text("OK", "text/plain"));

app.Run();