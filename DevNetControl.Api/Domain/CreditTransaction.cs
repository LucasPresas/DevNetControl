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
    public decimal Amount { get; set; }

    public decimal? SourceBalanceBefore { get; set; }
    public decimal? SourceBalanceAfter { get; set; }
    public decimal? TargetBalanceBefore { get; set; }
    public decimal? TargetBalanceAfter { get; set; }

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