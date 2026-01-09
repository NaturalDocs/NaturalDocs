/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.HTMLCodeSections
 * ____________________________________________________________________________
 *
 * Tests to make sure Natural Docs can generate HTML code sections correctly.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class HTMLCodeSections : TestRunners.HTML
		{

		protected override string RunTest (string input)
			{
			return ExtractHTML(input,
										tagName: "pre");
			}

		}
	}
