# PackageInstaller

This is small tool that can be used to install a package (zip file).  
Usage:
* PInstaller.exe certificates  
That command lists the available certificates. (useful for finding certificate for SSL in IIS)
* PInstaller.exe -p < packageFilePath >  -c < configFilePath > [-v]  
That command installs the package according to the config file. -v means verbose.  
  
The tool is extendable with plugins. It has a couple built-in plugins:
* CopyFiles
* CreateFolders
* IISStartWebSite
* IISApplicationPools
* IISWebApplication
* IISWebSite
* IISWebSiteBinding
* XmlAddNodes
* XmlChanges  

The plugin interface is really simple. It looks like this:  
```C#
public interface PIPlugin
{
	string BlockName();
	void Process(string jsonBlock, MainParameters mainParameters);
}
```
  
The config file is basically a JSON file. An empty config file looks like this:  
```Javascript
{
    "Plugins" : [],
    "TargetFolder" : "",
    "Blocks" : []
}
```

This is an example "CopyFiles" command block:
```Javascript
{
	"BlockName" : "CopyFiles",
	"Parameters" : [
		{
			"SourcePath" : "web.config",
			"TargetPath" : "{%PackageTargetFolder%}\\web.config",
			"Overwrite" : true
		}
	]
}
```

NOTE: "{%PackageTargetFolder%}" is the only placeholder you can use in the config file.

The "Plugins" field is a string array. Add the full name of the Plugin class you'd like to use. A plugin is responsible for a "BlockName", and there can be only one plugin to handle a "BlockName".
  
For more details about the built-in plugins take a look at the code.
  
