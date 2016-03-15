using Microsoft.Web.Administration;
using PI.Plugin.Exception;
using PI.Plugin.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PInstaller.BuiltInBlocks
{
    class IISWebSite : PIPlugin
    {
        private class IISWebSiteParam
        {
            public string Name { get; set; }
            public string TargetFolder { get; set; }
            public string ApplicationPoolName { get; set; }
            public int Port { get; set; }

            public IISWebSiteParam()
            {
                Port = 80;
            }
        }

        public string BlockName()
        {
            return "IISWebSites";
        }

        public void Process(string jsonBlock, MainParameters mainParameters)
        {
            var websites = GetData(jsonBlock);
            if (websites.Count == 0) return;

            using (var iisManager = new ServerManager())
            {
                if (iisManager.Sites.Any(site => websites.Any(ws => ws.Name == site.Name)))
                {
                    Console.WriteLine("Removing existing sites...");
                    var existingSites = iisManager.Sites.ToList();
                    foreach (var site in existingSites)
                    {
                        try
                        {
                            if (websites.Any(ws => ws.Name.ToLower() == site.Name.ToLower()))
                            {
                                Console.WriteLine("\tWebSite: {0}", site.Name);
                                iisManager.Sites.Remove(site);
                            }
                            iisManager.CommitChanges();
                        }
                        catch (Exception ex)
                        {
                            throw new PluginException(true, string.Format("Couldn't remove WebSite: {0}", site.Name));
                        }
                    }
                }

                Console.WriteLine("Settin up WebSites...");
                foreach (var site in websites)
                {
                    try
                    {
                        Console.WriteLine("\tWebSite: {0}", site.Name);
                        var newSite = iisManager.Sites.Add(site.Name, site.TargetFolder.Replace("{%PackageTargetFolder%}", mainParameters.GetTargetFolder()), site.Port);
                        var mainApp = newSite.Applications.FirstOrDefault(a => a.Path == "/");
                        if (mainApp != null) mainApp.ApplicationPoolName = site.ApplicationPoolName;
                        iisManager.CommitChanges();
                    }
                    catch (Exception ex)
                    {
                        throw new PluginException(true, string.Format("Couldn't create WebSite: {0}", site.Name));
                    }
                }
            }
        }

        private List<IISWebSiteParam> GetData(string jsonBlock)
        {
            List<IISWebSiteParam> data = null;
            try
            {
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<IISWebSiteParam>>(jsonBlock);
            }
            catch (Exception ex)
            {
                throw new PluginException(true, "Couldn't parse config block");
            }
            return data;
        }
    }
}
