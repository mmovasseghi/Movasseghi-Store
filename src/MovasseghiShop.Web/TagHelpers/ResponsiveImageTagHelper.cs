using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.TagHelpers;

[HtmlTargetElement("img", Attributes = "ms-responsive")]
public sealed class ResponsiveImageTagHelper(ResponsiveImageService images) : TagHelper
{
    [HtmlAttributeNotBound, ViewContext] public ViewContext ViewContext { get; set; } = null!;
    public string? Src { get; set; }
    public string? Sizes { get; set; }
    [HtmlAttributeName("ms-responsive")] public bool Responsive { get; set; } = true;
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.RemoveAll("ms-responsive");
        if (!Responsive || images.Get(Src, ViewContext.HttpContext) is not { } image) return;
        output.Attributes.SetAttribute("srcset", image.SrcSet);
        output.Attributes.SetAttribute("sizes", Sizes ?? "(min-width: 768px) 220px, 45vw");
        // Preserve the original aspect ratio even when a selected derivative's
        // integer dimensions have rounded by a fraction of a pixel.
        var style = output.Attributes["style"]?.Value?.ToString() ?? "";
        output.Attributes.SetAttribute("style", $"{style};aspect-ratio:{image.Width}/{image.Height}");
    }
}
