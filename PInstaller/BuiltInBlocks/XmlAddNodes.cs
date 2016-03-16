using PI.Plugin.Exception;
using PI.Plugin.Interface;
using System;
using System.Collections.Generic;
using System.Xml;

namespace PInstaller.BuiltInBlocks
{
    class XmlAddNodes : PIPlugin
    {
        private class XmlAddNodeParams
        {
            public string FilePath { get; set; }
            public string NodeXPath { get; set; }
            public string PlusNode { get; set; }
        }

        public string BlockType()
        {
            return "XmlAddNodes";
        }

        public void Process(string jsonBlock, MainParameters mainParameters)
        {
            var changes = GetData(jsonBlock);
            if (changes.Count == 0) return;

            Console.WriteLine("Adding nodes to XML files...");
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
                    var td = new XmlDocument();
                    td.LoadXml(change.PlusNode);
                    var newNode = (XmlNode)td.DocumentElement;
                    newNode = doc.ImportNode(newNode, true);
                    node.AppendChild(newNode);
                    doc.Save(fp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Couldn't add node to the XML file: {0}", change.NodeXPath);
                    if (mainParameters.IsVerbose()) Console.WriteLine(ex.Message);
                }
            }
        }

        private List<XmlAddNodeParams> GetData(string jsonBlock)
        {
            List<XmlAddNodeParams> data = null;
            try
            {
                data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<XmlAddNodeParams>>(jsonBlock);
            }
            catch (Exception ex)
            {
                throw new PluginException(true, "Couldn't parse config block");
            }
            return data;
        }
    }
}
