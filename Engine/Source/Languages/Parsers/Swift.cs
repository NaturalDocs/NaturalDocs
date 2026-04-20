/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Swift
 * ____________________________________________________________________________
 *
 * Additional language support for Swift.
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
		override public void SyntaxHighlight (Tokenizer source)
			{
			SyntaxHighlight(source.FirstToken, source.EndOfTokens);
			}


		/* Function: SyntaxHighlight
		 */
		public void SyntaxHighlight (TokenIterator start, TokenIterator end)
			{
			while (start < end)
				{
				if (TryToSkipAttribute(ref start, ParseMode.SyntaxHighlight) ||
					TryToSkipKeyword(ref start, ParseMode.SyntaxHighlight) ||
				    TryToSkipComment(ref start, ParseMode.SyntaxHighlight) ||
				    TryToSkipString(ref start, ParseMode.SyntaxHighlight) ||
				    TryToSkipNumber(ref start, ParseMode.SyntaxHighlight))
					{
					}
				else
					{  start.Next();  }
				}
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


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

			int hashCount = 0;

			while (lookahead.Character == '#')
				{
				hashCount++;
				lookahead.Next();
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

					int endHashCount = 0;

					while (lookahead.Character == '#' && endHashCount < hashCount)
						{
						endHashCount++;
						lookahead.Next();
						}

					if (endHashCount == hashCount)
						{  break;  }
					}

				// Interpolated strings
				else if (hashCount == 0 && lookahead.MatchesAcrossTokens("\\("))
					{
					if (mode == ParseMode.SyntaxHighlight)
						{  startOfLastStringSegment.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.String);  }

					TokenIterator startOfInterpolatedCode = lookahead;

					lookahead.Next();
					TryToSkipBlock(ref lookahead, false);

					if (mode == ParseMode.SyntaxHighlight)
						{  SyntaxHighlight(startOfInterpolatedCode, lookahead);  }

					startOfLastStringSegment = lookahead;
					}

				else if (hashCount == 0 && lookahead.Character == '\\')
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
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipNumber (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if ( (iterator.Character < '0' || iterator.Character > '9') &&
				  iterator.Character != '-' && iterator.Character != '+')
				{  return false;  }

			TokenIterator lookahead = iterator;
			TokenIterator endOfNumber = iterator;


			// Leading +/-, optional

			if (lookahead.Character == '-' || lookahead.Character == '+')
				{
				// Distinguish between -1 and x-1

				TokenIterator lookbehind = iterator;
				lookbehind.Previous();

				if (lookbehind.FundamentalType == FundamentalType.Text ||
					lookbehind.Character == '_')
					{  return false;  }

				lookahead.Next();
				}


			// Next 0-9.  This could be:
			//    - A full decimal value: [12]
			//    - Part of a floating point value before the decimal: [12].34
			//    - A full floating point value in scientific notation without a decimal: [12e3]
			//    - Part of a floating point value in scientific notation without a decimal but with an exponent sign: [12e]-3
			// Note that they can all contain digit separators (_) but cannot start with them.

			bool isHex = false;

			if (lookahead.Character >= '0' && lookahead.Character <= '9')
				{
				isHex = lookahead.MatchesAcrossTokens("0x", ignoreCase: true, matchPartialTokens: true);

				do
					{  lookahead.Next();  }
				while (lookahead.FundamentalType == FundamentalType.Text ||
						 lookahead.Character == '_');

				endOfNumber = lookahead;
				}
			else
				{  return false;  }


			//	Check for a dot, which would continue a floating point number.  There needs to be a preceding digit, it cannot
			// start with a dot like ".2".

			if (lookahead.Character == '.')
				{
				lookahead.Next();
				char character = lookahead.Character;

				// The part after a decimal can contain digit separators (_) but cannot start with them
				if (character >= '0' && character <= '9' ||
					(isHex && ((character >= 'a' && character <= 'f') || (character >= 'A' && character <= 'F'))) )
					{
					do
						{  lookahead.Next();  }
					while (lookahead.FundamentalType == FundamentalType.Text ||
							  lookahead.Character == '_');

					// The number is already marked as valid, so extend it to include this part
					endOfNumber = lookahead;
					}

				// If it wasn't a digit after the dot, the number is done and the dot isn't part of it
				else
					{  goto Done;  }
				}


			// Check for a +/-, which could continue a floating point number with an exponent

			if (lookahead.Character == '+' || lookahead.Character == '-')
				{
				// Make sure the character before this point was an E or P before we accept it.

				// We're safe to read RawTextIndex - 1 because we know we're not on its first character because we wouldn't
				// have made it this far otherwise.
				string rawText = lookahead.Tokenizer.RawText;
				char previousCharacter = rawText[ lookahead.RawTextIndex - 1 ];

				if ( (!isHex && previousCharacter != 'e' && previousCharacter != 'E') ||
					 (isHex && previousCharacter != 'p' && previousCharacter != 'P') )
					{  goto Done;  }

				lookahead.Next();

				// Rust allows underscores to appear between the +/- and the first exponent digit.  We still won't accept it until
				// we get to a digit.
				while (lookahead.Character == '_')
					{  lookahead.Next();  }

				// Only decimal digits are allowed in the exponent, even for hex floats
				if (lookahead.Character >= '0' && lookahead.Character <= '9')
					{
					do
						{  lookahead.Next();  }
					while (lookahead.FundamentalType == FundamentalType.Text ||
							 lookahead.Character == '_');

					// The number is already marked as valid, so extend it to include this part
					endOfNumber = lookahead;
					}

				// if there weren't digits after the E, end the number at the last valid point
				else
					{  goto Done;  }
				}


			Done:  // An evil goto target!  The shame!

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(endOfNumber, SyntaxHighlightingType.Number);  }

			iterator = endOfNumber;
			return true;
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
