using Newtonsoft.Json;
using PI.Plugin.Exception;
using PI.Plugin.Interface;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace PInstaller
{
    class Program
    {
        private static bool Verbose = false;
        private Dictionary<string, PIPlugin> plugins = new Dictionary<string, PIPlugin>();

        static void Main(string[] args)
        {
            var prg = new Program();
            string invokedVerb = string.Empty;
            object invokedVerbInstance = null;

            var options = new CmdOptions();
            var p = new CommandLine.Parser(s =>
            {
                s.IgnoreUnknownArguments = true;
            });
            if (!p.ParseArguments(args, options,
              (verb, subOptions) =>
              {
                  invokedVerb = verb;
                  invokedVerbInstance = subOptions;
              }))
            {
                invokedVerb = "help";
                invokedVerbInstance = options.HelpVerb;
            }

            if (invokedVerb == "certificates")
            {
                prg.GetListOfCertificates();
            }
            else if (invokedVerb == "install")
            {
                var installOptions = (InstallOptions)invokedVerbInstance;
                Verbose = installOptions.Verbose;
                prg.InstallPackage(installOptions);
            }
            else if (string.IsNullOrEmpty(invokedVerb) || invokedVerb == "help")
            {
                prg.ShowHelp();
            }
        }

        public void ShowHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\tPackageInstaller <command> <parameters>");
            Console.WriteLine();
            Console.WriteLine("Command: install");
            Console.WriteLine("Parameters: -p <packageZipFile> -c <configFile> [-v]");
            Console.WriteLine("Description: Installs a package according to the contents of the config file. '-v' means verbose output.");
            Console.WriteLine();
            Console.WriteLine("Command: certificates");
            Console.WriteLine("Description: Prints out a list of available certificate names.");
            Console.WriteLine();
            Console.WriteLine("Command: help");
            Console.WriteLine("Description: This help.");
        }

        public void GetListOfCertificates()
        {
            var certs = new List<Tuple<string, X509Certificate2>>();
            //WebHosting
            var store = new X509Store("WebHosting", StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            certs.AddRange(store.Certificates.Cast<X509Certificate2>().Select(c => new Tuple<string, X509Certificate2>("WebHosting", c)));
            store.Close();

            //My
            store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            certs.AddRange(store.Certificates.Cast<X509Certificate2>().Select(c => new Tuple<string, X509Certificate2>("My", c)));
            store.Close();

            certs = certs.Distinct().ToList();

            foreach (var t in certs)
            {
                Console.WriteLine("Store: {0}   Friendly: {1}   CN: {2}", t.Item1, t.Item2.FriendlyName, GetCN(t.Item2.Subject));
            }
        }

        public void InstallPackage(InstallOptions options)
        {
            AddBuiltInPlugins();
            var config = ValidateConfig(options.ConfigFile, options.Verbose);
            if (config == null) return;
            var mainParams = new MainParametersImpl(config.TargetFolder, options.Verbose);
            if (!SetupTargetFolder(options.PackageFile, mainParams)) return;
            foreach (var block in config.Blocks)
            {
                if (plugins.ContainsKey(block.BlockName))
                {
                    try
                    {
                        plugins[block.BlockName].Process(JsonConvert.SerializeObject(block.Parameters), mainParams);
                    }
                    catch (PluginException pe)
                    {
                        Console.WriteLine(string.Format("PluginException in block: {0}  Critical: {1}", block.BlockName, pe.Critical ? "Yes" : "No"));
                        if (options.Verbose) Console.WriteLine(string.Format("Message: {0}", pe.Message));
                        if(pe.Critical) break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(string.Format("Exception in block: {0}", block.BlockName));
                        if (options.Verbose) Console.WriteLine(string.Format("Message: {0}", ex.Message));
                        break;
                    }
                }
            }
        }

        private string GetCN(string cnData)
        {
            var cn = cnData.Split(',').Select(p => p.Trim().Split('=')).FirstOrDefault(p => p[0] == "CN");
            if (cn != null) return cn[1];
            return string.Empty;
        }

        private void AddBuiltInPlugins()
        {
            plugins.Clear();

            var builtInPlugins = System.Reflection.Assembly
                .GetExecutingAssembly()
                .DefinedTypes
                .Where(t => t.ImplementedInterfaces.Contains(typeof(PIPlugin)))
                .ToList();
            foreach (var type in builtInPlugins)
            {
                var p = Activator.CreateInstance(type) as PIPlugin;
                if (p != null) plugins.Add(p.BlockName(), p);
            }
        }

        private PackageConfig ValidateConfig(string configPath, bool verbose)
        {
            if (verbose) Console.WriteLine("Validating config file: {0}", configPath);
            if (!System.IO.File.Exists(configPath))
            {
                Console.WriteLine("Couldn't find config file");
                return null;
            }

            string configText = string.Empty;
            try
            {
                configText = System.IO.File.ReadAllText(configPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't read the config file");
                return null;
            }

            PackageConfig config = null;
            try
            {
                config = JsonConvert.DeserializeObject<PackageConfig>(configText);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't parse the config file");
                return null;
            }

            try
            {
                foreach (var pluginName in config.Plugins)
                {
                    var t = Type.GetType(pluginName);
                    var obj = Activator.CreateInstance(t) as PIPlugin;
                    if (obj == null)
                    {
                        var msg = string.Format("Not valid plugin type: {0}", pluginName);
                        if (verbose) Console.WriteLine(msg);
                        throw new Exception(msg);
                    }
                    var blockName = obj.BlockName();
                    if (plugins.ContainsKey(blockName))
                    {
                        var msg = string.Format("A plugin with the same BlockName already exists: {0}", blockName);
                        if (verbose) Console.WriteLine(msg);
                        throw new Exception(msg);
                    }
                    plugins[blockName] = obj;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't load plugins");
                plugins.Clear();
                return null;
            }

            foreach (var block in config.Blocks)
            {
                if (!plugins.ContainsKey(block.BlockName))
                {
                    Console.WriteLine("There is no matching plugin for block: {0}", block.BlockName);
                    plugins.Clear();
                    return null;
                }
            }

            return config;
        }

        private bool ClearTargetFolder(MainParameters mainParams)
        {
            try
            {
                Console.WriteLine("Setting up target folder...");
                if (!System.IO.Directory.Exists(mainParams.GetTargetFolder()))
                {
                    System.IO.Directory.CreateDirectory(mainParams.GetTargetFolder());
                }
                Action<string> DelPath = null;
                DelPath = p =>
                {
                    System.IO.Directory.EnumerateFiles(p).ToList().ForEach(System.IO.File.Delete);
                    System.IO.Directory.EnumerateDirectories(p).ToList().ForEach(DelPath);
                    System.IO.Directory.EnumerateDirectories(p).ToList().ForEach(System.IO.Directory.Delete);
                };
                DelPath(mainParams.GetTargetFolder());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: Couldn't create the target folder");
                if (mainParams.IsVerbose()) Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        private bool ExtractPackageToTargetFolder(string packageFile, MainParameters mainParams)
        {
            try
            {
                Console.Write("Extracting files... 0%");
                var zf = ZipFile.Open(packageFile, ZipArchiveMode.Read);
                var entriesCount = zf.Entries.Count;
                int i = 0;
                foreach (var ze in zf.Entries)
                {
                    ++i;
                    var p = (i * 100) / entriesCount;
                    var target = System.IO.Path.Combine(mainParams.GetTargetFolder(), ze.FullName);
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target));
                    if (!string.IsNullOrEmpty(System.IO.Path.GetFileName(target))) ze.ExtractToFile(target);
                    Console.Write("\rExtracting files... {0}%", p);
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nError: Couldn't extract the files to the target folder");
                if (mainParams.IsVerbose()) Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        private bool SetupTargetFolder(string packageFile, MainParameters mainParams)
        {
            if (!ClearTargetFolder(mainParams)) return false;
            if (!ExtractPackageToTargetFolder(packageFile, mainParams)) return false;
            return true;
        }
    }
}
