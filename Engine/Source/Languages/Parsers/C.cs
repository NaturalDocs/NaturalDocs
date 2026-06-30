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
 * Resources:
 *		- <C Language Reference: https://cppreference.com/c/language>
 *		- <C++ Language Reference: https://cppreference.com/cpp/language>
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class C : Parser
		{

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



		// Group: Parsing Functions
		// __________________________________________________________________________


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
