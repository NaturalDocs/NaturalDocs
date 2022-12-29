/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeStyles.Pascal
 * ____________________________________________________________________________
 *
 * Information to help format <ParameterStyle.Pascal> parameters.
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
	public static class Pascal
		{

		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: CalculateCells
		 * Takes a Pascal-style <ParameterSection> and generates a table of <PrototypeCellLayouts>.  Each row represents a parameter,
		 * and each cell is a column in <ColumnOrder>.
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


				// ModifierQualifier

				int currentColumn = 0;
				TokenIterator startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					if (type == PrototypeParsingType.TypeModifier ||
						type == PrototypeParsingType.ParamModifier ||
						type == PrototypeParsingType.ParamSeparator ||
						type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else if (type == PrototypeParsingType.OpeningTypeModifier ||
								type == PrototypeParsingType.OpeningParamModifier)
						{  Shared.SkipModifierBlock(ref iterator, endOfParam);  }
					else
						{  break;  }
					}


				// Do we have a name-type separator?  We may not, such as for SQL.

				bool hasNameTypeSeparator = false;
				TokenIterator lookahead = iterator;

				while (lookahead < endOfParam)
					{
					if (lookahead.PrototypeParsingType == PrototypeParsingType.NameTypeSeparator)
						{
						hasNameTypeSeparator = true;
						break;
						}

					lookahead.Next();
					}

				TokenIterator endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// Name

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					// Include the parameter separator because there may not be a type.
					if (type == PrototypeParsingType.Name ||
						type == PrototypeParsingType.KeywordName ||
						type == PrototypeParsingType.ParamSeparator ||
						type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					// Include modifiers because there still may be some after the name, but only if there's a name-type separator.
					else if (hasNameTypeSeparator &&
								(type == PrototypeParsingType.TypeModifier ||
								type == PrototypeParsingType.ParamModifier))
						{  iterator.Next();   }
					else if (type == PrototypeParsingType.OpeningTypeModifier ||
								type == PrototypeParsingType.OpeningParamModifier)
						{  Shared.SkipModifierBlock(ref iterator, endOfParam);  }
					else
						{  break;  }
					}

				endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// TypeNameSeparator

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					if (type == PrototypeParsingType.NameTypeSeparator ||
						type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else
						{  break;  }
					}

				endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// Symbols

				currentColumn++;
				startOfCell = iterator;

				if (iterator < endOfParam &&
					iterator.FundamentalType == FundamentalType.Symbol &&
					iterator.Character != '_')
					{
					while (iterator < endOfParam)
						{
						PrototypeParsingType type = iterator.PrototypeParsingType;

						if ( (
								( iterator.FundamentalType == FundamentalType.Symbol && iterator.Character != '_' ) ||
								( iterator.FundamentalType == FundamentalType.Whitespace )
								) &&
							( type == PrototypeParsingType.TypeModifier ||
								type == PrototypeParsingType.ParamModifier ||
								type == PrototypeParsingType.Null) )
							{  iterator.Next();   }
						else
							{  break;  }
						}
					}

				endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// Type

				currentColumn++;
				startOfCell = iterator;

				// Allow this column to claim the contents of a raw prototype.  They should all be null tokens.
				// We use the type column instead of the name column because the name column isn't syntax highlighted.
				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					// Include the parameter separator because there may not be a default value.
					if (type == PrototypeParsingType.Type ||
						type == PrototypeParsingType.TypeModifier ||
						type == PrototypeParsingType.TypeQualifier ||
						type == PrototypeParsingType.ParamModifier ||
						type == PrototypeParsingType.ParamSeparator ||
						type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else if (type == PrototypeParsingType.OpeningTypeModifier ||
								type == PrototypeParsingType.OpeningParamModifier)
						{  Shared.SkipModifierBlock(ref iterator, endOfParam);  }
					else if (type == PrototypeParsingType.StartOfTuple)
						{  Shared.SkipTuple(ref iterator, endOfParam);  }
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
		 * An array of <PrototypeColumnTypes> representing the order in which columns should appear for Pascal-style prototypes.
		 */
		static public PrototypeColumnType[] ColumnOrder = { PrototypeColumnType.ModifierQualifier,
																					  PrototypeColumnType.Name,
																					  PrototypeColumnType.TypeNameSeparator,
																					  PrototypeColumnType.Symbols,
																					  PrototypeColumnType.Type,
																					  PrototypeColumnType.PropertyValueSeparator,
																					  PrototypeColumnType.PropertyValue,
																					  PrototypeColumnType.DefaultValueSeparator,
																					  PrototypeColumnType.DefaultValue };
		/* var: ColumnsAlwaysSpaced
		 * An array of <PrototypeColumnTypes> representing the columns that should always be formatted with spaces on both
		 * sides of the content.
		 */
		static public PrototypeColumnType[] ColumnsAlwaysSpaced = { PrototypeColumnType.DefaultValueSeparator };

		/* var: ColumnsSpacedUnlessColon
		 * An array of <PrototypeColumnTypes> representing the columns that should always be formatted with spaces on both
		 * sides of the content, unless the content is a colon, in which case it should only be on the right.
		 */
		static public PrototypeColumnType[] ColumnsSpacedUnlessColon = { PrototypeColumnType.TypeNameSeparator,
																										   PrototypeColumnType.PropertyValueSeparator };

		}
	}
