/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.PowerBuilder
 * ____________________________________________________________________________
 *
 * Additional language support for PowerBuilder.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class PowerBuilder : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: PowerBuilder
		 */
		public PowerBuilder (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
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
				if (lookahead.Character == '~')
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

		}
	}
