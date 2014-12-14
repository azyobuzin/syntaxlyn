using System.Web;
using System.Web.Optimization;

namespace Syntaxlyn.Web
{
    public class BundleConfig
    {
        // バンドルの詳細については、http://go.microsoft.com/fwlink/?LinkId=301862  を参照してください
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.UseCdn = true;
            //BundleTable.EnableOptimizations = true;

            #region Libraries

            bundles.Add(new ScriptBundle("~/bundles/jquery", "http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.11.1.min.js")
                .Include("~/Scripts/jquery-{version}.js"));

            //bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
            //            "~/Scripts/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/signalr", "http://ajax.aspnetcdn.com/ajax/signalr/jquery.signalr-2.1.0.min.js")
                .Include("~/Scripts/jquery.signalR-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrapjs", "//maxcdn.bootstrapcdn.com/bootstrap/3.3.1/js/bootstrap.min.js")
                .Include("~/Scripts/bootstrap.js"));

            bundles.Add(new StyleBundle("~/bundles/bootstrapcss", "//maxcdn.bootstrapcdn.com/bootstrap/3.3.1/css/bootstrap.min.css")
                .Include("~/Content/bootstrap.css"));

            #endregion

            #region Home
            bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/Site.css"));
            #endregion

            #region SourceView
            bundles.Add(new StyleBundle("~/Content/SourceViewCss")
                .Include("~/Content/themes/default/style.css", "~/Content/SourceView.css", "~/Content/SyntaxHighlighting.css"));
            bundles.Add(new ScriptBundle("~/bundles/SourceView")
                .Include("~/Scripts/jstree.js", "~/Scripts/SourceView.js"));
            #endregion

            #region SourceView/Pending
            bundles.Add(new StyleBundle("~/Content/PendingCss").Include("~/Content/Pending.css"));
            bundles.Add(new ScriptBundle("~/bundles/Pending").Include("~/Scripts/Pending.js"));
            #endregion
        }
    }
}
