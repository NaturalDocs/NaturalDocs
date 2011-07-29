/* 
 * Class: GregValure.NaturalDocs.EngineTests.Framework.TestEngine
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

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using GregValure.NaturalDocs.Engine;


namespace GregValure.NaturalDocs.EngineTests.Framework
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
			temporaryFolder = null;
			configFolder = null;
			}


		/* Function: Start
		 * 
		 * Starts <Engine.Instance> using the passed folder of test data.
		 * 
		 * If you pass a relative path it will take the executing assembly path, skip up until it passes "bin", move into the "Test Data"
		 * subfolder, and then make the path relative to that.  This is because it's meant to be run from a Visual Studio source tree, 
		 * so from C:\Project\bin\debug\EngineTests.dll it will look for C:\Project\Test Data\[test folder].
		 */
		public static void Start (Path folder)
			{
			// Resolve and validate the test folder

			if (folder.IsRelative)
				{
				string assemblyPath = Path.GetExecutingAssembly();
				int binIndex = assemblyPath.IndexOf("/bin/");

				if (binIndex == -1)
					{  throw new Exception("Couldn't find bin folder in " + assemblyPath);  }

				folder = assemblyPath.Substring(0, binIndex) + "/Test Data/" + folder;
				}

			if (System.IO.Directory.Exists(folder) == false)
				{  throw new Exception("Cannot locate test folder " + folder);  }

			testDataFolder = folder;


			// Make temporary folders and see if there's a config folder already.

			temporaryFolder = testDataFolder + "/ND Temp";
			
			if (System.IO.Directory.Exists(testDataFolder + "/ND Config"))
				{  configFolder = testDataFolder + "/ND Config";  }
			else
				{
				configFolder = temporaryFolder + "/Config";
				System.IO.Directory.CreateDirectory(configFolder);
				}

			System.IO.Directory.CreateDirectory(HTMLOutputFolder);
			System.IO.Directory.CreateDirectory(WorkingDataFolder);


			// INITIALIZE ZE ENGINE!

			Engine.Instance.Create();

			Engine.Instance.Config.ProjectConfigFolder = ConfigFolder;
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
					{  System.IO.Directory.Delete(temporaryFolder, true);  }
				catch
					{  }

				testDataFolder = null;
				temporaryFolder = null;
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
				{  return temporaryFolder + "/HTML Output";  }
			}

		public static Path ConfigFolder
			{
			get
				{  return configFolder;  }
			}

		public static Path WorkingDataFolder
			{
			get
				{  return temporaryFolder + "/Working Data";  }
			}


		// Group: Variables
		// __________________________________________________________________________

		private static Path testDataFolder;
		private static Path temporaryFolder;
		private static Path configFolder;

		}
	}