using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WarrantyClaims.Data;
using WarrantyClaims.Models;

namespace WarrantyClaims.Pages;

public class NewClaimModel : PageModel
{
    private readonly WarrantyClaimsContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<NewClaimModel> _logger;

    public NewClaimModel(WarrantyClaimsContext context, IWebHostEnvironment environment, ILogger<NewClaimModel> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    private static readonly string[] AllowedUploadExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
    private const long MaxUploadSizeBytes = 10 * 1024 * 1024;

    private const string AgentIdSessionKey = "AgentId";
    private const string AgentNameSessionKey = "AgentName";

    [BindProperty]
    public ClaimInputModel Input { get; set; } = new();

    [BindProperty]
    public string? PinInput { get; set; }

    public List<string> ClaimTypes { get; } = new()
    {
        "Defect",
        "Damage",
        "Missing Parts",
        "Other"
    };

    public string? ErrorMessage { get; set; }

    public bool IsAgentVerified => HttpContext.Session.GetInt32(AgentIdSessionKey).HasValue;

    public string? VerifiedAgentName => HttpContext.Session.GetString(AgentNameSessionKey);

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostVerifyPinAsync()
    {
        var agent = await _context.Agents.FirstOrDefaultAsync(a => a.Pin == PinInput);
        if (agent is null)
        {
            ModelState.AddModelError(nameof(PinInput), "Invalid PIN. Please try again.");
            return Page();
        }

        HttpContext.Session.SetInt32(AgentIdSessionKey, agent.Id);
        HttpContext.Session.SetString(AgentNameSessionKey, agent.Name);

        return RedirectToPage();
    }

    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Remove(AgentIdSessionKey);
        HttpContext.Session.Remove(AgentNameSessionKey);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var agentId = HttpContext.Session.GetInt32(AgentIdSessionKey);
        if (!agentId.HasValue)
        {
            return RedirectToPage();
        }

        var agent = await _context.Agents.FindAsync(agentId.Value);
        if (agent is null)
        {
            HttpContext.Session.Remove(AgentIdSessionKey);
            HttpContext.Session.Remove(AgentNameSessionKey);
            return RedirectToPage();
        }

        ValidateUpload(Input.Photo, nameof(Input.Photo), "Photo");
        ValidateUpload(Input.Receipt, nameof(Input.Receipt), "Receipt");
        ValidateUpload(Input.ProofOfPurchase, nameof(Input.ProofOfPurchase), "Proof of Purchase");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var uploadsRoot = Path.Combine(_environment.ContentRootPath, "Uploads");

        try
        {
            Directory.CreateDirectory(uploadsRoot);

            string? photoPath = await SaveFileAsync(Input.Photo, uploadsRoot);
            string? receiptPath = await SaveFileAsync(Input.Receipt, uploadsRoot);
            string? proofOfPurchasePath = await SaveFileAsync(Input.ProofOfPurchase, uploadsRoot);

            var claim = new Claim
            {
                CustomerName = Input.CustomerName,
                CustomerEmail = Input.CustomerEmail,
                ProductName = Input.ProductName,
                SerialNumber = Input.SerialNumber,
                PurchaseDate = Input.PurchaseDate!.Value,
                ClaimType = Input.ClaimType,
                IssueDescription = Input.IssueDescription,
                Status = ClaimStatus.Submitted,
                AssignedAgent = agent.Name,
                ManufacturerDeadline = Input.ManufacturerDeadline,
                WarrantyExpiry = Input.WarrantyExpiry,
                SubmittedDate = DateTime.UtcNow,
                PhotoPath = photoPath,
                ReceiptPath = receiptPath,
                ProofOfPurchasePath = proofOfPurchasePath
            };

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Claim {ClaimId} created for customer {CustomerName}",
                claim.Id,
                claim.CustomerName);

            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to save new claim for customer {CustomerName}, serial number {SerialNumber}",
                Input.CustomerName,
                Input.SerialNumber);

            ErrorMessage = "Something went wrong while saving your claim. Please try again.";
            return Page();
        }
    }

    private void ValidateUpload(IFormFile? file, string propertyName, string displayName)
    {
        if (file is null || file.Length == 0)
        {
            return;
        }

        if (file.Length > MaxUploadSizeBytes)
        {
            ModelState.AddModelError($"Input.{propertyName}", $"{displayName} must be 10MB or smaller.");
            return;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedUploadExtensions.Contains(extension))
        {
            ModelState.AddModelError($"Input.{propertyName}", $"{displayName} must be a JPG, PNG, or PDF file.");
        }
    }

    private static async Task<string?> SaveFileAsync(IFormFile? file, string uploadsRoot)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsRoot, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Path.Combine("Uploads", uniqueFileName);
    }

    public class ClaimInputModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Customer Email")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Serial Number")]
        public string SerialNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Purchase Date")]
        public DateTime? PurchaseDate { get; set; }

        [Required]
        [Display(Name = "Claim Type")]
        public string ClaimType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Issue Description")]
        public string IssueDescription { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Manufacturer Deadline")]
        public DateTime? ManufacturerDeadline { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Warranty Expiry")]
        public DateTime? WarrantyExpiry { get; set; }

        [Display(Name = "Photo")]
        public IFormFile? Photo { get; set; }

        [Display(Name = "Receipt")]
        public IFormFile? Receipt { get; set; }

        [Display(Name = "Proof of Purchase")]
        public IFormFile? ProofOfPurchase { get; set; }
    }
}
