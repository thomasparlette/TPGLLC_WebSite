namespace TPGLLC.Services;

public sealed class GmailOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Tom Parlette Garage LLC";
    public string ToAddress { get; set; } = "";
}