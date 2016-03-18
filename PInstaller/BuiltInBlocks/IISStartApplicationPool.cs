using Microsoft.Web.Administration;
using PI.Plugin.Exception;
using PI.Plugin.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PInstaller.BuiltInBlocks
{
    class IISStartApplicationPool : PIPlugin
    {
        public string BlockType()
        {
            return "IISStartApplicationPool";
        }

        public void Process(string jsonBlock, MainParameters mainParameters)
        {
            var appPools = GetData(jsonBlock);
            if (appPools.Count == 0) return;
            Console.WriteLine("Starting ApplicationPools...");
            using (var iisManager = new ServerManager())
            {
                foreach (var appPool in appPools)
                {
                    try
                    {
                        var pool = iisManager.ApplicationPools.FirstOrDefault(a => a.Name.ToLower() == appPool.ToLower());
                        if (pool == null) continue;
                        Console.WriteLine("\tApplicationPool: {0}", pool.Name);
                        pool.Start();
                    }
                    catch (Exception ex)
                    {
                        if (mainParameters.IsVerbose()) Console.WriteLine("Error: {0}", ex.Message);
                        Console.WriteLine("Couldn't start WebSite: {0}", appPool);
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
