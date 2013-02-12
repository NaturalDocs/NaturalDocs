/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.CommentDetection
 * ____________________________________________________________________________
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
using GregValure.NaturalDocs.Engine.Comments;
using GregValure.NaturalDocs.Engine.Tests.Framework;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Tests
	{
	[TestFixture]
	public class CommentDetection : Framework.SourceToCommentsAndTopics
		{

		// Group: Tests
		// __________________________________________________________________________

		[Test, Category("Basic Language Support")]
		public void BasicSupport ()
			{
			TestFolder("Languages/Basic Support/Comment Detection", "Shared ND Config/Basic Language Support");
			}

		[Test, Category("General")]
		public void FileEncodings ()
			{
			TestFolder("General/File Encodings", "Shared ND Config/Basic Language Support");
			}


		// Group: Output
		// __________________________________________________________________________

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