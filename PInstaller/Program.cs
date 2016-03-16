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
            Console.WriteLine("\tPInstaller <command> <parameters>");
            Console.WriteLine();
            Console.WriteLine("Command: install");
            Console.WriteLine("Parameters: -p <packageZipFile> -c <configFile> [-b <blocks>] [-v]");
            Console.WriteLine("Description: Installs a package according to the contents of the config file. '-v' means verbose output.");
            Console.WriteLine("\t<blocks> is a comma sparated list of blocks to execute");
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
            var mainParams = new MainParametersImpl(options.PackageFile, config.TargetFolder, options.Verbose);
            var blocksToExecute = (options.Block ?? "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(b => b.Trim()).Distinct().ToList();
            foreach (var block in blocksToExecute)
            {
                if (!config.Blocks.Any(b => b.BlockName.ToLower() == block.ToLower()))
                {
                    Console.WriteLine("Block doesn't exist in the config file: {0}", block);
                    return;
                }
            }
            foreach (var block in config.Blocks)
            {
                if (blocksToExecute.Count > 0 && !blocksToExecute.Any(b => b.ToLower() == block.BlockName.ToLower())) continue;

                if (plugins.ContainsKey(block.BlockType))
                {
                    try
                    {
                        plugins[block.BlockType].Process(JsonConvert.SerializeObject(block.Parameters), mainParams);
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
                if (p != null) plugins.Add(p.BlockType(), p);
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
                    var blockType = obj.BlockType();
                    if (plugins.ContainsKey(blockType))
                    {
                        var msg = string.Format("A plugin with the same BlockType already exists: {0}", blockType);
                        if (verbose) Console.WriteLine(msg);
                        throw new Exception(msg);
                    }
                    plugins[blockType] = obj;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Couldn't load plugins");
                plugins.Clear();
                return null;
            }

            var names = new HashSet<string>();
            foreach (var block in config.Blocks)
            {
                if (names.Contains(block.BlockName))
                {
                    Console.WriteLine("BlockName is duplicated: {0}", block.BlockName);
                    plugins.Clear();
                    return null;
                }
                names.Add(block.BlockName);
                if (!plugins.ContainsKey(block.BlockType))
                {
                    Console.WriteLine("There is no matching plugin for block: {0}", block.BlockType);
                    plugins.Clear();
                    return null;
                }
            }

            return config;
        }
    }
}
