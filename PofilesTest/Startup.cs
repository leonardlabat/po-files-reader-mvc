using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PofilesTest.Startup))]
namespace PofilesTest
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
