using HtmlAgilityPack;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Merkato.TagHelpers;

[HtmlTargetElement("eagertabs", Attributes = "tab-index")]
public class EagerTabsTagHelper : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {

    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (!int.TryParse(context.AllAttributes["tab-index"].Value.ToString(), out int tabIndex))
            return;

        var childContent = await output.GetChildContentAsync();
        var htmlString = childContent.GetContent();

        output.Content.Clear();

        var doc = new HtmlDocument();
        doc.LoadHtml(htmlString);

        var children = doc.DocumentNode.ChildNodes
            .Where(x => x.NodeType == HtmlNodeType.Element)
            .Select(x => new { Title = x.GetAttributeValue<string>("title", new string("")) , Html = x.OuterHtml })
            .ToList();

        var guid = Guid.NewGuid();

        output.PostContent.AppendHtml("<div class=\"eager-tabs\">");

        for (int i = 0; i < children.Count; i++)
        {
            output.PostContent.AppendHtml($"<input name=\"{guid}\" class=\"tab-input\" {(i == tabIndex ? "checked" : "")} type=\"radio\" id=\"input-{i}\"></input>");
        }

        string style = "";

        output.PostContent.AppendHtml("<div class=\"titles\">");
        for (int i = 0; i < children.Count; i++)
        {
            style += $"#input-{i}:checked~.titles #title-{i}";
            if (i < children.Count - 1)
            {
                style += ",";
            }

            var child = children[i];
            output.PostContent.AppendHtml($"<label id=\"title-{i}\" class=\"tab-input-visible\">{child.Title}</label>");
        }
        output.PostContent.AppendHtml("</div>");

        style += $"{{background-color:#778DA9;color:#fff;}}";

        for (int i = 0; i < children.Count; i++)
        {
            style += $"#input-{i}:checked~#content-{i}";
            if (i < children.Count - 1)
            {
                style += ",";
            }

            output.PostContent.AppendHtml($"<div class=\"tab-content\" id=\"content-{i}\">");

            var child = children[i];
            output.PostContent.AppendHtml(child.Html);

            output.PostContent.AppendHtml("</div>");
        }

        output.PostContent.AppendHtml("</div>");

        style += $"{{display:block;}}";

        output.PostContent.AppendHtml($"<style>{style}</style>");
    }
}
