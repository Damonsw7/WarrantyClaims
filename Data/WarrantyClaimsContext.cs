using Microsoft.EntityFrameworkCore;
using WarrantyClaims.Models;

namespace WarrantyClaims.Data;

public class WarrantyClaimsContext : DbContext
{
    public WarrantyClaimsContext(DbContextOptions<WarrantyClaimsContext> options)
        : base(options)
    {
    }

    public DbSet<Claim> Claims { get; set; } = null!;
    public DbSet<Agent> Agents { get; set; } = null!;
}
