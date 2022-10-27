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
			lastCellEndsWithSpace = false;
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
			cells = new PrototypeCellLayout[ parameters.NumberOfParameters, PrototypeColumnLayout.CountOf(parameters.ParameterStyle) ];

			for (int parameterIndex = 0; parameterIndex < parameters.NumberOfParameters; parameterIndex++)
				{
				TokenIterator startOfParam, endOfParam;
				parameters.GetParameterBounds(parameterIndex, out startOfParam, out endOfParam);

				TokenIterator iterator = startOfParam;


				//
				// C-Style Parameters
				//

				// DEPENDENCY: This code depends on the order of PrototypeColumnLayout.CColumnOrder and PascalColumnOrder.

				if (parameters.ParameterStyle == ParsedPrototype.ParameterStyle.C)
					{
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
							{  SkipModifierBlock(ref iterator, endOfParam);  }
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
							{  SkipModifierBlock(ref iterator, endOfParam);  }
						else if (type == PrototypeParsingType.StartOfTuple)
							{  SkipTuple(ref iterator, endOfParam);  }
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
							{  SkipModifierBlock(ref iterator, endOfParam);  }
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


				//
				// Pascal-Style Parameters
				//

				else if (parameters.ParameterStyle == ParsedPrototype.ParameterStyle.Pascal)
					{
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
							{  SkipModifierBlock(ref iterator, endOfParam);  }
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
							{  SkipModifierBlock(ref iterator, endOfParam);  }
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
							{  SkipModifierBlock(ref iterator, endOfParam);  }
						else if (type == PrototypeParsingType.StartOfTuple)
							{  SkipTuple(ref iterator, endOfParam);  }
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

				else
					{  throw new NotImplementedException();  }
				}

			// Calculate the columns now that we're done
			columns = new PrototypeColumnLayout(parsedPrototype, parameters, cells);
			}


		/* Function: SkipModifierBlock
		 * If the iterator is on a <PrototypeParsingType.OpeningTypeModifier> or <PrototypeParsingType.OpeningParamModifier>
		 * token, moves the token iterator past the entire block, including any nested blocks.
		 */
		protected void SkipModifierBlock (ref TokenIterator iterator, TokenIterator limit)
			{
			if (iterator < limit &&
				(iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
				 iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier))
				{
				int level = 1;
				iterator.Next();

				while (iterator < limit && level > 0)
					{
					if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
						iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier)
						{  level++;  }
					else if (iterator.PrototypeParsingType == PrototypeParsingType.ClosingTypeModifier ||
							   iterator.PrototypeParsingType == PrototypeParsingType.ClosingParamModifier)
						{  level--;  }

					iterator.Next();
					}
				}
			}


		/* Function: SkipTuple
		 * If the iterator is on a <PrototypeParsingType.StartOfTuple> token, moves the token iterator past the entire tuple,
		 * including any nested tuples.
		 */
		protected void SkipTuple (ref TokenIterator iterator, TokenIterator limit)
			{
			if (iterator < limit &&
				iterator.PrototypeParsingType == PrototypeParsingType.StartOfTuple)
				{
				int level = 1;
				iterator.Next();

				while (iterator < limit && level > 0)
					{
					if (iterator.PrototypeParsingType == PrototypeParsingType.StartOfTuple)
						{  level++;  }
					else if (iterator.PrototypeParsingType == PrototypeParsingType.EndOfTuple)
						{  level--;  }

					iterator.Next();
					}
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: ParameterStyle
		 */
		public ParsedPrototype.ParameterStyle ParameterStyle
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


		/* Property: LastCellEndsWithSpace
		 * Whether the last column of the last parameter either doesn't have any content or its content is shorter than the
		 * column width, resulting in empty space between it and the part after the parameters.
		 */
		public bool LastCellEndsWithSpace
			{
			get
				{  return lastCellEndsWithSpace;  }
			set
				{  lastCellEndsWithSpace = value;  }
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

		/* var: lastCellEndsWithSpace
		 * Whether the last column of the last parameter either doesn't have any content or its content is shorter than the
		 * column width, resulting in empty space between it and the part after the parameters.
		 */
		protected bool lastCellEndsWithSpace;

		}
	}
