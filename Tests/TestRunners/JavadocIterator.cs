/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.JavadocIterator
 * ____________________________________________________________________________
 *
 * A class to test <Engine.Comments.Components.JavadocIterator's> ability to parse Javadoc content.
 *
 *
 * Commands:
 *
 *		The input files are a series of commands, one one each line, in one of the following formats:
 *
 *		> // text
 *		Comment.  Ignored.
 *
 *		> Property [name] in <tag>
 *		Attempts to retrieve the named property in the HTML tag.
 *
 *		> content
 *		Parses the content.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Comments.Components;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class JavadocIterator : TestRunner
		{

		public JavadocIterator ()
			: base (InputMode.Lines, EngineMode.NotNeeded)
			{  	}

		protected override string RunTest (string[] commands)
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
						Tokenizer htmlTag = new Tokenizer( findPropertyMatch.Groups[2].ToString() );

						Engine.Comments.Components.JavadocIterator iterator = new Engine.Comments.Components.JavadocIterator(htmlTag.FirstToken, htmlTag.EndOfTokens);

						if (iterator.Type != JavadocElementType.HTMLTag)
							{  output.AppendLine("- Not a HTML tag");  }
						else
							{
							string value = iterator.HTMLTagProperty(propertyName);

							if (value != null)
								{  output.AppendLine("- " + value);  }
							else
								{  output.AppendLine("- Not found");  }
							}
						}
					else
						{
						Tokenizer javadoc = new Tokenizer(command.Trim());
						Engine.Comments.Components.JavadocIterator iterator = new Engine.Comments.Components.JavadocIterator(javadoc.FirstToken, javadoc.EndOfTokens);

						while (iterator.IsInBounds)
							{
							if (iterator.Type == JavadocElementType.HTMLTag)
								{
								output.AppendLine("- " + iterator.HTMLTagForm.ToString() + " HTML Tag: " + iterator.TagType);

								var properties = iterator.GetAllHTMLTagProperties();

								foreach (var property in properties)
									{  output.AppendLine("  Property " + property.Key + ": " + property.Value);  }
								}
							else if (iterator.Type == JavadocElementType.HTMLComment)
								{
								output.AppendLine("- HTML Comment: " + iterator.String);
								}
							else if (iterator.Type == JavadocElementType.JavadocTag)
								{
								output.AppendLine("- " + iterator.JavadocTagForm.ToString() + " Javadoc Tag: " + iterator.TagType);

								if (iterator.JavadocTagForm == JavadocTagForm.Inline)
									{
									output.AppendLine("  Value: " + iterator.JavadocTagValue);
									}
								}
							else if (iterator.Type == JavadocElementType.EntityChar)
								{
								output.AppendLine("- Entity Char: " + iterator.EntityValue);
								}
							else if (iterator.Type == JavadocElementType.Text)
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
