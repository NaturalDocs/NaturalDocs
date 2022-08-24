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

			string parameterClass;

			switch (section.ParameterStyle)
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
			section.GetBeforeParameters(out start, out end);

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

			AppendText_ExcludePartialKeyword(start, end, output);

			if (addNBSP)
				{  output.Append("&nbsp;");  }

			output.Append("</div>");


			// Parameters

			output.Append("<div class=\"PParametersParentCell\">");
			AppendParameters(parameters, output);
			output.Append("</div>");


			// After parameters

			section.GetAfterParameters(out start, out end);

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

			AppendText(start, end, output);

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
		protected void AppendParameters (PrototypeParameters parameters, StringBuilder output)
			{
			int firstUsedColumn = parameters.Columns.FirstUsed;
			int lastUsedColumn = parameters.Columns.LastUsed;

			output.Append("<table class=\"PParameters\">");

			for (int parameterIndex = 0; parameterIndex < parameters.Count; parameterIndex++)
				{
				output.Append("<tr>");

				for (int columnIndex = firstUsedColumn; columnIndex <= lastUsedColumn; columnIndex++)
					{
					if (parameters.Columns.IsUsed(columnIndex))
						{
						string extraClass = null;

						if (columnIndex == firstUsedColumn && columnIndex == lastUsedColumn)
							{  extraClass = "first last";  }
						else if (columnIndex == firstUsedColumn)
							{  extraClass = "first";  }
						else if (columnIndex == lastUsedColumn)
							{  extraClass = "last";  }

						if (parameters.HasContentAt(parameterIndex, columnIndex))
							{
							if (extraClass == null)
								{  output.Append("<td></td>");  }
							else
								{  output.Append("<td class=\"" + extraClass + "\"></td>");  }
							}
						else
							{
							output.Append("<td class=\"P" + parameters.Columns.TypeOf(columnIndex).ToString() + (extraClass != null ? ' ' + extraClass : "") + "\">");

							AppendParameterColumn(parameters, parameterIndex, columnIndex, output);

							output.Append("</td>");
							}
						}
					}

				output.Append("</tr>");
				}

			output.Append("</table>");
			}


		/* Function: AppendParameterColumn
		 */
		protected void AppendParameterColumn (PrototypeParameters parameters, int parameterIndex, int columnIndex, StringBuilder output)
			{
			TokenIterator start, end;
			parameters.GetContentAt(parameterIndex, columnIndex, out start, out end);

			bool hadTrailingWhitespace = end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

			PrototypeColumnType type = parameters.Columns.TypeOf(columnIndex);

			// Find the type of the next used cell
			PrototypeColumnType? nextType = null;
			for (int nextCellIndex = columnIndex + 1; nextCellIndex < parameters.Columns.Count; nextCellIndex++)
				{
				if (parameters.Columns.IsUsed(nextCellIndex))
					{
					nextType = parameters.Columns.TypeOf(nextCellIndex);
					break;
					}
				}

			// Default value separators always get spaces before.
			// Property value separators get them unless they're ":", but watch out for ":=".
			// Type-name separators get them if they're text (SQL's "AS") instead of symbols (Pascal's ":").
			if (type == PrototypeColumnType.DefaultValueSeparator ||
				(type == PrototypeColumnType.PropertyValueSeparator && (start.Character != ':' || start.MatchesAcrossTokens(":="))) ||
				(type == PrototypeColumnType.TypeNameSeparator && start.FundamentalType == FundamentalType.Text))
				{  output.Append("&nbsp;");  }

			if (addLinks)
				{  AppendSyntaxHighlightedTextWithTypeLinks(start, end, output, links, linkTargets, extendTypeSearch: true);  }
			else
				{  AppendSyntaxHighlightedText(start, end, output);  }

			// Default value separators, property value separators, and type/name separators always get spaces after.  Make sure
			// the spaces won't be duplicated by the following cell.
			if (type == PrototypeColumnType.DefaultValueSeparator ||
				type == PrototypeColumnType.PropertyValueSeparator ||
				type == PrototypeColumnType.TypeNameSeparator ||
				(hadTrailingWhitespace &&
					type != PrototypeColumnType.DefaultValue &&
					nextType != PrototypeColumnType.DefaultValueSeparator &&
					nextType != PrototypeColumnType.PropertyValueSeparator &&
					nextType != PrototypeColumnType.TypeNameSeparator) )
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
