using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using serverapiorg.Models;

namespace serverapiorg
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected async void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            WSServer server = new WSServer();
            await server.Start("http://127.0.0.1:7980/");
        }
    }
}
