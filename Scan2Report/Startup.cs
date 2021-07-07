using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Scan2Report.Startup))]
namespace Scan2Report
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
