/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeStyles.SystemVerilog
 * ____________________________________________________________________________
 *
 * Information to help format <ParameterStyle.SystemVerilog> parameters.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeStyles
	{
	public static class SystemVerilog
		{

		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: CalculateCells
		 * Takes a SystemVerilog <ParameterSection> and generates a table of <PrototypeCellLayouts>.  Each row represents
		 * a parameter, and each cell is a column in <ColumnOrder>.
		 */
		public static PrototypeCellLayout[,] CalculateCells (ParameterSection parameters)
			{
			var cells = new PrototypeCellLayout[ parameters.NumberOfParameters, ColumnOrder.Length ];

			for (int parameterIndex = 0; parameterIndex < parameters.NumberOfParameters; parameterIndex++)
				{
				TokenIterator startOfParam, endOfParam;
				parameters.GetParameterBounds(parameterIndex, out startOfParam, out endOfParam);

				TokenIterator iterator = startOfParam;

				while (iterator < endOfParam &&
							iterator.PrototypeParsingType == PrototypeParsingType.Null &&
							iterator.FundamentalType == FundamentalType.Whitespace)
					{  iterator.Next();  }


				// Parameter Keywords

				int currentColumn = 0;
				TokenIterator startOfCell = iterator;
				TokenIterator startOfType = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					// We merge parameter keywords ("localparam" etc) with in/out ("input") since there will usually only
					// be one or the other, so they should share a column in the output.  Null covers whitespace if more than
					// one exists.
					if (type == PrototypeParsingType.ParamKeyword ||
						type == PrototypeParsingType.InOut ||
						type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else
						{  break;  }
					}

				TokenIterator endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// Type

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					if (type == PrototypeParsingType.Type ||
						type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else
						{  break;  }
					}

				endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// Signed

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					if (type == PrototypeParsingType.Signed ||
						type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else
						{  break;  }
					}

				endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// Type Dimension

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					if (type == PrototypeParsingType.OpeningTypeModifier)
						{  Shared.SkipModifierBlock(ref iterator, endOfParam);  }
					else if (type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else
						{  break;  }
					}

				endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// Name

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					// Include the parameter separator because there may not be a default value.
					if (type == PrototypeParsingType.Name ||
						type == PrototypeParsingType.KeywordName ||
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


				// Parameter Dimension

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					if (type == PrototypeParsingType.OpeningParamModifier)
						{  Shared.SkipModifierBlock(ref iterator, endOfParam);  }
					else if (type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else
						{  break;  }
					}

				endOfCell = iterator;

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

				endOfCell = endOfParam;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;
				}

			return cells;
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: ColumnOrder
		 * An array of <PrototypeColumnTypes> representing the order in which columns should appear for SystemVerilog prototypes.
		 */
		static public PrototypeColumnType[] ColumnOrder = { PrototypeColumnType.ParameterKeywords,
																					  PrototypeColumnType.Type,
																					  PrototypeColumnType.Signed,
																					  PrototypeColumnType.TypeDimension,
																					  PrototypeColumnType.Name,
																					  PrototypeColumnType.ParameterDimension,
																					  PrototypeColumnType.PropertyValueSeparator,
																					  PrototypeColumnType.PropertyValue,
																					  PrototypeColumnType.DefaultValueSeparator,
																					  PrototypeColumnType.DefaultValue };

		/* var: ColumnsAlwaysSpaced
		 * An array of <PrototypeColumnTypes> representing the columns that should always be formatted with spaces on both
		 * sides of the content.
		 */
		static public PrototypeColumnType[] ColumnsAlwaysSpaced = { PrototypeColumnType.TypeDimension,
																								   PrototypeColumnType.ParameterDimension,
																								   PrototypeColumnType.PropertyValueSeparator,
																								   PrototypeColumnType.DefaultValueSeparator };

		/* var: ColumnsSpacedUnlessColon
		 * An array of <PrototypeColumnTypes> representing the columns that should always be formatted with spaces on both
		 * sides of the content, unless the content is a colon, in which case it should only be on the right.
		 */
		static public PrototypeColumnType[] ColumnsSpacedUnlessColon = { };

		}
	}
