﻿
// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using NUnit.Framework;
using CodeClear.NaturalDocs.Engine.Tests.Framework;


namespace CodeClear.NaturalDocs.Engine.Tests.Output.HTML
	{
	[TestFixture]
	public class CodeSections : Framework.TestTypes.HTML
		{

		[Test]
		public void All ()
			{
			TestFolder("Output/HTML/Code Sections", null,

							tagName: "pre",

							outputTitle: "Code Section Tests"
							);
			}

		}
	}
