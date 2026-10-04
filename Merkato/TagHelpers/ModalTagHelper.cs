using Merkato.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;

namespace Merkato.TagHelpers;

[HtmlTargetElement("modal", Attributes = "title,is-open")]
public class ModalTagHelper : TagHelper
{
    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly IAntiforgery _antiforgery;

    [HtmlAttributeNotBound]
    [ViewContext]
    public required ViewContext ViewContext { get; set; }

    [HtmlAttributeName("title")]
    public required string Title { get; set; }

    [HtmlAttributeName("is-open")]
    public required ModelExpression IsOpen { get; set; }

    [HtmlAttributeName("can-close")]
    public bool CanClose { get; set; } = true;

    public ModalTagHelper(IUrlHelperFactory urlHelperFactory, IAntiforgery antiforgery)
    {
        _urlHelperFactory = urlHelperFactory;
        _antiforgery = antiforgery;
    }

    private string GetCurrentUrl()
    {
        StringBuilder stringBuilder = new();

        var dict = ViewContext.HttpContext.Request.Query.ToDictionary();

        foreach(var keyValuePair in dict)
        {
            if (keyValuePair.Key == IsOpen.Name)
            {
                dict.Remove(keyValuePair.Key);
            }
        }

        QueryCollection query = new(dict);

        for (int i = 0; i < query.Count; i++)
        {
            char separator;
            if (i == 0)
            {
                separator = '?';
            }
            else
            {
                separator = '&';
            }

            var param = query.ElementAt(i);

            stringBuilder.Append(separator);
            stringBuilder.Append($"{param.Key}={param.Value}");
        }

        return _urlHelperFactory
            .GetUrlHelper(ViewContext)
            .RouteUrl(ViewContext.RouteData.Values) + stringBuilder.ToString();
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {

    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        //var isOpen = bool.Parse(context.AllAttributes["is-open"].Value.ToString());
        if (!(bool)IsOpen.Model)
        {
            output.Content.Clear();
            return;
        }

        //var title = context.AllAttributes["title"].Value.ToString();

        ModalButtonOptions modalButtonOptions = ModalButtonOptions.OK | ModalButtonOptions.CANCEL;
        if (context.AllAttributes.TryGetAttribute("buttons", out var buttons))
        {
            if (!Enum.TryParse(buttons.Value.ToString(), out modalButtonOptions))
                throw new ArgumentException("Failed to parse buttons parameter.");
        }

        string submitText = "Ok";
        string cancelText = "Close";
        string? submitMethodName = null;
        if (context.AllAttributes.TryGetAttribute("submit-method", out var submitMethodNameAttrib))
        {
            submitMethodName = submitMethodNameAttrib.Value.ToString();
        }
        if (context.AllAttributes.TryGetAttribute("submit-text", out var submitTextAttrib))
        {
            submitText = submitTextAttrib.Value.ToString();
        }
        if (context.AllAttributes.TryGetAttribute("cancel-text", out var cancelTextAttrib))
        {
            cancelText = cancelTextAttrib.Value.ToString();
        }

        var childContent = await output.GetChildContentAsync();
        var htmlString = childContent.GetContent();

        output.Content.Clear();

        output.PostContent.AppendHtml($"<a href={GetCurrentUrl()} class=\"modal-background-tag\"></a>");
        output.PostContent.AppendHtml("<div class=\"modal-tag\">");
        output.PostContent.AppendHtml("<div class=\"modal-top\">");
        output.PostContent.AppendHtml($"<img src=\"icons/info.svg\" class=\"info-icon\"/>");
        output.PostContent.AppendHtml($"<p class=\"title\">{Title}</p>");
        output.PostContent.AppendHtml($"<a href={GetCurrentUrl()}>");
        output.PostContent.AppendHtml("<i class=\"bi bi-x\"></i>");
        output.PostContent.AppendHtml("</a>");
        output.PostContent.AppendHtml("</div>");
        output.PostContent.AppendHtml("<form method=\"post\" class=\"modal-content\">");
        output.PostContent.AppendHtml(htmlString);

        output.PostContent.AppendHtml("<div class=\"modal-buttons\">");
        if (modalButtonOptions.HasFlag(ModalButtonOptions.OK) && !string.IsNullOrEmpty(submitMethodName))
        {
            var postUrl = _urlHelperFactory.GetUrlHelper(ViewContext).RouteUrl(ViewContext.RouteData.Values) + $"?handler={submitMethodName}";
            output.PostContent.AppendHtml($"<button class=\"button-primary\" formaction={postUrl} type=\"submit\">{submitText}</button>");
        }
        if (modalButtonOptions.HasFlag(ModalButtonOptions.CANCEL))
        {
            output.PostContent.AppendHtml($"<a class=\"button-secondary\" href={GetCurrentUrl()}>{cancelText}</a>");
        }
        output.PostContent.AppendHtml("<div>");

        var token = _antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
        output.PostContent.AppendHtml($"<input name=\"{token.FormFieldName}\" value=\"{token.RequestToken}\" hidden/>");
        output.PostContent.AppendHtml("</form>");
        output.PostContent.AppendHtml("</div>");
    }
}
