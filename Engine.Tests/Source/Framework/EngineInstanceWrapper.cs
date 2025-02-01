/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.EngineInstanceManager
 * ____________________________________________________________________________
 *
 * A class that simplifies configuring and running <NaturalDocs.Engine.Instance> for tests.
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

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework
	{
	public class EngineInstanceManager
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Constructor: EngineInstanceManager
		 */
		public EngineInstanceManager ()
			{
			engineInstance = null;

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
		public void Start (Path testDataFolder, Path projectConfigFolder = default(Path), bool keepOutputFolder = false,
								 string outputTitle = null, string outputSubtitle = null, string outputStyle = null, bool autoGroup = false)
			{

			// keepOutputFolder

			this.keepOutputFolder = keepOutputFolder;


			// testDataRoot and testDataFolder

			AbsolutePath assemblyFolder = Path.FromAssembly( System.Reflection.Assembly.GetExecutingAssembly() ).ParentFolder;
			AbsolutePath testDataRoot = assemblyFolder;

			while (System.IO.Directory.Exists(testDataRoot + "/Engine.Tests.Data") == false)
				{
				if (testDataRoot.ParentFolder == testDataRoot)
					{  throw new Exception("Couldn't find Engine.Tests.Data folder in " + assemblyFolder + " or any of its parents.");  }

				testDataRoot = testDataRoot.ParentFolder;
				}

			testDataRoot = (AbsolutePath)(testDataRoot + "/Engine.Tests.Data");

			if (testDataFolder.IsRelative)
				{  testDataFolder = testDataRoot + '/' + testDataFolder;  }


			// inputFolder

			this.inputFolder = (AbsolutePath)testDataFolder;

			if (System.IO.Directory.Exists(inputFolder) == false)
				{  throw new Exception("Cannot locate input folder " + inputFolder);  }


			// temporaryFolderRoot

			this.temporaryFolderRoot = (AbsolutePath)(inputFolder + "/ND Temp");


			// projectConfigFolder

			if (projectConfigFolder == null)
				{
				projectConfigFolder = temporaryFolderRoot + "/Project";
				this.projectConfigFolder = (AbsolutePath)projectConfigFolder;
				}
			else
				{
				if (projectConfigFolder.IsRelative)
					{  projectConfigFolder = testDataRoot + '/' + projectConfigFolder;  }

				this.projectConfigFolder = (AbsolutePath)projectConfigFolder;

				if (System.IO.Directory.Exists(projectConfigFolder) == false)
					{  throw new Exception("Cannot locate config folder " + projectConfigFolder);  }
				}


			// workingDataFolder

			this.workingDataFolder = (AbsolutePath)(temporaryFolderRoot + "/Working Data");


			// outputFolder

			if (keepOutputFolder)
				{  outputFolder = (AbsolutePath)(inputFolder + "/HTML Output");  }
			else
				{  outputFolder = (AbsolutePath)(temporaryFolderRoot + "/HTML Output");  }


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

			engineInstance = new NaturalDocs.Engine.Instance();

			var config = new Config.ProjectConfig(Config.PropertySource.CommandLine);

			config.ProjectConfigFolder = this.projectConfigFolder;
			config.ProjectConfigFolderPropertyLocation = Config.PropertySource.CommandLine;

			config.WorkingDataFolder = this.workingDataFolder;
			config.WorkingDataFolderPropertyLocation = Config.PropertySource.CommandLine;

			config.AutoGroup = autoGroup;
			config.AutoGroupPropertyLocation = Config.PropertySource.CommandLine;

			var inputTarget = new Config.Targets.SourceFolder(Config.PropertySource.CommandLine);

			inputTarget.Folder = inputFolder;
			inputTarget.FolderPropertyLocation = Config.PropertySource.CommandLine;

			config.InputTargets.Add(inputTarget);

			var outputTarget = new Config.Targets.HTMLOutputFolder(Config.PropertySource.CommandLine);

			outputTarget.Folder = outputFolder;
			outputTarget.FolderPropertyLocation = Config.PropertySource.CommandLine;

			if (outputTitle != null)
				{
				outputTarget.Title = outputTitle;
				outputTarget.TitlePropertyLocation = Config.PropertySource.CommandLine;
				}

			if (outputSubtitle != null)
				{
				outputTarget.Subtitle = outputSubtitle;
				outputTarget.SubtitlePropertyLocation = Config.PropertySource.CommandLine;
				}

			if (outputStyle != null)
				{
				outputTarget.StyleName = outputStyle;
				outputTarget.StyleNamePropertyLocation = Config.PropertySource.CommandLine;
				}

			config.OutputTargets.Add(outputTarget);

			Engine.Errors.ErrorList startupErrors = new Engine.Errors.ErrorList();

			if (!engineInstance.Start(startupErrors, config))
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
		public void Run ()
			{
			var adder = EngineInstance.Files.CreateAdderProcess();
			adder.WorkOnAddingAllFiles(Engine.Delegates.NeverCancel);
			adder.Dispose();

			EngineInstance.Files.DeleteFilesNotReAdded(Engine.Delegates.NeverCancel);

			var changeProcessor = EngineInstance.Files.CreateChangeProcessor();
			changeProcessor.WorkOnProcessingChanges(Engine.Delegates.NeverCancel);
			changeProcessor.Dispose();

			var builder = EngineInstance.Output.CreateBuilderProcess();
			builder.WorkOnUpdatingOutput(Engine.Delegates.NeverCancel);
			builder.WorkOnFinalizingOutput(Engine.Delegates.NeverCancel);
			builder.Dispose();

			EngineInstance.Cleanup(Delegates.NeverCancel);
			}


		/* Function: Dispose
		 * Disposes of the <Engine.Instance> so you can create another one or end execution.
		 */
		public void Dispose ()
			{
			if (engineInstance != null)
				{
				engineInstance.Dispose(true);
				engineInstance = null;

				try
					{  System.IO.Directory.Delete(temporaryFolderRoot, true);  }
				catch
					{  }

				// We don't have to worry about the output folder.  It would be part of the temporary folder if we weren't
				// keeping it and separate if we were.
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		public NaturalDocs.Engine.Instance EngineInstance
			{
			get
				{  return engineInstance;  }
			}

		public Path InputFolder
			{
			get
				{  return inputFolder;  }
			}


		public Engine.Output.HTML.Target HTMLBuilder
			{
			get
				{
				foreach (var target in EngineInstance.Output.Targets)
					{
					if (target is Engine.Output.HTML.Target)
						{  return (Engine.Output.HTML.Target)target;  }
					}

				return null;
				}
			}



		// Group: Variables
		// __________________________________________________________________________

		protected NaturalDocs.Engine.Instance engineInstance;

		protected AbsolutePath inputFolder;
		protected AbsolutePath projectConfigFolder;
		protected AbsolutePath workingDataFolder;
		protected AbsolutePath outputFolder;

		protected AbsolutePath temporaryFolderRoot;

		protected bool keepOutputFolder;

		}
	}
