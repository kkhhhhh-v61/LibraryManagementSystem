using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagementSystem.Extensions;

public static class Extensions
{
    public static string IsActive(this IHtmlHelper html, string controller, string? action, string cssClass)
    {
        var routeData = html.ViewContext.RouteData;
        var routeAction = routeData.Values["action"] as string;
        var routeController = routeData.Values["controller"] as string;

        // 1. Check Controller
        if (!string.Equals(controller, routeController, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        // 2. Check Action
        // If 'action' param is passed, it must match.
        // If 'action' param is null, ignore it.
        if (action != null && !string.Equals(action, routeAction, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return cssClass;
    }

    public static bool IsValid(this ModelStateDictionary ms, string key)
    {
        if (ms == null) return false;
        if (string.IsNullOrEmpty(key)) return false;
        return ms.GetFieldValidationState(key) == ModelValidationState.Valid;
    }
}
