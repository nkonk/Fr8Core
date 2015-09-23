﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Core.StructureMap;
using PluginBase;
using StructureMap;

namespace pluginSlack
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "PluginSlack",
                routeTemplate: "plugin_slack/{controller}/{id}"                
            );

            //Web API Exception Filter
            config.Filters.Add(new WebApiExceptionFilterAttribute());
        }
    }
}
