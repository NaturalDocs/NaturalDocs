/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.JavaScript
 * ____________________________________________________________________________
 *
 * Additional language support for JavaScript.
 *
 * Language Version:
 *
 *		The parser is based on ECMAScript 2025, the latest release as of May 2026.
 *
 * Resources:
 *		- <Language Specification: https://262.ecma-international.org/>
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
	public class JavaScript : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: JavaScript
		 */
		public JavaScript (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */
		override public ParsedPrototype ParsePrototype (string stringPrototype, int commentTypeID)
			{
			var result = base.ParsePrototype(stringPrototype, commentTypeID);

			// Convert any instances of "this", "prototype", and "constructor" from Name to KeywordName so they retain their
			// syntax highlighting.
			TokenIterator iterator = result.Tokenizer.FirstToken;

			while (iterator.IsInBounds &&
					 iterator.PrototypeParsingType != PrototypeParsingType.StartOfParams)
				{
				if (iterator.PrototypeParsingType == PrototypeParsingType.Name &&
					IsOnAnyKeyword(iterator, "this", "prototype", "constructor"))
					{
					iterator.PrototypeParsingType = PrototypeParsingType.KeywordName;
					}

				iterator.Next();
				}

			return result;
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
				lookahead.Character == '_' ||
				lookahead.Character == '$')
				{  return true;  }

			// Just use iterator as a lookbehind instead of creating another one
			iterator.Previous();

			if (iterator.FundamentalType == FundamentalType.Text ||
				iterator.Character == '_' ||
				iterator.Character == '$' ||
				iterator.Character == '#')
				{  return true;  }

			return false;
			}


		/* Function: IsOnKeyword
		 * Returns whether the <TokenIterator> is on the passed keyword, making sure there are no other identifier tokens
		 * before or after it.  This allows us to be sure an iterator on "input" isn't actually on "_input" or similar.  This function
		 * assumes keywords are only one text token.
		 */
		public bool IsOnKeyword (TokenIterator iterator, string keyword)
			{
			return (iterator.MatchesToken(keyword) &&
					   !IsPartOfLongerIdentifier(iterator));
			}


		/* Function: IsOnAnyKeyword
		 * Returns whether the <TokenIterator> is on any of the passed keywords, making sure there are no other identifier
		 * tokens before or after it.  This allows us to be sure an iterator on "input" isn't actually on "_input" or similar.  This
		 * function assumes keywords are only one text token.
		 */
		public bool IsOnAnyKeyword (TokenIterator iterator, params string[] keywords)
			{
			return (iterator.MatchesAnyToken(keywords) != -1 &&
					   !IsPartOfLongerIdentifier(iterator));
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
			if (iterator.FundamentalType != FundamentalType.Text ||
				javascriptKeywords.Contains(iterator.String) == false ||
				IsPartOfLongerIdentifier(iterator))
				{  return false;  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

			iterator.Next();
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
			return TryToSkipNumber(ref iterator,
												ParseNumberFlags.AllowUnderscoreSeparators |
												ParseNumberFlags.RequireDigitAfterDot,
												mode);
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
			char delimiter = iterator.Character;

			if (delimiter == '/')
				{  return TryToSkipRegularExpression(ref iterator, mode);  }
			else if (delimiter != '\'' && delimiter != '"' && delimiter != '`')
				{  return false;  }

			TokenIterator startOfLastStringSegment = iterator;

			TokenIterator lookahead = iterator;
			lookahead.Next();

			while (lookahead.IsInBounds)
				{
				if (lookahead.Character == delimiter)
					{
					lookahead.Next();
					break;
					}

				// Interpolated strings
				else if (delimiter == '`' && lookahead.MatchesAcrossTokens("${"))
					{
					TokenIterator startOfInterpolatedCode = lookahead;

					if (mode == ParseMode.SyntaxHighlight)
						{  startOfLastStringSegment.SetSyntaxHighlightingTypeBetween(startOfInterpolatedCode, SyntaxHighlightingType.String);  }

					lookahead.Next(2);
					GenericSkipUntilAfter(ref lookahead, '}', false, skipToEndIfNotFound: false);

					if (mode == ParseMode.SyntaxHighlight)
						{  SyntaxHighlight(startOfInterpolatedCode, lookahead);  }

					startOfLastStringSegment = lookahead;
					}

				else if (lookahead.Character == '\\')
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


		/* Function: TryToSkipRegularExpression
		 *
		 * If the iterator is on the beginning of a regular expression, moves the iterator past it and returns true.  Unfortunately this
		 * function cannot be 100% reliable without fully parsing the JavaScript but it should be correct most of the time.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *			- Regular expressions will be highlighted as strings.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipRegularExpression (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '/')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			// Ignore slashes as part of // and /* comments

			if (lookahead.Character == '/' ||
				lookahead.Character == '*')
				{  return false;  }


			// Treat these forms as regular expressions and assume others are division:
			// - param1, /regex/
			// - function( /regex/
			// - var = /regex/
			// - return /regex/

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();
			lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator);

			if (lookbehind.Character != ',' &&
				lookbehind.Character != '(' &&
				lookbehind.Character != '=' &&
				!IsOnKeyword(lookbehind, "return"))
				{  return false;  }


			// Find the end

			for (;;)
				{
				if (!lookahead.IsInBounds)
					{  return false;  }
				else if (lookahead.Character == '\\')
					{  lookahead.Next(2);  }
				else if (lookahead.Character == '/')
					{  break;  }
				else if (lookahead.Character == '[')
					{
					// Unescaped slashes are allowed in character classes like [a-z/].  They can be escaped as well, like [a-z\/].
					lookahead.Next();

					for (;;)
						{
						if (!lookahead.IsInBounds)
							{  return false;  }
						else if (lookahead.Character == '\\')
							{  lookahead.Next(2);  }
						else if (lookahead.Character == ']')
							{  break;  }
						else
							{  lookahead.Next();  }
						}
					}
				else
					{  lookahead.Next();  }
				}


			// If we're here then lookahead is on the closing slash

			lookahead.Next();

			// Include extensions like /gi if present
			if (lookahead.FundamentalType == FundamentalType.Text)
				{  lookahead.Next();  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.String);  }

			iterator = lookahead;
			return true;
			}



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: javascriptKeywords
		 */
		static protected StringSet javascriptKeywords = new StringSet (KeySettings.Literal, new string[] {

			// Reserved words
			"await", "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete", "do", "else", "enum", "export",
			"extends", "false", "finally", "for", "function", "if", "import", "in", "instanceof", "new", "null", "return", "super", "switch", "this",
			"throw", "true", "try", "typeof", "var", "void", "while", "with", "yield",

			// Reserved words in strict mode
			"let", "static", "implements", "interface", "package", "private", "protected", "public",

			// Contextual reserved words
			"as", "async", "from", "get", "meta", "of", "set", "target",

			// Technically not reserved words, but highlight them anyway
			"undefined", "prototype", "constructor"

			});

		}
	}
