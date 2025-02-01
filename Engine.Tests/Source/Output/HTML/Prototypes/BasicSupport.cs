﻿
// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using NUnit.Framework;
using CodeClear.NaturalDocs.Engine.Tests.Framework;


namespace CodeClear.NaturalDocs.Engine.Tests.Output.HTML.Prototypes
	{
	[TestFixture]
	public class BasicSupport : Framework.TestTypes.HTML
		{

		[Test]
		public void All ()
			{
			TestFolder("Output/HTML/Prototypes/Basic Support", "Shared ND Config/Basic Language Support plus HTML Structure",

							tagName: "div",
							className: "NDPrototype",

							outputTitle: "BLS Prototype Tests",
							outputSubtitle: "Basic Language Support",
						    outputStyle: "Prominent Prototype Grid on Black",
							reformatHTML: true
							);
			}

		}
	}
