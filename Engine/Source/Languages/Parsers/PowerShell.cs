/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.PowerShell
 * ____________________________________________________________________________
 *
 * Additional language support for PowerShell.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class PowerShell : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: PowerShell
		 */
		public PowerShell (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: SyntaxHighlight
		 */
		override public void SyntaxHighlight (Tokenizer source)
			{
			TokenIterator iterator = source.FirstToken;

			while (iterator.IsInBounds)
				{
				// Backticks cancel out everything, including # starting comments and " starting strings
				if (iterator.Character == '`')
					{  iterator.Next(2);  }

				else if (TryToSkipKeyword(ref iterator, ParseMode.SyntaxHighlight) ||
						   TryToSkipLineComment(ref iterator, ParseMode.SyntaxHighlight) ||
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
				iterator.Character != '-' &&
				iterator.Character != '$')
				{  return false;  }

			TokenIterator lookahead = iterator;

			if (lookahead.Character == '-' || lookahead.Character == '$')
				{  lookahead.Next();  }

			if (lookahead.FundamentalType != FundamentalType.Text)
				{  return false;  }

			lookahead.Next();

			if (lookahead.FundamentalType == FundamentalType.Text ||
				lookahead.Character == '_')
				{  return false;  }

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			if (lookbehind.FundamentalType == FundamentalType.Text ||
				lookbehind.Character == '-' ||
				lookbehind.Character == '$' ||
				lookbehind.Character == '_')
				{  return false;  }

			string keyword = iterator.TextBetween(lookahead);

			if (!powershellKeywords.Contains(keyword))
				{  return false;  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Keyword);  }

			iterator = lookahead;
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
			if (iterator.Character != '\'' && iterator.Character != '\"')
				{  return false;  }

			char openingCharacter = iterator.Character;

			TokenIterator lookahead = iterator;
			lookahead.Next();

			while (lookahead.IsInBounds)
				{
				if (lookahead.Character == '`')
					{  lookahead.Next(2);  }

				else if (lookahead.Character == openingCharacter)
					{
					lookahead.Next();
					break;
					}

				else
					{  lookahead.Next();  }
				}

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.String);  }

			iterator = lookahead;
			return true;
			}



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: powershellKeywords
		 */
		static protected StringSet powershellKeywords = new StringSet (KeySettings.IgnoreCase, new string[] {

			"function", "if", "else", "elseif", "switch", "try", "catch", "throw", "for", "foreach", "in", "while", "break", "param",

			"-eq", "-ieq", "-ceq", "-ne", "-ine", "-cne", "-gt", "-igt", "-cgt", "-ge", "-ige", "-cge", "-lt", "-ilt", "-clt",
			"-le", "-ile", "-cle", "-like", "-ilike", "-clike", "-notlike", "-inotlike", "-cnotlike", "-match", "-imatch",
			"-cmatch", "-notmatch", "-inotmatch", "-cnotmatch", "-replace", "-ireplace", "-creplace", "-contains",
			"-icontains", "-ccontains", "-notcontains", "-inotcontains", "-cnotcontains", "-in", "-notin", "-is", "-isnot",
			"-and", "-or", "-xor", "-not", "-split", "-join", "-as",

			"$true", "$false", "$null", "$args", "$this"

			});

		}
	}
