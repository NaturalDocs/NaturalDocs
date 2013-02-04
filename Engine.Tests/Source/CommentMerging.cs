/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.CommentMerging
 * ____________________________________________________________________________
 * 
 * File-based tests to make sure Natural Docs can merge comment topics and code topics correctly.
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
	public class CommentMerging : Framework.SourceToTopics
		{

		[Test]
		public void All ()
			{
			TestFolder("Comment Merging");
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

				Topic topic = topics[i];

				if (topic.IsEmbedded)
					{  output.Append("Embedded ");  }
				output.Append(Engine.Instance.TopicTypes.FromID(topic.TopicTypeID).Name);
				if (topic.IsList)
					{  output.Append(" List");  }
				output.Append(": ");
				output.AppendLine(topic.Title);

				if (topic.CodeLineNumber != 0 && topic.CommentLineNumber != 0)
					{  output.AppendLine("(comment line " + topic.CommentLineNumber + ", code line " + topic.CodeLineNumber + ")");  }
				else if (topic.CommentLineNumber != 0)
					{  output.AppendLine("(comment line " + topic.CommentLineNumber + ")");  }
				else if (topic.CodeLineNumber != 0)
					{  output.AppendLine("(code line " + topic.CodeLineNumber + ")");  }

				if (topic.Body != null)
					{  output.AppendLine(topic.Body);  }
				}

			return output.ToString();
			}

		}
	}