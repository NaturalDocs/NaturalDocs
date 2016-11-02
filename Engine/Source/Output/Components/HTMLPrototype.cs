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

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Links;
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
			ModifierQualifier, Type, TypeNameSeparator, 
			NamePrefix, Name, 
			DefaultValueSeparator, DefaultValue
			}



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: HTMLPrototype
		 */
		public HTMLPrototype (HTMLTopicPage topicPage) : base (topicPage)
			{
			parsedPrototype = null;
			language = null;
			columnIndexes = null;
			endOfColumnsIndex = -1;
			columnWidths = null;
			htmlCells = null;
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

			if (parsedPrototype.NumberOfParameters == 0)
				{
				BuildNoParameterForm();
				return;
				}

			columnWidths = new int[NumberOfColumns];
			htmlCells = new string[parsedPrototype.NumberOfParameters, NumberOfColumns];

			for (int p = 0; p < parsedPrototype.NumberOfParameters; p++)
				{
				CalculateColumns(p);

				for (int c = 0; c < NumberOfColumns; c++)
					{
					TokenIterator start, end;
					ColumnType type;

					if (GetColumn(c, out start, out end, out type))
						{
						int length = end.RawTextIndex - start.RawTextIndex;

						if (length > columnWidths[c])
							{  columnWidths[c] = length;  }

						htmlCells[p,c] = BuildCellContents(start, end, type);
						}
					}
				}

			// Default to wide form so the length can be measured by the JavaScript.  The JS will convert them to narrow form
			// if necessary.
			BuildWideForm();
			}


		/* Function: BuildPrePrototypeLines
		 */
		protected void BuildPrePrototypeLines ()
			{
			int lineCount = parsedPrototype.NumberOfPrePrototypeLines;
			TokenIterator start, end;

			for (int i = 0; i < lineCount; i++)
				{
				parsedPrototype.GetPrePrototypeLine(i, out start, out end);

				htmlOutput.Append("<div class=\"PPrePrototypeLine\">");
				BuildSyntaxHighlightedText(start, end);
				htmlOutput.Append("</div>");
				}
			}


		/* Function: BuildPostPrototypeLines
		 */
		protected void BuildPostPrototypeLines ()
			{
			int lineCount = parsedPrototype.NumberOfPostPrototypeLines;
			TokenIterator start, end;

			for (int i = 0; i < lineCount; i++)
				{
				parsedPrototype.GetPostPrototypeLine(i, out start, out end);

				htmlOutput.Append("<div class=\"PPostPrototypeLine\">");
				BuildSyntaxHighlightedText(start, end);
				htmlOutput.Append("</div>");
				}
			}


		/* Function: CalculateColumns
		 * Fills in <columnIndexes> for the passed parameter.  If the parameter doesn't exist it will return false.
		 */
		protected bool CalculateColumns (int parameterIndex)
			{
			TokenIterator startParam, endParam;

			if (parsedPrototype.GetParameter(parameterIndex, out startParam, out endParam) == false)
				{  return false;  }

			if (columnIndexes == null)
				{  columnIndexes = new int[NumberOfColumns];  }

			TokenIterator iterator = startParam;
			iterator.NextPastWhitespace(endParam);
			PrototypeParsingType type = iterator.PrototypeParsingType;

			if (parsedPrototype.Style == ParsedPrototype.ParameterStyle.C)
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


			else if (parsedPrototype.Style == ParsedPrototype.ParameterStyle.Pascal)
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
							 type == PrototypeParsingType.ParamSeparator ||
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
		 * Returns the bounds of a parameter's column and what type it is, which depends on <ColumnOrder>.  You *must* call
		 * <CalculateColumns()> beforehand.  Returns false if the column index is out of bounds or the contents are empty for
		 * that particular slot.
		 */
		public bool GetColumn (int columnIndex, out TokenIterator start, out TokenIterator end, out ColumnType type)
			{
			if (columnIndex >= columnIndexes.Length)
				{ 
				start = parsedPrototype.Tokenizer.LastToken;
				end = parsedPrototype.Tokenizer.LastToken;
				type = ColumnType.Name;
				return false;
				}

			int startIndex = columnIndexes[columnIndex];
			int endIndex = (columnIndex + 1 >= columnIndexes.Length ? endOfColumnsIndex : columnIndexes[columnIndex + 1]);

			start = parsedPrototype.Tokenizer.FirstToken;

			if (startIndex > 0)
				{  start.Next(startIndex);  }

			end = start;

			if (endIndex > startIndex)
				{  end.Next(endIndex - startIndex);  }

			type = ColumnOrder[columnIndex];

			end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);
			start.NextPastWhitespace(end);

			return (end > start);
			}


		/* Function: BuildCellContents
		 * Unlike other functions, this returns the contents as a string rather than appending it to <htmlOutput>.
		 */
		protected string BuildCellContents (TokenIterator start, TokenIterator end, ColumnType type)
			{
			StringBuilder html = new StringBuilder();

			// We don't want syntax highlighting on the Name cell because identifiers can accidentally be marked as keywords with
			// simple highlighting and basic language support, such as "event" in "wxPaintEvent &event".
			if (type == ColumnType.Name)
				{  html.EntityEncodeAndAppend(parsedPrototype.Tokenizer.TextBetween(start, end));  }
			else if (addLinks)
				{  BuildTypeLinkedAndSyntaxHighlightedText(start, end, true, html);  }
			else
				{  BuildSyntaxHighlightedText(start, end, html);  }

			if (type == ColumnType.TypeNameSeparator ||
				 type == ColumnType.DefaultValueSeparator)
				{  html.Append("&nbsp;");  }

			else if (end.FundamentalType == FundamentalType.Whitespace &&
						  (type == ColumnType.Name ||
						   type == ColumnType.ModifierQualifier ||
							type == ColumnType.Type) )
				{  
				TokenIterator lookbehind = end;
				lookbehind.Previous();

				if (lookbehind.PrototypeParsingType != PrototypeParsingType.ParamSeparator)
					{  html.Append("&nbsp;");  }
				}

			// Insert a space after the type in cases of "int* x" because the star won't get a trailing space.
			else if (type == ColumnType.Type && end.PrototypeParsingType == PrototypeParsingType.NamePrefix_PartOfType)
				{  html.Append("&nbsp;");  }

			return html.ToString();
			}


		/* Function: BuildCell
		 */
		protected void BuildCell (string contents, ColumnType type, string extraClass = null)
			{
			if (contents == null)
				{
				if (extraClass == null)
					{  htmlOutput.Append("<td></td>");  }
				else
					{  htmlOutput.Append("<td class=\"" + extraClass + "\"></td>");  }
				}
			else
				{
				htmlOutput.Append("<td class=\"P");
				htmlOutput.Append(type.ToString());

				if (extraClass != null)
					{
					htmlOutput.Append(' ');
					htmlOutput.Append(extraClass);
					}

				htmlOutput.Append("\">");

				htmlOutput.Append(contents);

				htmlOutput.Append("</td>");
				}
			}

	
		/* Function: BuildParameterTable
		 */
		protected void BuildParameterTable ()
			{
			int firstUsedCell = 0;
			while (firstUsedCell < columnWidths.Length && columnWidths[firstUsedCell] == 0)
				{  firstUsedCell++;  }

			int lastUsedCell = columnWidths.Length - 1;
			while (lastUsedCell > 0 && columnWidths[lastUsedCell] == 0)
				{  lastUsedCell--;  }

			htmlOutput.Append("<table class=\"PParameters\">");

			for (int p = 0; p < parsedPrototype.NumberOfParameters; p++)
				{
				htmlOutput.Append("<tr>");

				for (int c = firstUsedCell; c <= lastUsedCell; c++)
					{
					if (columnWidths[c] != 0)
						{  
						string extraClass = null;

						if (c == firstUsedCell && c == lastUsedCell)
							{  extraClass = "first last";  }
						else if (c == firstUsedCell)
							{  extraClass = "first";  }
						else if (c == lastUsedCell)
							{  extraClass = "last";  }

						BuildCell(htmlCells[p,c], ColumnOrder[c], extraClass);
						}
					}

				htmlOutput.Append("</tr>");
				}

			htmlOutput.Append("</table>");
			}


		/* Function: BuildNoParameterForm
		 */
		protected void BuildNoParameterForm ()
			{
			htmlOutput.Append("<div id=\"NDPrototype" + topic.TopicID + "\" class=\"NDPrototype NoParameterForm\">");

			BuildPrePrototypeLines();

			TokenIterator start, end;
			parsedPrototype.GetCompletePrototype(out start, out end);

			if (addLinks)
				{  BuildTypeLinkedAndSyntaxHighlightedText(start, end);  }
			else
				{  BuildSyntaxHighlightedText(start, end);  }

			BuildPostPrototypeLines();

			htmlOutput.Append("</div>");
			}


		/* Function: BuildWideForm
		 */
		protected void BuildWideForm ()
			{
			htmlOutput.Append("<div id=\"NDPrototype" + topic.TopicID + "\" class=\"NDPrototype WideForm " +
				parsedPrototype.Style.ToString() + "Style\">");
				
			BuildPrePrototypeLines();
			
			htmlOutput.Append("<table><tr>");

			TokenIterator start, end;
			parsedPrototype.GetBeforeParameters(out start, out end);

			htmlOutput.Append("<td class=\"PBeforeParameters\">");

			if (addLinks)
				{  BuildTypeLinkedAndSyntaxHighlightedText(start, end);  }
			else
				{  BuildSyntaxHighlightedText(start, end);  }

			htmlOutput.Append("</td>");

			htmlOutput.Append("<td class=\"PParametersParentCell\">");
				BuildParameterTable();
			htmlOutput.Append("</td>");

			parsedPrototype.GetAfterParameters(out start, out end);

			htmlOutput.Append("<td class=\"PAfterParameters\">");

			if (addLinks)
				{  BuildTypeLinkedAndSyntaxHighlightedText(start, end);  }
			else
				{  BuildSyntaxHighlightedText(start, end);  }

			htmlOutput.Append("</td></tr></table>");
			
			BuildPostPrototypeLines();

			htmlOutput.Append("</div>");
			}


		/* Function: BuildNarrowForm
		 */
		protected void BuildNarrowForm ()
			{
			htmlOutput.Append("<div id=\"NDPrototype" + topic.TopicID + "\" class=\"NDPrototype NarrowForm " +
				parsedPrototype.Style.ToString() + "Style\">");
				
			BuildPrePrototypeLines();

			htmlOutput.Append("<table>");

			TokenIterator start, end;
			parsedPrototype.GetBeforeParameters(out start, out end);

			htmlOutput.Append("<tr><td class=\"PBeforeParameters\">");

			if (addLinks)
				{  BuildTypeLinkedAndSyntaxHighlightedText(start, end);  }
			else
				{  BuildSyntaxHighlightedText(start, end);  }

			htmlOutput.Append("</td></tr>");

			htmlOutput.Append("<tr><td class=\"PParametersParentCell\">");
				BuildParameterTable();
			htmlOutput.Append("</td></tr>");

			parsedPrototype.GetAfterParameters(out start, out end);

			htmlOutput.Append("<tr><td class=\"PAfterParameters\">");

			if (addLinks)
				{  BuildTypeLinkedAndSyntaxHighlightedText(start, end);  }
			else
				{  BuildSyntaxHighlightedText(start, end);  }

			htmlOutput.Append("</td></tr></table>");

			BuildPostPrototypeLines();

			htmlOutput.Append("</div>");
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
				switch (parsedPrototype.Style)
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

		/* var: columnIndexes
		 * An array of symbol indexes representing the starting position of each column.  The indexes are taken from 
		 * <TokenIterator.TokenIndex> and so are relative to the start of <parsedPrototype> rather than the parameter.
		 */
		protected int[] columnIndexes;

		/* var: endOfColumnsIndex
		 * The symbol index of the end of the last column in <columnIndexes>.
		 */
		protected int endOfColumnsIndex;

		/* var: columnWidths
		 * The width in characters of each column.
		 */
		protected int[] columnWidths;

		/* var: htmlCells
		 * The HTML contents of each parameter cell, not including the td tags.  The first index is the parameter, the
		 * second is the column.
		 */
		protected string[,] htmlCells;

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

