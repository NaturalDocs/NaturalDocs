/*
 * Class: CodeClear.NaturalDocs.Tests.CLI.Application
 * ____________________________________________________________________________
 *
 * The main application class for the command line interface to the Natural Docs engine tests.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Tests.CLI
	{
	public static partial class Application
		{

		// Group: Types
		// __________________________________________________________________________


		private enum ConsoleColor
			{  Default, TestSucceeded, TestFailed  }



		// Group: Functions
		// __________________________________________________________________________


		static Application ()
			{
			dashedLineLength = 15;
			pauseOnError = false;
			pauseBeforeExit = false;
			}


		/* Function: Main
		 * The program entry point.
		 */
		public static void Main (string[] commandLine)
			{
			var errorList = new ErrorList ();

			try
				{
				var parseCommandLineResult = ParseCommandLine(commandLine, errorList);

				if (parseCommandLineResult == ParseCommandLineResult.Error)
					{
					WriteErrorList(errorList);
					WriteLine();
					}

				else if (parseCommandLineResult == ParseCommandLineResult.Run)
					{
					RunTests();
					}

				else
					{
					throw new NotImplementedException();
					}
				}

			catch (Exception e)
				{
				WriteException(e);
				WriteLine();
				}

			if (pauseBeforeExit || (pauseOnError && errorList.Count > 0))
				{
				WriteLine();
				WriteLine("Press any key to continue...");
				System.Console.ReadKey(true);
				}
			}


		private static bool RunTests ()
			{
			WriteLine();
			WriteConsoleHeader();
			WriteDashedLine();
			WriteLine();

			List<TestFolder> testFolders;
			ErrorList errorList = new ErrorList ();
			bool success = true;
			bool lastFolderSucceeded = true;

			if (GetTestFolders(out testFolders, errorList))
				{
				foreach (var testFolder in testFolders)
					{
					lastFolderSucceeded = RunTestsIn(testFolder);

					if (!lastFolderSucceeded)
						{
						// Space out failure messages since they're multi-line
						WriteLine();

						success = false;
						// Continue running the rest of the tests though
						}
					}
				}
			else
				{
				WriteErrorList(errorList);
				success = false;
				}

			// There will already be an extra line if the last folder failed
			if (lastFolderSucceeded)
				{  WriteLine();  }

			WriteDashedLine();

			if (success)
				{  WriteLine("All tests successful.");  }
			else
				{  WriteLine("There were failures.");  }

			WriteLine();

			return success;
			}


		private static bool RunTestsIn (TestFolder testFolder)
			{
			Write("  " + PrettifyTestFolderPath(testFolder.Path) + "... ");


			// Get the test runner

			TestRunner testRunner = null;

			if (!testFolder.Failed)
				{
				testRunner = TestFolderConfig.GetTestRunner(testFolder.Config.TestType);

				if (testRunner == null)
					{  testFolder.MarkAsFailed("Unrecognized test type \"" + testFolder.Config.TestType + "\".");  }
				}


			// Run the tests

			if (!testFolder.Failed)
				{
				// We don't care about the return value.  It will mark the test folder as passed or failed itself.
				testRunner.RunAll(testFolder);
				}


			// Display the results

			if (testFolder.Passed)
				{
				WriteLine("Passed", ConsoleColor.TestSucceeded);
				}
			else
				{
				WriteLine("Failed", ConsoleColor.TestFailed);
				WriteErrorLine("    " + testFolder.FailureMessage);

				var tests = testFolder.Tests;

				if (tests != null)
					{
					foreach (var test in tests)
						{
						if (test.Failed)
							{
							WriteError("    - " + test.Name);

							if (test.FailureReason == Test.FailureReasons.ExpectedOutputMissing)
								{  WriteError(" (Missing expected output)");  }
							else if (test.FailureReason == Test.FailureReasons.ExceptionThrown)
								{  WriteError(" (Exception)");  }
							// Other reasons don't need added text

							WriteErrorLine();
							}
						}
					}
				}

			return testFolder.Passed;
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: GetTestFolders
		 *
		 * Retrieves a list of <TestFolders> from <testFolderPath> and all its subfolders.  Returns whether it succeeded.  Any errors will be
		 * added to the <ErrorList>.
		 */
		private static bool GetTestFolders (out List<TestFolder> testFolders, ErrorList errorList)
			{
			testFolders = new List<TestFolder>();
			int initialErrorCount = errorList.Count;

			GetTestFolders_Recurse(testFolderPath, ref testFolders, errorList);

			if (errorList.Count == initialErrorCount &&
				testFolders.Count == 0)
				{  errorList.Add("There were no tests in " + testFolderPath + " or any of its subfolders.  Do they have a \"Test Folder.txt\" file?");  }

			if (errorList.Count > initialErrorCount)
				{
				testFolders = null;
				return false;
				}
			else
				{  return true;  }
			}


		/* Function: GetTestFolders_Recurse
		 *
		 * Retrieves a list of <TestFolders> from the passed folder and all its subfolders.  Any errors will be added to the <ErrorList>.
		 *
		 * This is a helper function to <GetTestFolders()>.  Most code should call that instead.
		 */
		private static void GetTestFolders_Recurse (AbsolutePath path, ref List<TestFolder> testFolders, ErrorList errorList)
			{

			// Evaluate the passed folder first

			if (TestFolder.IsTestFolder(path, out AbsolutePath configFilePath))
				{
				// If Load() fails reasons why will already be on the error list.  We don't have to add any.
				if (TestFolderConfig.Load(configFilePath, out TestFolderConfig config, errorList))
					{
					testFolders.Add( new TestFolder(path, config) );
					}
				}


			// Check subfolders, even if the current path isn't a valid test folder

			string[] subfolders = System.IO.Directory.GetDirectories(path);

			foreach (AbsolutePath subfolder in subfolders)
				{
				// Filter out HTML output folders though to make the search faster
				if (TestFolder.IsHTMLOutputFolder(subfolder))
					{  continue;  }

				// Recursing this way will create a depth-first search, which looks nicer in the output
				GetTestFolders_Recurse(subfolder, ref testFolders, errorList);
				}
			}


		/* Function: PrettifyTestFolderPath
		 * Returns a nicely-formatted version of the test folder path, assuming it's a subfolder of a "Tests.Data" folder.  If it is not it will
		 * return the original path unaltered.
		 */
		private static string PrettifyTestFolderPath (AbsolutePath testFolderPath)
			{
			string pathString = testFolderPath.ToString();
			int testDataIndex = pathString.IndexOf("Tests.Data");

			if (testDataIndex != -1 &&
				pathString[testDataIndex - 1] == Engine.SystemInfo.PathSeparatorCharacter &&
				pathString[testDataIndex + 10] == Engine.SystemInfo.PathSeparatorCharacter)
				{
				pathString = pathString.Substring(testDataIndex + 11);
				pathString = pathString.Replace(Engine.SystemInfo.PathSeparatorCharacter.ToString(), " > ");
				}

			return pathString;
			}



		// Group: Output Functions
		// __________________________________________________________________________


		private static void Write (string text, ConsoleColor color = ConsoleColor.Default)
			{
			SetConsoleColor(color);
			System.Console.Write(text);
			}


		private static void WriteLine ()
			{
			System.Console.WriteLine();
			}


		private static void WriteLine (string text, ConsoleColor color = ConsoleColor.Default)
			{
			SetConsoleColor(color);
			System.Console.WriteLine(text);
			}


		private static void WriteError (string text, ConsoleColor color = ConsoleColor.Default)
			{
			SetConsoleColor(color);
			System.Console.Error.Write(text);
			}


		private static void WriteErrorLine ()
			{
			System.Console.Error.WriteLine();
			}


		private static void WriteErrorLine (string text, ConsoleColor color = ConsoleColor.Default)
			{
			SetConsoleColor(color);
			System.Console.Error.WriteLine(text);
			}


		private static void WriteConsoleHeader ()
			{
			string headerLine1 = "Natural Docs Engine Tests";
			string headerLine2 = "Version " + Engine.Instance.VersionString;

			WriteLine(headerLine1);
			WriteLine(headerLine2);

			if (headerLine1.Length > dashedLineLength)
				{  dashedLineLength = headerLine1.Length;  }
			if (headerLine2.Length > dashedLineLength)
				{  dashedLineLength = headerLine2.Length;	}
			}


		private static void WriteDashedLine ()
			{
			StringBuilder dashedLineBuilder = new StringBuilder(dashedLineLength);
			dashedLineBuilder.Append('-', dashedLineLength);
			string dashedLine = dashedLineBuilder.ToString();

			WriteLine(dashedLine);
			}


		static private void WriteException (Exception e)
			{
			WriteDashedLine();
			WriteLine();

			WriteErrorLine ("Natural Docs has closed because of the following error:");
			WriteErrorLine();
			WriteErrorLine(e.Message);

			if (e is not Engine.Exceptions.UserFriendly)
				{
				WriteErrorLine("(" + e.GetType() + ")");
				WriteErrorLine();
				WriteErrorLine(e.StackTrace);
				}
			}


		static private void WriteErrorList (ErrorList errorList)
			{
			WriteDashedLine();
			WriteLine();
			WriteErrorLine ("Natural Docs has closed because of the following error" + (errorList.Count != 1 ? "s" : "") + ":");
			WriteErrorLine();

			Path lastErrorFile = null;

			for (int i = 0; i < errorList.Count; i++)
				{
				var error = errorList[i];

				if (i > 0 &&
					(error.File == null || error.File != lastErrorFile))
					{
					WriteErrorLine();
					}

				if (error.File != lastErrorFile)
					{
					if (error.File != null)
						{  WriteErrorLine( "There are errors in " + error.File + ":");  }

					lastErrorFile = error.File;
					}

				if (error.File != null)
					{
					WriteError(" - ");

					if (error.LineNumber > 0)
						{  WriteError("Line " + error.LineNumber + ": " );  }
					}

				WriteErrorLine(error.Message);
				}
			}


		static private void SetConsoleColor (ConsoleColor color)
			{
			switch (color)
				{
				case ConsoleColor.Default:
					System.Console.ResetColor();
					break;

				case ConsoleColor.TestSucceeded:
					System.Console.ForegroundColor = System.ConsoleColor.DarkGreen;
					break;

				case ConsoleColor.TestFailed:
					System.Console.ForegroundColor = System.ConsoleColor.Red;
					break;

				default:
					throw new NotImplementedException();
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: testFolderPath
		 * The folder of files being tested, plus all its subfolders.
		 */
		static private AbsolutePath testFolderPath;

		/* var: dashedLineLength
		 * The number of dashes to include in horizontal lines in the output.
		 */
		static private int dashedLineLength;

		static private bool pauseOnError;
		static private bool pauseBeforeExit;

		}
	}
