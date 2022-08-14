/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.Prototype
 * ____________________________________________________________________________
 *
 * A reusable class for building HTML prototypes.
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
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class Prototype : HTML.Components.FormattedText
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


		/* Constructor: Prototype
		 */
		public Prototype (Context context) : base (context)
			{
			parsedPrototype = null;

			parameterTableSection = null;
			parameterTableTokenIndexes = null;
			parameterTableColumnsUsed = null;

			links = null;
			linkTargets = null;
			addLinks = false;
			}


		/* Function: BuildPrototype
		 *
		 * Builds the HTML for the passed prototype.
		 *
		 * In order to have type links, links must be specified and contain any links that appear in the prototype.
		 * linkTargets must also be specified and contain the target topics of all links.  If you do not need type
		 * links, set both to null.
		 *
		 * Requirements:
		 *
		 *		- The <Context>'s topic must be set.
		 *		- If type links are being generated, the <Context>'s page must also be set.
		 */
		public string BuildPrototype (ParsedPrototype parsedPrototype, Context context, IList<Link> links = null,
												  IList<Topics.Topic> linkTargets = null)
			{
			StringBuilder output = new StringBuilder();
			AppendPrototype(parsedPrototype, context, output, links, linkTargets);
			return output.ToString();
			}


		/* Function: AppendPrototype
		 *
		 * Builds the HTML for the passed prototype and appends it to the passed StringBuilder.
		 *
		 * In order to have type links, links must be specified and contain any links that appear in the prototype.
		 * linkTargets must also be specified and contain the target topics of all links.  If you do not need type
		 * links, set both to null.
		 *
		 * Requirements:
		 *
		 *		- The <Context>'s topic must be set.
		 *		- If type links are being generated, the <Context>'s page must also be set.
		 */
		public void AppendPrototype (ParsedPrototype parsedPrototype, Context context, StringBuilder output,
												   IList<Link> links = null, IList<Topics.Topic> linkTargets = null)
			{
			this.parsedPrototype = parsedPrototype;
			this.context = context;

			#if DEBUG
			if (context.Topic == null)
				{  throw new Exception("Tried to generate a prototype when the context's topic was not set.");  }
			#endif

			this.links = links;
			this.linkTargets = linkTargets;
			this.addLinks = (links != null && linkTargets != null);

			#if DEBUG
			if (this.addLinks && context.Page.IsNull)
				{  throw new Exception("Tried to generate a prototype with type links when the context's page was not set.");  }
			#endif

			if (parsedPrototype.Tokenizer.HasSyntaxHighlighting == false)
				{
				var language = EngineInstance.Languages.FromID(Context.Topic.LanguageID);
				language.Parser.SyntaxHighlight(parsedPrototype);
				}

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
			output.Append("<div id=\"NDPrototype" + Context.Topic.TopicID + "\" class=\"NDPrototype" +
								  (hasParameters ? " WideForm" : "") + "\">");

			foreach (var section in parsedPrototype.Sections)
				{
				if (section is Prototypes.ParameterSection && (section as Prototypes.ParameterSection).NumberOfParameters > 0)
					{  AppendParameterSection((Prototypes.ParameterSection)section, output);  }
				else
					{  AppendPlainSection(section, output);  }
				}

			output.Append("</div>");
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: CalculateParameterTable
		 * Fills in <parameterTableTokenIndexes> and <parameterTableColumnUsed> for the passed section.
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
						else if (type == PrototypeParsingType.StartOfTuple)
							{  SkipTuple(ref iterator, endOfParam);  }
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
						else if (type == PrototypeParsingType.StartOfTuple)
							{  SkipTuple(ref iterator, endOfParam);  }
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


		/* Function: AppendPlainSection
		 */
		protected void AppendPlainSection (Prototypes.Section section, StringBuilder output)
			{
			output.Append("<div class=\"PSection PPlainSection\">");
			AppendSectionText(section.Start, section.End, output, excludePartial: true);
			output.Append("</div>");
			}


		/* Function: AppendParameterSection
		 * Builds the HTML for a <Prototypes.ParameterSection>.  It will always be in wide form.
		 */
		protected void AppendParameterSection (Prototypes.ParameterSection parameterTableSection, StringBuilder output)
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

			output.Append("<div class=\"PSection PParameterSection " + parameterClass + "\">");


			// Before parameters

			TokenIterator start, end;
			parameterTableSection.GetBeforeParameters(out start, out end);

			// Add a &nbsp; if there was an ending whitespace character that was marked as part of the BeforeParameters section.
			// This should only happen if it was significant, it should have been excluded otherwise.
			bool addNBSP = end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

			// Also add it if the BeforeParameters section doesn't end with a nice symbol like (.  If it ends with something like (* we
			// want to add it anyway for legibility.  Also for {.
			if (!addNBSP)
				{
				TokenIterator beforeEnd = end;
				beforeEnd.Previous();

				addNBSP = (beforeEnd >= start &&
								  beforeEnd.Character != '(' &&
								  beforeEnd.Character != '[' &&
								  beforeEnd.Character != '<');
				}

			output.Append("<div class=\"PBeforeParameters\">");

			AppendSectionText(start, end, output, excludePartial: true);

			if (addNBSP)
				{  output.Append("&nbsp;");  }

			output.Append("</div>");


			// Parameters

			output.Append("<div class=\"PParametersParentCell\">");
			AppendParameterTable(output);
			output.Append("</div>");


			// After parameters

			parameterTableSection.GetAfterParameters(out start, out end);

			output.Append("<div class=\"PAfterParameters\">");

			// Add a &nbsp; if there was a leading whitespace character that was marked as part of the AfterParameters section.
			// This should only happen if it was significant, it should have been excluded otherwise.
			addNBSP = start.NextPastWhitespace(end);

			// Also add it if the AfterParameters section doesn't start with a nice symbol like ).  If it starts with something like *)
			// we want to add it anyway for legibility.
			if (!addNBSP)
				{
				addNBSP = (start < end &&
								  start.Character != ')' &&
								  start.Character != ']' &&
								  start.Character != '>');
				}

			if (addNBSP)
				{  output.Append("&nbsp;"); };

			AppendSectionText(start, end, output);

			output.Append("</div></div>");
			}


		/* Function: AppendSectionText
		 */
		protected void AppendSectionText (TokenIterator start, TokenIterator end, StringBuilder output, bool excludePartial = false)
			{
			TokenIterator partial;

			if (excludePartial &&
				start.Tokenizer.FindTokenBetween("partial", false, start, end, out partial) &&
				partial.IsStandaloneWord() &&
				partial.PrototypeParsingType == PrototypeParsingType.TypeModifier)
				{
				bool hasBeforePartial = (partial > start);
				bool hasSpaceBeforePartial = false;

				if (hasBeforePartial)
					{
					TokenIterator lookbehind = partial;
					hasSpaceBeforePartial = lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

					if (addLinks)
						{  AppendSyntaxHighlightedTextWithTypeLinks(start, lookbehind, output, links, linkTargets);  }
					else
						{  AppendSyntaxHighlightedText(start, lookbehind, output);  }
					}

				partial.Next();
				bool hasSpaceAfterPartial = partial.NextPastWhitespace();

				bool hasAfterPartial = (partial < end);

				if (hasAfterPartial)
					{
					if (hasBeforePartial && (hasSpaceBeforePartial || hasSpaceAfterPartial))
						{  output.Append(' ');  }

					if (addLinks)
						{  AppendSyntaxHighlightedTextWithTypeLinks(partial, end, output, links, linkTargets);  }
					else
						{  AppendSyntaxHighlightedText(partial, end, output);  }
					}
				}

			else // no partial
				{
				if (addLinks)
					{  AppendSyntaxHighlightedTextWithTypeLinks(start, end, output, links, linkTargets);  }
				else
					{  AppendSyntaxHighlightedText(start, end, output);  }
				}
			}


		/* Function: AppendParameterTable
		 */
		protected void AppendParameterTable (StringBuilder output)
			{
			CalculateParameterTable(parameterTableSection);

			int firstUsedCell = 0;
			while (firstUsedCell < NumberOfColumns && parameterTableColumnsUsed[firstUsedCell] == false)
				{  firstUsedCell++;  }

			int lastUsedCell = NumberOfColumns - 1;
			while (lastUsedCell > 0 && parameterTableColumnsUsed[lastUsedCell] == false)
				{  lastUsedCell--;  }

			output.Append("<table class=\"PParameters\">");

			for (int parameterIndex = 0; parameterIndex < parameterTableSection.NumberOfParameters; parameterIndex++)
				{
				output.Append("<tr>");

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
								{  output.Append("<td></td>");  }
							else
								{  output.Append("<td class=\"" + extraClass + "\"></td>");  }
							}
						else
							{
							output.Append("<td class=\"P" + ColumnOrder[cellIndex].ToString() + (extraClass != null ? ' ' + extraClass : "") + "\">");

							AppendCellContents(parameterIndex, cellIndex, output);

							output.Append("</td>");
							}
						}
					}

				output.Append("</tr>");
				}

			output.Append("</table>");
			}


		/* Function: AppendCellContents
		 */
		protected void AppendCellContents (int parameterIndex, int cellIndex, StringBuilder output)
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
				{  output.Append("&nbsp;");  }

			if (addLinks)
				{  AppendSyntaxHighlightedTextWithTypeLinks(start, end, output, links, linkTargets, extendTypeSearch: true);  }
			else
				{  AppendSyntaxHighlightedText(start, end, output);  }

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
				{  output.Append("&nbsp;");  }
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

		/* var: links
		 * A list of <Links> that contain any which will appear in the prototype, or null if links aren't needed.
		 */
		protected IList<Link> links;

		/* var: linkTargets
		 * A list of topics that contain the targets of any resolved links appearing in <links>, or null if links aren't needed.
		 */
		protected IList<Topics.Topic> linkTargets;

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
