/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.LinkScoring
 * ____________________________________________________________________________
 * 
 * A class to test <Engine.CodeDB.Manager.ScoreLink>.
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
 *		
 *		> Score
 *		Generates a score between the current link and topic settings.
 *		
 *		> Show
 *		Shows the topic and link settings in use.
 *		
 *		Link and topic settings persist, so you can set them both up, use "score", then change one or two
 *		properties and use "score" again.
 *		
 *		The code will tolerate semicolons after commands.  They're not necessary, but due to its similarity to
 *		actual code they're handled just in case.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using GregValure.NaturalDocs.Engine;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Tests.Framework;


namespace GregValure.NaturalDocs.Engine.Tests
	{
	[TestFixture]
	public class LinkScoring : Framework.TextCommands
		{

		[Test]
		public void All ()
			{
			TestFolder("LinkScoring");
			}

		public override string OutputOf (IList<string> commands)
			{
			StringBuilder output = new StringBuilder();
			bool lastWasLineBreak = true;

			Topic topic = new Topic();
			topic.LanguageID = Engine.Instance.Languages.FromName("C#").ID;
			topic.TopicTypeID = Engine.Instance.TopicTypes.FromKeyword("Function").ID;

			Link link = new Engine.Links.Link();
			link.LanguageID = Engine.Instance.Languages.FromName("C#").ID;
			link.Type = Engine.Links.LinkType.NaturalDocs;
			bool linkIsPlainText = true;

			for (int i = 0; i < commands.Count; i++)
				{
				string command = commands[i];
				if (command.EndsWith(";"))
					{  command = command.Substring(0, command.Length - 1).TrimEnd();  }

				var match = GetPropertyRegex.Match(command);
				string target, property, value, valueString;

				if (match.Success)
					{
					target = match.Groups[1].ToString().ToLower();
					property = match.Groups[2].ToString().ToLower();
					value = match.Groups[3].ToString();

					if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
						{  
						valueString = value.Substring(1, value.Length - 2).Trim();  
						}
					else
						{  
						value = value.ToLower();
						valueString = null;  
						}
					}
				else
					{
					target = null;
					property = null;
					value = null;
					valueString = null;
					}

				var lcCommand = command.ToLower();
				
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

							var language = Engine.Instance.Languages.FromName(valueString);

							if (language == null)
								{  throw new Exception("\"" + valueString + "\" is not recognized as a language.");  }

							topic.LanguageID = language.ID;
							}
						else if (property == "keyword")
							{
							if (valueString == null)
								{  throw new Exception("Topic.Keyword must be set to a string value.");  }

							var topicType = Engine.Instance.TopicTypes.FromName(valueString);

							if (topicType == null)
								{  throw new Exception("\"" + valueString + "\" is not recognized as a keyword.");  }

							topic.TopicTypeID = topicType.ID;
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
								temp.Scope = SymbolString.FromPlainText_ParenthesisAlreadyRemoved(valueString);
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

							var language = Engine.Instance.Languages.FromName(valueString);

							if (language == null)
								{  throw new Exception("\"" + valueString + "\" is not recognized as a language.");  }

							link.LanguageID = language.ID;
							}
						else if (property == "type")
							{
							if (valueString == null)
								{  throw new Exception("Link.Type must be set to a string value.");  }
							
							string lcValueString = valueString.ToLower();

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
								temp.Scope = SymbolString.FromPlainText_ParenthesisAlreadyRemoved(valueString);
								link.Context = temp;
								}
							}
						else
							{  throw new Exception("\"" + property + "\" is not recognized as a link property.");  }
						// Leave lastWasLineBreak alone since we're not generating output.
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

						string parenthesis;
						SymbolString topicSymbol = SymbolString.FromPlainText(topic.Title, out parenthesis);

						var topicType = Engine.Instance.TopicTypes.FromID(topic.TopicTypeID);

						if (topicType.Scope == Engine.TopicTypes.TopicType.ScopeValue.Normal &&
							 topic.PrototypeContext.ScopeIsGlobal == false)
							{
							topicSymbol = topic.PrototypeContext.Scope + topicSymbol;  
							}

						topic.Symbol = topicSymbol;

						if (link.Type == LinkType.Type || link.Type == LinkType.ClassParent)
							{
							if (linkIsPlainText)
								{
								SymbolString linkSymbol = SymbolString.FromPlainText_ParenthesisAlreadyRemoved(link.TextOrSymbol);
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

						var topicLanguage = Engine.Instance.Languages.FromID(topic.LanguageID);
						topicType = Engine.Instance.TopicTypes.FromID(topic.TopicTypeID);

						output.AppendLine(topicLanguage.Name + " " + topicType.Name + " Topic: " + topic.Title);
						output.AppendLine("   Symbol: " + topic.Symbol.ToString().Replace(Engine.Symbols.SymbolString.SeparatorChar, '.'));

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

						var linkLanguage = Engine.Instance.Languages.FromID(link.LanguageID);

						output.AppendLine(linkLanguage.Name + " " + link.Type + " Link: " + link.TextOrSymbol.Replace(Engine.Symbols.SymbolString.SeparatorChar, '*'));

						if (link.Context.ScopeIsGlobal)
							{  output.AppendLine("   Scope: Global");  }
						else
							{  output.AppendLine("   Scope: " + link.Context.Scope.ToString().Replace(Engine.Symbols.SymbolString.SeparatorChar, '.'));  }

						output.AppendLine();


						// Show score

						List<Engine.Links.LinkInterpretation> interpretations = null;

						if (link.Type == LinkType.NaturalDocs)
							{
							string ignore;
							interpretations = Engine.Instance.Comments.NaturalDocsParser.LinkInterpretations(link.TextOrSymbol, 
																								Engine.Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowNamedLinks | 
																								Engine.Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowPluralsAndPossessives | 
																								Engine.Comments.Parsers.NaturalDocs.LinkInterpretationFlags.ExcludeLiteral,
																								out ignore);
							}

						long score = Engine.Instance.CodeDB.ScoreLink(link, topic, 0, interpretations);

						if (score <= 0)
							{  output.AppendLine("☓☓☓ No Match ☓☓☓");  }
						else
							{
							string scoreString = score.ToString("X16");

							output.AppendLine("Match score: " + scoreString.Substring(0, 4) + " " + scoreString.Substring(4, 4) +
																" " + scoreString.Substring(8, 4) + " " + scoreString.Substring(12, 4));

							// DEPENDENCY: This code depends on the format generated by Engine.CodeDB.ScoreLink().

							// 0LCEPPPP PPPPPPPP PPPPPPPP PPPPPPSS SSSSSSSS IIIIIIBb bbbbbbbb Rrrrrrr1

							// L - Whether the topic matches the link's language.
							// C - Whether the topic and link's capitalization match if it matters to the language.
							// E - Whether the text is an exact match with no plural or possessive conversions applied.
							// P - How well the parameters match.
							// S - How high on the scope list the symbol match is.
							// I - How high on the interpretation list (named/plural/possessive) the match is.
							// B - Whether the topic has a body
							// b - The length of the body divided by 16.
							// R - Whether the topic has a prototype.
							// r - The length of the prototype divided by 16.

							long LValue = (score & 0x4000000000000000) >> 62;
							long CValue = (score & 0x2000000000000000) >> 61;
							long EValue = (score & 0x1000000000000000) >> 60;
							long PValue = (score & 0x0FFFFFFC00000000) >> 34;
							long SValue = (score & 0x00000003FF000000) >> 24;
							long IValue = (score & 0x0000000000FC0000) >> 18;
							long BValue = (score & 0x0000000000020000) >> 17;
							long bValue = (score & 0x000000000001FF00) >> 8;
							long RValue = (score & 0x0000000000000080) >> 7;
							long rValue = (score & 0x000000000000007E) >> 1;

							output.AppendLine("   " + (LValue == 1 ? "☒" : "☐") + " - Language");
							output.AppendLine("   " + (CValue == 1 ? "☒" : "☐") + " - Capitalization");
							output.AppendLine("   " + (EValue == 1 ? "☒" : "☐") + " - Exact text");

							output.Append("   ");
							for (int shift = 24; shift >= 0; shift -= 2)
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

							output.AppendLine("   " + (1023 - SValue) + " - Scope index");
								output.AppendLine("      (" + SValue + " score)");
							output.AppendLine("   " + (63 - IValue) + " - Interpretation index");
								output.AppendLine("      (" + IValue + " score)");

							output.AppendLine("   " + (BValue == 1 ? "☒" : "☐") + " - Body");
							if (BValue == 1)
								{  
								output.Append("      (" + bValue + " score, " + (bValue * 16));

								if (bValue == 0x1FF)
									{  output.Append('+');  }
								else
									{  output.Append("-" + ((bValue * 16) + 15));  }

								output.AppendLine(" characters)");
								}

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

		protected static Regex GetPropertyRegex = new Regex(@"^[ \t]*(Topic|Link).([a-z]+)[ \t]*=[ \t]*(null|"".*"")[ \t]*$", 
																											RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		}
	}