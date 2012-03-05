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
			try
				{
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
				}
			catch (Exception e)
				{
				System.Console.WriteLine("Exception: " + e.Message);
				}

			System.Console.WriteLine();
			System.Console.WriteLine("Press any key to continue...");
			System.Console.ReadKey(false);
			}
		}
	}
