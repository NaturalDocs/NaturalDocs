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
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class C : Parser
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: MTNType
		 *
		 * The type of modifier-type-name words being parsed.
		 *
		 * Function - A function definition from the start to the parameters, encompassing modifiers, the return value, and the function
		 *				   name.
		 * Parameter - A function parameter from the start to the default value, parameter separator, or end of parameters, encompassing
		 *					  modifiers, the type, and the name.
		 * NamelessParameter - A function parameter that doesn't have a name, encompassing modifiers and the type.
		 * Variable - A variable from the start to the start of the default value or the end of the definition, encompassing modifiers, the
		 *				   type, and the name.
		 * TrailingReturnType - A trailing function return type from the start to the end of the definition, encompassing modifiers and the type.
		 */
		public enum MTNType : byte
			{  Function, Parameter, NamelessParameter, Variable, TrailingReturnType  }


		/* Enum: TemplateSignatureType
		 * Definition - The signature of a template definition, such as "template<class T>".
		 * Instantiation - The signature of a template instantiation, such as "List<int>".
		 */
		public enum TemplateSignatureType : byte
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


		override protected bool TryToFindBasicPrototype (Topic topic, TokenIterator start, TokenIterator limit,
																				out TokenIterator prototypeStart, out TokenIterator prototypeEnd)
			{
			TryToSkipWhitespace(ref start);
			TokenIterator lookahead = start;

			// Needed because the opening brace in a "requires requires() { }" clause would otherwise be mistaken for the start of
			// the function or class body and end the prototype too early.
			if (TryToSkipClass(ref lookahead, ParseMode.ParsePrototype) ||
				TryToSkipFunction(ref lookahead, ParseMode.ParsePrototype))
				{
				prototypeStart = start;
				prototypeEnd = lookahead;
				return true;
				}
			else
				{
				return base.TryToFindBasicPrototype(topic, start, limit, out prototypeStart, out prototypeEnd);
				}
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */
		override public ParsedPrototype ParsePrototype(string stringPrototype, int commentTypeID)
			{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator iterator = tokenizedPrototype.FirstToken;
			bool parsed = false;

			if (commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("function", language.ID) ||
				commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("operator", language.ID))
			    {
				parsed = TryToSkipFunction(ref iterator, ParseMode.ParsePrototype);
				}

			if (!parsed &&
				(commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("variable", language.ID) ||
				 commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("constant", language.ID)))
			    {
				parsed = TryToSkipVariable(ref iterator, ParseMode.ParsePrototype);
				}

			if (!parsed &&
				commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("enum", language.ID))
			    {
				parsed = TryToSkipEnum(ref iterator, ParseMode.ParsePrototype);
				}

			if (parsed)
				{
				return new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID, engineInstance);
				}
			else
			    {
				return base.ParsePrototype(stringPrototype, commentTypeID);
				}
			}


		/* Function: ParseClassPrototype
		 * Converts a raw text prototype into a <ParsedClassPrototype>.  Will return null if it is not an appropriate prototype.
		 */
		override public ParsedClassPrototype ParseClassPrototype (string stringPrototype, int commentTypeID)
			{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator iterator = tokenizedPrototype.FirstToken;

			if (TryToSkipClass(ref iterator, ParseMode.ParseClassPrototype))
				{
				return new ParsedClassPrototype(tokenizedPrototype);
				}
			else
				{
				return base.ParseClassPrototype(stringPrototype, commentTypeID);
				}
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: IsPartOfLongerIdentifier
		 * Returns whether the <TokenIterator> is on token that's part of a longer identifier, such as by being next to an underscore.
		 * This is primarily used to validate keywords after checking the contents of the token against a keyword list, so that "input"
		 * by itself will be distinguished from "_input" or similar.
		 */
		protected bool IsPartOfLongerIdentifier (TokenIterator iterator, int characterLength)
			{
			TokenIterator lookahead = iterator;
			lookahead.NextByCharacters(characterLength);

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
		 * before or after it.  This allows us to be sure an iterator on "input" isn't actually on "_input" or similar.
		 *
		 * If you have multiple keywords to test against, it is more efficient to use one of the <IsOnAnyKeyword()> functions.
		 */
		public bool IsOnKeyword (TokenIterator iterator, string keyword)
			{
			return (iterator.MatchesAcrossTokens(keyword, ignoreCase: false) &&
					   !IsPartOfLongerIdentifier(iterator, keyword.Length));
			}


		/* Function: IsOnAnyKeyword
		 *
		 * Returns whether the <TokenIterator> is on the passed keyword, making sure there are no other identifier tokens
		 * before or after it.  This allows us to be sure an iterator on "input" isn't actually on "_input" or similar.
		 */
		public bool IsOnAnyKeyword (TokenIterator iterator, params string[] keywords)
			{
			int matchIndex = iterator.MatchesAnyAcrossTokens(keywords, ignoreCase: false);

			return (matchIndex != -1 &&
					   !IsPartOfLongerIdentifier(iterator, keywords[matchIndex].Length));
			}


		/* Function: TryToSkipClass
		 *
		 * If the iterator is on a class, struct, or union definition, moves it past it and returns true.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- <ParseMode.ParseClassPrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipClass (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;


			// Attributes and template declaration

			while (TryToSkipAttributes(ref lookahead, mode, classPrototypeParsingType: ClassPrototypeParsingType.PrePrototypeLine) ||
					  TryToSkipTemplateDeclaration(ref lookahead, mode) ||
					  TryToSkipKeywordAndParentheses(ref lookahead, "alignas", mode,
																	    requireParentheses: true, classPrototypeParsingType: ClassPrototypeParsingType.Modifier))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword

			if (!IsOnAnyKeyword(lookahead, "class", "struct", "union"))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParseClassPrototype)
				{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.Keyword;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Distinguish from class/struct enum

			if (IsOnKeyword(lookahead, "enum"))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}


			// Attributes After Keyword

			while (TryToSkipAttributes(ref lookahead, mode, classPrototypeParsingType: ClassPrototypeParsingType.PrePrototypeLine) ||
					  TryToSkipKeywordAndParentheses(ref lookahead, "alignas", mode,
																	    requireParentheses: true, classPrototypeParsingType: ClassPrototypeParsingType.Modifier))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Name

			if (!TryToSkipIdentifier(ref lookahead, mode))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			// Template signature for explicit instantiation

			if (TryToSkipTemplateSignature(ref lookahead, TemplateSignatureType.Instantiation, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Final

			if (IsOnKeyword(lookahead, "final"))
				{
				if (mode == ParseMode.ParseClassPrototype)
					{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.Modifier;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Inheritance

			if (lookahead.Character == ':')
				{
				if (mode == ParseMode.ParseClassPrototype)
					{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfParents;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				for (;;)
					{
					if (TryToSkipAttributes(ref lookahead, mode, classPrototypeParsingType: ClassPrototypeParsingType.Modifier))
						{  TryToSkipWhitespace(ref lookahead);  }

					while (IsOnAnyKeyword(lookahead, "public", "private", "protected", "virtual"))
						{
						if (mode == ParseMode.ParseClassPrototype)
							{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.Modifier;  }

						lookahead.Next();
						TryToSkipWhitespace(ref lookahead);
						}

					if (!TryToSkipIdentifier(ref lookahead, mode))
						{  return false;  }

					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == ',')
						{
						if (mode == ParseMode.ParseClassPrototype)
							{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.ParentSeparator;  }

						lookahead.Next();
						TryToSkipWhitespace(ref lookahead);
						}
					else
						{  break;  }
					}
				}

			iterator = lookahead;
			return true;
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


			// Template declaration and attributes

			if (TryToSkipAttributes(ref lookahead, mode, prototypeParsingType: PrototypeParsingType.StartOfPrototypeSection))
				{  TryToSkipWhitespace(ref lookahead);  }

			if (TryToSkipTemplateDeclaration(ref lookahead, mode))
				{
				TryToSkipWhitespace(ref lookahead);

				if (TryToSkipAttributes(ref lookahead, mode, prototypeParsingType: PrototypeParsingType.StartOfPrototypeSection))
					{  TryToSkipWhitespace(ref lookahead);  }
				}


			// The part before the parameters can be treated as a modifier-type-name group

			if (!TryToSkipMTNGroup(ref lookahead, MTNType.Function, out bool returnsFunctionPointer, mode,
											   failOnKeywords: ["class", "struct", "union", "enum", "concept"]))
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);


			// Parameters

			if (lookahead.Character != '(')
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TokenIterator openingParen = lookahead;

			if (mode == ParseMode.ParsePrototype)
				{  openingParen.PrototypeParsingType = PrototypeParsingType.StartOfParams;  }

			lookahead.Next();

			if (!GenericSkipUntilOn(ref lookahead, ')', angleBracketsAsBlocks: true, skipToEndIfNotFound: false))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TokenIterator closingParen = lookahead;

			if (mode == ParseMode.ParsePrototype)
				{  closingParen.PrototypeParsingType = PrototypeParsingType.EndOfParams;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// const/volatile

			if (IsOnAnyKeyword(lookahead, "const", "volatile"))
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// &/&&

			if (lookahead.Character == '&')
				{
				do
					{  lookahead.Next();  }
				while (lookahead.Character == '&');

				TryToSkipWhitespace(ref lookahead);
				}


			// noexcept

			if (IsOnKeyword(lookahead, "noexcept"))
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character == '(')
					{
					lookahead.Next();
					GenericSkipUntilAfter(ref lookahead, ')', skipToEndIfNotFound: false);
					TryToSkipWhitespace(ref lookahead);
					}
				}


			// throw

			else if (IsOnKeyword(lookahead, "throw"))
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character == '(')
					{
					TryToSkipParameters(ref lookahead, mode, markAsTypeModifiers: true);
					TryToSkipWhitespace(ref lookahead);
					}
				}


			// Rest of returned function pointer syntax

			if (returnsFunctionPointer)
				{
				if (!TryToSkipFunctionPointer_ClosingParenthesisThroughParameters(ref lookahead, mode))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				TryToSkipWhitespace(ref lookahead);

				if (TryToSkipFunctionPointer_TrailingModifiers(ref lookahead, mode))
					{  TryToSkipWhitespace(ref lookahead);  }
				}


			// -> trailing return type

			else if (lookahead.MatchesAcrossTokens("->"))
				{
				lookahead.Next(2);
				TryToSkipWhitespace(ref lookahead);

				TryToSkipMTNGroup(ref lookahead, MTNType.TrailingReturnType, out bool returnsTrailingFunctionPointer, mode);
				TryToSkipWhitespace(ref lookahead);

				if (returnsTrailingFunctionPointer)
					{
					if (!TryToSkipFunctionPointer_ClosingParenthesisThroughParameters(ref lookahead, mode))
						{
						ResetTokensBetween(iterator, lookahead, mode);
						return false;
						}

					TryToSkipWhitespace(ref lookahead);

					if (TryToSkipFunctionPointer_TrailingModifiers(ref lookahead, mode))
						{ TryToSkipWhitespace(ref lookahead); }
					}
				}


			// Trailing requires clause

			if (TryToSkipRequiresClause(ref lookahead, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// override/final

			if (IsOnAnyKeyword(lookahead, "override", "final"))
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// = 0/default/delete

			if (lookahead.Character == '=')
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				bool validKeyword = false;
				bool canHaveParentheses = false;

				if (IsOnKeyword(lookahead, "delete"))
					{
					validKeyword = true;
					canHaveParentheses = true;
					}
				else if (IsOnAnyKeyword(lookahead, "0", "default"))
					{
					validKeyword = true;
					canHaveParentheses = false;
					}

				if (validKeyword)
					{
					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);

					if (canHaveParentheses && lookahead.Character == '(')
						{
						lookahead.Next();
						GenericSkipUntilAfter(ref lookahead, ')', skipToEndIfNotFound: false);
						TryToSkipWhitespace(ref lookahead);
						}
					}
				}


			// Process the parameters.  We can skip this if we're just iterating over it.

			if (mode == ParseMode.ParsePrototype)
				{
				TokenIterator temp = openingParen;

				if (!TryToSkipParameters(ref temp, mode))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}
				}


			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipVariable
		 *
		 * If the iterator is on a variable or constant definition, moves it past it and returns true.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipVariable (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;


			// Attributes

			if (TryToSkipAttributes(ref lookahead, mode, prototypeParsingType: PrototypeParsingType.StartOfPrototypeSection))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Most of it can be treated as a modifier-type-name group

			if (!TryToSkipMTNGroup(ref lookahead, MTNType.Variable, out bool isFunctionPointer, mode,
											   failOnKeywords: ["class", "struct", "union", "enum", "concept"]))
				{  return false;  }

			TokenIterator endOfVariable = lookahead;
			TryToSkipWhitespace(ref lookahead);

			if (isFunctionPointer)
				{
				if (!TryToSkipFunctionPointer_ClosingParenthesisThroughParameters(ref lookahead, mode))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				endOfVariable = lookahead;
				TryToSkipWhitespace(ref lookahead);

				if (TryToSkipFunctionPointer_TrailingModifiers(ref lookahead, mode))
					{
					endOfVariable = lookahead;
					TryToSkipWhitespace(ref lookahead);
					}
				}


			// Default value or end of declaration

			if (lookahead.IsInBounds &&
				lookahead.Character != '=' &&
				lookahead.Character != ';')
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}


			iterator = endOfVariable;
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
		protected bool TryToSkipParameters (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
															 char openingChar = '(', bool nameless = false, bool markAsTypeModifiers = false)
			{
			if (iterator.Character != openingChar)
				{  return false;  }

			char closingChar;

			if (openingChar == '(')
				{  closingChar = ')';  }
			else if (openingChar == '<')
				{  closingChar = '>';  }
			else
				{  throw new InvalidOperationException();  }


			// Opening paren

			TokenIterator lookahead = iterator;

			if (mode == ParseMode.ParsePrototype)
				{
				lookahead.PrototypeParsingType = (markAsTypeModifiers ? PrototypeParsingType.OpeningTypeModifier
																								  : PrototypeParsingType.StartOfParams);
				}

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Parameter list

			while (lookahead.IsInBounds && lookahead.Character != closingChar)
				{
				if (lookahead.Character == ',')
					{
					if (mode == ParseMode.ParsePrototype && !markAsTypeModifiers)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				else if (TryToSkipParameter(ref lookahead, mode, closingChar: closingChar, nameless: nameless))
					{
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{  break;  }
				}


			// Closing paren

			if (lookahead.Character == closingChar)
				{
			if (mode == ParseMode.ParsePrototype)
				{
				lookahead.PrototypeParsingType = (markAsTypeModifiers ? PrototypeParsingType.ClosingTypeModifier
																								  : PrototypeParsingType.EndOfParams);
				}

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
		 * Tries to move the iterator past a parameter, such as "int x" or "List<int> y = null".  The parameter ends at a comma or the closing
		 * character.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipParameter (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
															char closingChar = ')', bool nameless = false)
			{

			// The majority of the parameter can be treated as a modifier-type-name group

			bool isPartOfFunctionPointer;
			if (!TryToSkipMTNGroup(ref iterator, (nameless ? MTNType.NamelessParameter : MTNType.Parameter), out isPartOfFunctionPointer, mode))
				{  return false;  }

			TokenIterator lookahead = iterator;
			TryToSkipWhitespace(ref lookahead);

			if (isPartOfFunctionPointer)
				{
				if (!TryToSkipFunctionPointer_ClosingParenthesisThroughParameters(ref lookahead, mode))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				TryToSkipWhitespace(ref lookahead);

				// For parameters, the trailing modifiers appear after the function pointer's parameters
				if (TryToSkipFunctionPointer_TrailingModifiers(ref lookahead, mode))
					{  TryToSkipWhitespace(ref lookahead);  }
				}


			// Default value

			if (lookahead.Character == '=')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.DefaultValueSeparator;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				TokenIterator startOfDefaultValue = lookahead;

				while (lookahead.IsInBounds &&
						  lookahead.Character != ',' &&
						  lookahead.Character != closingChar)
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


		/* Function: TryToSkipTemplateDeclaration
		 *
		 * Tries to move the iterator past a template declaration, such as "template<typename T>".  It supports the export keyword.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- <ParseMode.ParseClassPrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipTemplateDeclaration (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (!IsOnAnyKeyword(iterator, "template", "export"))
				{  return false;  }

			TokenIterator lookahead = iterator;


			// Export keyword

			// We know this is the export keyword from the test at the start of the function
			if (lookahead.Character == 'e')
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (!IsOnKeyword(lookahead, "template"))
					{  return false;  }
				}


			// Template keyword

			// We know the iterator is on the template keyword at this point
			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Template signature

			if (!TryToSkipTemplateSignature(ref lookahead, TemplateSignatureType.Definition, mode))
				{  return false;  }

			TokenIterator endOfTemplate = lookahead;
			TryToSkipWhitespace(ref lookahead);


			// Requires clauses

			TokenIterator requires = lookahead;
			bool hasRequires = TryToSkipRequiresClause(ref lookahead, mode);

			if (hasRequires)
				{  endOfTemplate = lookahead;  }


			// Success

			if (mode == ParseMode.ParsePrototype)
				{
				iterator.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;

				// Requires will create its own prototype section, so only end this one if it doesn't exist
				if (!hasRequires)
					{
					TokenIterator lookbehind = endOfTemplate;
					lookbehind.Previous();
					lookbehind.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;
					}
				}
			else if (mode == ParseMode.ParseClassPrototype)
				{
				iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;

				iterator.Next();
				iterator.SetClassPrototypeParsingTypeBetween(endOfTemplate, ClassPrototypeParsingType.PrePrototypeLine);

				if (hasRequires)
					{  requires.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;  }
				}

			iterator = endOfTemplate;
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

			if (!TryToSkipParameters(ref lookahead, mode, openingChar: '<', nameless: (signatureType == TemplateSignatureType.Instantiation),
												markAsTypeModifiers: true))
				{  return false;  }

			if (mode == ParseMode.ParseClassPrototype)
				{
				iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.TemplateSuffix);
				}
			// ParseMode.ParsePrototype is handled by TryToSkipParameters()

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipRequiresClause
		 *
		 * Tries to move the iterator past a constraint declaration.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- <ParseMode.ParseClassPrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipRequiresClause (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (!IsOnKeyword(iterator, "requires"))
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			TokenIterator endOfClause = lookahead;
			TryToSkipWhitespace(ref lookahead);


			// Constraints in parentheses

			if (lookahead.Character == '(')
				{
				lookahead.Next();

				if (GenericSkipUntilAfter(ref lookahead, ')',  angleBracketsAsBlocks: false))
					{  endOfClause = lookahead;  }
				else
					{  return false;  }
				}


			// Constraints defined inline with double requires

			else if (IsOnKeyword(lookahead, "requires"))
				{
				lookahead.Next();
				endOfClause = lookahead;

				TryToSkipWhitespace(ref lookahead);

				// Parameters, optional
				if (TryToSkipParameters(ref lookahead, mode, markAsTypeModifiers: true))
					{  TryToSkipWhitespace(ref lookahead);  }

				// Content
				if (lookahead.Character == '{')
					{
					TokenIterator openingBrace = lookahead;
					lookahead.Next();

					if (GenericSkipUntilOn(ref lookahead, '}', angleBracketsAsBlocks: false))
						{
						TokenIterator closingBrace = lookahead;

						lookahead.Next();
						endOfClause = lookahead;

						if (mode == ParseMode.ParsePrototype)
							{
							openingBrace.PrototypeParsingType = PrototypeParsingType.StartOfMetadataParams;
							closingBrace.PrototypeParsingType = PrototypeParsingType.EndOfMetadataParams;

							TokenIterator startOfConstraint = openingBrace;
							startOfConstraint.Next();
							startOfConstraint.NextPastWhitespace();

							TokenIterator constraintIterator = startOfConstraint;

							while (constraintIterator <= closingBrace)
								{
								if (constraintIterator == closingBrace ||
									constraintIterator.Character == ';')
									{
									TokenIterator endOfConstraint = constraintIterator;
									endOfConstraint.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

									if (endOfConstraint > startOfConstraint)
										{  startOfConstraint.SetPrototypeParsingTypeBetween(endOfConstraint, PrototypeParsingType.PropertyValue);  }

									if (constraintIterator.Character == ';')
										{
										constraintIterator.PrototypeParsingType = PrototypeParsingType.ParamSeparator;
										constraintIterator.Next();
										constraintIterator.NextPastWhitespace();
										startOfConstraint = constraintIterator;
										}
									else
										{  break;  }
									}
								else
									{  GenericSkip(ref constraintIterator, angleBracketsAsBlocks: false);  }
								}
							}
						}
					else
						{  return false;  }
					}
				else
					{  return false;  }
				}


			// Simple constraints as identifiers

			else
				{
				while (TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Type))
					{
					endOfClause = lookahead;
					TryToSkipWhitespace(ref lookahead);

					if (TryToSkipTemplateSignature(ref lookahead, TemplateSignatureType.Instantiation, mode))
						{
						endOfClause = lookahead;
						TryToSkipWhitespace(ref lookahead);
						}

					if (lookahead.MatchesAcrossTokens("&&") ||
						lookahead.MatchesAcrossTokens("||"))
						{
						lookahead.Next(2);
						endOfClause = lookahead;
						TryToSkipWhitespace(ref lookahead);
						}
					else
						{  break;  }
					}
				}


			// Success

			if (mode == ParseMode.ParsePrototype)
				{
				iterator.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;

				TokenIterator lookbehind = endOfClause;
				lookbehind.Previous();

				// Ideally we'd want to mark the last token as the end of the section, but don't overwrite something else like the
				// closing brace of a double requires being marked as EndOfMetadataParams.
				if (lookbehind.PrototypeParsingType == PrototypeParsingType.Null)
					{  lookbehind.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;  }

				// Otherwise apply it to endOfClause.  This is ugly since it can be potentially overwritten, but there's likely to be
				// whitespace here that we can use which would prevent it.  Also check if it's in bounds, since it could be the requires
				// following the parameters at the end of a function prototype.
				else if (endOfClause.IsInBounds)
					{  endOfClause.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;  }
				}

			iterator = endOfClause;
			return true;
			}


		/* Function: TryToSkipArrayBrackets
		 *
		 * If the iterator is on a set of one or more array brackets, such as in "int variableName[2][3]", moves it past all of them and
		 * return true.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipArrayBrackets (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '[')
				{  return false;  }

			bool result = false;
			TokenIterator lookahead = iterator;

			do
				{
				lookahead.Next();

				// Can't have two opening brackets in a row or else it's a property
				if (lookahead.Character == '[')
					{  return result;  }

				if (!GenericSkipUntilOn(ref lookahead, ']', angleBracketsAsBlocks: true))
					{  return result;  }

				if (mode == ParseMode.ParsePrototype)
					{
					iterator.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;
					lookahead.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
					}

				lookahead.Next();
				iterator = lookahead;
				result = true;

				TryToSkipWhitespace(ref lookahead);
				}
			while (lookahead.Character == '[');

			return result;
			}


		/* Function: TryToSkipOperatorOverloadIdentifier
		 *
		 * If the iterator is on the identifier of an operator oveload, moves it past it and returns true.  This will not handle the return
		 * values or parameters, just the identifier itself.  It will handle qualifiers before the operator keyword and any trailing keywords
		 * that are part of the identifier name, such as "new" or type names for casts.
		 *
		 * It supports all these forms:
		 *
		 * --- C++ ---
		 * operator+
		 * operator<<
		 * operator[]
		 * operator new
		 * operator new[]
		 * operator TypeName
		 * operator Namespace::TypeName
		 *
		 * ClassName::operator+
		 * // and all other ClassName:: varations of the above
		 *	---
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipOperatorOverloadIdentifier (ref TokenIterator iterator, out bool isTypeConversionOperator,
																					ParseMode mode = ParseMode.IterateOnly)
			{
			isTypeConversionOperator = false;

			// It can be ClassName::operator+ so we can't just check for "operator" here.  Still try to rule it out early if we can
			// to avoid expensive operations when this function will usually return false.
			if (iterator.FundamentalType != FundamentalType.Text &&
				iterator.Character != '_' &&
				iterator.Character != ':')
				{  return false;  }

			TokenIterator lookahead = iterator;

			// Iterate-only the first time so we don't have to undo anything if we fail, which will usually be the case.
			if (!TryToSkipIdentifier(ref lookahead, ParseMode.IterateOnly))
				{  return false;  }

			// Check that the last token in the iterator was "operator".
			lookahead.Previous();

			if (!lookahead.MatchesToken("operator"))
				{  return false;  }

			lookahead.Previous();

			if (lookahead.FundamentalType == FundamentalType.Text ||
				lookahead.Character == '_')
				{  return false;  }

			// Okay, now we know we're on an identifier that ends with "operator".  If we need to mark tokens, reset and go again
			if (mode == ParseMode.ParsePrototype)
				{
				lookahead = iterator;
				TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Name);
				lookahead.Previous();
				lookahead.PrototypeParsingType = PrototypeParsingType.KeywordName;
				lookahead.Next();
				}
			else
				{
				// If we're not marking tokens we still have to fix that we walked back two tokens.
				lookahead.Next(2);
				}

			TryToSkipWhitespace(ref lookahead);


			// operator new/delete

			if (IsOnAnyKeyword(lookahead, "new", "delete"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.KeywordName;  }

				lookahead.Next();

				// No whitespace allowed between new/delete and [] if it's included
				if (lookahead.MatchesAcrossTokens("[]"))
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.Name, 2);  }

					lookahead.Next(2);
					}
				}


			// operator co_await

			else if (IsOnKeyword(lookahead, "co_await"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.KeywordName, 8);  }

				lookahead.NextByCharacters(8);
				}


			// operator()

			else if (lookahead.MatchesAcrossTokens("()"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.Name, 2);  }

				lookahead.Next(2);
				}


			// operator+ and all other symbols

			else if (lookahead.FundamentalType == FundamentalType.Symbol &&
						lookahead.Character != ':')
				{
				do
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.Name;  }

					lookahead.Next();
					}
				while (lookahead.FundamentalType == FundamentalType.Symbol &&
						  lookahead.Character != '(');
				}


			// operator [type] conversions

			else if (lookahead.FundamentalType == FundamentalType.Text ||
					   lookahead.Character == '_' ||
					   lookahead.MatchesAcrossTokens("::"))
				{
				isTypeConversionOperator = true;

				// Set it to PrototypeParsingType.Type instead of Name so that it can potentially become a link.  This shouldn't cause
				// problems in function names the way it would in parameter names.
				while (TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Type))
					{
					lookahead.NextPastWhitespace();

					if (TryToSkipTemplateSignature(ref lookahead, TemplateSignatureType.Instantiation, mode))
						{  lookahead.NextPastWhitespace();  }

					while (lookahead.Character == '*' || lookahead.Character == '&')
						{
						if (mode == ParseMode.ParsePrototype)
							{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

						lookahead.Next();
						lookahead.NextPastWhitespace();
						}
					}

				while (lookahead.Character == '*' || lookahead.Character == '&')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

					lookahead.Next();
					lookahead.NextPastWhitespace();
					}
				}
			else
				{
				ResetTokensBetween(iterator, lookahead, mode);
				isTypeConversionOperator = false;
				return false;
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipFunctionPointer_OpeningParenthesisThroughIdentifier
		 *
		 * If the iterator is on the first opening parenthesis of a function pointer or reference, skips past it to the point of passing the
		 * identifier, if it exists.  For a function that returns a function pointer/reference, this leaves it before the opening parenthesis of
		 * the function's parameters.  For everything else, this leaves it at the closing parenthesis of the first part to be followed by the
		 * parentheses for the function pointer's parameters.
		 *
		 * It supports all these forms:
		 *
		 * --- C++ ---
		 * void (*)(int, int)
		 * void (*name)(int, int)
		 * void (ClassName::*)(int, int)
		 * void (ClassName::*name)(int, int)
		 * void (*FunctionName(float functionParam))(int, int)
		 * void (*name[])(int, int)
		 * void (&)(int, int)
		 * // and all other & variations of the above
		 *	---
		 *
		 * The iterator must start on the ( after "void" and it will be left on the spot indicated by ^:
		 *
		 * --- C++ ---
		 * void (*^)(int, int)
		 * void (*name^)(int, int)
		 * void (ClassName::*^)(int, int)
		 * void (ClassName::*name^)(int, int)
		 * void (*FunctionName^(float functionParam))(int, int)
		 * void (*name[]^)(int, int)
		 * void (&^)(int, int)
		 *	---
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipFunctionPointer_OpeningParenthesisThroughIdentifier (ref TokenIterator iterator,
																														   ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '(')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Class specifiers, may not exist

			// We're going to use iterate-only until we're sure we're on a function pointer, since it usually won't be the case but will
			// have a lot of false positives.  This limits the work we need to undo.
			if (TryToSkipUnqualifiedIdentifier(ref lookahead, ParseMode.IterateOnly))
				{
				do
					{
					TryToSkipWhitespace(ref lookahead);

					if (TryToSkipTemplateSignature(ref lookahead, TemplateSignatureType.Instantiation, ParseMode.IterateOnly))
						{  TryToSkipWhitespace(ref lookahead);  }

					if (lookahead.MatchesAcrossTokens("::"))
						{  lookahead.Next(2);  }
					else
						{  return false;  }
					}
				while (TryToSkipUnqualifiedIdentifier(ref lookahead, ParseMode.IterateOnly));
				}


			// Pointer or reference symbol

			if (lookahead.Character != '*' && lookahead.Character != '&')
				{  return false;  }

			do
				{  lookahead.Next();  }
			while (lookahead.Character == '*' || lookahead.Character == '&');

			// If we've reached this point we're likely on a function pointer and can start marking tokens.
			if (mode == ParseMode.ParsePrototype)
				{  iterator.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.TypeModifier);  }

			TokenIterator stopPoint = lookahead;
			TryToSkipWhitespace(ref lookahead);


			// Identifier.  May not exist.

			if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
				{
				stopPoint = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}


			// Array brackets

			if (TryToSkipArrayBrackets(ref lookahead, mode))
				{
				stopPoint = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}


			// Make sure what follows is either an opening parenthesis (function parameters for a function that returns a function pointer, like
			// "void (*FunctionName(int))(int)") or a closing one for everything else ("void (*variableName)(int)").

			if (lookahead.Character != '(' && lookahead.Character != ')')
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			iterator = stopPoint;
			return true;
			}


		/* Function: TryToSkipFunctionPointer_ClosingParenthesisThroughParameters
		 *
		 * If the iterator is on the closing parenthesis of the first part of a function pointer or reference, skips past it to the point of
		 * passing the function pointer's parameters.
		 *
		 * It supports all these forms.  The iterator must start on the spot indicated by ^:
		 *
		 * --- C++ ---
		 * void (*^)(int, int)
		 * void (*name^)(int, int)
		 * void (ClassName::*)(int, int)
		 * void (ClassName::*name^)(int, int)
		 * void (*FunctionName(float functionParam)^)(int, int)
		 * void (&^)(int, int)
		 *	---
		 *
		 *	Note that this is the same as where <TryToSkipFunctionPointer_OpeningParenthesisThroughIdentifier()> leaves it *except*
		 *	for functions returning function pointers.  In this case it is after the parameters of the function that returns the function
		 *	pointer.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipFunctionPointer_ClosingParenthesisThroughParameters (ref TokenIterator iterator,
																														    ParseMode mode = ParseMode.IterateOnly)
			{

			// Closing paren

			if (iterator.Character != ')')
				{  return false;  }

			TokenIterator lookahead = iterator;

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Function pointer parameters

			// We don't want these formatted the same as regular function parameters so mark the parentheses as type modifiers.
			if (!TryToSkipParameters(ref lookahead, mode, nameless: true, markAsTypeModifiers: true))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipFunctionPointer_TrailingModifiers
		 *
		 * If you know you're handling a function pointer, this will skip any modifiers that could appear after it.  This is inside the
		 * parentheses for functions that return function pointers and outside for other uses, such as below.  The iterator should be
		 * at the spot indicated by ^:
		 *
		 * --- C++ ---
		 * void (*)(int, int) ^noexcept
		 * void (*name)(int, int) ^&& const
		 * void (*FunctionName(float functionParam) ^noexcept(true))(int, int)
		 *	---
		 *
		 *	This function will return whether it moved the iterator, though since it's valid for there to be no trailing modifiers you should
		 *	not treat that as a parsing failure in the calling function.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipFunctionPointer_TrailingModifiers (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;
			bool result = false;


			// const/volatile

			if (IsOnAnyKeyword(iterator, "const", "volatile"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

				lookahead.Next();

				result = true;
				iterator = lookahead;

				TryToSkipWhitespace(ref lookahead);
				}


			// &/&&

			if (lookahead.Character == '&')
				{
				do
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

					lookahead.Next();
					}
				while (lookahead.Character == '&');

				result = true;
				iterator = lookahead;

				TryToSkipWhitespace(ref lookahead);
				}


			// noexcept/throw

			if (IsOnAnyKeyword(lookahead, "noexcept", "throw"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

				lookahead.Next();

				result = true;
				iterator = lookahead;

				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character == '(')
					{
					TokenIterator endingParen = lookahead;
					endingParen.Next();

					if (GenericSkipUntilOn(ref endingParen, ')', skipToEndIfNotFound: false))
						{
						if (mode == ParseMode.ParsePrototype)
							{
							lookahead.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;
							endingParen.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;

							lookahead = endingParen;
							lookahead.Next();
							}
						}

					// result is already true
					iterator = lookahead;
					}
				}

			return result;
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
		 *			- Set classPrototypeParsingType to the type you would like them to be marked as, such as <ClassPrototypeParsringType.Modifier>,
		 *			  <ClassPrototypeParsingType.StartOfPrePrototypeLine>, or <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttributes (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
															PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier,
															ClassPrototypeParsingType classPrototypeParsingType = ClassPrototypeParsingType.Modifier)
			{
			if (TryToSkipAttribute(ref iterator, mode, prototypeParsingType, classPrototypeParsingType))
				{
				TokenIterator lookahead = iterator;

				for (;;)
					{
					TryToSkipWhitespace(ref lookahead, true, mode);

					if (TryToSkipAttribute(ref lookahead, mode, prototypeParsingType, classPrototypeParsingType))
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
		 *			- Set classPrototypeParsingType to the type you would like them to be marked as, such as <ClassPrototypeParsringType.Modifier>,
		 *			  <ClassPrototypeParsingType.StartOfPrePrototypeLine>, or <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttribute (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
														  PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier,
														  ClassPrototypeParsingType classPrototypeParsingType = ClassPrototypeParsingType.Modifier)
			{
			return (TryToSkipBracketedAttribute(ref iterator, mode, prototypeParsingType, classPrototypeParsingType) ||
					   TryToSkipUnderscoredAttribute(ref iterator, mode, prototypeParsingType, classPrototypeParsingType) ||
					   TryToSkipDeclSpecAttribute(ref iterator, mode, prototypeParsingType, classPrototypeParsingType));
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
		 *			- Set classPrototypeParsingType to the type you would like them to be marked as, such as <ClassPrototypeParsringType.Modifier>,
		 *			  <ClassPrototypeParsingType.StartOfPrePrototypeLine>, or <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipBracketedAttribute (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
																		PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier,
																		ClassPrototypeParsingType classPrototypeParsingType = ClassPrototypeParsingType.Modifier)

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

				else if (TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.PropertyValue))
					{
					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == '(')
						{
						TokenIterator openingParen = lookahead;
						lookahead.Next();

						if (!GenericSkipUntilAfter(ref lookahead, ')'))
							{
							ResetTokensBetween(iterator, lookahead, mode);
							return false;
							}

						if (mode == ParseMode.ParsePrototype)
							{  openingParen.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.PropertyValue);  }

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
				if (classPrototypeParsingType == ClassPrototypeParsingType.StartOfPrePrototypeLine ||
					classPrototypeParsingType == ClassPrototypeParsingType.PrePrototypeLine)
					{
					iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;
					iterator.Next();

					iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.PrePrototypeLine);
					}
				else
					{
					iterator.SetClassPrototypeParsingTypeBetween(lookahead, classPrototypeParsingType);
					}
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
		 *			- Set classPrototypeParsingType to the type you would like them to be marked as, such as <ClassPrototypeParsringType.Modifier>,
		 *			  <ClassPrototypeParsingType.StartOfPrePrototypeLine>, or <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipUnderscoredAttribute (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
																			PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier,
																			ClassPrototypeParsingType classPrototypeParsingType = ClassPrototypeParsingType.Modifier)
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
				if (classPrototypeParsingType == ClassPrototypeParsingType.StartOfPrePrototypeLine ||
					classPrototypeParsingType == ClassPrototypeParsingType.PrePrototypeLine)
					{
					iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;
					iterator.Next();

					iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.PrePrototypeLine);
					}
				else
					{
					iterator.SetClassPrototypeParsingTypeBetween(lookahead, classPrototypeParsingType);
					}
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
		 *			- Set classPrototypeParsingType to the type you would like them to be marked as, such as <ClassPrototypeParsringType.Modifier>,
		 *			  <ClassPrototypeParsingType.StartOfPrePrototypeLine>, or <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDeclSpecAttribute (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
																	   PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier,
																	   ClassPrototypeParsingType classPrototypeParsingType = ClassPrototypeParsingType.Modifier)
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
				if (classPrototypeParsingType == ClassPrototypeParsingType.StartOfPrePrototypeLine ||
					classPrototypeParsingType == ClassPrototypeParsingType.PrePrototypeLine)
					{
					iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;
					iterator.Next();

					iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.PrePrototypeLine);
					}
				else
					{
					iterator.SetClassPrototypeParsingTypeBetween(lookahead, classPrototypeParsingType);
					}
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipKeywordAndParentheses
		 *
		 * Tries to move the iterator past a single keyword followed by parentheses, such as "alignas(8)".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.TypeModifier>,
		 *			  <PrototypeParsingType.ParamModifier>, or <PrototypeParsingType.StartOfPrototypeSection>.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Set classPrototypeParsingType to the type you would like them to be marked as, such as <ClassPrototypeParsingType.Modifier>,
		 *			  <ClassPrototypeParsingType.StartOfPrePrototypeLine>, or <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipKeywordAndParentheses (ref TokenIterator iterator, string keyword, ParseMode mode = ParseMode.IterateOnly,
																				bool allowParentheses = true, bool requireParentheses = false,
																			    PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier,
																				ClassPrototypeParsingType classPrototypeParsingType = ClassPrototypeParsingType.Modifier)
			{
			if (!IsOnKeyword(iterator, keyword))
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.NextByCharacters(keyword.Length);

			TokenIterator endOfAttribute = lookahead;

			if (allowParentheses || requireParentheses)
				{
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character == '(')
					{
					lookahead.Next();

					if (GenericSkipUntilAfter(ref lookahead, ')'))
						{  endOfAttribute = lookahead;  }
					}
				else if (requireParentheses)
					{  return false;  }
				}


			// Mark tokens

			if (mode == ParseMode.ParsePrototype)
				{
				if (prototypeParsingType == PrototypeParsingType.StartOfPrototypeSection)
					{
					iterator.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;
					endOfAttribute.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;
					}
				else
					{
					iterator.SetPrototypeParsingTypeBetween(endOfAttribute, prototypeParsingType);
					}
				}

			else if (mode == ParseMode.ParseClassPrototype)
				{
				if (classPrototypeParsingType == ClassPrototypeParsingType.StartOfPrePrototypeLine ||
					classPrototypeParsingType == ClassPrototypeParsingType.PrePrototypeLine)
					{
					iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;
					iterator.Next();

					iterator.SetClassPrototypeParsingTypeBetween(endOfAttribute, ClassPrototypeParsingType.PrePrototypeLine);
					}
				else
					{
					iterator.SetClassPrototypeParsingTypeBetween(endOfAttribute, classPrototypeParsingType);
					}
				}

			iterator = endOfAttribute;
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


		/* Function: TryToSkipMTNGroup
		 *
		 * Tries to move the iterator past a modifier-type-name group, such as "unsigned int Name".  It will continue until it reaches certain
		 * symbols relevant to the <MTNType> passed.  See <MTNType> and <TryToSkipMTNWord()> for behavior notes.
		 *
		 * You can optionally pass an array of keywords that will cause this function to fail if any of them are encountered.  This allows you to
		 * prevent MTN groups from possibly misinterpreting a code element by treating a keyword for a different one as a modifier.
		 *
		 * This will skip parentheses following modifiers (such as "alignas(8)") but not the one after the last MTN word, such as the parentheses
		 * following a function name.
		 *
		 * This function will return whether the last word was part function pointer.  If so, the iterator will be left on the place documented in
		 * <TryToSkipFunctionPointer_OpeningParenthesisThroughIdentifier()>.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipMTNGroup (ref TokenIterator iterator, MTNType mtnType, out bool isPartOfFunctionPointer,
															ParseMode mode = ParseMode.IterateOnly, string[] failOnKeywords = null)
			{
			TokenIterator lookahead = iterator;
			isPartOfFunctionPointer = false;


			// Attributes

			var attributePrototypeParsingType = (mtnType == MTNType.Function ? PrototypeParsingType.StartOfPrototypeSection
																												: PrototypeParsingType.TypeModifier);

			while (TryToSkipAttributes(ref lookahead, mode, prototypeParsingType: attributePrototypeParsingType) ||
					  TryToSkipTemplateDeclaration(ref lookahead, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Pass 1: Count the number of MTN words in the group.  We need to accept parentheses that appear after macros.

			TokenIterator startOfWords = lookahead;
			TokenIterator endOfWords = lookahead;
			int wordCount = 0;
			bool lastWordHadParentheses = false;
			bool isOperatorOverload = false;
			bool isTypeConversionOperator = false;

			while (lookahead.IsInBounds)
				{
				// Check for symbols that end the MTN group

				if (mtnType == MTNType.Parameter ||
					mtnType == MTNType.NamelessParameter)
					{
					if (lookahead.Character == ',' ||  // End of parameter
						lookahead.Character == ')' ||  // End of all parameters
						lookahead.Character == '>' ||  // End of all template parameters
						lookahead.Character == '=')  // Default value
						{  break;  }
					}

				else if (mtnType == MTNType.Variable)
					{
					if (lookahead.Character == ',' ||  // End of variable in a multi-variable declaration
						lookahead.Character == ';' ||  // End of definition
						lookahead.Character == '=')  // Default value
						{  break;  }
					}

				else if (mtnType == MTNType.Function)
					{
					if (lookahead.Character == ';' ||  // End of declaration
						lookahead.Character == '{' ||  // Body
						lookahead.Character == '=' ||  // = 0, = default, etc.
						lookahead.Character == ':' ||  // Calls to the base constructor
						lookahead.MatchesAcrossTokens("->"))  // Auto return values
						{  break;  }

					// Keywords that follow the parameters
					else if (IsOnAnyKeyword(lookahead, "volatile", "try", "throw", "noexcept", "requires", "override", "final"))
						{  break;  }

					// "const", "&", and "&&" can appear before a type and after the parentheses
					}

				else if (mtnType == MTNType.TrailingReturnType)
					{
					if (lookahead.Character == ';' ||  // End of declaration
						lookahead.Character == '{' ||  // Body
						lookahead.Character == '=')  // = 0, = default, etc.
						{  break;  }

					// Keywords that follow the return type
					else if (IsOnAnyKeyword(lookahead, "requires", "override", "final"))
						{  break;  }
					}

				else
					{  throw new NotImplementedException();  }


				// Check for keywords that signify a failure

				if (failOnKeywords != null &&
					IsOnAnyKeyword(lookahead, failOnKeywords))
					{  return false;  }


				// If we're on an operator overload identifier while processing a function, add it as a single word and it ends the MTN group.

				if (mtnType == MTNType.Function &&
					TryToSkipOperatorOverloadIdentifier(ref lookahead, out isTypeConversionOperator))
					{
					wordCount++;
					endOfWords = lookahead;
					isOperatorOverload = true;

					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == '(')
						{
						lookahead.Next();
						lastWordHadParentheses = GenericSkipUntilAfter(ref lookahead, ')', angleBracketsAsBlocks: true, skipToEndIfNotFound: false);
						}

					break;
					}


				// If we're on the opening parenthesis of a function pointer, add the part through the name and end the MTN group.

				else if (TryToSkipFunctionPointer_OpeningParenthesisThroughIdentifier(ref lookahead))
					{
					wordCount++;
					endOfWords = lookahead;
					isPartOfFunctionPointer = true;

					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == '(')
						{
						lookahead.Next();
						lastWordHadParentheses = GenericSkipUntilAfter(ref lookahead, ')', angleBracketsAsBlocks: true, skipToEndIfNotFound: false);
						}

					break;
					}


				// Otherwise add the word and continue

				else if (TryToSkipMTNWord(ref lookahead))
					{
					wordCount++;
					endOfWords = lookahead;

					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == '(')
						{
						// Make sure we don't treat the parentheses in function pointers, such as "void(*paramName)(int, int)", as
						// parentheses on regular modifiers, such as "alignas(8)".
						TokenIterator temp = lookahead;
						if (!TryToSkipFunctionPointer_OpeningParenthesisThroughIdentifier(ref temp, ParseMode.IterateOnly))
							{
							lookahead.Next();
							lastWordHadParentheses = GenericSkipUntilAfter(ref lookahead, ')', angleBracketsAsBlocks: true, skipToEndIfNotFound: false);
							TryToSkipWhitespace(ref lookahead);
							}
						else
							{  lastWordHadParentheses = false;  }
						}
					else
						{  lastWordHadParentheses = false;  }
					}

				else
					{  break;  }
				}

			if (wordCount == 0)
				{  return false;  }

			if ( (lastWordHadParentheses && mtnType != MTNType.Function) ||
				 (!lastWordHadParentheses && mtnType == MTNType.Function) )
				{  return false;  }


			// Pass 2: Mark the words.  The order of words goes [modifier] [modifier] [type] [name], though some MTN types won't
			// have a name.  We can skip this stage if we're not marking tokens.

			if (mode == ParseMode.ParsePrototype)
				{
				lookahead = startOfWords;
				TokenIterator wordStart, wordEnd;
				int wordsLeft = wordCount;

				bool hasName = ( (mtnType == MTNType.Function && !isTypeConversionOperator) ||
										   (mtnType == MTNType.Parameter && wordCount > 1) ||
										   mtnType == MTNType.Variable);

				while (wordsLeft > 0)
					{
					if (wordsLeft == 1 && isOperatorOverload)
						{
						TryToSkipOperatorOverloadIdentifier(ref lookahead, out _, mode);
						}

					else if (wordsLeft == 1 && isPartOfFunctionPointer)
						{
						TryToSkipFunctionPointer_OpeningParenthesisThroughIdentifier(ref lookahead, mode);
						}

					else
						{
						wordStart = lookahead;
						TryToSkipMTNWord(ref lookahead);
						wordEnd = lookahead;

						// Type modifier
						if (wordsLeft >= 3 || (!hasName && wordsLeft >= 2))
							{
							wordStart.SetPrototypeParsingTypeBetween(wordEnd, PrototypeParsingType.TypeModifier);
							}

						// Type
						else if (wordsLeft == 2 || (!hasName && wordsLeft == 1))
							{
							MarkType(wordStart, wordEnd);
							}

						// Name
						else if (wordsLeft == 1)
							{
							MarkName(wordStart, wordEnd);
							}

						// Parentheses.  Only skip if it's not the last word, or the second to last word when the next set of parentheses
						// is to a function pointer.
						if (wordsLeft > 1 && !(wordsLeft == 2 && isPartOfFunctionPointer))
							{
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
								}
							}

						if (wordsLeft > 1)
							{  TryToSkipWhitespace(ref lookahead);  }
						}

					wordsLeft--;
					}
				}

			iterator = endOfWords;
			return true;
			}


		/* Function: TryToSkipMTNWord
		 *
		 * If the iterator is on a modifier-type-name series, moves the iterator past one word and returns true.  A word can be a modifier
		 * like "const" or "unsigned", a type like "int", or a name like "PackageName::FunctionName".  Counting the words helps determine
		 * how each one is interpreted.
		 *
		 * Behavior:
		 *
		 * - Attributes like  "[[deprecated]]" and macros like "__declspec" are treated as attributes.
		 *
		 * - It will skip most symbols surrounding the word, such as "*int" and "int[]" and "List<int>".
		 *
		 * - It will not skip parentheses.  Parentheses following macros or modifiers like "alignas(8)" have to be handled separately.
		 */
		protected bool TryToSkipMTNWord (ref TokenIterator iterator)
			{
			if (TryToSkipAttribute(ref iterator, ParseMode.IterateOnly))
				{  return true;  }

			TokenIterator lookahead = iterator;


			// Pointer/reference symbols

			while (lookahead.Character == '*' || lookahead.Character == '&')
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Identifier

			if (TryToSkipIdentifier(ref lookahead, ParseMode.IterateOnly))
				{
				iterator = lookahead;
				TryToSkipWhitespace(ref lookahead);


				// Template signature

				if (TryToSkipTemplateSignature(ref lookahead, TemplateSignatureType.Instantiation, ParseMode.IterateOnly))
					{
					iterator = lookahead;
					TryToSkipWhitespace(ref lookahead);
					}


				// Array brackets

				if (TryToSkipArrayBrackets(ref lookahead, ParseMode.IterateOnly))
					{
					iterator = lookahead;
					}


				// Parameter pack

				if (lookahead.MatchesAcrossTokens("..."))
					{
					lookahead.Next(3);
					iterator = lookahead;
					}

				return true;
				}


			// Standalone ellipsis for variadic functions

			else if (lookahead.MatchesAcrossTokens("..."))
				{
				lookahead.Next(3);
				iterator = lookahead;
				return true;
				}

			else
				{  return false;  }
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
				bool hadWhitespace = TryToSkipWhitespace(ref lookahead);

				if (TryToSkipTemplateSignature(ref lookahead, templateSignatureType, mode))
					{
					hadWhitespace = TryToSkipWhitespace(ref lookahead);
					}

				if (lookahead.MatchesAcrossTokens("::"))
					{
					// If there was whitespace before this :: make sure the previous identifier wasn't "operator".  Otherwise type conversion
					// operators with global classes like "operator ::GlobalClass()" could cause it to be seen as one identifier.
					if (hadWhitespace)
						{
						TokenIterator lookbehind = endOfIdentifier;
						lookbehind.Previous();

						if (IsOnKeyword(lookbehind, "operator"))
							{  break;  }
						}

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
