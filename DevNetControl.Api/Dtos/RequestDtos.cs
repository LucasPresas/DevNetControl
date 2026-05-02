using System;

namespace DevNetControl.Api.Dtos;

// Auth
public record LoginRequest(string UserName, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record UpdateMyUserRequest(string? UserName, string? Password);
public record RefreshTokenRequest(string AccessToken, string RefreshToken);

// Créditos y Auditoría
public record TransferRequest(Guid ToUserId, decimal Amount);
public record AddCreditsRequest(decimal Amount);
public record CreditTransactionDto(Guid Id, string SourceUserName, string? TargetUserName, decimal Amount, string Type, string Direction, DateTime Timestamp);

// Planes
public record CreatePlanRequest(string Name, int DurationHours, decimal CreditCost, int MaxConnections, int MaxDevices);
public record UpdatePlanRequest(string? Name, int? DurationHours, decimal? CreditCost);
public record ExtendServiceRequest(int Days, Guid NodeId);

// Usuarios
public record CreateUserRequest(string UserName, string Password, Guid? PlanId = null, Guid? NodeId = null);
public record UpdateUserRequest(decimal? Credits, DevNetControl.Api.Domain.UserRole? Role);
public record UpdateUserBasicRequest(string? UserName = null, string? Password = null);
public record UpdateUserNodesRequest(List<Guid> NodeIds);
public record RemoveFromVpsRequest(Guid NodeId);

// Infraestructura
public record CreateNodeRequest(string IP, int SshPort, string Label, string Password, decimal CreditCost);
public record UpdateNodeRequest(string? IP, int? SshPort, string? Label, string? Password);
public record ExecuteCommandRequest(string Command);
public record GrantNodeAccessRequest(Guid UserId, Guid NodeId);

// Session Logs
public record CreateSessionLogRequest(string Action, string Details, string? NodeIp = null);
public record SessionLogDto(Guid Id, Guid? UserId, string UserName, string ClientIp, string NodeIp, string Action, string Details, DateTime Timestamp);

// Hierarchy & Resellers
public record HierarchyNodeDto(Guid Id, string UserName, DevNetControl.Api.Domain.UserRole Role, decimal Credits, List<HierarchyNodeDto> Children);
public record CreateResellerRequest(string UserName, string Password, List<Guid> PlanIds, bool IsSubReseller = false, decimal InitialCredits = 0);
public record LoadCreditsRequest(decimal Amount);

  // Bulk Operations
  public record BulkCreateUsersRequest(IFormFile CsvFile);
  public record BulkExtendServiceRequest(List<Guid> UserIds, int Days);
  public record BulkDeleteRequest(List<Guid> UserIds);
  public record GrantPlanAccessRequest(Guid UserId, Guid PlanId);
  public record RevokePlanAccessRequest(Guid UserId, Guid PlanId);
  public record MyNodesRequest();
  public record UpdateUserPlansRequest(List<Guid> PlanIds);