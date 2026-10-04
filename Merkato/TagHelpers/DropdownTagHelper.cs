using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.WebUtilities;

namespace Merkato.TagHelpers;

[HtmlTargetElement("dropdown", Attributes = "value,values", TagStructure = TagStructure.NormalOrSelfClosing)]
public class DropdownTagHelper : TagHelper
{
    [HtmlAttributeNotBound]
    [ViewContext]
    public required ViewContext ViewContext { get; set; }

    [HtmlAttributeName("value")]
    public required ModelExpression Value { get; set; }

    // 
    [HtmlAttributeName("values")]
    public required Dictionary<string, string> Values { get; set; }

    public DropdownTagHelper()
    {

    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Content.Clear();

        var query = QueryHelpers.ParseQuery(ViewContext.HttpContext.Request.QueryString.Value);
        var dict = query.ToDictionary(x => x.Key, y => y.Value.ToString());

        output.PostContent.AppendHtml("<div class=\"dropdown\">");
        // Revisit - preserve current value if inside a form - should this be set outside of the component?
        // if current value should not be posted for some reason?
        output.PostContent.AppendHtml($"<input name=\"{Value.Name}\" value=\"{Value.Model.ToString()}\" hidden/>");

        output.PostContent.AppendHtml("<details>");
        output.PostContent.AppendHtml($"<summary>{Values.First(x => x.Key == Value.Model.ToString()).Value}</summary>");
        output.PostContent.AppendHtml("</details>");

        output.PostContent.AppendHtml("<div class=\"list\">");
        foreach (var item in Values)
        {
            dict[Value.Name] = item.Key;

            var getUrl = ViewContext.HttpContext.Request.Path + QueryString.Create(dict).ToString();
            output.PostContent.AppendHtml($"<a href=\"{getUrl}\" class=\"item\">{item.Value}</a>");
        }
        output.PostContent.AppendHtml("</div>");
        output.PostContent.AppendHtml("</div>");
    }
}
