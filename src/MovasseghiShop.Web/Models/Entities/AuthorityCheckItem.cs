namespace MovasseghiShop.Web.Models.Entities;

public class AuthorityCheckItem
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HelpUrl { get; set; }
    public bool IsDone { get; set; }
    public int SortOrder { get; set; }
    public DateTime? CompletedAt { get; set; }
}
