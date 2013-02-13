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

		// Group: Tests
		// __________________________________________________________________________

		[Test, Category("HTML")]
		public void TopicTitles ()
			{
			TestFolder("Output/HTML/Topic Titles", null, "div", "CTitle");
			}

		[Test, Category("HTML")]
		public void Prototypes ()
			{
			TestFolder("Output/HTML/Prototypes/Basic Support", "Shared ND Config/Basic Language Support", "div", "NDPrototype", true);
			}

		[Test, Category("HTML")]
		public void ParameterLists ()
			{
			TestFolder("Output/HTML/Parameter Lists", null, "div", "CBody");
			}

		[Test, Category("HTML")]
		public void CodeSections ()
			{
			TestFolder("Output/HTML/Code Sections", null, "pre");
			}

		[Test, Category("HTML")]
		public void Anchors ()
			{
			TestFolder("Output/HTML/Anchors", null, "a");
			}

		}
	}