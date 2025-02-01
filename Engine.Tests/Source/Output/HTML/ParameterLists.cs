
// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using NUnit.Framework;
using CodeClear.NaturalDocs.Engine.Tests.Framework;


namespace CodeClear.NaturalDocs.Engine.Tests.Output.HTML
	{
	[TestFixture]
	public class ParameterLists : Framework.TestTypes.HTML
		{

		[Test]
		public void All ()
			{
			TestFolder("Output/HTML/Parameter Lists", "Shared ND Config/Basic Language Support",

							tagName: "div",
							className: "CBody",

						    outputTitle: "Parameter List Tests"
							);
			}

		}
	}
