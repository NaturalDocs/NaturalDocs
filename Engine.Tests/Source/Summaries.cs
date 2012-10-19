/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.Summaries
 * ____________________________________________________________________________
 * 
 * File-based tests to make sure Natural Docs can extract summaries from comments correctly.
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
using GregValure.NaturalDocs.Engine.Tests.Framework;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Tests
	{
	[TestFixture]
	public class Summaries : Framework.SourceToTopics
		{

		[Test]
		public void All ()
			{
			TestFolder("Summaries");
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

				if (topics[i].Summary == null)
					{  output.AppendLine("(No summary detected)");  }
				else
					{  output.AppendLine(topics[i].Summary);  }
				}

			return output.ToString();
			}

		}
	}