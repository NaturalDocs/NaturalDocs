/*
 * Class: CodeClear.NaturalDocs.Engine.Comments.Javadoc.Parser
 * ____________________________________________________________________________
 *
 * A parser to handle the Javadoc comment format.
 *
 *
 * Topic: Tag Support
 *
 *		Supported Javadoc Tags:
 *
 *			- @author
 *			- @deprecated
 *			- @exception
 *			- @param
 *			- @return
 *			- @see
 *			- @since
 *			- @throws
 *			- @version
 *			- {@code}
 *			- {@link}
 *			- {@linkPlain}
 *			- {@literal}
 *
 *		Supported HTML Tags:
 *
 *			- p
 *			- b, i, u, strong (converted to b), em (converted to i)
 *			- pre
 *			- ul, ol (converted to ul), li
 *			- a href for absolute URLs and e-mail addresses
 *
 *		Unsupported:
 *
 *			- @serial
 *			- @serialField
 *			- @serialData
 *			- {@docRoot}
 *			- {@inheritDoc}
 *			- {@value}
 *				- If a target is provided, it will be added as a link.
 *			- Custom Javadoc tags
 *			- a href with relative or {@docRoot} URLs
 *				- These links are removed since they likely depend on the Javadoc output file structure.
 *			- Mixed left lines
 *				- Natural Docs supports either having a horizontal line on the left side of the comment or not
 *				   having one.  Javadoc also supports having the left line partially exist so that you can have a
 *				   line of stars normally but still paste a code segment in without reformatting it.  Natural Docs
 *				   does not support this.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Comments.Components;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Comments.Javadoc
	{
	public class Parser : Comments.Parser
		{

		// Group: Types
		// __________________________________________________________________________


		public enum GetTextMode : byte
			{  Normal, ListItem  }



		// Group: Functions
		// __________________________________________________________________________


		/* Function: Parser
		 */
		public Parser (Comments.Manager manager) : base (manager)
			{
			}


		/* Function: Parse
		 *
		 * Attempts to parse the passed comment into <Topics>.  Returns whether it was successful, and if so, adds them
		 * to the list.  These fields will be set:
		 *
		 *		- CommentLineNumber
		 *		- Body, if present
		 *		- Summary, if available
		 */
		public bool Parse (PossibleDocumentationComment sourceComment, List<Topic> topics)
			{
			if (HasAnyTag(sourceComment.Start.FirstToken(LineBoundsMode.CommentContent),
								sourceComment.End.FirstToken(LineBoundsMode.Everything)) == false)
				{  return false;  }

			LineIterator firstBlockLine = sourceComment.Start;

			while (firstBlockLine < sourceComment.End && IsFirstBlockLine(firstBlockLine) == false)
				{  firstBlockLine.Next();  }

			JavadocComment parsedComment = new JavadocComment();

			if (sourceComment.Start < firstBlockLine)
				{  parsedComment.Description = GetText(sourceComment.Start, firstBlockLine);  }

			while (firstBlockLine < sourceComment.End)
				{
				// There may be unrecognized blocks so we have to manually advance if GetBlockTag() fails.
				if (!TryToGetBlock(ref firstBlockLine, sourceComment.End, parsedComment))
					{  firstBlockLine.Next();  }
				}

			Topic topic = GenerateTopic(parsedComment);

			if (topic != null)
				{
				topic.CommentLineNumber = sourceComment.Start.LineNumber;
				topics.Add(topic);
				}

			return true;
			}


		/* Function: HasAnyTag
		 * Whether the passed block of text contains any Javadoc tags at all.
		 */
		protected bool HasAnyTag (TokenIterator start, TokenIterator end)
			{
			string text = start.Tokenizer.RawText;
			int endIndex = end.RawTextIndex;

			int textIndex = text.IndexOf('@', start.RawTextIndex, endIndex - start.RawTextIndex);

			while (textIndex != -1)
				{
				start.RawTextIndex = textIndex;
				start.Next();

				if (textIndex > 0 && text[textIndex - 1] == '{')
					{
					if (InlineTags.Contains(start.String))
						{  return true;  }
					}
				else
					{
					if (BlockTags.Contains(start.String))
						{  return true;  }
					}

				textIndex = text.IndexOf('@', textIndex + 1, endIndex - (textIndex + 1));
				}

			return false;
			}


		/* Function: IsFirstBlockLine
		 * Whether the iterator is on a line that starts with one of the <BlockTags>.
		 */
		protected bool IsFirstBlockLine (LineIterator lineIterator)
			{
			// For simplicity we'll just hand off to TryToGetFirstBlockLine() and ignore the extracted content.
			string ignoreTag;
			TokenIterator ignoreStartOfContent;
			return TryToGetFirstBlockLine(lineIterator, out ignoreTag, out ignoreStartOfContent);
			}


		/* Function: TryToGetFirstBlockLine
		 * If the iterator is on a line that starts with one of the <BlockTags>, extracts the components and returns true.
		 * Use <GetBlockTag()> to get the complete block since it may span multiple lines.
		 */
		protected bool TryToGetFirstBlockLine (LineIterator lineIterator, out string tag, out TokenIterator startOfContent)
			{
			tag = null;
			startOfContent = default(TokenIterator);

			TokenIterator tokenIterator = lineIterator.FirstToken(LineBoundsMode.CommentContent);

			if (tokenIterator.Character != '@')
				{  return false;  }

			tokenIterator.Next();

			if (tokenIterator.FundamentalType != FundamentalType.Text)
				{  return false;  }

			string possibleTag = tokenIterator.String;

			if (BlockTags.Contains(possibleTag) == false)
				{  return false;  }

			tokenIterator.Next();
			tokenIterator.NextPastWhitespace();

			tag = possibleTag;
			startOfContent = tokenIterator;
			return true;
			}


		/* Function: TryToGetBlock
		 * If the iterator is on a line that starts with one of the <BlockTags>, parses it, adds its content to the comment,
		 * moves the iterator past it, and returns true.  If it is not at the start of a tag block it will return false and change
		 * nothing.
		 */
		protected bool TryToGetBlock (ref LineIterator lineIterator, LineIterator limit, JavadocComment comment)
			{
			if (lineIterator >= limit)
				{  return false;  }


			// Get the complete content across multiple lines.

			string tag;
			TokenIterator startOfContent;

			if (TryToGetFirstBlockLine(lineIterator, out tag, out startOfContent) == false)
				{  return false;  }

			for (;;)
				{
				lineIterator.Next();

				if (lineIterator >= limit || IsFirstBlockLine(lineIterator))
					{  break;  }
				}

			TokenIterator endOfContent = lineIterator.FirstToken(LineBoundsMode.Everything);


			// Any "@tag item description", possibly in a list
			if (tag == "exception" ||
				tag == "param" ||
				tag == "throws")
				{
				TokenIterator iterator = startOfContent;

				string symbol = null;
				TryToGetBlockSymbol(ref iterator, endOfContent, out symbol);
				iterator.NextPastWhitespace();

				string description = GetText(iterator, endOfContent);
				description = NormalizeNDMarkup(description);

				if (symbol == null || symbol == "" || description == null || description == "")
					{  return false;  }

				var listSection = comment.GetOrCreateListSection(tag);
				listSection.AddMember(symbol, description);
				return true;
				}

			// Any "@tag description", possibly in a list
			else if (tag == "author" ||
					  tag == "deprecated" ||
					  tag == "since" ||
					  tag == "version")
				{
				string description = GetText(startOfContent, endOfContent);
				description = NormalizeNDMarkup(description);

				if (description == null || description == "")
					{  return false;  }

				if (tag == "deprecated")
					{
					if (comment.Deprecated == null)
						{  comment.Deprecated = description;  }
					else
						{  comment.Deprecated += description;  }
					}
				else
					{
					var listSection = comment.GetOrCreateListSection(tag);
					listSection.AddMember(null, description);
					}

				return true;
				}

			// Any "@tag description" that can't be in a list
			else if (tag == "return")
				{
				string description = GetText(startOfContent, endOfContent);
				description = NormalizeNDMarkup(description);

				if (description == null || description == "")
					{  return false;  }

				var textSection = comment.GetOrCreateTextSection(tag);
				textSection.Append(description);
				return true;
				}

			else if (tag == "see")
				{
				string description = null;
				TokenIterator iterator = startOfContent;

				// @see "Description"
				// @see <a href="link">Description</a>
				if (iterator.Character == '"' ||
					iterator.Character == '<')
					{
					// There's not symbol so interpret the whole thing as the description.  We'll let GetText() handle the HTML link.
					description = GetText(iterator, endOfContent);
					description = NormalizeNDMarkup(description);
					}

				// @see Class.Class#Member
				// @see Class.Class#Member Description
				else
					{
					string symbol = GetJavadocLinkSymbol(ref iterator);
					iterator.NextPastWhitespace();

					description = GetSimpleText(iterator, endOfContent);
					description = NormalizeNDMarkup(description);

					if (description == null || description == "")
						{  description = "<p><link type=\"naturaldocs\" originaltext=\"" + symbol.EntityEncode() + "\"></p>";  }
					else
						{  description = "<p><link type=\"naturaldocs\" originaltext=\"" + description.EntityEncode() + " at " + symbol.EntityEncode() + "\"></p>";  }
					}

				if (description == null || description == "")
					{  return false;  }

				var listSection = comment.GetOrCreateListSection(tag);
				listSection.AddMember(null, description);
				return true;
				}

			// Ignored blocks
			// - serial
			// - serialField
			// - serialData
			else
				{  return true;  }
			}


		/* Function: TryToGetBlockSymbol
		 * If the iterator is on the symbol part of a block tag that has one, such as "@param symbol description", extracts the symbol,
		 * moves the iterator past it, and returns true.
		 */
		protected bool TryToGetBlockSymbol (ref TokenIterator iterator, TokenIterator limit, out string entryText)
			{
			TokenIterator lookahead = iterator;

			// Javadoc recommends documenting template parameters as "@param <T> ...".
			if (lookahead.Character == '<')
				{
				lookahead.Next();

				for (;;)
					{
					if (lookahead >= limit)
						{
						entryText = null;
						return false;
						}
					else if (lookahead.Character == '>')
						{
						lookahead.Next();
						break;
						}
					else
						{  lookahead.Next();  }
					}
				}

			else // not '<'
				{
				for (;;)
					{
					if (lookahead >= limit)
						{
						entryText = null;
						return false;
						}
					else if (lookahead.FundamentalType == FundamentalType.Text ||
							  lookahead.Character == '_' ||
							  lookahead.Character == '.')
						{
						lookahead.Next();
						}
					else if (lookahead.MatchesAcrossTokens("::") ||
							  lookahead.MatchesAcrossTokens("->"))
						{
						lookahead.NextByCharacters(2);
						}
					else
						{  break;  }
					}
				}

			if (lookahead >= limit || lookahead.FundamentalType != FundamentalType.Whitespace)
				{
				entryText = null;
				return false;
				}

			entryText = iterator.TextBetween(lookahead);
			iterator = lookahead;
			return true;
			}


		/* Function: GetText
		 */
		protected string GetText (LineIterator start, LineIterator end)
			{
			return GetText(start.FirstToken(LineBoundsMode.Everything), end.FirstToken(LineBoundsMode.Everything));
			}


		/* Function: GetText
		 */
		protected string GetText (TokenIterator start, TokenIterator end)
			{
			JavadocIterator iterator = new JavadocIterator(start, end);
			StringBuilder output = new StringBuilder();
			GetText(ref iterator, output);
			return output.ToString();
			}


		/* Function: GetText
		 *
		 * Converts a stretch of formatted text to NDMarkup.
		 *
		 * Modes:
		 *
		 *		Normal - The iterator continues until it goes out of bounds.
		 *		ListItem - The iterator continues until it reaches a closing li tag.  It also skips certain formatting that is not supported
		 *					  in list items in NDMarkup.
		 */
		protected void GetText (ref JavadocIterator iterator, StringBuilder output, GetTextMode mode = GetTextMode.Normal)
			{
			output.Append("<p>");

			TagStack tagStack = new TagStack();
			tagStack.OpenTag(null, "</p>");

			while (iterator.IsInBounds)
			    {

			    if (iterator.IsOn(JavadocElementType.Text))
			        {
			        output.EntityEncodeAndAppend(iterator.String);
			        iterator.Next();
			        }

			    else if (iterator.IsOn(JavadocElementType.EntityChar))
			        {
			        output.EntityEncodeAndAppend(iterator.EntityValue);
			        iterator.Next();
			        }

			    else if (iterator.IsOn(JavadocElementType.LineBreak))
			        {
			        // Add a literal line break.  We'll replace these with spaces or double spaces later.  Right now we can't decide
			        // which it should be because you can't run a regex directly on a StringBuilder and it would be inefficient to convert
			        // it to a string on every line break.
			        output.Append('\n');

			        iterator.Next();
			        }

				else if (iterator.IsOnHTMLTag("p"))
					{
					// Text can appear both inside and outside of <p> tags, whitespace can appear between <p> tags that can be
					// mistaken for content, and people can use <p> tags as standalone rather than opening tags.  Rather than put in
					// logic to try to account for all of this we handle it in a very dirty but simple way.  Every <p> tag--opening, closing,
					// or standalone--causes a paragraph break.  Normalize() will clean it up for us afterwards.

					tagStack.CloseTag(1, output);  // Reuse our surrounding tag
					output.Append("</p><p>");
					iterator.Next();
					}

				else if (iterator.IsOnHTMLTag("b") ||
						  iterator.IsOnHTMLTag("strong"))
					{
					if (iterator.HTMLTagForm == TagForm.Opening)
						{
						tagStack.OpenTag(iterator.TagType, "</b>");
						output.Append("<b>");
						}
					else if (iterator.HTMLTagForm == TagForm.Closing)
						{
						tagStack.CloseTag(iterator.TagType, output);
						}

					iterator.Next();
					}

				else if (iterator.IsOnHTMLTag("i") ||
						  iterator.IsOnHTMLTag("em"))
					{
					if (iterator.HTMLTagForm == TagForm.Opening)
						{
						tagStack.OpenTag(iterator.TagType, "</i>");
						output.Append("<i>");
						}
					else if (iterator.HTMLTagForm == TagForm.Closing)
						{
						tagStack.CloseTag(iterator.TagType, output);
						}

					iterator.Next();
					}

				else if (iterator.IsOnHTMLTag("u"))
					{
					if (iterator.HTMLTagForm == TagForm.Opening)
						{
						tagStack.OpenTag(iterator.TagType, "</u>");
						output.Append("<u>");
						}
					else if (iterator.HTMLTagForm == TagForm.Closing)
						{
						tagStack.CloseTag(iterator.TagType, output);
						}

					iterator.Next();
					}

				else if (iterator.IsOnHTMLTag("pre", TagForm.Opening) && mode == GetTextMode.Normal)  // Ignore pre's in list items
					{
					output.Append("</p>");
					GetPre(ref iterator, output);
					output.Append("<p>");
					}

				else if (iterator.IsOnHTMLTag("ul") ||
						  iterator.IsOnHTMLTag("ol"))
					{
					if (iterator.HTMLTagForm == TagForm.Opening)
						{
						output.Append("</p>");
						GetList(ref iterator, output);
						output.Append("<p>");
						}
					else if (iterator.HTMLTagForm == TagForm.Closing && mode == GetTextMode.ListItem)
						{  break;  }
					else
						{  iterator.Next();  }
					}

				else if (iterator.IsOnHTMLTag("li", TagForm.Closing) && mode == GetTextMode.ListItem)
					{
					break;
					}

				else if (iterator.IsOnHTMLTag("a", TagForm.Opening))
					{
					string href = iterator.HTMLTagProperty("href");

					if (href == null || href == "" || href == "#" || href.StartsWith("{@docRoot}") ||
						href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
						{  iterator.Next();  }
					else
						{  GetHTMLLink(ref iterator, output);  }
					}

				else if (iterator.IsOnJavadocTag("code") ||
						  iterator.IsOnJavadocTag("literal"))
					{
					// These get added without searching the contents for nested tags
					output.EntityEncodeAndAppend(iterator.JavadocTagValue);
					iterator.Next();
					}

				else if (iterator.IsOnJavadocTag("link") ||
						  iterator.IsOnJavadocTag("linkPlain"))
					{
					Tokenizer linkContent = new Tokenizer(iterator.JavadocTagValue);
					TokenIterator linkIterator = linkContent.FirstToken;

					string symbol = GetJavadocLinkSymbol(ref linkIterator);
					linkIterator.NextPastWhitespace();

					string description = GetSimpleText(linkIterator, linkContent.EndOfTokens);
					description = NormalizeNDMarkup(description);

					if (description == null || description == "")
						{
						output.Append("<link type=\"naturaldocs\" originaltext=\"");
						output.EntityEncodeAndAppend(symbol);
						output.Append("\">");
						}
					else
						{
						output.Append("<link type=\"naturaldocs\" originaltext=\"");
						output.EntityEncodeAndAppend(description);
						output.Append(" at ");
						output.EntityEncodeAndAppend(symbol);
						output.Append("\">");
						}

					iterator.Next();
					}

				else if (iterator.IsOnJavadocTag("value"))
					{
					string symbol = iterator.JavadocTagValue;

					if (symbol == null || symbol == "")
						{
						output.EntityEncodeAndAppend(
							Locale.Get("NaturalDocs.Engine", "Javadoc.Substitution.value")
							);
						}
					else
						{
						string substitution = Locale.Get("NaturalDocs.Engine", "Javadoc.Substitution.value(symbol)", '\x1F');
						int substitutionIndex = substitution.IndexOf('\x1F');

						if (substitutionIndex == -1)
							{  output.EntityEncodeAndAppend(substitution);  }
						else
							{
							if (substitutionIndex > 0)
								{  output.EntityEncodeAndAppend(substitution, 0, substitutionIndex);  }

							output.Append("<link type=\"naturaldocs\" originaltext=\"");
							output.EntityEncodeAndAppend(symbol);
							output.Append("\">");

							if (substitutionIndex < substitution.Length - 1)
								{  output.EntityEncodeAndAppend(substitution, substitutionIndex + 1, substitution.Length - (substitutionIndex + 1));  }
							}
						}

					iterator.Next();
					}

			    else
			        {
			        // Ignore indent.  Spaces between words will be handled by line breaks.
					// Ignore HTML comments.
					// Ignore unrecognized HTML tags.
			        iterator.Next();
			        }
			    }

			tagStack.CloseAllTags(output);
			}


		/* Function: GetSimpleText
		 */
		protected string GetSimpleText (TokenIterator start, TokenIterator end)
			{
			JavadocIterator iterator = new JavadocIterator(start, end);
			StringBuilder output = new StringBuilder();
			GetSimpleText(ref iterator, output);
			return output.ToString();
			}


		/* Function: GetSimpleText
		 * Converts a stretch of text to NDMarkup, ignoring the formatting.  Unlike <GetText()> this will not surround the output
		 * in paragraph tags.
		 */
		protected void GetSimpleText (ref JavadocIterator iterator, StringBuilder output)
			{
			while (iterator.IsInBounds)
			    {
			    if (iterator.IsOn(JavadocElementType.Text))
			        {
			        output.EntityEncodeAndAppend(iterator.String);
			        iterator.Next();
			        }

			    else if (iterator.IsOn(JavadocElementType.EntityChar))
			        {
			        output.EntityEncodeAndAppend(iterator.EntityValue);
			        iterator.Next();
			        }

			    else if (iterator.IsOn(JavadocElementType.LineBreak))
			        {
			        output.Append('\n');
			        iterator.Next();
			        }

				else if (iterator.IsOnJavadocTag("code") ||
						  iterator.IsOnJavadocTag("literal"))
					{
					output.EntityEncodeAndAppend(iterator.JavadocTagValue);
					iterator.Next();
					}

				else if (iterator.IsOnJavadocTag("link") ||
						  iterator.IsOnJavadocTag("linkPlain"))
					{
					Tokenizer linkContent = new Tokenizer(iterator.JavadocTagValue);
					TokenIterator linkIterator = linkContent.FirstToken;

					string symbol = GetJavadocLinkSymbol(ref linkIterator);
					linkIterator.NextPastWhitespace();

					string description = GetSimpleText(linkIterator, linkContent.EndOfTokens);
					description = NormalizeNDMarkup(description);

					if (description == null || description == "")
						{  output.EntityEncodeAndAppend(symbol);  }
					else
						{  output.EntityEncodeAndAppend(description);  }

					iterator.Next();
					}

				else if (iterator.IsOnJavadocTag("value"))
					{
					string symbol = iterator.JavadocTagValue;

					if (symbol == null || symbol == "")
						{
						output.EntityEncodeAndAppend(
							Locale.Get("NaturalDocs.Engine", "Javadoc.Substitution.value")
							);
						}
					else
						{
						output.EntityEncodeAndAppend(
							Locale.Get("NaturalDocs.Engine", "Javadoc.Substitution.value(symbol)", symbol)
							);
						}

					iterator.Next();
					}

			    else
			        {
			        // Ignore indent.  Spaces between words will be handled by line breaks.
					// Ignore HTML comments.
					// Ignore HTML tags.
			        iterator.Next();
			        }
			    }
			}


		/* Function: GetPre
		 * Converts the contents of a pre tag to NDMarkup and adds it to the output.  The iterator should be on an opening pre tag
		 * and when it ends it will be past the closing tag.
		 */
		protected void GetPre (ref JavadocIterator iterator, StringBuilder output)
			{
			#if DEBUG
			if (iterator.IsOnHTMLTag("pre", TagForm.Opening) == false)
				{  throw new Exception("GetPre() can only be called when the iterator is on an opening pre tag.");  }
			#endif

			output.Append("<pre type=\"code\">");

			iterator.Next();

			List<CodeLine> lines = new List<CodeLine>();

			CodeLine currentLine = new CodeLine();
			currentLine.Indent = -1;  // Don't use text immediately following the pre tag to figure out the shared indent.
			currentLine.Text = null;

			for (;;)
				{
				if (iterator.IsInBounds == false)
					{
					lines.Add(currentLine);
					break;
					}
				else if (iterator.IsOn(JavadocElementType.HTMLTag))
					{
					if (iterator.IsOnHTMLTag("pre", TagForm.Closing))
						{
						lines.Add(currentLine);
						break;
						}
					// Otherwise ignore it since we don't support tags embedded in code.
					}
				else if (iterator.IsOn(JavadocElementType.LineBreak))
					{
					lines.Add(currentLine);

					currentLine = new CodeLine();
					currentLine.Indent = 0;
					currentLine.Text = null;
					}
				else if (iterator.IsOn(JavadocElementType.Indent))
					{
					currentLine.Indent = iterator.Indent;
					}
				else if (iterator.IsOn(JavadocElementType.EntityChar))
					{
					currentLine.Text += iterator.EntityValue;
					}
				else if (iterator.IsOn(JavadocElementType.Text))
					{
					if (currentLine.Text == null)
						{  currentLine.Text = iterator.String;  }
					else
						{  currentLine.Text += iterator.String;  }
					}

				iterator.Next();
				}

			NormalizeCodeLines(lines);


			// Build the output.

			for (int i = 0; i < lines.Count; i++)
				{
				if (lines[i].Indent >= 1)
					{  output.Append(' ', lines[i].Indent);  }

				if (lines[i].Text != null)
					{  output.EntityEncodeAndAppend(lines[i].Text);  }

				if (i < lines.Count - 1)
					{  output.Append("<br>");  }
				}

			output.Append("</pre>");
			}


		/* Function: GetList
		 * Converts the contents of a ul or ol list to NDMarkup and adds it to the output.  The iterator should be on an opening list tag
		 * and when it ends it will be past the closing tag.
		 */
		protected void GetList (ref JavadocIterator iterator, StringBuilder output)
			{
			#if DEBUG
			if (iterator.IsOnHTMLTag("ul", TagForm.Opening) == false &&
				iterator.IsOnHTMLTag("ol", TagForm.Opening) == false)
				{  throw new Exception("GetList() can only be called when the iterator is on an opening ol or ul tag.");  }
			#endif

			output.Append("<ul>");
			string listTagType = iterator.TagType;

			iterator.Next();

			for (;;)
				{
				if (iterator.IsInBounds == false)
					{  break;  }
				else if (iterator.IsOnHTMLTag(listTagType, TagForm.Closing))
					{  break;  }
				else if (iterator.IsOnHTMLTag("li", TagForm.Opening))
					{
					output.Append("<li>");
					GetText(ref iterator, output, GetTextMode.ListItem);
					output.Append("</li>");

					// There's a chance it stopped on something other than the closing li tag so we have to check before advancing the iterator.
					if (iterator.IsOnHTMLTag("li", TagForm.Closing))
						{  iterator.Next();  }
					}
				else
					{  iterator.Next();  }
				}

			if (iterator.IsOnHTMLTag(listTagType, TagForm.Closing))
				{  iterator.Next();  }

			output.Append("</ul>");
			}


		/* Function: GetHTMLLink
		 * Converts the contents of an a href tag to NDMarkup and adds it to the output.  The iterator should be on an opening link tag
		 * and when it ends it will be past the closing tag.
		 */
		protected void GetHTMLLink (ref JavadocIterator iterator, StringBuilder output)
			{
			#if DEBUG
			if (iterator.IsOnHTMLTag("a", TagForm.Opening) == false)
				{  throw new Exception("GetHTMLLink() can only be called when the iterator is on an opening a href tag.");  }
			#endif

			string href = iterator.HTMLTagProperty("href");
			iterator.Next();

			StringBuilder label = new StringBuilder();

			while (iterator.IsInBounds &&
					 iterator.IsOnHTMLTag("a", TagForm.Closing) == false)
				{
				if (iterator.IsOn(JavadocElementType.Text))
					{  label.EntityEncodeAndAppend(iterator.String);  }
				else if (iterator.IsOn(JavadocElementType.EntityChar))
					{  label.EntityEncodeAndAppend(iterator.EntityValue);  }
				else if (iterator.IsOn(JavadocElementType.LineBreak))
					{  label.Append('\n');  }
				// Ignore everything else, including tags

				iterator.Next();
				}

			// Skip the closing tag
			if (iterator.IsInBounds)
				{  iterator.Next();  }

			if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
				{
				output.Append("<link type=\"email\" target=\"");
				output.EntityEncodeAndAppend(href.Substring(7));
				output.Append("\" text=\"");
				output.EntityEncodeAndAppend(label.ToString());
				output.Append("\">");

				return;
				}

			bool isExternalURL = false;
			int colonIndex = href.IndexOf(':');

			if (colonIndex != -1)
				{
				string possibleProtocol = href.Substring(0, colonIndex);
				isExternalURL = Manager.NaturalDocsParser.IsURLProtocol(possibleProtocol);
				}

			if (isExternalURL)
				{
				output.Append("<link type=\"url\" target=\"");
				output.EntityEncodeAndAppend(href);
				output.Append("\" text=\"");
				output.EntityEncodeAndAppend(label.ToString());
				output.Append("\">");

				return;
				}

			// Otherwise just ignore it.  Any relative links are probably to a file in Javadoc's output structure which wouldn't be valid
			// in ours.  While we could try to decode links to class files and convert them into Natural Docs links, it would make more
			// sense for the user to write those as {@link} tags so it's probably not worthwhile to try to support.

			output.Append(label);
			}


		/* Function: GetJavadocLinkSymbol
		 * Returns the symbol part of a @see or {@link} tag and moves the iterator past it.
		 */
		protected string GetJavadocLinkSymbol (ref TokenIterator iterator)
			{
			StringBuilder symbol = new StringBuilder();

			// In Javadoc, spaces are only allowed in parentheses.  We're going to go further and allow them in any braces to support
			// templates and other languages.  However, for angle brackets they must be preceded by a comma.  This allows
			// "Template<A, B>" to be supported while not getting tripped on "operator<".

			// Most symbols won't have braces so create this on demand.
			SafeStack<char> braceStack = null;

			while (iterator.IsInBounds)
				{
				if (iterator.Character == '(' ||
					iterator.Character == '[' ||
					iterator.Character == '{' ||
					iterator.Character == '<')
					{
					if (braceStack == null)
						{   braceStack = new SafeStack<char>();  }

					braceStack.Push(iterator.Character);
					symbol.Append(iterator.Character);
					}

				else if ( (iterator.Character == ')' && braceStack != null && braceStack.Peek() == '(') ||
						   (iterator.Character == ']' && braceStack != null && braceStack.Peek() == '[') ||
						   (iterator.Character == '}' && braceStack != null && braceStack.Peek() == '{') ||
						   (iterator.Character == '>' && braceStack != null && braceStack.Peek() == '<') )
					{
					braceStack.Pop();
					symbol.Append(iterator.Character);
					}
				else if (iterator.Character == '}' && (braceStack == null || braceStack.Contains('{') == false))
					{
					// If we're at an unopened closing brace we're probably at the end of a {@link}.  We check if the stack contains an
					// opening brace instead of checking whether it's empty to ignore any possible opening angle brackets that could
					// screw us up like "operator<".
					break;
					}
				else if (iterator.FundamentalType == FundamentalType.Text ||
						  iterator.FundamentalType == FundamentalType.Symbol)
					{
					iterator.AppendTokenTo(symbol);
					}
				else if (iterator.FundamentalType == FundamentalType.Whitespace)
					{
					if (braceStack == null || braceStack.Count == 0)
						{  break;  }
					else if (braceStack.Peek() == '<')
						{
						TokenIterator lookbehind = iterator;
						lookbehind.Previous();

						if (lookbehind.Character == ',')
							{  iterator.AppendTokenTo(symbol);  }
						else
							{  break;  }
						}
					else
						{  iterator.AppendTokenTo(symbol);  }
					}
				else // line break
					{  break;  }

				iterator.Next();
				}

			// Javadoc uses Class.Class#Member.  First remove leading hashes to handle just #Member
			while (symbol.Length > 0 && symbol[0] == '#')
				{  symbol.Remove(0, 1);  }

			// Convert any remaining hashes to dots.  Ideally we would use the language's native member operator but it's not
			// easy to get it here.
			return symbol.ToString().Replace('#', '.');
			}


		/* Function: GenerateTopic
		 * Creates and returns a <Topic> from the <JavadocComment> and returns it.  If the comment contains no useful content
		 * it will return null.
		 */
		protected Topic GenerateTopic (JavadocComment comment)
			{
			StringBuilder body = new StringBuilder();

			if (comment.Deprecated != null)
				{
				string deprecatedNote = comment.Deprecated;
				deprecatedNote = deprecatedNote.Replace("<p>", "<p><i>");
				deprecatedNote = deprecatedNote.Replace("</p>", "</i></p>");

				string deprecatedTitle = Locale.Get("NaturalDocs.Engine", "Javadoc.Heading.deprecated");

				if (deprecatedNote.StartsWith("<p><i>"))
					{
					body.Append("<p><i><b>" + deprecatedTitle + ":</b> ");
					body.Append(deprecatedNote, 6, deprecatedNote.Length - 6);
					}
				else
					{
					body.Append("<p><i><b>" + deprecatedTitle + ":</b></i></p>");
					body.Append(deprecatedNote);
					}
				}

			if (comment.Description != null)
				{
				body.Append( NormalizeNDMarkup(comment.Description.ToString()) );
				}

			foreach (var section in comment.Sections)
				{
				if (section is SectionedComment.TextSection)
					{
					SectionedComment.TextSection textSection = (SectionedComment.TextSection)section;

					string heading = Locale.SafeGet("NaturalDocs.Engine", "Javadoc.Heading." + textSection.Name, null);

					if (heading != null)
						{
						body.Append("<h>");
						body.EntityEncodeAndAppend(heading);
						body.Append("</h>");
						}

					body.Append(textSection.Content);
					}

				else if (section is SectionedComment.ListSection)
					{
					SectionedComment.ListSection listSection = (SectionedComment.ListSection)section;

					if (listSection.MemberCount > 0)
						{
						string heading = Engine.Locale.SafeGet("NaturalDocs.Engine", "Javadoc.Heading." + listSection.Name + "(count)", null,
																				listSection.MemberCount);

						if (heading != null)
							{
							if (listSection.Name == "param")
								{  body.Append("<h type=\"parameters\">");  }
							else
								{  body.Append("<h>");  }

							body.EntityEncodeAndAppend(heading);
							body.Append("</h>");
							}

						// Parameters always get definition lists even if they don't have descriptions so that the type information can appear with
						// them in HTML.
						bool useDefinitionList = (listSection.Name == "param" || (listSection.MembersHaveNames && listSection.MembersHaveDescriptions));
						bool addLinks = (listSection.Name == "exception" || listSection.Name == "throws");

						if (useDefinitionList)
							{  body.Append("<dl>");  }
						else if (listSection.MemberCount != 1)
							{  body.Append("<ul>");  }

						foreach (var listMember in listSection.Members)
							{
							if (useDefinitionList)
								{  body.Append("<de>");  }
							else if (listSection.MemberCount != 1)
								{  body.Append("<li>");  }

							if (listMember.Name != null)
								{
								if (addLinks)
									{  body.Append("<link type=\"naturaldocs\" originaltext=\"");  }

								body.EntityEncodeAndAppend(listMember.Name);

								if (addLinks)
									{  body.Append("\">");  }
								}

							if (useDefinitionList)
								{  body.Append("</de><dd>");  }

							if (listMember.Description != null)
								{
								body.Append( NormalizeNDMarkup(listMember.Description) );  // Should already be in NDMarkup
								}

							if (useDefinitionList)
								{  body.Append("</dd>");  }
							else if (listSection.MemberCount != 1)
								{  body.Append("</li>");  }
							}

						if (useDefinitionList)
							{  body.Append("</dl>");  }
						else if (listSection.MemberCount != 1)
							{  body.Append("</ul>");  }
						}
					}

				else
					{  throw new NotImplementedException();  }
				}

			Topic topic = null;

			if (body.Length > 0)
				{
				topic = new Topic(EngineInstance.CommentTypes);
				topic.Body = body.ToString();

				MakeSummaryFromBody(topic);
				}

			return topic;
			}



		// Group: Static Variables
		// __________________________________________________________________________


		public static StringSet BlockTags = new StringSet (KeySettings.Literal,
			"author", "deprecated", "exception", "param", "return", "see", "serial", "serialData", "serialField",
			"since", "throws", "version");

		public static StringSet InlineTags = new StringSet (KeySettings.Literal,
			"code", "docRoot", "inheritDoc", "link", "linkPlain", "literal", "value");

		public static StringToStringTable EntityChars = new StringToStringTable (KeySettings.Literal,
			"&lt;", "<", "&gt;", ">", "&amp;", "&", "&quot;", "\"");

		}
	}
