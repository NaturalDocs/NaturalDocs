/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.Framework.TestEngine
 * ____________________________________________________________________________
 * 
 * A class that simplifies configuring and running <Engine.Instance> for tests.
 * 
 * Usage:
 * 
 *		- You call <Start()> with a folder of test data.
 *			- It will use the test data folder as the input folder.
 *			- It will create a temporary folder for output and create a HTML target.
 *			- If there is a folder called "ND Config" in the Test Data folder, that will be used for the configuration.  If not,
 *			  a temporary folder will be created for it ensuring Natural Docs will run using its default configuration.
 *			- An exception is thrown if there are any errors initializing the engine.
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
			testDataFolder = null;
			projectConfigFolder = null;
			keepOutputFolder = false;
			}


		/* Function: Start
		 * 
		 * Starts <Engine.Instance> using the passed folder of test data.
		 * 
		 * If the test data folder is relative it will take the executing assembly path, skip up until it finds "Components", move 
		 * into the "EngineTests\Test Data" subfolder, and then make the path relative to that.  This is because it assumes all 
		 * the Natural Docs components will be subfolders of a shared Components folder, and Visual Studio or any other IDE
		 * is running an executable found inside a component's subfolder.
		 * 
		 * If projectConfigFolder is relative it follows the same rules as the test data folder.  If it's not specified all defaults will
		 * be used as if the project didn't have any configuration files defined.
		 * 
		 * If preserveInputFolder is true, the HTML output folder will not be deleted after the engine is disposed of.
		 */
		public static void Start (Path pTestDataFolder, Path pProjectConfigFolder = default(Path), bool pKeepOutputFolder = false)
			{
			// Stupid, but we can't use "this" in static classes.
			testDataFolder = pTestDataFolder;
			projectConfigFolder = pProjectConfigFolder;
			keepOutputFolder = pKeepOutputFolder;


			// Resolve and validate the folders

			Path baseTestDataFolder;

			if (testDataFolder.IsRelative || (projectConfigFolder != null && projectConfigFolder.IsRelative))
				{
				string assemblyPath = Path.GetExecutingAssembly();
				int binIndex = assemblyPath.IndexOf("/Components/");

				if (binIndex == -1)
					{  throw new Exception("Couldn't find Components folder in " + assemblyPath);  }

				baseTestDataFolder = assemblyPath.Substring(0, binIndex) + "/Components/Engine.Tests/Test Data";
				}

			if (testDataFolder.IsRelative)
				{  testDataFolder = baseTestDataFolder + '/' + testDataFolder;  }

			if (System.IO.Directory.Exists(testDataFolder) == false)
				{  throw new Exception("Cannot locate test folder " + testDataFolder);  }

			if (projectConfigFolder != null && projectConfigFolder.IsRelative)
				{  projectConfigFolder = baseTestDataFolder + '/' + projectConfigFolder;  }

			if (projectConfigFolder != null && System.IO.Directory.Exists(projectConfigFolder) == false)
				{  throw new Exception("Cannot locate config folder " + projectConfigFolder);  }


			// Clear out old data before start.

			try
				{  System.IO.Directory.Delete(TemporaryFolderRoot, true);  }
			catch
				{ }

			if (keepOutputFolder)
				{
				// Still need to clear it out so we can make a fresh copy.  We just won't delete it afterwards.
				try
					{  System.IO.Directory.Delete(HTMLOutputFolder, true);  }
				catch
					{ }
				}


			// Create new folders.  These functions do nothing if they already exist, they won't throw exceptions.

			System.IO.Directory.CreateDirectory(ProjectConfigFolder);
			System.IO.Directory.CreateDirectory(WorkingDataFolder);
			System.IO.Directory.CreateDirectory(HTMLOutputFolder);


			// INITIALIZE ZE ENGINE!

			Engine.Instance.Create();

			Engine.Instance.Config.ProjectConfigFolder = ProjectConfigFolder;
			Engine.Instance.Config.WorkingDataFolder = WorkingDataFolder;

			Engine.Instance.Config.CommandLineConfig.Entries.Add(
				new Engine.Config.Entries.InputFolder(InputFolder, Engine.Files.InputType.Source)
				);
			Engine.Instance.Config.CommandLineConfig.Entries.Add(
				new Engine.Config.Entries.HTMLOutputFolder(HTMLOutputFolder)
				);

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
			Engine.Instance.Files.WorkOnAddingAllFiles( Engine.Delegates.NeverCancel );
			Engine.Instance.Files.DeleteFilesNotInFileSources( Engine.Delegates.NeverCancel );
							
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
			if (testDataFolder != null)
				{
				Engine.Instance.Dispose(true);

				try
					{  System.IO.Directory.Delete(TemporaryFolderRoot, true);  }
				catch
					{  }

				// We don't have to worry about keepOutputFolder because it would be part of the temporary folder if we
				// weren't and out of it if we were.

				testDataFolder = null;
				}
			}


		// Group: Properties
		// __________________________________________________________________________

		public static Path InputFolder
			{
			get
				{  return testDataFolder;  }
			}

		public static Path HTMLOutputFolder
			{
			get
				{  
				if (keepOutputFolder)
					{  return testDataFolder + "/HTML Output";  }
				else
					{  return TemporaryFolderRoot + "/HTML Output";  }
				}
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

		public static Path ProjectConfigFolder
			{
			get
				{  
				if (projectConfigFolder != null)
					{  return projectConfigFolder;  }
				else
					{  return TemporaryFolderRoot + "/ND Config";  }
				}
			}

		public static Path WorkingDataFolder
			{
			get
				{  return TemporaryFolderRoot + "/Working Data";  }
			}

		public static Path TemporaryFolderRoot
			{
			get
				{  return testDataFolder + "/ND Temp";  }
			}


		// Group: Variables
		// __________________________________________________________________________

		private static Path testDataFolder;
		private static Path projectConfigFolder;
		private static bool keepOutputFolder;

		}
	}