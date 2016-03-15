using PI.Plugin.Exception;
using PI.Plugin.Interface;
using System;
using System.Collections.Generic;

namespace PInstaller.BuiltInBlocks
{
    class CreateFolders : PIPlugin
    {
        public string BlockName()
        {
            return "CreateFolders";
        }

        public void Process(string jsonBlock, MainParameters mainParameters)
        {
            var folders = GetData(jsonBlock);
            if (folders.Count == 0) return;
            Console.WriteLine("Creating folders...");
            foreach (var folder in folders)
            {
                try
                {
                    Console.WriteLine("\tFolder: {0}", folder);
                    System.IO.Directory.CreateDirectory(folder.Replace("{%PackageTargetFolder%}", mainParameters.GetTargetFolder()));
                }
                catch (Exception ex)
                {
                    if (mainParameters.IsVerbose()) Console.WriteLine("Error: {0}", ex.Message);
                    throw new PluginException(true, string.Format("Couldn't create folder: {0}", folder));
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
