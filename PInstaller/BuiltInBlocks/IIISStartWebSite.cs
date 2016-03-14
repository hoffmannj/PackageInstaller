using Microsoft.Web.Administration;
using PI.Plugin.Exception;
using PI.Plugin.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PInstaller.BuiltInBlocks
{
    class IIISStartWebSite : PIPlugin
    {
        public string BlockName()
        {
            return "IISStartWebSite";
        }

        public void Process(string jsonBlock, MainParameters mainParameters)
        {
            var sites = GetData(jsonBlock);
            if (sites.Count == 0) return;
            Console.WriteLine("Starting WebSites...");
            using (var iisManager = new ServerManager())
            {
                foreach (var site in sites)
                {
                    try
                    {
                        var website = iisManager.Sites.FirstOrDefault(s => s.Name.ToLower() == site.ToLower());
                        if (website == null) continue;
                        Console.WriteLine("\tWebSite: {0}", site);
                        website.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Couldn't start WebSite: {0}", site);
                    }
                }
            }
        }

        private List<string> GetData(string jsonBlock)
        {
            List<string> data = null;
            try
            {
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(jsonBlock);
            }
            catch (Exception ex)
            {
                throw new PluginException(true, "Couldn't parse config block");
            }
            return data;
        }
    }
}
