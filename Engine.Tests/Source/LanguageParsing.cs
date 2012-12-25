/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.LanguageParsing
 * ____________________________________________________________________________
 * 
 * File-based tests to make sure Natural Docs can detect language elements from source code correctly.
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
	public class LanguageParsing : Framework.SourceToElements
		{

		[Test]
		public void CSharp ()
			{
			TestFolder("Language Parsing/C#");
			}

		}
	}