/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.PowerShell
 * ____________________________________________________________________________
 *
 * Additional language support for PowerShell.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class PowerShell : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: PowerShell
		 */
		public PowerShell (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: SyntaxHighlight
		 */
		override public void SyntaxHighlight (Tokenizer source)
			{
			TokenIterator iterator = source.FirstToken;

			while (iterator.IsInBounds)
				{
				// Backticks cancel out everything, including # starting comments and " starting strings
				if (iterator.Character == '`')
					{  iterator.Next(2);  }

				else if (TryToSkipKeyword(ref iterator, ParseMode.SyntaxHighlight) ||
						   TryToSkipComment(ref iterator, ParseMode.SyntaxHighlight) ||
						   TryToSkipString(ref iterator, ParseMode.SyntaxHighlight) ||
						   TryToSkipNumber(ref iterator, ParseMode.SyntaxHighlight) ||
						   TryToSkipAttribute(ref iterator, ParseMode.SyntaxHighlight))
					{
					}
				else
					{  iterator.Next();  }
				}
			}


		/* Function: NormalizePrototype
		 */
		override protected string NormalizePrototype (Tokenizer input)
			{
			string normalizedInput = base.NormalizePrototype(input);

			// Rearrange attributes with types so that the type is last.  This lets prototypes format better.
			if (normalizedInput.IndexOf('[') != -1)
				{
				StringBuilder stringBuilder = new StringBuilder(normalizedInput.Length);
				Tokenizer tokenizer = new Tokenizer(normalizedInput);
				TokenIterator iterator = tokenizer.FirstToken;

				while (iterator.IsInBounds)
					{
					TokenIterator lookahead = iterator;

					if (TryToSkipAttributesWithType(ref lookahead, out int numberOfAttributes, out bool foundType,
																  out TokenIterator typeStart, out TokenIterator typeEnd))
						{
						// If it has a type and there's multiple attributes and the type isn't already the last one, rearrange them
						if (foundType && numberOfAttributes > 1 && typeEnd != lookahead)
							{
							// If the type isn't the first attribute, copy what's before it
							if (typeStart != iterator)
								{
								stringBuilder.Append(normalizedInput, iterator.RawTextIndex, typeStart.RawTextIndex - iterator.RawTextIndex);
								}

							// We already know it's not the last one, so copy what's after it
							stringBuilder.Append(normalizedInput, typeEnd.RawTextIndex, lookahead.RawTextIndex - typeEnd.RawTextIndex);

							// Now copy the type itself at the end
							stringBuilder.Append(normalizedInput, typeStart.RawTextIndex, typeEnd.RawTextIndex - typeStart.RawTextIndex);
							}

						// Don't need to rearrange anything, add it as is
						else
							{
							stringBuilder.Append(normalizedInput, iterator.RawTextIndex, lookahead.RawTextIndex - iterator.RawTextIndex);
							}

						iterator = lookahead;
						}

					else if (TryToSkipComment(ref lookahead) ||
							  TryToSkipString(ref lookahead))
						{
						stringBuilder.Append(normalizedInput, iterator.RawTextIndex, lookahead.RawTextIndex - iterator.RawTextIndex);
						iterator = lookahead;
						}

					else
						{
						stringBuilder.Append(normalizedInput, iterator.RawTextIndex, iterator.RawTextLength);
						iterator.Next();
						}
					}

				normalizedInput = stringBuilder.ToString();
				}

			return normalizedInput;
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */
		override public ParsedPrototype ParsePrototype (string stringPrototype, int commentTypeID)
			{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			ParsedPrototype parsedPrototype;


			// Mark any leading attributes.

			TokenIterator iterator = tokenizedPrototype.FirstToken;

			TryToSkipWhitespace(ref iterator, true, ParseMode.ParsePrototype);

			while (TryToSkipAttribute(ref iterator, ParseMode.ParsePrototype))
				{  TryToSkipWhitespace(ref iterator, true, ParseMode.ParsePrototype);  }


			// Search for the first opening bracket or brace.

			char closingBracket = '\0';

			while (iterator.IsInBounds)
				{
				if (iterator.Character == '(')
					{
					closingBracket = ')';
					break;
					}
				else if (iterator.Character == '{')
					{
					closingBracket = '}';
					break;
					}
				else if (TryToSkipComment(ref iterator) ||
						   TryToSkipString(ref iterator))
					{  }
				else
					{  iterator.Next();  }
				}


			// If we found brackets, it's either a function prototype or a class prototype that includes members.
			// Mark the delimiters.

			if (closingBracket != '\0')
				{
				iterator.PrototypeParsingType = PrototypeParsingType.StartOfParams;
				iterator.Next();

				while (iterator.IsInBounds)
					{
					if (iterator.Character == ',')
						{
						iterator.PrototypeParsingType = PrototypeParsingType.ParamSeparator;
						iterator.Next();
						}

					else if (iterator.Character == closingBracket)
						{
						iterator.PrototypeParsingType = PrototypeParsingType.EndOfParams;
						break;
						}

					// Unlike prototype detection, here we treat < as an opening bracket.  Since we're already in the parameter list
					// we shouldn't run into it as part of an operator overload, and we need it to not treat the comma in "template<a,b>"
					// as a parameter divider.
					else if (TryToSkipComment(ref iterator) ||
							   TryToSkipString(ref iterator) ||
							   TryToSkipAttribute(ref iterator) ||
							   TryToSkipBlock(ref iterator, true))
						{  }

					else
						{  iterator.Next();  }
					}


				// We have enough tokens marked to create the parsed prototype.  This will also let us iterate through the parameters
				// easily.

				parsedPrototype = new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID,
																		  parameterStyle: ParameterStyle.C, supportsImpliedTypes: false);


				// Set the main section to the last one, since any attributes present will each be in their own section.  Some can have
				// parameter lists and we don't want those confused for the actual parameter list.

				parsedPrototype.MainSectionIndex = parsedPrototype.Sections.Count - 1;


				// If there are any parameters, mark the tokens in them.

				if (parsedPrototype.NumberOfParameters > 0)
					{
					TokenIterator start, end;

					for (int i = 0; i < parsedPrototype.NumberOfParameters; i++)
						{
						parsedPrototype.GetParameter(i, out start, out end);
						MarkPowerShellParameter(start, end);
						}
					}
				}


			// If there's no brackets, it's a variable, property, or class.

			else
				{
				parsedPrototype = new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID,
																		  parameterStyle: ParameterStyle.C, supportsImpliedTypes: false);
				TokenIterator start = tokenizedPrototype.FirstToken;
				TokenIterator end = tokenizedPrototype.EndOfTokens;

				MarkPowerShellParameter(start, end);
				}

			return parsedPrototype;
			}


		/* Function: MarkPowerShellParameter
		 */
		protected void MarkPowerShellParameter (TokenIterator start, TokenIterator end)
			{
			TokenIterator iterator = start;
			iterator.NextPastWhitespace(end);


			// Attributes and type

			while (TryToSkipAttributesWithType(ref iterator, out int numberOfAttributes, out bool foundType,
															   out TokenIterator typeStart, out TokenIterator typeEnd, ParseMode.ParsePrototype))
				{
				iterator.NextPastWhitespace(end);
				}


			// Parameter Name

			if (iterator.Character == '$')
				{
				iterator.PrototypeParsingType = PrototypeParsingType.Name;
				iterator.Next();

				while (iterator < end &&
							(iterator.FundamentalType == FundamentalType.Text ||
							iterator.Character == ':' ||
							iterator.Character == '.' ||
							iterator.Character == '_'))
					{
					iterator.PrototypeParsingType = PrototypeParsingType.Name;
					iterator.Next();
					}

				iterator.NextPastWhitespace(end);
				}


			// Default values

			if (iterator.Character == '=')
				{
				iterator.PrototypeParsingType = PrototypeParsingType.DefaultValueSeparator;
				iterator.Next();
				iterator.NextPastWhitespace(end);

				TokenIterator endOfDefaultValue = end;

				TokenIterator lookbehind = endOfDefaultValue;
				lookbehind.Previous();

				while (lookbehind >= iterator && lookbehind.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
					{
					endOfDefaultValue = lookbehind;
					lookbehind.Previous();
					}

				endOfDefaultValue.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, iterator);

				if (iterator < endOfDefaultValue)
					{  iterator.SetPrototypeParsingTypeBetween(endOfDefaultValue, PrototypeParsingType.DefaultValue);  }
				}
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToSkipKeyword
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipKeyword (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.FundamentalType != FundamentalType.Text &&
				iterator.Character != '-' &&
				iterator.Character != '$')
				{  return false;  }

			TokenIterator lookahead = iterator;

			if (lookahead.Character == '-' || lookahead.Character == '$')
				{  lookahead.Next();  }

			if (lookahead.FundamentalType != FundamentalType.Text)
				{  return false;  }

			lookahead.Next();

			if (lookahead.FundamentalType == FundamentalType.Text ||
				lookahead.Character == '_' ||
				lookahead.Character == '-')
				{  return false;  }

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			if (lookbehind.FundamentalType == FundamentalType.Text ||
				lookbehind.Character == '-' ||
				lookbehind.Character == '$' ||
				lookbehind.Character == '_')
				{  return false;  }

			string keyword = iterator.TextBetween(lookahead);

			if (!powershellKeywords.Contains(keyword))
				{  return false;  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Keyword);  }

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipString
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipString (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '\'' && iterator.Character != '\"')
				{  return false;  }

			char openingCharacter = iterator.Character;

			TokenIterator lookahead = iterator;
			lookahead.Next();

			while (lookahead.IsInBounds)
				{
				if (lookahead.Character == '`')
					{  lookahead.Next(2);  }

				else if (lookahead.Character == openingCharacter)
					{
					lookahead.Next();
					break;
					}

				else
					{  lookahead.Next();  }
				}

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.String);  }

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipAttribute
		 *
		 * If the iterator is at the beginning of an attribute, skips it and returns true.  Otherwise returns false and doesn't change the iterator.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttribute (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '[')
				{  return false;  }

			TokenIterator lookahead = iterator;

			if (!TryToSkipBlock(ref lookahead, false))
				{  return false;  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Metadata);  }

			if (mode == ParseMode.ParsePrototype)
				{
				iterator.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;

				TokenIterator closingBracket = lookahead;
				closingBracket.Previous();
				closingBracket.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipAttributeOrType
		 *
		 * If the iterator is at the beginning of an attribute, skips it and returns true.  Otherwise returns false and doesn't change the iterator.
		 *
		 * This version of the function also accounts for the fact that the attribute may be a type such as [string].  Since that can't be 100%
		 * determined it returns a score representing the likelihood of the attribute being a type.  For a value of zero it is definitely not a type,
		 * such as [Parameter(Mandatory=$true)].  For a non-zero value it could be a type, with higher scores representing higher confidences.
		 * For a parameter or a variable you should use the highest non-zero attribute as the type.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttributeOrType (ref TokenIterator iterator, out int typeScore, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator attributeStart = iterator;

			if (!TryToSkipAttribute(ref iterator, mode))
				{
				typeScore = 0;
				return false;
				}

			// Skip the opening bracket
			attributeStart.Next();
			TryToSkipWhitespace(ref attributeStart, includeLineBreaks: true);

			if (attributeStart.FundamentalType == FundamentalType.Text)
				{
				string attributeString = attributeStart.ToString();

				if (powershellDefiniteTypeNames.Contains(attributeString))
					{  typeScore = 100;  }
				else if (powershellDefiniteNotTypeNames.Contains(attributeString) ||
						  attributeString.StartsWith("Validate", StringComparison.InvariantCultureIgnoreCase) ||
						  attributeString.StartsWith("Allow", StringComparison.InvariantCultureIgnoreCase))
					{  typeScore = 0;  }
				else
					{
					// Text is maybe an attribute, so start at 50
					typeScore = 50;
					TokenIterator lookahead = attributeStart;

					for (;;)
						{
						lookahead.Next();

						if (lookahead >= iterator ||
							lookahead.Character == ']' || // end of attribute
							lookahead.Character == '[' || // start of something like List[int]
							lookahead.Character == '=') // start of default value
							{  break;  }

						else if (lookahead.FundamentalType == FundamentalType.Text ||
								  lookahead.Character == '.' ||
								  lookahead.Character == ':' ||
								  lookahead.Character == '_')
							{  lookahead.Next();  }

						else if (lookahead.Character == '.' ||
								  lookahead.Character == ':' ||
								  lookahead.Character == '_')
							{  lookahead.Next();  }

						else if (TryToSkipWhitespace(ref lookahead, includeLineBreaks: true))
							{
							// Only allow whitespace if the next thing is the end of the attribute or the default value
							if (lookahead.Character == ']' ||
								lookahead.Character == '=')
								{  break;  }
							else
								{
								typeScore = 0;
								break;
								}
							}

						else // fail at open parenthesis or unexpected symbol
							{
							typeScore = 0;
							break;
							}
						}
					}
				}

			else // not text
				{  typeScore = 0;  }

			return true;
			}


		/* Function: TryToSkipAttributesWithType
		 *
		 * If the iterator is at the beginning of one or more attributes, skips them all and returns true.  Otherwise returns false and doesn't
		 * affect the iterator.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Type attributes will be marked as <PrototypeParsingType.Type>.  Other attributes will be marked as modifiers.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttributesWithType (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			// Ignore out parameters
			return TryToSkipAttributesWithType (ref iterator, out _, out _, out _, out _, mode);
			}


		/* Function: TryToSkipAttributesWithType
		 *
		 * If the iterator is at the beginning of one or more attributes, skips them all and returns true.  Otherwise returns false and doesn't
		 * affect the iterator.  If one of them is a type it will return it.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Type attributes will be marked as <PrototypeParsingType.Type>.  Other attributes will be marked as modifiers.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttributesWithType (ref TokenIterator iterator, out int numberOfAttributes,
																		 out bool foundType, out TokenIterator typeStart, out TokenIterator typeEnd,
																		 ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;
			numberOfAttributes = 0;

			int bestTypeScore = 0;
			typeStart = lookahead;
			typeEnd = lookahead;

			for (;;)
				{
				TokenIterator startOfAttribute = lookahead;

				if (TryToSkipAttributeOrType(ref lookahead, out int typeScore, mode))
					{
					if (typeScore > bestTypeScore)
						{
						bestTypeScore = typeScore;
						typeStart = iterator;
						typeEnd = lookahead;
						foundType = true;
						}

					iterator = lookahead;
					numberOfAttributes++;
					}
				else
					{  break;  }

				TryToSkipWhitespace(ref lookahead, includeLineBreaks: true, mode);
				}

			foundType = (bestTypeScore > 0);

			if (foundType && mode == ParseMode.ParsePrototype)
				{
				// Switch from type modifier to type
				TokenIterator openingBracket = typeStart;

				TokenIterator closingBracket = typeEnd;
				closingBracket.Previous();

				TokenIterator startOfTypeContent = openingBracket;
				startOfTypeContent.Next();
				startOfTypeContent.NextPastWhitespace(closingBracket);

				TokenIterator endOfTypeContent = closingBracket;
				endOfTypeContent.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfTypeContent);

				// We use TypeModifier instead of Opening/ClosingTypeModifier so it doesn't skip the middle content
				openingBracket.PrototypeParsingType = PrototypeParsingType.TypeModifier;
				closingBracket.PrototypeParsingType = PrototypeParsingType.TypeModifier;
				startOfTypeContent.SetPrototypeParsingTypeBetween(endOfTypeContent, PrototypeParsingType.Type);
				}

			return (numberOfAttributes > 0);
			}



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: powershellKeywords
		 */
		static protected StringSet powershellKeywords = new StringSet (KeySettings.IgnoreCase, new string[] {

			"begin", "break", "catch", "class", "continue", "data", "define", "do", "dynamicparam", "else", "elseif", "end",
			"exit", "filter", "finally", "for", "foreach", "from", "function", "if", "in", "inlinescript", "parallel", "param", "process",
			"return", "switch", "throw", "trap", "try", "until", "using", "var", "while", "workflow",

			"-and", "-band", "-bnot", "-bor", "-bxor", "-not", "-or", "-xor",

			"-as", "-ccontains", "-ceq", "-cge", "-cgt", "-cle", "-clike", "-clt", "-cmatch", "-cne", "-cnotcontains", "-cnotlike",
			"-cnotmatch", "-contains", "-creplace", "-csplit", "-eq", "-ge", "-gt", "-icontains", "-ieq", "-ige", "-igt", "-ile", "-ilike",
			"-ilt", "-imatch", "-in", "-ine", "-inotcontains", "-inotlike", "-inotmatch", "-ireplace", "-is", "-isnot", "-isplit", "-join",
			"-le", "-like", "-lt", "-match", "-ne", "-notcontains", "-notin", "-notlike", "-notmatch", "-replace", "-shr", "-split",

			"$true", "$false", "$null", "$args", "$this"

			});

		/* var: powershellDefiniteTypeNames
		 * Words that if they appear as an attribute, we're sure they're a type.
		 */
		static protected StringSet powershellDefiniteTypeNames = new StringSet (KeySettings.IgnoreCase, new string[] {

			"object", "bool", "char", "int", "long", "byte", "float", "double", "single", "decimal", "switch", "string", "array",
			"hashtable", "xml", "regex", "scriptblock", "credential", "PSCredential", "DateTime"

			});

		/* var: powershellDefiniteNotTypeNames
		 * Words that if they appear as an attribute, we're sure they're _not_ a type.
		 */
		static protected StringSet powershellDefiniteNotTypeNames = new StringSet (KeySettings.IgnoreCase, new string[] {

			"Alias", "Cmdlet", "OutputType", "Parameter"
			// Validate* and Allow* are handled in TryToSkipAttributeOrType()

			});

		}
	}
