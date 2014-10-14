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
	public static class Application
		{
		
		// Group: Types
		// __________________________________________________________________________
		
		
		/* enum: ParseCommandLineResult
		 * 
		 * The result returned from <ParseCommandLine()>.
		 * 
		 * OK - The command line was OK and execution should continue.
		 * Error - There was an error on the command line.
		 * InformationalExit - The program was asked for information, such as the version number, that 
		 *								<ParseCommandLine()> provided and now execution should cease.
		 */
		public enum ParseCommandLineResult : byte
			{  OK, Error, InformationalExit  };
			
			
			
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
			
						

		// Group: Functions
		// __________________________________________________________________________


		/* Function: Main
		 * The program entry point.
		 */
		public static void Main (string[] commandLine)
			{
			#if SHOW_EXECUTION_TIME
				var executionTimer = new ExecutionTimer();
				executionTimer.Start("Total Execution");
			#endif

			#if PAUSE_BEFORE_EXIT
				bool pauseBeforeExit = true;
			#elif PAUSE_ON_ERROR
				bool pauseBeforeExit = false;
			#endif
			
			bool gracefulExit = false;
			quiet = false;
			var standardOutput = System.Console.Out;

			try
				{
				ErrorList startupErrors = new ErrorList();

				NaturalDocs.Engine.Instance.Create();
				
				ProjectConfig commandLineConfig;
				ParseCommandLineResult parseCommandLineResult = ParseCommandLine(commandLine, out commandLineConfig, startupErrors);

				
				if (parseCommandLineResult == ParseCommandLineResult.Error)
					{
					HandleErrorList(startupErrors);
					
					#if PAUSE_ON_ERROR
						pauseBeforeExit = true;
					#endif
					}

				else if (parseCommandLineResult == ParseCommandLineResult.InformationalExit)
					{
					gracefulExit = true;
					}

				else // (parseCommandLineResult == ParseCommandLineResult.OK)
					{
					if (quiet)
						{  
						// This is easier and less error prone than putting conditional statements around all the non-error console
						// output, even if it's less efficient.
						System.Console.SetOut(System.IO.TextWriter.Null);
						}


					// Heading

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


					// Create project configuration files only

					if (commandLineConfig.ProjectConfigFolderPropertyLocation.IsDefined &&
						commandLineConfig.InputTargets.Count == 0 &&
						commandLineConfig.OutputTargets.Count == 0 &&
						System.IO.File.Exists(commandLineConfig.ProjectConfigFolder + "/Project.txt") == false)
						{
						System.Console.WriteLine(
							Engine.Locale.Get("NaturalDocs.CLI", "Status.CreatingProjectConfigFiles")
							);

						Engine.Config.Project_txt projectTxtParser = new Engine.Config.Project_txt();
						projectTxtParser.Save(commandLineConfig.ProjectConfigFolder + "/Project.txt", commandLineConfig, startupErrors);

						if (System.IO.File.Exists(commandLineConfig.ProjectConfigFolder + "/Languages.txt") == false)
							{
							Engine.Languages.Languages_txt languagesTxtParser = new Engine.Languages.Languages_txt();
							languagesTxtParser.Save(commandLineConfig.ProjectConfigFolder + "/Languages.txt",
																new List<Engine.Languages.ConfigFileLanguage>(), new List<string>(),
																startupErrors, true, false);
							}

						if (System.IO.File.Exists(commandLineConfig.ProjectConfigFolder + "/Topics.txt") == false)
							{
							Engine.TopicTypes.Topics_txt topicsTxtParser = new Engine.TopicTypes.Topics_txt();
							topicsTxtParser.Save(commandLineConfig.ProjectConfigFolder + "/Topics.txt",
														   new List<Engine.TopicTypes.ConfigFileTopicType>(), new List<string>(), new List<string>(),
														   startupErrors, true, false);
							}

						if (startupErrors.Count > 0)
							{
							HandleErrorList(startupErrors);
					
							#if PAUSE_ON_ERROR
								pauseBeforeExit = true;
							#endif
							}
						else
							{
							System.Console.Write(
								Engine.Locale.Get("NaturalDocs.CLI", "Status.End.multiline")
								);
							System.Console.WriteLine();
						
							gracefulExit = true;
							}
						}


					// Normal execution

					else
						{
						bool rebuildAllOutputFromCommandLine = Engine.Instance.Config.RebuildAllOutput;
						bool reparseEverythingFromCommandLine = Engine.Instance.Config.ReparseEverything;

						NaturalDocs.Engine.Instance.AddStartupWatcher(new EngineStartupWatcher());

						if (NaturalDocs.Engine.Instance.Start(startupErrors, commandLineConfig) == true)
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
						
							#if SHOW_EXECUTION_TIME
								executionTimer.Start("Parsing Source Files");
							#endif

							using ( StatusManagers.Parsing statusManager = new StatusManagers.Parsing(alternateStartMessage) )
								{
								statusManager.Start();

								Multithread("Parser", Engine.Instance.Files.WorkOnProcessingChanges);							
							
								statusManager.End();
								}
							
							#if SHOW_EXECUTION_TIME
								executionTimer.End("Parsing Source Files");
							#endif

							
							// Resolving
						
							#if SHOW_EXECUTION_TIME
								executionTimer.Start("Resolving Links");
							#endif

							using ( StatusManagers.ResolvingLinks statusManager = new StatusManagers.ResolvingLinks() )
								{
								statusManager.Start();

								Multithread("Resolver", Engine.Instance.CodeDB.WorkOnResolvingLinks);
							
								statusManager.End();
								}
							
							#if SHOW_EXECUTION_TIME
								executionTimer.End("Resolving Links");
							#endif

							
							// Building
						
							#if SHOW_EXECUTION_TIME
								executionTimer.Start("Building Output");
							#endif

							using ( StatusManagers.Building statusManager = new StatusManagers.Building() )
								{
								statusManager.Start();

								Multithread("Builder", Engine.Instance.Output.WorkOnUpdatingOutput);
								Multithread("Finalizer", Engine.Instance.Output.WorkOnFinalizingOutput);							
							
								statusManager.End();
								}
							
							#if SHOW_EXECUTION_TIME
								executionTimer.End("Building Output");
							#endif

							
							// End
						
							Engine.Instance.Cleanup(Delegates.NeverCancel);
						
							System.Console.Write(
								Engine.Locale.Get("NaturalDocs.CLI", "Status.End.multiline")
								);
							System.Console.WriteLine();
						
							gracefulExit = true;
							}

						else // engine did not start correctly
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
				
			#if SHOW_EXECUTION_TIME
				executionTimer.End("Total Execution");
				System.Console.Write(executionTimer.StatisticsToString());
			#endif

			#if PAUSE_BEFORE_EXIT || PAUSE_ON_ERROR
				if (pauseBeforeExit)
					{
					System.Console.WriteLine();
					System.Console.WriteLine("Press any key to continue...");
					System.Console.ReadKey(true);
					}
			#endif
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
		static public void Multithread (string threadName, CancellableTask task)
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


		/* Function: ParseCommandLine
		 * 
		 * Parses the command line and applies the relevant settings in in <NaturalDocs.Engine's> modules.  If there were 
		 * errors they will be placed on errorList and it will return <ParseCommandLineResult.Error>.  If the command line 
		 * was used to ask for information such as the program version, it will write it to the console and return
		 * <ParseCommandLineResult.InformationalExit>.
		 * 
		 * Supported:
		 * 
		 *		- -i, --input, --source
		 *		- -o, --output
		 *		- -p, --project, --project-config --project-configuration
		 *		- -w, --data, --working-data
		 *		- -xi, --exclude-input, --exclude-source
		 *		- -xip, --exclude-input-pattern, --exclude-source-pattern
		 *		- -img, --images
		 *		- -t, --tab, --tab-width, --tab-length
		 *		- -do, --documented-only
		 *		- -nag, --no-auto-group
		 *		- -s, --style, --default-style
		 *		- -r, --rebuild
		 *		- -ro, --rebuild-output
		 *		- -h, --help
		 *		- -?
		 *		- -v, --version
		 *		
		 * Unsupported so far:
		 * 
		 *		- -q, --quiet
		 *		
		 * No longer supported:
		 * 
		 *		- -cs, --char-set, --charset, --character-set, --characterset
		 *		- -ho, --headers-only, --headersonly
		 *		- -ag, --auto-group, --autogroup
		 */
		public static ParseCommandLineResult ParseCommandLine (string[] commandLineSegments, out ProjectConfig commandLineConfig, ErrorList errorList)
			{
			int originalErrorCount = errorList.Count;

			Engine.CommandLine commandLine = new CommandLine(commandLineSegments);

			commandLine.AddAliases("--input", "-i", "--source");
			commandLine.AddAliases("--output", "-o");
			commandLine.AddAliases("--project", "-p", "--project-config", "--projectconfig", "--project-configuration", "--projectconfiguration");
			commandLine.AddAliases("--working-data", "-w", "--data", "--workingdata");
			commandLine.AddAliases("--exclude-input", "-xi", "--excludeinput", "--exclude-source", "--excludesource");
			commandLine.AddAliases("--exclude-input-pattern", "-xip", "--excludeinputpattern", "--exclude-source-pattern", "--excludesourcepattern");
			commandLine.AddAliases("--images", "-img");
			commandLine.AddAliases("--tab-width", "-t", "--tab", "--tabwidth", "--tab-length", "--tablength");
			commandLine.AddAliases("--documented-only", "-do", "--documentedonly");
			commandLine.AddAliases("--no-auto-group", "-nag", "--noautogroup");
			commandLine.AddAliases("--style", "-s", "--default-style", "--defaultstyle");
			commandLine.AddAliases("--rebuild", "-r");
			commandLine.AddAliases("--rebuild-output", "-ro", "--rebuildoutput");
			commandLine.AddAliases("--quiet", "-q");
			commandLine.AddAliases("--help", "-h", "-?");
			commandLine.AddAliases("--version", "-v");

			// No longer supported
			commandLine.AddAliases("--charset", "-cs", "--char-set", "--character-set", "--characterset");
			commandLine.AddAliases("--headers-only", "-ho", "--headersonly");
			commandLine.AddAliases("--auto-group", "-ag", "--autogroup");
			
			commandLineConfig = new ProjectConfig(Source.CommandLine);
			
			string parameter, parameterAsEntered;
			bool isFirst = true;
				
			while (commandLine.IsInBounds)
				{
				// If the first segment isn't a parameter, assume it's the project folder specified without -p.
				if (isFirst && !commandLine.IsOnParameter)
					{  
					parameter = "--project";
					parameterAsEntered = parameter;
					}
				else
					{  
					if (!commandLine.GetParameter(out parameter, out parameterAsEntered))
						{
						string bareWord;
						commandLine.GetBareWord(out bareWord);

						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.UnrecognizedParameter(param)", bareWord)
							);

						commandLine.SkipToNextParameter();
						}
					}

				isFirst = false;

					
				// Input folders
					
				if (parameter == "--input")
					{
					Path folder;
					
					if (!commandLine.GetPathValue(out folder))
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedFolder(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						if (folder.IsRelative)
							{  folder = System.Environment.CurrentDirectory + "/" + folder;  }
					
						var target = new Engine.Config.Targets.SourceFolder(Source.CommandLine, Engine.Files.InputType.Source);

						target.Folder = folder;
						target.FolderPropertyLocation = Source.CommandLine;

						commandLineConfig.InputTargets.Add(target);
						}
					}
					

					
				// Output folders
				
				else if (parameter == "--output")
					{
					string format;
					Path folder;
					
					if (!commandLine.GetBareWordAndPathValue(out format, out folder))
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedFormatAndFolder(param)", parameter)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						if (folder.IsRelative)
							{  folder = System.Environment.CurrentDirectory + "/" + folder;  }
					
						format = format.ToLower();

						if (format == "html" || format == "framedhtml")
							{  
							var target = new Engine.Config.Targets.HTMLOutputFolder(Source.CommandLine);

							target.Folder = folder;
							target.FolderPropertyLocation = Source.CommandLine;

							commandLineConfig.OutputTargets.Add(target);
							}
						else
							{
							errorList.Add( 
								Locale.Get("NaturalDocs.CLI", "CommandLine.UnrecognizedOutputFormat(format)", format)
								);

							commandLine.SkipToNextParameter();
							}
						}
					}



				// Project configuration folder
					
				else if (parameter == "--project")
					{
					Path folder;
					
					if (!commandLine.GetPathValue(out folder))
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedFolder(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						if (folder.IsRelative)
							{  folder = System.Environment.CurrentDirectory + "/" + folder;  }

						// Accept the parameter being set to Project.txt instead of the folder.
						if (folder.NameWithoutPath.ToLower() == "project.txt")
							{  folder = folder.ParentFolder;  }
						
						if (commandLineConfig.ProjectConfigFolderPropertyLocation.IsDefined)
							{
							errorList.Add( 
								Locale.Get("NaturalDocs.CLI", "CommandLine.OnlyOneProjectConfigFolder")
								);
							}
						else
							{  
							commandLineConfig.ProjectConfigFolder = folder;
							commandLineConfig.ProjectConfigFolderPropertyLocation = Source.CommandLine;
							}
						}
					}
					
					

				// Working data folder
					
				else if (parameter == "--working-data")
					{
					Path folder;
					
					if (!commandLine.GetPathValue(out folder))
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedFolder(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						if (folder.IsRelative)
							{  folder = System.Environment.CurrentDirectory + "/" + folder;  }

						if (commandLineConfig.WorkingDataFolderPropertyLocation.IsDefined)
							{
							errorList.Add( 
								Locale.Get("NaturalDocs.CLI", "CommandLine.OnlyOneWorkingDataFolder")
								);
							}
						else
							{
							commandLineConfig.WorkingDataFolder = folder;
							commandLineConfig.WorkingDataFolderPropertyLocation = Source.CommandLine;
							}
						}
					}
					
					
				
				// Ignored input folders
					
				else if (parameter == "--exclude-input")
					{
					Path folder;
					
					if (!commandLine.GetPathValue(out folder))
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedFolder(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						if (folder.IsRelative)
							{  folder = System.Environment.CurrentDirectory + "/" + folder;  }
					
						var target = new Engine.Config.Targets.IgnoredSourceFolder(Source.CommandLine);

						target.Folder = folder;
						target.FolderPropertyLocation = Source.CommandLine;

						commandLineConfig.FilterTargets.Add(target);
						}
					}
					
					
					
				// Ignored input folder patterns
					
				else if (parameter == "--exclude-input-pattern")
					{
					string pattern;

					if (!commandLine.GetBareOrQuotedWordsValue(out pattern))
						{
						errorList.Add(
							Engine.Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedPattern(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						var target =  new Engine.Config.Targets.IgnoredSourceFolderPattern(Source.CommandLine);

						target.Pattern = pattern;
						target.PatternPropertyLocation = Source.CommandLine;

						commandLineConfig.FilterTargets.Add(target);
						}
					}
					
					
					
				// Image folders
					
				else if (parameter == "--images")
					{
					Path folder;
					
					if (!commandLine.GetPathValue(out folder))
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedFolder(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						if (folder.IsRelative)
							{  folder = System.Environment.CurrentDirectory + "/" + folder;  }
					
						var target = new Engine.Config.Targets.SourceFolder(Source.CommandLine, Engine.Files.InputType.Image);

						target.Folder = folder;
						target.FolderPropertyLocation = Source.CommandLine;

						commandLineConfig.InputTargets.Add(target);
						}
					}
					
					
					
				// Tab Width
				
				else if (parameter == "--tab-width")
					{
					int tabWidth;
					
					if (!commandLine.GetIntegerValue(out tabWidth))
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedNumber(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else if (tabWidth < 1)
						{
						errorList.Add( 
							Locale.Get("NaturalDocs.CLI", "CommandLine.InvalidTabWidth")
							);

						commandLine.SkipToNextParameter();
						}
					else
						{  
						commandLineConfig.TabWidth = tabWidth;
						commandLineConfig.TabWidthPropertyLocation = Source.CommandLine;
						}	
					}
					
				// Support the -t4 form ini addition to -t 4.  Doesn't support --tablength4.
				else if (parameter.StartsWith("-t") && parameter.Length > 2 && parameter[2] >= '0' && parameter[2] <= '9')
					{
					string tabWidthString = parameter.Substring(2);
						
					int tabWidth;

					if (!Int32.TryParse(tabWidthString, out tabWidth))
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedNumber(param)", "-t")
							);

						commandLine.SkipToNextParameter();
						}
					else if (tabWidth < 1)
						{
						errorList.Add( 
							Locale.Get("NaturalDocs.CLI", "CommandLine.InvalidTabWidth")
							);

						commandLine.SkipToNextParameter();
						}
					else if (!commandLine.NoValue())
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedNoValue(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{  
						commandLineConfig.TabWidth = tabWidth;
						commandLineConfig.TabWidthPropertyLocation = Source.CommandLine;
						}
					}
					
					
					
				// Documented Only
					
				else if (parameter == "--documented-only")
					{
					if (!commandLine.NoValue())
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedNoValue(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						commandLineConfig.DocumentedOnly = true;
						commandLineConfig.DocumentedOnlyPropertyLocation = Source.CommandLine;
						}
					}
					
					

				// No Auto-Group
					
				else if (parameter == "--no-auto-group")
					{
					if (!commandLine.NoValue())
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedNoValue(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						commandLineConfig.AutoGroup = false;
						commandLineConfig.AutoGroupPropertyLocation = Source.CommandLine;
						}
					}
					
					

				// Style
					
				else if (parameter == "--style")
					{
					string styleName;
					
					if (!commandLine.GetBareOrQuotedWordsValue(out styleName))
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedStyleName(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						commandLineConfig.ProjectInfo.StyleName = styleName;
						commandLineConfig.ProjectInfo.StyleNamePropertyLocation = Source.CommandLine;
						}
					}
					
					

				// Rebuild
				
				else if (parameter == "--rebuild")
					{
					if (!commandLine.NoValue())
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedNoValue(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						Engine.Instance.Config.ReparseEverything = true;
						Engine.Instance.Config.RebuildAllOutput = true;
						}
					}
					
				else if (parameter == "--rebuild-output")
					{
					if (!commandLine.NoValue())
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedNoValue(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						Engine.Instance.Config.RebuildAllOutput = true;
						}
					}
					
					
					
				// Quiet

				else if (parameter == "--quiet")
					{
					if (!commandLine.NoValue())
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedNoValue(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						quiet = true;
						}
					}


				// Help
				
				else if (parameter == "--help")
					{
					Console.WriteLine( 
						Locale.Get("NaturalDocs.CLI", "CommandLine.SyntaxReference(version).multiline", NaturalDocs.Engine.Instance.VersionString) 
						);

					return ParseCommandLineResult.InformationalExit;
					}



				// Version
				
				else if (parameter == "--version")
					{
					Console.WriteLine( Engine.Instance.VersionString );
					return ParseCommandLineResult.InformationalExit;
					}



				// Charset

				else if (parameter == "--charset")
					{
					errorList.Add(
						Locale.Get("NaturalDocs.CLI", "CommandLine.NoLongerSupported(param)", parameterAsEntered)
						);

					commandLine.SkipToNextParameter();
					}
					
					
					
				// Headers only

				else if (parameter == "--headers-only")
					{
					errorList.Add(
						Locale.Get("NaturalDocs.CLI", "CommandLine.NoLongerSupported.HeadersOnly(param)", parameterAsEntered)
						);

					commandLine.SkipToNextParameter();
					}
					
					
					
				// Auto-group

				else if (parameter == "--auto-group")
					{
					errorList.Add(
						Locale.Get("NaturalDocs.CLI", "CommandLine.NoLongerSupported.AutoGroup(param)", parameterAsEntered)
						);

					commandLine.SkipToNextParameter();
					}
					
					
					
				// Everything else

				else
					{
					errorList.Add( 
						Locale.Get("NaturalDocs.CLI", "CommandLine.UnrecognizedParameter(param)", parameterAsEntered)
						);

					commandLine.SkipToNextParameter();
					}
				}
				
				
			// Validation
			
			if (!commandLineConfig.ProjectConfigFolderPropertyLocation.IsDefined)
				{
				errorList.Add( 
					Locale.Get("NaturalDocs.CLI", "CommandLine.NoProjectConfigFolder")
					);
				}				
				
				
			// Done.
				
			if (errorList.Count == originalErrorCount)
				{  return ParseCommandLineResult.OK;  }
			else
				{  return ParseCommandLineResult.Error;  }
			}
		
			

		/* Function: HandleException
		 */
		static public void HandleException (Exception e)
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
		static public void HandleErrorList (Engine.Errors.ErrorList errorList)
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


		/* Property: Quiet
		 * Whether the application should suppress all non-error output.
		 */
		static public bool Quiet
			{
			get
				{  return quiet;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: quiet
		 * Whether the application should suppress all non-error output.
		 */
		static private bool quiet;
		
		}
	}
