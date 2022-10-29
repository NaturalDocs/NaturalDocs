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

		// Group: Types
		// __________________________________________________________________________


		/* Enum: ParameterGroupAlignment
		 *
		 * The way multiple <PrototypeParameterLayouts> should be aligned when together in a group.
		 *
		 *		AlignAllColumns - All columns are aligned between the parameter sections.
		 *
		 *		--- SystemVerilog Code ---
		 *		(* param1  = "abcde" *)
		 *		(* param22 = "abc"   *)
		 *		---
		 *
		 *		AlignBeforeParameters - Only the BeforeParameters parts align between sections.
		 *
		 *		--- C# Code --
		 *		int Indexer [unsigned int x,
		 *		                       int y ]
		 *		           { get,
		 *		             set  }
		 */
		public enum ParameterGroupAlignment
			{  AlignAllColumns, AlignBeforeParameters  }



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

			//
			// Set up variables
			//

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


			//
			// Build the parameter layouts array
			//

			// Allocate the array or reallocate it if it's not big enough.  Make it at least four long so we're unlikely to need to
			// reallocate it for other prototypes.
			if (parameterLayouts == null ||
				parameterLayouts.Length < parsedPrototype.Sections.Count)
				{
				parameterLayouts = new PrototypeParameterLayout[ Math.Max(parsedPrototype.Sections.Count, 4) ];
				}

			// Also determine if there are any parameters at all since we're going to be walking through the sections.
			bool hasParameters = false;

			// Loop until the end of parameterLayouts.Length instead of parsedPrototype.Sections.Count because if the array
			// is longer we want to make sure its extra entries are null.
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


			//
			// Apply syntax highlighting if it hasn't already been done
			//

			if (parsedPrototype.Tokenizer.HasSyntaxHighlighting == false)
				{
				var language = EngineInstance.Languages.FromID(Context.Topic.LanguageID);
				language.Parser.SyntaxHighlight(parsedPrototype);
				}


			//
			// Add the outer tag to the output
			//

			// We always build the wide form by default, but only include the extra CSS class if there's parameters
			output.Append("<div id=\"NDPrototype" + Context.Topic.TopicID + "\" class=\"NDPrototype" +
								  (hasParameters ? " WideForm" : "") + "\">");


			//
			// Add the content sections to the output
			//

			int sectionIndex = 0;

			while (sectionIndex < parsedPrototype.Sections.Count)
				{
 				var sectionParameterLayout = parameterLayouts[sectionIndex];

				// sectionParameterLayout will be null if it wasn't a parameter section or it contained no parameters
				if (sectionParameterLayout == null)
					{
					AppendPlainSection(sectionIndex, output);
					sectionIndex++;
					}
				else
					{
					int sectionCount;
					ParameterGroupAlignment alignment;

					GroupParameterSections(sectionIndex, out sectionCount, out alignment);

					var sharedColumnLayout = FormatParameterSections(sectionIndex, sectionCount, alignment);

					if (alignment == ParameterGroupAlignment.AlignAllColumns)
						{  AppendSharedColumnParameterSections(sectionIndex, sectionCount, sharedColumnLayout, output);  }
					else if (alignment == ParameterGroupAlignment.AlignBeforeParameters)
						{  AppendLeftAlignedParameterSections(sectionIndex, sectionCount, output);  }
					else
						{  throw new NotImplementedException();  }

					sectionIndex += sectionCount;
					}
				}


			//
			// Close the outer tag in the output
			//

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
		 * <AppendParameterSections()>, and how they should be aligned in the output.  The count will always be at least 1.
		 */
		protected void GroupParameterSections (int sectionIndex, out int count, out ParameterGroupAlignment alignment)
			{

			// First determine how long the group should be

			count = 1;

			TokenIterator start, end;
			var parameterStyle = parameterLayouts[sectionIndex].ParameterStyle;

			for (int i = sectionIndex + 1; i < parsedPrototype.Sections.Count; i++)
				{
				// Don't include the section in the group if it's not a parameter section or not the same parameter style
				if (parameterLayouts[i] == null ||
					parameterLayouts[i].ParameterStyle != parameterStyle)
					{  break;  }

				// Also, only include it if the BeforeParameters section is something short like { or (*
				(parsedPrototype.Sections[i] as Prototypes.ParameterSection).GetBeforeParameters(out start, out end);
				int beforeParametersLength = end.RawTextIndex - start.RawTextIndex;

				if (beforeParametersLength > 2)
					{  break;  }
				else if (start.Character != '(' &&
						   start.Character != '[' &&
						   start.Character != '{' &&
						   start.Character != '<')
					{  break;  }

				count++;
				}


			// Next determine the alignment, which is only relevant if there's more than one member of the group

			alignment = ParameterGroupAlignment.AlignAllColumns;

			if (count > 1)
				{
				// The alignment defaults to AlignAllColumns.  The parameter sections can be one of three types, determined by
				// the syntax highlighting of the variable name:
				//
				//    - Regular parameters, which should have no highlighting on the name
				//    - Property members like get/set/init, which should be highlighted as keywords
				//    - Metadata properties, which should be highlighted as metadata
				//
				// All sections must have the same type to keep using AlignAllColumns.  If any are different then we switch to
				// AlignBeforeParameters.  Trying to align the columns of disparate parameter types will not look good.

				(parsedPrototype.Sections[sectionIndex] as Prototypes.ParameterSection).GetParameterName(0, out start, out end);
				var highlightType = start.SyntaxHighlightingType;

				for (int i = sectionIndex + 1; i < sectionIndex + count; i++)
					{
					(parsedPrototype.Sections[i] as Prototypes.ParameterSection).GetParameterName(0, out start, out end);

					if (start.SyntaxHighlightingType != highlightType)
						{
						alignment = ParameterGroupAlignment.AlignBeforeParameters;
						break;
						}
					}
				}
			}


		/* Function: FormatParameterSections
		 * Reformats some cells in the <Prototype.ParameterSections> to make the spacing consistent, including some tweaks
		 * based on the shared column layout.  Returns the shared column layout if you're using
		 * <ParameterGroupAlignment.AlignAllColumns>, null otherwise.
		 */
		protected PrototypeColumnLayout FormatParameterSections (int sectionIndex, int sectionCount,
																								ParameterGroupAlignment alignment)
			{
			// If we're using AlignBeforeParameters, format each one independently instead of with a shared layout.
			if (sectionCount > 1 && alignment == ParameterGroupAlignment.AlignBeforeParameters)
				{
				for (int i = sectionIndex; i < sectionIndex + sectionCount; i++)
					{  FormatParameterSections(i, 1, alignment);  }

				return null;
				}


			PrototypeColumnLayout columnLayout;

			if (sectionCount > 1 && alignment == ParameterGroupAlignment.AlignAllColumns)
				{  columnLayout = GetSharedColumnLayout(sectionIndex, sectionCount);  }
			else
				{  columnLayout = parameterLayouts[sectionIndex].Columns;  }


			//
			// First determine some column indexes we'll need
			//

			int firstUsedColumnIndex = columnLayout.FirstUsed;
			int lastUsedColumnIndex = columnLayout.LastUsed;

			int defaultValueSeparatorColumnIndex = -1;
			int beforeDefaultValueSeparatorColumnIndex = -1;

			if (columnLayout.IsUsed(PrototypeColumnType.DefaultValueSeparator))
				{
				defaultValueSeparatorColumnIndex = columnLayout.IndexOf(PrototypeColumnType.DefaultValueSeparator);
				beforeDefaultValueSeparatorColumnIndex = columnLayout.PreviousUsed(defaultValueSeparatorColumnIndex);
				}

			int propertyValueSeparatorColumnIndex = -1;
			int beforePropertyValueSeparatorColumnIndex = -1;

			if (columnLayout.IsUsed(PrototypeColumnType.PropertyValueSeparator))
				{
				propertyValueSeparatorColumnIndex = columnLayout.IndexOf(PrototypeColumnType.PropertyValueSeparator);
				beforePropertyValueSeparatorColumnIndex = columnLayout.PreviousUsed(propertyValueSeparatorColumnIndex);
				}

			int typeNameSeparatorColumnIndex = -1;
			int beforeTypeNameSeparatorColumnIndex = -1;

			if (columnLayout.IsUsed(PrototypeColumnType.TypeNameSeparator))
				{
				typeNameSeparatorColumnIndex = columnLayout.IndexOf(PrototypeColumnType.TypeNameSeparator);
				beforeTypeNameSeparatorColumnIndex = columnLayout.PreviousUsed(typeNameSeparatorColumnIndex);
				}


			//
			// Normalize column spacing, such as default value separators always having spaces before and after them
			//

			TokenIterator start, end;

			for (int i = sectionIndex; i < sectionIndex + sectionCount; i++)
				{
				var sectionLayout = parameterLayouts[i];

				for (int parameterIndex = 0; parameterIndex < sectionLayout.NumberOfParameters; parameterIndex++)
					{
					// The first used column always gets leading spaces removed
					sectionLayout.SetLeadingSpace(parameterIndex, firstUsedColumnIndex, false);

					// The last used column always gets trailing spaces removed
					sectionLayout.SetTrailingSpace(parameterIndex, lastUsedColumnIndex, false);

					// If there's a default value separator it should always have both leading and trailing spaces
					if (defaultValueSeparatorColumnIndex != -1)
						{
						if (sectionLayout.HasContent(parameterIndex, defaultValueSeparatorColumnIndex))
							{
							sectionLayout.SetLeadingSpace(parameterIndex, defaultValueSeparatorColumnIndex, true);
							sectionLayout.SetTrailingSpace(parameterIndex, defaultValueSeparatorColumnIndex, true);
							}

						// Also remove the trailing space of the column before it.  It doesn't matter if the cell has content in this
						// particular parameter, just that the column exists at all.
						if (beforeDefaultValueSeparatorColumnIndex != -1)
							{  sectionLayout.SetTrailingSpace(parameterIndex, beforeDefaultValueSeparatorColumnIndex, false);  }
						}

					// If there's a property value separator it should always have a trailing space.  It should also have a leading
					// space unless it's ":".  Watch out for ":=" though.
					if (propertyValueSeparatorColumnIndex != -1)
						{
						if (sectionLayout.GetContent(parameterIndex, propertyValueSeparatorColumnIndex, out start, out end))
							{
							bool leadingSpace = (start.Character != ':' || start.MatchesAcrossTokens(":=") || start.MatchesAcrossTokens("::="));

							sectionLayout.SetLeadingSpace(parameterIndex, propertyValueSeparatorColumnIndex, leadingSpace);
							sectionLayout.SetTrailingSpace(parameterIndex, propertyValueSeparatorColumnIndex, true);
							}

						if (beforePropertyValueSeparatorColumnIndex != -1)
							{  sectionLayout.SetTrailingSpace(parameterIndex, beforePropertyValueSeparatorColumnIndex, false);  }
						}

					// If there's a type name separator it should always have a trailing space.  It should not have a leading space
					// unless it's text-based, such as SQL's "AS", as opposed to something like Pascal's ":".
					if (typeNameSeparatorColumnIndex != -1)
						{
						if (sectionLayout.GetContent(parameterIndex, typeNameSeparatorColumnIndex, out start, out end))
							{
							bool leadingSpace = (start.FundamentalType == FundamentalType.Text);

							sectionLayout.SetLeadingSpace(parameterIndex, typeNameSeparatorColumnIndex, leadingSpace);
							sectionLayout.SetTrailingSpace(parameterIndex, typeNameSeparatorColumnIndex, true);
							}

						if (beforeTypeNameSeparatorColumnIndex != -1)
							{  sectionLayout.SetTrailingSpace(parameterIndex, beforeTypeNameSeparatorColumnIndex, false);  }
						}
					}
				}


			// Recalculate the columns since their widths may have changed
			if (sectionCount > 1 && alignment == ParameterGroupAlignment.AlignAllColumns)
				{
				RecalculateColumns(sectionIndex, sectionCount);
				columnLayout = GetSharedColumnLayout(sectionIndex, sectionCount);
				}
			else
				{  parameterLayouts[sectionIndex].RecalculateColumns();  }


			//
			// Now look for leading spaces we can remove because the column before it is empty or its content doesn't use
			// the whole column width.  This creates its own space between the cells so we don't have to add another one,
			// and it will look better if we don't.  However, this has to be applicable to EVERY cell in the column for us to
			// apply it because the column's contents won't be aligned otherwise.
			//

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
				var sectionLayout = parameterLayouts[i];

				for (int parameterIndex = 0; parameterIndex < sectionLayout.NumberOfParameters; parameterIndex++)
					{
					if (defaultValueSeparatorColumnIndex == -1 ||
						beforeDefaultValueSeparatorColumnIndex == -1 ||

						// Don't apply this tweak when both columns have content and the left one uses the full column width,
						// since then they'll be right next to each other with no implied space so we need the extra one.
						(sectionLayout.HasContent(parameterIndex, defaultValueSeparatorColumnIndex) &&
						 sectionLayout.HasContent(parameterIndex, beforeDefaultValueSeparatorColumnIndex) &&
						 sectionLayout.GetContentWidth(parameterIndex, beforeDefaultValueSeparatorColumnIndex) >= beforeDefaultValueSeparatorColumnWidth) ||

						// Also don't apply this tweak when the separator is text based, like SQL's "DEFAULT".  Doesn't look as good.
						(sectionLayout.GetContent(parameterIndex, defaultValueSeparatorColumnIndex, out start, out end) &&
						 start.FundamentalType == FundamentalType.Text))
						{
						canRemoveDefaultValueSeparatorLeadingSpace = false;
						}

					if (propertyValueSeparatorColumnIndex == -1 ||
						beforePropertyValueSeparatorColumnIndex == -1 ||

						// Don't apply this tweak when both columns have content and the left one uses the full column width,
						// since then they'll be right next to each other with no implied space so we need the extra one.
						(sectionLayout.HasContent(parameterIndex, propertyValueSeparatorColumnIndex) &&
						 sectionLayout.HasContent(parameterIndex, beforePropertyValueSeparatorColumnIndex) &&
						 sectionLayout.GetContentWidth(parameterIndex, beforePropertyValueSeparatorColumnIndex) >= beforePropertyValueSeparatorColumnWidth))
						{
						canRemovePropertyValueSeparatorLeadingSpace = false;
						}

					if (typeNameSeparatorColumnIndex == -1 ||
						beforeTypeNameSeparatorColumnIndex == -1 ||

						// Don't apply this tweak when both columns have content and the left one uses the full column width,
						// since then they'll be right next to each other with no implied space so we need the extra one.
						(sectionLayout.HasContent(parameterIndex, typeNameSeparatorColumnIndex) &&
						 sectionLayout.HasContent(parameterIndex, beforeTypeNameSeparatorColumnIndex) &&
						 sectionLayout.GetContentWidth(parameterIndex, beforeTypeNameSeparatorColumnIndex) >= beforeTypeNameSeparatorColumnWidth) ||

						// Also don't apply this tweak when the separator is text based, like SQL's "AS".  Doesn't look as good.
						(sectionLayout.GetContent(parameterIndex, typeNameSeparatorColumnIndex, out start, out end) &&
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
				if (sectionCount > 1 && alignment == ParameterGroupAlignment.AlignAllColumns)
					{
					RecalculateColumns(sectionIndex, sectionCount);
					columnLayout = GetSharedColumnLayout(sectionIndex, sectionCount);
					}
				else
					{  parameterLayouts[sectionIndex].RecalculateColumns();  }
				}


			//
			// Now check the parts before and after the parameters, such as "void FunctionName (" and ")",  to see if they
			// need spaces.
			//

			for (int i = sectionIndex; i < sectionIndex + sectionCount; i++)
				{
				var sectionLayout = parameterLayouts[i];

				(parsedPrototype.Sections[i] as Prototypes.ParameterSection).GetBeforeParameters(out start, out end);

				// Add a space before the parameters if there was an ending whitespace character that was marked as part of
				// the BeforeParameters section.  This should only happen if it was significant; it should have been excluded
				// otherwise.
				if (end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start))
					{
					sectionLayout.HasSpaceBeforeParameters = true;
					}

				// Also add one if the BeforeParameters section doesn't end with a nice symbol like (.  If it ends with something
				// like (* we want to add it anyway for legibility.  Also for {.
				else
					{
					TokenIterator beforeEnd = end;
					beforeEnd.Previous();

					bool spaceBeforeParams = (beforeEnd >= start &&
															beforeEnd.Character != '(' &&
															beforeEnd.Character != '[' &&
															beforeEnd.Character != '<');

					sectionLayout.HasSpaceBeforeParameters = spaceBeforeParams;
					}

				// Not every prototype with parameters will have an AfterParameters section, mainly Microsoft SQL functions
				// because they don't require parentheses.
				if ((parsedPrototype.Sections[i] as Prototypes.ParameterSection).GetAfterParameters(out start, out end))
					{
					// Add a space between this and the parameters if there was a leading whitespace character that was marked
					// as part of the AfterParameters section.  This should only happen if it was significant; it should have been
					// excluded otherwise.
					if (start.NextPastWhitespace(end))
						{
						sectionLayout.HasSpaceAfterParameters = true;
						}

					// Also add it if the AfterParameters section doesn't start with a nice symbol like ).  If it starts with something
					// like *) we want to add it anyway for legibility.  Also for }.
					else
						{
						bool spaceAfterParams = (start < end &&
															  start.Character != ')' &&
															  start.Character != ']' &&
															  start.Character != '>');

						sectionLayout.HasSpaceAfterParameters = spaceAfterParams;
						}
					}
				}


			if (alignment == ParameterGroupAlignment.AlignAllColumns)
				{  return columnLayout;  }
			else
				{  return null;  }
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


		/* Function: AppendSharedColumnParameterSections
		 *
		 * Builds the HTML for one or more <Prototypes.ParameterSections>.  They will always be in wide form.
		 *
		 * This function will build the output so that all the parameter sections share a single column layout.  This lets
		 * the content align across sections, such as for consecutive SystemVerilog attributes:
		 *
		 *		--- SystemVerilog Code ---
		 *		(* param1  = "abcde" *)
		 *		(* param22 = "abc"   *)
		 *		---
		 */
		protected void AppendSharedColumnParameterSections (int sectionIndex, int sectionCount, PrototypeColumnLayout columnLayout,
																						  StringBuilder output)
			{
			#if DEBUG
			if (parsedPrototype.Sections[sectionIndex] is not Prototypes.ParameterSection)
				{  throw new Exception("The indexed section isn't a parameter section.");  }
			if (parameterLayouts[sectionIndex] == null)
				{  throw new Exception("The indexed section doesn't have a generated layout.");  }
			#endif


			bool allBeforeAndAfterParametersSectionsAreShort = true;
			bool allLastCellsEndWithSpace = true;

			for (int i = sectionIndex; i < sectionIndex + sectionCount; i++)
				{
				var parameterLayout = parameterLayouts[i];

				if (parameterLayout.BeforeParametersWidth > 3 ||
					parameterLayout.AfterParametersWidth > 3)
					{  allBeforeAndAfterParametersSectionsAreShort = false;  }
				if (!parameterLayout.LastCellEndsWithSpace)
					{  allLastCellsEndWithSpace = false;  }
				}


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

			for (int i = sectionIndex; i < sectionIndex + sectionCount; i++)
				{
				var parameterContent = (parsedPrototype.Sections[i]) as Prototypes.ParameterSection;
				var parameterLayout = parameterLayouts[i];


				// Before parameters

				TokenIterator start, end;
				parameterContent.GetBeforeParameters(out start, out end);
				end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

				// If the content is short enough to fit into the left indent of the narrow prototype form stuff it in there
				// instead of creating another line that doesn't actually save any space.
				bool beforeParametersFitsIntoIndent = (parameterLayout.BeforeParametersWidth <= 3);

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

				output.Append("<div class=\"PBeforeParameters" +
												(parameterLayout.HasSpaceBeforeParameters ? " RightSpaceOnWide" : "") +
												(i > sectionIndex ? " RightAlignOnWide" : "") +
												(beforeParametersFitsIntoIndent ? " FitIntoLeftIndentOnNarrow RightAlignOnNarrow" : "") +
												(beforeParametersFitsIntoIndent && parameterLayout.HasSpaceBeforeParameters ? " RightSpaceOnNarrow" : "") + "\" " +
											"data-WideGridArea=\"" + wideGridArea + "\" " +
											"data-NarrowGridArea=\"" + narrowGridArea + "\" " +
											"style=\"grid-area:" + wideGridArea + "\">");

				AppendText_ExcludePartialKeyword(start, end, output);

				output.Append("</div>");

				if (!beforeParametersFitsIntoIndent)
					{  narrowRowStart++;  }


				// Parameters

				AppendParameters(parameterLayout, columnLayout, wideRowStart, 2, narrowRowStart, 1, output);


				// After parameters

				// Not every prototype with parameters will have an after parameters section, mainly Microsoft SQL functions
				// because they don't require parentheses.  Just omit it.
				bool hasAfterParameters = parameterContent.GetAfterParameters(out start, out end);

				if (hasAfterParameters)
					{
					start.NextPastWhitespace(end);

					// Put it in the last row, last column
					wideGridArea = (wideRowStart + Math.Max(parameterLayout.NumberOfParameters, 1) - 1) + "/" +
											(2 + columnLayout.UsedCount) + "/" +
											(wideRowStart + Math.Max(parameterLayout.NumberOfParameters, 1)) + "/" +
											(3 + columnLayout.UsedCount);

					if (allBeforeAndAfterParametersSectionsAreShort)
						{
						// If all the AfterParameters content is short enough we can leave it in the same row in the narrow form
						// instead of giving it its own row.  However, only do this when the BeforeParameters section is also short.
						// This lets things like { } and (* *) not move at all but keeps the old behavior for things like
						// "void Function (" and ")".  Also, only do this when it applies to ALL of the sections because we don't
						// want a mix of styles.
						narrowGridArea = (narrowRowStart + Math.Max(parameterLayout.NumberOfParameters, 1) - 1) + "/" +
													(2 + columnLayout.UsedCount) + "/" +
													(narrowRowStart + Math.Max(parameterLayout.NumberOfParameters, 1)) + "/" +
													(3 + columnLayout.UsedCount);
						}
					else
						{
						// Put it in the last row, all columns.  Add one more column than the parameters use so the cells don't get
						// stretched out if this is longer than them.
						narrowGridArea = (narrowRowStart + parameterLayout.NumberOfParameters) +
													"/1/" +
													(narrowRowStart + parameterLayout.NumberOfParameters + 1) + "/" +
													(1 + columnLayout.UsedCount + 1);
						}

					string extraCSSClass;

					if (parameterLayout.HasSpaceAfterParameters)
						{
						// We only need to actually add the space if the last parameter doesn't already have one at the end.
						// Otherwise ignore it so it's not overly wide.
						extraCSSClass = (allLastCellsEndWithSpace ? "" : " LeftSpaceOnWide") +
												 (allBeforeAndAfterParametersSectionsAreShort ? " FitIntoRightIndentOnNarrow LeftSpaceOnNarrow" : "");
						}
					else
						{
						// On the other hand, if there's not supposed to be a space and the last parameter ends with one anyway
						// we can bleed the ending part an extra character into it.  This lets closing parentheses line up with the
						// commas in between parameters.
						extraCSSClass = (allLastCellsEndWithSpace ? " NegativeLeftSpaceOnWide" : "") +
												 (allBeforeAndAfterParametersSectionsAreShort ? " FitIntoRightIndentOnNarrow" : "");
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
				narrowRowStart += parameterLayout.NumberOfParameters;

				if (hasAfterParameters)
					{  narrowRowStart++;  }
				}


			// Closing tags

			output.Append("</div></div>");
			}


		/* Function: AppendLeftAlignedParameterSections
		 *
		 * Builds the HTML for one or more <Prototypes.ParameterSections>.  They will always be in wide form.
		 *
		 * This function will build the output so that each parameter section will be aligned to the BeforeParameters
		 * section of the first one.  However, their columns layouts will remain independent.  For example:
		 *
		 *		--- C# Code --
		 *		int Indexer [unsigned int x,
		 *		                       int y ]
		 *		           { get,
		 *		             set  }
		 *		---
		 */
		protected void AppendLeftAlignedParameterSections (int sectionIndex, int sectionCount, StringBuilder output)
			{
			#if DEBUG
			if (parsedPrototype.Sections[sectionIndex] is not Prototypes.ParameterSection)
				{  throw new Exception("The indexed section isn't a parameter section.");  }
			if (parameterLayouts[sectionIndex] == null)
				{  throw new Exception("The indexed section doesn't have a generated layout.");  }
			#endif


			bool allBeforeAndAfterParametersSectionsAreShort = true;

			for (int i = sectionIndex; i < sectionIndex + sectionCount; i++)
				{
				var parameterLayout = parameterLayouts[i];

				if (parameterLayout.BeforeParametersWidth > 3 ||
					parameterLayout.AfterParametersWidth > 3)
					{
					allBeforeAndAfterParametersSectionsAreShort = false;
					break;
					}
				}


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


			// There's always 2 wide columns: before parameters and the cell for the nested grid
			// There's always 1 narrow column for everything
			output.Append("<div class=\"PParameterCells\" " +
										"data-WideColumnCount=\"2\" " +
										"data-NarrowColumnCount=\"1\">");


			// Sections

			int wideRowStart = 1;
			int narrowRowStart = 1;

			for (int i = sectionIndex; i < sectionIndex + sectionCount; i++)
				{
				var parameterContent = (parsedPrototype.Sections[i]) as Prototypes.ParameterSection;
				var parameterLayout = parameterLayouts[i];


				// Before parameters

				TokenIterator start, end;
				parameterContent.GetBeforeParameters(out start, out end);
				end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

				// If the content is short enough to fit into the left indent of the narrow prototype form stuff it in there
				// instead of creating another line that doesn't actually save any space.
				bool beforeParametersFitsIntoIndent = (parameterLayout.BeforeParametersWidth <= 3);

				// The order for grid-area is grid-row-start/grid-column-start/grid-row-end/grid-column-end
				string wideGridArea = wideRowStart +
												"/1/" +
												(wideRowStart + 1) +
												"/2";

				string narrowGridArea = narrowRowStart +
													"/1/" +
													(narrowRowStart + 1) +
													"/2";

				output.Append("<div class=\"PBeforeParameters" +
												(parameterLayout.HasSpaceBeforeParameters ? " RightSpaceOnWide" : "") +
												(i > sectionIndex ? " RightAlignOnWide" : "") +
												(beforeParametersFitsIntoIndent ? " FitIntoLeftIndentOnNarrow RightAlignOnNarrow" : "") +
												(beforeParametersFitsIntoIndent && parameterLayout.HasSpaceBeforeParameters ? " RightSpaceOnNarrow" : "") + "\" " +
											"data-WideGridArea=\"" + wideGridArea + "\" " +
											"data-NarrowGridArea=\"" + narrowGridArea + "\" " +
											"style=\"grid-area:" + wideGridArea + "\">");

				AppendText_ExcludePartialKeyword(start, end, output);

				output.Append("</div>");

				if (!beforeParametersFitsIntoIndent)
					{  narrowRowStart++;  }


				// Nested cell container

				wideGridArea = wideRowStart +
										"/2/" +
										(wideRowStart + 1) +
										"/3";

				narrowGridArea = narrowRowStart +
										   "/1/" +
										   (narrowRowStart + 1) +
										   "/2";

				// Need one extra column in case the containing cell is wider than the nested grid.  If we didn't have it other
				// columns would stretch to fill the horizontal space.
				int nestedWideColumnCount = parameterLayout.Columns.UsedCount + 2;
				int nestedNarrowColumnCount = parameterLayout.Columns.UsedCount + 1;

				output.Append("<div class=\"PParameterCells Nested\" " +
											"data-WideGridArea=\"" + wideGridArea + "\" " +
											"data-NarrowGridArea=\"" + narrowGridArea + "\" " +
											"style=\"grid-area:" + wideGridArea + "\" " +
											"data-WideColumnCount=\"" + nestedWideColumnCount + "\" " +
											"data-NarrowColumnCount=\"" + nestedNarrowColumnCount + "\">");


				// Parameters

				AppendParameters(parameterLayout, parameterLayout.Columns, 1, 1, 1, 1, output);


				// After parameters

				// Not every prototype with parameters will have an after parameters section, mainly Microsoft SQL functions
				// because they don't require parentheses.  Just omit it.
				bool hasAfterParameters = parameterContent.GetAfterParameters(out start, out end);

				if (hasAfterParameters)
					{
					start.NextPastWhitespace(end);

					// Put it in the last row, last column
					wideGridArea = (1 + Math.Max(parameterLayout.NumberOfParameters, 1) - 1) + "/" +
											(1 + parameterLayout.Columns.UsedCount) + "/" +
											(1 + Math.Max(parameterLayout.NumberOfParameters, 1)) + "/" +
											(2 + parameterLayout.Columns.UsedCount);

					if (allBeforeAndAfterParametersSectionsAreShort)
						{
						// If all the AfterParameters content is short enough we can leave it in the same row in the narrow form
						// instead of giving it its own row.  However, only do this when the BeforeParameters section is also short.
						// This lets things like { } and (* *) not move at all but keeps the old behavior for things like
						// "void Function (" and ")".  Also, only do this when it applies to ALL of the sections because we don't
						// want a mix of styles.
						narrowGridArea = wideGridArea;
						}
					else
						{
						// Put it in the last row, all columns.  Add one more column than the parameters use so the cells don't get
						// stretched out if this is longer than them.
						narrowGridArea = (1 + parameterLayout.NumberOfParameters) +
													"/1/" +
													(1 + parameterLayout.NumberOfParameters + 1) + "/" +
													(1 + parameterLayout.Columns.UsedCount + 1);
						}

					string extraCSSClass;

					if (parameterLayout.HasSpaceAfterParameters)
						{
						// We only need to actually add the space if the last parameter doesn't already have one at the end.
						// Otherwise ignore it so it's not overly wide.
						extraCSSClass = (parameterLayout.LastCellEndsWithSpace ? "" : " LeftSpaceOnWide") +
												 (allBeforeAndAfterParametersSectionsAreShort ? " FitIntoRightIndentOnNarrow LeftSpaceOnNarrow" : "");
						}
					else
						{
						// On the other hand, if there's not supposed to be a space and the last parameter ends with one anyway
						// we can bleed the ending part an extra character into it.  This lets closing parentheses line up with the
						// commas in between parameters.
						extraCSSClass = (parameterLayout.LastCellEndsWithSpace ? " NegativeLeftSpaceOnWide" : "") +
												 (allBeforeAndAfterParametersSectionsAreShort ? " FitIntoRightIndentOnNarrow" : "");
						}

					output.Append("<div class=\"PAfterParameters" + extraCSSClass + "\" " +
												"data-WideGridArea=\"" + wideGridArea + "\" " +
												"data-NarrowGridArea=\"" + narrowGridArea + "\" " +
												"style=\"grid-area:" + wideGridArea + "\">");

					AppendText(start, end, output);

					output.Append("</div>");
					}


				// Close nested cell container

				output.Append("</div>");


				// Update position for next section

				wideRowStart++;
				narrowRowStart++;
				}


			// Closing tags

			output.Append("</div></div>");
			}


		/* Function: AppendParameters
		 */
		protected void AppendParameters (PrototypeParameterLayout parameters, PrototypeColumnLayout columnLayout, int wideRowStart,
														  int wideColumnStart, int narrowRowStart, int narrowColumnStart, StringBuilder output)
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
															 (wideColumnStart + columnLayout.UsedColumnIndexOf(columnIndex)) + "/" +
															 (wideRowStart + parameterIndex + 1) + "/" +
															 (wideColumnStart + columnLayout.UsedColumnIndexOf(columnIndex) + 1);

							string narrowGridArea = (narrowRowStart + parameterIndex) + "/" +
																(narrowColumnStart + columnLayout.UsedColumnIndexOf(columnIndex)) + "/" +
																(narrowRowStart + parameterIndex + 1) + "/" +
																(narrowColumnStart + columnLayout.UsedColumnIndexOf(columnIndex) + 1);

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
