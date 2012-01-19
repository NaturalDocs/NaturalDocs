/* 
 * Class: GregValure.NaturalDocs.EngineTests.PrototypeDetection
 * ____________________________________________________________________________
 * 
 * File-based tests to make sure Natural Docs can detect prototypes correctly.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
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
	public class PrototypeDetection : Framework.SourceToTopics
		{

		[Test]
		public void BasicLanguageSupport ()
			{
			TestFolder("Prototype Detection/Basic Language Support", "Shared ND Config/Basic Language Support");
			}

		public override string OutputOf (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics found)";  }

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