/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.CommentDetection
 * ____________________________________________________________________________
 *
 * Tests to make sure Natural Docs can detect comments in a source file, including distinguishing between Natural Docs,
 * Javadoc, and XML-formatted comments.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Comments;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class CommentDetection : TestRunner
		{

		public CommentDetection ()
			: base (InputMode.CommentsAndTopics, EngineMode.InstanceOnly)
			{  	}

		protected override string RunTest (IList<PossibleDocumentationComment> comments, IList<Topic> topics)
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
