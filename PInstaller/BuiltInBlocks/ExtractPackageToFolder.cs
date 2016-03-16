using PI.Plugin.Exception;
using PI.Plugin.Interface;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PInstaller.BuiltInBlocks
{
    class ExtractPackageToFolder : PIPlugin
    {
        public string BlockType()
        {
            return "ExtractPackageToFolder";
        }

        public void Process(string jsonBlock, MainParameters mainParameters)
        {
            var tf = GetData(jsonBlock);
            Console.WriteLine("Extracting package to folder...");
            try
            {
                Console.Write("\tExtracting files... 0%");
                var zf = ZipFile.Open(mainParameters.GetPackagePath(), ZipArchiveMode.Read);
                var entriesCount = zf.Entries.Count;
                int i = 0;
                foreach (var ze in zf.Entries)
                {
                    ++i;
                    var p = (i * 100) / entriesCount;
                    var target = System.IO.Path.Combine(mainParameters.GetTargetFolder(), ze.FullName);
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target));
                    if (!string.IsNullOrEmpty(System.IO.Path.GetFileName(target))) ze.ExtractToFile(target);
                    Console.Write("\r\tExtracting files... {0}%", p);
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                if (mainParameters.IsVerbose()) Console.WriteLine(ex.Message);
                throw new PluginException(true, "Couldn't extract the files to the target folder");
            }
        }

        private string GetData(string jsonBlock)
        {
            string data = null;
            try
            {
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(jsonBlock);
            }
            catch (Exception ex)
            {
                throw new PluginException(true, "Couldn't parse config block");
            }
            return data;
        }
    }
}
