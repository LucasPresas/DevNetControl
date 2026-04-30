using FluentValidation;
using DevNetControl.Api.Controllers;

namespace DevNetControl.Api.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MinimumLength(3).WithMessage("El nombre de usuario debe tener al menos 3 caracteres.")
            .MaximumLength(50).WithMessage("El nombre de usuario no puede tener mas de 50 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("La contraseña actual es obligatoria.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La nueva contraseña debe tener al menos 6 caracteres.")
            .Matches(@"[A-Z]").WithMessage("La nueva contraseña debe contener al menos una mayuscula.")
            .Matches(@"[a-z]").WithMessage("La nueva contraseña debe contener al menos una minuscula.")
            .Matches(@"[0-9]").WithMessage("La nueva contraseña debe contener al menos un numero.")
            .NotEqual(x => x.CurrentPassword).WithMessage("La nueva contraseña debe ser diferente a la actual.");
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MinimumLength(3).WithMessage("El nombre de usuario debe tener al menos 3 caracteres.")
            .MaximumLength(50).WithMessage("El nombre de usuario no puede tener mas de 50 caracteres.")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("El nombre de usuario solo puede contener letras, numeros y guiones bajos.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("El plan es obligatorio.");
    }
}

public class CreatePlanRequestValidator : AbstractValidator<CreatePlanRequest>
{
    public CreatePlanRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del plan es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede tener mas de 100 caracteres.");

        RuleFor(x => x.DurationHours)
            .GreaterThan(0).WithMessage("La duracion debe ser mayor a cero.")
            .LessThanOrEqualTo(87600).WithMessage("La duracion maxima es 87600 horas (10 anios).");

        RuleFor(x => x.CreditCost)
            .GreaterThanOrEqualTo(0).WithMessage("El costo en creditos no puede ser negativo.");

        RuleFor(x => x.MaxConnections)
            .GreaterThan(0).WithMessage("Las conexiones deben ser mayor a cero.")
            .LessThanOrEqualTo(100).WithMessage("El maximo de conexiones es 100.");

        RuleFor(x => x.MaxDevices)
            .GreaterThan(0).WithMessage("Los dispositivos deben ser mayor a cero.")
            .LessThanOrEqualTo(100).WithMessage("El maximo de dispositivos es 100.");
    }
}

public class UpdatePlanRequestValidator : AbstractValidator<UpdatePlanRequest>
{
    public UpdatePlanRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("El nombre no puede tener mas de 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.DurationHours)
            .GreaterThan(0).WithMessage("La duracion debe ser mayor a cero.")
            .When(x => x.DurationHours.HasValue);

        RuleFor(x => x.CreditCost)
            .GreaterThanOrEqualTo(0).WithMessage("El costo no puede ser negativo.")
            .When(x => x.CreditCost.HasValue);
    }
}

public class ExtendServiceRequestValidator : AbstractValidator<ExtendServiceRequest>
{
    public ExtendServiceRequestValidator()
    {
        RuleFor(x => x.Days)
            .GreaterThan(0).WithMessage("Los dias deben ser mayores a cero.")
            .LessThanOrEqualTo(365).WithMessage("El maximo de dias es 365.");
    }
}

public class UpdateMyUserRequestValidator : AbstractValidator<UpdateMyUserRequest>
{
    public UpdateMyUserRequestValidator()
    {
        RuleFor(x => x.UserName)
            .MaximumLength(50).WithMessage("El nombre de usuario no puede tener mas de 50 caracteres.")
            .Matches(@"^[a-zA-Z0-9_]*$").WithMessage("El nombre de usuario solo puede contener letras, numeros y guiones bajos.")
            .When(x => !string.IsNullOrEmpty(x.UserName));

        RuleFor(x => x.Password)
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}

public class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    public TransferRequestValidator()
    {
        RuleFor(x => x.ToUserId)
            .NotEmpty().WithMessage("El ID del destinatario es obligatorio.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a cero.")
            .LessThanOrEqualTo(999999999).WithMessage("El monto excede el limite permitido.");
    }
}

public class CreateNodeRequestValidator : AbstractValidator<CreateNodeRequest>
{
    public CreateNodeRequestValidator()
    {
        RuleFor(x => x.IP)
            .NotEmpty().WithMessage("La IP es obligatoria.")
            .Matches(@"^(\d{1,3}\.){3}\d{1,3}$").WithMessage("La IP debe tener un formato valido (ej: 192.168.1.1)");

        RuleFor(x => x.SshPort)
            .InclusiveBetween(1, 65535).WithMessage("El puerto SSH debe estar entre 1 y 65535.");

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("La etiqueta es obligatoria.")
            .MaximumLength(100).WithMessage("La etiqueta no puede tener mas de 100 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña del nodo es obligatoria.")
            .MinimumLength(4).WithMessage("La contraseña del nodo debe tener al menos 4 caracteres.");

        RuleFor(x => x.CreditCost)
            .GreaterThanOrEqualTo(0).WithMessage("El costo en creditos no puede ser negativo.");
    }
}

public class UpdateNodeRequestValidator : AbstractValidator<UpdateNodeRequest>
{
    public UpdateNodeRequestValidator()
    {
        RuleFor(x => x.IP)
            .Matches(@"^(\d{1,3}\.){3}\d{1,3}$").WithMessage("La IP debe tener un formato valido.")
            .When(x => !string.IsNullOrEmpty(x.IP));

        RuleFor(x => x.SshPort)
            .InclusiveBetween(1, 65535).WithMessage("El puerto SSH debe estar entre 1 y 65535.")
            .When(x => x.SshPort.HasValue);

        RuleFor(x => x.Label)
            .MaximumLength(100).WithMessage("La etiqueta no puede tener mas de 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Label));

        RuleFor(x => x.Password)
            .MinimumLength(4).WithMessage("La contraseña del nodo debe tener al menos 4 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}

public class ExecuteCommandRequestValidator : AbstractValidator<ExecuteCommandRequest>
{
    public ExecuteCommandRequestValidator()
    {
        RuleFor(x => x.Command)
            .NotEmpty().WithMessage("El comando es obligatorio.")
            .MaximumLength(500).WithMessage("El comando no puede tener mas de 500 caracteres.");
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Credits)
            .GreaterThanOrEqualTo(0).WithMessage("Los creditos no pueden ser negativos.")
            .When(x => x.Credits.HasValue);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("El rol proporcionado no es valido.")
            .When(x => x.Role.HasValue);
    }
}

public class AddCreditsRequestValidator : AbstractValidator<AddCreditsRequest>
{
    public AddCreditsRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a cero.")
            .LessThanOrEqualTo(999999999).WithMessage("El monto excede el limite permitido.");
    }
}

public class GrantNodeAccessRequestValidator : AbstractValidator<GrantNodeAccessRequest>
{
    public GrantNodeAccessRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El ID de usuario es obligatorio.");

        RuleFor(x => x.NodeId)
            .NotEmpty().WithMessage("El ID del nodo es obligatorio.");
    }
}
