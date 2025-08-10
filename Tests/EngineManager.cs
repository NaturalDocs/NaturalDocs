/*
 * Class: CodeClear.NaturalDocs.Tests.EngineManager
 * ____________________________________________________________________________
 *
 * A class that simplifies configuring and running a <NaturalDocs.Engine.Instance> for tests.  The configuration is
 * automatically determined by the passed <TestFolder> and any <TestFolderConfig> settings it has.
 *
 * You can use this to start an <Engine.Instance> and then either access its internal functions directly or use
 * <BuildDocumentation()> to parse the source and build HTML output.  All instances should be disposed of when no
 * longer needed.
 *
 *
 * Configuration:
 *
 *		- The input folder will be set to the <TestFolder>, though this is only relevant if you use <BuildDocumentation()>.
 *
 *		- The output folder will be set to a "HTML Output" subfolder if <TestFolderConfig.KeepHTML> is set, or to a
 *		  temporary folder if not.  You should use the <HTMLOutputFolder> property to retrieve it.  This is only relevant
 *		  if you use <BuildDocumentation()>.
 *
 *		- The project configuration folder will be set to <TestFolderConfig.EngineConfigFolder> if it is set.
 *
 *		- The title, subtitle, and style will be set from <TestFolderConfig> properties if they are set, though this is only
 *		  relevant if you use <BuildDocumentation()>.
 *
 *		- <TestFolderConfig.AutoGroup> will be applied.
 *
 *		- A temporary folder will be created by <Start()> for anything else the engine needs and it will be deleted by
 *		  <Dispose()>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Tests
	{
	public class EngineManager : IDisposable
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: EngineManager
		 */
		public EngineManager ()
			{
			this.engineInstance = null;
			this.testFolder = null;
			this.temporaryDataFolder = null;
			}

		~EngineManager ()
			{
			Dispose(strictRulesApply: true);
			}


		/* Function: Start
		 * Starts <Engine.Instance> incorporating the data in <TestFolder.Config>.
		 */
		public void Start (TestFolder testFolder)
			{
			this.testFolder = testFolder;
			this.temporaryDataFolder = testFolder.Path + "/ND Temp";


			// Determine paths needed by the engine

			AbsolutePath inputFolder = testFolder.Path;

			AbsolutePath projectFolder;

			if (testFolder.Config.EngineConfigFolder != null)
				{  projectFolder = testFolder.Config.EngineConfigFolder;  }
			else
				{  projectFolder = temporaryDataFolder + "/ND Config";  }

			AbsolutePath workingDataFolder = temporaryDataFolder + "/Working Data";

			AbsolutePath outputFolder;

			if (testFolder.Config.KeepHTML)
				{  outputFolder = testFolder.Path + "/HTML Output";  }
			else
				{  outputFolder = temporaryDataFolder + "/HTML Output";  }


			// Clear out old data before we start

			try
				{  System.IO.Directory.Delete(temporaryDataFolder, recursive: true);  }
			catch
				{  }

			if (testFolder.Config.KeepHTML)
				{
				// Still need to clear it out so we can make a fresh copy, we just won't delete it afterwards
				try
					{  System.IO.Directory.Delete(outputFolder, recursive: true);  }
				catch
					{  }
				}


			// Create new folders.  These functions won't throw exceptions if they already exist.

			System.IO.Directory.CreateDirectory(temporaryDataFolder);
			System.IO.Directory.CreateDirectory(workingDataFolder);
			System.IO.Directory.CreateDirectory(outputFolder);

			if (testFolder.Config.EngineConfigFolder != null)
				{  System.IO.Directory.CreateDirectory(projectFolder);  }


			// Create an engine instance and transfer our configuration to it

			engineInstance = new NaturalDocs.Engine.Instance();

			var engineConfig = new Engine.Config.ProjectConfig(Engine.Config.PropertySource.CommandLine);

			var inputTarget = new Engine.Config.Targets.SourceFolder(Engine.Config.PropertySource.CommandLine);

				inputTarget.Folder = inputFolder;
				inputTarget.FolderPropertyLocation = Engine.Config.PropertySource.CommandLine;

				engineConfig.InputTargets.Add(inputTarget);

			engineConfig.ProjectConfigFolder = projectFolder;
				engineConfig.ProjectConfigFolderPropertyLocation = Engine.Config.PropertySource.CommandLine;

			var outputTarget = new Engine.Config.Targets.HTMLOutputFolder(Engine.Config.PropertySource.CommandLine);

				outputTarget.Folder = outputFolder;
				outputTarget.FolderPropertyLocation = Engine.Config.PropertySource.CommandLine;

				if (testFolder.Config.HTMLTitle != null)
					{
					outputTarget.Title = testFolder.Config.HTMLTitle;
					outputTarget.TitlePropertyLocation = Engine.Config.PropertySource.CommandLine;
					}

				if (testFolder.Config.HTMLSubtitle != null)
					{
					outputTarget.Subtitle = testFolder.Config.HTMLSubtitle;
					outputTarget.SubtitlePropertyLocation = Engine.Config.PropertySource.CommandLine;
					}

				if (testFolder.Config.HTMLStyle != null)
					{
					outputTarget.StyleName = testFolder.Config.HTMLStyle;
					outputTarget.StyleNamePropertyLocation = Engine.Config.PropertySource.CommandLine;
					}

				engineConfig.OutputTargets.Add(outputTarget);

			engineConfig.WorkingDataFolder = workingDataFolder;
				engineConfig.WorkingDataFolderPropertyLocation = Engine.Config.PropertySource.CommandLine;

			engineConfig.AutoGroup = testFolder.Config.AutoGroup;
				engineConfig.AutoGroupPropertyLocation = Engine.Config.PropertySource.CommandLine;


			// Attempt to start the engine

			ErrorList startupErrors = new ErrorList();

			if (!engineInstance.Start(startupErrors, engineConfig))
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


		/* Function: BuildDocumentation
		 * Runs the engine to build a full set of HTML documentation using the <TestFolder> as the input.  May not be
		 * needed for all test types.
		 */
		public void BuildDocumentation ()
			{
			var adder = EngineInstance.Files.CreateAdderProcess();
			adder.WorkOnAddingAllFiles(Delegates.NeverCancel);
			adder.Dispose();

			EngineInstance.Files.DeleteFilesNotReAdded(Delegates.NeverCancel);

			var changeProcessor = EngineInstance.Files.CreateChangeProcessor();
			changeProcessor.WorkOnProcessingChanges(Delegates.NeverCancel);
			changeProcessor.Dispose();

			var builder = EngineInstance.Output.CreateBuilderProcess();
			builder.WorkOnUpdatingOutput(Delegates.NeverCancel);
			builder.WorkOnFinalizingOutput(Delegates.NeverCancel);
			builder.Dispose();

			EngineInstance.Cleanup(Delegates.NeverCancel);
			}


		/* Function: Dispose
		 * Disposes of the <Engine.Instance> so you can create another one or end execution.
		 */
		public void Dispose ()
			{
			Dispose(strictRulesApply: false);
			}


		/* Function: Dispose
		 */
		protected void Dispose (bool strictRulesApply)
			{
			if (!strictRulesApply && engineInstance != null)
				{
				engineInstance.Dispose(graceful: true);
				engineInstance = null;

				try
					{  System.IO.Directory.Delete(temporaryDataFolder, recursive: true);  }
				catch
					{  }

				temporaryDataFolder = null;
				testFolder = null;
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		public NaturalDocs.Engine.Instance EngineInstance
			{
			get
				{  return engineInstance;  }
			}


		public TestFolder TestFolder
			{
			get
				{  return testFolder;  }
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


		public AbsolutePath HTMLOutputFolder
			{
			get
				{
				var builder = HTMLBuilder;

				if (builder == null)
					{  return null;  }
				else
					{  return (AbsolutePath)builder.OutputFolder;  }
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: engineInstance
		 * The <NaturalDocs.Engine.Instance> being managed.  It will be null if it hasn't been created yet.
		 */
		protected NaturalDocs.Engine.Instance engineInstance;

		/* var: testFolder
		 * The <TestFolder> associated with this engine instance.
		 */
		protected TestFolder testFolder;

		/* var: temporaryDataFolder
		 * The folder where temporary data for the engine is stored.  It's contents will be removed afterwards.
		 */
		protected AbsolutePath temporaryDataFolder;

		}
	}
