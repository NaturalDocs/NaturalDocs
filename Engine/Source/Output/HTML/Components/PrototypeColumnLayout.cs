/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeColumnLayout
 * ____________________________________________________________________________
 *
 * A class for storing information about the columns in one or more <PrototypeParameterLayouts>.
 *
 *
 * Threading: Not Thread Safe
 *
 *		This class is only designed to be used by one thread at a time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Prototypes;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class PrototypeColumnLayout
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: PrototypeColumnLayout
		 * Calculates the column layout of a <ParsedPrototype> <ParameterSection> that has already been divided into
		 * cells.  This should only be called by <HTML.Components.PrototypeParameterLayout>.
		 */
		internal PrototypeColumnLayout (ParsedPrototype parsedPrototype, Prototypes.ParameterSection parameterSection,
														PrototypeCellLayout[,] cells)
			{
			parameterStyle = parameterSection.ParameterStyle;
			columnWidths = new int[Formatter.ColumnCount];

			RecalculateWidths(parsedPrototype, parameterSection, cells);
			}


		/* Constructor: PrototypeColumnLayout
		 * Duplicates the passed PrototypeColumnLayout.
		 */
		public PrototypeColumnLayout (PrototypeColumnLayout toCopy)
			{
			parameterStyle = toCopy.parameterStyle;
			columnWidths = (int[])toCopy.columnWidths.Clone();
			}


		/* Function: Duplicate
		 * Creates an independent copy of the PrototypeColumnLayout.
		 */
		public PrototypeColumnLayout Duplicate ()
			{
			return new PrototypeColumnLayout(this);
			}


		/* Function: MergeWith
		 * Merges the values of the passed column layout into our own to provide information about a combined set of columns.
		 * The passed columns must have the same <ParameterStyle>.
		 */
		public void MergeWith (PrototypeColumnLayout toMergeWith)
			{
			#if DEBUG
			if (this.parameterStyle != toMergeWith.parameterStyle)
				{  throw new Exception("Can only merge with the same parameter style.");  }
			#endif

			for (int i = 0; i < columnWidths.Length; i++)
				{
				this.columnWidths[i] = Math.Max(this.columnWidths[i], toMergeWith.columnWidths[i]);
				}
			}


		/* Function: IsUsed
		 * Returns whether the column is used at all in any of the parameters.
		 */
		public bool IsUsed (int columnIndex)
			{
			if (columnIndex < columnWidths.Length)
				{  return (columnWidths[columnIndex] != 0);  }
			else
				{  throw new IndexOutOfRangeException();  }
			}


		/* Function: IsUsed
		 * Returns whether the column type is used at all in any of the parameters.
		 */
		public bool IsUsed (PrototypeColumnType columnType)
			{
			int columnIndex = IndexOf(columnType);

			if (columnIndex == -1)
				{  return false;  }
			else
				{  return IsUsed(columnIndex);  }
			}


		/* Function: NextUsed
		 * Returns the index of the next used column, or -1 if there isn't one.
		 */
		 public int NextUsed (int columnIndex)
			{
			for (int i = columnIndex + 1; i < columnWidths.Length; i++)
				{
				if (columnWidths[i] != 0)
					{  return i;  }
				}

			return -1;
			}


		/* Function: PreviousUsed
		 * Returns the index of the previous used column, or -1 if there isn't one.
		 */
		 public int PreviousUsed (int columnIndex)
			{
			for (int i = columnIndex - 1; i >= 0; i--)
				{
				if (columnWidths[i] != 0)
					{  return i;  }
				}

			return -1;
			}


		/* Function: WidthOf
		 * Returns the width of the column in characters, which will be the width across all parameters.  Will return zero if the column
		 * is not used.
		 */
		public int WidthOf (int columnIndex)
			{
			if (columnIndex < columnWidths.Length)
				{  return columnWidths[columnIndex];  }
			else
				{  throw new IndexOutOfRangeException();  }
			}


		/* Function: TypeOf
		 * Returns the <PrototypeColumnType> of the column.
		 */
		public PrototypeColumnType TypeOf (int columnIndex)
			{
			if (columnIndex < columnWidths.Length)
				{  return Order[columnIndex];  }
			else
				{  throw new IndexOutOfRangeException();  }
			}


		/* Function: IndexOf
		 * Returns the column index of the passed type, or -1 if it doesn't exist for the parameter style.
		 */
		public int IndexOf (PrototypeColumnType type)
			{
			var order = this.Order;

			for (int i = 0; i < order.Length; i++)
				{
				if (order[i] == type)
					{  return i;  }
				}

			return -1;
			}


		/* Function: UsedColumnIndexOf
		 * Converts a column index to an index into the number of used columns, so this will return zero for the first used column,
		 * one for the second used column, etc.  It will return -1 for columns that aren't used.
		 */
		 public int UsedColumnIndexOf (int columnIndex)
			{
			if (!IsUsed(columnIndex))
				{  return -1;  }

			int usedColumnIndex = 0;

			for (int i = 0; i < columnIndex; i++)
				{
				if (columnWidths[i] != 0)
					{  usedColumnIndex++;  }
				}

			return usedColumnIndex;
			}


		/* Function: RecalculateWidths
		 * Updates the column widths if the <PrototypeCells> have changed.
		 */
		public void RecalculateWidths (ParsedPrototype parsedPrototype, Prototypes.ParameterSection parameterSection,
													 PrototypeCellLayout[,] cells)
			{
			#if DEBUG
			if (parameterSection.ParameterStyle != parameterStyle)
				{  throw new Exception("Can only call RecalculateWidths() on the same prototype.");  }
			#endif

			if (parameterSection.NumberOfParameters == 0)
				{
				for (int columnIndex = 0; columnIndex < this.Count; columnIndex++)
					{  columnWidths[columnIndex] = 0;  }
				}

			else
				{
				// Just copy the first parameter's widths.  Don't merge this loop into the second one because we may be
				// recalculating an existing array and want to overwrite its values to start.
				for (int columnIndex = 0; columnIndex < this.Count; columnIndex++)
					{
					columnWidths[columnIndex] = cells[0, columnIndex].Width;
					}

				// See if later parameter widths are bigger.
				for (int parameterIndex = 1; parameterIndex < parameterSection.NumberOfParameters; parameterIndex++)
					{
					for (int columnIndex = 0; columnIndex < this.Count; columnIndex++)
						{
						int oldWidth = columnWidths[columnIndex];
						int newWidth = cells[parameterIndex, columnIndex].Width;

						if (newWidth > oldWidth)
							{  columnWidths[columnIndex] = newWidth;  }
						}
					}
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Count
		 * Returns the total number of columns for the parameter style, regardless of how many are used.
		 */
		public int Count
			{
			get
				{  return Formatter.ColumnCount;  }
			}


		/* Property: UsedCount
		 * Returns the number of used columns.
		 */
		public int UsedCount
			{
			get
				{
				int usedCount = 0;

				for (int i = 0; i < columnWidths.Length; i++)
					{
					if (columnWidths[i] != 0)
						{  usedCount++;  }
					}

				return usedCount;
				}
			}


		/* Property: FirstUsed
		 * Returns the index of the first used column, or -1 if there isn't one.
		 */
		 public int FirstUsed
			{
			get
				{
				for (int i = 0; i < columnWidths.Length; i++)
					{
					if (columnWidths[i] != 0)
						{  return i;  }
					}

				return -1;
				}
			}


		/* Property: LastUsed
		 * Returns the index of the last used column, or -1 if there isn't one.
		 */
		 public int LastUsed
			{
			get
				{
				for (int i = columnWidths.Length - 1; i >= 0; i--)
					{
					if (columnWidths[i] != 0)
						{  return i;  }
					}

				return -1;
				}
			}


		/* Property: Order
		 * Returns the order of column types for the parameter style.  Do not change the data.
		 */
		public PrototypeColumnType[] Order
			{
			get
				{  return Formatter.ColumnOrder;  }
			}


		/* Property: ParameterStyle
		 * Returns the <ParameterStyle> associated with the columns.
		 */
		public ParameterStyle ParameterStyle
			{
			get
				{  return parameterStyle;  }
			}


		/* Property: Formatter
		 * Returns the <PrototypeStyleFormatter> associated with <ParameterStyle>.
		 */
		public PrototypeStyleFormatter Formatter
			{
			get
				{  return Prototype.FormatterOf(parameterStyle);  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: parameterStyle
		 * The <ParameterStyle> of the columns.
		 */
		protected ParameterStyle parameterStyle;

		/* var: columnWidths
		 * An array of the column widths in characters.  Each one will be the longest width of that column in any individual
		 * parameter.  If the width is zero then that column is not used.
		 */
		protected int[] columnWidths;

		}
	}
