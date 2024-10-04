/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.Topics
 * ____________________________________________________________________________
 *
 * File-based tests to make sure Natural Docs can extract topics from a file.
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

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class Topics : Framework.BaseTestTypes.SourceToTopics
		{

		public override string OutputOf (IList<Topic> topics)
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
					{  output.AppendLine("Topic: " + topics[i].Title);  }

				output.AppendLine();

				if (topics[i].CommentLineNumber != 0 && topics[i].CodeLineNumber != 0)
					{  output.AppendLine("   Location: Comment on line " + topics[i].CommentLineNumber + ", code on line " + topics[i].CodeLineNumber);  }
				else if (topics[i].CommentLineNumber != 0)
					{  output.AppendLine("   Location: Comment on line " + topics[i].CommentLineNumber);  }
				else if (topics[i].CodeLineNumber != 0)
					{  output.AppendLine("   Location: Code on line " + topics[i].CodeLineNumber);  }

				output.AppendLine();

				if (topics[i].CommentTypeID != 0)
					{  output.AppendLine("   Comment Type: " + EngineInstance.CommentTypes.FromID(topics[i].CommentTypeID).DisplayName);  }
				if (topics[i].Symbol != null)
					{  output.AppendLine("   Symbol: " + topics[i].Symbol.FormatWithSeparator('.'));  }

				if (topics[i].Prototype != null)
					{
					output.AppendLine();
					output.AppendLine("   Prototype: " + topics[i].Prototype);
					}

				if (topics[i].Body != null)
					{
					output.AppendLine();
					output.AppendLine("   Body: " + topics[i].Body);
					}
				}

			return output.ToString();
			}

		}
	}
