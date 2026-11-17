using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;

namespace Merkato.Helpers;

public static class UrlHelper
{
    public static string GetCurrentUrlWithUpdatedQueryParams(ViewContext viewContext, Dictionary<string, object> parameters)
    {
        var query = QueryHelpers.ParseQuery(viewContext.HttpContext.Request.QueryString.Value);
        var dict = query.ToDictionary(x => x.Key, y => y.Value.ToString());

        foreach (var parameter in parameters)
        {
            dict[parameter.Key] = parameter.Value.ToString();
        }

        return viewContext.HttpContext.Request.Path + QueryString.Create(dict).ToString();
    }
}
