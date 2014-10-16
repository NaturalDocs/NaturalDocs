/* 
 * Class: GregValure.NaturalDocs.CLI.Application
 * ____________________________________________________________________________
 * 
 * The main application class for the command line interface to Natural Docs.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine;
using GregValure.NaturalDocs.Engine.Config;
using GregValure.NaturalDocs.Engine.Errors;


namespace GregValure.NaturalDocs.CLI
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

			#if PAUSE_BEFORE_EXIT
				bool pauseBeforeExit = true;
			#elif PAUSE_ON_ERROR
				bool pauseBeforeExit = false;
			#endif
			
			bool gracefulExit = false;
			quiet = false;
			showExecutionTime = false;

			var standardOutput = System.Console.Out;

			try
				{
				NaturalDocs.Engine.Instance.Create();
				
				ErrorList startupErrors = new ErrorList();
				ParseCommandLineResult parseCommandLineResult = ParseCommandLine(commandLine, out commandLineConfig, startupErrors);

				
				if (parseCommandLineResult == ParseCommandLineResult.Error)
					{
					HandleErrorList(startupErrors);
					
					#if PAUSE_ON_ERROR
						pauseBeforeExit = true;
					#endif
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
							{
							HandleErrorList(startupErrors);
					
							#if PAUSE_ON_ERROR
								pauseBeforeExit = true;
							#endif
							}
						}


					// Normal execution

					else
						{
						if (BuildDocumentation(startupErrors))
							{  gracefulExit = true;  }
						else
							{
							HandleErrorList(startupErrors);
					
							#if PAUSE_ON_ERROR
								pauseBeforeExit = true;
							#endif
							}
						}
					}
				}

			catch (Exception e)
				{  
				HandleException(e);  
				
				#if PAUSE_ON_ERROR
					pauseBeforeExit = true;
				#endif
				}
				
			finally
				{
				Engine.Instance.Dispose(gracefulExit);

				// Restore the standard output
				if (quiet)
					{  System.Console.SetOut(standardOutput);  }
				}
				
			executionTimer.End("Total Execution");

			if (showExecutionTime)
				{  System.Console.Write(executionTimer.StatisticsToString());  }

			#if PAUSE_BEFORE_EXIT || PAUSE_ON_ERROR
				if (pauseBeforeExit)
					{
					System.Console.WriteLine();
					System.Console.WriteLine("Press any key to continue...");
					System.Console.ReadKey(true);
					}
			#endif
			}


		private static bool BuildDocumentation (ErrorList errorList)
			{
			ShowConsoleHeader();

			bool rebuildAllOutputFromCommandLine = Engine.Instance.Config.RebuildAllOutput;
			bool reparseEverythingFromCommandLine = Engine.Instance.Config.ReparseEverything;

			NaturalDocs.Engine.Instance.AddStartupWatcher(new EngineStartupWatcher());

			if (NaturalDocs.Engine.Instance.Start(errorList, commandLineConfig) == true)
				{

				// File Search
						
				using ( StatusManagers.FileSearch statusManager = new StatusManagers.FileSearch() )
					{
					statusManager.Start();
							
					Multithread("File Adder", Engine.Instance.Files.WorkOnAddingAllFiles);
							
					statusManager.End();
					}
							
				Engine.Instance.Files.DeleteFilesNotInFileSources( Engine.Delegates.NeverCancel );
							
						
				// Rebuild notice

				string alternateStartMessage = null;
						
				if (reparseEverythingFromCommandLine || rebuildAllOutputFromCommandLine)
					{  alternateStartMessage = "Status.RebuildEverythingByRequest";  }
				else if (Engine.Instance.Config.ReparseEverything && Engine.Instance.Config.RebuildAllOutput)
					{  alternateStartMessage = "Status.RebuildEverythingAutomatically";  }
							
							
				// Parsing
						
				executionTimer.Start("Parsing Source Files");

				using ( StatusManagers.Parsing statusManager = new StatusManagers.Parsing(alternateStartMessage) )
					{
					statusManager.Start();

					Multithread("Parser", Engine.Instance.Files.WorkOnProcessingChanges);							
							
					statusManager.End();
					}
							
				executionTimer.End("Parsing Source Files");

							
				// Resolving
						
				executionTimer.Start("Resolving Links");

				using ( StatusManagers.ResolvingLinks statusManager = new StatusManagers.ResolvingLinks() )
					{
					statusManager.Start();

					Multithread("Resolver", Engine.Instance.CodeDB.WorkOnResolvingLinks);
							
					statusManager.End();
					}
							
				executionTimer.End("Resolving Links");

							
				// Building
						
				executionTimer.Start("Building Output");

				using ( StatusManagers.Building statusManager = new StatusManagers.Building() )
					{
					statusManager.Start();

					Multithread("Builder", Engine.Instance.Output.WorkOnUpdatingOutput);
					Multithread("Finalizer", Engine.Instance.Output.WorkOnFinalizingOutput);							
							
					statusManager.End();
					}
							
				executionTimer.End("Building Output");

							
				// End
						
				Engine.Instance.Cleanup(Delegates.NeverCancel);
						
				ShowConsoleFooter(true);
				return true;
				}

			else // engine did not start correctly
				{  
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

			if (System.IO.File.Exists(commandLineConfig.ProjectConfigFolder + "/Topics.txt") == false)
				{
				Engine.TopicTypes.Topics_txt topicsTxtParser = new Engine.TopicTypes.Topics_txt();
				topicsTxtParser.Save(commandLineConfig.ProjectConfigFolder + "/Topics.txt",
												new List<Engine.TopicTypes.ConfigFileTopicType>(), new List<string>(), new List<string>(),
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
			Engine.Thread[] threads = new Engine.Thread[ Engine.Instance.Config.BackgroundThreadsPerTask ];

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


		/* Function: HandleException
		 */
		static private void HandleException (Exception e)
			{
			var errorOutput = Console.Error;

			errorOutput.Write ("\n\n------------------------------------------------------------\n");
			errorOutput.WriteLine (Locale.SafeGet("NaturalDocs.CLI", "Crash.Exception",
														"Natural Docs has closed because of the following error:"));
			errorOutput.WriteLine();
			errorOutput.WriteLine(e.Message);
				
			
			// If it's not a user friendly exception or a thread exception wrapping a user friendly exception...
			if ( e.GetType() != typeof(Engine.Exceptions.UserFriendly) &&
				 ( e.GetType() == typeof(Engine.Exceptions.Thread) &&
				   e.InnerException.GetType() == typeof(Engine.Exceptions.UserFriendly) ) == false )
				{
				Engine.Path crashFile = Engine.Instance.BuildCrashReport(e);

				if (crashFile != null)
					{
					errorOutput.WriteLine();
					errorOutput.Write (Locale.SafeGet("NaturalDocs.CLI", "Crash.ReportAt(file).multiline", 
														"A crash report has been generated at {0}.\n" +
														"Please include this file when asking for help at naturaldocs.org.\n", crashFile));
					}
					
				else
					{
					errorOutput.WriteLine ();
					errorOutput.WriteLine (e.StackTrace);  
					
					// If it's a thread exception, skip the first inner one because that's the wrapped one, which we already got the
					// message for.
					if (e is Engine.Exceptions.Thread)
						{  e = e.InnerException;  }
						
					while (e.InnerException != null)
						{
						e = e.InnerException;

						errorOutput.WriteLine ();
						errorOutput.WriteLine (Locale.SafeGet("NaturalDocs.CLI", "Crash.NestedException",
																   "This error was caused by the following error:") + "\n");

						errorOutput.WriteLine (e.Message);
						}
						
					try
						{
						errorOutput.WriteLine ();
						errorOutput.WriteLine ( Locale.SafeGet("NaturalDocs.CLI", "Crash.Version", "Version") +
																	": " + Engine.Instance.VersionString );
						errorOutput.WriteLine ( Locale.SafeGet("NaturalDocs.CLI", "Crash.Platform", "Platform") +
																	": " + Environment.OSVersion.VersionString +
																	" (" + Environment.OSVersion.Platform + ")" );
						errorOutput.WriteLine ( "SQLite: " + Engine.SQLite.API.LibVersion() );
						errorOutput.WriteLine ();
						errorOutput.WriteLine ( Locale.SafeGet("NaturalDocs.CLI", "Crash.CommandLine", "Command Line") + ":" );
						errorOutput.WriteLine ();
						errorOutput.WriteLine ("   " + Environment.CommandLine );
						}
					catch
						{
						}
						
					errorOutput.WriteLine ();
					errorOutput.WriteLine (Locale.SafeGet("NaturalDocs.CLI", "Crash.IncludeInfoAndGetHelp",
															   "Please include this information when asking for help at naturaldocs.org."));
					}
				}

			errorOutput.Write ("\n------------------------------------------------------------\n\n");
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



		// Group: Variables
		// __________________________________________________________________________


		static private ProjectConfig commandLineConfig;

		/* var: quiet
		 * Whether the application should suppress all non-error output.
		 */
		static private bool quiet;

		/* var: showExecutionTime
		 * Whether the application should show how long it takes to execute various sections of code.
		 */
		static private bool showExecutionTime;

		static private ExecutionTimer executionTimer;
		
		}
	}
