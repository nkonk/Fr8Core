﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin;
using Newtonsoft.Json;
using Owin;
using PluginBase;
using PluginBase.BaseClasses;

[assembly: OwinStartup(typeof(pluginDocuSign.Startup))]

namespace pluginDocuSign
{
    public class Startup : BaseConfiguration
    {
        public void Configuration(IAppBuilder app)
        {
            StartHosting("plugin_docusign");
        }
    }
}
