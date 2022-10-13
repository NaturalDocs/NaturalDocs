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
using System.Runtime.InteropServices;
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
			parameterLayouts = null;

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

			// Set up variables

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


			// Build the parameter layouts array

			// Allocate the array or reallocate it if it's not big enough.  Make it at least four long so we're unlikely to need to
			// reallocate it for other prototypes.
			if (parameterLayouts == null ||
				parameterLayouts.Length < parsedPrototype.Sections.Count)
				{
				parameterLayouts = new PrototypeParameterLayout[ Math.Max(parsedPrototype.Sections.Count, 4) ];
				}

			// Also determine if there are any parameters at all since we're going to be walking through the sections.
			bool hasParameters = false;

			// Go by parameterLayouts.Length instead of parsedPrototype.Sections.Count because if the array is longer we want
			// to make sure its extra entries are null.
			for (int i = 0; i < parameterLayouts.Length; i++)
				{
				if (i < parsedPrototype.Sections.Count &&
					parsedPrototype.Sections[i] is Prototypes.ParameterSection)
					{
					var parameterSection = (Prototypes.ParameterSection)parsedPrototype.Sections[i];

					// Empty parameter sections don't get a layout nor do they make hasParameters true.
					if (parameterSection.NumberOfParameters > 0)
						{
						parameterLayouts[i] = new PrototypeParameterLayout(parsedPrototype, parameterSection);
						hasParameters = true;
						}
					else
						{  parameterLayouts[i] = null;  }
					}
				else
					{  parameterLayouts[i] = null;  }
				}


			// Apply syntax highlighting if it hasn't already been done

			if (parsedPrototype.Tokenizer.HasSyntaxHighlighting == false)
				{
				var language = EngineInstance.Languages.FromID(Context.Topic.LanguageID);
				language.Parser.SyntaxHighlight(parsedPrototype);
				}


			// Start prototype outer tag

			// We always build the wide form by default, but only include the extra CSS class if there's parameters
			output.Append("<div id=\"NDPrototype" + Context.Topic.TopicID + "\" class=\"NDPrototype" +
								  (hasParameters ? " WideForm" : "") + "\">");


			// Prototype content sections

			int sectionIndex = 0;

			while (sectionIndex < parsedPrototype.Sections.Count)
				{
				var section = parsedPrototype.Sections[sectionIndex];
				var sectionLayout = parameterLayouts[sectionIndex];  // will be null if not a parameter section or is an empty one

				if (sectionLayout != null)
					{
					PrototypeColumnLayout columnLayout;
					int sectionCount = CalculateParameterSectionGroup(sectionIndex, out columnLayout);

					AppendParameterSectionGroup(sectionIndex, sectionCount, columnLayout, output);

					sectionIndex += sectionCount;
					}
				else
					{
					AppendPlainSection(sectionIndex, output);
					sectionIndex++;
					}
				}


			// End prototype outer tag

			output.Append("</div>");
			}



		// Group: Support Functions
		// __________________________________________________________________________


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


		/* Function: AppendPlainSection
		 */
		protected void AppendPlainSection (int sectionIndex, StringBuilder output)
			{
			var section = parsedPrototype.Sections[sectionIndex];

			output.Append("<div class=\"PSection PPlainSection\">");
			AppendText_ExcludePartialKeyword(section.Start, section.End, output);
			output.Append("</div>");
			}


		/* Function: CalculateParameterSectionGroup
		 * Determines how many <Prototype.ParameterSections> should be grouped together for the next call to
		 * <AppendParameterSectionGroup()>.
		 */
		protected int CalculateParameterSectionGroup (int sectionIndex, out PrototypeColumnLayout columnLayout)
			{
			int count = 1;
			columnLayout = parameterLayouts[sectionIndex].Columns;

			while (sectionIndex + count < parsedPrototype.Sections.Count &&
					  parameterLayouts[sectionIndex + count] != null &&
					  parameterLayouts[sectionIndex + count].ParameterStyle == parameterLayouts[sectionIndex].ParameterStyle)
				{  count++;  }

			return count;
			}


		/* Function: AppendParameterSectionGroup
		 * Builds the HTML for one or more <Prototypes.ParameterSections>.  They will always be in wide form.
		 */
		protected void AppendParameterSectionGroup (int sectionIndex, int sectionCount, PrototypeColumnLayout columnLayout, StringBuilder output)
			{
			#if DEBUG
			if (parsedPrototype.Sections[sectionIndex] is not Prototypes.ParameterSection)
				{  throw new Exception("AppendParameterSectionGroup was called on an index that wasn't a parameter section.");  }
			if (parameterLayouts[sectionIndex] == null)
				{  throw new Exception("AppendParameterSectionGroup was called on an index that doesn't have a layout.");  }
			#endif


			// Opening tags

			string parameterCSSClass;

			switch (parameterLayouts[sectionIndex].ParameterStyle)
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


			int wideColumnCount = 1 + columnLayout.UsedCount + 1;

			// Need one extra column in case the before/after parameters section are wider than the parameter columns.  If we didn't
			// have it the other columns would stretch to fill the horizontal space.
			int narrowColumnCount = columnLayout.UsedCount + 1;

			output.Append("<div class=\"PParameterCells\" " +
										"data-WideColumnCount=\"" + wideColumnCount + "\" " +
										"data-NarrowColumnCount=\"" + narrowColumnCount + "\">");


			// Sections

			int wideRowStart = 1;
			int narrowRowStart = 1;

			for (int i = 0; i < sectionCount; i++)
				{
				var parameterContent = (parsedPrototype.Sections[sectionIndex + i]) as Prototypes.ParameterSection;
				var parameterLayout = parameterLayouts[sectionIndex + i];


				// Before parameters

				TokenIterator start, end;
				parameterContent.GetBeforeParameters(out start, out end);

				// The order for grid-area is grid-row-start/grid-column-start/grid-row-end/grid-column-end

				// Put it in the first column, full height.
				string wideGridArea = wideRowStart +
												"/1/" +
												(wideRowStart + Math.Max(parameterLayout.NumberOfParameters, 1)) +
												"/2";

				// Put it in the first row, all columns.  Add one more column than the parameters use so the cells don't get
				// stretched out if this is longer than them.
				string narrowGridArea = narrowRowStart +
													"/1/" +
													(narrowRowStart + 1) + "/" +
													(1 + columnLayout.UsedCount + 1);

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

				output.Append("<div class=\"PBeforeParameters" + (addSpace ? " RightSpaceOnWide" : "") + (sectionCount > 1 ? " RightAlignOnWide" : "") + "\" " +
											"data-WideGridArea=\"" + wideGridArea + "\" " +
											"data-NarrowGridArea=\"" + narrowGridArea + "\" " +
											"style=\"grid-area:" + wideGridArea + "\">");

				AppendText_ExcludePartialKeyword(start, end, output);

				output.Append("</div>");


				// Parameters

				bool lastCellEndsWithSpace;

				AppendParameters(parameterLayout, columnLayout, wideRowStart, narrowRowStart, output, out lastCellEndsWithSpace);


				// After parameters

				// Not every prototype with parameters will have an after parameters section, mainly Microsoft SQL functions
				// because they don't require parentheses.  Just omit it.
				bool hasAfterParameters = parameterContent.GetAfterParameters(out start, out end);

				if (hasAfterParameters)
					{

					// Put it in the last row, last column
					wideGridArea = (wideRowStart + Math.Max(parameterLayout.NumberOfParameters, 1) - 1) + "/" +
											(2 + columnLayout.UsedCount) + "/" +
											(wideRowStart + Math.Max(parameterLayout.NumberOfParameters, 1)) + "/" +
											(3 + columnLayout.UsedCount);

					// Put it in the last row, all columns.  Add one more column than the parameters use so the cells don't get
					// stretched out if this is longer than them.
					narrowGridArea = (narrowRowStart + 1 + parameterLayout.NumberOfParameters) +
												"/1/" +
												(narrowRowStart + 1 + parameterLayout.NumberOfParameters + 1) + "/" +
												(1 + columnLayout.UsedCount + 1);

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
					}


				// Update position for next section

				wideRowStart += Math.Max(parameterLayout.NumberOfParameters, 1);
				narrowRowStart += 1 + parameterLayout.NumberOfParameters;

				if (hasAfterParameters)
					{  narrowRowStart++;  }
				}


			// Closing tags

			output.Append("</div></div>");
			}


		/* Function: AppendParameters
		 */
		protected void AppendParameters (PrototypeParameterLayout parameters, PrototypeColumnLayout columnLayout, int wideRowStart,
														  int narrowRowStart, StringBuilder output, out bool lastCellEndsWithSpace)
			{
			int firstUsedColumn = columnLayout.FirstUsed;
			int lastUsedColumn = columnLayout.LastUsed;

			for (int parameterIndex = 0; parameterIndex < parameters.NumberOfParameters; parameterIndex++)
				{
				for (int columnIndex = firstUsedColumn; columnIndex <= lastUsedColumn; columnIndex++)
					{
					if (columnLayout.IsUsed(columnIndex))
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
															 (columnLayout.UsedColumnIndexOf(columnIndex) + 2) + "/" +
															 (wideRowStart + parameterIndex + 1) + "/" +
															 (columnLayout.UsedColumnIndexOf(columnIndex) + 3);

							string narrowGridArea = (narrowRowStart + 1 + parameterIndex) + "/" +
																(columnLayout.UsedColumnIndexOf(columnIndex) + 1) + "/" +
																(narrowRowStart + 1 + parameterIndex + 1) + "/" +
																(columnLayout.UsedColumnIndexOf(columnIndex) + 2);

							output.Append("<div class=\"P" + columnLayout.TypeOf(columnIndex).ToString() + (extraCSSClass != null ? ' ' + extraCSSClass : "") + "\" " +
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

			int lastCellWidth = parameters.GetContentWidth(parameters.NumberOfParameters - 1, lastUsedColumn);
			lastCellEndsWithSpace = (lastCellWidth < columnLayout.WidthOf(lastUsedColumn));
			}


		/* Function: AppendParameterCell
		 */
		protected void AppendParameterCell (PrototypeParameterLayout parameters, int parameterIndex, int columnIndex, StringBuilder output)
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

		/* var: parameterLayouts
		 * An array of <ParameterLayouts> corresponding to the sections of <parsedPrototype>.  Each <Prototypes.Section>
		 * will have an entry at the same index.  If that section is not a <Prototypes.ParameterSection> its entry will be null.
		 */
		protected PrototypeParameterLayout[] parameterLayouts;

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
