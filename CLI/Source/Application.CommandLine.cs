/*
 * Class: CodeClear.NaturalDocs.CLI.Application
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Globalization;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Config;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.CLI
	{
	public static partial class Application
		{

		/* enum: ParseCommandLineResult
		 *
		 * The result returned from <ParseCommandLine()>.
		 *
		 * Run - The command line was OK and Natural Docs should run normally.
		 * Error - There was an error on the command line.
		 * ShowCommandLineReference - The user asked for the command line reference to be displayed.
		 * ShowVersion - The user asked for the version number to be displayed.
		 * ShowAllVersions - The user asked for the version number of Natural Docs and all supporting systems like .NET and Mono to be displayed.
		 * ShowEncodings - The user asked for all the supported character encodings to be displayed.
		 */
		public enum ParseCommandLineResult : byte
			{  Run, Error, ShowCommandLineReference, ShowVersion, ShowAllVersions, ShowEncodings  };


		/* Function: ParseCommandLine
		 *
		 * Parses the command line and applies the relevant settings in in <NaturalDocs.Engine.Instance's> modules.  If there were
		 * errors they will be placed on errorList and it will return <ParseCommandLineResult.Error>.
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
		 *		- -e, --encoding, --default-encoding
		 *			- -cs, --char-set, --charset, --character-set, --characterset
		 *		- -t, --tab, --tab-width, --tab-length
		 *		- -do, --documented-only
		 *		- -nag, --no-auto-group
		 *		- -s, --style
		 *		- -r, --rebuild
		 *		- -ro, --rebuild-output
		 *		- -q, --quiet
		 *		- -v, --version
		 *		- -vs, --versions, --all-versions
		 *		- --encodings, --show-encodings, --list-encodings, --show-charsets, --list-charsets
		 *		- --benchmark
		 *		- --worker-threads, --threads
		 *		- --pause-before-exit, --pause
		 *		- --pause-on-error
		 *		- --dont-shrink-files
		 *		- -h, --help
		 *		- -?
		 *
		 * No Longer Supported:
		 *
		 *		- -ho, --headers-only, --headersonly
		 *		- -ag, --auto-group, --autogroup
		 */
		private static ParseCommandLineResult ParseCommandLine (string[] commandLineSegments, out ProjectConfig commandLineConfig, ErrorList errorList)
			{
			int originalErrorCount = errorList.Count;
			ParseCommandLineResult result = ParseCommandLineResult.Run;

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
			commandLine.AddAliases("--style", "-s");
			commandLine.AddAliases("--rebuild", "-r");
			commandLine.AddAliases("--rebuild-output", "-ro", "--rebuildoutput");
			commandLine.AddAliases("--quiet", "-q");
			commandLine.AddAliases("--version", "-v");
			commandLine.AddAliases("--all-versions", "-vs", "--versions", "--allversions");
			commandLine.AddAliases("--encoding", "-e", "-cs", "--char-set", "--charset", "--character-set", "--characterset");
			commandLine.AddAliases("--encodings", "--show-encodings", "--showencodings",
																		"--list-encodings", "--listencodings",
																		"--show-char-set", "--show-charset", "--showcharset",
																		"--show-char-sets", "--show-charsets", "--showcharsets",
																		"--list-char-set", "--list-charset", "--listcharset",
																		"--list-char-sets", "--list-charsets", "--listcharsets",
																		"--char-sets", "--charsets");
			commandLine.AddAliases("--pause-before-exit", "--pausebeforexit", "--pause");
			commandLine.AddAliases("--pause-on-error", "--pauseonerror");
			commandLine.AddAliases("--dont-shrink-files", "--dontshrinkfiles", "--dont-shrink-output", "--dontshrinkoutput", "--dont-shrink", "--dontshrink");
			commandLine.AddAliases("--worker-threads", "--threads");
			// no aliases for --benchmark
			commandLine.AddAliases("--help", "-h", "-?");

			// No longer supported
			commandLine.AddAliases("--headers-only", "-ho", "--headersonly");
			commandLine.AddAliases("--auto-group", "-ag", "--autogroup");

			commandLineConfig = new ProjectConfig(PropertySource.CommandLine);

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
				else if (!commandLine.GetParameter(out parameter, out parameterAsEntered))
					{
					string bareWord;
					commandLine.GetBareWord(out bareWord);

					errorList.Add(
						Locale.Get("NaturalDocs.CLI", "CommandLine.UnrecognizedParameter(param)", bareWord)
						);

					commandLine.SkipToNextParameter();
					continue;
					}



				// Source folders

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
						var target = new Engine.Config.Targets.SourceFolder(PropertySource.CommandLine);

						if (folder.IsRelative)
							{  folder = System.Environment.CurrentDirectory + "/" + folder;  }

						target.Folder = (AbsolutePath)folder;
						target.FolderPropertyLocation = PropertySource.CommandLine;

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
						format = format.ToLower(CultureInfo.InvariantCulture);

						if (folder.IsRelative)
							{  folder = System.Environment.CurrentDirectory + "/" + folder;  }

						if (format == "html" || format == "framedhtml")
							{
							var target = new Engine.Config.Targets.HTMLOutputFolder(PropertySource.CommandLine);

							target.Folder = (AbsolutePath)folder;
							target.FolderPropertyLocation = PropertySource.CommandLine;

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
						if (folder.NameWithoutPath.ToLower(CultureInfo.InvariantCulture) == "project.txt")
							{  folder = folder.ParentFolder;  }

						if (commandLineConfig.ProjectConfigFolderPropertyLocation.IsDefined)
							{
							errorList.Add(
								Locale.Get("NaturalDocs.CLI", "CommandLine.OnlyOneProjectConfigFolder")
								);
							}
						else
							{
							commandLineConfig.ProjectConfigFolder = (AbsolutePath)folder;
							commandLineConfig.ProjectConfigFolderPropertyLocation = PropertySource.CommandLine;
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
							commandLineConfig.WorkingDataFolder = (AbsolutePath)folder;
							commandLineConfig.WorkingDataFolderPropertyLocation = PropertySource.CommandLine;
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
						var target = new Engine.Config.Targets.IgnoredSourceFolder(PropertySource.CommandLine);

						if (folder.IsRelative)
							{  folder = System.Environment.CurrentDirectory + "/" + folder;  }

						target.Folder = (AbsolutePath)folder;
						target.FolderPropertyLocation = PropertySource.CommandLine;

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
						var target =  new Engine.Config.Targets.IgnoredSourceFolderPattern(PropertySource.CommandLine);

						target.Pattern = pattern;
						target.PatternPropertyLocation = PropertySource.CommandLine;

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
						var target = new Engine.Config.Targets.ImageFolder(PropertySource.CommandLine);

						if (folder.IsRelative)
							{  folder = System.Environment.CurrentDirectory + "/" + folder;  }

						target.Folder = (AbsolutePath)folder;
						target.FolderPropertyLocation = PropertySource.CommandLine;

						commandLineConfig.InputTargets.Add(target);
						}
					}



				// Encoding

				else if (parameter == "--encoding")
					{
					string encoding;

					if (!commandLine.GetBareOrQuotedWordsValue(out encoding))
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedEncodingName(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						string encodingName;
						int codePage;

						// None of the valid encoding names start with a number, but use TryParse just in case
						if (encoding[0] >= '0' && encoding[0] <= '9' &&
							Int32.TryParse(encoding, out codePage))
							{
							// codePage set by TryParse
							encodingName = null;
							}
						else
							{
							codePage = 0;
							encodingName = encoding;
							}

						commandLineConfig.InputSettings.AddCharacterEncodingRule(
							new CharacterEncodingRule(codePage, encodingName, null, null, PropertySource.CommandLine)
							);
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
						commandLineConfig.TabWidthPropertyLocation = PropertySource.CommandLine;
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
						commandLineConfig.TabWidthPropertyLocation = PropertySource.CommandLine;
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
						commandLineConfig.DocumentedOnlyPropertyLocation = PropertySource.CommandLine;
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
						commandLineConfig.AutoGroupPropertyLocation = PropertySource.CommandLine;
						}
					}



				// Don't Shrink Files

				else if (parameter == "--dont-shrink-files")
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
						commandLineConfig.ShrinkFiles = false;
						commandLineConfig.ShrinkFilesPropertyLocation = PropertySource.CommandLine;
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
						commandLineConfig.OutputSettings.StyleName = styleName;
						commandLineConfig.OutputSettings.StyleNamePropertyLocation = PropertySource.CommandLine;
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
						EngineInstance.Config.UserWantsEverythingRebuilt = true;
						}
					}



				// Rebuild Output

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
						EngineInstance.Config.UserWantsOutputRebuilt = true;
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



				// Worker Threads

				else if (parameter == "--worker-threads")
					{
					int value;

					if (!commandLine.GetIntegerValue(out value))
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.ExpectedNumber(param)", parameterAsEntered)
							);

						commandLine.SkipToNextParameter();
						}
					else if (value < 1)
						{
						errorList.Add(
							Locale.Get("NaturalDocs.CLI", "CommandLine.InvalidWorkerThreadCount")
							);

						commandLine.SkipToNextParameter();
						}
					else
						{
						workerThreadCount = value;
						}
					}



				// Benchmark

				else if (parameter == "--benchmark")
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
						benchmark = true;
						}
					}



				// Pause Before Exit

				else if (parameter == "--pause-before-exit")
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
						pauseBeforeExit = true;
						}
					}



				// Pause on Error

				else if (parameter == "--pause-on-error")
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
						pauseOnError = true;
						}
					}



				// Help

				else if (parameter == "--help")
					{
					result = ParseCommandLineResult.ShowCommandLineReference;
					}



				// Version

				else if (parameter == "--version")
					{
					result = ParseCommandLineResult.ShowVersion;
					}



				// All Versions

				else if (parameter == "--all-versions")
					{
					result = ParseCommandLineResult.ShowAllVersions;
					}



				// Encodings

				else if (parameter == "--encodings")
					{
					result = ParseCommandLineResult.ShowEncodings;
					}



				// No longer supported parameters

				else if (parameter == "--headers-only" ||
						  parameter == "--auto-group")
					{
					errorList.Add(
						Locale.Get("NaturalDocs.CLI", "CommandLine.NoLongerSupported(param)", parameterAsEntered)
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



				isFirst = false;
				}


			// Validation

			if (result == ParseCommandLineResult.Run &&
				!commandLineConfig.ProjectConfigFolderPropertyLocation.IsDefined)
				{
				errorList.Add(
					Locale.Get("NaturalDocs.CLI", "CommandLine.NoProjectConfigFolder")
					);
				}


			// Done.

			if (errorList.Count != originalErrorCount)
				{  result = ParseCommandLineResult.Error;  }

			return result;
			}

		}
	}
