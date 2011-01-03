/*
	Script: Natural Docs Engine Post-Build Script

	Parameters:
		targetComponent - "Engine", "CLI", etc.
		buildType - "Debug" or "Release".
*/


// Group: Variables
// ____________________________________________________________________________


// var: fs
// FileSystem object
var fs = new ActiveXObject("Scripting.FileSystemObject");

// var: targetComponent
// The target component, such as "Engine" or "CLI".
var targetComponent = WScript.Arguments(0);

// var: buildType
// The current build configuration. "Debug" or "Release".
var buildType = WScript.Arguments(1);



// Group: Path Functions
// ____________________________________________________________________________


// Function: OutputPathOf
// Returns the full output path of the passed component and the current build configuration.
function OutputPathOf (component)
	{
	return "F:\\Projects\\Natural Docs 2\\Components\\" + component + "\\bin\\" + buildType;
	}

// Function: ResourcePathOf
// Returns the full resource path of the passed component.
function ResourcePathOf (component)
	{
	return "F:\\Projects\\Natural Docs 2\\Components\\" + component + "\\Resources";
	}



// Group: Support Functions
// ____________________________________________________________________________


// Function: CopyResourceFolder
// Copies the resource folder of the passed component to the output folder of the target component.
function CopyResourceFolder (resourceComponent, folder)
	{
	fs.CopyFolder( ResourcePathOf(resourceComponent) + "\\" + folder, OutputPathOf(targetComponent) + "\\" + folder, true );
	}


// Function: CopyResourceFiles
// Copies the contents of the resource folder of the passed component to the output folder of the target component.
function CopyResourceFiles (resourceComponent, folder)
	{
	fs.CopyFile( ResourcePathOf(resourceComponent) + "\\" + folder + "\\*.*", OutputPathOf(targetComponent), true );
	}



// Group: Parameter Validation
// ____________________________________________________________________________


if (buildType != "Debug" && buildType != "Release")
	{
	WScript.Echo("Unsupported build type " + buildType);
	WScript.Quit(1);
	}

if (!fs.folderExists(OutputPathOf(targetComponent)))
	{
	WScript.Echo("Target component " + targetComponent + " doesn't exist.");
	WScript.Quit(1);
	}



// Group: Main Code
// ____________________________________________________________________________


CopyResourceFiles("Engine", "SQLite");

CopyResourceFolder("Engine", "Config");
CopyResourceFolder("Engine", "Styles");
CopyResourceFolder("Engine", "Translations");

// The regex DLL will be copied by Visual Studio automatically because of the reference.
