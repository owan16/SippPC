using Owin;
using System;
using System.Web.Http;
using System.Web.Http.Cors;
using WebApiContrib.Formatting.Jsonp;

namespace Sipp_PC
{
    public class SippServer
    {
        // This code configures Web API. The SippServer class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            var cors = new EnableCorsAttribute("scratchx.org", "*", "*");
            config.EnableCors(cors);
            config.Routes.MapHttpRoute(
                name: "Pedometer",
                routeTemplate: "services/{controller}/data/{type}"
            );
            config.Routes.MapHttpRoute(
                name: "Thing",
                routeTemplate: "services/{controller}/status/{type}"
                );
            config.Routes.MapHttpRoute(
                name: "Accelerometer",
                routeTemplate: "services/{controller}"
                );
            config.Routes.MapHttpRoute(
                name: "Gyro",
                routeTemplate: "services/{controller}"
                );
            var jsonpFormatter = new JsonpMediaTypeFormatter(config.Formatters.JsonFormatter);
            config.Formatters.Insert(0, jsonpFormatter);
            appBuilder.UseWebApi(config);
        }

        
    }
}