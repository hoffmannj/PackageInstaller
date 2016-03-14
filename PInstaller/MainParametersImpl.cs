using PI.Plugin.Interface;

namespace PInstaller
{
    class MainParametersImpl : MainParameters
    {
        private string targetFolder;
        private bool verbose;

        public MainParametersImpl(string targetFolder, bool verbose)
        {
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
    }
}
