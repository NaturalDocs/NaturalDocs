/* 
 * Class: GregValure.NaturalDocs.EngineTests.PrototypeDetection
 * ____________________________________________________________________________
 * 
 * File-based tests to make sure Natural Docs can detect prototypes correctly.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using GregValure.NaturalDocs.Engine;
using GregValure.NaturalDocs.EngineTests.Framework;


namespace GregValure.NaturalDocs.EngineTests
	{
	[TestFixture]
	public class PrototypeDetection : Framework.FileBasedTesting.Tester
		{

		[Test]
		public void BasicLanguageSupport ()
			{
			RunFolder("Prototype Detection/Basic Language Support");
			}

		public override string Run (string input, string inputFileExtension)
			{
			IList<Topic> topics = TestHooks.SourceCodeToTopics(input, inputFileExtension);

			if (topics == null)
				{  return "(No topics found)";  }
			else 
				{
				StringBuilder output = new StringBuilder();

				for (int i = 0; i < topics.Count; i++)
					{
					// We manually use \n because calling AppendLine() will use \r\n, which conflicts with the plain
					// \n's in the prototypes.

					if (i != 0)
						{  output.Append("-----\n");  }

					if (topics[i].Prototype == null)
						{  output.Append("(No prototype detected)\n");  }
					else
						{  
						output.Append(topics[i].Prototype);  
						output.Append('\n');
						}
					}

				return output.ToString();
				}
			}

		}
	}