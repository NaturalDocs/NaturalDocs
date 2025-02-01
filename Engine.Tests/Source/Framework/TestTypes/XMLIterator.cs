/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.XMLIterator
 * ____________________________________________________________________________
 *
 * A class to test <Engine.Comments.Parsers.XMLIterator's> ability to parse XML.
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
 *
 * Commands:
 *
 *		> // text
 *		Comment.  Ignored.
 *
 *		> Property [name] in <tag>
 *		Attempts to retrieve the named property in the tag.
 *
 *		> content
 *		Parses the content.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Comments.Components;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class XMLIterator : Framework.BaseTestTypes.TextCommands
		{

		public override string OutputOf (IList<string> commands)
			{
			StringBuilder output = new StringBuilder();

			foreach (string command in commands)
				{
				output.AppendLine(command);
				Match findPropertyMatch = FindPropertyRegex.Match(command);

				try
					{
					if (command == "" || command.StartsWith("//"))
						{
						// Ignore
						}
					else if (findPropertyMatch.Success)
						{
						string propertyName = findPropertyMatch.Groups[1].ToString();
						Tokenizer xmlTag = new Tokenizer( findPropertyMatch.Groups[2].ToString() );

						Engine.Comments.Components.XMLIterator iterator = new Engine.Comments.Components.XMLIterator(xmlTag.FirstToken, xmlTag.EndOfTokens);

						if (iterator.Type != XMLElementType.Tag)
							{  output.AppendLine("- Not an XML tag");  }
						else
							{
							string value = iterator.TagProperty(propertyName);

							if (value != null)
								{  output.AppendLine("- " + value);  }
							else
								{  output.AppendLine("- Not found");  }
							}
						}
					else
						{
						Tokenizer xml = new Tokenizer(command.Trim());
						Engine.Comments.Components.XMLIterator iterator = new Engine.Comments.Components.XMLIterator(xml.FirstToken, xml.EndOfTokens);

						while (iterator.IsInBounds)
							{
							if (iterator.Type == XMLElementType.Tag)
								{
								output.AppendLine("- " + iterator.TagForm.ToString() + " Tag: " + iterator.TagType);

								var properties = iterator.GetAllTagProperties();

								foreach (var property in properties)
									{  output.AppendLine("  Property " + property.Key + ": " + property.Value);  }
								}
							else if (iterator.Type == XMLElementType.EntityChar)
								{
								output.AppendLine("- Entity Char: " + iterator.EntityValue);
								}
							else if (iterator.Type == XMLElementType.Text)
								{
								output.AppendLine("- Text: " + iterator.String);
								}
							else
								{
								output.AppendLine("- Unexpected element type " + iterator.Type.ToString());
								}

							iterator.Next();
							}

						output.AppendLine();
						}
					}
				catch (Exception e)
					{
					output.AppendLine("Exception: " + e.Message);
					output.AppendLine("(" + e.GetType().ToString() + ")");
					}
				}

			return output.ToString();
			}

		static private Regex FindPropertyRegex = new Regex(" *Property ([a-z\\-_]+) in (.*)$",
																					 RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		}
	}
