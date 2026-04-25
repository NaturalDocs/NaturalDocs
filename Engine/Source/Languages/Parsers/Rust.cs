/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Rust
 * ____________________________________________________________________________
 *
 * Additional language support for Rust.
 *
 * Resources:
 *		- <Language Reference: https://doc.rust-lang.org/reference/>
 *		- <Tutorial: https://doc.rust-lang.org/book/>
 *		- <Playground: https://play.rust-lang.org/>
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
	public class Rust : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Rust
		 */
		public Rust (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
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
			if (iterator.Character != '#')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.Character == '!')
				{  lookahead.Next();  }

			if (lookahead.Character != '[')
				{  return false;  }

			if (!TryToSkipBlock(ref lookahead, false))
				{  return false;  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Metadata);  }

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
			if (iterator.FundamentalType != FundamentalType.Text &&
				iterator.Character != '_')
				{  return false;  }

			TokenIterator end = iterator;
			end.Next();

			while (end.FundamentalType == FundamentalType.Text ||
					 end.Character == '_')
				{  end.Next();  }

			if (rustKeywords.Contains(iterator.TextBetween(end)) == false)
				{  return false;  }


			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			// Check for raw identifiers, which is r# preceding an otherwise reserved word.
			if (lookbehind.Character == '#')
				{
				lookbehind.Previous();

				if (lookbehind.Character == 'r' &&
					lookbehind.TokenLength == 1)
					{  return false;  }
				}

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

			if (character != '\'' && character != '\"' &&
				character != 'c' && character != 'b' && character != 'r')
				{  return false;  }

			TokenIterator lookahead = iterator;

			bool isRawString = false;
			int hashCount = 0;


			// Check for the b, c, r, br, or cr prefix

			if (lookahead.FundamentalType == FundamentalType.Text)
				{
				if (lookahead.TokenLength == 1)
					{
					character = lookahead.Character;

					if (character == 'r')
						{  isRawString = true;  }
					else if (character != 'b' && character != 'c')
						{  return false;  }
					}
				else if (lookahead.TokenLength == 2)
					{
					if (lookahead.MatchesToken("br") || lookahead.MatchesToken("cr"))
						{  isRawString = true;  }
					else
						{  return false;  }
					}
				else
					{  return false;  }

				lookahead.Next();
				}


			// If it's a raw string, count hashes.  Zero is fine.

			if (isRawString && lookahead.Character == '#')
				{
				hashCount = ConsecutiveCharacterCount(lookahead);
				lookahead.Next(hashCount);
				}


			// String contents

			if (lookahead.Character == '"')
				{
				lookahead.Next();

				while (lookahead.IsInBounds)
					{
					if (lookahead.Character == '"')
						{
						lookahead.Next();

						if (isRawString && hashCount > 0)
							{
							if (lookahead.Character == '#' && ConsecutiveCharacterCount(lookahead) >= hashCount)
								{
								lookahead.Next(hashCount);
								break;
								}
							}
						else
							{  break;  }
						}
					else if (lookahead.Character == '\\' && !isRawString)
						{  lookahead.Next(2);  }
					else
						{  lookahead.Next();  }
					}
				}


			// Char contents

			else if (lookahead.Character == '\'')
				{
				lookahead.Next();

				TokenIterator startOfContent = lookahead;
				bool isClosed = false;

				while (lookahead.IsInBounds)
					{
					if (lookahead.Character == '\'')
						{
						lookahead.Next();
						isClosed = true;
						break;
						}
					else if (lookahead.Character == '\\')
						{  lookahead.Next(2);  }
					else
						{  lookahead.Next();  }
					}

				// We need to check the entire thing to make sure it isn't part of a lifetime signature like in
				// "fn FunctionName<'a> (varName: &'a i32)".

				// If we didn't find a closing quote we assume it was a lifetime
				if (!isClosed)
					{  return false;  }

				// A lifetime has to start with an identifier character.  Any other symbols we'll treat as a char.
				if (startOfContent.FundamentalType == FundamentalType.Text ||
					startOfContent.Character == '_')
					{
					startOfContent.Next();

					// If the second token isn't the end quote assume it's a lifetime.  This covers:
					// - 'a' and '_' which get treated as chars
					// - 'r#a and 'r#_ which have a symbol as the second character
					// - '\x32' was previously ruled out by the first character
					// - 'a has a space as the second character
					// - 'a, has a comma as the second character
					if (startOfContent.Character != '\'')
						{  return false;  }
					}
				}


			else // not " or '
				{  return false;  }


			// Done

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.String);  }

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

			if (lookahead.Character >= '0' && lookahead.Character <= '9')
				{
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

				// The part after a decimal can contain digit separators (_) but cannot start with them
				if (lookahead.Character >= '0' && lookahead.Character <= '9')
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
				// Make sure the character before this point was an E before we accept it.

				// We're safe to read RawTextIndex - 1 because we know we're not on its first character because we wouldn't
				// have made it this far otherwise.
				string rawText = lookahead.Tokenizer.RawText;
				char previousCharacter = rawText[ lookahead.RawTextIndex - 1 ];

				if (previousCharacter == 'e')
					{
					// Also make sure it's not just the 'e' from "isize" or "usize".  We can just check for the 'z' since that can't
					// be a digit.
					if (lookahead.RawTextIndex < 2 ||
						rawText[lookahead.RawTextIndex - 2] == 'z')
						{  goto Done;  }
					}

				// If it wasn't an 'e' or an 'E' before the +/-, end the number at the last valid point
				else if (previousCharacter != 'E')
					{  goto Done;  }

				lookahead.Next();

				// Rust allows underscores to appear between the +/- and the first exponent digit.  We still won't accept it until
				// we get to a digit.
				while (lookahead.Character == '_')
					{  lookahead.Next();  }

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

		/* var: rustKeywords
		 */
		static protected StringSet rustKeywords = new StringSet (KeySettings.Literal, new string[] {

			// Language keywords as of April 2026.  Includes strict, reserved, and weak keywords

			"_", "abstract", "as", "async", "await", "become", "box", "break", "const", "continue", "crate", "do", "dyn",
			"else", "enum", "extern", "false", "final", "fn", "for", "gen", "if", "impl", "in", "let", "loop", "macro_rules",
			"macro", "match", "mod", "move", "mut", "override", "priv", "pub", "raw", "ref", "return", "safe", "self",
			"Self", "static", "struct", "super", "trait", "true", "try", "type", "typeof", "union", "unsafe", "unsized", "use",
			"virtual", "where", "while", "yield",

			// Primitive types

			"bool", "u8", "i8", "u16", "i16", "u32", "i32", "u64", "i64", "u128", "i128", "f32", "f64", "usize", "isize",
			"char", "str"

			});

		}
	}
