/*
	Script: Natural Docs Engine Post-Build Script

	Parameters:
		componentFolder - The root folder of all components, such as "F:\Projects\Natural Docs 2\Components\".
											 It is assumed that it can reach any other component by adding the component's folder name.
		outputFolder - The folder to use as an output target, such as "F:\Projects\Natural Docs 2\Components\Engine\bin\Debug\".
*/


// Group: Variables
// ____________________________________________________________________________


// var: fs
// FileSystem object
var fs = new ActiveXObject("Scripting.FileSystemObject");

// var: componentFolder
var componentFolder = WScript.Arguments(0);

// var: outputFolder
var outputFolder = WScript.Arguments(1);


if (!fs.folderExists(componentFolder))
	{
	WScript.Echo("Component folder " + componentFolder + " doesn't exist.");
	WScript.Quit(1);
	}

if (!fs.folderExists(outputFolder))
	{
	WScript.Echo("Output folder " + outputFolder + " doesn't exist.");
	WScript.Quit(1);
	}



// Group: Support Functions
// ____________________________________________________________________________


// Function: ResourceFolderOf
// Returns the full resource path of the passed component.
function ResourceFolderOf (component)
	{
	return componentFolder + component + "\\Resources\\";
	}


// Function: CopyResourceFolder
// Copies the resource folder of the passed component to the output folder.
function CopyResourceFolder (component, subfolder)
	{
	var resourceFolder = ResourceFolderOf(component) + subfolder;

	if (!fs.folderExists(resourceFolder))
		{
		WScript.Echo("Resource folder " + resourceFolder + " doesn't exist.");
		WScript.Quit(1);
		}

	fs.CopyFolder( resourceFolder, outputFolder + subfolder, true );
	}


// Function: CopyResourceFiles
// Copies the contents of the resource folder of the passed component to the output folder of the target component.
function CopyResourceFiles (component, subfolder)
	{
	var resourceFolder = ResourceFolderOf(component) + subfolder;

	if (!fs.folderExists(resourceFolder))
		{
		WScript.Echo("Resource folder " + resourceFolder + " doesn't exist.");
		WScript.Quit(1);
		}

	fs.CopyFile( resourceFolder + "\\*.*", outputFolder, true );
	}




// Group: Main Code
// ____________________________________________________________________________


CopyResourceFiles("Engine", "SQLite");
CopyResourceFiles("Engine", "License");

CopyResourceFolder("Engine", "Config");
CopyResourceFolder("Engine", "Styles");
CopyResourceFolder("Engine", "Translations");

// The regex DLL will be copied by Visual Studio automatically because of the reference.
