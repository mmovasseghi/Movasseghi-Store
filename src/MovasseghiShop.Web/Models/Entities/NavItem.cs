namespace MovasseghiShop.Web.Models.Entities;

public class NavItem
{
    public int Id { get; set; }
    public string Zone { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
