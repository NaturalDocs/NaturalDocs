/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.AttributeInheritance
 * ____________________________________________________________________________
 *
 * File-based tests to make sure Natural Docs topics inherit certain attributes correctly.
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
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class AttributeInheritance : Framework.BaseTestTypes.SourceToTopics
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

				output.AppendLine(topics[i].Title);

				output.AppendLine("- Language: " + EngineInstance.Languages.FromID(topics[i].LanguageID).Name);
				output.AppendLine("- Declared Access Level: " + topics[i].DeclaredAccessLevel);
				output.AppendLine("- Effective Access Level: " + topics[i].EffectiveAccessLevel);

				if (topics[i].TagIDs != null && topics[i].TagIDs.IsEmpty == false)
					{
					output.Append("- Tags: ");
					bool isFirst = true;

					foreach (int tagID in topics[i].TagIDs)
						{
						if (isFirst)
							{  isFirst = false;  }
						else
							{  output.Append(", ");  }

						output.Append(EngineInstance.CommentTypes.TagFromID(tagID).Name);
						}

					output.AppendLine();
					}

				ShowUsingStatements(topics[i].PrototypeContext, "Prototype", output);
				ShowUsingStatements(topics[i].BodyContext, "Body", output);
				}

			return output.ToString();
			}

		void ShowUsingStatements (ContextString context, string contextName, StringBuilder output)
			{
			if (context.HasUsingStatements)
				{
				var usingStatements = context.GetUsingStatements();

				foreach (var usingStatement in usingStatements)
					{
					output.Append("- " + contextName + " Using Statement: ");

					if (usingStatement.Type == UsingString.UsingType.AddPrefix)
						{
						output.AppendLine("Add " + usingStatement.PrefixToAdd.FormatWithSeparator('.'));
						}
					else if (usingStatement.Type == UsingString.UsingType.ReplacePrefix)
						{
						output.AppendLine("Replace " + usingStatement.PrefixToRemove.FormatWithSeparator('.') +
												  " with " + usingStatement.PrefixToAdd.FormatWithSeparator('.'));
						}
					else
						{
						output.AppendLine("Unknown using type " + usingStatement.Type);
						}
					}
				}
			}

		}
	}
