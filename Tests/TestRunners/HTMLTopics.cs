/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.HTMLTopics
 * ____________________________________________________________________________
 *
 * Tests to make sure Natural Docs can generate HTML topics correctly.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class HTMLTopics : TestRunners.HTML
		{

		protected override string RunTest (string input)
			{
			return ExtractHTML(input,
										tagName: "div",
										className: "CTopic",
										reformatHTML: true);
			}

		}
	}
