/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Components.HTMLPrototype
 * ____________________________________________________________________________
 * 
 * A reusable helper class to build prototypes for <Output.Builders.HTML>.
 * 
 * Why a Separate Class?:
 * 
 *		Well, building a HTML prototype isn't straightforward.  There's a lot of parsing stages and this keeps them separate
 *		and organized.  More importantly, the parsing stages have to pass a lot of data between them.  You can't store it
 *		in instance variables in <Builders.HTML> because multiple threads may be calling the build functions at the same time.
 *		Passing the structures around individually as function parameters quickly becomes unwieldy.  If you bundle them into
 *		a single object to pass around, well then you've already introduced a separate object allocation just for prototype
 *		parsing so you might as well move the functions in there to make it cleaner, right?
 * 
 * 
 * Topic: Usage
 *		
 *		- Create a HTMLPrototype object.
 *		- Call <Build()>.
 *		- The object can be reused on different prototypes by calling <Build()> again as long as they come from the same
 *		  <HTMLTopicPage>.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.  It has an internal state that is used during a call to
 *		<Build()>, and another <Build()> should not be started until it's completed.  Instead each thread should create its 
 *		own object.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output.Components
	{
	public class HTMLPrototype : HTMLComponent
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: ColumnType
		 * 
		 * A prototype's parameter column type.  Note that the prototype CSS classes are directly mapped to these 
		 * names.
		 * 
		 * ModifierQualifier - For C-style prototypes, a separate column for modifiers and qualifiers.  For Pascal-style
		 *							  prototypes, any modifiers that appear before the name.
		 * Type - The parameter type.  For C-style prototypes this will only be the last word.  For Pascal-style
		 *			  prototypes this will be the entire symbol.
		 * TypeNameSeparator - For Pascal-style prototypes, the symbol separating the name from the type.
		 * Symbols - Symbols between names and types that should be formatted in a separate column, such as * 
		 *				   and &.
		 * Name - The parameter name.
		 * DefaultValueSeparator - If present, the symbol for assigning a default value like = or :=.
		 * DefaultValue - The default value.
		 * PropertyValueSeparator - If present, the symbol for assigning a value to a property like = or :.
		 * PropertyValue - The property value, such as could appear in Java annotations.
		 */
		public enum ColumnType : byte
			{  
			ModifierQualifier, Type, TypeNameSeparator, 
			Symbols, Name, 
			DefaultValueSeparator, DefaultValue,
			PropertyValueSeparator, PropertyValue
			}



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: HTMLPrototype
		 */
		public HTMLPrototype (HTMLTopicPage topicPage) : base (topicPage)
			{
			parsedPrototype = null;
			language = null;
			parameterTableSection = null;
			parameterTableTokenIndexes = null;
			parameterTableColumnsUsed = null;
			symbolColumnWidth = 0;
			addLinks = false;
			}


		/* Function: Build
		 * 
		 * Builds the HTML for the <Topic's> prototype and returns it as a string.  If the string is going to be appended to
		 * a StringBuilder, it is more efficient to use the other function.
		 * 
		 * In order to have type links, links must be specified and contain any links that appear in the prototype.
		 * linkTargets must also be specified and contain the target <Topics> of all links.  If you do not need type
		 * links, set both to null.
		 */
		public string Build (Topic topic, IList<Link> links, IList<Topic> linkTargets)
			{
			StringBuilder output = new StringBuilder();
			Build(topic, links, linkTargets, output);
			return output.ToString();
			}


		/* Function: Build
		 * 
		 * Builds the HTML for the <Topic's> prototype and appends it to the passed StringBuilder.
		 * 
		 * In order to have type links, links must be specified and contain any links that appear in the prototype.
		 * linkTargets must also be specified and contain the target <Topics> of all links.  If you do not need type
		 * links, set both to null.
		 */
		public void Build (Topic topic, IList<Link> links, IList<Topic> linkTargets, StringBuilder output)
			{
			this.topic = topic;
			this.addLinks = (links != null && linkTargets != null);
			this.links = links;
			this.linkTargets = linkTargets;
			htmlOutput = output;

			language = EngineInstance.Languages.FromID(topic.LanguageID);
			parsedPrototype = topic.ParsedPrototype;

			if (parsedPrototype.Tokenizer.HasSyntaxHighlighting == false)
				{  language.SyntaxHighlight(parsedPrototype);  }

			// Determine if there's parameters anywhere.  It's possible for parsedPrototype.NumberOfParameters to be zero and 
			// there still be a parameters section present somewhere such as for SQL's return table definitions.
			bool hasParameters = false;

			foreach (var section in parsedPrototype.Sections)
				{
				if (section is Prototypes.ParameterSection && (section as Prototypes.ParameterSection).NumberOfParameters > 0)
					{  
					hasParameters = true;
					break;
					}
				}

			// We always build the wide form by default, but only include the attribute if there's parameters.  The JavaScript assumes
			// there will be a parameters section in any prototype that has it.
			htmlOutput.Append("<div id=\"NDPrototype" + topic.TopicID + "\" class=\"NDPrototype" +
										(hasParameters ? " WideForm" : "") + "\">");

			foreach (var section in parsedPrototype.Sections)
				{
				if (section is Prototypes.ParameterSection && (section as Prototypes.ParameterSection).NumberOfParameters > 0)
					{  BuildParameterSection((Prototypes.ParameterSection)section);  }
				else
					{  BuildPlainSection(section);  }
				}

			htmlOutput.Append("</div>");
			}


		/* Function: CalculateParameterTable
		 * Fills in <parameterTableTokenIndexes>, <parameterTableColumnUsed>, and <symbolColumnWidth> for the
		 * passed section.
		 */
		protected void CalculateParameterTable (Prototypes.ParameterSection section)
			{
			
			//
			// Check if this is a raw section with nothing other than parameter separators, meaning no name, type, etc. tokens
			//

			TokenIterator iterator = section.Start;
			TokenIterator endOfSection = section.End;
			bool isRaw= true;

			while (iterator < endOfSection)
				{
				var type = iterator.PrototypeParsingType;

				if (type != PrototypeParsingType.Null &&
					type != PrototypeParsingType.StartOfPrototypeSection &&
					type != PrototypeParsingType.StartOfParams &&
					type != PrototypeParsingType.ParamSeparator &&
					type != PrototypeParsingType.EndOfParams &&
					type != PrototypeParsingType.EndOfPrototypeSection)
					{
					isRaw = false;
					break;
					}

				iterator.Next();
				}


			//
			// Now fill in parameterTableTokenIndexes
			//

			parameterTableTokenIndexes = new int[section.NumberOfParameters, NumberOfColumns + 1];

			for (int parameterIndex = 0; parameterIndex < section.NumberOfParameters; parameterIndex++)
				{
				TokenIterator startOfParam, endOfParam;
				section.GetParameterBounds(parameterIndex, out startOfParam, out endOfParam);

				iterator = startOfParam;
				iterator.NextPastWhitespace(endOfParam);


				// C-Style Parameters

				if (section.ParameterStyle == ParsedPrototype.ParameterStyle.C)
					{
					while (iterator < endOfParam && 
							  iterator.PrototypeParsingType == PrototypeParsingType.Null &&
							  iterator.FundamentalType == FundamentalType.Whitespace)
						{  iterator.Next();  }


					// ModifierQualifier
				
					int currentColumn = 0;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					TokenIterator startOfType = iterator;

					if (!isRaw)
						{
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
						}


					// Type

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					// Allow this column to claim the contents of a raw prototype.  They should all be null tokens.
					// We use the type column instead of the name column because the name column isn't fully syntax highlighted.
					while (iterator < endOfParam)
						{  
						PrototypeParsingType type = iterator.PrototypeParsingType;

						// The previous loop already got any modifiers before the type, so this will only cover the type
						// plus any modifiers following it.
						if (type == PrototypeParsingType.Type ||
							type == PrototypeParsingType.TypeModifier ||
							type == PrototypeParsingType.ParamModifier ||
							(isRaw && type == PrototypeParsingType.ParamSeparator) ||
							type == PrototypeParsingType.Null)
							{  iterator.Next();   }
						else if (type == PrototypeParsingType.OpeningTypeModifier ||
									type == PrototypeParsingType.OpeningParamModifier)
							{  SkipModifierBlock(ref iterator, endOfParam);  }
						else
							{  break;  }						
						}


					// Symbols

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					if (!isRaw)
						{
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
								parameterTableTokenIndexes[parameterIndex, currentColumn] = lookbehind.TokenIndex;
								lookbehind.Previous();

								while (lookbehind >= startOfType)
									{
									if (lookbehind.FundamentalType == FundamentalType.Symbol &&
										lookbehind.Character != '_' &&
										lookbehind.PrototypeParsingType != PrototypeParsingType.ClosingTypeModifier &&
										lookbehind.PrototypeParsingType != PrototypeParsingType.ClosingParamModifier)
										{
										parameterTableTokenIndexes[parameterIndex, currentColumn] = lookbehind.TokenIndex;
										lookbehind.Previous();
										}
									else
										{  break;  }
									}

								// Fix up any columns we stole from
								for (int i = 0; i < currentColumn; i++)
									{
									if (parameterTableTokenIndexes[parameterIndex, i] > parameterTableTokenIndexes[parameterIndex, currentColumn])
										{  parameterTableTokenIndexes[parameterIndex, i] = parameterTableTokenIndexes[parameterIndex, currentColumn];  }
									}
								}
							}
						}


					// Name

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					if (!isRaw)
						{
						while (iterator < endOfParam)
							{  
							PrototypeParsingType type = iterator.PrototypeParsingType;

							// Include the parameter separator because there may not be a default value.
							// Include modifiers because there still may be some after the name.
							if (type == PrototypeParsingType.Name ||
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
						}


					// PropertyValueSeparator

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					if (!isRaw)
						{
						while (iterator < endOfParam)
							{  
							PrototypeParsingType type = iterator.PrototypeParsingType;

							if (type == PrototypeParsingType.PropertyValueSeparator ||
								type == PrototypeParsingType.Null)
								{  iterator.Next();   }
							else
								{  break;  }						
							}
						}


					// PropertyValue

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					if (!isRaw)
						{
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
						}


					// DefaultValueSeparator

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					if (!isRaw)
						{
						while (iterator < endOfParam)
							{  
							PrototypeParsingType type = iterator.PrototypeParsingType;

							if (type == PrototypeParsingType.DefaultValueSeparator ||
								type == PrototypeParsingType.Null)
								{  iterator.Next();   }
							else
								{  break;  }						
							}
						}


					// DefaultValue

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;


					// End of param

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = endOfParam.TokenIndex;
					}


				// Pascal-Style Parameters

				else if (section.ParameterStyle == ParsedPrototype.ParameterStyle.Pascal)
					{
					while (iterator < endOfParam && 
							  iterator.PrototypeParsingType == PrototypeParsingType.Null &&
							  iterator.FundamentalType == FundamentalType.Whitespace)
						{  iterator.Next();  }


					// ModifierQualifier
				
					int currentColumn = 0;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					if (!isRaw)
						{
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
						}


					// Do we have a name-type separator?  We may not, such as for SQL.

					bool hasNameTypeSeparator = false;
					TokenIterator lookahead = iterator;

					if (!isRaw)
						{
						while (lookahead < endOfParam)
							{
							if (lookahead.PrototypeParsingType == PrototypeParsingType.NameTypeSeparator)
								{
								hasNameTypeSeparator = true;
								break;
								}

							lookahead.Next();
							}
						}


					// Name

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					if (!isRaw)
						{
						while (iterator < endOfParam)
							{  
							PrototypeParsingType type = iterator.PrototypeParsingType;

							// Include the parameter separator because there may not be a type.
							if (type == PrototypeParsingType.Name ||
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
						}


					// TypeNameSeparator

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					if (!isRaw)
						{
						while (iterator < endOfParam)
							{  
							PrototypeParsingType type = iterator.PrototypeParsingType;

							if (type == PrototypeParsingType.NameTypeSeparator ||
								type == PrototypeParsingType.Null)
								{  iterator.Next();   }
							else
								{  break;  }						
							}
						}


					// Symbols

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					if (!isRaw)
						{
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
						}


					// Type

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

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
						else
							{  break;  }						
						}


					// PropertyValueSeparator

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					if (!isRaw)
						{
						while (iterator < endOfParam)
							{  
							PrototypeParsingType type = iterator.PrototypeParsingType;

							if (type == PrototypeParsingType.PropertyValueSeparator ||
								type == PrototypeParsingType.Null)
								{  iterator.Next();   }
							else
								{  break;  }						
							}
						}


					// PropertyValue

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					if (!isRaw)
						{
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
						}


					// DefaultValueSeparator

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;

					if (!isRaw)
						{
						while (iterator < endOfParam)
							{  
							PrototypeParsingType type = iterator.PrototypeParsingType;

							if (type == PrototypeParsingType.DefaultValueSeparator ||
								type == PrototypeParsingType.Null)
								{  iterator.Next();   }
							else
								{  break;  }						
							}
						}


					// DefaultValue

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = iterator.TokenIndex;


					// End of param

					currentColumn++;
					parameterTableTokenIndexes[parameterIndex, currentColumn] = endOfParam.TokenIndex;
					}
				}



			//
			// Next fill in parameterTableColumnsUsed
			//

			// There's a very high likelihood of this array always being the same length so it's worth it to try to reuse the
			// memory and avoid a reallocation.
			if (parameterTableColumnsUsed != null &&
				parameterTableColumnsUsed.Length == NumberOfColumns)
				{  Array.Clear(parameterTableColumnsUsed, 0, NumberOfColumns);  }
			else
				{  parameterTableColumnsUsed = new bool[NumberOfColumns];  }

			for (int parameterIndex = 0; parameterIndex < section.NumberOfParameters; parameterIndex++)
				{
				for (int columnIndex = 0; columnIndex < NumberOfColumns; columnIndex++)
					{
					if (parameterTableTokenIndexes[parameterIndex, columnIndex] !=
						parameterTableTokenIndexes[parameterIndex, columnIndex + 1])
						{
						parameterTableColumnsUsed[columnIndex] = true;
						}
					}
				}



			//
			// Next determine the symbol column's width
			//

			symbolColumnWidth = 0;

			if (parameterTableColumnsUsed[SymbolsColumnIndex])
				{
				for (int parameterIndex = 0; parameterIndex < section.NumberOfParameters; parameterIndex++)
					{
					int startTokenIndex = parameterTableTokenIndexes[parameterIndex, SymbolsColumnIndex];
					int endTokenIndex = parameterTableTokenIndexes[parameterIndex, SymbolsColumnIndex + 1];

					if (endTokenIndex > startTokenIndex)
						{
						TokenIterator start, end;
						section.GetParameterBounds(parameterIndex, out start, out end);

						start.Next(startTokenIndex - start.TokenIndex);
						end.Previous(end.TokenIndex - endTokenIndex);

						int paramColumnWidth = end.RawTextIndex - start.RawTextIndex;

						if (paramColumnWidth > symbolColumnWidth)
							{  symbolColumnWidth = paramColumnWidth;  }
						}
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


		/* Function: BuildPlainSection
		 */
		protected void BuildPlainSection (Prototypes.Section section)
			{
			htmlOutput.Append("<div class=\"PSection PPlainSection\">");

			if (addLinks)
				{  BuildTypeLinkedAndSyntaxHighlightedText(section.Start, section.End);  }
			else
				{  BuildSyntaxHighlightedText(section.Start, section.End);  }

			htmlOutput.Append("</div>");
			}


		/* Function: BuildParameterSection
		 * Builds the HTML for a <Prototypes.ParameterSection>.  It will always be in wide form.
		 */
		protected void BuildParameterSection (Prototypes.ParameterSection parameterTableSection)
			{
			this.parameterTableSection = parameterTableSection;

			string parameterClass;

			switch (parameterTableSection.ParameterStyle)
				{
				case ParsedPrototype.ParameterStyle.C:
					parameterClass = "CStyle";
					break;
				case ParsedPrototype.ParameterStyle.Pascal:
					parameterClass = "PascalStyle";
					break;
				default:
					throw new NotImplementedException();
				}

			htmlOutput.Append("<div class=\"PSection PParameterSection " + parameterClass + "\">");

			htmlOutput.Append("<table><tr>");

			TokenIterator start, end;
			parameterTableSection.GetBeforeParameters(out start, out end);

			htmlOutput.Append("<td class=\"PBeforeParameters\">");

			bool addNBSP = end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

			if (addLinks)
				{ BuildTypeLinkedAndSyntaxHighlightedText(start, end); }
			else
				{ BuildSyntaxHighlightedText(start, end); }

			if (addNBSP)
				{  htmlOutput.Append("&nbsp;");  }

			htmlOutput.Append("</td>");

			htmlOutput.Append("<td class=\"PParametersParentCell\">");
			BuildParameterTable();
			htmlOutput.Append("</td>");

			parameterTableSection.GetAfterParameters(out start, out end);

			htmlOutput.Append("<td class=\"PAfterParameters\">");

			if (start.NextPastWhitespace(end))
				{  htmlOutput.Append("&nbsp;");  };

			if (addLinks)
				{ BuildTypeLinkedAndSyntaxHighlightedText(start, end); }
			else
				{ BuildSyntaxHighlightedText(start, end); }

			htmlOutput.Append("</td></tr></table>");

			htmlOutput.Append("</div>");
			}


		/* Function: BuildParameterTable
		 */
		protected void BuildParameterTable ()
			{
			CalculateParameterTable(parameterTableSection);

			int firstUsedCell = 0;
			while (firstUsedCell < NumberOfColumns && parameterTableColumnsUsed[firstUsedCell] == false)
				{  firstUsedCell++;  }

			int lastUsedCell = NumberOfColumns - 1;
			while (lastUsedCell > 0 && parameterTableColumnsUsed[lastUsedCell] == false)
				{  lastUsedCell--;  }

			htmlOutput.Append("<table class=\"PParameters\">");

			for (int parameterIndex = 0; parameterIndex < parameterTableSection.NumberOfParameters; parameterIndex++)
				{
				htmlOutput.Append("<tr>");

				for (int cellIndex = firstUsedCell; cellIndex <= lastUsedCell; cellIndex++)
					{
					if (parameterTableColumnsUsed[cellIndex])
						{
						string extraClass = null;

						if (cellIndex == firstUsedCell && cellIndex == lastUsedCell)
							{  extraClass = "first last";  }
						else if (cellIndex == firstUsedCell)
							{  extraClass = "first";  }
						else if (cellIndex == lastUsedCell)
							{  extraClass = "last";  }

						if (parameterTableTokenIndexes[parameterIndex, cellIndex] == parameterTableTokenIndexes[parameterIndex, cellIndex + 1])
							{  
							if (extraClass == null)
								{  htmlOutput.Append("<td></td>");  }
							else
								{  htmlOutput.Append("<td class=\"" + extraClass + "\"></td>");  }
							}
						else
							{
							htmlOutput.Append("<td class=\"P" + ColumnOrder[cellIndex].ToString() + (extraClass != null ? ' ' + extraClass : "") + "\">");

							BuildCellContents(parameterIndex, cellIndex);

							htmlOutput.Append("</td>");
							}
						}
					}

				htmlOutput.Append("</tr>");
				}

			htmlOutput.Append("</table>");
			}


		/* Function: BuildCellContents
		 */
		protected void BuildCellContents (int parameterIndex, int cellIndex)
			{
			TokenIterator start = parameterTableSection.Start;
			start.Next(parameterTableTokenIndexes[parameterIndex, cellIndex] - start.TokenIndex);

			TokenIterator end = start;
			end.Next(parameterTableTokenIndexes[parameterIndex, cellIndex + 1] - end.TokenIndex);

			bool hadTrailingWhitespace = end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

			ColumnType type = ColumnOrder[cellIndex];

			// Find the type of the next used cell
			ColumnType? nextType = null;
			for (int nextCellIndex = cellIndex + 1; nextCellIndex < NumberOfColumns; nextCellIndex++)
				{
				if (parameterTableColumnsUsed[nextCellIndex])
					{
					nextType = ColumnOrder[nextCellIndex];
					break;
					}
				}
			
			// Default value separators always get spaces before.
			// Property value separators get them unless they're ":", but watch out for ":=".
			// Type-name separators get them if they're text (SQL's "AS") instead of symbols (Pascal's ":").
			if (type == ColumnType.DefaultValueSeparator ||
				(type == ColumnType.PropertyValueSeparator && (start.Character != ':' || start.MatchesAcrossTokens(":="))) ||
				(type == ColumnType.TypeNameSeparator && start.FundamentalType == FundamentalType.Text))
				{  htmlOutput.Append("&nbsp;");  }

			// We don't want to highlight keywords on the Name cell because identifiers can accidentally be marked as them with
			// basic language support, such as "event" in "wxPaintEvent &event".
			if (type == ColumnType.Name)
				{  BuildSyntaxHighlightedText(start, end, htmlOutput, excludeKeywords: true);  }
			else if (addLinks)
				{  BuildTypeLinkedAndSyntaxHighlightedText(start, end, true, htmlOutput);  }
			else
				{  BuildSyntaxHighlightedText(start, end, htmlOutput);  }

			// Default value separators, property value separators, and type/name separators always get spaces after.  Make sure 
			// the spaces aren't duplicated by the preceding cells.
			if (type == ColumnType.DefaultValueSeparator ||
				type == ColumnType.PropertyValueSeparator ||
				type == ColumnType.TypeNameSeparator ||
				(hadTrailingWhitespace && 
					type != ColumnType.DefaultValue &&
					nextType != ColumnType.DefaultValueSeparator && 
					nextType != ColumnType.PropertyValueSeparator && 
					nextType != ColumnType.TypeNameSeparator) )
				{  htmlOutput.Append("&nbsp;");  }
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: NumberOfColumns
		 * Returns the number of columns used in the parameter style.
		 */
		protected int NumberOfColumns
			{
			get
				{  return ColumnOrder.Length;  }
			}


		/* Property: ColumnOrder
		 * Returns the column order array appropriate for the parameter style.  Do not change the data.
		 */
		protected ColumnType[] ColumnOrder
			{
			get
				{
				switch (parameterTableSection.ParameterStyle)
					{
					case ParsedPrototype.ParameterStyle.C:
						return CColumnOrder;
					case ParsedPrototype.ParameterStyle.Pascal:
						return PascalColumnOrder;
					default:
						throw new NotSupportedException();
					}
				}
			}


		/* Property: SymbolsColumnIndex
		 * The index into <ColumnOrder> where <ColumnType.Symbols> appears.
		 */
		protected int SymbolsColumnIndex
			{
			get
				{
				switch (parameterTableSection.ParameterStyle)
					{
					case ParsedPrototype.ParameterStyle.C:
						return CSymbolsColumnIndex;
					case ParsedPrototype.ParameterStyle.Pascal:
						return PascalSymbolsColumnIndex;
					default:
						throw new NotSupportedException();
					}
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: parsedPrototype
		 * The prototype as a <ParsedPrototype> object.
		 */
		protected ParsedPrototype parsedPrototype;

		/* var: language
		 * The <Languages.Language> of the prototype.
		 */
		protected Languages.Language language;

		/* var: parameterTableSection
		 * The <Prototypes.ParameterSection> currently being processed.
		 */
		protected Prototypes.ParameterSection parameterTableSection;

		/* var: parameterTableTokenIndexes
		 * A table representing the <parameterTableSection> as rows and the columns determined by <ColumnOrder>.
		 * Each value represents the starting token index of that cell.  Each row will also contain one extra value 
		 * representing the token index of the end of the final cell.
		 */
		protected int[,] parameterTableTokenIndexes;

		/* var: parameterTableColumnsUsed
		 * An array representing whether each column in <parameterTableTokenIndexes> is used at all.
		 */
		protected bool[] parameterTableColumnsUsed;

		/* var: symbolColumnWidth
		 * The width in characters of the longest entry in the symbol column, if any.  Will be zero if it's not used.
		 */
		protected int symbolColumnWidth;

		/* var: addLinks
		 * Whether to add type links to the prototype.
		 */
		protected bool addLinks;



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: CColumnOrder
		 * An array of <ColumnTypes> representing the order in which columns should appear for C-style prototypes.
		 */
		static public ColumnType[] CColumnOrder = { ColumnType.ModifierQualifier,
																		   ColumnType.Type,
																		   ColumnType.Symbols,
																		   ColumnType.Name,
																		   ColumnType.PropertyValueSeparator,
																		   ColumnType.PropertyValue,
																		   ColumnType.DefaultValueSeparator,
																		   ColumnType.DefaultValue };

		/* var: CSymbolsColumnIndex
		 * The index into <CColumnOrder> where <ColumnType.Symbols> appears.
		 */
		static public int CSymbolsColumnIndex = 2;

		/* var: PascalColumnOrder
		 * An array of <ColumnTypes> representing the order in which columns should appear for Pascal-style prototypes.
		 */
		static public ColumnType[] PascalColumnOrder = { ColumnType.ModifierQualifier,
																				  ColumnType.Name,
																				  ColumnType.TypeNameSeparator,
																				  ColumnType.Symbols,
																				  ColumnType.Type,
																				  ColumnType.PropertyValueSeparator,
																				  ColumnType.PropertyValue,
																				  ColumnType.DefaultValueSeparator,
																				  ColumnType.DefaultValue };

		/* var: PascalSymbolsColumnIndex
		 * The index into <PascalColumnOrder> where <ColumnType.Symbols> appears.
		 */
		static public int PascalSymbolsColumnIndex = 3;

		}
	}

