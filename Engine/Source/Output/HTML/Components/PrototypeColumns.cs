﻿/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeColumns
 * ____________________________________________________________________________
 *
 * A class for storing information about the columns in one or more <Prototypes.ParameterSections>.
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
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class PrototypeColumns
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: PrototypeColumns
		 * Creates a new columns object from the token indexes in a <PrototypeParameters> object.  This should only be called by
		 * <PrototypeParameters>.
		 */
		public PrototypeColumns (ParsedPrototype parsedPrototype, Prototypes.ParameterSection parameterSection,
											  PrototypeCell[,] cells)
			{
			parameterStyle = parameterSection.ParameterStyle;
			columnWidths = new int[CountOf(parameterStyle)];

			RecalculateWidths(parsedPrototype, parameterSection, cells);
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
													 PrototypeCell[,] cells)
			{
			#if DEBUG
			if (CountOf(parameterSection.ParameterStyle) != columnWidths.Length)
				{  throw new Exception("Can only call PrototypeColumns.RecalculateWidths() on the same prototype.");  }
			#endif

			if (parameterSection.NumberOfParameters == 0)
				{  return;  }

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



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Count
		 * Returns the total number of columns for the parameter style, regardless of how many are used.
		 */
		public int Count
			{
			get
				{  return CountOf(parameterStyle);  }
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
		 * Returns an array of column types appropriate for the parameter style.  Do not change the data.
		 */
		public PrototypeColumnType[] Order
			{
			get
				{  return OrderOf(parameterStyle);  }
			}


		/* Property: ParameterStyle
		 * Returns the <ParsedPrototype.ParameterStyle> associated with the columns.
		 */
		public ParsedPrototype.ParameterStyle ParameterStyle
			{
			get
				{  return parameterStyle;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: parameterStyle
		 * The <ParsedPrototype.ParameterStyle> of the columns.
		 */
		protected ParsedPrototype.ParameterStyle parameterStyle;

		/* var: columnWidths
		 * An array of the column widths in characters.  Each one will be the longest width of that column in any individual
		 * parameter.  If the width is zero then that column is not used.
		 */
		protected int[] columnWidths;



		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: CountOf
		 * Returns the number of columns in the passed <ParsedPrototype.ParameterStyle>.
		 */
		public static int CountOf (ParsedPrototype.ParameterStyle parameterStyle)
			{
			return OrderOf(parameterStyle).Length;
			}


		/* Function: OrderOf
		 * Returns an array of column types appropriate for the passed parameter style.  Do not change the data.
		 */
		public static PrototypeColumnType[] OrderOf (ParsedPrototype.ParameterStyle parameterStyle)
			{
			switch (parameterStyle)
				{
				case ParsedPrototype.ParameterStyle.C:
					return CColumnOrder;
				case ParsedPrototype.ParameterStyle.Pascal:
					return PascalColumnOrder;
				default:
					throw new NotSupportedException();
				}
			}



		// Group: Static Variables
		// __________________________________________________________________________

		// DEPENDENCY: PrototypeParameters.CalculateTokenIndexes() depends on the order of these.

		/* var: CColumnOrder
		 * An array of <PrototypeColumnTypes> representing the order in which columns should appear for C-style prototypes.
		 */
		static public PrototypeColumnType[] CColumnOrder = { PrototypeColumnType.ModifierQualifier,
																						 PrototypeColumnType.Type,
																						 PrototypeColumnType.Symbols,
																						 PrototypeColumnType.Name,
																						 PrototypeColumnType.PropertyValueSeparator,
																						 PrototypeColumnType.PropertyValue,
																						 PrototypeColumnType.DefaultValueSeparator,
																						 PrototypeColumnType.DefaultValue };

		/* var: PascalColumnOrder
		 * An array of <PrototypeColumnTypes> representing the order in which columns should appear for Pascal-style prototypes.
		 */
		static public PrototypeColumnType[] PascalColumnOrder = { PrototypeColumnType.ModifierQualifier,
																								PrototypeColumnType.Name,
																								PrototypeColumnType.TypeNameSeparator,
																								PrototypeColumnType.Symbols,
																								PrototypeColumnType.Type,
																								PrototypeColumnType.PropertyValueSeparator,
																								PrototypeColumnType.PropertyValue,
																								PrototypeColumnType.DefaultValueSeparator,
																								PrototypeColumnType.DefaultValue };
		}
	}