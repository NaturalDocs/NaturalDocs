/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.LinkScoring
 * ____________________________________________________________________________
 *
 * A class to test <Engine.CodeDB.Manager.ScoreLink>.
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
 *		Comment.
 *
 *		> Topic.[property] = "value"
 *		Sets the topic property to that value.  Possibilities are:
 *		   > Topic.LanguageName = "C#"
 *		   > Topic.Keyword = "Function"
 *		   > Topic.Title = "FunctionName"
 *		   > Topic.Body = "<p>This is my function.</p>" or null
 *		   > Topic.Prototype = "void FunctionName (int x)" or null
 *		   > Topic.Scope = "ClassName" or null
 *
 *		> Link.[property] = "value"
 *		Sets the link property to that value.  Possibilities are:
 *			> Link.LanguageName = "C#"
 *			> Link.Type = "Natural Docs/Class Parent/Type"
 *			> Link.Text = "<FunctionName()>"
 *			> Link.Scope = "ClassName" or null
 *			>
 *			> Link.Using = add prefix "PackageA.PackageB"
 *			> Link.Using = replace prefix "PackageA" with "PackageB"
 *			> Link.Using = null
 *			> Link.Using += add prefix "PackageA.PackageB"
 *			> Link.Using += replace prefix "PackageA" with "PackageB"
 *
 *		> Score
 *		Generates a score between the current link and topic settings.
 *
 *		> Show [property], [property] ...
 *		Sets the properties that will be shown by a Score command.  This lets you limit the output to just what's
 *		being tested.  Defaults to All.  Possible values are:
 *
 *			All - Show everything
 *			RawScore - The generated value in hex.
 *			Language - Whether the topic matches the link's language (L)
 *			Capitalization - Whether the topic and link's capitalization match if it matters to the language (C)
 *			Parameters - How well the parameters match (T, P)
 *			Scope - How high on the scope list the symbol match is (S)
 *			Interpretation - How high on the interpretation list (named/plural/possessive) the match is (E, I)
 *			Body - Whether the topic has a body (B, b)
 *			Prototype - Whether the topic has a prototype (R, r)
 *			SameSymbol - How high on the list of topics that define the same symbol in the same file this is (F)
 *
 *		Link and topic settings persist, so you can set them both up, use "score", then change one or two
 *		properties and use "score" again.
 *
 *		The code will tolerate semicolons after commands.  They're not necessary, but due to its similarity to
 *		actual code they're handled just in case.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class LinkScoring : Framework.BaseTestTypes.TextCommands
		{

		public override string OutputOf (IList<string> commands)
			{
			StringBuilder output = new StringBuilder();
			bool lastWasLineBreak = true;

			Topic topic = new Topic(EngineInstance.CommentTypes);
			topic.LanguageID = EngineInstance.Languages.FromName("C#").ID;
			topic.CommentTypeID = EngineInstance.CommentTypes.FromKeyword("Function", topic.LanguageID).ID;

			Link link = new Engine.Links.Link();
			link.LanguageID = EngineInstance.Languages.FromName("C#").ID;
			link.Type = Engine.Links.LinkType.NaturalDocs;
			bool linkIsPlainText = true;

			string show = "all";

			for (int i = 0; i < commands.Count; i++)
				{
				string command = commands[i];
				if (command.EndsWith(";"))
					{  command = command.Substring(0, command.Length - 1).TrimEnd();  }

				Match match = GetPropertyRegex.Match(command);
				string target, property, value, valueString;

				Match addPrefixMatch = null;
				Match replacePrefixMatch = null;

				if (match.Success)
					{
					target = match.Groups[1].ToString().ToLower(CultureInfo.InvariantCulture);
					property = match.Groups[2].ToString().ToLower(CultureInfo.InvariantCulture);
					value = match.Groups[3].ToString();

					if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
						{
						valueString = value.Substring(1, value.Length - 2).Trim();
						}
					else
						{
						value = value.ToLower(CultureInfo.InvariantCulture);
						valueString = null;
						}
					}
				else
					{
					addPrefixMatch = AddPrefixRegex.Match(command);
					replacePrefixMatch = ReplacePrefixRegex.Match(command);

					if (addPrefixMatch.Success || replacePrefixMatch.Success)
						{
						target = "link";
						property = "using";
						value = null;
						valueString = null;
						}
					else
						{
						target = null;
						property = null;
						value = null;
						valueString = null;
						}
					}

				var lcCommand = command.ToLower(CultureInfo.InvariantCulture);

				try
					{
					if (command == "")
						{
						if (!lastWasLineBreak)
							{
							output.AppendLine();
							lastWasLineBreak = true;
							}
						}
					else if (command.StartsWith("//"))
						{
						output.AppendLine(command);
						lastWasLineBreak = false;
						}


					// Topic properties

					else if (target == "topic")
						{
						if (property == "languagename")
							{
							if (valueString == null)
								{  throw new Exception("Topic.LanguageName must be set to a string value.");  }

							var language = EngineInstance.Languages.FromName(valueString);

							if (language == null)
								{  throw new Exception("\"" + valueString + "\" is not recognized as a language.");  }

							topic.LanguageID = language.ID;
							}
						else if (property == "keyword")
							{
							if (valueString == null)
								{  throw new Exception("Topic.Keyword must be set to a string value.");  }

							var commentType = EngineInstance.CommentTypes.FromKeyword(valueString, topic.LanguageID);

							if (commentType == null)
								{  throw new Exception("\"" + valueString + "\" is not recognized as a keyword.");  }

							topic.CommentTypeID = commentType.ID;
							}
						else if (property == "title")
							{
							if (valueString == null)
								{  throw new Exception("Topic.Title must be set to a string value.");  }
							if (valueString == "")
								{  throw new Exception("Topic.Title cannot be set to an empty string.");  }

							topic.Title = valueString;
							}
						else if (property == "body")
							{
							if (value == "null")
								{  topic.Body = null;  }
							else if (valueString == null)
								{  throw new Exception("Topic.Body must be set to null or a string value.");  }
							else
								{  topic.Body = valueString;  }
							}
						else if (property == "prototype")
							{
							if (value == "null")
								{  topic.Prototype = null;  }
							else if (valueString == null)
								{  throw new Exception("Topic.Prototype must be set to null or a string value.");  }
							else
								{  topic.Prototype = valueString;  }
							}
						else if (property == "scope")
							{
							if (value == "null")
								{
								ContextString temp = topic.PrototypeContext;
								temp.Scope = new SymbolString();
								topic.PrototypeContext = temp;
								}
							else if (valueString == null)
								{  throw new Exception("Topic.Scope must be set to null or a string value.");  }
							else
								{
								ContextString temp = topic.PrototypeContext;
								temp.Scope = SymbolString.FromPlainText_NoParameters(valueString);
								topic.PrototypeContext = temp;
								}
							}
						else
							{  throw new Exception("\"" + property + "\" is not a recognized Topic property.");  }

						// Leave lastWasLineBreak alone since we're not generating output.
						}


					// Link properties

					else if (target == "link")
						{
						if (property == "languagename")
							{
							if (valueString == null)
								{  throw new Exception("Link.LanguageName must be set to a string value.");  }

							var language = EngineInstance.Languages.FromName(valueString);

							if (language == null)
								{  throw new Exception("\"" + valueString + "\" is not recognized as a language.");  }

							link.LanguageID = language.ID;
							}
						else if (property == "type")
							{
							if (valueString == null)
								{  throw new Exception("Link.Type must be set to a string value.");  }

							string lcValueString = valueString.ToLower(CultureInfo.InvariantCulture);

							if (lcValueString == "naturaldocs" || lcValueString == "natural docs")
								{  link.Type = LinkType.NaturalDocs;  }
							else if (lcValueString == "type")
								{  link.Type = LinkType.Type;  }
							else if (lcValueString == "classparent" || lcValueString == "class parent")
								{  link.Type = LinkType.ClassParent;  }
							else
								{  throw new Exception("\"" + valueString + "\" is not recognized as a link type.");  }
							}
						else if (property == "text")
							{
							if (valueString == null)
								{  throw new Exception("Link.Text must be set to a string value");  }
							if (valueString == "")
								{  throw new Exception("Link.Text cannot be set to an empty string.");  }

							link.TextOrSymbol = valueString;
							linkIsPlainText = true;
							}
						else if (property == "scope")
							{
							if (value == "null")
								{
								ContextString temp = link.Context;
								temp.Scope = new SymbolString();
								link.Context = temp;
								}
							else if (valueString == null)
								{  throw new Exception("Link.Scope must be set to null or a string value.");  }
							else if (valueString == "")
								{  throw new Exception("Link.Scope cannot be set to an empty string.");  }
							else
								{
								ContextString temp = link.Context;
								temp.Scope = SymbolString.FromPlainText_NoParameters(valueString);
								link.Context = temp;
								}
							}
						else if (property == "using")
							{
							if (value == "null")
								{
								ContextString temp = link.Context;
								temp.ClearUsingStatements();
								link.Context = temp;
								}
							else if (addPrefixMatch != null && addPrefixMatch.Success)
								{
								string op = addPrefixMatch.Groups[1].ToString();
								string add = addPrefixMatch.Groups[2].ToString();

								ContextString temp = link.Context;

								if (op == "=")
									{  temp.ClearUsingStatements();  }

								temp.AddUsingStatement(
									UsingString.FromParameters(
										UsingString.UsingType.AddPrefix,
										SymbolString.FromPlainText_NoParameters(add)
										)
									);

								link.Context = temp;
								}
							else if (replacePrefixMatch != null && replacePrefixMatch.Success)
								{
								string op = replacePrefixMatch.Groups[1].ToString();
								string remove = replacePrefixMatch.Groups[2].ToString();
								string add = replacePrefixMatch.Groups[3].ToString();

								ContextString temp = link.Context;

								if (op == "=")
									{  temp.ClearUsingStatements();  }

								temp.AddUsingStatement(
									UsingString.FromParameters(
										UsingString.UsingType.ReplacePrefix,
										SymbolString.FromPlainText_NoParameters(add),
										SymbolString.FromPlainText_NoParameters(remove)
										)
									);

								link.Context = temp;
								}
							else
								{  throw new Exception("\"" + command + "\" is not a recognized Link.Using statement.");  }
							}
						else
							{  throw new Exception("\"" + property + "\" is not recognized as a link property.");  }
						// Leave lastWasLineBreak alone since we're not generating output.
						}


					// Show

					else if (lcCommand.StartsWith("show"))
						{
						show = lcCommand.Substring(4);
						}


					// Score

					else if (lcCommand == "score")
						{
						// Validate fields

						if (topic.Title == null)
							{  throw new Exception("You didn't set Topic.Title.");  }
						if (link.TextOrSymbol == null)
							{  throw new Exception("You didn't set Link.Text.");  }


						// Calculate fields

						string parametersString;
						SymbolString topicSymbol = SymbolString.FromPlainText(topic.Title, out parametersString);

						var commentType = EngineInstance.CommentTypes.FromID(topic.CommentTypeID);

						if (commentType.Scope == Engine.CommentTypes.CommentType.ScopeValue.Normal &&
							 topic.PrototypeContext.ScopeIsGlobal == false)
							{
							topicSymbol = topic.PrototypeContext.Scope + topicSymbol;
							}

						topic.Symbol = topicSymbol;

						if (link.Type == LinkType.Type || link.Type == LinkType.ClassParent)
							{
							if (linkIsPlainText)
								{
								SymbolString linkSymbol = SymbolString.FromPlainText_NoParameters(link.TextOrSymbol);
								link.TextOrSymbol = linkSymbol.ToString();
								link.EndingSymbol = linkSymbol.EndingSymbol;
								linkIsPlainText = false;
								}
							}
						else
							{
							string ignore;
							SymbolString linkSymbol = SymbolString.FromPlainText(link.TextOrSymbol, out ignore);
							link.EndingSymbol = linkSymbol.EndingSymbol;
							}


						// Show topic

						if (!lastWasLineBreak)
							{  output.AppendLine();  }

						var topicLanguage = EngineInstance.Languages.FromID(topic.LanguageID);
						commentType = EngineInstance.CommentTypes.FromID(topic.CommentTypeID);

						output.AppendLine(topicLanguage.Name + " " + commentType.Name + " Topic: " + topic.Title);
						output.AppendLine("   Symbol: " + topic.Symbol.FormatWithSeparator('.'));

						if (topic.TitleParameters != null)
							{
							output.AppendLine("   Title Parameters: " + topic.TitleParameters.ToString().Replace(Engine.Symbols.ParameterString.SeparatorChar, ','));
							}
						if (topic.PrototypeParameters != null)
							{
							output.AppendLine("   Prototype Parameters: " + topic.PrototypeParameters.ToString().Replace(Engine.Symbols.ParameterString.SeparatorChar, ','));
							}
						if (topic.Prototype != null)
							{  output.AppendLine("   Prototype: " + topic.Prototype);  }
						if (topic.Body != null)
							{  output.AppendLine("   Body: " + topic.Body);  }

						output.AppendLine();


						// Show link

						var linkLanguage = EngineInstance.Languages.FromID(link.LanguageID);

						output.AppendLine(linkLanguage.Name + " " + link.Type + " Link: " + link.TextOrSymbol.Replace(Engine.Symbols.SymbolString.SeparatorChar, '*'));

						if (link.Context.ScopeIsGlobal)
							{  output.AppendLine("   Scope: Global");  }
						else
							{  output.AppendLine("   Scope: " + link.Context.Scope.FormatWithSeparator('.'));  }

						var usingStatements = link.Context.GetUsingStatements();

						if (usingStatements != null)
							{
							foreach (var usingStatement in usingStatements)
								{
								if (usingStatement.Type == UsingString.UsingType.AddPrefix)
									{
									output.AppendLine("   Using: Add Prefix " + usingStatement.PrefixToAdd.FormatWithSeparator('.'));
									}
								else if (usingStatement.Type == UsingString.UsingType.ReplacePrefix)
									{
									output.AppendLine("   Using: Replace Prefix " + usingStatement.PrefixToRemove.FormatWithSeparator('.') +
															  " with " + usingStatement.PrefixToAdd.FormatWithSeparator('.'));
									}
								else
									{  throw new NotImplementedException("Unexpected using type " + usingStatement.Type);  }
								}
							}

						output.AppendLine();


						// Show score

						List<Engine.Links.LinkInterpretation> interpretations = null;

						if (link.Type == LinkType.NaturalDocs)
							{
							string ignore;
							interpretations = EngineInstance.Comments.NaturalDocsParser.LinkInterpretations(link.TextOrSymbol,
																								Engine.Comments.NaturalDocs.Parser.LinkInterpretationFlags.AllowNamedLinks |
																								Engine.Comments.NaturalDocs.Parser.LinkInterpretationFlags.AllowPluralsAndPossessives,
																								out ignore);
							}

						long score = EngineInstance.Links.Score(link, topic, 0, interpretations);

						if (score <= 0)
							{  output.AppendLine("☓☓☓ No Match ☓☓☓");  }
						else
							{
							output.Append("Match score:");

							if (show.Contains("all") || show.Contains("rawscore"))
								{
								string scoreString = score.ToString("X16");
								output.Append(" " + scoreString.Substring(0, 4) + " " + scoreString.Substring(4, 4) +
															 " " + scoreString.Substring(8, 4) + " " + scoreString.Substring(12, 4));
								}

							output.AppendLine();

							// DEPENDENCY: This code depends on the score format generated by Engine.Links.Manager.Score().

							// Format:
							// 0LCETPPP PPPPPPPP PPPPPPPP PSSSSSSS SSSIIIII IBFFFFFF Rbbbbbbb brrrrrr1

							// L - Whether the topic matches the link's language.
							// C - Whether the topic and link's capitalization match if it matters to the language.
							// E - Whether the text is an exact match with no plural or possessive conversions applied.
							// T - Whether the link parameters exactly match the topic title parameters.
							// P - How well the parameters match.
							// S - How high on the scope list the symbol match is.
							// I - How high on the interpretation list (named/plural/possessive) the match is.
							// B - Whether the topic has a body
							// F - How high on the list of topics that define the same symbol in the same file this is.
							// R - Whether the topic has a prototype.
							// b - The length of the body divided by 16.
							// r - The length of the prototype divided by 16.

							long LValue = (score & 0x4000000000000000) >> 62;
							long CValue = (score & 0x2000000000000000) >> 61;
							long EValue = (score & 0x1000000000000000) >> 60;
							long TValue = (score & 0x0800000000000000) >> 59;
							long PValue = (score & 0x07FFFF8000000000) >> 39;
							long SValue = (score & 0x0000007FE0000000) >> 29;
							long IValue = (score & 0x000000001F800000) >> 23;
							long BValue = (score & 0x0000000000400000) >> 22;
							long FValue = (score & 0x00000000003F0000) >> 16;
							long RValue = (score & 0x0000000000008000) >> 15;
							long bValue = (score & 0x0000000000007F80) >> 7;
							long rValue = (score & 0x000000000000007E) >> 1;

							if (show.Contains("all") || show.Contains("language"))
								{  output.AppendLine("   " + (LValue == 1 ? "☒" : "☐") + " - Language");  }
							if (show.Contains("all") || show.Contains("capitalization"))
								{  output.AppendLine("   " + (CValue == 1 ? "☒" : "☐") + " - Capitalization");  }
							if (show.Contains("all") || show.Contains("interpretation"))
								{  output.AppendLine("   " + (EValue == 1 ? "☒" : "☐") + " - Exact text");  }

							if (show.Contains("all") || show.Contains("parameters"))
								{
								output.AppendLine("   " + (TValue == 1 ? "☒" : "☐") + " - Topic title parameters");

								output.Append("   ");
								for (int shift = 18; shift >= 0; shift -= 2)
									{
									long individualPValue = PValue >> shift;
									individualPValue &= 0x0000000000000003;

									switch (individualPValue)
										{
										case 3:
											output.Append("☒");
											break;
										case 2:
											output.Append("↑");
											break;
										case 1:
											output.Append("↓");
											break;
										case 0:
											output.Append("☐");
											break;
										}
									}
								output.AppendLine(" - Parameters");
								}

							if (show.Contains("all") || show.Contains("scope"))
								{
								output.AppendLine("   " + (1023 - SValue) + " - Scope index");
								output.AppendLine("      (" + SValue + " score)");
								}
							if (show.Contains("all") || show.Contains("interpretation"))
								{
								output.AppendLine("   " + (63 - IValue) + " - Interpretation index");
								output.AppendLine("      (" + IValue + " score)");
								}

							if (show.Contains("all") || show.Contains("body"))
								{
								output.AppendLine("   " + (BValue == 1 ? "☒" : "☐") + " - Body");
								if (BValue == 1)
									{
									output.Append("      (" + bValue + " score, " + (bValue * 16));

									if (bValue == 0xFF)
										{  output.Append('+');  }
									else
										{  output.Append("-" + ((bValue * 16) + 15));  }

									output.AppendLine(" characters)");
									}
								}

							if (show.Contains("all") || show.Contains("prototype"))
								{
								output.AppendLine("   " + (RValue == 1 ? "☒" : "☐") + " - Prototype");
								if (RValue == 1)
									{
									output.Append("      (" + rValue + " score, " + (rValue * 16));

									if (rValue == 0x3F)
										{  output.Append('+');  }
									else
										{  output.Append("-" + ((rValue * 16) + 15));  }

									output.AppendLine(" characters)");
									}
								}

							if (show.Contains("all") || show.Contains("samesymbol"))
								{
								output.AppendLine("   " + (63 - FValue) + " - Same symbol in same file index");
								output.AppendLine("      (" + FValue + " score)");
								}
							}

						output.AppendLine();
						lastWasLineBreak = true;
						}

					else
						{  throw new Exception("Unknown command " + command);  }
					}
				catch (Exception e)
					{
					output.AppendLine("Command: " + command);
					output.AppendLine("Exception: " + e.Message);
					output.AppendLine("(" + e.GetType().ToString() + ")");
					lastWasLineBreak = false;
					}
				}

			return output.ToString();
			}

		protected static Regex GetPropertyRegex = new Regex(@"^[ \t]*(Topic|Link)\.([a-z]+)[ \t]*=[ \t]*(null|"".*"")[ \t]*$",
																				  RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		protected static Regex AddPrefixRegex = new Regex(@"^[ \t]*Link\.Using[ \t]*(\+?=)[ \t]*Add Prefix ""(.*)""[ \t]*$",
																			  RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		protected static Regex ReplacePrefixRegex = new Regex(@"^[ \t]*Link\.Using[ \t]*(\+?=)[ \t]*Replace Prefix ""(.*)"" with ""(.*)""[ \t]*$",
																					RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		}
	}
