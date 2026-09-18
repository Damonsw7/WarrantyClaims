using System.ComponentModel.DataAnnotations;

namespace WarrantyClaims.Models;

public class Claim
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string SerialNumber { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; }

    [Required]
    [StringLength(100)]
    public string ClaimType { get; set; } = string.Empty;

    [Required]
    public string IssueDescription { get; set; } = string.Empty;

    public ClaimStatus Status { get; set; } = ClaimStatus.Submitted;

    [StringLength(200)]
    public string? AssignedAgent { get; set; }

    [StringLength(200)]
    public string? LastUpdatedBy { get; set; }

    public DateTime? ManufacturerDeadline { get; set; }

    public DateTime? WarrantyExpiry { get; set; }

    public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

    public string? PhotoPath { get; set; }

    public string? ReceiptPath { get; set; }

    public string? ProofOfPurchasePath { get; set; }
}
