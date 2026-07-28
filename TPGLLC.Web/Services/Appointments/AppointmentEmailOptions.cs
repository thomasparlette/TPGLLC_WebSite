namespace TPGLLC.Web.Services.Appointments;

public sealed class AppointmentEmailOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Tom Parlette Garage LLC";
    public string ToAddress { get; set; } = string.Empty;

    public string ShopName { get; set; } = "Tom Parlette Garage LLC";
    public string Tagline { get; set; } = "Built on Trust. Driven by Results.";
    public string WebsiteUrl { get; set; } = "https://tomparlettegarage.org/";
    public string LogoUrl { get; set; } = "https://tomparlettegarage.org/images/logo.jpeg";
    public string ShopPhone { get; set; } = "(765) 346-3354";
    public string ShopEmail { get; set; } = "tomparlette@tomparlettegarage.org";
}