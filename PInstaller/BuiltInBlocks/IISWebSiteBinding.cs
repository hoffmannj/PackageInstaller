using Microsoft.Web.Administration;
using PI.Plugin.Exception;
using PI.Plugin.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace PInstaller.BuiltInBlocks
{
    class IISWebSiteBinding : PIPlugin
    {
        private class IISBindingParam
        {
            public string WebSiteName { get; set; }
            public string IPAddress { get; set; }
            public int Port { get; set; }
            public string Protocol { get; set; }
            public string HostHeader { get; set; }
            public string CertificateName { get; set; }
            public bool ServerNameIndication { get; set; }

            public IISBindingParam()
            {
                Port = 80;
                IPAddress = "*";
                Protocol = "http";
                HostHeader = string.Empty;
                CertificateName = string.Empty;
            }
        }

        public string BlockName()
        {
            return "IISWebSiteBindings";
        }

        public void Process(string jsonBlock, MainParameters mainParameters)
        {
            var bindings = GetData(jsonBlock);
            if (bindings.Count == 0) return;

            using (var iisManager = new ServerManager())
            {
                Console.WriteLine("Clear existing bindings...");
                var sites = bindings.Select(b => b.WebSiteName).Distinct().ToList();
                foreach (var site in sites)
                {
                    try
                    {
                        var website = iisManager.Sites.FirstOrDefault(s => s.Name.ToLower() == site.ToLower());
                        if (website == null)
                        {
                            Console.WriteLine("\tSite not found: {0}", site);
                            continue;
                        }
                        website.Bindings.Clear();
                        iisManager.CommitChanges();
                    }
                    catch { }
                }

                Console.WriteLine("Setting bindings...");
                foreach (var binding in bindings)
                {
                    Console.WriteLine("\tWebSite: {0}  Protocol: {1}", binding.WebSiteName, binding.Protocol);
                    try
                    {
                        var site = iisManager.Sites.FirstOrDefault(s => s.Name.ToLower() == binding.WebSiteName.ToLower());
                        if (site == null) continue;
                        var bindingInfo = string.Format("{0}:{1}:{2}", binding.IPAddress, binding.Port, binding.HostHeader);
                        if (!string.IsNullOrEmpty(binding.CertificateName))
                        {
                            var hash = GetCertificateHash(binding.CertificateName);
                            if (hash == null)
                            {
                                throw new Exception(string.Format("Couldn't find certificate: {0}", binding.CertificateName));
                            }
                            var newBinding = site.Bindings.Add(bindingInfo, hash.Item2, hash.Item1);
                            var flag = newBinding.Attributes.FirstOrDefault(a => a.Name.ToLower() == "sslflags");
                            if (flag != null && binding.ServerNameIndication)
                            {
                                flag.Value = 1;
                            }
                        }
                        else
                        {
                            var newBinding = site.Bindings.Add(bindingInfo, binding.Protocol);
                        }
                        iisManager.CommitChanges();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: {0}", ex.Message);
                    }
                }
            }
        }

        private Tuple<string,byte[]> GetCertificateHash(string certName)
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

            var cert = certs.FirstOrDefault(c => c.Item2.FriendlyName.ToLower() == certName.ToLower());
            if (cert != null) return new Tuple<string, byte[]>(cert.Item1, cert.Item2.GetCertHash());

            cert = certs.FirstOrDefault(c => GetCN(c.Item2.Subject).ToLower().StartsWith(certName.ToLower()));
            if (cert != null) return new Tuple<string, byte[]>(cert.Item1, cert.Item2.GetCertHash());

            return null;
        }

        private string GetCN(string cnData)
        {
            var cn = cnData.Split(',').Select(p => p.Trim().Split('=')).FirstOrDefault(p => p[0] == "CN");
            if (cn != null) return cn[1];
            return string.Empty;
        }

        private List<IISBindingParam> GetData(string jsonBlock)
        {
            List<IISBindingParam> data = null;
            try
            {
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<IISBindingParam>>(jsonBlock);
            }
            catch (Exception ex)
            {
                throw new PluginException(true, "Couldn't parse config block");
            }
            return data;
        }
    }
}
