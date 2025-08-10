/*
 * Class: CodeClear.NaturalDocs.Tests.TestFolderConfig
 * ____________________________________________________________________________
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine;


namespace CodeClear.NaturalDocs.Tests
	{
	public partial class TestFolderConfig
		{

		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: GetTestRunner
		 * Creates a <TestRunner> object for the passed test type, as found in <Test Folder.txt>.  Returns null if it couldn't find one.
		 */
		static public TestRunner GetTestRunner (string testType)
			{
			string lcTestType = testType.ToLowerInvariant();

			switch (lcTestType)
				{
				case "numberset":
					return new TestRunners.NumberSet();

				default:
					return null;
				}
			}

		}
	}
