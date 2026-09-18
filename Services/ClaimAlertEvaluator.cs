using WarrantyClaims.Models;

namespace WarrantyClaims.Services;

/// <summary>
/// Single source of truth for which alert conditions apply to a claim, so the
/// dashboard and ClaimDetail page can't drift out of sync on the rules.
/// </summary>
public static class ClaimAlertEvaluator
{
    private static readonly TimeSpan SlaResponseWindow = TimeSpan.FromHours(48);

    private static readonly ClaimStatus[] AlertSuppressedStatuses =
    {
        ClaimStatus.Denied,
        ClaimStatus.Resolved
    };

    public static bool IsManufacturerDeadlineUrgent(Claim claim) =>
        !AlertSuppressedStatuses.Contains(claim.Status) &&
        claim.ManufacturerDeadline.HasValue &&
        claim.ManufacturerDeadline.Value.Date <= DateTime.Today.AddDays(3);

    public static bool IsWarrantyExpiryUrgent(Claim claim) =>
        !AlertSuppressedStatuses.Contains(claim.Status) &&
        claim.WarrantyExpiry.HasValue &&
        claim.WarrantyExpiry.Value.Date <= DateTime.Today.AddDays(7);

    public static bool IsSlaBreached(Claim claim) =>
        claim.Status == ClaimStatus.Submitted && DateTime.UtcNow - claim.SubmittedDate > SlaResponseWindow;

    public static bool HasActiveAlert(Claim claim) =>
        IsManufacturerDeadlineUrgent(claim) || IsWarrantyExpiryUrgent(claim) || IsSlaBreached(claim);
}
