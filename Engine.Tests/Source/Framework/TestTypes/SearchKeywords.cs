/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.SearchKeywords
 * ____________________________________________________________________________
 *
 * File-based tests to make sure Natural Docs can generate search keywords correctly.
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
	public class SearchKeywords : Framework.BaseTestTypes.SourceToTopics
		{

		public override string OutputOf (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics found)";  }

			StringBuilder output = new StringBuilder();
			var searchIndex = (EngineInstance.Output.Targets[0] as Engine.Output.HTML.Target).SearchIndex;

			for (int i = 0; i < topics.Count; i++)
				{
				if (i != 0)
					{  output.AppendLine();  }

				output.AppendLine("Topic: " + topics[i].Title);

				var searchEntry = new Engine.Output.HTML.SearchIndex.Entries.Topic(topics[i], searchIndex);

				output.Append("- Display Name: ");

				if (searchEntry.EndOfDisplayNameQualifiers > 0)
					{
					output.Append('(');
					output.Append(searchEntry.DisplayName, 0, searchEntry.EndOfDisplayNameQualifiers);
					output.Append(')');
					output.Append(searchEntry.DisplayName, searchEntry.EndOfDisplayNameQualifiers,
										 searchEntry.DisplayName.Length - searchEntry.EndOfDisplayNameQualifiers);
					output.AppendLine();
					}
				else
					{  output.AppendLine(searchEntry.DisplayName);  }

				output.Append("- Search Text: ");

				if (searchEntry.EndOfSearchTextQualifiers > 0)
					{
					output.Append('(');
					output.Append(searchEntry.SearchText, 0, searchEntry.EndOfSearchTextQualifiers);
					output.Append(')');
					output.Append(searchEntry.SearchText, searchEntry.EndOfSearchTextQualifiers,
										 searchEntry.SearchText.Length - searchEntry.EndOfSearchTextQualifiers);
					output.AppendLine();
					}
				else
					{  output.AppendLine(searchEntry.SearchText);  }

				output.Append("- Keywords: ");

				if (searchEntry.Keywords == null || searchEntry.Keywords.Count == 0)
					{  output.AppendLine("(none)");  }
				else
					{  output.AppendLine(string.Join(", ", searchEntry.Keywords.ToArray()));  }
				}

			return output.ToString();
			}

		}
	}
