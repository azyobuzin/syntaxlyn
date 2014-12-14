using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Syntaxlyn.Web.Startup))]

namespace Syntaxlyn.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
