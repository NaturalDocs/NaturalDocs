/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.Framework.TestEngine
 * ____________________________________________________________________________
 * 
 * A class that simplifies configuring and running <Engine.Instance> for tests.
 * 
 * Usage:
 * 
 *		- Call <Start()> with a folder of test data.
 *			
 *		- Call <Run()> if you want to generate HTML.  This is not necessary if you only want to test text conversion
 *		  functions directly.
 *		  
 *		- Call <Dispose()> when you're done.  This will remove the temporary folders in addition to disposing of the engine.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using GregValure.NaturalDocs.Engine;


namespace GregValure.NaturalDocs.Engine.Tests.Framework
	{
	public static class TestEngine
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Constructor: TestEngine
		 */
		static TestEngine ()
			{
			inputFolder = null;
			projectConfigFolder = null;
			workingDataFolder = null;
			outputFolder = null;

			temporaryFolderRoot = null;

			keepOutputFolder = false;
			}


		/* Function: Start
		 * 
		 * Starts <Engine.Instance> using the passed folder of test data.
		 * 
		 * If the input folder is relative it will take the executing assembly path, skip up until it finds "Components", move 
		 * into the "EngineTests\Test Data" subfolder, and then make the path relative to that.  This is because it assumes all 
		 * the Natural Docs components will be subfolders of a shared Components folder, and Visual Studio or any other IDE
		 * is running an executable found inside a component's subfolder.
		 * 
		 * If projectConfigFolder is relative it follows the same rules as the test data folder.  If it's not specified all defaults will
		 * be used as if the project didn't have any configuration files defined.
		 * 
		 * If preserveInputFolder is true, the HTML output folder will not be deleted after the engine is disposed of.
		 */
		public static void Start (Path pInputFolder, Path pProjectConfigFolder = default(Path), bool pKeepOutputFolder = false, 
										 string outputTitle = null, string outputSubTitle = null)
			{
			// Stupid, but we can't use "this" in static classes.
			inputFolder = pInputFolder;
			projectConfigFolder = pProjectConfigFolder;
			keepOutputFolder = pKeepOutputFolder;


			Path baseFolder;

			if (inputFolder.IsRelative || (projectConfigFolder != null && projectConfigFolder.IsRelative))
				{
				string assemblyPath = Path.GetExecutingAssembly();
				int binIndex = assemblyPath.IndexOf("Components");

				if (binIndex == -1)
					{  throw new Exception("Couldn't find Components folder in " + assemblyPath);  }

				baseFolder = assemblyPath.Substring(0, binIndex) + "Components/Engine.Tests/Test Data";
				}


			// inputFolder

			if (inputFolder.IsRelative)
				{  inputFolder = baseFolder + '/' + inputFolder;  }

			if (System.IO.Directory.Exists(inputFolder) == false)
				{  throw new Exception("Cannot locate input folder " + inputFolder);  }


			// temporaryFolderRoot

			temporaryFolderRoot = inputFolder + "/ND Temp";


			// projectConfigFolder

			if (projectConfigFolder == null)
				{  projectConfigFolder = temporaryFolderRoot + "/Project";   }
			else
				{
				if (projectConfigFolder.IsRelative)
					{  projectConfigFolder = baseFolder + '/' + projectConfigFolder;  }

				if (System.IO.Directory.Exists(projectConfigFolder) == false)
					{  throw new Exception("Cannot locate config folder " + projectConfigFolder);  }
				}


			// workingDataFolder

			workingDataFolder = temporaryFolderRoot + "/Working Data";


			// outputFolder

			if (keepOutputFolder)
				{  outputFolder = inputFolder + "/HTML Output";  }
			else
				{  outputFolder = temporaryFolderRoot + "/HTML Output";  }


			// Clear out old data before start.

			try
				{  System.IO.Directory.Delete(temporaryFolderRoot, true);  }
			catch
				{ }

			if (keepOutputFolder)
				{
				// Still need to clear it out so we can make a fresh copy.  We just won't delete it afterwards.
				try
					{  System.IO.Directory.Delete(outputFolder, true);  }
				catch
					{ }
				}


			// Create new folders.  These functions do nothing if they already exist, they won't throw exceptions.

			System.IO.Directory.CreateDirectory(projectConfigFolder);
			System.IO.Directory.CreateDirectory(workingDataFolder);
			System.IO.Directory.CreateDirectory(outputFolder);


			// INITIALIZE ZE ENGINE!

			Engine.Instance.Create();

			Engine.Instance.Config.ProjectConfigFolder = projectConfigFolder;
			Engine.Instance.Config.WorkingDataFolder = workingDataFolder;

			Engine.Instance.Config.CommandLineConfig.Entries.Add(
				new Engine.Config.Entries.InputFolder(inputFolder, Engine.Files.InputType.Source)
				);

			var outputEntry = new Engine.Config.Entries.HTMLOutputFolder(outputFolder);
			outputEntry.ProjectInfo.Title = outputTitle;
			outputEntry.ProjectInfo.Subtitle = outputSubTitle;

			Engine.Instance.Config.CommandLineConfig.Entries.Add(outputEntry);

			Engine.Errors.ErrorList startupErrors = new Engine.Errors.ErrorList();

			if (!Engine.Instance.Start(startupErrors))
				{
				StringBuilder message = new StringBuilder();
				message.Append("Could not start the Natural Docs engine for testing:");

				foreach (var error in startupErrors)
					{  
					message.Append("\n - ");
					if (error.File != null)
						{  message.Append(error.File + " line " + error.LineNumber + ": ");  }
					message.Append(error.Message);
					}

				Dispose();

				throw new Exception(message.ToString());
				}
			}


		/* Function: Run
		 */
		public static void Run ()
			{
			Engine.Instance.Files.WorkOnAddingAllFiles(Engine.Delegates.NeverCancel);
			Engine.Instance.Files.DeleteFilesNotInFileSources(Engine.Delegates.NeverCancel);
							
			Engine.Instance.Files.WorkOnProcessingChanges(Engine.Delegates.NeverCancel);

			Engine.Instance.Output.WorkOnUpdatingOutput(Engine.Delegates.NeverCancel);
			Engine.Instance.Output.WorkOnFinalizingOutput(Engine.Delegates.NeverCancel);
						
			Engine.Instance.Cleanup(Delegates.NeverCancel);
			}


		/* Function: Dispose
		 * Disposes of the <Engine.Instance> so you can create another one or end execution.
		 */
		public static void Dispose ()
			{
			if (inputFolder != null)
				{
				Engine.Instance.Dispose(true);

				try
					{  System.IO.Directory.Delete(temporaryFolderRoot, true);  }
				catch
					{  }

				// We don't have to worry about the output folder.  It would be part of the temporary folder if we weren't
				// keeping it and separate if we were.

				inputFolder = null;
				}
			}


		// Group: Properties
		// __________________________________________________________________________


		public static Path InputFolder
			{
			get
				{  return inputFolder;  }
			}


		public static Engine.Output.Builders.HTML HTMLBuilder
			{
			get
				{
				foreach (var builder in Engine.Instance.Output.Builders)
					{
					if (builder is Engine.Output.Builders.HTML)
						{  return (Engine.Output.Builders.HTML)builder;  }
					}

				return null;
				}
			}



		// Group: Variables
		// __________________________________________________________________________

		private static Path inputFolder;
		private static Path projectConfigFolder;
		private static Path workingDataFolder;
		private static Path outputFolder;

		private static Path temporaryFolderRoot;

		private static bool keepOutputFolder;

		}
	}