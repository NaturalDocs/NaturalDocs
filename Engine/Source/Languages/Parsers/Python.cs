/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Python
 * ____________________________________________________________________________
 *
 * Additional language support for Python.
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
	public class Python : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Python
		 */
		public Python (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
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


		/* Function: TryToSkipMetadata
		 *
		 * Override to support detecting decorators as metadata.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Each annotation will create a new prototype section.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipMetadata (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			return TryToSkipDecorator(ref iterator, mode);
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

					else
						{  GenericSkip(ref lookahead, true);  }
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
				else
					{  GenericSkip(ref equals, true);  }
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
			if (iterator.Character != '\'' && iterator.Character != '\"' &&
				(iterator.FundamentalType == FundamentalType.Text && iterator.TokenLength <= 2) == false)
				{  return false;  }

			TokenIterator lookahead = iterator;
			TokenIterator startOfLastStringSegment = iterator;


			// Text prefix

			bool interpolated = false;

			// We've already established that it's only one or two characters long
			if (lookahead.FundamentalType == FundamentalType.Text)
				{
				char character = lookahead.Character;

				if (character == 'f' || character == 'F' ||
					character == 't' || character == 'T')
					{  interpolated = true;  }
				else if (character != 'r' && character != 'R' &&
						   character != 'b' && character != 'B' &&
						   character != 'u' && character != 'U')
					{  return false;  }

				if (lookahead.TokenLength == 2)
					{
					character = lookahead.Tokenizer.RawText[ lookahead.RawTextIndex + 1 ];

					if (character == 'f' || character == 'F' ||
						character == 't' || character == 'T')
						{  interpolated = true;  }
					else if (character != 'r' && character != 'R' &&
							   character != 'b' && character != 'B')
						{  return false;  }
					}

				lookahead.Next();
				}


			// Opening delimiter

			char delimiter;
			int delimiterCount;

			if (lookahead.MatchesAcrossTokens("'''") ||
				lookahead.MatchesAcrossTokens("\"\"\""))
				{
				delimiter = lookahead.Character;
				delimiterCount = 3;
				lookahead.Next(3);
				}
			else if (lookahead.Character == '\'' ||
					   lookahead.Character == '"')
				{
				delimiter = lookahead.Character;
				delimiterCount = 1;
				lookahead.Next();
				}
			else
				{  return false;  }


			// Contents

			while (lookahead.IsInBounds)
				{
				if (lookahead.Character == delimiter &&
					ConsecutiveCharacterCount(lookahead) >= delimiterCount)
					{
					lookahead.Next(delimiterCount);
					break;
					}

				else if (lookahead.Character == '\\')
					{
					lookahead.Next(2);
					}

				// Interpolated strings
				else if (interpolated && lookahead.Character == '{')
					{
					TokenIterator startOfInterpolatedCode = lookahead;
					lookahead.Next();

					// Double braces are escaped, so ignore
					if (lookahead.Character == '{')
						{  lookahead.Next();  }
					else
						{
						if (mode == ParseMode.SyntaxHighlight)
							{  startOfLastStringSegment.SetSyntaxHighlightingTypeBetween(startOfInterpolatedCode, SyntaxHighlightingType.String);  }

						GenericSkipUntilAfter(ref lookahead, '}', skipToEndIfNotFound: true);

						if (mode == ParseMode.SyntaxHighlight)
							{  SyntaxHighlight(startOfInterpolatedCode, lookahead);  }

						startOfLastStringSegment = lookahead;
						}
					}

				else
					{  lookahead.Next();  }
				}


			// Done

			if (mode == ParseMode.SyntaxHighlight)
				{  startOfLastStringSegment.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.String);  }

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipNumber
		 *
		 * If the iterator is on a numeric literal, moves the iterator past it and returns true.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipNumber (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (TryToSkipNumber(ref iterator,
										  ParseNumberFlags.AllowUnderscoreSeparators,
										  mode))
				{
				// Still need to catch the case of "123.j".  All other cases are handled by the above function.
				if (iterator.Character == 'j' && iterator.TokenLength == 1)
					{
					// We know the character before exists because we just skipped a value, so do this to avoid creating a lookbehind
					// iterator.
					if (iterator.Tokenizer.RawText[ iterator.RawTextIndex - 1 ] == '.')
						{
						TokenIterator lookahead = iterator;
						lookahead.Next();

						if (lookahead.FundamentalType != FundamentalType.Text &&
							lookahead.Character != '_')
							{
							// Now we can add the j to the number
							if (mode == ParseMode.SyntaxHighlight)
								{  iterator.SyntaxHighlightingType = SyntaxHighlightingType.Number;  }

							iterator.Next();
							}
						}
					}

				return true;
				}
			else
				{  return false;  }
			}



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: pythonKeywords
		 */
		static protected StringSet pythonKeywords = new StringSet (KeySettings.Literal, new string[] {

			// Keywords
			"False", "await", "else", "import", "pass",
			"None", "break", "except", "in", "raise",
			"True", "class", "finally", "is", "return",
			"and", "continue", "for", "lambda", "try",
			"as", "def", "from", "nonlocal", "while",
			"assert", "del", "global", "not" ,"with",
			"async", "elif", "if", "or", "yield",

			// Soft Keywords
			"match", "case", "type",

			// Primitive Types
			"int", "float", "complex",
			"bool",
			"list", "tuple", "range",
			"str",
			"bytes", "bytearray", "memoryview",
			"set", "frozenset", "dict",

			// Misc
			"metaclass", "NotImplemented"

			});

		}
	}
