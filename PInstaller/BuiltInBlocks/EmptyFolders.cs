using PI.Plugin.Exception;
using PI.Plugin.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PInstaller.BuiltInBlocks
{
    class EmptyFolders : PIPlugin
    {
        public string BlockType()
        {
            return "EmptyFolders";
        }

        public void Process(string jsonBlock, MainParameters mainParameters)
        {
            var folders = GetData(jsonBlock);
            if (folders.Count == 0) return;
            Console.WriteLine("Clearing folders...");
            foreach (var folder in folders)
            {
                try
                {
                    Console.WriteLine("\tFolder: {0}", folder);
                    EmptyFolder(folder.Replace("{%PackageTargetFolder%}", mainParameters.GetTargetFolder()));
                }
                catch (Exception ex)
                {
                    if (mainParameters.IsVerbose()) Console.WriteLine("Error: {0}", ex.Message);
                    throw new PluginException(true, string.Format("Couldn't empty folder: {0}", folder));
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

        private void EmptyFolder(string path)
        {
            System.IO.Directory.EnumerateFiles(path).ToList().ForEach(System.IO.File.Delete);
            System.IO.Directory.EnumerateDirectories(path).ToList().ForEach(EmptyFolder);
            System.IO.Directory.EnumerateDirectories(path).ToList().ForEach(System.IO.Directory.Delete);
        }
    }
}
