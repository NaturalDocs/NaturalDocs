/*
 * Class: CodeClear.NaturalDocs.Tests.CLI.Application
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Tests.CLI
	{
	public static partial class Application
		{

		/* enum: ParseCommandLineResult
		 *
		 * The result returned from <ParseCommandLine()>.
		 *
		 * Run - The command line was OK and Natural Docs should run normally.
		 * Error - There was an error on the command line.
		 */
		public enum ParseCommandLineResult : byte
			{  Run, Error  };


		/* Function: ParseCommandLine
		 *
		 * Parses the command line and applies the relevant settings in in <NaturalDocs.Engine.Instance's> modules.  If there were
		 * errors they will be placed on errorList and it will return <ParseCommandLineResult.Error>.
		 *
		 * Supported:
		 *
		 *		- -t, --test, --test-folder
		 *		- --pause-before-exit, --pause
		 *		- --pause-on-error
		 */
		private static ParseCommandLineResult ParseCommandLine (string[] commandLineSegments, ErrorList errorList)
			{
			int originalErrorCount = errorList.Count;
			ParseCommandLineResult result = ParseCommandLineResult.Run;

			Engine.CommandLine commandLine = new CommandLine(commandLineSegments);

			commandLine.AddAliases("--test", "-t", "--test-folder", "--testfolder");
			commandLine.AddAliases("--pause-before-exit", "--pausebeforexit", "--pause");
			commandLine.AddAliases("--pause-on-error", "--pauseonerror");

			string parameter, parameterAsEntered;
			bool isFirst = true;

			while (commandLine.IsInBounds)
				{
				// If the first segment isn't a parameter, assume it's the test folder specified without -t.
				if (isFirst && !commandLine.IsOnParameter)
					{
					parameter = "--test";
					parameterAsEntered = parameter;
					}
				else if (!commandLine.GetParameter(out parameter, out parameterAsEntered))
					{
					string bareWord;
					commandLine.GetBareWord(out bareWord);

					errorList.Add("Unrecognized parameter: " + parameterAsEntered);

					commandLine.SkipToNextParameter();
					continue;
					}


				// Test folder

				if (parameter == "--test")
					{
					Path testFolderAsEntered;

					if (!commandLine.GetPathValue(out testFolderAsEntered))
						{
						errorList.Add("Expected a folder: " + parameterAsEntered);
						commandLine.SkipToNextParameter();
						}
					else
						{
						if (testFolderAsEntered.IsAbsolute)
							{  testFolderPath = (AbsolutePath)testFolderAsEntered;  }
						else
							{  testFolderPath = (AbsolutePath)(System.Environment.CurrentDirectory + "/" + testFolderAsEntered);  }

						if (!System.IO.Directory.Exists(testFolderPath))
							{
							errorList.Add("Cannot find folder: " + testFolderPath);
							commandLine.SkipToNextParameter();
							}
						}
					}


				// Pause Before Exit

				else if (parameter == "--pause-before-exit")
					{
					if (!commandLine.NoValue())
						{
						errorList.Add("There should be no value after " + parameterAsEntered);
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
						errorList.Add("There should be no value after " + parameterAsEntered);
						commandLine.SkipToNextParameter();
						}
					else
						{
						pauseOnError = true;
						}
					}


				// Everything else

				else
					{
					errorList.Add("Unrecognized parameter: " + parameterAsEntered);
					commandLine.SkipToNextParameter();
					}


				isFirst = false;
				}


			// Done.

			if (errorList.Count != originalErrorCount)
				{  result = ParseCommandLineResult.Error;  }

			return result;
			}

		}
	}
