/*
 * Struct: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeCell
 * ____________________________________________________________________________
 *
 * A simlpe struct for storing information about a single parameter cell in a <Prototypes.ParameterSections>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public struct PrototypeCell
		{

		// Group: Properties
		// __________________________________________________________________________


		/* Property: IsEmpty
		 */
		public bool IsEmpty
			{
			get
				{  return (StartingTextIndex == EndingTextIndex);  }
			}

		/* Property: Width
		 * The width of the cell in characters.
		 */
		public int Width
			{
			get
				{
				int width = EndingTextIndex - StartingTextIndex;

				if (LeadingSpace)
					{  width++;  }
				if (TrailingSpace)
					{  width++;  }

				return width;
				}
			}



		// Group: Public Variables
		// __________________________________________________________________________


		/* var: StartingTextIndex
		 * The text index of the first character in the cell.
		 */
		public int StartingTextIndex;

		/* var: EndingTextIndex
		 * The text index of one past the last character in the cell.  This will not include trailing whitespace.
		 */
		public int EndingTextIndex;

		/* var: LeadingSpace
		 * Whether the cell has a leading space.
		 */
		public bool LeadingSpace;

		/* var: TrailingSpace
		 * Whether the cell has a trailing space.
		 */
		public bool TrailingSpace;

		}
	}
