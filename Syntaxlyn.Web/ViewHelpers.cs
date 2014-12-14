using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Syntaxlyn.Web
{
    public static class ViewHelpers
    {
        public static MvcHtmlString BootstrapMenu(this HtmlHelper htmlHelper, string linkText, string actionName, string controllerName)
        {
            var li = new TagBuilder("li");
            li.InnerHtml = htmlHelper.ActionLink(linkText, actionName, controllerName).ToHtmlString();

            var routeData = htmlHelper.ViewContext.RouteData;
            if (routeData.GetRequiredString("action") == actionName && routeData.GetRequiredString("controller") == controllerName)
                li.AddCssClass("active");

            return MvcHtmlString.Create(li.ToString());
        }
    }
}