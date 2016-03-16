using Microsoft.Web.Administration;
using PI.Plugin.Exception;
using PI.Plugin.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PInstaller.BuiltInBlocks
{
    class IISWebApplication : PIPlugin
    {
        private class IISApplicationParam
        {
            public string WebSiteName { get; set; }
            public string Name { get; set; }
            public string TargetFolder { get; set; }
            public string ApplicationPoolName { get; set; }
        }

        public string BlockType()
        {
            return "IISWebApplications";
        }

        public void Process(string jsonBlock, MainParameters mainParameters)
        {
            var apps = GetData(jsonBlock);
            if (apps.Count == 0) return;

            using (var iisManager = new ServerManager())
            {

                Console.WriteLine("Removing existing applications...");
                foreach (var app in apps)
                {
                    try
                    {
                        var website = iisManager.Sites.FirstOrDefault(s => s.Name.ToLower() == app.WebSiteName.ToLower());
                        if (website == null) continue;
                        var wapp = website.Applications.FirstOrDefault(a => a.Path.ToLower() == app.Name.ToLower());
                        if (wapp == null) continue;
                        Console.WriteLine("\tWebSite: {0}   Path: {1}", app.WebSiteName, app.Name);
                        website.Applications.Remove(wapp);
                        iisManager.CommitChanges();
                    }
                    catch (Exception ex)
                    {
                        if (mainParameters.IsVerbose()) Console.WriteLine("Error: {0}", ex.Message);
                        throw new PluginException(true, string.Format("Couldn't remove Application: {0}", app.Name));
                    }
                }

                Console.WriteLine("Setting up Applications...");
                foreach (var app in apps)
                {
                    try
                    {
                        var website = iisManager.Sites.FirstOrDefault(s => s.Name.ToLower() == app.WebSiteName.ToLower());
                        if (website == null)
                        {
                            Console.WriteLine("\tUnknown WebSite: {0}", app.WebSiteName);
                            continue;
                        }
                        Console.WriteLine("\tWebSite: {0}  Path: {1}", app.WebSiteName, app.Name);
                        var appTF = app.TargetFolder.Replace("{%PackageTargetFolder%}", mainParameters.GetTargetFolder());
                        var newApp = website.Applications.Add("/" + app.Name, appTF);
                        newApp.ApplicationPoolName = app.ApplicationPoolName;
                        iisManager.CommitChanges();
                    }
                    catch (Exception ex)
                    {
                        if (mainParameters.IsVerbose()) Console.WriteLine("Error: {0}", ex.Message);
                        throw new PluginException(true, string.Format("Couldn't create Application: {0}", app.Name));
                    }
                }
            }
        }

        private List<IISApplicationParam> GetData(string jsonBlock)
        {
            List<IISApplicationParam> data = null;
            try
            {
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<IISApplicationParam>>(jsonBlock);
            }
            catch (Exception ex)
            {
                throw new PluginException(true, "Couldn't parse config block");
            }
            return data;
        }
    }
}
