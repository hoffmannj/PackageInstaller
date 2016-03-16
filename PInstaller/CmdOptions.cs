using CommandLine;

namespace PInstaller
{
    class CmdOptions
    {
        [VerbOption("certificates")]
        public CertificatesOptions CertificatesVerb { get; set; }

        [VerbOption("install")]
        public InstallOptions InstallVerb { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [VerbOption("help")]
        public HelpOptions HelpVerb { get; set; }
    }

    class HelpOptions
    { }

    class CertificatesOptions
    { }

    class InstallOptions
    {
        [Option('p', "package", Required = true)]
        public string PackageFile { get; set; }

        [Option('c', "config", Required = true)]
        public string ConfigFile { get; set; }

        [Option('b', "block", Required = false)]
        public string Block { get; set; }

        [Option('v', "verbose")]
        public bool Verbose { get; set; }
    }
}
