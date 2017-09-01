/* 
 * Class: CodeClear.NaturalDocs.CLI.Application
 * ____________________________________________________________________________
 * 
 * The main application class for the command line interface to Natural Docs.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Config;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.CLI
	{
	public static partial class Application
		{
			
		// Group: Constants
		// __________________________________________________________________________
		
		
		/* Constant: StatusInterval
		 * The amount of time in milliseconds that must go by before a status update.
		 */
		public const int StatusInterval = 5000;
			
		/* Constant: DelayedMessageThreshold
		 * The amount of time in milliseconds that certain operations must take before they warrant a status update.
		 */
		public const int DelayedMessageThreshold = 1500;
			
						

		// Group: Primary Execution Paths
		// __________________________________________________________________________


		/* Function: Main
		 * The program entry point.
		 */
		public static void Main (string[] commandLine)
			{
			executionTimer = new ExecutionTimer();
			executionTimer.Start("Total Execution");

			engineInstance = null;

			quiet = false;
			workerThreadCount = DefaultWorkerThreadCount;
			totalFileChanges = 0;
			benchmark = false;
			pauseOnError = false;
			pauseBeforeExit = false;
			
			bool gracefulExit = false;

			var standardOutput = System.Console.Out;

			try
				{
				engineInstance = new NaturalDocs.Engine.Instance();
				
				ErrorList startupErrors = new ErrorList();
				ParseCommandLineResult parseCommandLineResult = ParseCommandLine(commandLine, out commandLineConfig, startupErrors);

				
				if (parseCommandLineResult == ParseCommandLineResult.Error)
					{
					HandleErrorList(startupErrors);
					}

				else if (parseCommandLineResult == ParseCommandLineResult.ShowCommandLineReference)
					{
					ShowCommandLineReference();
					gracefulExit = true;
					}

				else if (parseCommandLineResult == ParseCommandLineResult.ShowVersion)
					{
					ShowVersion();
					gracefulExit = true;
					}

				else // (parseCommandLineResult == ParseCommandLineResult.Run)
					{
					if (quiet)
						{  
						// This is easier and less error prone than putting conditional statements around all the non-error console
						// output, even if it's less efficient.
						System.Console.SetOut(System.IO.TextWriter.Null);
						}


					// Create project configuration files only

					if (commandLineConfig.ProjectConfigFolderPropertyLocation.IsDefined &&
						commandLineConfig.InputTargets.Count == 0 &&
						commandLineConfig.OutputTargets.Count == 0 &&
						!System.IO.File.Exists(commandLineConfig.ProjectConfigFolder + "/Project.txt"))
						{
						if (CreateProjectConfiguration(startupErrors))
							{  gracefulExit = true;  }
						else
							{  HandleErrorList(startupErrors);  }
						}


					// Normal execution

					else
						{
						if (BuildDocumentation(startupErrors))
							{  gracefulExit = true;  }
						else
							{  HandleErrorList(startupErrors);  }
						}
					}
				}

			catch (Exception e)
				{  
				HandleException(e);  
				}
				
			finally
				{
				Engine.Path projectFolder = engineInstance.Config.ProjectConfigFolder;

				engineInstance.Dispose(gracefulExit);
				engineInstance = null;

				executionTimer.End("Total Execution");

				if (benchmark)
					{  ShowBenchmarksAndBuildCSV(projectFolder + "/Benchmarks.csv");  } 

				// Restore the standard output.  We do this before "Press any key to continue" because we never want that to
				// be hidden.
				if (quiet)
					{  System.Console.SetOut(standardOutput);  }
				}
				
			if (pauseBeforeExit || (pauseOnError && !gracefulExit))
				{
				System.Console.WriteLine();
				System.Console.WriteLine(
					Engine.Locale.SafeGet("NaturalDocs.CLI", "Status.PressAnyKeyToContinue", "Press any key to continue...")
					);
				System.Console.ReadKey(true);
				}
			}


		private static bool BuildDocumentation (ErrorList errorList)
			{
			ShowConsoleHeader();

			bool rebuildAllOutputFromCommandLine = EngineInstance.Config.RebuildAllOutput;
			bool reparseEverythingFromCommandLine = EngineInstance.Config.ReparseEverything;

			EngineInstance.AddStartupWatcher(new EngineStartupWatcher());


			executionTimer.Start("Engine Startup");
			
			if (EngineInstance.Start(errorList, commandLineConfig) == true)
				{
				executionTimer.End("Engine Startup");


				// File Search
						
				executionTimer.Start("Finding Source Files");

				using ( StatusManagers.FileSearch statusManager = new StatusManagers.FileSearch() )
					{
					statusManager.Start();
							
					Multithread("File Adder", EngineInstance.Files.WorkOnAddingAllFiles);
							
					statusManager.End();
					}
							
				EngineInstance.Files.DeleteFilesNotInFileSources( Engine.Delegates.NeverCancel );
							
				executionTimer.End("Finding Source Files");

						
				// Rebuild notice

				string alternateStartMessage = null;
						
				if (reparseEverythingFromCommandLine || rebuildAllOutputFromCommandLine)
					{  alternateStartMessage = "Status.RebuildEverythingByRequest";  }
				else if (EngineInstance.Config.ReparseEverything && EngineInstance.Config.RebuildAllOutput)
					{  alternateStartMessage = "Status.RebuildEverythingAutomatically";  }
							
							
				// Parsing
						
				executionTimer.Start("Parsing Source Files");

				using ( StatusManagers.Parsing statusManager = new StatusManagers.Parsing(alternateStartMessage) )
					{
					statusManager.Start();
					totalFileChanges = statusManager.TotalFilesToProcess;

					Multithread("Parser", EngineInstance.Files.WorkOnProcessingChanges);							
							
					statusManager.End();
					}
							
				executionTimer.End("Parsing Source Files");

							
				// Resolving
						
				executionTimer.Start("Resolving Links");

				using ( StatusManagers.ResolvingLinks statusManager = new StatusManagers.ResolvingLinks() )
					{
					statusManager.Start();

					Multithread("Resolver", EngineInstance.CodeDB.WorkOnResolvingLinks);
							
					statusManager.End();
					}
							
				executionTimer.End("Resolving Links");

							
				// Building
						
				executionTimer.Start("Building Output");

				using ( StatusManagers.Building statusManager = new StatusManagers.Building() )
					{
					statusManager.Start();

					Multithread("Builder", EngineInstance.Output.WorkOnUpdatingOutput);
					Multithread("Finalizer", EngineInstance.Output.WorkOnFinalizingOutput);							
							
					statusManager.End();
					}
							
				executionTimer.End("Building Output");

							
				// End
						
				EngineInstance.Cleanup(Delegates.NeverCancel);
						
				ShowConsoleFooter(true);
				return true;
				}

			else // engine did not start correctly
				{  
				executionTimer.End("Engine Startup");

				ShowConsoleFooter(false);
				return false;
				}
			}

		private static bool CreateProjectConfiguration (ErrorList errorList)
			{
			ShowConsoleHeader();

			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.CreatingProjectConfigFiles")
				);

			int priorErrorCount = errorList.Count;

			Engine.Config.Project_txt projectTxtParser = new Engine.Config.Project_txt();
			projectTxtParser.Save(commandLineConfig.ProjectConfigFolder + "/Project.txt", commandLineConfig, errorList);

			if (System.IO.File.Exists(commandLineConfig.ProjectConfigFolder + "/Languages.txt") == false)
				{
				Engine.Languages.Languages_txt languagesTxtParser = new Engine.Languages.Languages_txt();
				languagesTxtParser.Save(commandLineConfig.ProjectConfigFolder + "/Languages.txt",
													new List<Engine.Languages.ConfigFileLanguage>(), new List<string>(),
													errorList, true, false);
				}

			if (System.IO.File.Exists(commandLineConfig.ProjectConfigFolder + "/Comments.txt") == false)
				{
				Engine.CommentTypes.Comments_txt commentsTxtParser = new Engine.CommentTypes.Comments_txt();
				commentsTxtParser.Save(commandLineConfig.ProjectConfigFolder + "/Comments.txt",
												new List<Engine.CommentTypes.ConfigFileCommentType>(), new List<string>(), new List<string>(),
												errorList, true, false);
				}

			if (errorList.Count > priorErrorCount)
				{  
				ShowConsoleFooter(false);
				return false;
				}
			else
				{
				ShowConsoleFooter(true);
				return true;
				}
			}

		private static void ShowCommandLineReference ()
			{
			Console.WriteLine( 
				Locale.Get("NaturalDocs.CLI", "CommandLine.SyntaxReference(version).multiline", NaturalDocs.Engine.Instance.VersionString) 
				);
			}


		private static void ShowVersion ()
			{
			Console.WriteLine( Engine.Instance.VersionString );
			}



		// Group: Support Functions
		// __________________________________________________________________________
			

		private static void ShowConsoleHeader ()
			{
			string version = Engine.Instance.Version.PrimaryVersionString;
			string subversion = Engine.Instance.Version.SecondaryVersionString;

			if (subversion == null)
				{
				System.Console.WriteLine();
				System.Console.Write(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.Start(version).multiline", version)
					);
				}
			else
				{
				System.Console.WriteLine();
				System.Console.Write(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.Start(version,subversion).multiline", version, subversion)
					);
				}
			}


		private static void ShowConsoleFooter (bool successful)
			{
			if (successful)
				{
				System.Console.Write(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.End.multiline")
					);
				System.Console.WriteLine();
				}
			}


		static private void ShowBenchmarksAndBuildCSV (Engine.Path csvPath)
			{
			System.Console.Write(executionTimer.BuildStatistics());

			bool csvFileExisted = System.IO.File.Exists(csvPath);
			System.IO.StreamWriter csvFile = null;
			
			try
				{
				csvFile = new System.IO.StreamWriter(csvPath, true, System.Text.Encoding.UTF8);

				if (!csvFileExisted)
					{
					csvFile.Write("\"Date\",\"Threads Used\",\"Cores Available\",\"File Changes\",");
					csvFile.Write(executionTimer.BuildCSVHeadings());
					csvFile.WriteLine();
					}

				csvFile.Write(
					string.Format("\"{0:yyyy-MM-dd HH:mm:ss}\"", DateTime.Now)
					);

				csvFile.Write(',');
				csvFile.Write(workerThreadCount);
				csvFile.Write(',');
				csvFile.Write(System.Environment.ProcessorCount);
				csvFile.Write(',');
				csvFile.Write(totalFileChanges);
				csvFile.Write(',');
				csvFile.Write(executionTimer.BuildCSVValues());
				csvFile.WriteLine();

				Console.WriteLine();
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.BenchmarksSavedIn(file)", csvPath)
					);
				}
			catch (Exception e)
				{
				Console.WriteLine();
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.CouldNotSaveBenchmarksIn(file)", csvPath)
					);
				Console.WriteLine(e.Message);
				}
			finally
				{
				if (csvFile != null)
					{
					csvFile.Dispose();
					csvFile = null;
					}
				}
			}

			
		/* Function: Multithread
		 * 
		 * Executes the task across multiple threads.  The function passed must be suitably thread safe.  This
		 * function will not return until the task is complete.
		 * 
		 * Parameters:
		 * 
		 *		threadName - What the execution threads should be named.  "Thread #" will be appended so "Builder" will lead to 
		 *								 names like "Builder Thread 3".  This is important to specify because thread names are reported in 
		 *								 exceptions and crash reports.
		 *		task - The task to execute.  This must be thread safe.
		 */
		static private void Multithread (string threadName, CancellableTask task)
			{
			if (workerThreadCount == 1)
				{
				// If there's only one thread, execute it on the main thread instead of spawning a new one.
				// Uses fewer resources and makes debugging easier.
				task(Engine.Delegates.NeverCancel);
				}
			else
				{
				Engine.Thread[] threads = new Engine.Thread[workerThreadCount];

				for (int i = 0; i < threads.Length; i++)
					{  
					Engine.Thread thread = new Engine.Thread();

					thread.Name = threadName + " Thread " + (i + 1);
					thread.Task = task;
					thread.CancelDelegate = Engine.Delegates.NeverCancel;
					thread.Priority = System.Threading.ThreadPriority.BelowNormal;

					threads[i] = thread;
					}

				foreach (var thread in threads)
					{  thread.Start();  }

				foreach (var thread in threads)
					{  thread.Join();  }

				foreach (var thread in threads)
					{  thread.ThrowExceptions();  }
				}
			}


		/* Function: HandleException
		 */
		static private void HandleException (Exception e)
			{
			var errorOutput = Console.Error;

			errorOutput.Write ("\n\n------------------------------------------------------------\n");
			errorOutput.WriteLine (Locale.SafeGet("NaturalDocs.CLI", "Crash.Exception",
																	"Natural Docs has closed because of the following error:"));
			errorOutput.WriteLine();


			// If it's a user friendly exception, just display it

			if ( e.GetType() == typeof(Engine.Exceptions.UserFriendly) ||
				 ( e.GetType() == typeof(Engine.Exceptions.Thread) &&
				   e.InnerException.GetType() == typeof(Engine.Exceptions.UserFriendly) ))
				{
				errorOutput.WriteLine(e.Message);
				}

			else
				{
				Path crashFile = EngineInstance.BuildCrashReport(e);


				// If we were able to build a crash report, display the exception and the report's location

				if (crashFile != null)
					{
					errorOutput.WriteLine(e.Message);
					errorOutput.WriteLine();
					errorOutput.Write (Locale.SafeGet("NaturalDocs.CLI", "Crash.ReportAt(file).multiline", 
																	  "A crash report has been generated at {0}.\n" +
																	  "Please include this file when asking for help at naturaldocs.org.\n", crashFile));
					}


				// If we couldn't build the crash report, display the information on the screen.
					
				else
					{
					errorOutput.Write( EngineInstance.GetCrashInformation(e) );
					errorOutput.WriteLine ();
					errorOutput.WriteLine (Locale.SafeGet("NaturalDocs.CLI", "Crash.IncludeInfoAndGetHelp",
																			"Please include this information when asking for help at naturaldocs.org."));
					}
				}

			errorOutput.Write ("------------------------------------------------------------\n\n");
			}
			
			
		/* Function: HandleErrorList
		 */
		static private void HandleErrorList (Engine.Errors.ErrorList errorList)
			{
			// Annotate any config files before printing them to the console, since the line numbers may change.
			
			Engine.ConfigFile.TryToAnnotateWithErrors(errorList);
			
			
			// Write them to the console.
			
			var errorOutput = Console.Error;
			errorOutput.WriteLine();
			
			Path lastErrorFile = null;
			bool hasNonFileErrors = false;
				
			foreach (Engine.Errors.Error error in errorList)
				{  
				if (error.File != lastErrorFile)
					{
					if (error.File != null)
						{  errorOutput.WriteLine( Locale.Get("NaturalDocs.CLI", "CommandLine.ErrorsInFile(file)", error.File) );  }

					lastErrorFile = error.File;
					}
					
				if (error.File != null)
					{
					errorOutput.Write("   - ");
					
					if (error.LineNumber > 0)
						{  errorOutput.Write( Locale.Get("NaturalDocs.CLI", "CommandLine.Line") + " " + error.LineNumber + ": " );  }
					}
				else
					{  hasNonFileErrors = true;  }
					
				errorOutput.WriteLine(error.Message);  
				}
				
			if (hasNonFileErrors == true)
				{  errorOutput.WriteLine( Locale.Get("NaturalDocs.CLI", "CommandLine.HowToGetCommandLineRef") );  }
				
			errorOutput.WriteLine();
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: EngineInstance
		 * The <Engine.Instance> associated with the application.
		 */
		public static Engine.Instance EngineInstance
			{
			get
				{  return engineInstance;  }
			}

		public static int WorkerThreadCount
			{
			get
				{  return workerThreadCount;  }
			}

		public static int DefaultWorkerThreadCount
			{
			get
				{
				int processorCount = System.Environment.ProcessorCount;
			
				if (processorCount < 1)  // Sanity check
					{  return 1;  }
				else if (processorCount <= 4)  // Use everything on dual and quad core processors
					{  return processorCount;  }
				else if (processorCount <= 7)  // Don't take the whole processor above 4 cores
					{  return 4;  }
				else  // Max out at 6 if we have 8 cores or more
					{  return 6;  }
				}
			}



		// Group: Variables
		// __________________________________________________________________________

		static private Engine.Instance engineInstance;

		static private ProjectConfig commandLineConfig;

		/* var: quiet
		 * Whether the application should suppress all non-error output.
		 */
		static private bool quiet;

		static private int workerThreadCount;
		static private int totalFileChanges;

		/* var: benchmark
		 * Whether the application should show how long it takes to execute various sections of code.
		 */
		static private bool benchmark;

		static private bool pauseOnError;
		static private bool pauseBeforeExit;

		static private ExecutionTimer executionTimer;
		
		}
	}
