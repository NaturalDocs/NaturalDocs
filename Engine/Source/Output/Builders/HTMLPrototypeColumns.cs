/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTMLPrototypeColumns
 * ____________________________________________________________________________
 * 
 * A class that gathers information about a <ParsedPrototype> for use in formatting.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine.Tokenization;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public class HTMLPrototypeColumns
		{

		// Group: Types
		// __________________________________________________________________________

		/* Enum: ColumnType
		 * 
		 * The type of column it is.  Note that the prototype CSS classes are directly mapped to these names.
		 * 
		 * ModifierQualifier - For C-style prototypes, a separate column for modifiers and qualifiers.
		 * Type - The parameter type.  For C-style prototypes this will only be the last word.  For Pascal-style
		 *				  prototypes this will be the entire symbol.
		 * TypeNameSeparator - For Pascal-style prototypes, the symbol separating the name from the type.
		 * NamePrefix - A prefix for a parameter name that should be formatted with the name, such as * and &.
		 * Name - The parameter name.
		 * DefaultValueSeparator - If present, the symbol for assigning a default value like = or :=.
		 * DefaultValue - The default value.
		 */
		public enum ColumnType : byte
			{  
			ModifierQualifier = 0, 
			Type, 
			TypeNameSeparator, 
			
			NamePrefix, 
			Name, 
			
			DefaultValueSeparator, 
			DefaultValue
			}


		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: HTMLPrototypeColumns
		 */
		public HTMLPrototypeColumns (ParsedPrototype prototype)
			{
			this.prototype = prototype;

			if (prototype.CStyle)
				{  columnIndexes = new int[CColumnOrder.Length];  }
			else
				{  columnIndexes = new int[PascalColumnOrder.Length];  }

			calculatedParameterIndex = -1;
	 		}


		/* Function: CalculateParameterColumns
		 * Fills in <columnSymbolIndexes> for the passed parameter index.  If the parameter doesn't exist it will
		 * return false.
		 */
		protected bool CalculateParameterColumns (int parameterIndex)
			{
			TokenIterator startParam, endParam;

			if (prototype.GetParameter(parameterIndex, out startParam, out endParam) == false)
				{  return false;  }

			calculatedParameterIndex = parameterIndex;

			TokenIterator iterator = startParam;
			iterator.NextPastWhitespace(endParam);
			PrototypeParsingType type = iterator.PrototypeParsingType;

			if (prototype.CStyle)
				{

				// ModifierQualifier
				
				int currentColumn = 0;
				columnIndexes[currentColumn] = iterator.TokenIndex;

				// Null covers whitespace and any random symbols we encountered that went unmarked.
				while (iterator < endParam && 
							(type == PrototypeParsingType.TypeModifier ||
							 type == PrototypeParsingType.TypeQualifier ||
							 type == PrototypeParsingType.Null))
					{  
					iterator.Next();  
					type = iterator.PrototypeParsingType;
					}


				// Type

				currentColumn++;
				columnIndexes[currentColumn] = iterator.TokenIndex;

				int typeNesting = 0;

				while (iterator < endParam && 
							(type == PrototypeParsingType.Type ||
							 type == PrototypeParsingType.TypeSuffix ||
							 type == PrototypeParsingType.OpeningTypeSuffix ||
							 type == PrototypeParsingType.ClosingTypeSuffix ||
							 type == PrototypeParsingType.Null ||
							 typeNesting > 0))
					{  
					if (type == PrototypeParsingType.OpeningTypeSuffix)
						{  typeNesting++;  }
					else if (type == PrototypeParsingType.ClosingTypeSuffix)
						{  typeNesting--;  }

					iterator.Next();
					type = iterator.PrototypeParsingType;
					}


				// NamePrefix

				currentColumn++;
				columnIndexes[currentColumn] = iterator.TokenIndex;

				while (iterator < endParam && 
							type == PrototypeParsingType.NamePrefix_PartOfType)
					{  
					iterator.Next();  
					type = iterator.PrototypeParsingType;
					}


				// Name

				currentColumn++;
				columnIndexes[currentColumn] = iterator.TokenIndex;

				// Include the parameter separator because there may not be a default value
				while (iterator < endParam &&
							(type == PrototypeParsingType.Name||
							 type == PrototypeParsingType.NameSuffix_PartOfType||
							 type == PrototypeParsingType.ParamSeparator ||
							 type == PrototypeParsingType.Null))
					{  
					iterator.Next();  
					type = iterator.PrototypeParsingType;
					}


				// DefaultValueSeparator

				currentColumn++;
				columnIndexes[currentColumn] = iterator.TokenIndex;

				while (iterator < endParam && 
							type == PrototypeParsingType.DefaultValueSeparator)
					{  
					iterator.Next();  
					type = iterator.PrototypeParsingType;
					}


				// DefaultValue

				currentColumn++;
				columnIndexes[currentColumn] = iterator.TokenIndex;
				}

			else // prototype.PascalStyle
				{

				// Name

				int columnSymbolIndex = 0;
				columnIndexes[columnSymbolIndex] = iterator.TokenIndex;

				// Null covers whitespace and any random symbols we encountered that went unmarked.
				// Include the parameter separator because there may not be a type
				while (iterator < endParam && 
							(type == PrototypeParsingType.Name ||
							 type == PrototypeParsingType.NamePrefix_PartOfType||
							 type == PrototypeParsingType.NameSuffix_PartOfType ||
							 type == PrototypeParsingType.ParamSeparator ||
							 type == PrototypeParsingType.Null))
					{  
					iterator.Next();  
					type = iterator.PrototypeParsingType;
					}


				// TypeNameSeparator

				columnSymbolIndex++;
				columnIndexes[columnSymbolIndex] = iterator.TokenIndex;

				while (iterator < endParam && 
							type == PrototypeParsingType.NameTypeSeparator)
					{  
					iterator.Next();  
					type = iterator.PrototypeParsingType;
					}


				// Type

				columnSymbolIndex++;
				columnIndexes[columnSymbolIndex] = iterator.TokenIndex;

				int typeNesting = 0;

				// Include the parameter separator because there may not be a default value
				while (iterator < endParam && 
							(type == PrototypeParsingType.TypeModifier ||
							 type == PrototypeParsingType.TypeQualifier ||
							 type == PrototypeParsingType.Type ||
							 type == PrototypeParsingType.TypeSuffix ||
							 type == PrototypeParsingType.OpeningTypeSuffix ||
							 type == PrototypeParsingType.ClosingTypeSuffix ||
							 type == PrototypeParsingType.Null ||
							 typeNesting > 0))
					{  
					if (type == PrototypeParsingType.OpeningTypeSuffix)
						{  typeNesting++;  }
					else if (type == PrototypeParsingType.ClosingTypeSuffix)
						{  typeNesting--;  }

					iterator.Next();  
					type = iterator.PrototypeParsingType;
					}


				// DefaultValueSeparator

				columnSymbolIndex++;
				columnIndexes[columnSymbolIndex] = iterator.TokenIndex;

				while (iterator < endParam && 
							type == PrototypeParsingType.DefaultValueSeparator)
					{  
					iterator.Next();  
					type = iterator.PrototypeParsingType;
					}


				// DefaultValue

				columnSymbolIndex++;
				columnIndexes[columnSymbolIndex] = iterator.TokenIndex;
				}


			// End of parameter

			endOfColumnsIndex = endParam.TokenIndex;

			return true;
			}


		/* Function: GetColumn
		 * 
		 * Returns the bounds of a parameter's column and what type it is, which depends on whether it's a C-style or
		 * Pascal-style parameter.  Returns false if the parameter or column indexes are out of bounds, or if the contents
		 * are empty for that particular slot.
		 * 
		 * You can retrieve the order in which columns will appear with <ColumnOrder>.
		 */
		public bool GetColumn (int parameterIndex, int columnIndex, out TokenIterator start, out TokenIterator end, 
													 out ColumnType type)
			{
			if (calculatedParameterIndex != parameterIndex)
				{  
				if (CalculateParameterColumns(parameterIndex) == false)
					{
					start = prototype.Tokenizer.LastToken;
					end = prototype.Tokenizer.LastToken;
					type = ColumnType.Name;
					return false;
					}
				}

			if (columnIndex >= columnIndexes.Length)
				{ 
				start = prototype.Tokenizer.LastToken;
				end = prototype.Tokenizer.LastToken;
				type = ColumnType.Name;
				return false;
				}

			int startIndex = columnIndexes[columnIndex];
			int endIndex = (columnIndex + 1 >= columnIndexes.Length ? endOfColumnsIndex : columnIndexes[columnIndex + 1]);

			start = prototype.Tokenizer.FirstToken;

			if (startIndex > 0)
				{  start.Next(startIndex);  }

			end = start;

			if (endIndex > startIndex)
				{  end.Next(endIndex - startIndex);  }

			type = ColumnOrder[columnIndex];

			end.PreviousPastWhitespace(start);
			start.NextPastWhitespace(end);

			return (end > start);
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: NumberOfColumns
		 * The number of columns in the prototype.
		 */
		public int NumberOfColumns
			{
			get
				{  return ColumnOrder.Length;  }
			}


		/* Property: ColumnOrder
		 * Returns an array of <ColumnTypes> representing the order in which <GetColumn()> will return them.
		 * Do not alter the contents.
		 */
		public ColumnType[] ColumnOrder
			{
			get
				{
				if (prototype.CStyle)
					{  return CColumnOrder;  }
				else
					{  return PascalColumnOrder;  }
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: prototype
		 */
		protected ParsedPrototype prototype;

		/* var: columnIndexes
		 * An array of symbol indexes representing the starting position of each entry in <CColumnOrder> or 
		 * <PascalColumnOrder>, plus one index to represent the end of the last one.  The indexes are taken from 
		 * <TokenIterator.TokenIndex> and so are relative to the start of <prototype> rather than the parameter.
		 */
		protected int[] columnIndexes;

		/* var: endOfColumnsIndex
		 * The symbol index of the end of the last column in <columnIndexes>.
		 */
		protected int endOfColumnsIndex;

		/* var: calculatedParameterIndex
		 * The parameter <columnIndexes> is calculated for, or -1 if none.
		 */
		protected int calculatedParameterIndex;

		/* var: CColumnOrder
		 * An array of <ColumnTypes> representing the order in which columns should appear for C-style prototypes.
		 */
		static public ColumnType[] CColumnOrder = { ColumnType.ModifierQualifier,
																						  ColumnType.Type,
																						  ColumnType.NamePrefix,
																						  ColumnType.Name,
																						  ColumnType.DefaultValueSeparator,
																						  ColumnType.DefaultValue };

		/* var: PascalColumnOrder
		 * An array of <ColumnTypes> representing the order in which columns should appear for Pascal-style prototypes.
		 */
		static public ColumnType[] PascalColumnOrder = { ColumnType.Name,
																								  ColumnType.TypeNameSeparator,
																								  ColumnType.Type,
																								  ColumnType.DefaultValueSeparator,
																								  ColumnType.DefaultValue };


		}
	}

