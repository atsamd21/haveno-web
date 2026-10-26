using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.WebUtilities;

namespace Merkato.TagHelpers;

/// <summary>
/// Will toggle the parent boolean on post. The value can then be read in the provided submit method in the parent.
/// </summary>
[HtmlTargetElement("toggle", Attributes = "is-toggled")]
public class InteractiveToggleTagHelper : TagHelper
{
    private readonly IAntiforgery _antiforgery;
    private readonly IUrlHelperFactory _urlHelperFactory;

    [HtmlAttributeNotBound]
    [ViewContext]
    public required ViewContext ViewContext { get; set; }

    [HtmlAttributeName("is-toggled")]
    public required ModelExpression IsToggled { get; set; }

    [HtmlAttributeName("submit-method")]
    public Func<Task>? SubmitMethod { get; set; }

    [HtmlAttributeName("method")]
    public string Method { get; set; } = "post";
    [HtmlAttributeName("disabled")]
    public bool Disabled { get; set; }

    public InteractiveToggleTagHelper(IUrlHelperFactory urlHelperFactory, IAntiforgery antiforgery)
    {
        _urlHelperFactory = urlHelperFactory;
        _antiforgery = antiforgery;
    }

    //private string GetPostUrl()
    //{
    //    return _urlHelperFactory
    //        .GetUrlHelper(ViewContext)
    //        .RouteUrl(ViewContext.RouteData.Values) + $"?handler={_submitMethodHandlerName}";
    //}

    //private string GetCurrentUrl()
    //{
    //    StringBuilder stringBuilder = new();

    //    var dict = ViewContext.HttpContext.Request.Query.ToDictionary();

    //    foreach(var keyValuePair in dict)
    //    {
    //        if (keyValuePair.Key == IsToggled.Name)
    //        {
    //            dict.Remove(keyValuePair.Key);
    //        }
    //    }

    //    QueryCollection query = new(dict);

    //    for (int i = 0; i < query.Count; i++)
    //    {
    //        char separator;
    //        if (i == 0)
    //        {
    //            separator = '?';
    //        }
    //        else
    //        {
    //            separator = '&';
    //        }

    //        var param = query.ElementAt(i);

    //        stringBuilder.Append(separator);
    //        stringBuilder.Append($"{param.Key}={param.Value}");
    //    }

    //    return _urlHelperFactory
    //        .GetUrlHelper(ViewContext)
    //        .RouteUrl(ViewContext.RouteData.Values) + stringBuilder.ToString();
    //}

    // 
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Method.ToLower() == "post")
        {
            if (SubmitMethod is null)
            {
                throw new ArgumentException("A \"submit-method\" is required when method is set to \"post\".");
            }

            string submitMethodHandlerName;

            if (SubmitMethod.Method.Name.StartsWith("OnPost"))
            {
                submitMethodHandlerName = SubmitMethod.Method.Name
                    .Replace("OnPost", string.Empty);

                if (SubmitMethod.Method.Name.EndsWith("Async"))
                {
                    submitMethodHandlerName = SubmitMethod.Method.Name
                        .Replace("Async", string.Empty);
                }
            }
            else
            {
                throw new ArgumentException("Submit method name must begin with \"OnPost\"");
            }

            output.Content.Clear();

            var postUrl = _urlHelperFactory
                .GetUrlHelper(ViewContext)
                .RouteUrl(ViewContext.RouteData.Values) + $"?handler={submitMethodHandlerName}";

            output.PostContent.AppendHtml($"<form method=\"post\" class=\"toggle-main {(Disabled ? "toggle-disabled" : "")}\">");
            var token = _antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
            output.PostContent.AppendHtml($"<input name=\"{IsToggled.Name}\" value=\"{!(bool)IsToggled.Model}\" hidden/>");
            output.PostContent.AppendHtml($"<input name=\"{token.FormFieldName}\" value=\"{token.RequestToken}\" hidden/>");
            output.PostContent.AppendHtml($"<button class=\"{((bool)IsToggled.Model ? "toggle toggled" : "toggle")}\" formaction=\"{submitMethodHandlerName}\"></button>");
            output.PostContent.AppendHtml("</form>");
        }
        else if (Method.ToLower() == "get")
        {
            output.Content.Clear();

            var query = QueryHelpers.ParseQuery(ViewContext.HttpContext.Request.QueryString.Value);

            var dict = query.ToDictionary(x => x.Key, y => y.Value.ToString());
            dict[IsToggled.Name] = (!(bool)IsToggled.Model).ToString();
            var getUrl = ViewContext.HttpContext.Request.Path + QueryString.Create(dict).ToString();

            output.PostContent.AppendHtml($"<div class=\"toggle-main {(Disabled ? "toggle-disabled" : "")}\">");
            output.PostContent.AppendHtml($"<input name=\"{IsToggled.Name}\" value=\"{(bool)IsToggled.Model}\" hidden/>");
            output.PostContent.AppendHtml($"<a href=\"{getUrl}\" class=\"{((bool)IsToggled.Model ? "toggle toggled" : "toggle")}\"></a>");
            output.PostContent.AppendHtml("</div>");
        }
        else
        {
            throw new ArgumentException("Invalid method type.");
        }
    }
}
