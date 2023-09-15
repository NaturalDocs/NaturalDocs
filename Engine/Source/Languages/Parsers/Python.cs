/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Python
 * ____________________________________________________________________________
 *
 * Additional language support for Python.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class Python : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Python
		 */
		public Python (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: SyntaxHighlight
		 */
		override public void SyntaxHighlight (Tokenizer source)
			{
			TokenIterator iterator = source.FirstToken;

			while (iterator.IsInBounds)
				{
				if (TryToSkipComment(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipString(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipNumber(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipKeyword(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipDecorator(ref iterator, ParseMode.SyntaxHighlight))
					{
					}
				else
					{  iterator.Next();  }
				}
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */
		override public ParsedPrototype ParsePrototype (string stringPrototype, int commentTypeID)
			{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			ParsedPrototype parsedPrototype;


			// Mark any leading decorators.

			TokenIterator iterator = tokenizedPrototype.FirstToken;

			TryToSkipWhitespace(ref iterator, true, ParseMode.ParsePrototype);

			if (TryToSkipDecorators(ref iterator, ParseMode.ParsePrototype))
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
				else if (iterator.Character == '[')
					{
					// Only treat brackets as parameters if it's following "this", meaning it's an iterator.  Ignore all others so we
					// don't get tripped up on metadata or array brackets on return values.

					TokenIterator lookbehind = iterator;
					lookbehind.Previous();
					lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator);

					if (lookbehind.MatchesToken("this"))
						{
						closingBracket = ']';
						break;
						}
					else
						{  iterator.Next();  }
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
							   TryToSkipBlock(ref iterator, true))
						{  }

					else
						{  iterator.Next();  }
					}


				// We have enough tokens marked to create the parsed prototype.  This will also let us iterate through the parameters
				// easily.

				parsedPrototype = new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID,
																		  parameterStyle: ParameterStyle.Pascal, supportsImpliedTypes: false);


				// Set the main section to the last one, since any decorators present will each be in their own section.  Some can have
				// parameter lists and we don't want those confused for the actual parameter list.

				parsedPrototype.MainSectionIndex = parsedPrototype.Sections.Count - 1;


				// Mark the part before the parameters, which includes the name and return value.

				TokenIterator start, end;
				parsedPrototype.GetBeforeParameters(out start, out end);

				// Exclude the opening bracket
				end.Previous();
				end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

				if (start < end)
					{  MarkPascalParameter(start, end);  }


				// If there are any parameters, mark the tokens in them.

				if (parsedPrototype.NumberOfParameters > 0)
					{
					for (int i = 0; i < parsedPrototype.NumberOfParameters; i++)
						{
						parsedPrototype.GetParameter(i, out start, out end);
						MarkPascalParameter(start, end);
						}
					}
				}


			// If there's no brackets, it's a variable, property, or class.

			else
				{
				parsedPrototype = new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID,
																		  parameterStyle: ParameterStyle.Pascal, supportsImpliedTypes: false);
				TokenIterator start = tokenizedPrototype.FirstToken;
				TokenIterator end = tokenizedPrototype.EndOfTokens;

				MarkPascalParameter(start, end);
				}

			return parsedPrototype;
			}


		/* Function: ParseClassPrototype
		 * Converts a raw text prototype into a <ParsedClassPrototype>.  Will return null if it is not an appropriate prototype.
		 */
		override public ParsedClassPrototype ParseClassPrototype (string stringPrototype, int commentTypeID)
			{
			if (EngineInstance.CommentTypes.InClassHierarchy(commentTypeID) == false)
				{  return null;  }

			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator startOfPrototype = tokenizedPrototype.FirstToken;
			ParsedClassPrototype parsedPrototype = new ParsedClassPrototype(tokenizedPrototype);
			bool success = false;

			success = TryToSkipClassDeclarationLine(ref startOfPrototype, ParseMode.ParseClassPrototype);

			if (success)
				{  return parsedPrototype;  }
			else
			    {  return base.ParseClassPrototype(stringPrototype, commentTypeID);  }
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToSkipClassDeclarationLine
		 *
		 * If the iterator is on a class's declaration line, moves it past it and returns true.  It does not handle the class body.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParseClassPrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipClassDeclarationLine (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;


			// Decorators

			if (TryToSkipDecorators(ref lookahead, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword

			if (lookahead.MatchesToken("class") == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParseClassPrototype)
				{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.Keyword;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Name

			TokenIterator startOfIdentifier = lookahead;

			if (TryToSkipIdentifier(ref lookahead) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParseClassPrototype)
				{  startOfIdentifier.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.Name);  }

			TryToSkipWhitespace(ref lookahead);


			// Base classes

			if (lookahead.Character == '(')
				{
				if (mode == ParseMode.ParseClassPrototype)
					{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfParents;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				for (;;)
					{
					if (lookahead.Character == ')')
						{
						if (mode == ParseMode.ParseClassPrototype)
							{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.EndOfParents;  }

						break;
						}

					if (TryToSkipClassParent(ref lookahead, mode) == false)
						{
						ResetTokensBetween(iterator, lookahead, mode);
						return false;
						}

					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == ',')
						{
						if (mode == ParseMode.ParseClassPrototype)
							{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.ParentSeparator;  }

						lookahead.Next();
						TryToSkipWhitespace(ref lookahead);
						}
					}
				}


			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipDecorators
		 *
		 * Tries to move the iterator past a group of decorators.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark each decorator with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDecorators (ref TokenIterator iterator, ParseMode mode = ParseMode.ParseClassPrototype)
			{
			if (TryToSkipDecorator(ref iterator, mode) == false)
				{  return false;  }

			for (;;)
				{
				TokenIterator lookahead = iterator;
				TryToSkipWhitespace(ref lookahead);

				if (TryToSkipDecorator(ref lookahead, mode) == true)
					{  iterator = lookahead;  }
				else
					{  break;  }
				}

			return true;
			}


		/* Function: TryToSkipDecorator
		 *
		 * Tries to move the iterator past a single decorator.  Note that there may be more than one decorator in a row, so use <TryToSkipDecorators()>
		 * if you need to move past all of them.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Each decorator will create a new prototype section.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark the first token with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and the rest with <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDecorator (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '@')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (TryToSkipIdentifier(ref lookahead) == false)
				{  return false;  }

			TokenIterator decoratorStart = iterator;
			TokenIterator decoratorEnd = lookahead;

			if (mode == ParseMode.SyntaxHighlight)
				{  decoratorStart.SetSyntaxHighlightingTypeBetween(decoratorEnd, SyntaxHighlightingType.Metadata);  }

			TryToSkipWhitespace(ref lookahead);

			if (TryToSkipDecoratorParameters(ref lookahead, mode))
				{  decoratorEnd = lookahead;  }

			if (mode == ParseMode.ParsePrototype)
				{
				decoratorStart.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;
				decoratorEnd.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;
				}
			else if (mode == ParseMode.ParseClassPrototype)
				{
				iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.PrePrototypeLine);
				iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;
				}

			iterator = decoratorEnd;
			return true;
			}


		/* Function: TryToSkipDecoratorParameters
		 *
		 * Tries to move the iterator past a decorator parameter section, such as "("String")" in "@Copynight("String")".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- The contents will be marked with parameter tokens.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDecoratorParameters (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '(')
				{  return false;  }

			TokenIterator lookahead = iterator;

			if (!TryToSkipBlock(ref lookahead, false))
				{  return false;  }

			TokenIterator end = lookahead;

			if (mode == ParseMode.SyntaxHighlight)
				{
				iterator.SetSyntaxHighlightingTypeBetween(end, SyntaxHighlightingType.Metadata);
				}

			else if (mode == ParseMode.ParsePrototype)
				{
				TokenIterator openingParen = iterator;

				TokenIterator closingParen = lookahead;
				closingParen.Previous();

				openingParen.PrototypeParsingType = PrototypeParsingType.StartOfParams;
				closingParen.PrototypeParsingType = PrototypeParsingType.EndOfParams;

				lookahead = openingParen;
				lookahead.Next();

				TokenIterator startOfParam = lookahead;

				while (lookahead < closingParen)
					{
					if (lookahead.Character == ',')
						{
						MarkDecoratorParameter(startOfParam, lookahead, mode);

						lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;
						lookahead.Next();

						startOfParam = lookahead;
						}

					else if (TryToSkipComment(ref lookahead) ||
							   TryToSkipString(ref lookahead) ||
							   TryToSkipBlock(ref lookahead, true))
						{  }

					else
						{  lookahead.Next();  }
					}

				MarkDecoratorParameter(startOfParam, lookahead, mode);
				}

			iterator = end;
			return true;
			}


		/* Function: MarkDecoratorParameter
		 *
		 * Applies types to an decorator parameter, such as ""String"" in "@Copynight("String")" or "id = 12" in
		 * "@RequestForEnhancement(id = 12, engineer = "String")".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.ParsePrototype>
		 *			- The contents will be marked with parameter tokens.
		 *		- Everything else has no effect.
		 */
		protected void MarkDecoratorParameter (TokenIterator start, TokenIterator end, ParseMode mode = ParseMode.IterateOnly)
			{
			if (mode != ParseMode.ParsePrototype)
				{  return;  }

			start.NextPastWhitespace(end);
			end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

			if (start >= end)
				{  return;  }


			// Find and mark the equals sign, if there is one

			TokenIterator equals = start;

			while (equals < end)
				{
				if (equals.Character == '=')
					{
					equals.PrototypeParsingType = PrototypeParsingType.PropertyValueSeparator;
					break;
					}
				else if (TryToSkipComment(ref equals) ||
							TryToSkipString(ref equals) ||
							TryToSkipBlock(ref equals, true))
					{  }
				else
					{  equals.Next();  }
				}


			// The equals sign will be at or past the end if it doesn't exist.

			if (equals >= end)
				{
				start.SetPrototypeParsingTypeBetween(end, PrototypeParsingType.PropertyValue);
				}
			else
				{
				TokenIterator iterator = equals;
				iterator.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

				if (start < iterator)
					{  start.SetPrototypeParsingTypeBetween(iterator, PrototypeParsingType.Name);  }

				iterator = equals;
				iterator.Next();
				iterator.NextPastWhitespace(end);

				if (iterator < end)
					{  iterator.SetPrototypeParsingTypeBetween(end, PrototypeParsingType.PropertyValue);  }
				}
			}


		/* Function: TryToSkipClassParent
		 *
		 * Tries to move the iterator past a single class parent declaration.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParseClassPrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipClassParent (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;

			if (lookahead.MatchesToken("metaclass"))
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character == '=')
					{
					if (mode == ParseMode.ParseClassPrototype)
						{  iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.Modifier;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{
					// Nevermind, reset
					lookahead = iterator;
					}
				}


			TokenIterator startOfIdentifier = lookahead;

			if (TryToSkipIdentifier(ref lookahead) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParseClassPrototype)
				{  startOfIdentifier.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.Name);  }

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
			// All python keywords are a single text token

			if (iterator.FundamentalType != FundamentalType.Text)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.FundamentalType == FundamentalType.Text ||
				lookahead.Character == '_')
				{  return false;  }

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			if (lookbehind.FundamentalType == FundamentalType.Text ||
				lookbehind.Character == '_')
				{  return false;  }

			if (!pythonKeywords.Contains(iterator.String))
				{  return false;  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

			iterator.Next();
			return true;
			}


		/* Function: TryToSkipIdentifier
		 * Tries to move the iterator past a qualified identifier, such as "X.Y.Z".  Use <TryToSkipUnqualifiedIdentifier()> if you only want
		 * to skip a single segment.
		 */
		protected bool TryToSkipIdentifier (ref TokenIterator iterator)
			{
			TokenIterator lookahead = iterator;

			for (;;)
				{
				if (TryToSkipUnqualifiedIdentifier(ref lookahead) == false)
					{  return false;  }

				if (lookahead.Character == '.')
					{  lookahead.Next();  }
				else
					{  break;  }
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipUnqualifiedIdentifier
		 * Tries to move the iterator past a single unqualified identifier, which means only "X" in "X.Y.Z".
		 */
		protected bool TryToSkipUnqualifiedIdentifier (ref TokenIterator iterator)
			{
			if (iterator.FundamentalType == FundamentalType.Text)
				{
				if (iterator.Character >= '0' && iterator.Character <= '9')
					{  return false;  }
				}
			else if (iterator.FundamentalType == FundamentalType.Symbol)
				{
				if (iterator.Character != '_')
					{  return false;  }
				}
			else
				{  return false;  }

			do
				{  iterator.Next();  }
			while (iterator.FundamentalType == FundamentalType.Text || iterator.Character == '_');

			return true;
			}



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: pythonKeywords
		 */
		static protected StringSet pythonKeywords = new StringSet (KeySettings.Literal, new string[] {

			"False", "await", "else", "import", "pass",
			"None", "break", "except", "in", "raise",
			"True", "class", "finally", "is", "return",
			"and", "continue", "for", "lambda", "try",
			"as", "def", "from", "nonlocal", "while",
			"assert", "del", "global", "not" ,"with",
			"async", "elif", "if", "or", "yield",

			"metaclass",

			"int", "float", "complex",
			"list", "tuple", "range",
			"str",
			"bytes", "bytearray", "memoryview",
			"set", "frozenset", "dict",
			"bool"

			});

		}
	}
