/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeStyleFormatters.SystemVerilog
 * ____________________________________________________________________________
 *
 * A class to help format <ParameterStyle.SystemVerilog> parameters.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeStyleFormatters
	{
	public class SystemVerilog : PrototypeStyleFormatter
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: CalculateCells
		 * Takes a SystemVerilog <ParameterSection> and generates a table of <PrototypeCellLayouts>.  Each row represents
		 * a parameter, and each cell is a column in <ColumnOrder>.
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


				// Port Attributes

				int currentColumn = 0;
				TokenIterator startOfCell = iterator;
				TokenIterator startOfType = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					if (type == PrototypeParsingType.OpeningTypeModifier &&
						iterator.MatchesAcrossTokens("(*"))
						{  SkipModifierBlock(ref iterator, endOfParam);  }
					else if (type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else
						{  break;  }
					}

				TokenIterator endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// Direction or Parameter Keyword

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					// We put both direction keywords ("inout" etc.) and parameter keywords ("localparam" etc) in the same column.
					// We have to check for specific keywords because otherwise other type modifiers that appear without one of them
					// ("signed portA") would fall into it.
					if (type == PrototypeParsingType.TypeModifier &&
						 (Languages.Parsers.SystemVerilog.IsOnDirectionKeyword(iterator) ||
						  Languages.Parsers.SystemVerilog.IsOnParameterKeyword(iterator)) )
						{  iterator.Next();   }
					else if (type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else
						{  break;  }
					}

				endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// Type
				// Includes net type, qualifier, data type, signing

				// Why not a separate column for qualifier?  Because it appears between the net type and the data type, so you
				// would need separate net type, qualifier, and data type columns.  It's more common for module prototypes to
				// use a mix of ports declared as just "wire" and just "reg" than to use qualifiers, and if you put them in separate
				// columns they appear staggered.  We have to make the more common case look the best.

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					if (type == PrototypeParsingType.Type ||
						type == PrototypeParsingType.TypeModifier ||
						type == PrototypeParsingType.TypeQualifier ||
						type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else if (type == PrototypeParsingType.OpeningTypeModifier &&
							   (iterator.Character == '(' || iterator.MatchesAcrossTokens("#(") || iterator.Character == '{'))
						{  SkipModifierBlock(ref iterator, endOfParam);  }
					else
						{  break;  }
					}

				// If we ran into packed dimensions, see if there's an enum body following them.  Enums can have them on the
				// date type and/or the overall port ("enum bit [7:0] { val1, val2 } [7:0]") and we only want to put the latter
				// in the packed dimensions column.

				if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier &&
					iterator.Character == '[')
					{
					TokenIterator lookahead = iterator;
					SkipModifierBlock(ref lookahead, endOfParam);

					while (lookahead < endOfParam)
						{
						PrototypeParsingType type = lookahead.PrototypeParsingType;

						if (type == PrototypeParsingType.Null)
							{  lookahead.Next();   }
						else if (type == PrototypeParsingType.OpeningTypeModifier &&
								  lookahead.Character == '[')
							{  SkipModifierBlock(ref lookahead, endOfParam);  }
						else
							{  break;  }
						}

					// Found a body?
					if (lookahead.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier &&
						lookahead.Character == '{')
						{
						SkipModifierBlock(ref lookahead, endOfParam);

						while (lookahead < endOfParam &&
								 lookahead.PrototypeParsingType == PrototypeParsingType.Null)
							{  lookahead.Next();   }

						iterator = lookahead;
						}
					}

				endOfCell = iterator;

				cells[parameterIndex, currentColumn].StartingTextIndex = startOfCell.RawTextIndex;
				cells[parameterIndex, currentColumn].HasTrailingSpace = endOfCell.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfCell);
				cells[parameterIndex, currentColumn].EndingTextIndex = endOfCell.RawTextIndex;


				// Packed Dimensions

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					if (type == PrototypeParsingType.OpeningTypeModifier &&
						iterator.Character == '[')
						{  SkipModifierBlock(ref iterator, endOfParam);  }
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
				// Includes unpacked dimensions

				currentColumn++;
				startOfCell = iterator;

				while (iterator < endOfParam)
					{
					PrototypeParsingType type = iterator.PrototypeParsingType;

					// Include the parameter separator because there may not be a default value.
					if (type == PrototypeParsingType.Name ||
						type == PrototypeParsingType.KeywordName ||
						type == PrototypeParsingType.ParamModifier ||  // for the dot in port binding like .PortName(x)
						type == PrototypeParsingType.ParamSeparator ||
						type == PrototypeParsingType.Null)
						{  iterator.Next();   }
					else if (type == PrototypeParsingType.OpeningParamModifier)
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

			return cells;
			}


		/* Function: ColumnSpacingOf
		 * Returns the spacing of the passed column type.
		 */
		override public ColumnSpacing ColumnSpacingOf (PrototypeColumnType columnType)
			{
			switch (columnType)
				{
				case PrototypeColumnType.PortAttributes:
				case PrototypeColumnType.DirectionOrParameterKeyword:
				case PrototypeColumnType.PackedDimensions:
				case PrototypeColumnType.PropertyValueSeparator:
				case PrototypeColumnType.DefaultValueSeparator:
					return ColumnSpacing.AlwaysBoth;

				default:
					return ColumnSpacing.Normal;
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: ColumnOrder
		 * An array of <PrototypeColumnTypes> representing the order in which columns should appear for SystemVerilog prototypes.
		 */
		override public PrototypeColumnType[] ColumnOrder
			{
			get
				{  return ColumnOrderValues;  }
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: ColumnOrderValues
		 * An array of <PrototypeColumnTypes> representing the order in which columns should appear for SystemVerilog prototypes.
		 */
		readonly static public PrototypeColumnType[] ColumnOrderValues = { PrototypeColumnType.PortAttributes,
																											PrototypeColumnType.DirectionOrParameterKeyword,
																											PrototypeColumnType.Type,
																											PrototypeColumnType.PackedDimensions,
																											PrototypeColumnType.Name,
																											PrototypeColumnType.PropertyValueSeparator,
																											PrototypeColumnType.PropertyValue,
																											PrototypeColumnType.DefaultValueSeparator,
																											PrototypeColumnType.DefaultValue };
		}
	}
