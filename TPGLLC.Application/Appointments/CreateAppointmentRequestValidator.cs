using FluentValidation;

namespace TPGLLC.Application.Appointments;

public sealed class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);

        RuleFor(x => x.VehicleType).NotEmpty().MaximumLength(30);

        When(x => !string.Equals(x.VehicleType, "Other", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.VehicleYear).NotEmpty().MaximumLength(20);
            RuleFor(x => x.VehicleMake).NotEmpty().MaximumLength(120);
            RuleFor(x => x.VehicleModel).NotEmpty().MaximumLength(120);
        });

        RuleFor(x => x.Vin).MaximumLength(17);
        RuleFor(x => x.Mileage).MaximumLength(50);
        RuleFor(x => x.PreferredDate).NotEmpty().MaximumLength(20);
        RuleFor(x => x.PreferredTime).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ServiceNeeded).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
    }
}