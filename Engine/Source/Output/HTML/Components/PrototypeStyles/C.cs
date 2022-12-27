/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeStyles.C
 * ____________________________________________________________________________
 *
 * Information to help format <ParsedPrototype.ParameterStyles.C> parameters.
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
	public static class C
		{

		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: CalculateCells
		 * Takes a C-style <ParameterSection> and generates a table of <PrototypeCellLayouts>.  Each row represents a parameter, and
		 * each cell is a column in <ColumnOrder>.
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
				TokenIterator startOfType = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					// Null covers whitespace and any random symbols we encountered that went unmarked.
					if (type == PrototypeParsingType.TypeModifier ||
						type == PrototypeParsingType.TypeQualifier ||
						type == PrototypeParsingType.ParamModifier ||
						type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else if (type == PrototypeParsingType.OpeningTypeModifier ||
								type == PrototypeParsingType.OpeningParamModifier)
						{  Shared.SkipModifierBlock(ref iterator, endOfParam);  }
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

					// The previous loop already got any modifiers before the type, so this will only cover the type
					// plus any modifiers following it.
					if (type == PrototypeParsingType.Type ||
						type == PrototypeParsingType.TypeModifier ||
						type == PrototypeParsingType.ParamModifier ||
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


				// Symbols

				currentColumn++;

				// All symbols are part of the type column right now because they're marked as type or param modifiers.
				// Start with an empty column and walk backwards to claim the symbols from the type column.

				cells[parameterIndex, currentColumn].StartingTextIndex = iterator.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = false;
				cells[parameterIndex, currentColumn].EndingTextIndex = iterator.RawTextIndex;

				if (iterator > startOfType)
					{
					TokenIterator lookbehind = iterator;
					lookbehind.Previous();

					if (lookbehind.FundamentalType == FundamentalType.Symbol &&
						lookbehind.Character != '_' &&
						lookbehind.PrototypeParsingType != PrototypeParsingType.ClosingTypeModifier &&
						lookbehind.PrototypeParsingType != PrototypeParsingType.ClosingParamModifier)
						{
						cells[parameterIndex, currentColumn].StartingTextIndex = lookbehind.RawTextIndex;
						lookbehind.Previous();

						while (lookbehind >= startOfType)
							{
							if (lookbehind.FundamentalType == FundamentalType.Symbol &&
								lookbehind.Character != '_' &&
								lookbehind.PrototypeParsingType != PrototypeParsingType.ClosingTypeModifier &&
								lookbehind.PrototypeParsingType != PrototypeParsingType.ClosingParamModifier)
								{
								cells[parameterIndex, currentColumn].StartingTextIndex = lookbehind.RawTextIndex;
								lookbehind.Previous();
								}
							else
								{  break;  }
							}


						// Fix up any columns we stole from

						int cutPoint = cells[parameterIndex, currentColumn].StartingTextIndex;

						for (int i = currentColumn - 1; i >= 0; i--)
							{
							// If the starting point is at or after the cut, we cancelled out an entire column
							if (cells[parameterIndex, i].StartingTextIndex >= cutPoint)
								{
								cells[parameterIndex, i].StartingTextIndex = cutPoint;
								cells[parameterIndex, i].HasTrailingSpace = false;
								cells[parameterIndex, i].EndingTextIndex = cutPoint;
								}

							// If only the ending point is after the cut, we truncated the column
							else if (cells[parameterIndex, i].EndingTextIndex > cutPoint)
								{
								cells[parameterIndex, i].EndingTextIndex = cutPoint;

								TokenIterator cellStart = iterator;
								cellStart.RawTextIndex = cells[parameterIndex, i].StartingTextIndex;

								TokenIterator cellEnd = cellStart;
								cellEnd.RawTextIndex = cutPoint;

								if (cellEnd.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, cellStart))
									{
									cells[parameterIndex, i].EndingTextIndex = cellEnd.RawTextIndex;
									cells[parameterIndex, i].HasTrailingSpace = true;
									}
								else
									{
									cells[parameterIndex, i].HasTrailingSpace = false;
									}

								break;
								}

							else
								{  break;  }
							}
						}
					}


				// Name

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					// Include the parameter separator because there may not be a default value.
					// Include modifiers because there still may be some after the name.
					if (type == PrototypeParsingType.Name ||
						type == PrototypeParsingType.KeywordName ||
						type == PrototypeParsingType.TypeModifier ||
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
		 * An array of <PrototypeColumnTypes> representing the order in which columns should appear for C-style prototypes.
		 */
		static public PrototypeColumnType[] ColumnOrder = { PrototypeColumnType.ModifierQualifier,
																					  PrototypeColumnType.Type,
																					  PrototypeColumnType.Symbols,
																					  PrototypeColumnType.Name,
																					  PrototypeColumnType.PropertyValueSeparator,
																					  PrototypeColumnType.PropertyValue,
																					  PrototypeColumnType.DefaultValueSeparator,
																					  PrototypeColumnType.DefaultValue };
		}
	}
