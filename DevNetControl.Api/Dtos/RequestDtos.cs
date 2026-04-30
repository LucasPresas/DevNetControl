using System;

namespace DevNetControl.Api.Dtos;

// Auth
public record LoginRequest(string UserName, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record UpdateMyUserRequest(string? UserName, string? Password);

// Créditos y Auditoría
public record TransferRequest(Guid ToUserId, decimal Amount);
public record AddCreditsRequest(decimal Amount);
public record CreditTransactionDto(Guid Id, string SourceUserName, string? TargetUserName, decimal Amount, string Type, string Direction, DateTime Timestamp);

// Planes
public record CreatePlanRequest(string Name, int DurationHours, decimal CreditCost, int MaxConnections, int MaxDevices);
public record UpdatePlanRequest(string? Name, int? DurationHours, decimal? CreditCost);
public record ExtendServiceRequest(int Days);

// Usuarios
public record CreateUserRequest(string UserName, string Password, Guid PlanId);
public record UpdateUserRequest(decimal? Credits, DevNetControl.Api.Domain.UserRole? Role);

// Infraestructura
public record CreateNodeRequest(string IP, int SshPort, string Label, string Password, decimal CreditCost);
public record UpdateNodeRequest(string? IP, int? SshPort, string? Label, string? Password);
public record ExecuteCommandRequest(string Command);
public record GrantNodeAccessRequest(Guid UserId, Guid NodeId);