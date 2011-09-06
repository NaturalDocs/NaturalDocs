/* 
 * Class: GregValure.NaturalDocs.EngineTests.HTMLGeneration
 * ____________________________________________________________________________
 * 
 * File-based tests to check Natural Docs' HTML generation.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using NUnit.Framework;
using GregValure.NaturalDocs.EngineTests.Framework;


namespace GregValure.NaturalDocs.EngineTests
	{
	[TestFixture]
	public class HTMLGeneration : Framework.SourceToHTML
		{

		[Test]
		public void TopicTitles ()
			{
			TestFolder("HTML Generation/Topic Titles", "div", "CTitle");
			}

		[Test]
		public void Prototypes ()
			{
			TestFolder("HTML Generation/Prototypes", "div", "NDPrototype");
			}

		[Test]
		public void ParameterLists ()
			{
			TestFolder("HTML Generation/Parameter Lists", "div", "CBody");
			}

		}
	}