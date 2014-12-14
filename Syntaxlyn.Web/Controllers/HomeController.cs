using System.Web.Mvc;

namespace Syntaxlyn.Web.Controllers
{
    public class HomeController : Controller
    {
        [Route("")]
        public ActionResult Index()
        {
            return this.View();
        }

        [Route("go")]
        public ActionResult Go(string service, string repo)
        {
            var s = repo.Split('/');
            return s.Length == 2
                ? this.RedirectToActionPermanent(
                    service,
                    "SourceView",
                    new { user = s[0], repo = s[1], path = "" }
                )
                : (ActionResult)this.HttpNotFound();
        }
    }
}