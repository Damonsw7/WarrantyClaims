using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WarrantyClaims.Data;
using WarrantyClaims.Models;
using WarrantyClaims.Services;

namespace WarrantyClaims.Pages;

public class ClaimDetailModel : PageModel
{
    private readonly WarrantyClaimsContext _context;
    private readonly ILogger<ClaimDetailModel> _logger;

    public ClaimDetailModel(WarrantyClaimsContext context, ILogger<ClaimDetailModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    private const string AgentIdSessionKey = "AgentId";
    private const string AgentNameSessionKey = "AgentName";

    public Claim Claim { get; set; } = null!;

    [BindProperty]
    public ClaimStatus Status { get; set; }

    [BindProperty]
    public string? SelectedAssignedAgent { get; set; }

    [BindProperty]
    public string? PinInput { get; set; }

    public List<string> AgentNames { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public bool IsAgentVerified => HttpContext.Session.GetInt32(AgentIdSessionKey).HasValue;

    public string? VerifiedAgentName => HttpContext.Session.GetString(AgentNameSessionKey);

    public bool IsManufacturerDeadlineUrgent => ClaimAlertEvaluator.IsManufacturerDeadlineUrgent(Claim);

    public bool IsSlaBreached => ClaimAlertEvaluator.IsSlaBreached(Claim);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var claim = await _context.Claims.FindAsync(id);
        if (claim is null)
        {
            return NotFound();
        }

        Claim = claim;
        Status = claim.Status;
        SelectedAssignedAgent = claim.AssignedAgent;
        AgentNames = await BuildAgentNamesAsync(claim.AssignedAgent);

        if (IsManufacturerDeadlineUrgent)
        {
            _logger.LogWarning(
                "Claim {ClaimId} has a manufacturer deadline of {ManufacturerDeadline} that is within 3 days or has passed",
                claim.Id,
                claim.ManufacturerDeadline!.Value);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostVerifyPinAsync(int id)
    {
        var agent = await _context.Agents.FirstOrDefaultAsync(a => a.Pin == PinInput);
        if (agent is null)
        {
            ModelState.AddModelError(nameof(PinInput), "Invalid PIN. Please try again.");

            var claim = await _context.Claims.FindAsync(id);
            if (claim is null)
            {
                return NotFound();
            }

            Claim = claim;
            Status = claim.Status;
            SelectedAssignedAgent = claim.AssignedAgent;
            AgentNames = await BuildAgentNamesAsync(claim.AssignedAgent);
            return Page();
        }

        HttpContext.Session.SetInt32(AgentIdSessionKey, agent.Id);
        HttpContext.Session.SetString(AgentNameSessionKey, agent.Name);

        return RedirectToPage(new { id });
    }

    public IActionResult OnPostLogout(int id)
    {
        HttpContext.Session.Remove(AgentIdSessionKey);
        HttpContext.Session.Remove(AgentNameSessionKey);

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var agentId = HttpContext.Session.GetInt32(AgentIdSessionKey);
        if (!agentId.HasValue)
        {
            return RedirectToPage(new { id });
        }

        var claim = await _context.Claims.FindAsync(id);
        if (claim is null)
        {
            return NotFound();
        }

        var agent = await _context.Agents.FindAsync(agentId.Value);
        if (agent is null)
        {
            HttpContext.Session.Remove(AgentIdSessionKey);
            HttpContext.Session.Remove(AgentNameSessionKey);
            return RedirectToPage(new { id });
        }

        var oldStatus = claim.Status;
        var oldAssignedAgent = claim.AssignedAgent;
        var newAssignedAgent = string.IsNullOrWhiteSpace(SelectedAssignedAgent) ? null : SelectedAssignedAgent;

        try
        {
            claim.Status = Status;
            claim.AssignedAgent = newAssignedAgent;
            claim.LastUpdatedBy = agent.Name;
            await _context.SaveChangesAsync();

            if (oldStatus != claim.Status)
            {
                _logger.LogInformation(
                    "Claim {ClaimId} status changed from {OldStatus} to {NewStatus} by {Agent}",
                    claim.Id,
                    oldStatus,
                    claim.Status,
                    agent.Name);
            }

            if (newAssignedAgent != oldAssignedAgent)
            {
                _logger.LogInformation(
                    "Claim {ClaimId} reassigned from {OldAgent} to {NewAgent}",
                    claim.Id,
                    oldAssignedAgent ?? "(unassigned)",
                    newAssignedAgent ?? "(unassigned)");
            }

            StatusMessage = "Claim updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update claim {ClaimId} (attempted status {AttemptedStatus}, attempted agent {AttemptedAgent}, performed by {PerformingAgent})",
                id,
                Status,
                newAssignedAgent,
                agent.Name);

            ErrorMessage = "Something went wrong while saving your changes. Please try again.";
        }

        return RedirectToPage(new { id });
    }

    private async Task<List<string>> BuildAgentNamesAsync(string? currentAssignedAgent)
    {
        var names = await _context.Agents.OrderBy(a => a.Name).Select(a => a.Name).ToListAsync();

        if (!string.IsNullOrEmpty(currentAssignedAgent) && !names.Contains(currentAssignedAgent))
        {
            names.Insert(0, currentAssignedAgent);
        }

        return names;
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var agentId = HttpContext.Session.GetInt32(AgentIdSessionKey);
        if (!agentId.HasValue)
        {
            return RedirectToPage(new { id });
        }

        var claim = await _context.Claims.FindAsync(id);
        if (claim is null)
        {
            return NotFound();
        }

        var agent = await _context.Agents.FindAsync(agentId.Value);
        if (agent is null)
        {
            HttpContext.Session.Remove(AgentIdSessionKey);
            HttpContext.Session.Remove(AgentNameSessionKey);
            return RedirectToPage(new { id });
        }

        try
        {
            var claimId = claim.Id;
            var customerName = claim.CustomerName;
            var performedBy = agent.Name;

            _context.Claims.Remove(claim);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Claim {ClaimId} (customer {CustomerName}) deleted by agent {Agent}",
                claimId,
                customerName,
                performedBy);

            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete claim {ClaimId}", id);

            ErrorMessage = "Something went wrong while deleting this claim. Please try again.";
            return RedirectToPage(new { id });
        }
    }
}
