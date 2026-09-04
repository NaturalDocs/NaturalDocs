/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.C
 * ____________________________________________________________________________
 *
 * Additional language support for C and C++.
 *
 * Language Version:
 *
 *		The parser is primarily based on C23 and C++23, the latest releases as of June 2026.
 *
 * Primary Resources:
 *		- <C Language Reference: https://cppreference.com/c/language>
 *		- <C++ Language Reference: https://cppreference.com/cpp/language>
 *		- <Hyperlinked BNF Grammar: https://alx71hub.github.io/hcb/>
 *
 *	Additional Topics:
 *		- <Microsoft-Specific Modifiers: https://github.com/MicrosoftDocs/cpp-docs/blob/main/docs/cpp/microsoft-specific-modifiers.md>
 *			- <__declspec: https://github.com/MicrosoftDocs/cpp-docs/blob/main/docs/cpp/declspec.md>
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class C : Parser
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: TemplateSignatureType
		 * Definition - The signature of a template definition, such as "template<class T>".
		 * Instantiation - The signature of a template instantiation, such as "List<int>".
		 */
		public enum TemplateSignatureType: byte
			{  Definition, Instantiation  }



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: C
		 */
		public C (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: SyntaxHighlight
		 */
		override public void SyntaxHighlight (TokenIterator start, TokenIterator end)
			{
			TokenIterator iterator = start;

			TokenIterator lastCodeToken = iterator.Tokenizer.EndOfTokens;  // Default to out of bounds

			while (iterator < end)
				{
				if (TryToSkipPreprocessingDirective(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipAttributes(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipKeyword(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipComment(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipString(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipNumber(ref iterator, ParseMode.SyntaxHighlight))
					{
					}
				else
					{
					iterator.Next();
					}
				}
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */
		override public ParsedPrototype ParsePrototype(string stringPrototype, int commentTypeID)
			{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator iterator = tokenizedPrototype.FirstToken;

			if (TryToSkipFunction(ref iterator, ParseMode.ParsePrototype) ||
				TryToSkipEnum(ref iterator, ParseMode.ParsePrototype))
				{
				return new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID, engineInstance);
				}
			else
				{
				return base.ParsePrototype(stringPrototype, commentTypeID);
				}
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: IsPartOfLongerIdentifier
		 * Returns whether the <TokenIterator> is on token that's part of a longer identifier, such as by being next to an underscore.
		 * This is primarily used to validate keywords after checking the contents of the token against a keyword list, so that "input"
		 * by itself will be distinguished from "_input" or similar.
		 */
		protected bool IsPartOfLongerIdentifier (TokenIterator iterator)
			{
			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.FundamentalType == FundamentalType.Text ||
				lookahead.Character == '_')
				{  return true;  }

			// Just use iterator as a lookbehind instead of creating another one
			iterator.Previous();

			if (iterator.FundamentalType == FundamentalType.Text ||
				iterator.Character == '_')
				{  return true;  }

			return false;
			}


		/* Function: IsOnKeyword
		 *
		 * Returns whether the <TokenIterator> is on the passed keyword, making sure there are no other identifier tokens
		 * before or after it.  This allows us to be sure an iterator on "input" isn't actually on "_input" or similar.  This function
		 * assumes keywords are only one text token.
		 *
		 * If you have multiple keywords to test against, it is more efficient to use one of the <IsOnAnyKeyword()> functions.
		 */
		public bool IsOnKeyword (TokenIterator iterator, string keyword)
			{
			return (iterator.MatchesToken(keyword) &&
					   !IsPartOfLongerIdentifier(iterator));
			}


		/* Function: IsOnAnyKeyword
		 *
		 * Returns whether the <TokenIterator> is on the passed keyword, making sure there are no other identifier tokens
		 * before or after it.  This allows us to be sure an iterator on "input" isn't actually on "_input" or similar.  This function
		 * assumes keywords are only one text token.
		 */
		public bool IsOnAnyKeyword (TokenIterator iterator, params string[] keywords)
			{
			return (iterator.MatchesAnyAcrossTokens(keywords, true) != -1 &&
					   !IsPartOfLongerIdentifier(iterator));
			}


		/* Function: TryToSkipFunction
		 *
		 * If the iterator is on a function definition, moves it past it and returns true.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipFunction (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;


			// Attributes

			if (TryToSkipAttributes(ref lookahead, mode, PrototypeParsingType.StartOfPrototypeSection))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Pass 1: Count the number of words before the parentheses.  We need to accept parentheses that appear after macros
			// like __declspec() though, so find the last one before the body, the end of the declaration, or a modifier that follows the
			// function parameters like "noexcept".

			TokenIterator startOfWords = lookahead;
			int wordCount = 0;
			bool lastWordHadParentheses = false;

			while (lookahead.IsInBounds)
				{
				// Fail on keywords that show we're not on a function
				if (IsOnAnyKeyword(lookahead, "class", "struct", "enum"))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				// Symbols that end the declaration or appear after the parameters
				else if (lookahead.Character == ';' ||  // End of declaration
						   lookahead.Character == '{' ||  // Body
						   lookahead.Character == '=' ||  // = 0, = default, etc.
						   lookahead.Character == ':' ||  // Calls to the base constructor
						   lookahead.MatchesAcrossTokens("->"))  // Auto return values
					{  break;  }

				// Keywords that follow the parameters
				else if (IsOnAnyKeyword(lookahead, "volatile", "try", "throw", "noexcept"))
					{  break;  }

				// xxx "const", "&", and "&&" can appear before a type and after the parentheses

				else if (TryToSkipTypeWord(ref lookahead, includeTemplateSignatures: true))
					{
					wordCount++;
					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == '(')
						{
						lookahead.Next();
						lastWordHadParentheses = GenericSkipUntilAfter(ref lookahead, ')', angleBracketsAsBlocks: true, skipToEndIfNotFound: false);
						TryToSkipWhitespace(ref lookahead);
						}
					else
						{  lastWordHadParentheses = false;  }
					}

				else
					{  break;  }
				}

			if (wordCount == 0 ||
				lastWordHadParentheses == false)
				{  return false;  }


			// Pass 2: Mark the words before the parentheses.  The order of words goes [modifier] [modifier] [type] [name], starting
			// from the right.  We can skip this if we're just iterating over it.

			// These will only be set if we're on ParseMode.ParsePrototype
			TokenIterator openingParen = lookahead;
			TokenIterator closingParen = lookahead;

			if (mode == ParseMode.ParsePrototype)
				{
				lookahead = startOfWords;
				TokenIterator wordStart, wordEnd;

				while (wordCount > 0)
					{
					wordStart = lookahead;

					TryToSkipTypeWord(ref lookahead, includeTemplateSignatures: true);
					wordEnd = lookahead;

					// Process the word we found
					if (wordCount >= 3)
						{
						wordStart.SetPrototypeParsingTypeBetween(wordEnd, PrototypeParsingType.TypeModifier);
						}
					else if (wordCount == 2)
						{
						MarkType(wordStart, wordEnd);
						}
					else if (wordCount == 1)
						{
						MarkName(wordStart, wordEnd);
						}

					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == '(')
						{
						openingParen = lookahead;
						lookahead.Next();

						if (GenericSkipUntilOn(ref lookahead, ')', angleBracketsAsBlocks: true, skipToEndIfNotFound: false))
							{
							closingParen = lookahead;

							// Process the parentheses after the word we found
							if (wordCount >= 2)
								{
								openingParen.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;
								closingParen.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
								}
							else if (wordCount == 1)
								{
								openingParen.PrototypeParsingType = PrototypeParsingType.StartOfParams;
								closingParen.PrototypeParsingType = PrototypeParsingType.EndOfParams;
								}

							lookahead.Next();
							}

						TryToSkipWhitespace(ref lookahead);
						}

					wordCount--;
					}
				}


			// xxx process anything following the parameters


			// Process the parameters.  We can skip this if we're just iterating over it.

			if (mode == ParseMode.ParsePrototype &&
				closingParen > openingParen)
				{
				TokenIterator temp = openingParen;

				if (!TryToSkipParameters(ref temp, mode))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}
				}


			return true;
			}


		/* Function: TryToSkipParameters
		 *
		 * Tries to move the iterator past a comma-separated list of parameters in parentheses.  The iterator must be on the opening
		 * parenthesis.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipParameters (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '(')
				{  return false;  }


			// Opening paren

			TokenIterator lookahead = iterator;

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfParams;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Parameter list

			while (lookahead.IsInBounds && lookahead.Character != ')')
				{
				if (lookahead.Character == ',')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				else if (TryToSkipParameter(ref lookahead, mode))
					{
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{  break;  }
				}


			// Closing paren

			if (lookahead.Character == ')')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.EndOfParams;  }

				lookahead.Next();
				iterator = lookahead;
				return true;
				}
			else
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}
			}


		/* Function: TryToSkipParameter
		 *
		 * Tries to move the iterator past a parameter, such as "int x" or "List<int> y = null".  The parameter ends at a comma or the
		 * closing parenthesis.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipParameter (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;


			// Attributes

			if (TryToSkipAttributes(ref lookahead, mode, PrototypeParsingType.TypeModifier))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Pass 1: Count the number of words in the parameter declaration, excluding the default value if it exists.  We may need to
			// accept parentheses that appear after macros, so as long as the last word doesn't end with parentheses it's okay to include
			// as a modifier.

			TokenIterator startOfWords = lookahead;
			int wordCount = 0;
			bool lastWordHadParentheses = false;

			while (lookahead.IsInBounds)
				{
				// Symbols that end this part of the parameter
				if (lookahead.Character == ',' ||  // End of parameter
					lookahead.Character == ')' ||  // End of all parameters
					lookahead.Character == '=')  // Default value
					{  break;  }

				else if (TryToSkipTypeWord(ref lookahead, includeTemplateSignatures: true))
					{
					wordCount++;
					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == '(')
						{
						lookahead.Next();
						lastWordHadParentheses = GenericSkipUntilAfter(ref lookahead, ')', angleBracketsAsBlocks: true, skipToEndIfNotFound: false);
						TryToSkipWhitespace(ref lookahead);
						}
					else
						{  lastWordHadParentheses = false;  }
					}

				else
					{  break;  }
				}

			if (wordCount == 0 ||
				lastWordHadParentheses)
				{  return false;  }


			// Pass 2: Mark the words.  The order of words goes [modifier] [modifier] [type] [name], starting from the right.  We can
			// skip this if we're just iterating over it.

			if (mode == ParseMode.ParsePrototype)
				{
				lookahead = startOfWords;
				TokenIterator wordStart, wordEnd;

				while (wordCount > 0)
					{
					wordStart = lookahead;

					TryToSkipTypeWord(ref lookahead, includeTemplateSignatures: true);
					wordEnd = lookahead;

					// Process the word we found
					if (wordCount >= 3)
						{
						wordStart.SetPrototypeParsingTypeBetween(wordEnd, PrototypeParsingType.TypeModifier);
						}
					else if (wordCount == 2)
						{
						MarkType(wordStart, wordEnd);
						}
					else if (wordCount == 1)
						{
						MarkName(wordStart, wordEnd);
						}

					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == '(')
						{
						TokenIterator openingParen = lookahead;
						lookahead.Next();

						if (GenericSkipUntilOn(ref lookahead, ')', angleBracketsAsBlocks: true, skipToEndIfNotFound: false))
							{
							openingParen.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;
							lookahead.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;

							lookahead.Next();
							}

						TryToSkipWhitespace(ref lookahead);
						}

					wordCount--;
					}
				}


			// Handle the default value

			if (lookahead.Character == '=')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.DefaultValueSeparator;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				TokenIterator startOfDefaultValue = lookahead;

				while (lookahead.IsInBounds &&
						  lookahead.Character != ',' &&
						  lookahead.Character != ')')
					{  GenericSkip(ref lookahead);  }

				if (mode == ParseMode.ParsePrototype)
					{
					TokenIterator endOfDefaultValue = lookahead;
					endOfDefaultValue.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

					if (endOfDefaultValue > startOfDefaultValue)
						{  startOfDefaultValue.SetPrototypeParsingTypeBetween(endOfDefaultValue, PrototypeParsingType.DefaultValue);  }
					}
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipEnum
		 *
		 * If the iterator is on an enum definition, moves it past it and returns true.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipEnum (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;


			// Attributes before the keyword

			if (TryToSkipAttributes(ref lookahead, mode, PrototypeParsingType.StartOfPrototypeSection))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword
			// Can be "enum", "class enum", or "struct enum"

			if (IsOnAnyKeyword(lookahead, "class", "struct"))
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}

			if (!IsOnKeyword(lookahead, "enum"))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Attributes after the keyword

			if (TryToSkipAttributes(ref lookahead, mode, PrototypeParsingType.StartOfPrototypeSection))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Name

			if (!TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			// Optional type

			if (lookahead.Character == ':')
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				TokenIterator endOfType = lookahead;

				while (endOfType.IsInBounds &&
							endOfType.Character != ';' &&
							endOfType.Character != '{')
					{  GenericSkip(ref endOfType, angleBracketsAsBlocks: true);  }

				if (mode == ParseMode.ParsePrototype)
					{  MarkTypeAndModifiers(lookahead, endOfType);  }

				lookahead = endOfType;
				}


			// At this point we're only parsing prototypes so there will not be a body and we can stop here.  Documenting this in
			// case it changes in the future.

			return true;
			}


		/* Function: TryToSkipTemplateSignature
		 *
		 * Tries to move the iterator past a template signature, such as "<int>" in "List<int>".  It can handle nested templates.  Set the
		 * <TemplateSignatureType> to set whether it's handling a definition, such as "template<class T>", or an instantiation, such as
		 * "List<int>".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- When using <TemplateSignatureType.Instantiation>, it will mark tokens with these types, including in nested templates:
		 *				- <PrototypeParsingType.OpeningTypeModifier>
		 *				- <PrototypeParsingType.ClosingTypeModifier>
		 *				- <PrototypeParsingType.Type>
		 *				- <PrototypeParsingType.TypeQualifier>
		 *				- <PrototypeParsingType.TypeModifier>
		 *			- When using <TemplateSignatureType.Definition>, it will mark everything with these types:
		 *				- <PrototypeParsingType.OpeningParamModifier>
		 *				- <PrototypeParsingType.ClosingParamModifier>
		 *				- <PrototypeParsingType.Name>
		 *		- <ParseMode.ParseClassPrototype>
		 *			- All tokens will be marked with <ClassPrototypeParsingType.TemplateSuffix>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipTemplateSignature (ref TokenIterator iterator, TemplateSignatureType signatureType,
																		ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '<')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			// xxx still need to parse contents properly
			if (!GenericSkipUntilOn(ref lookahead, '>', angleBracketsAsBlocks: true))
				{  return false;  }

			if (mode == ParseMode.ParseClassPrototype)
				{
				lookahead.Next();
				iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.TemplateSuffix);
				}
			else if (mode == ParseMode.ParsePrototype)
				{
				if (signatureType == TemplateSignatureType.Instantiation)
					{
					iterator.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;
					lookahead.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
					}
				else if (signatureType == TemplateSignatureType.Definition)
					{
					iterator.PrototypeParsingType = PrototypeParsingType.OpeningParamModifier;
					lookahead.PrototypeParsingType = PrototypeParsingType.ClosingParamModifier;
					}
				else
					{  throw new NotImplementedException();  }

				lookahead.Next();
				}
			else
				{  lookahead.Next();  }

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipMetadata
		 *
		 * Override to support detecting attributes as metadata.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- This will be the equivalent of calling <TryToSkipAttributes()> with <PrototypeParsingType.StartOfPrototypeSection>.  If
		 *			  you need a different interpretation call <TryToSkipAttributes()> directly.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark the first one with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and the rest with
		 *			  <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipMetadata (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			return TryToSkipAttributes(ref iterator, mode, PrototypeParsingType.StartOfPrototypeSection);
			}


		/* Function: TryToSkipAttributes
		 *
		 * Tries to move the iterator past one or more attributes like "[[deprecated]]".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.TypeModifier>,
		 *			  <PrototypeParsingType.ParamModifier>, or <PrototypeParsingType.StartOfPrototypeSection>.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark the first one with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and the rest with
		 *			  <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttributes (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
															PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier)
			{
			if (TryToSkipAttribute(ref iterator, mode, prototypeParsingType))
				{
				TokenIterator lookahead = iterator;

				for (;;)
					{
					TryToSkipWhitespace(ref lookahead, true, mode);

					if (TryToSkipAttribute(ref lookahead, mode, prototypeParsingType))
						{  iterator = lookahead;  }
					else
						{  break;  }
					}

				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToSkipAttribute
		 *
		 * Tries to move the iterator past a single attribute.  This can be a bracketed attribute like "[[deprecated]]", an underscored attribute
		 * like "__attribute__((noinline))", or a declspec attribute like "__declspec(noinline)".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.TypeModifier>,
		 *			  <PrototypeParsingType.ParamModifier>, or <PrototypeParsingType.StartOfPrototypeSection>.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark the first one with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and the rest with
		 *			  <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttribute (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
														  PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier)
			{
			return (TryToSkipBracketedAttribute(ref iterator, mode, prototypeParsingType) ||
					   TryToSkipUnderscoredAttribute(ref iterator, mode, prototypeParsingType) ||
					   TryToSkipDeclSpecAttribute(ref iterator, mode, prototypeParsingType));
			}


		/* Function: TryToSkipBracketedAttribute
		 *
		 * Tries to move the iterator past a single bracketed attribute like "[[deprecated]]".  It will also handle list attributes like "[[attrA, attrB]]".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.TypeModifier>,
		 *			  <PrototypeParsingType.ParamModifier>, or <PrototypeParsingType.StartOfPrototypeSection>.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark the first one with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and the rest with
		 *			  <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipBracketedAttribute (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
																		PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier)
			{
			// According to the spec double opening brackets aren't allowed anywhere else, so we don't have to worry about this being part of
			// an array signature or something.  However, whitespace is allowed between them.

			if (iterator.Character != '[')
				{  return false;  }

			TokenIterator lookahead = iterator;

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			if (lookahead.Character != '[')
				{  return false;  }

			// If we're making a prototype section, the outermost [ and ] will be the start and end.  If it's a list of attributes we'll format them
			// as parameters, marking the inner [ and ] as the start and end of parameters symbols.  However, if there's a "using:" statement
			// we'll use its colon as the start of parameters instead.

			// Also, C++ doesn't let you reference parameters by name (like "Function(name: value)"), only by position, so we don't need to
			// reserve parameter formatting for that.

			TokenIterator startOfParams = lookahead;

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			bool formatAsList = false;


			// Using

			if (lookahead.MatchesToken("using"))
				{
				TokenIterator startOfUsing = lookahead;
				bool validUsingSyntax = false;

				lookahead.Next();

				if (TryToSkipWhitespace(ref lookahead) &&
					TryToSkipIdentifier(ref lookahead))
					{
					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == ':')
						{
						startOfParams = lookahead;
						lookahead.Next();

						if (lookahead.Character != ':')
							{  validUsingSyntax = true;  }

						TryToSkipWhitespace(ref lookahead);
						}
					}

				// Reset the position so we can reparse it as a name, such as if it was "using_attr" instead of "using:"
				if (!validUsingSyntax)
					{  lookahead = startOfUsing;  }
				}


			// Content

			TokenIterator firstClosingBracket, secondClosingBracket;

			for (;;)
				{
				if (!lookahead.IsInBounds)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				else if (lookahead.Character == ']')
					{
					firstClosingBracket = lookahead;

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == ']')
						{
						secondClosingBracket = lookahead;

						lookahead.Next();
						break;
						}
					}

				else if (TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
					{
					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == '(')
						{
						lookahead.Next();

						if (!GenericSkipUntilAfter(ref lookahead, ')'))
							{
							ResetTokensBetween(iterator, lookahead, mode);
							return false;
							}

						TryToSkipWhitespace(ref lookahead);
						}
					}

				else if (lookahead.Character == ',')
					{
					if (mode == ParseMode.ParsePrototype && prototypeParsingType == PrototypeParsingType.StartOfPrototypeSection)
						{
						formatAsList = true;
						lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;
						}

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}

				else
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}
				}


			// Parameter contents and separators should already be marked.

			if (mode == ParseMode.SyntaxHighlight)
				{
				iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Metadata);
				}

			else if (mode == ParseMode.ParsePrototype)
				{
				if (prototypeParsingType == PrototypeParsingType.StartOfPrototypeSection)
					{
					iterator.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;

					if (formatAsList)
						{
						startOfParams.PrototypeParsingType = PrototypeParsingType.StartOfMetadataParams;
						firstClosingBracket.PrototypeParsingType = PrototypeParsingType.EndOfMetadataParams;
						}

					secondClosingBracket.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;
					}
				else if (prototypeParsingType == PrototypeParsingType.TypeModifier ||
						  prototypeParsingType == PrototypeParsingType.OpeningTypeModifier)
					{
					iterator.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;
					secondClosingBracket.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
					}
				else
					{
					iterator.SetPrototypeParsingTypeBetween(lookahead, prototypeParsingType);
					}
				}

			else if (mode == ParseMode.ParseClassPrototype)
				{
				iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;
				iterator.Next();

				iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.PrePrototypeLine);
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipUnderscoredAttribute
		 *
		 * Tries to move the iterator past a single underscored attribute like "__attribute__((noinline))".  It will also handle list attributes like
		 * "__attribute__((noinline, section("SectionName")))".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.TypeModifier>,
		 *			  <PrototypeParsingType.ParamModifier>, or <PrototypeParsingType.StartOfPrototypeSection>.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark the first one with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and the rest with
		 *			  <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipUnderscoredAttribute (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
																			PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier)
			{
			// According to the spec double opening parentheses are required, not just a convention.  However, whitespace is allowed between
			// them.
			if (!iterator.MatchesAcrossTokens("__attribute__"))
				{  return false;  }

			TokenIterator lookahead = iterator;

			lookahead.NextByCharacters(13);
			TryToSkipWhitespace(ref lookahead);

			if (lookahead.Character != '(')
				{  return false;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			if (lookahead.Character != '(')
				{  return false;  }

			TokenIterator secondOpeningParen = lookahead;

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			bool formatAsList = false;


			// Content in parentheses

			TokenIterator startOfParameter = lookahead;
			TokenIterator firstClosingParen, secondClosingParen;

			for (;;)
				{
				if (!lookahead.IsInBounds)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				else if (lookahead.Character == ')')
					{
					firstClosingParen = lookahead;

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == ')')
						{
						secondClosingParen = lookahead;

						if (formatAsList && mode == ParseMode.ParsePrototype && prototypeParsingType == PrototypeParsingType.StartOfPrototypeSection)
							{
							// Mark the last parameter contents
							TokenIterator endOfParameter = firstClosingParen;
							endOfParameter.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

							startOfParameter.SetPrototypeParsingTypeBetween(endOfParameter, PrototypeParsingType.PropertyValue);
							}

						lookahead.Next();
						break;
						}
					}

				else if (lookahead.Character == ',')
					{
					if (mode == ParseMode.ParsePrototype && prototypeParsingType == PrototypeParsingType.StartOfPrototypeSection)
						{
						formatAsList = true;
						lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;

						TokenIterator endOfParameter = lookahead;
						endOfParameter.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

						if (endOfParameter > startOfParameter)
							{  startOfParameter.SetPrototypeParsingTypeBetween(endOfParameter, PrototypeParsingType.PropertyValue);  }
						}

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);

					startOfParameter = lookahead;
					}

				else
					{
					GenericSkip(ref lookahead);
					TryToSkipWhitespace(ref lookahead);
					}
				}


			// Parameter contents and separators should already be marked.

			if (mode == ParseMode.SyntaxHighlight)
				{
				iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Metadata);
				}

			else if (mode == ParseMode.ParsePrototype)
				{
				if (prototypeParsingType == PrototypeParsingType.StartOfPrototypeSection)
					{
					iterator.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;

					if (formatAsList)
						{
						secondOpeningParen.PrototypeParsingType = PrototypeParsingType.StartOfMetadataParams;
						firstClosingParen.PrototypeParsingType = PrototypeParsingType.EndOfMetadataParams;
						}

					secondClosingParen.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;
					}
				else
					{
					iterator.SetPrototypeParsingTypeBetween(lookahead, prototypeParsingType);
					}
				}

			else if (mode == ParseMode.ParseClassPrototype)
				{
				iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;
				iterator.Next();

				iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.PrePrototypeLine);
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipDeclSpecAttribute
		 *
		 * Tries to move the iterator past a single declspec attribute like "__declspec(noinline)".  It will also handle list attributes like
		 * "__declspec(noinline, code_seg("SectionName"))".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.TypeModifier>,
		 *			  <PrototypeParsingType.ParamModifier>, or <PrototypeParsingType.StartOfPrototypeSection>.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark the first one with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and the rest with
		 *			  <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDeclSpecAttribute (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
																	   PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier)
			{
			if (!iterator.MatchesAcrossTokens("__declspec"))
				{  return false;  }

			TokenIterator lookahead = iterator;

			lookahead.NextByCharacters(10);
			TryToSkipWhitespace(ref lookahead);

			if (lookahead.Character != '(')
				{  return false;  }

			TokenIterator openingParen = lookahead;

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			bool formatAsList = false;


			// Content in parentheses

			TokenIterator startOfParameter = lookahead;
			TokenIterator closingParen;

			for (;;)
				{
				if (!lookahead.IsInBounds)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				else if (lookahead.Character == ')')
					{
					closingParen = lookahead;

					if (formatAsList && mode == ParseMode.ParsePrototype && prototypeParsingType == PrototypeParsingType.StartOfPrototypeSection)
						{
						// Mark the last parameter contents
						TokenIterator endOfParameter = closingParen;
						endOfParameter.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

						if (endOfParameter > startOfParameter)
							{  startOfParameter.SetPrototypeParsingTypeBetween(endOfParameter, PrototypeParsingType.PropertyValue);  }
						}

					lookahead.Next();
					break;
					}

				else if (lookahead.FundamentalType == FundamentalType.Whitespace ||
						   lookahead.FundamentalType == FundamentalType.LineBreak)
					{
					TokenIterator afterWhitespace = lookahead;
					afterWhitespace.Next();
					TryToSkipWhitespace(ref afterWhitespace);

					// If the whitespace is followed by an opening parenthesis, ignore it.  It's a parameter section that should be attached to the
					// previous parameter.  If it's followed by a closing parenthesis, also ignore it because it's the end of the parameters instead
					// of a separator.
					if (afterWhitespace.Character == '(' ||
						afterWhitespace.Character == ')')
						{
						lookahead = afterWhitespace;
						continue;
						}

					// Otherwise we treat it as a parameter separator
					if (mode == ParseMode.ParsePrototype && prototypeParsingType == PrototypeParsingType.StartOfPrototypeSection)
						{
						formatAsList = true;
						lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;

						TokenIterator endOfParameter = lookahead;
						endOfParameter.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

						if (endOfParameter > startOfParameter)
							{  startOfParameter.SetPrototypeParsingTypeBetween(endOfParameter, PrototypeParsingType.PropertyValue);  }
						}

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);

					startOfParameter = lookahead;
					}

				else
					{
					GenericSkip(ref lookahead);
					}
				}


			// Parameter contents and separators should already be marked.

			if (mode == ParseMode.SyntaxHighlight)
				{
				iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Metadata);
				}

			else if (mode == ParseMode.ParsePrototype)
				{
				if (prototypeParsingType == PrototypeParsingType.StartOfPrototypeSection)
					{
					iterator.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;

					if (formatAsList)
						{
						openingParen.PrototypeParsingType = PrototypeParsingType.StartOfMetadataParams;
						closingParen.PrototypeParsingType = PrototypeParsingType.EndOfMetadataParams;

						// This could get overwritten if there's no whitespace between the attribute and the next token, but that shouldn't
						// happen very often in practice
						lookahead.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;
						}
					else
						{
						closingParen.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;
						}
					}
				else
					{
					iterator.SetPrototypeParsingTypeBetween(lookahead, prototypeParsingType);
					}
				}

			else if (mode == ParseMode.ParseClassPrototype)
				{
				iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;
				iterator.Next();

				iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.PrePrototypeLine);
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipPreprocessingDirective
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipPreprocessingDirective (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '#')
				{  return false;  }

			// Only actual whitespace may precede the hash character on the line.  Comments are not allowed.
			TokenIterator lookbehind = iterator;
			lookbehind.Previous();
			lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator);

			if (lookbehind.IsInBounds && lookbehind.FundamentalType != FundamentalType.LineBreak)
				{  return false;  }

			TokenIterator startOfDirective = iterator;

			// Technically only the enumerated preprocessing keywords are valid here, but we'll be tolerant and accept anything in the
			// format.  Comments are allowed after directives.
			do
				{
				if (iterator.Character == '\\')
					{
					iterator.Next();

					if (iterator.FundamentalType == FundamentalType.LineBreak)
						{  iterator.Next();  }
					}
				else
					{  iterator.Next();  }
				}
			while (iterator.IsInBounds &&
					  iterator.FundamentalType != FundamentalType.LineBreak &&
					  iterator.MatchesAcrossTokens("//") == false &&
					  iterator.MatchesAcrossTokens("/*") == false);

			// Trim trailing whitespace, although we technically don't have to
			iterator.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

			if (mode == ParseMode.SyntaxHighlight)
				{  startOfDirective.SetSyntaxHighlightingTypeBetween(iterator, SyntaxHighlightingType.PreprocessingDirective);  }

			return true;
			}


		/* Function: TryToSkipTypeWord
		 *
		 * If the iterator is on what could be a complex type or an identifier with modifiers, moves the iterator past one word and
		 * returns true.  A word can be a type like "int", an identifier like "PackageName::FunctionName", a modifier like "const", an
		 * attribute like  "[[deprecated]]", or a macro like "__declspec" sans parentheses.  Counting the words helps determine how
		 * each one is interpreted.
		 *
		 * This will skip most symbols surrounding the word, such as "*int" and "int[]".  It will not skip parentheses following it.  It
		 * will always skip template signatures appearing inside a qualified identifier ("List<int>::Function") but only the one at the
		 * end of it if the option is set ("Function<int>").
		 *
		 * It will return false on obvious endpoints like ;, {, and =, but otherwise it will accept almost anything so it's up to the calling
		 * code to check for ends of the declaration.
		 */
		protected bool TryToSkipTypeWord (ref TokenIterator iterator, bool includeTemplateSignatures)
			{
			if (TryToSkipAttribute(ref iterator, ParseMode.IterateOnly))
				{  return true;  }

			TokenIterator lookahead = iterator;
			bool success = false;


			// Symbols before an identifier

			for (;;)
				{
				if (iterator.Character == '*' || iterator.Character == '&')
					{
					lookahead.Next();

					// We want to move on unexpected symbols rather than get stuck, so return this as a success even if it's the only thing
					// we find.
					iterator = lookahead;
					success = true;
					}
				else
					{  break;  }

				TryToSkipWhitespace(ref lookahead);
				}


			// Identifier

			if (TryToSkipIdentifier(ref lookahead, ParseMode.IterateOnly))
				{
				iterator = lookahead;
				success = true;

				TryToSkipWhitespace(ref lookahead);
				}


			// Symbols after an identifier

			for (;;)
				{
				if (lookahead.Character == '[')
					{
					lookahead.Next();

					// If it's an attribute stop here so it's its own word
					if (lookahead.Character == '[')
						{  return success;  }

					// If the block isn't closed stop here
					if (!GenericSkipUntilAfter(ref lookahead, ']', angleBracketsAsBlocks: true))
						{  return success;  }
					else
						{
						iterator = lookahead;
						success = true;
						TryToSkipWhitespace(ref lookahead);
						}
					}
				else if (includeTemplateSignatures && lookahead.Character == '<')
					{
					lookahead.Next();

					// If the block isn't closed stop here
					if (!GenericSkipUntilAfter(ref lookahead, '>', angleBracketsAsBlocks: true))
						{  return success;  }
					else
						{
						iterator = lookahead;
						success = true;
						TryToSkipWhitespace(ref lookahead);
						}
					}
				else
					{  break;  }
				}


			// If we didn't find anything with the above, try to move forward anyway since there might be an unexpected symbol
			// that we didn't plan for.

			if (!success)
				{
				// Definitely fail on these symbols though, since we don't want them included here.
				if (!lookahead.IsInBounds ||
					lookahead.Character == ';' || lookahead.Character == '=' ||
					lookahead.Character == '{' || lookahead.Character == '(' ||
					(lookahead.Character == '<' && !includeTemplateSignatures) ||
					lookahead.Character == ',')
					{  return false;  }

				else
					{
					GenericSkip(ref lookahead);
					iterator = lookahead;
					return true;
					}
				}

			return success;
			}


		/* Function: TryToSkipIdentifier
		 *
		 * Tries to move the iterator past a qualified identifier, such as "A::B::C".  Use <TryToSkipUnqualifiedIdentifier()> if you only want
		 * to skip a single segment.
		 *
		 * Note that this function will handle internal template signatures ("A.B<X>.C") but not the one at the end ("A.B.C<X>") since it
		 * is assumed that you would want to handle that one manually.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.Name> or
		 *			  <PrototypeParsingType.Type>.  If set to Type, it will use both <PrototypeParsingType.Type> and
		 *			  <PrototypeParsingType.TypeQualifier>.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- The tokens will be marked with <ClassPrototypeParsingType.Name>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
																	   PrototypeParsingType prototypeParsingType = PrototypeParsingType.Name)
			{
			TokenIterator lookahead = iterator;
			TokenIterator endOfIdentifier;
			TokenIterator endOfQualifier = iterator;

			var templateSignatureType = (prototypeParsingType == PrototypeParsingType.Type ?
														TemplateSignatureType.Instantiation : TemplateSignatureType.Definition);

			// Allow a leading :: for globals
			if (lookahead.MatchesAcrossTokens("::"))
				{
				lookahead.Next(2);
				endOfQualifier = lookahead;
				}

			for (;;)
				{
				if (TryToSkipUnqualifiedIdentifier(ref lookahead) == false)
					{  return false;  }

				endOfIdentifier = lookahead;
				TryToSkipWhitespace(ref lookahead);

				if (TryToSkipTemplateSignature(ref lookahead, templateSignatureType, mode))
					{
					TryToSkipWhitespace(ref lookahead);
					}

				if (lookahead.MatchesAcrossTokens("::"))
					{
					lookahead.Next(2);
					}
				else
					{  break;  }

				TryToSkipWhitespace(ref lookahead);
				endOfQualifier = lookahead;
				}

			if (mode == ParseMode.ParsePrototype)
				{
				if (prototypeParsingType == PrototypeParsingType.Type)
					{
					if (endOfQualifier > iterator)
						{  iterator.SetPrototypeParsingTypeBetween(endOfQualifier, PrototypeParsingType.TypeQualifier);  }

					endOfQualifier.SetPrototypeParsingTypeBetween(endOfIdentifier, PrototypeParsingType.Type);
					}
				else
					{  iterator.SetPrototypeParsingTypeBetween(endOfIdentifier, prototypeParsingType);  }
				}
			else if (mode == ParseMode.ParseClassPrototype)
				{  iterator.SetClassPrototypeParsingTypeBetween(endOfIdentifier, ClassPrototypeParsingType.Name);  }

			iterator = endOfIdentifier;
			return true;
			}


		/* Function: TryToSkipUnqualifiedIdentifier
		 *
		 * Tries to move the iterator past a single unqualified identifier, which means only "A" in "A::B::C".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.Name> or
		 *			  <PrototypeParsingType.Type>.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- The tokens will be marked with <ClassPrototypeParsingType.Name>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipUnqualifiedIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
																					 PrototypeParsingType prototypeParsingType = PrototypeParsingType.Name)
			{
			if (iterator.FundamentalType == FundamentalType.Text)
				{
				if (iterator.Character >= '0' && iterator.Character <= '9')
					{  return false;  }
				}
			else if (iterator.Character != '_')
				{  return false;  }

			TokenIterator lookahead = iterator;

			do
				{  lookahead.Next();  }
			while (lookahead.FundamentalType == FundamentalType.Text || lookahead.Character == '_');

			if (mode == ParseMode.ParsePrototype)
				{  iterator.SetPrototypeParsingTypeBetween(lookahead, prototypeParsingType);  }
			else if (mode == ParseMode.ParseClassPrototype)
				{  iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.Name);  }

			iterator = lookahead;
			return true;
			}


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
			// Everything in cKeywords starts with lowercase a-z
			if (iterator.Character < 'a' || iterator.Character > 'z')
				{  return false;  }

			// Make sure the previous token isn't part of the identifier
			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			if (lookbehind.FundamentalType == FundamentalType.Text ||
				lookbehind.Character == '_')
				{  return false;  }

			// Find the end
			TokenIterator endOfKeyword = iterator;

			do
				{  endOfKeyword.Next();  }
			while (endOfKeyword.FundamentalType == FundamentalType.Text ||
					  endOfKeyword.Character == '_');

			if (!cKeywords.Contains(iterator.TextBetween(endOfKeyword)))
				{  return false;  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(endOfKeyword, SyntaxHighlightingType.Keyword);  }

			iterator = endOfKeyword;
			return true;
			}


		/* Function: TryToSkipNumber
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipNumber (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			bool success = TryToSkipNumber(ref iterator,
															ParseNumberFlags.AllowDigitSeparators |
															ParseNumberFlags.AllowHexFloats,
															// Doesn't require digit before or after dot
															mode,
															digitSeparator: '\'');

			// Extend to include user-defined suffixes.  They must start with an underscore.
			if (success && iterator.Character == '_')
				{
				TokenIterator lookahead = iterator;

				do
					{  lookahead.Next();  }
				while (lookahead.FundamentalType == FundamentalType.Text ||
						  lookahead.Character == '_');

				if (mode == ParseMode.SyntaxHighlight)
					{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Number);  }

				iterator = lookahead;
				}

			return success;
			}


		/* Function: TryToSkipString
		 *
		 * If the iterator is on a quote or apostrophe, moves it past the entire string and returns true.  Since regular expressions
		 * will be formatted as strings, it will skip over them as well.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipString (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '\'' &&
				iterator.Character != '\"' &&
				(iterator.FundamentalType == FundamentalType.Text && iterator.TokenLength <= 3) == false)
				{  return false;  }


			// String prefix

			TokenIterator lookahead = iterator;
			bool isRawString = false;

			if (lookahead.FundamentalType == FundamentalType.Text)
				{
				int matchIndex = lookahead.MatchesAnyToken(cStringPrefixes, ignoreCase: false);

				if (matchIndex == -1)
					{  return false;  }

				string prefix = cStringPrefixes[matchIndex];

				if (prefix[prefix.Length - 1] == 'R')
					{  isRawString = true;  }

				// Make sure it's not part of a longer token
				TokenIterator lookbehind = iterator;
				lookbehind.Previous();

				if (lookbehind.FundamentalType == FundamentalType.Text ||
					lookbehind.Character == '_')
					{  return false;  }

				lookahead.Next();
				}


			// Delimiter

			char delimiter = lookahead.Character;

			if (delimiter != '\'' && delimiter != '\"')
				{  return false;  }

			lookahead.Next();


			// Extra chars in raw string delimiter.  There may not be any..

			string extraDelimiterChars = null;

			if (isRawString)
				{
				if (lookahead.Character != '(')
					{
					TokenIterator startOfExtraDelimiter = lookahead;

					do
						{  lookahead.Next();  }
					while (lookahead.IsInBounds &&
							  lookahead.Character != '(');

					extraDelimiterChars = startOfExtraDelimiter.TextBetween(lookahead);
					}

				if (lookahead.Character != '(')
					{  return false;  }

				lookahead.Next();
				}


			// Regular string contents

			if (!isRawString)
				{
				while (lookahead.IsInBounds)
					{
					if (lookahead.Character == delimiter)
						{
						lookahead.Next();
						break;
						}
					else if (lookahead.Character == '\\')
						{  lookahead.Next(2);  }
					else
						{  lookahead.Next();  }
					}
				}


			// Raw string contents

			else
				{
				while (lookahead.IsInBounds)
					{
					if (lookahead.Character == ')')
						{
						lookahead.Next();
						bool extraDelimitersMatch = true;

						if (extraDelimiterChars != null)
							{
							if (lookahead.MatchesAcrossTokens(extraDelimiterChars, ignoreCase: false, matchPartialTokens: false))
								{  lookahead.NextByCharacters(extraDelimiterChars.Length);  }
							else
								{  extraDelimitersMatch = false;  }
							}

						if (extraDelimitersMatch &&
							lookahead.Character == delimiter)
							{
							lookahead.Next();
							break;
							}
						}

					// There's no backslash or any other escaping in raw strings
					else
						{  lookahead.Next();  }
					}
				}


			// User defined suffixes

			if (lookahead.Character == '_')
				{
				do
					{  lookahead.Next();  }
				while (lookahead.FundamentalType == FundamentalType.Text ||
						  lookahead.Character == '_');
				}


			// Done

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.String);  }

			iterator = lookahead;
			return true;
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: cKeywords
		 */
		static protected StringSet cKeywords = new StringSet (KeySettings.Literal, new string[] {

			// C++ Keywords
			"alignas", "alignof", "and", "and_eq", "asm", "atomic_cancel", "atomic_commit", "atomic_noexcept", "auto",
			"bitand", "bitor", "bool", "break", "case", "catch", "char", "char8_t", "char16_t", "char32_t", "class", "compl",
			"concept", "const", "consteval", "constexpr", "constinit", "const_cast", "continue", "contract_assert", "co_await",
			"co_return", "co_yield", "decltype", "default", "delete", "do", "double", "dynamic_cast", "else", "enum", "explicit",
			"export", "extern", "false", "float", "for", "friend", "goto", "if", "inline", "int", "long", "mutable", "namespace",
			"new", "noexcept", "not", "not_eq", "nullptr", "operator", "or", "or_eq", "private", "protected", "public", "reflexpr",
			"register", "reinterpret_cast", "requires", "return", "short", "signed", "sizeof", "static", "static_assert", "static_cast",
			"struct", "switch", "synchronized", "template", "this", "thread_local", "throw", "true", "try", "typedef", "typeid",
			"typename", "union", "unsigned", "using", "virtual", "void", "volatile", "wchar_t", "while", "xor", "xor_eq",

			// C++ identifiers with special meaning
			"final", "override", "transaction_safe", "transaction_safe_dynamic", "import", "module", "pre", "post",

			// C keywords not in C++
			"restrict", "typeof", "typeof_unqual"

			});


		/* var: cStringPrefixes
		 */
		static protected string[] cStringPrefixes = new string[] { "R", "L", "LR", "u8", "u8R", "u", "uR", "U", "UR" };
		}
	}
