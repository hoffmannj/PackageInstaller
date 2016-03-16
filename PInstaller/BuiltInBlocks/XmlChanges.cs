using PI.Plugin.Exception;
using PI.Plugin.Interface;
using System;
using System.Collections.Generic;
using System.Xml;

namespace PInstaller.BuiltInBlocks
{
    class XmlChanges : PIPlugin
    {
        private class XmlChangeParams
        {
            public string FilePath { get; set; }
            public string NodeXPath { get; set; }
            public string TargetAttributeName { get; set; }
            public string Value { get; set; }
        }

        public string BlockType()
        {
            return "XmlChanges";
        }

        public void Process(string jsonBlock, MainParameters mainParameters)
        {
            var changes = GetData(jsonBlock);
            if (changes.Count == 0) return;

            Console.WriteLine("Changing XML files...");
            foreach (var change in changes)
            {
                Console.WriteLine("\tFile: {0}", change.FilePath);
                Console.WriteLine("\tNode: {0}", change.NodeXPath);
                try
                {
                    var fp = change.FilePath.Replace("{%PackageTargetFolder%}", mainParameters.GetTargetFolder());
                    var doc = new XmlDocument();
                    doc.Load(fp);
                    var node = doc.SelectSingleNode(change.NodeXPath);
                    var value = change.Value.Replace("&quot;", "\"");
                    if (!string.IsNullOrEmpty(change.TargetAttributeName))
                    {
                        node.Attributes[change.TargetAttributeName].Value = value;
                    }
                    else
                    {
                        node.InnerText = value;
                    }
                    doc.Save(fp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't change values in XML file: {0}", change.NodeXPath);
                    if (mainParameters.IsVerbose()) Console.WriteLine(ex.Message);
                }
            }
        }

        private List<XmlChangeParams> GetData(string jsonBlock)
        {
            List<XmlChangeParams> data = null;
            try
            {
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<XmlChangeParams>>(jsonBlock);
            }
            catch (Exception ex)
            {
                throw new PluginException(true, "Couldn't parse config block");
            }
            return data;
        }
    }
}
