/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.Runner
 * ____________________________________________________________________________
 * 
 * A simple class to run NUnit tests from a Visual Studio console project which allows you to set breakpoints
 * in the tests and debug them.  You can pass a test or group of tests to run as a command line option,
 * such as "LinkScoring", or not pass anything to run all tests.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using System.Text.RegularExpressions;
using GregValure.NaturalDocs.Engine;

namespace GregValure.NaturalDocs.Engine.Tests
	{
	class Runner
		{
		static void Main (string[] commandLineOptions)
			{
			testGroup = null;
			pauseOnError = false;
			pauseBeforeExit = false;

			try
				{
				System.Console.WriteLine();
				System.Console.WriteLine("Natural Docs Engine Tests");
				System.Console.WriteLine("Version " + Engine.Instance.VersionString);
				System.Console.WriteLine("-------------------------");

				var assembly = System.Reflection.Assembly.GetExecutingAssembly();
				Path assemblyFolder = Path.FromAssembly(assembly).ParentFolder;
				Path dllPath = assemblyFolder + "/NaturalDocs.Engine.Tests.dll";

				if (!ParseCommandLine(commandLineOptions))
					{  return;  }

				string[] runnerParams;

				if (testGroup == null)
					{  
					System.Console.WriteLine("Running all tests...");
					runnerParams = new string[2] { "/work:" + assemblyFolder, dllPath };
					}
				else
					{
					System.Console.WriteLine("Running " + testGroup + " tests...");
					runnerParams = new string[3] { "/work:" + assemblyFolder, "/fixture:GregValure.NaturalDocs.Engine.Tests." + testGroup, dllPath };
					}


				// Use the NUnit console runner to execute the tests and capture the output.

				#if !USE_NUNIT_OUTPUT
					var oldConsoleOut = System.Console.Out;
					var capturedConsoleOut = new System.IO.StringWriter();
	
					System.Console.SetOut(capturedConsoleOut);
				#endif

				NUnit.ConsoleRunner.Runner.Main(runnerParams);

				#if !USE_NUNIT_OUTPUT
					System.Console.SetOut(oldConsoleOut);
				#endif


				// Attempt to extract the failure count and notices from the generated XML file.

				Path xmlPath = assemblyFolder + "/TestResult.xml";
				string xmlContent = System.IO.File.ReadAllText(xmlPath);

				// <failure>
				// <message><![CDATA[1 out of 1 test failed for F:\Projects\Natural Docs 2\Source\Engine.Tests.Data\Comments\XML\Parsing:
				// - Test: Expected output file missing
				// ]]></message>
				// <stack-trace><![CDATA[at GregValure.NaturalDocs.Engine.Tests.Framework.SourceToTopics.TestFolder(Path testFolder, Path projectConfigFolder) in F:\Projects\Natural Docs 2\Source\Engine.Tests\Source\Framework\SourceToTopics.cs:line 123
				// at GregValure.NaturalDocs.Engine.Tests.Comments.XML.Parsing.All() in F:\Projects\Natural Docs 2\Source\Engine.Tests\Source\Comments\XML\Parsing.cs:line 20
				// ]]></stack-trace>
				// </failure>

				int foundFailures = 0;
				StringBuilder failures = new StringBuilder();
				MatchCollection failureMatches = Regex.Matches(xmlContent, @"<failure>.*?<message><!\[CDATA\[(.*?)\]\]>.*?</message>.*?</failure>", RegexOptions.Singleline);

				foreach (Match failureMatch in failureMatches)
					{  
					failures.AppendLine(failureMatch.Groups[1].ToString());
					foundFailures++;
					}

				//<test-results 
				//		name="F:\Projects\Natural Docs 2\Source\Engine.Tests.Runner\bin\Debug\NaturalDocs.Engine.Tests.dll"
				//		total="1" errors="0" failures="0" not-run="0" inconclusive="0" ignored="0" skipped="0" invalid="0"
				//		date="2012-03-06" time="11:02:19"
				//>
				Match failureCountMatch = System.Text.RegularExpressions.Regex.Match(xmlContent, @"<test-results.+errors=\""([0-9]+)\"".+failures=\""([0-9]+)\"".*>");
				int failureCount = -1;

				if (failureCountMatch.Success)
					{
					failureCount = int.Parse(failureCountMatch.Groups[1].Value) + int.Parse(failureCountMatch.Groups[2].Value);
					}


				// Display the output, falling back to the captured console output if our XML extraction didn't work.

				if (failureCount == -1 || failureCount != foundFailures)
					{  
					#if !USE_NUNIT_OUTPUT
						System.Console.WriteLine();
						System.Console.Write(capturedConsoleOut.ToString());
					#endif

					if (pauseOnError)
						{  pauseBeforeExit = true;  }
					}
				else if (failureCount != 0)
					{
					#if !USE_NUNIT_OUTPUT
						System.Console.WriteLine();
						System.Console.Write(failures.ToString());
					#endif

					if (pauseOnError)
						{  pauseBeforeExit = true;  }
					}
				else
					{
					#if !USE_NUNIT_OUTPUT
						System.Console.WriteLine("All tests successful.");
					#endif
					}

				System.Console.WriteLine("-------------------------");
				}
			catch (Exception e)
				{
				if (pauseOnError)
					{  pauseBeforeExit = true;  }

				System.Console.WriteLine("-------------------------");
				System.Console.WriteLine();
				System.Console.WriteLine("Exception: " + e.Message);
				}

			if (pauseBeforeExit)
				{
				System.Console.WriteLine();
				System.Console.WriteLine("Press any key to continue...");
				System.Console.ReadKey(false);
				}
			}


		private static bool ParseCommandLine (string[] commandLineSegments)
			{
			Engine.CommandLine commandLine = new CommandLine(commandLineSegments);

			commandLine.AddAliases("--test-group", "--testgroup", "--test", "--tests");
			commandLine.AddAliases("--pause-before-exit", "--pausebeforexit", "--pause");
			commandLine.AddAliases("--pause-on-error", "--pauseonerror");
			commandLine.AddAliases("--help", "-h", "-?");
			
			string parameter, parameterAsEntered;
			bool isFirst = true;
				
			while (commandLine.IsInBounds)
				{
				// If the first segment isn't a parameter, it's the test group
				if (isFirst && !commandLine.IsOnParameter)
					{
					parameter = "--test-group";
					parameterAsEntered = parameter;
					}
				else
					{  
					if (!commandLine.GetParameter(out parameter, out parameterAsEntered))
						{
						System.Console.Error.WriteLine("Unrecognized parameter \"" + parameterAsEntered + "\"");
						return false;
						}
					}

				isFirst = false;

					
				// Test Group

				if (parameter == "--test-group")
					{
					if (!commandLine.GetBareWord(out testGroup))
						{
						System.Console.Error.WriteLine("\"" + parameterAsEntered + "\" must be followed by a test group name");
						return false;
						}
					}


				// Pause Before Exit

				else if (parameter == "--pause-before-exit")
					{
					if (!commandLine.NoValue())
						{
						System.Console.Error.WriteLine("\"" + parameterAsEntered + "\" can't be followed by a value");
						return false;
						}
					else
						{
						pauseBeforeExit = true;
						}
					}


				// Pause On Error

				else if (parameter == "--pause-on-error")
					{
					if (!commandLine.NoValue())
						{
						System.Console.Error.WriteLine("\"" + parameterAsEntered + "\" can't be followed by a value");
						return false;
						}
					else
						{
						pauseOnError = true;
						}
					}


				// Help
				
				else if (parameter == "--help")
					{
					System.Console.WriteLine("TestRunner [group].  If no group is specified all tests will be run.");
					return false;
					}


				// Everything else

				else
					{
					System.Console.Error.WriteLine("Unrecognized parameter \"" + parameterAsEntered + "\"");
					return false;
					}
				}
				
				
			// Done.

			return true;				
			}



		// Group: Variables
		// __________________________________________________________________________

		private static string testGroup;

		private static bool pauseBeforeExit;
		private static bool pauseOnError;

		}
	}
