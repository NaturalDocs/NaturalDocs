/* 
 * Class: GregValure.NaturalDocs.EngineTests.NDMarkup
 * ____________________________________________________________________________
 * 
 * File-based tests to make sure Natural Docs can convert comments to <NDMarkup> correctly.
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
	public class NDMarkup : Framework.SourceToTopics
		{

		[Test]
		public void All ()
			{
			TestFolder("NDMarkup");
			}

		public override string OutputOf (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics found)";  }

			StringBuilder output = new StringBuilder();

			for (int i = 0; i < topics.Count; i++)
				{
				if (i != 0)
					{  output.AppendLine("-----");  }

				if (topics[i].Body == null)
					{  output.AppendLine("(No body detected)");  }
				else
					{  output.AppendLine(topics[i].Body);  }
				}

			return output.ToString();
			}

		}
	}