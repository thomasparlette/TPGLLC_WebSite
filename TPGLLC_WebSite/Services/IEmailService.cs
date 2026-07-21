using TPGLLC_WebSite.Models;

namespace TPGLLC_WebSite.Services;

public interface IEmailService
{
    Task SendContactMessageAsync(ContactMessage message, CancellationToken cancellationToken = default);
}