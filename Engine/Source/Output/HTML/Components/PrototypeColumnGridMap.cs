/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeColumnGridMap
 * ____________________________________________________________________________
 *
 * A class that maps <PrototypeColumns> to values that can be used in a CSS grid.  Only the used columns
 * are assigned numbers.
 *
 *
 * Threading: Not Thread Safe
 *
 *		This class is only designed to be used by one thread at a time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Prototypes;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class PrototypeColumnGridMap
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: PrototypeColumnGridMap
		 */
		public PrototypeColumnGridMap (PrototypeColumns columns)
			{
			parameterStyle = columns.ParameterStyle;
			gridMap = new int[columns.Count];

			int nextValue = 1;

			for (int i = 0; i < columns.Count; i++)
				{
				if (columns.IsUsed(i))
					{
					gridMap[i] = nextValue;
					nextValue++;
					}
				else
					{  gridMap[i] = 0;  }
				}

			usedColumnCount = nextValue - 1;
			}


		/* Function: Add
		 * Adds a second <PrototypeColumns> object to the map so the grid represents the union of both of them.  The
		 * columns <ParsedPrototype.ParameterStyle> must match the one passed to the constructor.
		 */
		public void Add (PrototypeColumns columns)
			{
			if (columns.ParameterStyle != this.parameterStyle)
				{  throw new InvalidOperationException();  }

			int nextValue = 1;

			for (int i = 0; i < columns.Count; i++)
				{
				if (gridMap[i] != 0 || columns.IsUsed(i))
					{
					gridMap[i] = nextValue;
					nextValue++;
					}
				else
					{  gridMap[i] = 0;  }
				}

			usedColumnCount = nextValue - 1;
			}


		/* Function: GridValueOf
		 * Returns the CSS grid number that should be used for the passed column index, or zero if the column isn't
		 * used.  You can set the value for the first grid column and all others will be offset from that.  If you don't the
		 * grid values start at one.
		 */
		public int GridValueOf (int columnIndex, int firstValue = 1)
			{
			if (columnIndex >= gridMap.Length)
				{  throw new IndexOutOfRangeException();  }
			else if (gridMap[columnIndex] == 0)
				{  return 0;  }
			else
				{  return gridMap[columnIndex] + (firstValue - 1);  }
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: UsedColumnCount
		 * The number of used columns in the grid.
		 */
		public int UsedColumnCount
			{
			get
				{  return usedColumnCount;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: gridMap
		 * An array of grid position values, one representing each column in <columns>.  The first used column will
		 * have the value 1, the second used column 2, etc.  Unused columns will be zero.
		 */
		protected int[] gridMap;

		/* var: usedColumnCount
		 * The number of used columns in the grid.
		 */
		protected int usedColumnCount;

		/* var: parameterStyle
		 * The <ParsedPrototype.ParameterStyle> of the grid.
		 */
		protected ParsedPrototype.ParameterStyle parameterStyle;

		}
	}
