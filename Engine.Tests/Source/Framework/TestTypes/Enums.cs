/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.Enums
 * ____________________________________________________________________________
 *
 * File-based tests to make sure Natural Docs can find enum values and any inline comments for them and add
 * them to a <Topic's> body.
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
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class Enums : Framework.BaseTestTypes.SourceToTopics
		{

		public override string OutputOf (IList<Topic> topics)
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
