/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.CommentMerging
 * ____________________________________________________________________________
 *
 * File-based tests to make sure Natural Docs can merge comment topics and code topics correctly.
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
	public class CommentMerging : Framework.BaseTestTypes.SourceToTopics
		{

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
				output.Append(EngineInstance.CommentTypes.FromID(topic.CommentTypeID).Name);
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
