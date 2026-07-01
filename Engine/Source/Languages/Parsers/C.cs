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



		// Group: Parsing Functions
		// __________________________________________________________________________


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
		 * Tries to move the iterator past a single attribute like "[[deprecated]]".  It will also handle list attributes like "[[attrA, attrB]]".
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
			// According to the spec double opening brackets aren't allowed anywhere else, so we don't have to worry about this being part of
			// an array signature or something.
			if (!iterator.MatchesAcrossTokens("[["))
				{  return false;  }

			// If we're making a prototype section, the outermost [ and ] will be the start and end.  If it's a list of attributes we'll format them
			// as parameters, marking the inner [ and ] as the start and end of parameters symbols.  However, if there's a "using:" statement
			// we'll use its colon as the start of parameters instead.

			// Also, C++ doesn't let you reference parameters by name (like "Function(name: value)"), only by position, so we don't need to
			// reserve parameter formatting for that.

			TokenIterator lookahead = iterator;
			lookahead.Next();

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

			for (;;)
				{
				if (!lookahead.IsInBounds)
					{  return false;  }
				else if (lookahead.MatchesAcrossTokens("]]"))
					{  break;  }
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


			if (mode == ParseMode.SyntaxHighlight)
				{
				lookahead.Next(2);
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
						lookahead.PrototypeParsingType = PrototypeParsingType.EndOfMetadataParams;
						}

					lookahead.Next();
					lookahead.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;
					lookahead.Next();
					}
				else
					{
					lookahead.Next(2);
					iterator.SetPrototypeParsingTypeBetween(lookahead, prototypeParsingType);
					}
				}

			else if (mode == ParseMode.ParseClassPrototype)
				{
				iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;
				iterator.Next();

				lookahead.Next(2);
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

			for (;;)
				{
				if (TryToSkipUnqualifiedIdentifier(ref lookahead) == false)
					{  return false;  }

				endOfIdentifier = lookahead;
				TryToSkipWhitespace(ref lookahead);

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
