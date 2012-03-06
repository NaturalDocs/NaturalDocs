/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.Runner
 * ____________________________________________________________________________
 * 
 * A simple class to run NUnit tests from a Visual Studio console project which allows you to set breakpoints
 * in the tests and debug them.  You can pass a test or group of tests to run as a command line option,
 * such as "LinkScoring", or not pass anything to run all tests.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine;

namespace GregValure.NaturalDocs.Engine.Tests
	{
	class Runner
		{
		static void Main (string[] commandLineOptions)
			{
			#if PAUSE_BEFORE_EXIT
				bool pauseBeforeExit = true;
			#elif PAUSE_ON_ERROR
				bool pauseBeforeExit = false;
			#endif

			try
				{
				System.Console.WriteLine();
				System.Console.WriteLine("Natural Docs Engine Tests");
				System.Console.WriteLine("Version " + Engine.Instance.VersionString);
				System.Console.WriteLine("-------------------------");
				System.Console.WriteLine();

				Path assemblyPath = Path.GetExecutingAssembly();
				Path dllPath = assemblyPath.ParentFolder + "/NaturalDocs.Engine.Tests.dll";

				string[] runnerParams;

				if (commandLineOptions.Length == 0)
					{  
					runnerParams = new string[1] { dllPath };
					}
				else
					{
					string testParam = "/fixture:GregValure.NaturalDocs.Engine.Tests." + commandLineOptions[0];
					runnerParams = new string[2] { testParam, dllPath };
					}

				NUnit.ConsoleRunner.Runner.Main(runnerParams);

				System.Console.WriteLine("-------------------------");
				}
			catch (Exception e)
				{
				#if PAUSE_ON_ERROR
					pauseBeforeExit = true;
				#endif

				System.Console.WriteLine("-------------------------");
				System.Console.WriteLine();
				System.Console.WriteLine("Exception: " + e.Message);
				}

			#if PAUSE_BEFORE_EXIT || PAUSE_ON_ERROR
				if (pauseBeforeExit)
					{
					System.Console.WriteLine();
					System.Console.WriteLine("Press any key to continue...");
					System.Console.ReadKey(false);
					}
			#endif
			}
		}
	}
