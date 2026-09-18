using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WarrantyClaims.Data;
using WarrantyClaims.Models;
using WarrantyClaims.Services;

namespace WarrantyClaims.Pages;

public class IndexModel : PageModel
{
    private readonly WarrantyClaimsContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(WarrantyClaimsContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public ClaimStatus? StatusFilter { get; set; }

    public IList<Claim> Claims { get; set; } = new List<Claim>();

    public async Task OnGetAsync()
    {
        var query = _context.Claims.AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim();
            query = query.Where(c =>
                EF.Functions.Like(c.CustomerName, $"%{term}%") ||
                EF.Functions.Like(c.SerialNumber, $"%{term}%"));
        }

        if (StatusFilter.HasValue)
        {
            query = query.Where(c => c.Status == StatusFilter.Value);
        }

        var matchingClaims = await query.ToListAsync();

        Claims = matchingClaims
            .OrderBy(c => ClaimAlertEvaluator.HasActiveAlert(c) ? 0 : 1)
            .ThenBy(c => c.ManufacturerDeadline.HasValue ? 0 : 1)
            .ThenBy(c => c.ManufacturerDeadline)
            .ToList();

        var submittedClaims = await _context.Claims
            .Where(c => c.Status == ClaimStatus.Submitted)
            .ToListAsync();

        foreach (var claim in submittedClaims.Where(ClaimAlertEvaluator.IsSlaBreached))
        {
            _logger.LogWarning(
                "Claim {ClaimId} has breached the 48-hour SLA — still Submitted {ElapsedHours:F1} hours after being submitted on {SubmittedDate}",
                claim.Id,
                (DateTime.UtcNow - claim.SubmittedDate).TotalHours,
                claim.SubmittedDate);
        }
    }
}
