/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Kotlin
 * ____________________________________________________________________________
 *
 * Additional language support for Kotlin.
 *
 * Resources:
 *		- <Docs Home: https://kotlinlang.org/docs/home.html>
 *		- <Language Specification: https://kotlinlang.org/spec/> and <Grammar: https://kotlinlang.org/grammar/>
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
	public class Kotlin : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Kotlin
		 */
		public Kotlin (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: TryToFindBasicPrototype
		 */
		override protected bool TryToFindBasicPrototype (Topic topic, TokenIterator start, TokenIterator limit,
																			   out TokenIterator prototypeStart, out TokenIterator prototypeEnd)
			{
			// Skip the annotations before each item.  This allows us to ignore their line breaks in prototypes where line breaks
			// are significant.  As a bonus it also prevents the annotations from being matched against the topic title.
			TokenIterator lookahead = start;

			TryToSkipWhitespace(ref lookahead, mode: ParseMode.ParsePrototype);
			if (TryToSkipAnnotations(ref lookahead, mode: ParseMode.ParsePrototype, breakPrototypeSections: false))
				{  TryToSkipWhitespace(ref lookahead, mode: ParseMode.ParsePrototype);  }

			if (base.TryToFindBasicPrototype(topic, lookahead, limit, out prototypeStart, out prototypeEnd))
				{
				prototypeStart = start;
				return true;
				}
			else
				{  return false;  }
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: MarkParameter
		 */
		override protected void MarkParameter (TokenIterator start, TokenIterator end, ParameterStyle parameterStyle = ParameterStyle.Unknown)
			{
			// Mark and skip the annotations before each parameter.  This allows us to use the generic function for the rest
			// of it and not have it be confiused by the colons in annotation scopes like on "@field:Ann val x".
			TryToSkipWhitespace(ref start, mode: ParseMode.ParsePrototype);
			TryToSkipAnnotations(ref start, mode: ParseMode.ParsePrototype, breakPrototypeSections: false);

			MarkPascalParameter(start, end);
			}


		/* Function: TryToSkipMetadata
		 *
		 * Override to support detecting annotations as metadata.
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
			return TryToSkipAnnotation(ref iterator, mode, breakPrototypeSections: true);
			}


		/* Function: TryToSkipAnnotations
		 *
		 * Tries to move the iterator past one or more annotations, like "@Preliminary" or "@Copynight("String")".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Each annotation will create a new prototype section.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAnnotations (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
															   bool breakPrototypeSections = true)
			{
			if (TryToSkipAnnotation(ref iterator, mode, breakPrototypeSections))
				{
				TokenIterator lookahead = iterator;

				for (;;)
					{
					TryToSkipWhitespace(ref lookahead, true, mode);

					if (TryToSkipAnnotation(ref lookahead, mode, breakPrototypeSections))
						{  iterator = lookahead;  }
					else
						{  break;  }
					}

				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToSkipAnnotation
		 *
		 * Tries to move the iterator past a single annotation, like "@Preliminary" or "@Copynight("String")".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Each annotation will create a new prototype section.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAnnotation (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
															 bool breakPrototypeSections = true)
			{
			if (iterator.Character != '@')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			bool hasScope = kotlinAnnotationScopes.Contains(lookahead.String);

			if (hasScope)
				{
				lookahead.Next();

				if (lookahead.Character == ':')
					{  lookahead.Next();  }
				else
					{
					// Not a scope after all, reset position
					lookahead = iterator;
					hasScope = false;
					}
				}

			if (hasScope && lookahead.Character == '[')
				{
				TryToSkipBlock(ref lookahead, false);
				}
			else
				{
				if (!TryToSkipIdentifier(ref lookahead, mode))
					{  return false;  }

				TokenIterator endOfIdentifier = lookahead;

				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character == '(')
					{  TryToSkipBlock(ref lookahead, false);  }
				else
					{  lookahead = endOfIdentifier;  }
				}

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Metadata);  }

			else if (mode == ParseMode.ParsePrototype)
				{
				iterator.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.ParamModifier);

				if (breakPrototypeSections)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;  }
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipIdentifier
		 *
		 * Tries to move the iterator past a qualified identifier, such as "X.Y.Z".  Use <TryToSkipUnqualifiedIdentifier()> if you only want
		 * to skip a single segment.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
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
		 *
		 * Tries to move the iterator past a single unqualified identifier, which means only "X" in "X.Y.Z".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipUnqualifiedIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;

			if (lookahead.FundamentalType == FundamentalType.Text)
				{
				if (lookahead.Character >= '0' && lookahead.Character <= '9')
					{  return false;  }
				}
			else if (lookahead.FundamentalType == FundamentalType.Symbol)
				{
				if (lookahead.Character != '_')
					{  return false;  }
				}
			else
				{  return false;  }

			iterator = lookahead;

			do
				{  iterator.Next();  }
			while (iterator.FundamentalType == FundamentalType.Text || iterator.Character == '_');

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
			// No keywords have underscores, so they must be text and only one token long.

			if (iterator.FundamentalType != FundamentalType.Text)
				{  return false;  }

			if (kotlinKeywords.Contains(iterator.ToString()) == false)
				{  return false;  }

			// Check if it's part of another identifier ("x_keyword") or surrounded in backticks ("`keyword`")

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			if (lookbehind.Character == '`' || lookbehind.Character == '_' || lookbehind.FundamentalType == FundamentalType.Text)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.Character == '`' || lookahead.Character == '_' || lookahead.FundamentalType == FundamentalType.Text)
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
			char character = iterator.Character;

			if (character != '\'' && character != '\"' && character != '$')
				{  return false;  }

			TokenIterator lookahead = iterator;
			TokenIterator startOfLastStringSegment = iterator;


			// Count dollar signs for $$" strings.  Zero is fine, but if it exists there must be at least two.

			int delimiterDollarSignCount = 0;

			while (lookahead.Character == '$')
				{
				delimiterDollarSignCount++;
				lookahead.Next();
				}

			if (delimiterDollarSignCount == 1)
				{  return false;  }


			// Check for multiline strings.

			char delimiter = lookahead.Character;
			bool isMultiline = false;

			if (lookahead.MatchesAcrossTokens("\"\"\""))
				{
				isMultiline = true;
				lookahead.Next(3);
				}
			else if (delimiter == '\"' || delimiter == '\'')
				{  lookahead.Next();  }
			else
				{  return false;  }


			// Contents

			while (lookahead.IsInBounds)
				{
				if (!isMultiline && lookahead.Character == delimiter)
					{
					lookahead.Next();
					break;
					}

				else if (isMultiline && lookahead.MatchesAcrossTokens("\"\"\""))
					{
					lookahead.Next(3);
					break;
					}

				// Interpolated strings
				else if (lookahead.Character == '$')
					{
					TokenIterator startOfInterpolatedCode = lookahead;

					int contentDollarSignCount = ConsecutiveCharacterCount(lookahead);
					lookahead.Next(contentDollarSignCount);

					if (lookahead.Character == '{' &&
						( (delimiterDollarSignCount == 0 && contentDollarSignCount == 1) ||
						  (delimiterDollarSignCount >= 2 && contentDollarSignCount == delimiterDollarSignCount))
						)
						{
						if (mode == ParseMode.SyntaxHighlight)
							{  startOfLastStringSegment.SetSyntaxHighlightingTypeBetween(startOfInterpolatedCode, SyntaxHighlightingType.String);  }

						TryToSkipBlock(ref lookahead, false);

						if (mode == ParseMode.SyntaxHighlight)
							{  SyntaxHighlight(startOfInterpolatedCode, lookahead);  }

						startOfLastStringSegment = lookahead;
						}
					}

				else if (isMultiline == false && lookahead.Character == '\\')
					{  lookahead.Next(2);  }
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
			if ( ((iterator.Character >= '0' && iterator.Character <= '9') ||
				   iterator.Character == '-' || iterator.Character == '+' || iterator.Character == '.') == false)
				{  return false;  }

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			// Check that we're not following an underscore or letter.  This prevents "_12" from being seen as a number.

			// We do this check before the tests for +, -, or . because if any of them are following a letter they aren't part of
			// the number.  For example, in "x+2", "x-2", and "x.2" the symbols are not part of the number the way they are
			// in "+2", "-2", or ".2".

			if (lookbehind.FundamentalType == FundamentalType.Text ||
				lookbehind.Character == '_')
				{  return false;  }

			TokenIterator lookahead = iterator;
			bool passedPeriod = false;
			bool lastCharWasE = false;
			bool isHex = false;

			if (lookahead.Character == '-' || lookahead.Character == '+')
				{
				// Distinguish between -1 and x-1

				lookbehind = iterator;
				lookbehind.Previous();

				lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator);

				if (lookbehind.FundamentalType == FundamentalType.Text || lookbehind.Character == '_')
					{  return false;  }

				lookahead.Next();
				}

			if (lookahead.Character == '.')
				{
				lookahead.Next();
				passedPeriod = true;
				}

			if (lookahead.Character >= '0' && lookahead.Character <= '9')
				{
				if (lookahead.Character == '0' && lookahead.RawTextLength > 1)
					{
					char secondChar = iterator.Tokenizer.RawText[ lookahead.RawTextIndex + 1 ];
					isHex = (secondChar == 'x' || secondChar == 'X');
					}

				do
					{  lookahead.Next();  }
				while (lookahead.FundamentalType == FundamentalType.Text || lookahead.Character == '_');

				char lastChar = iterator.Tokenizer.RawText[ lookahead.RawTextIndex - 1 ];
				lastCharWasE = (lastChar == 'e' || lastChar == 'E');
				}
			else
				{  return false;  }

			// We're definitely on a number, so apply the position in case the later lookaheads fail.
			TokenIterator startOfNumber = iterator;
			iterator = lookahead;

			if (lookahead.Character == '.' && !passedPeriod)
				{
				lookahead.Next();

				if (lookahead.Character >= '0' && lookahead.Character <= '9')
					{
					iterator = lookahead;

					do
						{  lookahead.Next();  }
					while (lookahead.FundamentalType == FundamentalType.Text || lookahead.Character == '_');

					passedPeriod = true;

					char lastChar = iterator.Tokenizer.RawText[ lookahead.RawTextIndex - 1 ];
					lastCharWasE = (lastChar == 'e' || lastChar == 'E');
					}
				else
					{  lookahead = iterator;  }
				}

			if (lastCharWasE && !isHex && (lookahead.Character == '-' || lookahead.Character == '+'))
				{
				lookahead.Next();

				if (lookahead.Character >= '0' && lookahead.Character <= '9')
					{
					iterator = lookahead;

					do
						{  lookahead.Next();  }
					while (lookahead.FundamentalType == FundamentalType.Text || lookahead.Character == '_');
					}
				else
					{  lookahead = iterator;  }
				}

			if (mode == ParseMode.SyntaxHighlight)
				{  startOfNumber.SetSyntaxHighlightingTypeBetween(iterator, SyntaxHighlightingType.Number);  }

			return true;
			}



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: kotlinKeywords
		 */
		static protected StringSet kotlinKeywords = new StringSet (KeySettings.Literal, new string[] {

			// Hard keywords as of April 2026

			"as", "break", "class", "continue", "do", "else", "false", "for", "fun", "if", "in", "interface", "is", "null", "object",
			"package", "return", "super", "this", "throw", "true", "try", "typalias", "typeof", "val", "var", "when", "while",

			// Soft keywords

			"by", "catch", "constructor", "delegate", "dynamic", "field", "file", "finally", "get", "import", "init", "param",
			"property", "receiver", "set", "setparam", "value", "where",

			// Modifier keywords

			"abstract", "actual", "annotation", "companion", "const", "crossinline", "data", "enum", "expect", "external",
			"final", "infix", "inline", "inner", "internal", "lateinit", "noinline", "open", "operator", "out", "override",
			"private", "protected", "public", "reified", "sealed", "suspend", "tailrec", "vararg",

			// Special identifiers

			"field", "it",

			// Types

			"Nothing", "Any", "Unit",
			"Byte", "Short", "Int", "Long", "UByte", "UShort", "UInt", "ULong", "Float", "Double", "Boolean",
			"Char", "String"

			});

		/* var: kotlinAnnotationScopes
		 */
		static protected StringSet kotlinAnnotationScopes = new StringSet (KeySettings.Literal, new string[] {

			"file", "field", "property", "get", "set", "all", "receiver", "param", "setparam", "delegate"

			});

		}
	}
