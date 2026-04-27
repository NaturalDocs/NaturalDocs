/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Swift
 * ____________________________________________________________________________
 *
 * Additional language support for Swift.
 *
 * Language Version:
 *
 *		The parser is based on Swift 6.3, the latest release as of April 2026.
 *
 * Resources:
 *
 *		- <Language Reference: https://docs.swift.org/swift-book/documentation/the-swift-programming-language>
 *
 *		- <Standard Library: https://developer.apple.com/documentation/swift/swift-standard-library>
 *			- Primitive types like Int are actually defined in the standard library rather than the language.
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
	public class Swift : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Swift
		 */
		public Swift (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: SyntaxHighlight
		 */
		override public void SyntaxHighlight (TokenIterator start, TokenIterator end)
			{
			TokenIterator iterator = start;

			while (iterator < end)
				{
				if (TryToSkipAttribute(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipKeyword(ref iterator, ParseMode.SyntaxHighlight) ||
				    TryToSkipComment(ref iterator, ParseMode.SyntaxHighlight) ||
				    TryToSkipString(ref iterator, ParseMode.SyntaxHighlight) ||
				    TryToSkipNumber(ref iterator, ParseMode.SyntaxHighlight))
					{
					}
				else
					{  iterator.Next();  }
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
		 *			- Each annotation will create a new prototype section.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipMetadata (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			return TryToSkipAttribute(ref iterator, mode);
			}


		/* Function: TryToSkipAttribute
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttribute (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '@')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.FundamentalType != FundamentalType.Text)
				{  return false;  }

			do
				{  lookahead.Next();  }
			while (lookahead.FundamentalType == FundamentalType.Text ||
					  lookahead.Character == '_');

			TokenIterator end = lookahead;

			lookahead.NextPastWhitespace();

			if (lookahead.Character == '(')
				{
				if (TryToSkipBlock(ref lookahead, false))
					{
					end = lookahead;
					}
				}

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(end, SyntaxHighlightingType.Metadata);  }

			iterator = end;
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
			if (iterator.FundamentalType != FundamentalType.Text &&
				iterator.Character != '_')
				{  return false;  }

			TokenIterator end = iterator;
			end.Next();

			while (end.FundamentalType == FundamentalType.Text ||
					 end.Character == '_')
				{  end.Next();  }

			if (swiftKeywords.Contains(iterator.TextBetween(end)) == false)
				{  return false;  }

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			// Check for backticks surrounding it, which allows reserved words to be used as identifiers
			if (end.Character == '`' && lookbehind.Character == '`')
				{  return false;  }

			// Also check if we're in the middle of an existing identifier
			else if (lookbehind.FundamentalType == FundamentalType.Text ||
					  lookbehind.Character == '_')
				{  return false;  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(end, SyntaxHighlightingType.Keyword);  }

			iterator = end;
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

			if (character != '\"' && character != '#')
				{  return false;  }

			TokenIterator lookahead = iterator;
			TokenIterator startOfLastStringSegment = iterator;


			// Count hash symbols for #" strings.  Zero is fine.

			int delimiterHashCount = 0;

			if (lookahead.Character == '#')
				{
				delimiterHashCount = ConsecutiveCharacterCount(lookahead);
				lookahead.Next(delimiterHashCount);
				}


			// Check for multiline strings.

			bool isMultiline = false;

			if (lookahead.MatchesAcrossTokens("\"\"\""))
				{
				isMultiline = true;
				lookahead.Next(3);
				}
			else if (lookahead.Character == '\"')
				{  lookahead.Next();  }
			else
				{  return false;  }


			// String contents

			while (lookahead.IsInBounds)
				{
				if ( (!isMultiline && lookahead.Character == '"') ||
					 (isMultiline && lookahead.MatchesAcrossTokens("\"\"\"")) )
					{
					if (!isMultiline)
						{  lookahead.Next();  }
					else
						{  lookahead.Next(3);  }

					if (delimiterHashCount == 0 ||
						(lookahead.Character == '#' && ConsecutiveCharacterCount(lookahead) >= delimiterHashCount))
						{
						lookahead.Next(delimiterHashCount);
						break;
						}
					}

				else if (lookahead.Character == '\\')
					{
					TokenIterator startOfInterpolatedCode = lookahead;

					lookahead.Next();
					int contentHashCount = (lookahead.Character == '#' ? ConsecutiveCharacterCount(lookahead) : 0);

					// Interpolated strings
					if (contentHashCount == delimiterHashCount)
						{
						if (contentHashCount > 0)
							{  lookahead.Next(contentHashCount);  }

						if (lookahead.Character == '(')
							{
							if (mode == ParseMode.SyntaxHighlight)
								{  startOfLastStringSegment.SetSyntaxHighlightingTypeBetween(startOfInterpolatedCode, SyntaxHighlightingType.String);  }

							TryToSkipBlock(ref lookahead, false);

							if (mode == ParseMode.SyntaxHighlight)
								{  SyntaxHighlight(startOfInterpolatedCode, lookahead);  }

							startOfLastStringSegment = lookahead;
							}
						else
							{  lookahead.Next();  }
						}
					else
						{  lookahead.Next();  }
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
												ParseNumberFlags.AllowHexFloats |
												ParseNumberFlags.RequireDigitAfterDot,
												mode);
			}



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: swiftKeywords
		 */
		static protected StringSet swiftKeywords = new StringSet (KeySettings.Literal, new string[] {

			// Language keywords as of April 2026

			// Keywords used in declarations
			"associatedtype", "borrowing", "class", "consuming", "deinit", "enum", "extension", "fileprivate", "func", "import", "init",
			"inout", "internal", "let", "nonisolated", "open", "operator", "precedencegroup", "private", "protocol", "public", "rethrows",
			"static", "struct", "subscript", "typealias", "var",

			// Keywords used in statements
			"break", "case", "catch", "continue", "default", "defer", "do", "else", "fallthrough", "for", "guard", "if", "in", "repeat", "return",
			"switch", "throw", "where", "while",

			// Keywords used in expressions and types
			"Any", "as", "await", "catch", "false", "is", "nil", "rethrows", "self", "Self", "super", "throw", "throws", "true", "try",

			// Keywords used in patterns
			"_",

			// Keywords reserved in particular contexts
			"associativity", "async", "convenience", "didSet", "dynamic", "final", "get", "indirect", "infix", "lazy", "left", "mutating", "none",
			"nonmutating", "optional", "override", "package", "postfix", "precedence", "prefix", "Protocol", "required", "right", "set", "some",
			"Type", "unowned", "weak", "willSet",

			// Common types from the standard library
			"Bool", "Int", "Double", "Float", "String", "Character",
			"BinaryInteger", "FixedWidthInteger", "SignedInteger", "UnsignedInteger", "FloatingPoint", "BinaryFloatingPoint",
			"UInt", "UInt8", "UInt16", "UInt32", "UInt64", "UInt128", "Int8", "Int16", "Int32", "Int64", "Int128",
			"Float16", "Float32", "Float64", "Float80"

			});

		}
	}
