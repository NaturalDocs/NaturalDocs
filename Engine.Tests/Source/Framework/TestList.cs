/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.Framework.TestList
 * ____________________________________________________________________________
 * 
 * A class storing information about a list of file-based tests.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine;


namespace GregValure.NaturalDocs.Engine.Tests.Framework
	{
	public class TestList : List<Test>
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: BuildFailureMessage
		 * If there are any failures in the list of tests, generates a string identifying which ones.  If there was
		 * no failure, returns null.
		 */
		public string BuildFailureMessage ()
			{
			StringBuilder failedTests = new StringBuilder();
			int failureCount = 0;
			Path failedTestFolder;

			foreach (var test in this)
				{
				if (test.Passed == false)
					{  
					failedTests.Append(" - " + test.Name);

					if (test.TestResult != Test.TestResults.DoesNotMatchExpectedOutput)
						{  failedTests.Append(": " + test.TestResultExplanation);  }

					failedTests.Append("\n");
					failureCount++;
					failedTestFolder = test.ExpectedOutputFile.ParentFolder;
					}
				}

			if (failureCount == 0)
				{  return null;  }
			else
				{
				return failureCount.ToString() + " out of " + Count + " test" + (Count == 1 ? "" : "s") + 
							" failed for " + failedTestFolder + ":\n" + failedTests.ToString();
				}
			}



		// Group: Properties
		// _________________________________________________________________________


		/* Property: Passed
		 * Whether all tests on the list succeeded.
		 */
		public bool Passed
			{
			get
				{
				foreach (var test in this)
					{
					if (test.Passed == false)
						{  return false;  }
					}

				return true;
				}
			}

		}
	}