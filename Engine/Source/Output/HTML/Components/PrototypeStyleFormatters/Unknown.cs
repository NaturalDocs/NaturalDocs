/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeStyleFormatters.Unknown
 * ____________________________________________________________________________
 *
 * A stub class for prototypes set to <ParameterStyle.Unknown>.  This can happen when the prototype has an empty set of parentheses,
 * which makes it a prototype with parameters but an unknown style.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeStyleFormatters
	{
	public class Unknown : PrototypeStyleFormatter
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: CalculateCells
		 */
		override public PrototypeCellLayout[,] CalculateCells (ParameterSection parameters)
			{
			var cells = new PrototypeCellLayout[ parameters.NumberOfParameters, ColumnCount ];

			for (int parameterIndex = 0; parameterIndex < parameters.NumberOfParameters; parameterIndex++)
				{
				TokenIterator startOfParam, endOfParam;
				parameters.GetParameterBounds(parameterIndex, out startOfParam, out endOfParam);

				TokenIterator iterator = startOfParam;

				while (iterator < endOfParam &&
							iterator.PrototypeParsingType == PrototypeParsingType.Null &&
							iterator.FundamentalType == FundamentalType.Whitespace)
					{  iterator.Next();  }


				// Put everything except values in the name column

				int currentColumn = 0;
				TokenIterator startOfCell = iterator;
				TokenIterator startOfType = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					if (type != PrototypeParsingType.PropertyValueSeparator &&
						type != PrototypeParsingType.PropertyValue &&
						type != PrototypeParsingType.DefaultValueSeparator &&
						type != PrototypeParsingType.DefaultValue)
						{  iterator.Next();   }
					else
						{  break;  }
					}

				TokenIterator endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// PropertyValueSeparator

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					if (type == PrototypeParsingType.PropertyValueSeparator ||
						type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else
						{  break;  }
					}

				endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// PropertyValue

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					if (type == PrototypeParsingType.PropertyValue ||
						type == PrototypeParsingType.ParamSeparator ||
						type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else
						{  break;  }
					}

				endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// DefaultValueSeparator

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					if (type == PrototypeParsingType.DefaultValueSeparator ||
						type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else
						{  break;  }
					}

				endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// DefaultValue

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{  iterator.Next();   }

				endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;
				}

			return cells;
			}


		/* Function: ColumnSpacingOf
		 * Returns the spacing of the passed column type.
		 */
		override public ColumnSpacing ColumnSpacingOf (PrototypeColumnType columnType)
			{
			switch (columnType)
				{
				case PrototypeColumnType.DefaultValueSeparator:
					return ColumnSpacing.AlwaysBoth;

				case PrototypeColumnType.PropertyValueSeparator:
					return ColumnSpacing.SpacedUnlessColon;

				default:
					return ColumnSpacing.Normal;
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: ColumnOrder
		 */
		override public PrototypeColumnType[] ColumnOrder
			{
			get
				{  return ColumnOrderValues;  }
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: ColumnOrderValues
		 */
		readonly static public PrototypeColumnType[] ColumnOrderValues = { PrototypeColumnType.Name,
																											  PrototypeColumnType.PropertyValueSeparator,
																											  PrototypeColumnType.PropertyValue,
																											  PrototypeColumnType.DefaultValueSeparator,
																											  PrototypeColumnType.DefaultValue };

		}
	}
