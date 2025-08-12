/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.Summaries
 * ____________________________________________________________________________
 *
 * Tests to make sure Natural Docs can extract summaries from comments correctly.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class Summaries : TestRunner
		{

		public Summaries ()
			: base (InputMode.Topics, EngineMode.InstanceOnly)
			{  	}

		protected override string RunTest (IList<Topic> topics)
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
