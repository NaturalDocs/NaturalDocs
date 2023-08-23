/*
 * Class: CodeClear.NaturalDocs.Engine.Symbols.SeparatorChars
 * ____________________________________________________________________________
 *
 * The reserved characters that can be used as separators by the symbol encodings.  They use consecutive values
 * so they can be checked for by looking between <LowestValue> and <HighestValue> in addition to individually by name.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Symbols
	{
	public static class SeparatorChars
		{

		/* Constant: Level1
		 * A character that can be used to separate strings which do not contain any other separator characters.
		 */
		public const char Level1 = '\x1F';

		/* Constant: Level2
		 * A character that can be used to separate strings which may contain <Level1> characters.
		 */
		public const char Level2 = '\x1E';

		/* Constant: Level3
		 * A character that can be used to separate strings which may contain <Level1> and <Level2> characters.
		 */
		public const char Level3 = '\x1D';

		/* Constant: Level4
		 * A character that can be used to separate strings which may contain <Level1>, <Level2>, and <Level3> characters.
		 */
		public const char Level4 = '\x1C';

		/* Constant: LowestValue
		 * The lowest value of all the separator characters so that you can check for them with a range.
		 */
		public const char LowestValue = Level4;

		/* Constant: HighestValue
		 * The highest value of all the separator characters so that you can check for them with a range.
		 */
		public const char HighestValue = Level1;

		}
	}
