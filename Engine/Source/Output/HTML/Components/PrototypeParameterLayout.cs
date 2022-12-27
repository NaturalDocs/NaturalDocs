/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeParameterLayout
 * ____________________________________________________________________________
 *
 * A class for storing information about the HTML layout of a single <Prototypes.ParameterSection>.
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
	public class PrototypeParameterLayout
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: PrototypeParameterLayout
		 */
		public PrototypeParameterLayout (ParsedPrototype parsedPrototype, Prototypes.ParameterSection parameterSection)
			{
			this.parsedPrototype = parsedPrototype;
			this.parameters = parameterSection;

			CalculateCells();

			columns = new PrototypeColumnLayout(parsedPrototype, parameterSection, cells);

			hasSpaceBeforeParameters = false;
			hasSpaceAfterParameters = false;

			TokenIterator start, end;

			parameterSection.GetBeforeParameters(out start, out end);
			end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);
			beforeParametersContentWidth = end.RawTextIndex - start.RawTextIndex;

			parameterSection.GetAfterParameters(out start, out end);
			afterParametersContentWidth = end.RawTextIndex - start.RawTextIndex;
			}


		/* Function: HasContent
		 * Whether there is content at the specified parameter and column.
		 */
		public bool HasContent (int parameterIndex, int columnIndex)
			{
			if (parameterIndex < parameters.NumberOfParameters &&
				columnIndex < columns.Count)
				{
				return (cells[parameterIndex, columnIndex].IsEmpty == false);
				}
			else
				{  throw new IndexOutOfRangeException();  }
			}


		/* Function: GetContent
		 * Returns the content at a specific parameter and column, or false if there wasn't any.
		 */
		public bool GetContent (int parameterIndex, int columnIndex, out TokenIterator start, out TokenIterator end)
			{
			if (parameterIndex < parameters.NumberOfParameters &&
				columnIndex < columns.Count)
				{
				start = parsedPrototype.Tokenizer.FirstToken;
				start.RawTextIndex = cells[parameterIndex, columnIndex].StartingTextIndex;

				end = start;
				end.RawTextIndex = cells[parameterIndex, columnIndex].EndingTextIndex;

				return (start < end);
				}
			else
				{  throw new IndexOutOfRangeException();  }
			}


		/* Function: GetContentWidth
		 * Returns the width of the content at the specified parameter and column, in characters.
		 */
		public int GetContentWidth (int parameterIndex, int columnIndex)
			{
			if (parameterIndex < parameters.NumberOfParameters &&
				columnIndex < columns.Count)
				{
				return cells[parameterIndex, columnIndex].Width;
				}
			else
				{  throw new IndexOutOfRangeException();  }
			}


		/* Function: HasLeadingSpace
		 * Whether the passed parameter and column has a leading space.
		 */
		public bool HasLeadingSpace (int parameterIndex, int columnIndex)
			{
			if (parameterIndex < parameters.NumberOfParameters &&
				columnIndex < columns.Count)
				{  return cells[parameterIndex, columnIndex].HasLeadingSpace;  }
			else
				{  throw new IndexOutOfRangeException();  }
			}


		/* Function: HasTrailingSpace
		 * Whether the passed parameter and column has a trailing space.
		 */
		public bool HasTrailingSpace (int parameterIndex, int columnIndex)
			{
			if (parameterIndex < parameters.NumberOfParameters &&
				columnIndex < columns.Count)
				{  return cells[parameterIndex, columnIndex].HasTrailingSpace;  }
			else
				{  throw new IndexOutOfRangeException();  }
			}


		/* Function: SetLeadingSpace
		 * Sets whether the passed parameter and column has a leading space.  Calling this does not automatically update
		 * <Columns>.  After you're done making changes with this function and <SetTrailingSpace()> call
		 * <RecalculateColumns()> to update it.
		 */
		public void SetLeadingSpace (int parameterIndex, int columnIndex, bool hasLeadingSpace)
			{
			if (parameterIndex < parameters.NumberOfParameters &&
				columnIndex < columns.Count)
				{  cells[parameterIndex, columnIndex].HasLeadingSpace = hasLeadingSpace;  }
			else
				{  throw new IndexOutOfRangeException();  }
			}


		/* Function: SetTrailingSpace
		 * Sets whether the passed parameter and column has a trailing space.  Calling this does not automatically update
		 * <Columns>.  After you're done making changes with this function and <SetLeadingSpace()> call
		 * <RecalculateColumns()> to update it.
		 */
		public void SetTrailingSpace (int parameterIndex, int columnIndex, bool hasTrailingSpace)
			{
			if (parameterIndex < parameters.NumberOfParameters &&
				columnIndex < columns.Count)
				{  cells[parameterIndex, columnIndex].HasTrailingSpace = hasTrailingSpace;  }
			else
				{  throw new IndexOutOfRangeException();  }
			}


		/* Function: LastCellEndsWithSpace
		 * Whether the last column of the last parameter either doesn't have any content or its content is shorter than the
		 * column width, resulting in empty space between it and the part after the parameters.
		 */
		public bool LastCellEndsWithSpace (PrototypeColumnLayout columnLayout)
			{
			if (NumberOfParameters == 0)
				{  return false;  }

			int lastParameterIndex = NumberOfParameters - 1;
			int lastColumnIndex = columnLayout.LastUsed;

			return (HasContent(lastParameterIndex, lastColumnIndex) == false ||
						GetContentWidth(lastParameterIndex, lastColumnIndex) < columnLayout.WidthOf(lastColumnIndex));
			}


		/* Function: RecalculateColumns
		 * Recalculates <Columns> after making changes to the layout via <SetleadingSpace()> or <SetTrailingSpace()>.
		 */
		public void RecalculateColumns ()
			{
			columns.RecalculateWidths(parsedPrototype, parameters, cells);
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: CalculateCells
		 * Creates and fills in <cells> and <columns>.
		 */
		protected void CalculateCells ()
			{
			if (parameters.NumberOfParameters == 0)
				{  cells = null;  }
			else if (parameters.ParameterStyle == ParsedPrototype.ParameterStyles.C)
				{  cells = PrototypeStyles.C.CalculateCells(parameters);  }
			else if (parameters.ParameterStyle == ParsedPrototype.ParameterStyles.Pascal)
				{  cells = PrototypeStyles.Pascal.CalculateCells(parameters);  }
			else
				{  throw new NotImplementedException();  }

			columns = new PrototypeColumnLayout(parsedPrototype, parameters, cells);
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: ParameterStyle
		 */
		public ParsedPrototype.ParameterStyles ParameterStyle
			{
			get
				{  return parameters.ParameterStyle;  }
			}


		/* Property: NumberOfParameters
		 * The number of parameters.
		 */
		public int NumberOfParameters
			{
			get
				{  return parameters.NumberOfParameters;  }
			}


		/* Property: Columns
		 * Information about the columns across all parameters.
		 */
		public PrototypeColumnLayout Columns
			{
			get
				{  return columns;  }
			}


		/* Property: HasSpaceBeforeParameters
		 * Whether there should be a space between the parameters and the part before it, which would include things
		 * like the opening parenthesis.
		 */
		public bool HasSpaceBeforeParameters
			{
			get
				{  return hasSpaceBeforeParameters;  }
			set
				{  hasSpaceBeforeParameters = value;  }
			}


		/* Property: HasSpaceAfterParameters
		 * Whether there should be a space between the parameters and the part after it, which would include things
		 * like the closing parenthesis.
		 */
		public bool HasSpaceAfterParameters
			{
			get
				{  return hasSpaceAfterParameters;  }
			set
				{  hasSpaceAfterParameters = value;  }
			}


		/* Property: BeforeParametersWidth
		 * The width of the section before the parameters.  This includes <HasSpaceBeforeParameters>.
		 */
		public int BeforeParametersWidth
			{
			get
				{  return beforeParametersContentWidth + (hasSpaceBeforeParameters ? 1 : 0);  }
			}


		/* Property: AfterParameterswidth
		 * The width of the section after the parameters, or zero if there is none.  This includes <HasSpaceAfterParameters>.
		 */
		public int AfterParametersWidth
			{
			get
				{
				// We always want to return zero here, regardless of hasSpaceAfterParameters
				if (afterParametersContentWidth == 0)
					{  return 0;  }

				return afterParametersContentWidth + (hasSpaceAfterParameters ? 1 : 0);
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: parsedPrototype
		 * The prototype as a <ParsedPrototype> object.
		 */
		protected ParsedPrototype parsedPrototype;

		/* var: parameters
		 * The <Prototypes.ParameterSection> being represented.
		 */
		protected Prototypes.ParameterSection parameters;

		/* var: cells
		 * A table of cells representing the parameter table.  There is one row per parameter, and each row is a series of
		 * <PrototypeCellLayouts> in either <PrototypeColumnLayout.CColumnOrder> or
		 * <PrototypeColumnLayout.PascalColumnOrder>.  If the column is empty the start and end indexes will be the same.
		 * If there are no parameters, this will be null.
		 */
		protected PrototypeCellLayout[,] cells;

		/* var: columns
		 * Information about the columns across all parameters.
		 */
		protected PrototypeColumnLayout columns;

		/* var: hasSpaceBeforeParameters
		 * Whether there should be a space between the parameters and the part before it, which would include things
		 * like the opening parenthesis.
		 */
		protected bool hasSpaceBeforeParameters;

		/* var: hasSpaceAfterParameters
		 * Whether there should be a space between the parameters and the part after it, which would include things
		 * like the closing parenthesis.
		 */
		protected bool hasSpaceAfterParameters;

		/* var: beforeParametersContentWidth
		 * The width of the content of the section before the parameters.  This does not include <hasSpaceBeforeParameters>.
		 */
		protected int beforeParametersContentWidth;

		/* var: afterParametersContentWidth
		 * The width of the content of the section after the parameters, or zero if there is none.  This does not include
		 * <hasSpaceAfterParameters>.
		 */
		protected int afterParametersContentWidth;

		}
	}
