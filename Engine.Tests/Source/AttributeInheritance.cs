/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.AttributeInheritance
 * ____________________________________________________________________________
 * 
 * File-based tests to make sure Natural Docs topics inherit certain attributes correctly.
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
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Tests.Framework;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Tests
	{
	[TestFixture]
	public class AttributeInheritance : Framework.SourceToTopics
		{

		[Test]
		public void All ()
			{
			TestFolder("Attribute Inheritance", "Shared ND Config/Basic Language Support");
			}

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

				output.AppendLine("- Language: " + Engine.Instance.Languages.FromID(topics[i].LanguageID).Name);
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

						output.Append(Engine.Instance.TopicTypes.TagFromID(tagID).Name);
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