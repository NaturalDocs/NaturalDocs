/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.InlineComments
 * ____________________________________________________________________________
 *
 * Tests to make sure Natural Docs can handle inline comments correctly.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class InlineComments : TestRunner
		{

		public InlineComments ()
			: base (InputMode.Topics, EngineMode.InstanceOnly)
			{  	}

		protected override string RunTest (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics found)";  }

			StringBuilder output = new StringBuilder();

			for (int i = 0; i < topics.Count; i++)
				{
				Topic topic = topics[i];

				if (!topic.IsGroup)
					{  output.Append("  ");  }

				if (topic.IsEmbedded)
					{  output.Append("Embedded ");  }
				output.Append(EngineInstance.CommentTypes.FromID(topic.CommentTypeID).Name);
				if (topic.IsList)
					{  output.Append(" List");  }
				output.Append(": ");
				output.AppendLine(topic.Title);

				if (topic.IsGroup)
					{  }
				else if (topic.Body != null)
					{  output.AppendLine("  Body: " + topic.Body);  }
				else
					{  output.AppendLine("  (no body)");  }

				if (topic.IsGroup)
					{
					output.AppendLine("----------------------------------------");
					output.AppendLine();
					}
				else if (i + 1 < topics.Count)
					{
					output.AppendLine();

					if (topics[i + 1].IsGroup)
						{  output.AppendLine();  }
					}
				}

			return output.ToString();
			}

		}
	}
