/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeParameterStyles.Parsing
 * ____________________________________________________________________________
 *
 * Some shared functions to help parameter parsing across all styles.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeStyles
	{
	public static class Shared
		{

		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: SkipModifierBlock
		 * If the iterator is on a <PrototypeParsingType.OpeningTypeModifier> or <PrototypeParsingType.OpeningParamModifier>
		 * token, moves the token iterator past the entire block, including any nested blocks.
		 */
		public static void SkipModifierBlock (ref TokenIterator iterator, TokenIterator limit)
			{
			if (iterator < limit &&
				(iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
				 iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier))
				{
				int level = 1;
				iterator.Next();

				while (iterator < limit && level > 0)
					{
					if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
						iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier)
						{  level++;  }
					else if (iterator.PrototypeParsingType == PrototypeParsingType.ClosingTypeModifier ||
							   iterator.PrototypeParsingType == PrototypeParsingType.ClosingParamModifier)
						{  level--;  }

					iterator.Next();
					}
				}
			}


		/* Function: SkipTuple
		 * If the iterator is on a <PrototypeParsingType.StartOfTuple> token, moves the token iterator past the entire tuple,
		 * including any nested tuples.
		 */
		public static void SkipTuple (ref TokenIterator iterator, TokenIterator limit)
			{
			if (iterator < limit &&
				iterator.PrototypeParsingType == PrototypeParsingType.StartOfTuple)
				{
				int level = 1;
				iterator.Next();

				while (iterator < limit && level > 0)
					{
					if (iterator.PrototypeParsingType == PrototypeParsingType.StartOfTuple)
						{  level++;  }
					else if (iterator.PrototypeParsingType == PrototypeParsingType.EndOfTuple)
						{  level--;  }

					iterator.Next();
					}
				}
			}

		}
	}
