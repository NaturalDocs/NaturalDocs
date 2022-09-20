/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeParameters
 * ____________________________________________________________________________
 *
 * A class for storing information about a single <Prototypes.ParameterSection>.
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
	public class PrototypeParameters
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: PrototypeParameters
		 */
		public PrototypeParameters (ParsedPrototype parsedPrototype, Prototypes.ParameterSection parameterSection)
			{
			this.parsedPrototype = parsedPrototype;
			this.parameterSection = parameterSection;

			tokenIndexes = null;

			CalculateTokenIndexes();

			columns = new PrototypeColumns(parsedPrototype, parameterSection, tokenIndexes);
			}


		/* Function: HasContentAt
		 * Whether there is content at the specified parameter and column.
		 */
		public bool HasContentAt (int parameterIndex, int columnIndex)
			{
			if (parameterIndex <= Count && columnIndex <= columns.Count)
				{
				return ( tokenIndexes[parameterIndex, columnIndex + 1] > tokenIndexes[parameterIndex, columnIndex] );
				}
			else
				{  throw new IndexOutOfRangeException();  }
			}


		/* Function: GetContentAt
		 * Returns the content at a specific parameter and column, or false if there wasn't any.
		 */
		public bool GetContentAt (int parameterIndex, int columnIndex, out TokenIterator start, out TokenIterator end)
			{
			if (parameterIndex <= Count && columnIndex <= columns.Count)
				{
				int startingTokenIndex = tokenIndexes[parameterIndex, columnIndex];

				start = parsedPrototype.Tokenizer.FirstToken;
				start.Next( startingTokenIndex - start.TokenIndex );

				int endingTokenIndex = tokenIndexes[parameterIndex, columnIndex + 1];

				end = start;
				end.Next( endingTokenIndex - end.TokenIndex );

				return (start < end);
				}
			else
				{  throw new IndexOutOfRangeException();  }
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: CalculateTokenIndexes
		 * Creates and fills in <tokenIndexes>.
		 */
		protected void CalculateTokenIndexes ()
			{
			// DEPENDENCY: This code depends on the order of PrototypeColumns.CColumnOrder and PascalColumnOrder.

			tokenIndexes = new int[parameterSection.NumberOfParameters, PrototypeColumns.CountOf(parameterSection.ParameterStyle) + 1];

			for (int parameterIndex = 0; parameterIndex < parameterSection.NumberOfParameters; parameterIndex++)
				{
				TokenIterator startOfParam, endOfParam;
				parameterSection.GetParameterBounds(parameterIndex, out startOfParam, out endOfParam);

				TokenIterator iterator = startOfParam;
				iterator.NextPastWhitespace(endOfParam);


				// C-Style Parameters

				if (parameterSection.ParameterStyle == ParsedPrototype.ParameterStyle.C)
					{
					while (iterator < endOfParam &&
							  iterator.PrototypeParsingType == PrototypeParsingType.Null &&
							  iterator.FundamentalType == FundamentalType.Whitespace)
						{  iterator.Next();  }


					// ModifierQualifier

					int currentColumn = 0;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

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


					// Type

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

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


					// Symbols

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					// All symbols are part of the type column right now because they're marked as type or param
					// modifiers.  Walk backwards to claim the symbols from the type column.

					if (iterator > startOfType)
						{
						TokenIterator lookbehind = iterator;
						lookbehind.Previous();

						if (lookbehind.FundamentalType == FundamentalType.Symbol &&
							lookbehind.Character != '_' &&
							lookbehind.PrototypeParsingType != PrototypeParsingType.ClosingTypeModifier &&
							lookbehind.PrototypeParsingType != PrototypeParsingType.ClosingParamModifier)
							{
							tokenIndexes[parameterIndex, currentColumn] = lookbehind.TokenIndex;
							lookbehind.Previous();

							while (lookbehind >= startOfType)
								{
								if (lookbehind.FundamentalType == FundamentalType.Symbol &&
									lookbehind.Character != '_' &&
									lookbehind.PrototypeParsingType != PrototypeParsingType.ClosingTypeModifier &&
									lookbehind.PrototypeParsingType != PrototypeParsingType.ClosingParamModifier)
									{
									tokenIndexes[parameterIndex, currentColumn] = lookbehind.TokenIndex;
									lookbehind.Previous();
									}
								else
									{  break;  }
								}

							// Fix up any columns we stole from
							for (int i = 0; i < currentColumn; i++)
								{
								if (tokenIndexes[parameterIndex, i] > tokenIndexes[parameterIndex, currentColumn])
									{  tokenIndexes[parameterIndex, i] = tokenIndexes[parameterIndex, currentColumn];  }
								}
							}
						}


					// Name

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

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


					// PropertyValueSeparator

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					while (iterator < endOfParam)
						{
						PrototypeParsingType type = iterator.PrototypeParsingType;

						if (type == PrototypeParsingType.PropertyValueSeparator ||
							type == PrototypeParsingType.Null)
							{  iterator.Next();   }
						else
							{  break;  }
						}


					// PropertyValue

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

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


					// DefaultValueSeparator

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					while (iterator < endOfParam)
						{
						PrototypeParsingType type = iterator.PrototypeParsingType;

						if (type == PrototypeParsingType.DefaultValueSeparator ||
							type == PrototypeParsingType.Null)
							{  iterator.Next();   }
						else
							{  break;  }
						}


					// DefaultValue

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;


					// End of param

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = endOfParam.TokenIndex;
					}


				// Pascal-Style Parameters

				else if (parameterSection.ParameterStyle == ParsedPrototype.ParameterStyle.Pascal)
					{
					while (iterator < endOfParam &&
							  iterator.PrototypeParsingType == PrototypeParsingType.Null &&
							  iterator.FundamentalType == FundamentalType.Whitespace)
						{  iterator.Next();  }


					// ModifierQualifier

					int currentColumn = 0;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

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


					// Name

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

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


					// TypeNameSeparator

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					while (iterator < endOfParam)
						{
						PrototypeParsingType type = iterator.PrototypeParsingType;

						if (type == PrototypeParsingType.NameTypeSeparator ||
							type == PrototypeParsingType.Null)
							{  iterator.Next();   }
						else
							{  break;  }
						}


					// Symbols

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

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


					// Type

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

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


					// PropertyValueSeparator

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					while (iterator < endOfParam)
						{
						PrototypeParsingType type = iterator.PrototypeParsingType;

						if (type == PrototypeParsingType.PropertyValueSeparator ||
							type == PrototypeParsingType.Null)
							{  iterator.Next();   }
						else
							{  break;  }
						}


					// PropertyValue

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

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


					// DefaultValueSeparator

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					while (iterator < endOfParam)
						{
						PrototypeParsingType type = iterator.PrototypeParsingType;

						if (type == PrototypeParsingType.DefaultValueSeparator ||
							type == PrototypeParsingType.Null)
							{  iterator.Next();   }
						else
							{  break;  }
						}


					// DefaultValue

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;


					// End of param

					currentColumn++;
					tokenIndexes[parameterIndex, currentColumn] = endOfParam.TokenIndex;
					}
				}
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


		/* Property: Count
		 * The number of parameters.
		 */
		public int Count
			{
			get
				{  return parameterSection.NumberOfParameters;  }
			}


		/* Property: Columns
		 * Information about the columns across all parameters.
		 */
		public PrototypeColumns Columns
			{
			get
				{  return columns;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: parsedPrototype
		 * The prototype as a <ParsedPrototype> object.
		 */
		protected ParsedPrototype parsedPrototype;

		/* var: parameterSection
		 * The <Prototypes.ParameterSection> being represented.
		 */
		protected Prototypes.ParameterSection parameterSection;

		/* var: tokenIndexes
		 * A table of token indexes representing the parameters.  There is one row per parameter, and each one is a series of
		 * token indexes representing the start of each column in either <PrototypeColumns.CColumnOrder> or
		 * <PrototypeColumns.PascalColumnOrder>.  If the column is empty it will be the same as the previous value.  Each
		 * column runs from its own index representing its start to the next column's index.  There will also be one extra
		 * column value per row representing the token index of the end of the final column.
		 */
		protected int[,] tokenIndexes;
		// DEPENDENCY: PrototypeColumns depends on the format of this table since it is passed to it.

		/* var: columns
		 * Information about the columns across all parameters.
		 */
		protected PrototypeColumns columns;

		}
	}
