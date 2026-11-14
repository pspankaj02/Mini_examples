using Microsoft.AspNetCore.Razor.TagHelpers;

namespace KiranaHub.MvcWeb.TagHelpers;

// =========================================================================================
// [TOPIC]: Custom ASP.NET Core Razor Tag Helper
// [PURPOSE]: Transforms custom HTML element <kirana-badge> into dynamic styled Bootstrap badges.
// =========================================================================================

// [KYA KARTA HAI]: Target HTML tag name specify karta hai jise ye TagHelper intercept aur process karega.
// [KYU ZAROORI HAI]: Razor engine ko batata hai ki markup me `<kirana-badge>` tag encounter hone par ye class execute kare.
// [KAISE CHALTA HAI]: Attribute metadata Razor compilation pipeline ko tag binding register karta hai.
[HtmlTargetElement("kirana-badge")]
public class KiranaBadgeTagHelper : TagHelper
{
    // [KYA KARTA HAI]: HTML attribute 'is-perishable' ko C# boolean property se bind karta hai (kebab-case to PascalCase).
    // [KYU ZAROORI HAI]: Razor markup se perishable flag pass karne ke liye (e.g. `<kirana-badge is-perishable="true">`).
    // [KAISE CHALTA HAI]: TagHelper binding engine automatic kebab-case to PascalCase type conversion karta hai.
    public bool IsPerishable { get; set; }

    // [KYA KARTA HAI]: HTML attribute 'stock' ko integer property se bind karta hai.
    // [KYU ZAROORI HAI]: Product stock count evaluate karke badge color decide karna (in stock vs out of stock).
    public int Stock { get; set; }

    // [KYA KARTA HAI]: Tag Helper execution lifecycle ka core synchronous method jaha HTML transformation code run hota hai.
    // [KYU ZAROORI HAI]: HTML element name, attributes, aur inner content dynamically generate karna.
    // [KAISE CHALTA HAI]: Razor view engine rendering time par call karta hai; context me input metadata aur output me output HTML model milta hai.
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // [KYA KARTA HAI]: Rendered HTML tag name ko custom `<kirana-badge>` se standard `<span>` me change karta hai.
        // [KYU ZAROORI HAI]: Browser valid semantic HTML <span> element samajhta hai.
        // [OUTPUT / SIDE EFFECT]: `<kirana-badge>` becomes `<span ...>`.
        output.TagName = "span";

        // [KYA KARTA HAI]: Check karta hai ki item ka stock 0 ya negative hai ya nahi.
        // [KYU ZAROORI HAI]: Out-of-stock items ko sabse highest priority visual alert dena.
        if (Stock <= 0)
        {
            // [KYA KARTA HAI]: Red danger badge Bootstrap CSS classes set karta hai.
            // [OUTPUT / SIDE EFFECT]: class="badge bg-danger rounded-pill" attribute add hota hai.
            output.Attributes.SetAttribute("class", "badge bg-danger rounded-pill");

            // [KYA KARTA HAI]: Span ke andar "Out of Stock" text content inject karta hai.
            // [OUTPUT / SIDE EFFECT]: `<span>Out of Stock</span>` render hota hai.
            output.Content.SetContent("Out of Stock");
        }
        // [KYA KARTA HAI]: Check karta hai ki product perishable (doodh/dahi/butter) category ka hai ya nahi.
        else if (IsPerishable)
        {
            // [KYA KARTA HAI]: Yellow warning badge CSS classes set karta hai with dark readable text.
            output.Attributes.SetAttribute("class", "badge bg-warning text-dark rounded-pill");

            // [KYA KARTA HAI]: Badge label "Cold Chain / Perishable" set karta hai.
            output.Content.SetContent("Cold Chain / Perishable");
        }
        // [KYA KARTA HAI]: Default case jab item in-stock aur non-perishable ho (dal/chawal/oil/snacks).
        else
        {
            // [KYA KARTA HAI]: Green success badge CSS classes set karta hai.
            output.Attributes.SetAttribute("class", "badge bg-success rounded-pill");

            // [KYA KARTA HAI]: Remaining stock count ke sath "In Stock (X left)" message inject karta hai.
            // [OUTPUT / SIDE EFFECT]: e.g., `<span>In Stock (20 left)</span>`.
            output.Content.SetContent($"In Stock ({Stock} left)");
        }
    }
}
