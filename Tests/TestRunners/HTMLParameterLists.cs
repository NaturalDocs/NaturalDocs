/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.HTMLParameterLists
 * ____________________________________________________________________________
 *
 * Tests to make sure Natural Docs can generate HTML parameter lists correctly.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class HTMLParameterLists : TestRunners.HTML
		{

		protected override string RunTest (string input)
			{
			return ExtractHTML(input,
										tagName: "div",
										className: "CBody");
			}

		}
	}
