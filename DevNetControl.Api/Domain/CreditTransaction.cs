using System.ComponentModel.DataAnnotations;

namespace DevNetControl.Api.Domain;

public class CreditTransaction
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid TenantId { get; set; } 

    public Guid? SourceUserId { get; set; } 
    public User? SourceUser { get; set; }

    public Guid? TargetUserId { get; set; } 
    public User? TargetUser { get; set; }

    [Required]
    public int Amount { get; set; }

    public int? SourceBalanceBefore { get; set; }
    public int? SourceBalanceAfter { get; set; }
    public int? TargetBalanceBefore { get; set; }
    public int? TargetBalanceAfter { get; set; }

    [Required]
    public CreditTransactionType Type { get; set; }

    public string? Note { get; set; } 

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum CreditTransactionType
{
    Transfer,
    UserCreation,     
    ServiceExtension, 
    PlanPurchase,     
    AdminCredit,      
    Refund
}