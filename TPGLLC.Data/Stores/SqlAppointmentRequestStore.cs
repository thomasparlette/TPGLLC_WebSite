using TPGLLC.Data.Entities;

namespace TPGLLC.Data.Stores;

public sealed class SqlAppointmentRequestStore : IAppointmentRequestStore
{
    private readonly TPGLLCDbContext _db;

    public SqlAppointmentRequestStore(TPGLLCDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(AppointmentRequest request, CancellationToken cancellationToken = default)
    {
        _db.AppointmentRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);
    }
}