using PI.Plugin.Exception;
using PI.Plugin.Interface;
using System;
using System.Collections.Generic;

namespace PInstaller.BuiltInBlocks
{
    class CopyFiles : PIPlugin
    {
        class Parameters
        {
            public string SourcePath { get; set; }
            public string TargetPath { get; set; }
            public bool Overwrite { get; set; }
            public bool Critical { get; set; }

            public Parameters()
            {
                Overwrite = true;
                Critical = false;
            }
        }


        public string BlockType()
        {
            return "CopyFiles";
        }

        public void Process(string jsonBlock, MainParameters mainParameters)
        {
            var copyList = GetData(jsonBlock);
            if (copyList.Count == 0) return;
            Console.WriteLine("Copying files...");
            foreach (var copy in copyList)
            {
                try
                {
                    Console.WriteLine("\tFile: {0}", copy.SourcePath);
                    System.IO.File.Copy(
                        copy.SourcePath.Replace("{%PackageTargetFolder%}", mainParameters.GetTargetFolder()),
                        copy.TargetPath.Replace("{%PackageTargetFolder%}", mainParameters.GetTargetFolder()),
                        copy.Overwrite);
                }
                catch (Exception ex)
                {
                    if (mainParameters.IsVerbose()) Console.WriteLine("Error: {0}", ex.Message);
                    var msg = string.Format("Couldn't copy file: \nFrom: {0}\nTo: {1}", copy.SourcePath, copy.TargetPath);
                    if (!copy.Critical) Console.WriteLine(msg);
                    else throw new PluginException(copy.Critical, msg);
                }
            }
        }

        private List<Parameters> GetData(string jsonBlock)
        {
            List<Parameters> data = null;
            try
            {
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Parameters>>(jsonBlock);
            }
            catch (Exception ex)
            {
                throw new PluginException(true, "Couldn't parse config block");
            }
            return data;
        }
    }
}
