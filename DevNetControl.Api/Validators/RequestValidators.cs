using FluentValidation;
using DevNetControl.Api.Dtos;
using DevNetControl.Api.Domain;

namespace DevNetControl.Api.Validators;

/// <summary>
/// Reglas de validación reutilizables para mantener la consistencia en todo el sistema.
/// </summary>
public static class ValidationRules
{
    // Soporte para string (requerido)
    public static IRuleBuilderOptions<T, string> UserNameRules<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
                   .MinimumLength(3).WithMessage("Mínimo 3 caracteres.")
                   .MaximumLength(50).WithMessage("Máximo 50 caracteres.")
                   .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Solo letras, números y guiones bajos.");

    public static IRuleBuilderOptions<T, string> PasswordRules<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder.NotEmpty().WithMessage("La contraseña es obligatoria.")
                   .MinimumLength(6).WithMessage("Mínimo 6 caracteres.");

    // Soporte para string? (opcional/actualización) - Resuelve Warning CS8620
    public static IRuleBuilderOptions<T, string?> UserNameOptionalRules<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder.MaximumLength(50).WithMessage("Máximo 50 caracteres.")
                   .Matches(@"^[a-zA-Z0-9_]*$").WithMessage("Formato inválido.");

    public static IRuleBuilderOptions<T, string?> PasswordOptionalRules<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder.MinimumLength(6).WithMessage("Mínimo 6 caracteres.");
}

#region Authentication & Profile

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("Usuario requerido.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Contraseña requerida.");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Contraseña actual requerida.");
        RuleFor(x => x.NewPassword)
            .PasswordRules()
            .Matches(@"[A-Z]").WithMessage("Debe incluir una mayúscula.")
            .Matches(@"[a-z]").WithMessage("Debe incluir una minúscula.")
            .Matches(@"[0-9]").WithMessage("Debe incluir un número.")
            .NotEqual(x => x.CurrentPassword).WithMessage("La nueva contraseña debe ser diferente a la anterior.");
    }
}

public class UpdateMyUserRequestValidator : AbstractValidator<UpdateMyUserRequest>
{
    public UpdateMyUserRequestValidator()
    {
        RuleFor(x => x.UserName)
            .UserNameOptionalRules()
            .When(x => !string.IsNullOrEmpty(x.UserName));

        RuleFor(x => x.Password)
            .PasswordOptionalRules()
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}

#endregion

#region Credit & Transactions

public class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    public TransferRequestValidator()
    {
        RuleFor(x => x.ToUserId).NotEmpty().WithMessage("ID de destinatario obligatorio.");
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0.")
            .LessThanOrEqualTo(1000000).WithMessage("El monto excede el límite por operación.");
    }
}

public class AddCreditsRequestValidator : AbstractValidator<AddCreditsRequest>
{
    public AddCreditsRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Monto inválido.");
    }
}

#endregion

#region Plans & Services

public class CreatePlanRequestValidator : AbstractValidator<CreatePlanRequest>
{
    public CreatePlanRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage("Nombre de plan requerido.");
        RuleFor(x => x.DurationHours).InclusiveBetween(1, 87600).WithMessage("Duración inválida.");
        RuleFor(x => x.CreditCost).GreaterThanOrEqualTo(0).WithMessage("Costo no puede ser negativo.");
        RuleFor(x => x.MaxConnections).InclusiveBetween(1, 100).WithMessage("Máximo 100 conexiones.");
        RuleFor(x => x.MaxDevices).InclusiveBetween(1, 100).WithMessage("Máximo 100 dispositivos.");
    }
}

public class ExtendServiceRequestValidator : AbstractValidator<ExtendServiceRequest>
{
    public ExtendServiceRequestValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(1, 365).WithMessage("Rango permitido: 1 a 365 días.");
        RuleFor(x => x.NodeId).NotEmpty().WithMessage("ID de nodo obligatorio.");
    }
}

#endregion

#region Infrastructure & Nodes

public class CreateNodeRequestValidator : AbstractValidator<CreateNodeRequest>
{
    public CreateNodeRequestValidator()
    {
        RuleFor(x => x.IP)
            .NotEmpty()
            .Matches(@"^(\d{1,3}\.){3}\d{1,3}$").WithMessage("Formato de IP inválido.");
        
        RuleFor(x => x.SshPort).InclusiveBetween(1, 65535).WithMessage("Puerto fuera de rango.");
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100).WithMessage("Etiqueta requerida.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(4).WithMessage("Contraseña de nodo muy corta.");
    }
}

public class ExecuteCommandRequestValidator : AbstractValidator<ExecuteCommandRequest>
{
    public ExecuteCommandRequestValidator()
    {
        RuleFor(x => x.Command)
            .NotEmpty().WithMessage("Comando requerido.")
            .MaximumLength(500)
            .Must(cmd => !cmd.Contains(";") && !cmd.Contains("&") && !cmd.Contains("|"))
            .WithMessage("El comando contiene caracteres prohibidos por seguridad.");
    }
}

#endregion

#region User Management

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.UserName).UserNameRules();
        RuleFor(x => x.Password).PasswordRules();
        RuleFor(x => x.PlanId).NotEmpty().WithMessage("Debe seleccionar un plan.");
        RuleFor(x => x.NodeId).NotEmpty().When(x => x.NodeId.HasValue).WithMessage("ID de nodo inválido.");
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Credits).GreaterThanOrEqualTo(0).When(x => x.Credits.HasValue);
        RuleFor(x => x.Role).IsInEnum().When(x => x.Role.HasValue);
    }
}

public class RemoveFromVpsRequestValidator : AbstractValidator<RemoveFromVpsRequest>
{
    public RemoveFromVpsRequestValidator()
    {
        RuleFor(x => x.NodeId).NotEmpty().WithMessage("ID de nodo obligatorio.");
    }
}

#endregion

#region Node Management

public class UpdateNodeRequestValidator : AbstractValidator<UpdateNodeRequest>
{
    public UpdateNodeRequestValidator()
    {
        RuleFor(x => x.IP)
            .Matches(@"^(\d{1,3}\.){3}\d{1,3}$").WithMessage("Formato de IP inválido.")
            .When(x => !string.IsNullOrEmpty(x.IP));
        
        RuleFor(x => x.SshPort).InclusiveBetween(1, 65535).WithMessage("Puerto fuera de rango.")
            .When(x => x.SshPort.HasValue);
        
        RuleFor(x => x.Label).MaximumLength(100).WithMessage("Etiqueta máximo 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Label));
        
        RuleFor(x => x.Password).MinimumLength(4).WithMessage("Contraseña muy corta.")
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}

public class GrantNodeAccessRequestValidator : AbstractValidator<GrantNodeAccessRequest>
{
    public GrantNodeAccessRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("ID de usuario obligatorio.");
        RuleFor(x => x.NodeId).NotEmpty().WithMessage("ID de nodo obligatorio.");
    }
}

#endregion

#region Plans

public class UpdatePlanRequestValidator : AbstractValidator<UpdatePlanRequest>
{
    public UpdatePlanRequestValidator()
    {
        RuleFor(x => x.Name).MaximumLength(100).WithMessage("Nombre máximo 100 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Name));
        
        RuleFor(x => x.DurationHours).InclusiveBetween(1, 87600).WithMessage("Duración inválida.")
            .When(x => x.DurationHours.HasValue);
        
        RuleFor(x => x.CreditCost).GreaterThanOrEqualTo(0).WithMessage("Costo no puede ser negativo.")
            .When(x => x.CreditCost.HasValue);
    }
}

#endregion

#region Session Logs

public class CreateSessionLogRequestValidator : AbstractValidator<CreateSessionLogRequest>
{
    public CreateSessionLogRequestValidator()
    {
        RuleFor(x => x.Action).NotEmpty().MaximumLength(100).WithMessage("Acción requerida.");
        RuleFor(x => x.Details).NotEmpty().MaximumLength(1000).WithMessage("Detalles requeridos.");
        RuleFor(x => x.NodeIp)
            .Matches(@"^(\d{1,3}\.){3}\d{1,3}$").WithMessage("Formato de IP inválido.")
            .When(x => !string.IsNullOrEmpty(x.NodeIp));
    }
}

#endregion

#region Resellers & Credits

public class CreateResellerRequestValidator : AbstractValidator<CreateResellerRequest>
{
    public CreateResellerRequestValidator()
    {
        RuleFor(x => x.UserName).UserNameRules();
        RuleFor(x => x.Password).PasswordRules();
        RuleFor(x => x.InitialCredits).GreaterThanOrEqualTo(0).WithMessage("Creditos iniciales no pueden ser negativos.");
    }
}

public class LoadCreditsRequestValidator : AbstractValidator<LoadCreditsRequest>
{
    public LoadCreditsRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("El monto debe ser mayor a 0.");
    }
}

#endregion