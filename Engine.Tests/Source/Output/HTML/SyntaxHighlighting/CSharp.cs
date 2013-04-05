
// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using NUnit.Framework;
using GregValure.NaturalDocs.Engine.Tests.Framework;


namespace GregValure.NaturalDocs.Engine.Tests.Output.HTML.SyntaxHighlighting
	{
	[TestFixture]
	public class CSharp : Framework.SourceToHTML
		{

		[Test]
		public void All ()
			{
			TestFolder("Output/HTML/Syntax Highlighting/C#", null, "pre",
							outputTitle: "Syntax Highlighting Tests", outputSubTitle: "C#");
			}

		}
	}