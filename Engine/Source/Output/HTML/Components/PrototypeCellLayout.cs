/*
 * Struct: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeCellLayout
 * ____________________________________________________________________________
 *
 * A simple struct for storing information about a single cell in a <PrototypeParameterLayout>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public struct PrototypeCellLayout
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

				if (HasLeadingSpace)
					{  width++;  }
				if (HasTrailingSpace)
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

		/* var: HasLeadingSpace
		 * Whether the cell has a leading space.
		 */
		public bool HasLeadingSpace;

		/* var: HasTrailingSpace
		 * Whether the cell has a trailing space.
		 */
		public bool HasTrailingSpace;

		}
	}
