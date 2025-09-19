/*
 * Class: CodeClear.NaturalDocs.CLI.Application
 * ____________________________________________________________________________
 *
 * The main application class for the command line interface to Natural Docs.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Config;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.CLI
	{
	public static partial class Application
		{

		// Group: Constants
		// __________________________________________________________________________


		/* Constant: SimpleOutputStatusInterval
		 * The amount of time in milliseconds that must go by before a status update when using <SimpleOutput>.
		 */
		public const int SimpleOutputStatusInterval = 5000;

		/* Constant: LiveOutputStatusInterval
		 * The amount of time in milliseconds that must go by before a status update when not using <SimpleOutput>.
		 */
		public const int LiveOutputStatusInterval = 200;

		/* Constant: DelayedMessageThreshold
		 * The amount of time in milliseconds that certain operations must take before they warrant a status update.
		 */
		public const int DelayedMessageThreshold = 1500;

		/* Constant: SecondaryStatusIndent
		 * The string to use to indent secondary status messages when not using <SimpleOutput>.
		 */
		public const string SecondaryStatusIndent = " - ";



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
			simpleOutput = false;
			statusInterval = SimpleOutputStatusInterval;
			dashLength = 15;
			workerThreadCount = DefaultWorkerThreadCount;
			totalFileChanges = 0;
			benchmark = false;
			pauseOnError = false;
			pauseBeforeExit = false;

			ErrorList startupErrors = new ErrorList();
			bool gracefulExit = false;

			var standardOutput = System.Console.Out;

			try
				{
				engineInstance = new NaturalDocs.Engine.Instance();

				ParseCommandLineResult parseCommandLineResult = ParseCommandLine(commandLine, out commandLineConfig, startupErrors);


				if (parseCommandLineResult == ParseCommandLineResult.Error)
					{
					HandleErrorList(startupErrors);
					}

				else if (parseCommandLineResult == ParseCommandLineResult.ShowCommandLineReference)
					{
					ShowCommandLineReference();
					}

				else if (parseCommandLineResult == ParseCommandLineResult.ShowVersion)
					{
					ShowVersion();
					}

				else if (parseCommandLineResult == ParseCommandLineResult.ShowAllVersions)
					{
					ShowAllVersions();
					}

				else if (parseCommandLineResult == ParseCommandLineResult.ShowEncodings)
					{
					ShowEncodings();
					}

				else // (parseCommandLineResult == ParseCommandLineResult.Run)
					{
					if (quiet)
						{
						// This is easier and less error prone than putting conditional statements around all the non-error console
						// output, even if it's less efficient.
						System.Console.SetOut(System.IO.TextWriter.Null);
						}


					// Set up live vs. simple console output and related settings

					if (System.Console.IsOutputRedirected)
						{  simpleOutput = true;  }

					try
						{
						// Also test if we're in an environment where we can get and set the cursor position.  Checking IsOutputRedirected
						// *should* be enough, but check for exceptions just in case and we can fall back to simple output if one is thrown.
						int x = System.Console.CursorLeft;
						System.Console.CursorLeft = x;
						}
					catch
						{  simpleOutput = true;  }

					// Different status intervals for different output settings
					statusInterval = (simpleOutput ? SimpleOutputStatusInterval : LiveOutputStatusInterval);

					// Try to hide the cursor during execution, which isn't supported on all platforms
					try
						{  System.Console.CursorVisible = false;  }
					catch
						{  }


					// Create project configuration files only

					if (commandLineConfig.ProjectConfigFolderPropertyLocation.IsDefined &&
						commandLineConfig.InputTargets.Count == 0 &&
						commandLineConfig.OutputTargets.Count == 0 &&
						!System.IO.File.Exists(commandLineConfig.ProjectConfigFolder + "/Project.txt"))
						{
						if (!CreateProjectConfiguration(startupErrors))
							{  HandleErrorList(startupErrors);  }
						}


					// Normal execution

					else
						{
						if (!BuildDocumentation(startupErrors))
							{  HandleErrorList(startupErrors);  }
						}


					// Restore the cursor
					try
						{  System.Console.CursorVisible = true;  }
					catch
						{  }
					}

				gracefulExit = true;
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

			if (pauseBeforeExit || (pauseOnError && (!gracefulExit || startupErrors.Count > 0)))
				{
				// Flush any buffered input.  We only want to respond to new keypresses.
				while (Console.KeyAvailable)
					{  Console.ReadKey(true);  }

				System.Console.WriteLine();
				System.Console.Write(
					Engine.Locale.SafeGet("NaturalDocs.CLI", "Status.PressAnyKeyToContinue", "Press any key to continue...")
					);
				System.Console.ReadKey(true);
				System.Console.WriteLine();
				}
			}


		private static bool BuildDocumentation (ErrorList errorList)
			{
			bool successful = true;


			ShowConsoleHeader();

			EngineInstance.AddStartupWatcher(new EngineStartupWatcher());


			executionTimer.Start("Engine Startup");

			if (EngineInstance.Start(errorList, commandLineConfig) == true)
				{
				executionTimer.End("Engine Startup");


				// File Search

				executionTimer.Start("Finding Source Files");

				var adderProcess = EngineInstance.Files.CreateAdderProcess();

				using ( StatusManagers.FileSearch statusManager = new StatusManagers.FileSearch(adderProcess) )
					{
					statusManager.Start();

					Multithread("File Adder", adderProcess.WorkOnAddingAllFiles);

					statusManager.End();
					}

				EngineInstance.Files.DeleteFilesNotReAdded( Engine.Delegates.NeverCancel );
				adderProcess.Dispose();

				executionTimer.End("Finding Source Files");


				// Rebuild notice

				string alternativeSummaryMessage = null;

				if (EngineInstance.Config.UserWantsEverythingRebuilt ||
					EngineInstance.Config.UserWantsOutputRebuilt)
					{
					alternativeSummaryMessage = "Status.RebuildEverythingByRequest";
					}
				else if (EngineInstance.HasIssues(StartupIssues.NeedToStartFresh |
																  StartupIssues.NeedToReparseAllFiles |
																  StartupIssues.NeedToRebuildAllOutput))
					{
					alternativeSummaryMessage = "Status.RebuildEverythingAutomatically";
					}


				// Parsing

				executionTimer.Start("Parsing Source Files");

				var changeProcessor = EngineInstance.Files.CreateChangeProcessor();

				using ( StatusManagers.Parsing statusManager = new StatusManagers.Parsing(changeProcessor, alternativeSummaryMessage) )
					{
					statusManager.Start();
					totalFileChanges = statusManager.TotalFilesToProcess;

					Multithread("Parser", changeProcessor.WorkOnProcessingChanges);

					statusManager.End();
					}

				changeProcessor.Dispose();

				executionTimer.End("Parsing Source Files");


				// Resolving

				executionTimer.Start("Resolving Links");

				var resolverProcess = EngineInstance.Links.CreateResolverProcess();

				using ( StatusManagers.ResolvingLinks statusManager = new StatusManagers.ResolvingLinks(resolverProcess) )
					{
					statusManager.Start();

					Multithread("Resolver", resolverProcess.WorkOnResolvingLinks);

					statusManager.End();
					}

				resolverProcess.Dispose();

				executionTimer.End("Resolving Links");


				// Building

				executionTimer.Start("Building Output");

				var builderProcess = EngineInstance.Output.CreateBuilderProcess();

				using ( StatusManagers.Building statusManager = new StatusManagers.Building(builderProcess) )
					{
					statusManager.Start();

					Multithread("Builder", builderProcess.WorkOnUpdatingOutput);
					Multithread("Finalizer", builderProcess.WorkOnFinalizingOutput);

					statusManager.End();
					}

				builderProcess.Dispose();

				executionTimer.End("Building Output");


				// End

				EngineInstance.Cleanup(Delegates.NeverCancel);

				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.End")
					);

				successful = true;
				}

			else // engine did not start correctly
				{
				executionTimer.End("Engine Startup");
				successful = false;
				}


			ShowConsoleFooter(successful);

			return successful;
			}

		private static bool CreateProjectConfiguration (ErrorList errorList)
			{
			ShowConsoleHeader();

			if (System.IO.Directory.Exists(commandLineConfig.ProjectConfigFolder) == false)
				{
				errorList.Add(
					Engine.Locale.Get("NaturalDocs.Engine", "Error.ProjectConfigFolderDoesntExist(name)", commandLineConfig.ProjectConfigFolder)
					);
				return false;
				}

			System.Console.Write(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.CreatingProjectConfigFiles")
				);

			int priorErrorCount = errorList.Count;

			Engine.Config.ConfigFiles.TextFileParser projectTxtParser = new Engine.Config.ConfigFiles.TextFileParser();
			projectTxtParser.Save(commandLineConfig.ProjectConfigFolder + "/Project.txt", commandLineConfig, errorList);

			if (System.IO.File.Exists(commandLineConfig.ProjectConfigFolder + "/Languages.txt") == false)
				{
				var  languagesTxtParser = new Engine.Languages.ConfigFiles.TextFileParser();
				languagesTxtParser.Save(commandLineConfig.ProjectConfigFolder + "/Languages.txt", PropertySource.ProjectLanguagesFile,
													 new Engine.Languages.ConfigFiles.TextFile(), errorList);
				}

			if (System.IO.File.Exists(commandLineConfig.ProjectConfigFolder + "/Comments.txt") == false)
				{
				var commentsTxtParser = new Engine.CommentTypes.ConfigFiles.TextFileParser();
				commentsTxtParser.Save(commandLineConfig.ProjectConfigFolder + "/Comments.txt", PropertySource.ProjectCommentsFile,
													 new Engine.CommentTypes.ConfigFiles.TextFile(), errorList);
				}

			if (errorList.Count > priorErrorCount)
				{
				System.Console.WriteLine();
				ShowConsoleFooter(false);
				return false;
				}
			else
				{
				// Make the "Done" position consistent with how it would look in a normal build
				if (SimpleOutput)
					{  System.Console.WriteLine();  }
				else
					{  System.Console.Write(' ');  }

				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.End")
					);

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


		private static void ShowAllVersions ()
			{
			Console.Write( SystemInfo.BuildDiagnosticSummary() );
			}


		private static void ShowEncodings ()
			{
			// Get the system encodings

			var systemEncodings = System.Text.Encoding.GetEncodings();


			// Convert them to a List so that we can sort it by description.  We also use our own struct because we can't create
			// System.Text.Encoding.EncodingInfos to add our auto-detect option or change them to make the UTF-16 descriptions
			// clearer.

			List<EncodingTableEntry> encodingTable = new List<EncodingTableEntry>(systemEncodings.Length + 1);
			bool missingDescriptions = false;

			foreach (var systemEncoding in systemEncodings)
				{
				string description = systemEncoding.DisplayName;

				// Improve the descriptions for UTF-16
				if (systemEncoding.CodePage == 1200 && description == "Unicode")
					{  description = "Unicode (UTF-16)";  }
				else if (systemEncoding.CodePage == 1201 && description == "Unicode (Big-Endian)")
					{  description = "Unicode (UTF-16 Big-Endian)";  }

				// Unfortunately in Mono there are no descriptions, just "Globalization.cp_" plus the code page number.
				// That makes the sort really suck, so replace them with the name.
				else if (description.StartsWith("Globalization.cp_", StringComparison.OrdinalIgnoreCase))
					{
					// We can manually do the Unicode ones at least.
					if (systemEncoding.CodePage == 1200)
						{  description = "Unicode (UTF-16)";  }
					else if (systemEncoding.CodePage == 1201)
						{  description = "Unicode (UTF-16 Big-Endian)";  }
					else if (systemEncoding.CodePage == 12000)
						{  description = "Unicode (UTF-32)";  }
					else if (systemEncoding.CodePage == 12001)
						{  description = "Unicode (UTF-32 Big-Endian)";  }
					else if (systemEncoding.CodePage == 65000)
						{  description = "Unicode (UTF-7)";  }
					else if (systemEncoding.CodePage == 65001)
						{  description = "Unicode (UTF-8)";  }
					else
						{
						description = systemEncoding.Name;

						if (description.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
							{  description = description.Substring(2);  }

						missingDescriptions = true;
						}
					}

				encodingTable.Add(
					new EncodingTableEntry(systemEncoding.Name, description, systemEncoding.CodePage)
					);
				}


			// Add our auto-detect option

			string unicodeName = "Unicode";
			string unicodeDescription = Locale.Get("NaturalDocs.CLI", "Encodings.UnicodeDescription");

			encodingTable.Add(
				new EncodingTableEntry(unicodeName, unicodeDescription, 0)
				);


			// Sort it by description

			encodingTable.Sort(
				delegate (EncodingTableEntry a, EncodingTableEntry b)
					{  return string.Compare(a.Description, b.Description);  }
				);


			// Get the headers

			string nameHeader = Locale.Get("NaturalDocs.CLI", "Encodings.Name");
			string descriptionHeader = Locale.Get("NaturalDocs.CLI", "Encodings.Description");
			string codePageHeader = Locale.Get("NaturalDocs.CLI", "Encodings.CodePage");


			// Get the column widths

			int nameColumnWidth = nameHeader.Length;
			int descriptionColumnWidth = descriptionHeader.Length;
			int codePageColumnWidth = codePageHeader.Length;
			int maxCodePage = 0;

			foreach (var encoding in encodingTable)
				{
				nameColumnWidth = Math.Max(nameColumnWidth, encoding.Name.Length);
				descriptionColumnWidth = Math.Max(descriptionColumnWidth, encoding.Description.Length);
				maxCodePage = Math.Max(maxCodePage, encoding.CodePage);
				}

			codePageColumnWidth = Math.Max(codePageColumnWidth, maxCodePage.ToString().Length);


			// Display the header

			string format = "{0,-" + descriptionColumnWidth + "}  {1,-" + nameColumnWidth + "}  {2,-" + codePageColumnWidth + "}";

			string formatLine = String.Format(format, descriptionHeader, nameHeader, codePageHeader);
			System.Console.WriteLine(formatLine);

			StringBuilder dashedLineBuilder = new StringBuilder(descriptionColumnWidth + nameColumnWidth + codePageColumnWidth + 6);
			dashedLineBuilder.Append('-', descriptionColumnWidth);
			dashedLineBuilder.Append(' ', 2);
			dashedLineBuilder.Append('-', nameColumnWidth);
			dashedLineBuilder.Append(' ', 2);
			dashedLineBuilder.Append('-', codePageColumnWidth);

			string dashedLine = dashedLineBuilder.ToString();
			System.Console.WriteLine(dashedLine);


			// Display the encodings

			foreach (var encoding in encodingTable)
				{
				System.Console.WriteLine(format, encoding.Description, encoding.Name, encoding.CodePage);
				}


			// Display a footer similar to the header

			System.Console.WriteLine(dashedLine);
			System.Console.WriteLine(formatLine);


			// Display the description notice if necessary

			if (missingDescriptions)
				{
				System.Console.WriteLine();
				System.Console.Write(
					Locale.Get("NaturalDocs.CLI", "Encodings.MissingDescriptions.multiline")
					);
				}
			}



		// Group: Support Functions
		// __________________________________________________________________________


		private static void ShowConsoleHeader ()
			{
			string version = Engine.Instance.Version.PrimaryVersionString;
			string subversion = Engine.Instance.Version.SecondaryVersionString;

			string versionOutput = Engine.Locale.Get("NaturalDocs.CLI", "Status.Start(version)", version);

			System.Console.WriteLine();
			System.Console.WriteLine(versionOutput);

			dashLength = Math.Max(dashLength, versionOutput.Length);

			if (subversion != null)
				{
				System.Console.WriteLine(subversion);
				dashLength = Math.Max(dashLength, subversion.Length);
				}

			StringBuilder dashedLine = new StringBuilder(dashLength);
			dashedLine.Append('-', dashLength);

			System.Console.WriteLine(dashedLine.ToString());
			}


		private static void ShowConsoleFooter (bool successful)
			{
			if (successful)
				{
				StringBuilder dashedLine = new StringBuilder(dashLength);
				dashedLine.Append('-', dashLength);

				System.Console.WriteLine(dashedLine.ToString());
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

			System.Console.Write ("\n\n------------------------------------------------------------\n");
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
					errorOutput.WriteLine("(" + e.GetType() + ")");
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

			System.Console.Write ("------------------------------------------------------------\n\n");
			}


		/* Function: HandleErrorList
		 */
		static private void HandleErrorList (ErrorList errorList)
			{
			// Annotate any config files before printing them to the console, since the line numbers may change.

			Engine.ConfigFile.TryToAnnotateWithErrors(errorList);


			// Write them to the console.

			var errorOutput = Console.Error;
			System.Console.WriteLine();

			Path lastErrorFile = null;
			bool hasNonFileErrors = false;

			foreach (var error in errorList)
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

			System.Console.WriteLine();
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

		/* Property: SimpleOutput
		 * Whether the application should only use simple console output commands.
		 */
		public static bool SimpleOutput
			{
			get
				{  return simpleOutput;  }
			}

		/* Property: StatusInterval
		 * The amount of time in milliseconds that must go by before a status update.
		 */
		public static int StatusInterval
			{
			get
				{  return statusInterval;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		static private Engine.Instance engineInstance;

		static private ProjectConfig commandLineConfig;

		/* var: quiet
		 * Whether the application should suppress all non-error output.
		 */
		static private bool quiet;

		/* var: simpleOutput
		 * Whether the application should only use simple console output commands.
		 */
		static private bool simpleOutput;

		/* var: statusInterval
		 * The amount of time in milliseconds that must go by before a status update.
		 */
		static private int statusInterval;

		/* var: dashLength
		 * The number of dashes to include in horizontal lines in the output.
		 */
		static private int dashLength;

		static private int workerThreadCount;
		static private int totalFileChanges;

		/* var: benchmark
		 * Whether the application should show how long it takes to execute various sections of code.
		 */
		static private bool benchmark;

		static private bool pauseOnError;
		static private bool pauseBeforeExit;

		static private ExecutionTimer executionTimer;


		/* __________________________________________________________________________
		 *
		 * Struct: EncodingTableEntry
		 * __________________________________________________________________________
		 */
		private struct EncodingTableEntry
			{
			public EncodingTableEntry (string name, string description, int codePage)
				{
				this.Name = name;
				this.Description = description;
				this.CodePage = codePage;
				}

			public string Name;
			public string Description;
			public int CodePage;
			}

		}
	}
