using System.ComponentModel.DataAnnotations;

namespace DevNetControl.Api.Domain;

public class CreditTransaction
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid TenantId { get; set; } 

    [Required]
    public Guid SourceUserId { get; set; } 
    public User SourceUser { get; set; } = null!;

    public Guid? TargetUserId { get; set; } 
    public User? TargetUser { get; set; }

    [Required]
    public decimal Amount { get; set; }

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