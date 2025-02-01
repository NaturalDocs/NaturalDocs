
// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using NUnit.Framework;
using CodeClear.NaturalDocs.Engine.Tests.Framework;


namespace CodeClear.NaturalDocs.Engine.Tests.Output.HTML.SyntaxHighlighting
	{
	[TestFixture]
	public class PowerBuilder : Framework.TestTypes.HTML
		{

		[Test]
		public void All ()
			{
			TestFolder("Output/HTML/Syntax Highlighting/PowerBuilder", "Shared ND Config/HTML Structure",

							tagName: "pre",

							outputTitle: "PowerBuilder Syntax Highlighting Tests",
							outputStyle: "Prominent Syntax Highlighting"
							);
			}

		}
	}
