using System.Collections.Generic;

namespace PInstaller
{
    class PackageConfig
    {
        public List<string> Plugins { get; set; }
        public string TargetFolder { get; set; }
        public List<BlockConfig> Blocks { get; set; }

        public PackageConfig()
        {
            Plugins = new List<string>();
            Blocks = new List<BlockConfig>();
        }
    }

    class BlockConfig
    {
        public string BlockName { get; set; }
        public object Parameters { get; set; }
    }
}
