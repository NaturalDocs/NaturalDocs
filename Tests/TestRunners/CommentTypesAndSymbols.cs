/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.CommentTypesAndSymbols
 * ____________________________________________________________________________
 *
 * Tests to make sure comment types, symbols, and their scope are correct.
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
	public class CommentTypesAndSymbols : TestRunner
		{

		public CommentTypesAndSymbols ()
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
					{
					output.AppendLine();
					output.AppendLine("-----");
					output.AppendLine();
					}

				if (topics[i].Title == null)
					{  output.AppendLine("(untitled topic)");  }
				else
					{  output.AppendLine(topics[i].Title);  }

				if (topics[i].CommentTypeID != 0)
					{  output.AppendLine("   Comment Type: " + EngineInstance.CommentTypes.FromID(topics[i].CommentTypeID).Name);  }
				else
					{  output.AppendLine("   Comment Type: (undefined)");  }

				if (topics[i].Symbol != null)
					{  output.AppendLine("   Symbol: " + topics[i].Symbol.FormatWithSeparator('|'));  }
				else
					{  output.AppendLine("   Symbol: (undefined)");  }
				}

			return output.ToString();
			}

		}
	}
