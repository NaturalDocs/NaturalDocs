/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.CommentDetection
 * ____________________________________________________________________________
 *
 * File-based tests to make sure Natural Docs can detect comments in a source file, including distinguishing
 * between Natural Docs, Javadoc, and XML-formatted comments.
 *
 *
 * Deriving a Test Class:
 *
 *		- Derive a class and add the [TestFixture] attribute.
 *
 *		- Create a function with the [Test] attribute that calls TestFolder(), pointing it to the input files.
 *
 *
 * Input and Output Files:
 *
 *		- All files in the test folder in the format "[Test Name] - Input.[extension]" will be tested when NUnit runs.
 *
 *		- A corresponding file "[Test Name] - Actual Output.txt" will be created for each one.
 *
 *		- If it matches the contents of the file "[Test Name] - Expected Output.txt", the test will pass.  If it doesn't,
 *		  that file doesn't exist, or an exception was thrown, the test will fail.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Comments;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class CommentDetection : Framework.BaseTestTypes.SourceToCommentsAndTopics
		{

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
