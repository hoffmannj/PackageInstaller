using Microsoft.Web.Administration;
using PI.Plugin.Exception;
using PI.Plugin.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PInstaller.BuiltInBlocks
{
    class IISApplicationPools : PIPlugin
    {
        private class IISWebApplicationPool
        {
            public string Name { get; set; }
            public bool IsIntegrated { get; set; }
            public bool Enable32Bit { get; set; }
            public string ManagedRuntimeVersion { get; set; }
            public bool AutoStart { get; set; }
            public bool AlwaysRunning { get; set; }

            public IISWebApplicationPool()
            {
                IsIntegrated = true;
                Enable32Bit = true;
                ManagedRuntimeVersion = "v4.0";
                AutoStart = true;
                AlwaysRunning = true;
            }
        }

        public string BlockType()
        {
            return "IISApplicationPools";
        }

        public void Process(string jsonBlock, MainParameters mainParameters)
        {
            var appPools = GetData(jsonBlock);
            if (appPools.Count == 0) return;
            using (var iisManager = new ServerManager())
            {
                Console.WriteLine("Removing existing ApplicationPools...");
                foreach (var appPool in appPools)
                {
                    try
                    {
                        var pool = iisManager.ApplicationPools.FirstOrDefault(ap => ap.Name.ToLower() == appPool.Name.ToLower());
                        if (pool != null)
                        {
                            Console.WriteLine("\tApplicationPool: {0}", appPool.Name);
                            if (pool.State != ObjectState.Stopped)
                            {
                                try
                                {
                                    pool.Stop();
                                }
                                catch { }
                            }
                            iisManager.ApplicationPools.Remove(pool);
                        }
                        iisManager.CommitChanges();
                    }
                    catch (Exception ex)
                    {
                        if (mainParameters.IsVerbose()) Console.WriteLine("Error: {0}", ex.Message);
                        throw new PluginException(true, string.Format("Couldn't remove ApplicationPool: {0}", appPool.Name));
                    }
                }

                Console.WriteLine("Creating ApplicationPools...");
                foreach (var appPool in appPools)
                {
                    try
                    {
                        Console.WriteLine("\tApplicationPool: {0}", appPool.Name);
                        ApplicationPool newPool = iisManager.ApplicationPools.Add(appPool.Name);
                        newPool.ManagedRuntimeVersion = appPool.ManagedRuntimeVersion;
                        newPool.AutoStart = appPool.AutoStart;
                        newPool.Enable32BitAppOnWin64 = appPool.Enable32Bit;
                        newPool.ManagedPipelineMode = appPool.IsIntegrated ? ManagedPipelineMode.Integrated : ManagedPipelineMode.Classic;
                        newPool.Recycling.PeriodicRestart.PrivateMemory = 1024 * 1024;
                        newPool.Recycling.PeriodicRestart.Time = TimeSpan.Zero;
                        newPool.ProcessModel.IdleTimeout = TimeSpan.Zero;
                        var attr = newPool.Attributes.FirstOrDefault(a => a.Name == "startMode");
                        if (attr != null) attr.Value = appPool.AlwaysRunning ? 1 : 0; // OnDemand = 0;   AlwaysRunning = 1
                        iisManager.CommitChanges();
                    }
                    catch (Exception ex)
                    {
                        if (mainParameters.IsVerbose()) Console.WriteLine("Error: {0}", ex.Message);
                        throw new PluginException(true, string.Format("Couldn't create ApplicationPool: {0}", appPool.Name));
                    }
                }
            }
        }

        private List<IISWebApplicationPool> GetData(string jsonBlock)
        {
            List<IISWebApplicationPool> data = null;
            try
            {
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<IISWebApplicationPool>>(jsonBlock);
            }
            catch (Exception ex)
            {
                throw new PluginException(true, "Couldn't parse config block");
            }
            return data;
        }
    }
}
