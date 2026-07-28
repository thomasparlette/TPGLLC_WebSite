namespace TPGLLC.Application.Vehicles;

public sealed class VehicleMakeDto
{
    public int MakeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ModelCount { get; set; }
}