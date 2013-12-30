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

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
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
		 * If the test data folder is relative it will look for a "Engine.Tests.Data" subfolder where the executing assembly is.  If
		 * one doesn't exist, it will check each parent folder for it.  Once found it will make the test data folder relative to that.
		 * 
		 * If projectConfigFolder is relative it follows the same rules as the test data folder.  If it's not specified all defaults will
		 * be used as if the project didn't have any configuration files defined.
		 * 
		 * If keepOutputFolder is true, the HTML output folder will not be deleted after the engine is disposed of.
		 */
		public static void Start (Path pTestDataFolder, Path pProjectConfigFolder = default(Path), bool pKeepOutputFolder = false, 
										 string outputTitle = null, string outputSubTitle = null, bool autoGroup = false)
			{
			// Stupid, but we can't use "this" in static classes.
			inputFolder = pTestDataFolder;
			projectConfigFolder = pProjectConfigFolder;
			keepOutputFolder = pKeepOutputFolder;


			Path testDataFolder;

			if (inputFolder.IsRelative || (projectConfigFolder != null && projectConfigFolder.IsRelative))
				{
				Path assemblyFolder = Path.GetExecutingAssembly().ParentFolder;
				Path folder = assemblyFolder;

				while (System.IO.Directory.Exists(folder + "/Engine.Tests.Data") == false)
					{
					if (folder.ParentFolder == folder)
						{  throw new Exception("Couldn't find Engine.Tests.Data folder in " + assemblyFolder + " or any of its parents.");  }

					folder = folder.ParentFolder;
					}

				testDataFolder = folder + "/Engine.Tests.Data";
				}


			// inputFolder

			if (inputFolder.IsRelative)
				{  inputFolder = testDataFolder + '/' + inputFolder;  }

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
					{  projectConfigFolder = testDataFolder + '/' + projectConfigFolder;  }

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

			var config = new Engine.Config.ProjectConfig(Config.Source.CommandLine);

			config.ProjectConfigFolder = projectConfigFolder;
			config.ProjectConfigFolderPropertyLocation = Config.Source.CommandLine;

			config.WorkingDataFolder = workingDataFolder;
			config.WorkingDataFolderPropertyLocation = Config.Source.CommandLine;

			config.AutoGroup = autoGroup;
			config.AutoGroupPropertyLocation = Config.Source.CommandLine;

			var inputTarget = new Config.Targets.SourceFolder(Config.Source.CommandLine, Files.InputType.Source);

			inputTarget.Folder = inputFolder;
			inputTarget.FolderPropertyLocation = Config.Source.CommandLine;

			config.InputTargets.Add(inputTarget);

			var outputTarget = new Config.Targets.HTMLOutputFolder(Config.Source.CommandLine);
			
			outputTarget.Folder = outputFolder;
			outputTarget.FolderPropertyLocation = Config.Source.CommandLine;
			
			if (outputTitle != null)
				{
				outputTarget.ProjectInfo.Title = outputTitle;
				outputTarget.ProjectInfo.TitlePropertyLocation = Config.Source.CommandLine;
				}

			if (outputSubTitle != null)
				{
				outputTarget.ProjectInfo.SubTitle = outputSubTitle;
				outputTarget.ProjectInfo.SubTitlePropertyLocation = Config.Source.CommandLine;
				}

			config.OutputTargets.Add(outputTarget);

			Engine.Errors.ErrorList startupErrors = new Engine.Errors.ErrorList();

			if (!Engine.Instance.Start(startupErrors, config))
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