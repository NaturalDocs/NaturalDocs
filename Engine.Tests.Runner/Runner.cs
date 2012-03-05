/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.Runner
 * ____________________________________________________________________________
 * 
 * A simple class to run NUnit tests from a Visual Studio console project which allows you to set breakpoints
 * in the tests and debug them.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;

namespace GregValure.NaturalDocs.Engine.Tests
	{
	class Runner
		{
		static void Main (string[] args)
			{
			NUnit.ConsoleRunner.Runner.Main(args);

			System.Console.WriteLine();
			System.Console.WriteLine("Press any key to continue...");
			System.Console.ReadKey(false);
			}
		}
	}
