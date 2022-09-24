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


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class Prototype : HTML.Components.FormattedText
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Prototype
		 */
		public Prototype (Context context) : base (context)
			{
			parsedPrototype = null;

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


		/* Function: AppendPlainSection
		 */
		protected void AppendPlainSection (Prototypes.Section section, StringBuilder output)
			{
			output.Append("<div class=\"PSection PPlainSection\">");
			AppendText_ExcludePartialKeyword(section.Start, section.End, output);
			output.Append("</div>");
			}


		/* Function: AppendParameterSection
		 * Builds the HTML for a <Prototypes.ParameterSection>.  It will always be in wide form.
		 */
		protected void AppendParameterSection (Prototypes.ParameterSection section, StringBuilder output)
			{
			var parameters = new PrototypeParameters(parsedPrototype, section);


			// Opening tags

			string parameterCSSClass;

			switch (section.ParameterStyle)
				{
				case ParsedPrototype.ParameterStyle.C:
					parameterCSSClass = "CStyle";
					break;
				case ParsedPrototype.ParameterStyle.Pascal:
					parameterCSSClass = "PascalStyle";
					break;
				default:
					throw new NotImplementedException();
				}

			output.Append("<div class=\"PSection PParameterSection " + parameterCSSClass + "\">");


			int wideColumnCount = 1 + parameters.Columns.UsedCount + 1;

			// Need one extra column in case the before/after parameters section are wider than the parameter columns.  If we didn't
			// have it the other columns would stretch to fill the horizontal space.
			int narrowColumnCount = parameters.Columns.UsedCount + 1;

			output.Append("<div class=\"PParameterCells\" " +
										"data-WideColumnCount=\"" + wideColumnCount + "\" " +
										"data-NarrowColumnCount=\"" + narrowColumnCount + "\">");


			// Before parameters

			int wideRowStart = 1;
			int narrowRowStart = 1;

			TokenIterator start, end;
			section.GetBeforeParameters(out start, out end);

			// The order for grid-area is grid-row-start/grid-column-start/grid-row-end/grid-column-end

			// Put it in the first column, full height.
			string wideGridArea = wideRowStart +
											"/1/" +
											(wideRowStart + Math.Max(parameters.Count, 1)) +
											"/2";

			// Put it in the first row, all columns.  Add one more column than the parameters use so the cells don't get
			// stretched out if this is longer than them.
			string narrowGridArea = narrowRowStart +
												"/1/" +
												(narrowRowStart + 1) + "/" +
												(1 + parameters.Columns.UsedCount + 1);

			// Add a space between this and the parameters if there was an ending whitespace character that was marked
			// as part of the BeforeParameters section.  This should only happen if it was significant; it should have been
			// excluded otherwise.
			bool addSpace = end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

			// Also add it if the BeforeParameters section doesn't end with a nice symbol like (.  If it ends with something
			// like (* we want to add it anyway for legibility.  Also for {.
			if (!addSpace)
				{
				TokenIterator beforeEnd = end;
				beforeEnd.Previous();

				addSpace = (beforeEnd >= start &&
								   beforeEnd.Character != '(' &&
								   beforeEnd.Character != '[' &&
								   beforeEnd.Character != '<');
				}

			output.Append("<div class=\"PBeforeParameters" + (addSpace ? " RightSpaceOnWide" : "") + "\" " +
										"data-WideGridArea=\"" + wideGridArea + "\" " +
										"data-NarrowGridArea=\"" + narrowGridArea + "\" " +
										"style=\"grid-area:" + wideGridArea + "\">");

			AppendText_ExcludePartialKeyword(start, end, output);

			output.Append("</div>");


			// Parameters

			bool lastCellEndsWithSpace;

			AppendParameters(parameters, wideRowStart, narrowRowStart, output, out lastCellEndsWithSpace);


			// After parameters

			section.GetAfterParameters(out start, out end);

			// Put it in the last row, last column
			wideGridArea = (wideRowStart + Math.Max(parameters.Count, 1) - 1) + "/" +
									(2 + parameters.Columns.UsedCount) + "/" +
									(wideRowStart + Math.Max(parameters.Count, 1)) + "/" +
									(3 + parameters.Columns.UsedCount);

			// Put it in the last row, all columns.  Add one more column than the parameters use so the cells don't get
			// stretched out if this is longer than them.
			narrowGridArea = (narrowRowStart + 1 + parameters.Count) +
										"/1/" +
										(narrowRowStart + 1 + parameters.Count + 1) + "/" +
										(1 + parameters.Columns.UsedCount + 1);

			// Add a space between this and the parameters if there was a leading whitespace character that was marked
			// as part of the AfterParameters section.  This should only happen if it was significant; it should have been
			// excluded otherwise.
			addSpace = start.NextPastWhitespace(end);

			// Also add it if the AfterParameters section doesn't start with a nice symbol like ).  If it starts with something
			// like *) we want to add it anyway for legibility.  Also for }.
			if (!addSpace)
				{
				addSpace = (start < end &&
								   start.Character != ')' &&
								   start.Character != ']' &&
								   start.Character != '>');
				}

			string extraCSSClass;

			if (addSpace)
				{
				// We only need to actually add the space if the last parameter doesn't already have one at the end.
				// Otherwise ignore it so it's not overly wide.
				extraCSSClass = (lastCellEndsWithSpace ? "" : " LeftSpaceOnWide");
				}
			else
				{
				// On the other hand, if there's not supposed to be a space and the last parameter ends with one anyway
				// we can bleed the ending part an extra character into it.  This lets closing parentheses line up with the
				// commas in between parameters.
				extraCSSClass = (lastCellEndsWithSpace ? " NegativeLeftSpaceOnWide" : "");
				}

			output.Append("<div class=\"PAfterParameters" + extraCSSClass + "\" " +
										"data-WideGridArea=\"" + wideGridArea + "\" " +
										"data-NarrowGridArea=\"" + narrowGridArea + "\" " +
										"style=\"grid-area:" + wideGridArea + "\">");

			AppendText(start, end, output);

			output.Append("</div>");


			// Closing tags

			output.Append("</div></div>");
			}


		/* Function: AppendText
		 */
		protected void AppendText (TokenIterator start, TokenIterator end, StringBuilder output)
			{
			if (addLinks)
				{  AppendSyntaxHighlightedTextWithTypeLinks(start, end, output, links, linkTargets);  }
			else
				{  AppendSyntaxHighlightedText(start, end, output);  }
			}


		/* Function: AppendText_ExcludePartialKeyword
		 * The same as <AppendText()>, but if the keyword "partial" appears in the text as a type modifier it will be excluded
		 * from the output.
		 */
		protected void AppendText_ExcludePartialKeyword (TokenIterator start, TokenIterator end, StringBuilder output)
			{
			TokenIterator partial;

			if (start.Tokenizer.FindTokenBetween("partial", false, start, end, out partial) &&
				partial.IsStandaloneWord() &&
				partial.PrototypeParsingType == PrototypeParsingType.TypeModifier)
				{
				bool hasBeforePartial = (partial > start);
				bool hasSpaceBeforePartial = false;

				if (hasBeforePartial)
					{
					TokenIterator lookbehind = partial;
					hasSpaceBeforePartial = lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

					AppendText(start, lookbehind, output);
					}

				partial.Next();
				bool hasSpaceAfterPartial = partial.NextPastWhitespace();

				bool hasAfterPartial = (partial < end);

				if (hasAfterPartial)
					{
					if (hasBeforePartial && (hasSpaceBeforePartial || hasSpaceAfterPartial))
						{  output.Append(' ');  }

					AppendText(partial, end, output);
					}
				}

			else // no partial
				{  AppendText(start, end, output);  }
			}


		/* Function: AppendParameters
		 */
		protected void AppendParameters (PrototypeParameters parameters, int wideRowStart, int narrowRowStart,  StringBuilder output,
														  out bool lastCellEndsWithSpace)
			{
			int firstUsedColumn = parameters.Columns.FirstUsed;
			int lastUsedColumn = parameters.Columns.LastUsed;

			for (int parameterIndex = 0; parameterIndex < parameters.Count; parameterIndex++)
				{
				for (int columnIndex = firstUsedColumn; columnIndex <= lastUsedColumn; columnIndex++)
					{
					if (parameters.Columns.IsUsed(columnIndex))
						{
						string extraCSSClass = null;

						if (columnIndex == firstUsedColumn && columnIndex == lastUsedColumn)
							{  extraCSSClass = "InFirstParameterColumn InLastParameterColumn";  }
						else if (columnIndex == firstUsedColumn)
							{  extraCSSClass = "InFirstParameterColumn";  }
						else if (columnIndex == lastUsedColumn)
							{  extraCSSClass = "InLastParameterColumn";  }

						if (parameters.HasContent(parameterIndex, columnIndex))
							{
							// The order for grid-area is grid-row-start/grid-column-start/grid-row-end/grid-column-end

							string wideGridArea = (wideRowStart + parameterIndex) + "/" +
																(parameters.Columns.UsedColumnIndexOf(columnIndex) + 2) + "/" +
																(wideRowStart + parameterIndex + 1) + "/" +
																(parameters.Columns.UsedColumnIndexOf(columnIndex) + 3);

							string narrowGridArea = (narrowRowStart + 1 + parameterIndex) + "/" +
																	(parameters.Columns.UsedColumnIndexOf(columnIndex) + 1) + "/" +
																	(narrowRowStart + 1 + parameterIndex + 1) + "/" +
																	(parameters.Columns.UsedColumnIndexOf(columnIndex) + 2);

							output.Append("<div class=\"P" + parameters.Columns.TypeOf(columnIndex).ToString() + (extraCSSClass != null ? ' ' + extraCSSClass : "") + "\" " +
														"data-WideGridArea=\"" + wideGridArea + "\" " +
														"data-NarrowGridArea=\"" + narrowGridArea + "\" " +
														"style=\"grid-area:" + wideGridArea + "\">");

							AppendParameterCell(parameters, parameterIndex, columnIndex, output);

							output.Append("</div>");
							}
						}
					}
				}


			// Determine lastCellEndsWithSpace before returning

			int lastCellWidth = parameters.GetContentWidth(parameters.Count - 1, lastUsedColumn);
			lastCellEndsWithSpace = (lastCellWidth < parameters.Columns.WidthOf(lastUsedColumn));
			}


		/* Function: AppendParameterCell
		 */
		protected void AppendParameterCell (PrototypeParameters parameters, int parameterIndex, int columnIndex, StringBuilder output)
			{
			TokenIterator start, end;
			parameters.GetContent(parameterIndex, columnIndex, out start, out end);

			if (parameters.HasLeadingSpace(parameterIndex, columnIndex))
				{  output.Append("&nbsp");  }

			if (addLinks)
				{  AppendSyntaxHighlightedTextWithTypeLinks(start, end, output, links, linkTargets, extendTypeSearch: true);  }
			else
				{  AppendSyntaxHighlightedText(start, end, output);  }

			if (parameters.HasTrailingSpace(parameterIndex, columnIndex))
				{  output.Append("&nbsp;");  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: parsedPrototype
		 * The prototype as a <ParsedPrototype> object.
		 */
		protected ParsedPrototype parsedPrototype;

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

		}
	}
