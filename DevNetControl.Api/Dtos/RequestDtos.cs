using System;

namespace DevNetControl.Api.Dtos;

// Auth
public record LoginRequest(string UserName, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record UpdateMyUserRequest(string? UserName, string? Password);
public record RefreshTokenRequest(string AccessToken, string RefreshToken);

// Créditos y Auditoría
public record TransferRequest(Guid ToUserId, int Amount);
public record AddCreditsRequest(int Amount);
public record CreditTransactionDto(Guid Id, string SourceUserName, string? TargetUserName, int Amount, string Type, string Direction, DateTime Timestamp);

public record CreditTransactionWithBalanceDto(
    Guid Id,
    string SourceUserName,
    string? TargetUserName,
    int Amount,
    string Type,
    string Direction,
    DateTime Timestamp,
    int? SourceBalanceBefore,
    int? SourceBalanceAfter,
    int? TargetBalanceBefore,
    int? TargetBalanceAfter,
    string? Note
);

// Planes
public record CreatePlanRequest(string Name, int DurationHours, int CreditCost, int MaxConnections, int MaxDevices);
public record UpdatePlanRequest(string? Name, int? DurationHours, int? CreditCost);
public record ExtendServiceRequest(int Days, Guid NodeId);

// Usuarios
public record CreateUserRequest(string UserName, string Password, Guid? PlanId = null, Guid? NodeId = null);
public record UpdateUserRequest(int? Credits, DevNetControl.Api.Domain.UserRole? Role);
public record UpdateUserBasicRequest(string? UserName = null, string? Password = null, Guid? ParentId = null, int? MaxConnections = null, Guid? NodeId = null);
public record UpdateUserNodesRequest(List<Guid> NodeIds);
public record RemoveFromVpsRequest(Guid NodeId);
public record AddConnectionRequest(int ConnectionsToAdd = 1);
public record RenewPlanRequest(Guid PlanId, int DurationHours);

  // Infraestructura
  public record CreateNodeRequest(string IP, int SshPort, string Label, string Password, int CreditCost);
  public record UpdateNodeRequest(string? IP, int? SshPort, string? Label, string? Password);
  public record ExecuteCommandRequest(string Command);
  public record GrantNodeAccessRequest(Guid UserId, Guid NodeId);
  public record BulkDeleteNodesRequest(List<Guid> NodeIds);

// Session Logs
public record CreateSessionLogRequest(string Action, string Details, string? NodeIp = null);
public record SessionLogDto(Guid Id, Guid? UserId, string UserName, string ClientIp, string? UserAgent, string NodeIp, string Action, string Details, DateTime Timestamp);

// Hierarchy & Resellers
public record HierarchyNodeDto(Guid Id, string UserName, DevNetControl.Api.Domain.UserRole Role, int Credits, List<HierarchyNodeDto> Children);
public record CreateResellerRequest(string UserName, string Password, List<Guid> PlanIds, bool IsSubReseller = false, int InitialCredits = 0);
public record LoadCreditsRequest(int Amount);

  // Bulk Operations
  public record BulkCreateUsersRequest(IFormFile CsvFile);
  public record BulkExtendServiceRequest(List<Guid> UserIds, int Days);
  public record BulkDeleteRequest(List<Guid> UserIds);
  public record BulkToggleSuspendRequest(List<Guid> UserIds);
  public record BulkDeletePlansRequest(List<Guid> PlanIds);
  public record GrantPlanAccessRequest(Guid UserId, Guid PlanId);
  public record RevokePlanAccessRequest(Guid UserId, Guid PlanId);
  public record MyNodesRequest();
  public record UpdateUserPlansRequest(List<Guid> PlanIds);

// Activity Logs
public record ActivityLogDto(
    Guid Id,
    string ActionType,
    Guid ActorUserId,
    string ActorUserName,
    string ActorRole,
    Guid? TargetUserId,
    string? TargetUserName,
    int CreditsConsumed,
    int CreditsBalanceBefore,
    int CreditsBalanceAfter,
    string Description,
    string? Details,
    Guid? PlanId,
    string? PlanName,
    Guid? NodeId,
    string? NodeLabel,
    DateTime Timestamp
);

public record ActivityLogDetailDto(
    Guid Id,
    string ActionType,
    Guid ActorUserId,
    string ActorUserName,
    string ActorRole,
    Guid? TargetUserId,
    string? TargetUserName,
    int CreditsConsumed,
    int CreditsBalanceBefore,
    int CreditsBalanceAfter,
    string Description,
    string? Details,
    Guid? PlanId,
    string? PlanName,
    Guid? NodeId,
    string? NodeLabel,
    DateTime Timestamp,
    int? SourceBalanceBefore,
    int? SourceBalanceAfter,
    int? TargetBalanceBefore,
    int? TargetBalanceAfter
);

public record ActivityStatsDto(
    int TotalActivities,
    int UniqueActors,
    int ActivitiesLast24Hours,
    int ActivitiesToday,
    List<ActionCountDto> TopActions,
    List<RoleCountDto> ActivitiesByRole,
    int TotalCreditsConsumed,
    DateTime Timestamp
);

public record ActionCountDto(string ActionType, int Count);
public record RoleCountDto(string Role, int Count);