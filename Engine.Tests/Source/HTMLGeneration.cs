/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.HTMLGeneration
 * ____________________________________________________________________________
 * 
 * File-based tests to check Natural Docs' HTML generation.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using NUnit.Framework;
using GregValure.NaturalDocs.Engine.Tests.Framework;


namespace GregValure.NaturalDocs.Engine.Tests
	{
	[TestFixture]
	public class HTMLGeneration : Framework.SourceToHTML
		{

		[Test]
		public void TopicTitles ()
			{
			TestFolder("HTML Generation/Topic Titles", null, "div", "CTitle");
			}

		[Test]
		public void Prototypes ()
			{
			TestFolder("HTML Generation/Prototypes/Basic Language Support", "Shared ND Config/Basic Language Support", "div", "NDPrototype", true);
			}

		[Test]
		public void ParameterLists ()
			{
			TestFolder("HTML Generation/Parameter Lists", null, "div", "CBody");
			}

		[Test]
		public void CodeSections ()
			{
			TestFolder("HTML Generation/Code Sections", null, "pre");
			}

		[Test]
		public void Anchors ()
			{
			TestFolder("HTML Generation/Anchors", null, "a");
			}

		}
	}