namespace MovasseghiShop.Web.Models.Entities;

public class PillarKeyword
{
    public int Id { get; set; }
    public string Phrase { get; set; } = string.Empty;
    public string Cluster { get; set; } = "general";
    public int Priority { get; set; } = 10;
    public string OwnerPageKey { get; set; } = string.Empty;
    public string SearchIntent { get; set; } = "commercial";
    public bool IsActive { get; set; } = true;
}
