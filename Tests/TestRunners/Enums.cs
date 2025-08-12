/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.Enums
 * ____________________________________________________________________________
 *
 * Tests to make sure Natural Docs can find enum values and any inline comments for them and add them to a <Topic's> body.
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
	public class Enums : TestRunner
		{

		public Enums ()
			: base (InputMode.Topics, EngineMode.InstanceOnly)
			{  	}

		protected override string RunTest (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics found)";  }

			int enumCommentTypeID = EngineInstance.CommentTypes.IDFromKeyword("enum", topics[0].LanguageID);
			int groupCommentTypeID = EngineInstance.CommentTypes.GroupCommentTypeID;

			StringBuilder output = new StringBuilder();

			bool isFirst = true;
			for (int i = 0; i < topics.Count; i++)
				{
				if (topics[i].CommentTypeID == enumCommentTypeID ||
					topics[i].CommentTypeID == groupCommentTypeID)
					{
					if (isFirst)
						{
						output.AppendLine();
						isFirst = false;
						}
					else
						{
						output.AppendLine();
						output.AppendLine("-----");
						output.AppendLine();
						}

					output.AppendLine(EngineInstance.CommentTypes.FromID(topics[i].CommentTypeID).DisplayName + ": " + topics[i].Title);
					}

				if (topics[i].CommentTypeID == enumCommentTypeID)
					{
					output.AppendLine();

					if (topics[i].Prototype == null)
						{  output.AppendLine("   Prototype: (none)");  }
					else
						{  output.AppendLine("   Prototype: " + topics[i].Prototype);  }

					output.AppendLine();

					int memberIndex = i + 1;

					if (memberIndex >= topics.Count ||
						topics[memberIndex].IsEmbedded == false)
						{  output.AppendLine("   Members: (none)");  }
					else
						{
						output.AppendLine("   Members:");

						do
							{
							output.Append("      " + topics[memberIndex].Title);

							if (topics[memberIndex].Body != null)
								{  output.Append(" - " + topics[memberIndex].Body);  }

							output.AppendLine();

							memberIndex++;
							}
						while (memberIndex < topics.Count &&
								 topics[memberIndex].IsEmbedded);
						}

					output.AppendLine();

					if (topics[i].Body == null)
						{  output.AppendLine("   Body: (none)");  }
					else
						{  output.AppendLine("   Body: " + topics[i].Body);  }
					}
				}

			return output.ToString();
			}

		}
	}
