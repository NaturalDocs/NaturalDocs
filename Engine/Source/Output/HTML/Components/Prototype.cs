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
 				var sectionParameterLayout = parameterLayouts[sectionIndex];

				// Will be null if it wasn't a parameter section or it contained no parameters
				if (sectionParameterLayout == null)
					{
					AppendPlainSection(sectionIndex, output);
					sectionIndex++;
					}
				else
					{
					int sectionCount = GroupParameterSections(sectionIndex);
					var sharedColumnLayout = FormatParameterSections(sectionIndex, sectionCount);

					AppendParameterSections(sectionIndex, sectionCount, sharedColumnLayout, output);

					sectionIndex += sectionCount;
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


		/* Function: GroupParameterSections
		 * Determines how many consecutive <Prototype.ParameterSections> should be grouped together for the next call to
		 * <AppendParameterSections()>.  It will always return at least 1.
		 */
		protected int GroupParameterSections (int sectionIndex)
			{
			int count = 1;
			var parameterStyle = parameterLayouts[sectionIndex].ParameterStyle;

			for (int i = sectionIndex + 1; i < parsedPrototype.Sections.Count; i++)
				{
				if (parameterLayouts[i] == null ||  // not a parameter section
					parameterLayouts[i].ParameterStyle != parameterStyle)  // not the same style
					{  break;  }

				count++;
				}

			return count;
			}


		/* Function: FormatParameterSections
		 * Reformats some cells in the <Prototype.ParameterSections> to make the spacing consistent, including some tweaks
		 * based on the shared column layout.  Returns the shared column layout.
		 */
		protected PrototypeColumnLayout FormatParameterSections (int sectionIndex, int sectionCount)
			{
			var columnLayout = GetSharedColumnLayout(sectionIndex, sectionCount);


			// First gather some information about the column layout

			int firstUsedColumnIndex = columnLayout.FirstUsed;
			int lastUsedColumnIndex = columnLayout.LastUsed;

			int defaultValueSeparatorColumnIndex;
			int beforeDefaultValueSeparatorColumnIndex;

			if (columnLayout.IsUsed(PrototypeColumnType.DefaultValueSeparator))
				{
				defaultValueSeparatorColumnIndex = columnLayout.IndexOf(PrototypeColumnType.DefaultValueSeparator);
				beforeDefaultValueSeparatorColumnIndex = columnLayout.PreviousUsed(defaultValueSeparatorColumnIndex);
				}
			else
				{
				defaultValueSeparatorColumnIndex = -1;
				beforeDefaultValueSeparatorColumnIndex = -1;
				}

			int propertyValueSeparatorColumnIndex;
			int beforePropertyValueSeparatorColumnIndex;

			if (columnLayout.IsUsed(PrototypeColumnType.PropertyValueSeparator))
				{
				propertyValueSeparatorColumnIndex = columnLayout.IndexOf(PrototypeColumnType.PropertyValueSeparator);
				beforePropertyValueSeparatorColumnIndex = columnLayout.PreviousUsed(propertyValueSeparatorColumnIndex);
				}
			else
				{
				propertyValueSeparatorColumnIndex = -1;
				beforePropertyValueSeparatorColumnIndex = -1;
				}

			int typeNameSeparatorColumnIndex;
			int beforeTypeNameSeparatorColumnIndex;

			if (columnLayout.IsUsed(PrototypeColumnType.TypeNameSeparator))
				{
				typeNameSeparatorColumnIndex = columnLayout.IndexOf(PrototypeColumnType.TypeNameSeparator);
				beforeTypeNameSeparatorColumnIndex = columnLayout.PreviousUsed(typeNameSeparatorColumnIndex);
				}
			else
				{
				typeNameSeparatorColumnIndex = -1;
				beforeTypeNameSeparatorColumnIndex = -1;
				}


			// Initial run through for easy normalizations

			TokenIterator start, end;

			for (int i = sectionIndex; i < sectionIndex + sectionCount; i++)
				{
				var section = parameterLayouts[i];

				for (int parameterIndex = 0; parameterIndex < section.NumberOfParameters; parameterIndex++)
					{
					// The first used column always gets leading spaces removed
					section.SetLeadingSpace(parameterIndex, firstUsedColumnIndex, false);

					// The last used column always gets trailing spaces removed
					section.SetTrailingSpace(parameterIndex, lastUsedColumnIndex, false);

					// If there's a default value separator it should always have both leading and trailing spaces
					if (defaultValueSeparatorColumnIndex != -1 &&
						section.HasContent(parameterIndex, defaultValueSeparatorColumnIndex))
						{
						section.SetLeadingSpace(parameterIndex, defaultValueSeparatorColumnIndex, true);
						section.SetTrailingSpace(parameterIndex, defaultValueSeparatorColumnIndex, true);
						}

					// Also remove the trailing space of the column before it.  This isn't conditional on this column having
					// content in this particular parameter, just that the column exists at all.
					if (beforeDefaultValueSeparatorColumnIndex != -1)
						{  section.SetTrailingSpace(parameterIndex, beforeDefaultValueSeparatorColumnIndex, false);  }

					// If there's a property value separator it should always have a trailing space.  It should also have a leading
					// space unless it's ":".  Watch out for ":=" though.
					if (propertyValueSeparatorColumnIndex != -1 &&
						section.GetContent(parameterIndex, propertyValueSeparatorColumnIndex, out start, out end))
						{
						bool leadingSpace = (start.Character != ':' || start.MatchesAcrossTokens(":=") || start.MatchesAcrossTokens("::="));

						section.SetLeadingSpace(parameterIndex, propertyValueSeparatorColumnIndex, leadingSpace);
						section.SetTrailingSpace(parameterIndex, propertyValueSeparatorColumnIndex, true);
						}

					// Also remove the trailing space of the column before it.  This isn't conditional on this column having
					// content in this particular parameter, just that the column exists at all.
					if (beforePropertyValueSeparatorColumnIndex != -1)
						{  section.SetTrailingSpace(parameterIndex, beforePropertyValueSeparatorColumnIndex, false);  }

					// If there's a type name separator it should always have a trailing space.  It should not have a leading
					// space unless it's text-based, such as SQL's "AS", as opposed to something like Pascal's ":".
					if (typeNameSeparatorColumnIndex != -1 &&
						section.GetContent(parameterIndex, typeNameSeparatorColumnIndex, out start, out end))
						{
						bool leadingSpace = (start.FundamentalType == FundamentalType.Text);

						section.SetLeadingSpace(parameterIndex, typeNameSeparatorColumnIndex, leadingSpace);
						section.SetTrailingSpace(parameterIndex, typeNameSeparatorColumnIndex, true);
						}

					// Also remove the trailing space of the column before it.  This isn't conditional on this column having
					// content in this particular parameter, just that the column exists at all.
					if (beforeTypeNameSeparatorColumnIndex != -1)
						{  section.SetTrailingSpace(parameterIndex, beforeTypeNameSeparatorColumnIndex, false);  }
					}
				}


			// Recalculate the columns since their widths may have changed
			RecalculateColumns(sectionIndex, sectionCount);
			columnLayout = GetSharedColumnLayout(sectionIndex, sectionCount);


			// Now we get into pickier things.  Cells with leading spaces don't need them if the preceding one is empty or
			// shorter than the entire column, because there would be space there anyway.  However, this has to be true of
			// ALL cells in that column for us to remove them because they won't be aligned otherwise.

			bool canRemoveDefaultValueSeparatorLeadingSpace = true;
			bool canRemovePropertyValueSeparatorLeadingSpace = true;
			bool canRemoveTypeNameSeparatorLeadingSpace = true;

			int beforeDefaultValueSeparatorColumnWidth = (beforeDefaultValueSeparatorColumnIndex == -1 ?
																				   0 : columnLayout.WidthOf(beforeDefaultValueSeparatorColumnIndex));
			int beforePropertyValueSeparatorColumnWidth = (beforePropertyValueSeparatorColumnIndex == -1 ?
																					0 : columnLayout.WidthOf(beforePropertyValueSeparatorColumnIndex));
			int beforeTypeNameSeparatorColumnWidth = (beforeTypeNameSeparatorColumnIndex == -1 ?
																			   0 : columnLayout.WidthOf(beforeTypeNameSeparatorColumnIndex));

			for (int i = sectionIndex; i < sectionIndex + sectionCount; i++)
				{
				var section = parameterLayouts[i];

				for (int parameterIndex = 0; parameterIndex < section.NumberOfParameters; parameterIndex++)
					{
					if (defaultValueSeparatorColumnIndex == -1 ||

						// Don't apply this tweak when both columns have content and the left one uses the full column width,
						// since then they'll be right next to each other with no implied space so we need the extra one.
						(section.HasContent(parameterIndex, defaultValueSeparatorColumnIndex) &&
						 section.HasContent(parameterIndex, beforeDefaultValueSeparatorColumnIndex) &&
						 section.GetContentWidth(parameterIndex, beforeDefaultValueSeparatorColumnIndex) == beforeDefaultValueSeparatorColumnWidth) ||

						// Also don't apply this tweak when the separator is text based, like SQL's "DEFAULT".  Doesn't look as good.
						(section.GetContent(parameterIndex, defaultValueSeparatorColumnIndex, out start, out end) &&
						 start.FundamentalType == FundamentalType.Text))
						{
						canRemoveDefaultValueSeparatorLeadingSpace = false;
						}

					if (propertyValueSeparatorColumnIndex == -1 ||

						// Don't apply this tweak when both columns have content and the left one uses the full column width,
						// since then they'll be right next to each other with no implied space so we need the extra one.
						(section.HasContent(parameterIndex, propertyValueSeparatorColumnIndex) &&
						 section.HasContent(parameterIndex, beforePropertyValueSeparatorColumnIndex) &&
						 section.GetContentWidth(parameterIndex, beforePropertyValueSeparatorColumnIndex) == beforePropertyValueSeparatorColumnWidth))
						{
						canRemovePropertyValueSeparatorLeadingSpace = false;
						}

					if (typeNameSeparatorColumnIndex == -1 ||

						// Don't apply this tweak when both columns have content and the left one uses the full column width,
						// since then they'll be right next to each other with no implied space so we need the extra one.
						(section.HasContent(parameterIndex, typeNameSeparatorColumnIndex) &&
						 section.HasContent(parameterIndex, beforeTypeNameSeparatorColumnIndex) &&
						 section.GetContentWidth(parameterIndex, beforeTypeNameSeparatorColumnIndex) == beforeTypeNameSeparatorColumnWidth) ||

						// Also don't apply this tweak when the separator is text based, like SQL's "AS".  Doesn't look as good.
						(section.GetContent(parameterIndex, typeNameSeparatorColumnIndex, out start, out end) &&
						 start.FundamentalType == FundamentalType.Text))
						{
						canRemoveTypeNameSeparatorLeadingSpace = false;
						}
					}
				}

			if (canRemoveDefaultValueSeparatorLeadingSpace ||
				canRemovePropertyValueSeparatorLeadingSpace ||
				canRemoveTypeNameSeparatorLeadingSpace)
				{
				for (int i = sectionIndex; i < sectionIndex + sectionCount; i++)
					{
					var section = parameterLayouts[i];

					for (int parameterIndex = 0; parameterIndex < section.NumberOfParameters; parameterIndex++)
						{
						if (canRemoveDefaultValueSeparatorLeadingSpace)
							{  section.SetLeadingSpace(parameterIndex, defaultValueSeparatorColumnIndex, false);  }

						if (canRemovePropertyValueSeparatorLeadingSpace)
							{  section.SetLeadingSpace(parameterIndex, propertyValueSeparatorColumnIndex, false);  }

						if (canRemoveTypeNameSeparatorLeadingSpace)
							{  section.SetLeadingSpace(parameterIndex, typeNameSeparatorColumnIndex, false);  }
						}
					}

				// Recalculate the columns since their widths may have changed again
				RecalculateColumns(sectionIndex, sectionCount);
				columnLayout = GetSharedColumnLayout(sectionIndex, sectionCount);
				}

			return columnLayout;
			}


		/* Function: GetSharedColumnLayout
		 * Returns the combined column layout of the <PrototypeParameterLayouts>.  If there is only one it will return a
		 * reference to that layout's columns.
		 */
		protected PrototypeColumnLayout GetSharedColumnLayout (int sectionIndex, int sectionCount)
			{
			if (sectionCount == 1)
				{  return parameterLayouts[sectionIndex].Columns;  }
			else
				{
				var sharedColumnLayout = parameterLayouts[sectionIndex].Columns.Duplicate();

				for (int i = sectionIndex + 1; i < sectionIndex + sectionCount; i++)
					{
					sharedColumnLayout.MergeWith(parameterLayouts[i].Columns);
					}

				return sharedColumnLayout;
				}
			}


		/* Function: RecalculateColumns
		 * Runs <PrototypeColumnLayout.RecalculateColumns()> on each <PrototypeParameterLayout>.
		 */
		protected void RecalculateColumns (int sectionIndex, int sectionCount)
			{
			for (int i = sectionIndex; i < sectionIndex + sectionCount; i++)
				{  parameterLayouts[i].RecalculateColumns();  }
			}


		/* Function: AppendParameterSections
		 * Builds the HTML for one or more <Prototypes.ParameterSections>.  They will always be in wide form.
		 */
		protected void AppendParameterSections (int sectionIndex, int sectionCount, PrototypeColumnLayout columnLayout,
																	StringBuilder output)
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
