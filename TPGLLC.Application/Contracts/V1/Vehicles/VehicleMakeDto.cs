namespace TPGLLC.Application.Contracts.V1.Vehicles;

public sealed class VehicleMakeDto
{
    public int MakeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ModelCount { get; set; }
}