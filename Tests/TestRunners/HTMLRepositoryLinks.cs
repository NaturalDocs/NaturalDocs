/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.HTMLRepositoryLinks
 * ____________________________________________________________________________
 *
 * Tests to make sure Natural Docs can generate HTML repository links correctly.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class HTMLRepositoryLinks : TestRunners.HTML
		{

		public HTMLRepositoryLinks () : base (InputMode.ClassHTML)
			{  }

		protected override string RunTest (string input)
			{
			return ExtractHTML(input,
										tagName: "div",
										className: "PRepository",
										reformatHTML: true);
			}

		}
	}
