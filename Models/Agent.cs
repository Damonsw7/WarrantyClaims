namespace WarrantyClaims.Models;

public class Agent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
