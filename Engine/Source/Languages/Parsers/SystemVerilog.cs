/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.SystemVerilog
 * ____________________________________________________________________________
 *
 * Full language support parser for SystemVerilog.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class SystemVerilog : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: SystemVerilog
		 */
		public SystemVerilog (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: GetCodeElements
		 */
		override public List<Element> GetCodeElements (Tokenizer source)
			{
			List<Element> elements = new List<Element>();

			return elements;
			}


		/* Function: SyntaxHighlight
		 */
		override public void SyntaxHighlight (Tokenizer source)
			{
			TokenIterator iterator = source.FirstToken;

			while (iterator.IsInBounds)
				{
				if (TryToSkipAttributes(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipComment(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipString(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipNumber(ref iterator, ParseMode.SyntaxHighlight))
					{
					}

				// Backslash identifiers.  Don't want them to mess up parsing other things since they can contain symbols.
				else if (iterator.Character == '\\' && TryToSkipIdentifier(ref iterator))
					{
					}

				// Text.  Check for keywords.
				else if (iterator.FundamentalType == FundamentalType.Text ||
						   iterator.Character == '_' || iterator.Character == '$')
					{
					TokenIterator endOfIdentifier = iterator;

					do
						{  endOfIdentifier.Next();  }
					while (endOfIdentifier.FundamentalType == FundamentalType.Text ||
							  endOfIdentifier.Character == '_' || endOfIdentifier.Character == '$');

					// All SV keywords start with a lowercase letter
					if (iterator.Character >= 'a' && iterator.Character <= 'z')
						{
						string identifier = source.TextBetween(iterator, endOfIdentifier);

						if (Keywords.Contains(identifier))
							{  iterator.SetSyntaxHighlightingTypeByCharacters(SyntaxHighlightingType.Keyword, identifier.Length);  }
						}

					iterator = endOfIdentifier;
					}

				else
					{
					iterator.Next();
					}
				}
			}



		// Group: Component Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToSkipAttributes
		 *
		 * If the iterator is on attributes, moves it past them and returns true.  This will skip multiple consecutive attributes
		 * blocks if they are only separated by whitespace.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttributes (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (!TryToSkipAttributesBlock(ref iterator, mode))
				{  return false;  }

			TokenIterator lookahead = iterator;

			for (;;)
				{
				TryToSkipWhitespace(ref lookahead, mode);

				if (TryToSkipAttributesBlock(ref lookahead, mode))
					{
					iterator = lookahead;
					}
				else
					{
					ResetTokensBetween(iterator, lookahead, mode);
					break;
					}
				}

			return true;
			}


		/* Function: TryToSkipAttributesBlock
		 *
		 * If the iterator is on an attributes block, moves it past it and returns true.  This will only skip a single (* *) block.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttributesBlock (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.MatchesAcrossTokens("(*") == false)
				{  return false;  }

			bool success = false;

			if (mode == ParseMode.ParsePrototype)
				{  iterator.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.StartOfParams, 2);  }

			TokenIterator lookahead = iterator;
			lookahead.NextByCharacters(2);

			while (lookahead.IsInBounds)
				{
				if (lookahead.MatchesAcrossTokens("*)"))
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.EndOfParams, 2);  }

					lookahead.NextByCharacters(2);

					success = true;
					break;
					}
				else if (lookahead.Character == '=')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.PropertyValueSeparator;  }

					lookahead.Next();
					}
				else if (lookahead.Character == ',')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;  }

					lookahead.Next();
					}
				else if (lookahead.Character == ')')
					{
					// If we found a ) before a *) then this isn't an attributes block.  It could be a pointer like (*pointer), so
					// break and fail.
					break;
					}
				else
					{
					// This will skip nested parentheses so we won't fail on the closing parenthesis of (* name = (value) *).
					GenericSkip(ref lookahead);
					}
				}

			if (!success)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.SyntaxHighlight)
				{
				iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Metadata);
				}
			else if (mode == ParseMode.ParsePrototype)
				{
				// We marked StartOfParams, EndOfParams, ParamSeparator, and PropertyValueSeparator.
				// Find and mark tokens that should be Name and PropertyValue now.
				TokenIterator end = lookahead;
				lookahead = iterator;

				bool inValue = false;

				while (lookahead < end)
					{
					if (lookahead.PrototypeParsingType == PrototypeParsingType.PropertyValueSeparator)
						{  inValue = true;  }
					else if (lookahead.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
						{  inValue = false;  }
					else if (lookahead.PrototypeParsingType == PrototypeParsingType.Null &&
							   lookahead.FundamentalType != FundamentalType.Whitespace)
						{
						if (inValue)
							{  lookahead.PrototypeParsingType = PrototypeParsingType.PropertyValue;  }
						else
							{  lookahead.PrototypeParsingType = PrototypeParsingType.Name;  }
						}

					lookahead.Next();
					}
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipIdentifier
		 *
		 * Tries to move past and retrieve an identifier.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipIdentifier (ref TokenIterator iterator, out string identifier, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator start = iterator;

			if (TryToSkipIdentifier(ref iterator, mode))
				{
				identifier = start.TextBetween(iterator);
				return true;
				}
			else
				{
				identifier = null;
				return false;
				}
			}


		/* Function: TryToSkipIdentifier
		 *
		 * Tries to move the iterator past an identifier.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			// Simple identifiers start with letters or underscores.  They can also contain numbers and $ but cannot start with
			// them.
			if (iterator.FundamentalType == FundamentalType.Text ||
				iterator.Character == '_')
				{
				if (iterator.Character >= '0' && iterator.Character <= '9')
					{  return false;  }

				do
					{  iterator.Next();  }
				while (iterator.FundamentalType == FundamentalType.Text ||
						  iterator.Character == '_' ||
						  iterator.Character == '$');

				return true;
				}

			// Escaped identifiers start with \ and can contain any characters, including symbols.  They continue until the next
			// whitespace or line break.
			else if (iterator.Character == '\\')
				{
				do
					{  iterator.Next();  }
				while (iterator.FundamentalType == FundamentalType.Text ||
						  iterator.FundamentalType == FundamentalType.Symbol);

				return true;
				}

			else
				{  return false;  }
			}



		// Group: Base Parsing Functions
		// __________________________________________________________________________


		/* Function: GenericSkip
		 *
		 * Advances the iterator one place through general code.
		 *
		 * - If the position is on a string, it will skip it completely.
		 * - If the position is on an opening brace, parenthesis, or bracket it will skip until the past the closing symbol.
		 * - Otherwise it skips one token.
		 */
		protected void GenericSkip (ref TokenIterator iterator)
			{
			if (iterator.MatchesAcrossTokens("(*"))
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, "*)");
				}
			else if (iterator.Character == '(')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, ')');
				}
			else if (iterator.Character == '[')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, ']');
				}
			else if (iterator.Character == '{')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, '}');
				}
			else if (TryToSkipString(ref iterator) ||
					  TryToSkipWhitespace(ref iterator))
				{  }
			else
				{  iterator.Next();  }
			}


		/* Function: GenericSkipUntilAfter
		 * Advances the iterator via <GenericSkip()> until a specific symbol is reached and passed.
		 */
		protected void GenericSkipUntilAfter (ref TokenIterator iterator, char symbol)
			{
			while (iterator.IsInBounds)
				{
				if (iterator.Character == symbol)
					{
					iterator.Next();
					break;
					}
				else
					{  GenericSkip(ref iterator);  }
				}
			}


		/* Function: GenericSkipUntilAfter
		 * Advances the iterator via <GenericSkip()> until a specific symbol is reached and passed.
		 */
		protected void GenericSkipUntilAfter (ref TokenIterator iterator, string symbol)
			{
			while (iterator.IsInBounds)
				{
				if (iterator.MatchesAcrossTokens(symbol))
					{
					iterator.NextByCharacters(symbol.Length);
					break;
					}
				else
					{  GenericSkip(ref iterator);  }
				}
			}


		/* Function: TryToSkipWhitespace
		 *
		 * For skipping whitespace and comments.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipWhitespace (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			int originalTokenIndex = iterator.TokenIndex;

			for (;;)
				{
				if (iterator.FundamentalType == FundamentalType.Whitespace ||
					iterator.FundamentalType == FundamentalType.LineBreak)
					{  iterator.Next();  }

				else if (TryToSkipComment(ref iterator, mode))
					{  }

				else
					{  break;  }
				}

			return (iterator.TokenIndex != originalTokenIndex);
			}


		/* Function: TryToSkipComment
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		new protected bool TryToSkipComment (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			return (TryToSkipLineComment(ref iterator, mode) ||
					  TryToSkipBlockComment(ref iterator, mode));
			}


		/* Function: TryToSkipLineComment
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		new protected bool TryToSkipLineComment (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.MatchesAcrossTokens("//"))
				{
				TokenIterator startOfComment = iterator;
				iterator.NextByCharacters(2);

				while (iterator.IsInBounds &&
						 iterator.FundamentalType != FundamentalType.LineBreak)
					{  iterator.Next();  }

				if (mode == ParseMode.SyntaxHighlight)
					{  startOfComment.SetSyntaxHighlightingTypeBetween(iterator, SyntaxHighlightingType.Comment);  }

				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToSkipBlockComment
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		new protected bool TryToSkipBlockComment (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.MatchesAcrossTokens("/*"))
				{
				TokenIterator startOfComment = iterator;
				iterator.NextByCharacters(2);

				while (iterator.IsInBounds &&
						 iterator.MatchesAcrossTokens("*/") == false)
					{  iterator.Next();  }

				if (iterator.MatchesAcrossTokens("*/"))
					{  iterator.NextByCharacters(2);  }

				if (mode == ParseMode.SyntaxHighlight)
					{  startOfComment.SetSyntaxHighlightingTypeBetween(iterator, SyntaxHighlightingType.Comment);  }

				return true;
				}
			else
				{  return false;  }
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
			if (iterator.Character != '\"')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			while (lookahead.IsInBounds)
				{
				if (lookahead.Character == '\\')
					{
					// This also covers line breaks
					lookahead.Next(2);
					}

				else if (lookahead.Character == '\"')
					{
					lookahead.Next();
					break;
					}

				else
					{  lookahead.Next();  }
				}


			if (lookahead.IsInBounds)
				{
				if (mode == ParseMode.SyntaxHighlight)
					{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.String);  }

				iterator = lookahead;
				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToSkipNumber
		 *
		 * If the iterator is on a numeric literal, moves the iterator past it and returns true.  This covers:
		 *
		 *		- Simple numbers like 12 and 1.23.
		 *		- Scientific notation like 1.2e3.
		 *		- Numbers in different bases like 'H01AB and 'b1010.
		 *		- Sized numbers like 4'b0011.
		 *		- Digit separators like 1_000 and 'b0000_0011.
		 *		- X, Z, and ? digits like 'H01XX.
		 *		- Constants like '0, '1, 'X, and 'Z.
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
				   iterator.Character == '-' || iterator.Character == '+' || iterator.Character == '\'') == false)
				{  return false;  }

			TokenIterator lookahead = iterator;

			bool validNumber = false;
			TokenIterator endOfNumber = iterator;


			// Leading +/-, optional

			if (lookahead.Character == '-' || lookahead.Character == '+')
				{
				// Distinguish between -1 and x-1

				TokenIterator lookbehind = iterator;
				lookbehind.Previous();

				lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator);

				if (lookbehind.FundamentalType == FundamentalType.Text ||
					lookbehind.Character == '_' || lookbehind.Character == '$')
					{  return false;  }

				lookahead.Next();

				// This isn't enough to set validNumber to true but we have a new endOfNumber.
				endOfNumber = lookahead;
				}


			// Next 0-9.  This could be:
			//    - A full decimal value: [12]
			//    - Part of a floating point value before the decimal: [12].34
			//    - A full floating point value in scientific notation without a decimal: [12e3]
			//    - Part of a floating point value in scientific notation without a decimal but with an exponent sign: [12e]-3
			//    - The size of the type preceding the base signifier: [4]'b1001
			// Note that they can all contain digit separators (_) but cannot start with them.

			if (lookahead.Character >= '0' && lookahead.Character <= '9')
				{
				do
					{  lookahead.Next();  }
				while (lookahead.FundamentalType == FundamentalType.Text ||
						  lookahead.Character == '_');

				validNumber = true;
				endOfNumber = lookahead;
				}


			//	Check for a dot, which would continue a floating point number

			if (validNumber && lookahead.Character == '.')
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

			if (validNumber &&
				(lookahead.Character == '+' || lookahead.Character == '-'))
				{
				// Make sure the character before this point was an E before we accept it.
				// We're safe to read RawTextIndex - 1 because we know we're not on its first character because validNumber
				// wouldn't be set otherwise.
				char previousCharacter = lookahead.Tokenizer.RawText[ lookahead.RawTextIndex - 1 ];

				if (previousCharacter == 'e' || previousCharacter == 'E')
					{
					lookahead.Next();

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

				// If it wasn't an E before the +/-, end the number at the last valid point
				else
					{  goto Done;  }
				}


			// There can be whitespace between a type size and the base signifier.

			if (validNumber)
				{  lookahead.NextPastWhitespace();  }


			// Check for an apostrophe, which could be:
			//   - A bit constant like '0 or 'Z
			//   - A base signifier like 'b
			// It can start a number so we don't need to check if it's already valid.

			if (lookahead.Character == '\'')
				{
				lookahead.Next();


				// Constants '0, '1, 'z, or 'x.  There is no '? constant.

				if (lookahead.RawTextLength == 1 &&
					(lookahead.Character == '0' || lookahead.Character == '1' ||
					 lookahead.Character == 'z' || lookahead.Character == 'Z' ||
					 lookahead.Character == 'x' || lookahead.Character == 'X'))
					{
					lookahead.Next();

					validNumber = true;
					endOfNumber = lookahead;

					goto Done;
					}


				// Base type signifiers: 'd, 'h, 'b, 'o, optionally preceded with s such as 'sd.

				char baseChar = lookahead.Character;
				bool baseIsSigned = false;

				if ((baseChar == 's' || baseChar == 'S') &&
					lookahead.RawTextLength > 1)
					{
					baseChar = lookahead.Tokenizer.RawText[ lookahead.RawTextIndex + 1 ];
					baseIsSigned = true;
					}

				if (baseChar == 'd' || baseChar == 'D' ||
					baseChar == 'h' || baseChar == 'H' ||
					baseChar == 'b' || baseChar == 'B' ||
					baseChar == 'o' || baseChar == 'O')
					{
					// There can be space between the base signifier and the value after it
					if ( (lookahead.RawTextLength == 1 && !baseIsSigned) ||
						 (lookahead.RawTextLength == 2 && baseIsSigned) )
						{
						lookahead.Next();
						lookahead.NextPastWhitespace();

						// Check that what's next is valid
						if ( (lookahead.Character >= '0' && lookahead.Character <= '9') ||
							  (lookahead.Character >= 'a' && lookahead.Character <= 'f') ||
							  (lookahead.Character >= 'A' && lookahead.Character <= 'F') ||
							  lookahead.Character == 'x' || lookahead.Character == 'X' ||
							  lookahead.Character == 'z' || lookahead.Character == 'Z' ||
							  lookahead.Character == '?' )
							{
							lookahead.Next();

							validNumber = true;
							endOfNumber = lookahead;
							}
						else
							{  goto Done;  }
						}

					// If the token was longer than the base signifer, assume it runs together with the value and is valid
					else
						{
						lookahead.Next();

						validNumber = true;
						endOfNumber = lookahead;
						}

					// Get the rest of the value's tokens.  If it wasn't valid we would have gone to Done already.
					while ( (lookahead.Character >= '0' && lookahead.Character <= '9') ||
							   (lookahead.Character >= 'a' && lookahead.Character <= 'f') ||
							   (lookahead.Character >= 'A' && lookahead.Character <= 'F') ||
								lookahead.Character == 'x' || lookahead.Character == 'X' ||
								lookahead.Character == 'z' || lookahead.Character == 'Z' ||
								lookahead.Character == '?' || lookahead.Character == '_' )
						{
						lookahead.Next();
						endOfNumber = lookahead;
						}
					}
				}


			// Time units.  There can whitespace between it and the value.

			if (validNumber)
				{
				lookahead.NextPastWhitespace();

				// Only lowercase is valid.
				if (lookahead.MatchesToken("s") ||
					lookahead.MatchesToken("ms") ||
					lookahead.MatchesToken("us") ||
					lookahead.MatchesToken("ns") ||
					lookahead.MatchesToken("ps") ||
					lookahead.MatchesToken("fs"))
					{
					lookahead.Next();
					endOfNumber = lookahead;
					}
				}


			Done:  // An evil goto target!  The shame!

			if (validNumber)
				{
				if (mode == ParseMode.SyntaxHighlight)
					{  iterator.SetSyntaxHighlightingTypeBetween(endOfNumber, SyntaxHighlightingType.Number);  }

				iterator = endOfNumber;
				return true;
				}
			else
				{  return false;  }
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: Keywords
		 */
		static protected StringSet Keywords = new StringSet (KeySettings.Literal, new string[] {

			// Listed in the SystemVerilog 2017 reference's keywords section

			"accept_on", "alias", "always", "always_comb", "always_ff", "always_latch", "and", "assert", "assign", "assume",
			"automatic", "before", "begin", "bind", "bins", "binsof", "bit", "break", "buf", "bufif0", "bufif1", "byte", "case", "casex",
			"casez", "cell", "chandle", "checker", "class", "clocking", "cmos", "config", "const", "constraint", "context", "continue",
			"cover", "covergroup", "coverpoint", "cross", "deassign", "default", "defparam", "design", "disable", "dist", "do", "edge",
			"else", "end", "endcase", "endchecker", "endclass", "endclocking", "endconfig", "endfunction", "endgenerate", "endgroup",
			"endinterface", "endmodule", "endpackage", "endprimitive", "endprogram", "endproperty", "endspecify", "endsequence",
			"endtable", "endtask", "enum", "event", "eventually", "expect", "export", "extends", "extern", "final", "first_match", "for",
			"force", "foreach", "forever", "fork", "forkjoin", "function", "generate", "genvar", "global", "highz0", "highz1", "if", "iff",
			"ifnone", "ignore_bins", "illegal_bins", "implements", "implies", "import", "incdir", "include", "initial", "inout", "input",
			"inside", "instance", "int", "integer", "interconnect", "interface", "intersect", "join", "join_any", "join_none", "large", "let",
			"liblist", "library", "local", "localparam", "logic", "longint", "macromodule", "matches", "medium", "modport", "module",
			"nand", "negedge", "nettype", "new", "nexttime", "nmos", "nor", "noshowcancelled", "not", "notif0", "notif1", "null", "or",
			"output", "package", "packed", "parameter", "pmos", "posedge", "primitive", "priority", "program", "property", "protected",
			"pull0", "pull1", "pulldown", "pullup", "pulsestyle_ondetect", "pulsestyle_onevent", "pure", "rand", "randc", "randcase",
			"randsequence", "rcmos", "real", "realtime", "ref", "reg", "reject_on", "release", "repeat", "restrict", "return", "rnmos",
			"rpmos", "rtran", "rtranif0", "rtranif1", "s_always", "s_eventually", "s_nexttime", "s_until", "s_until_with", "scalared",
			"sequence", "shortint", "shortreal", "showcancelled", "signed", "small", "soft", "solve", "specify", "specparam", "static",
			"string", "strong", "strong0", "strong1", "struct", "super", "supply0", "supply1", "sync_accept_on", "sync_reject_on",
			"table", "tagged", "task", "this", "throughout", "time", "timeprecision", "timeunit", "tran", "tranif0", "tranif1", "tri", "tri0",
			"tri1", "triand", "trior", "trireg", "type", "typedef", "union", "unique", "unique0", "unsigned", "until", "until_with", "untyped",
			"use", "uwire", "var", "vectored", "virtual", "void", "wait", "wait_order", "wand", "weak", "weak0", "weak1", "while",
			"wildcard", "wire", "with", "within", "wor", "xnor", "xor"
			});

		}
	}
