/* 
 * Class: GregValure.NaturalDocs.EngineTests.Comments
 * ____________________________________________________________________________
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
using GregValure.NaturalDocs.Engine.Comments;
using GregValure.NaturalDocs.EngineTests.Framework;


namespace GregValure.NaturalDocs.EngineTests
	{
	[TestFixture]
	public class Comments : Framework.SourceToCommentsAndTopics
		{

		[Test]
		public void All ()
			{
			TestFolder("Comments");
			}

		public override string OutputOf (IList<PossibleDocumentationComment> comments, IList<Topic> topics)
			{
			if (comments == null || comments.Count == 0)
				{  return "(No comments found)";  }

			int commentIndex = 0;
			int topicIndex = 0;
			StringBuilder output = new StringBuilder();

			while (commentIndex < comments.Count)
				{
				PossibleDocumentationComment comment = comments[commentIndex];

				output.Append("- ");

				if (comment.Javadoc && comment.XML)
					{  output.Append("Javadoc+XML");  }
				else if (comment.Javadoc)
					{  output.Append("Javadoc");  }
				else if (comment.XML)
					{  output.Append("XML");  }
				else
					{  output.Append("Plain");  }

				output.Append(" comment, ");
				if (comment.End.LineNumber == comment.Start.LineNumber + 1)
					{  output.AppendLine("line " + comment.Start.LineNumber + ':');  }
				else
					{  output.AppendLine("lines " + comment.Start.LineNumber + '-' + (comment.End.LineNumber - 1) + ':');  }

				output.AppendLine();

				if (topics != null && topicIndex < topics.Count && topics[topicIndex].CommentLineNumber < comment.End.LineNumber)
					{
					do
						{
						Topic topic = topics[topicIndex];
						output.AppendLine("  - Topic \"" + topic.Title + "\", line " + topic.CommentLineNumber + ':');

						if (topic.Body != null)
							{  output.AppendLine("    - " + topic.Body);  }
						else
							{  output.AppendLine("    - (No body detected)");  }

						output.AppendLine();
 						topicIndex++;
						}
					while (topicIndex < topics.Count && topics[topicIndex].CommentLineNumber < comment.End.LineNumber);
					}
				else
					{
					output.AppendLine("  - (No topics detected)");
					output.AppendLine();
					}

				commentIndex++;
				}

			return output.ToString();
			}

		}
	}