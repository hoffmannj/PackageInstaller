using PI.Plugin.Interface;

namespace PInstaller
{
    class MainParametersImpl : MainParameters
    {
        private string packagePath;
        private string targetFolder;
        private bool verbose;

        public MainParametersImpl(string packagePath, string targetFolder, bool verbose)
        {
            this.packagePath = packagePath;
            this.targetFolder = targetFolder;
            this.verbose = verbose;
        }

        public string GetTargetFolder()
        {
            return targetFolder;
        }

        public bool IsVerbose()
        {
            return verbose;
        }

        public string GetPackagePath()
        {
            return packagePath;
        }
    }
}
