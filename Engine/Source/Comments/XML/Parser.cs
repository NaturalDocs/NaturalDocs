/*
 * Class: CodeClear.NaturalDocs.Engine.Comments.XML.Parser
 * ____________________________________________________________________________
 *
 * A parser to handle Microsoft's XML comment format.
 *
 *
 * Topic: Tag Support
 *
 *		Supported Tags:
 *
 *			code - Added to the body as a code block.
 *			example - Added to the body with a heading.  Both top-level and nested are supported.
 *			exception - Added to the body under an Exceptions heading.
 *			list - Added to the body as a bullet or definition list depending on whether it includes both terms and definitions
 *				   or not.  The list type is ignored.  This also means numbered lists will be converted to bullets.
 *			para - Added to the body as a paragraph break.
 *			param - Added to the body under a Parameters heading.
 *			paramref - Replaced with the name property in the body.
 *			permission - Added to the body under a Permissions heading.
 *			remark - Added to the beginning of the body without a header.
 *			returns - Added to the body under a Returns heading.
 *			see - When used inline, added as a link.  When used top-level, added to the body under a See Also heading.
 *			seealso - Added to the body under a See Also heading.
 *			summary - Added to the beginning of the body without a header.
 *			typeparam - Added to the body under a Type Parameters heading.
 *			typeparamref - Replaced with the name property in the body.
 *			value - Added to the beginning of the body without a header.
 *
 *		Supported Non-Standard Tags:
 *
 *			remarks - Treated as remark.  Found in Ookii.Dialogs documentation.
 *			a href - A link to an URL, or an e-mail address if it uses mailto.
 *			see href - See with the href property instead of cref.  Links to an URL, or an e-mail address if it uses mailto.
 *			see langword - See with the langword property instead of cref.  Langword is added as plain text.  Found in
 *								  Ookii.Dialogs documentation.
 *
 *		Unsupported Tags:
 *
 *			c - Ignored.  It will be formatted as regular text.
 *
 *				 If you support c then the question becomes whether to automatically format things like paramrefs the same
 *				 way.  Individual users may or may not religiously apply c tags everywhere and if the paramref formatting
 *				 doesn't match what they do the text will appear very inconsistent.  Ignoring c guarantees the text appears
 *				 consistently in every scenario, although at the cost of irritating those who actually use it.
 *
 *				 The other issue is if you decide to apply c formatting to things like paramrefs, then the output of XML
 *				 comments will always look different from the output of Natural Docs comments even for users who never
 *				 use c.  It's important for the documentation to be consistent as a whole regardless of the underlying format
 *				 of individual comments.
 *
 *			include - Ignored.  Natural Docs is not set up to handle extracting external XML via query.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Comments.Components;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Comments.XML
	{
	public class Parser : Comments.Parser
		{

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
			XMLIterator iterator = new XMLIterator(sourceComment.Start.FirstToken(Tokenization.LineBoundsMode.Everything),
																	  sourceComment.End.FirstToken(Tokenization.LineBoundsMode.Everything));

			while (iterator.Type == XMLElementType.Indent ||
					 iterator.Type == XMLElementType.LineBreak)
				{  iterator.Next();  }

			if (iterator.Type != XMLElementType.Tag)
				{  return false;  }

			XMLComment xmlComment = new XMLComment();

			while (iterator.IsInBounds)
				{
				if (TryToGetTopLevelTextSection(ref iterator, xmlComment) ||
					TryToGetTopLevelListSection(ref iterator, xmlComment))
				    {  }
				else if (TryToSkipOpeningTagAndContents(ref iterator))
					{  }
				else
					{  iterator.Next();  }
				}

			Topic topic = GenerateTopic(xmlComment);

			if (topic != null)
				{
				topic.CommentLineNumber = sourceComment.Start.LineNumber;
				topics.Add(topic);
				}

			return true;
			}


		/* Function: TryToGetTopLevelTextSection
		 * If the iterator is on a top-level text section such as a summary or returns tag it will convert it to NDMarkup, add it
		 * to the comment as a text section, move the iterator past it, and return true.  Otherwise it returns false and nothing
		 * is changed.
		 */
		protected bool TryToGetTopLevelTextSection (ref XMLIterator iterator, XMLComment comment)
			{
			if (iterator.IsOnTag("summary", TagForm.Opening) == false &&
				iterator.IsOnTag("remark", TagForm.Opening) == false &&
				iterator.IsOnTag("remarks", TagForm.Opening) == false &&
				iterator.IsOnTag("example", TagForm.Opening) == false &&
				iterator.IsOnTag("returns", TagForm.Opening) == false &&
				iterator.IsOnTag("value", TagForm.Opening) == false)
				{  return false;  }

			string keyword = iterator.TagType;
			string sectionType = keyword;

			if (keyword == "remarks")
				{  sectionType = "remark";  }

			XMLComment.TextSection section = comment.GetOrCreateTextSection(sectionType);

			TagStack tagStack = new TagStack();
			tagStack.OpenTag(keyword);

			iterator.Next();

			GetText(ref iterator, section.Content, tagStack);

			tagStack.CloseAllTags(section.Content);

			if (iterator.IsOnTag(keyword, TagForm.Closing))
				{  iterator.Next();  }

			return true;
			}


		/* Function: TryToGetTopLevelListSection
		 * If the iterator is on a top-level list section such as a param tag it will convert it to NDMarkup, add it to the
		 * comment in a list section, move the iterator past it, and return true.  Otherwise it returns false and nothing
		 * is changed.
		 */
		protected bool TryToGetTopLevelListSection (ref XMLIterator iterator, XMLComment comment)
			{
			if (iterator.IsOnTag("param", TagForm.Opening) == false &&
				iterator.IsOnTag("exception", TagForm.Opening) == false &&
				iterator.IsOnTag("permission", TagForm.Opening) == false &&
				iterator.IsOnTag("typeparam", TagForm.Opening) == false &&
				iterator.IsOnTag("see", TagForm.Opening) == false &&
				iterator.IsOnTag("see", TagForm.Standalone) == false &&
				iterator.IsOnTag("seealso", TagForm.Opening) == false &&
				iterator.IsOnTag("seealso", TagForm.Standalone) == false)
				{  return false;  }

			string keyword = iterator.TagType;
			string sectionType = keyword;

			if (keyword == "see")
				{  sectionType = "seealso";  }

			XMLComment.ListSection section = comment.GetOrCreateListSection(sectionType);

			string name = null;
			string description = null;

			if (keyword == "param" || keyword == "typeparam")
				{  name = iterator.TagProperty("name");  }
			else
				{  name = iterator.TagProperty("cref") ?? iterator.TagProperty("langword");  }

			bool foundSeeLink = false;

			if (keyword == "see")
				{
				StringBuilder linkOutput = new StringBuilder();
				foundSeeLink = TryToGetLink(ref iterator, linkOutput);

				if (foundSeeLink)
					{
					name = linkOutput.ToString();
					foundSeeLink = true;
					}
				}

			if (!foundSeeLink)
				{
				if (iterator.TagForm == TagForm.Opening)
					{
					TagStack tagStack = new TagStack();
					tagStack.OpenTag(keyword);

					iterator.Next();

					StringBuilder descriptionBuilder = new StringBuilder();
					GetText(ref iterator, descriptionBuilder, tagStack);
					tagStack.CloseAllTags(descriptionBuilder);

					description = NormalizeNDMarkup(descriptionBuilder.ToString());

					if (iterator.IsOnTag(keyword, TagForm.Closing))
						{  iterator.Next();  }
					}
				else
					{  iterator.Next();  }
				}

			if (name != null)
				{  section.AddMember(name, description);  }

			return true;
			}


		/* Function: TryToSkipOpeningTagAndContents
		 * If the iterator is on an opening tag it will move past it, all contained content, and its closing tag and return true.
		 * Otherwise returns false.
		 */
		protected bool TryToSkipOpeningTagAndContents (ref XMLIterator iterator)
			{
			if (iterator.IsOnTag(TagForm.Opening))
				{
				TagStack tagStack = new TagStack();
				tagStack.OpenTag(iterator.TagType);
				iterator.Next();

				while (iterator.IsInBounds && !tagStack.IsEmpty)
					{
					if (iterator.Type == XMLElementType.Tag)
						{
						if (iterator.TagForm == TagForm.Opening)
							{  tagStack.OpenTag(iterator.TagType);  }
						else if (iterator.TagForm == TagForm.Closing)
							{  tagStack.CloseTag(iterator.TagType);  }
						// Ignore standalone tags
						}

					iterator.Next();
					}

				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToGetLink
		 * If the iterator is on a link tag it will convert it to NDMarkup, add it to the output, move the iterator past it, and
		 * return true.  Otherwise it will return false and nothing will be affected.
		 */
		protected bool TryToGetLink (ref XMLIterator iterator, StringBuilder output)
			{
			return (TryToGetStandaloneLink(ref iterator, output) ||
					   TryToGetNamedLink(ref iterator, output));
			}


		/* Function: TryToGetStandaloneLink
		 * If the iterator is on a standalone link tag it will convert it to NDMarkup, add it to the output, move the iterator
		 * past it, and return true.  Otherwise it will return false and nothing will be affected.
		 */
		protected bool TryToGetStandaloneLink (ref XMLIterator iterator, StringBuilder output)
			{
			if (!iterator.IsOnTag("see", TagForm.Standalone) &&
				!iterator.IsOnTag("a", TagForm.Standalone))
				{  return false;  }

			string keyword = iterator.TagType;


			// Code references: <see cref="">

			string cref = iterator.TagProperty("cref");

			if (cref != null && keyword == "see")
				{
				output.Append("<link type=\"naturaldocs\" originaltext=\"");
				output.EntityEncodeAndAppend(cref);
				output.Append("\">");

				iterator.Next();
				return true;
				}


			// URL and e-mail links: <a href="">, <see href="">

			string href = iterator.TagProperty("href");

			if (href != null)
				{
				if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
					{
					string emailAddress = href.Substring(7);

					output.Append("<link type=\"email\" target=\"");
					output.EntityEncodeAndAppend(emailAddress);
					output.Append("\">");
					}

				else
					{
					output.Append("<link type=\"url\" target=\"");
					output.EntityEncodeAndAppend(href);
					output.Append("\">");
					}

				iterator.Next();
				return true;
				}


			// Language links: <see langword="">

			string langword = iterator.TagProperty("langword");

			if (keyword == "see" && langword != null)
				{
				// Just replace it with the text
				output.EntityEncodeAndAppend(langword);

				iterator.Next();
				return true;
				}

			return false;
			}


		/* Function: TryToGetNamedLink
		 * If the iterator is on an opening link tag it will convert it and everything through the corresponding closing tag to
		 * NDMarkup, add it to the output, move the iterator past it, and return true.  Otherwise it will return false and
		 * nothing will be affected.
		 */
		protected bool TryToGetNamedLink (ref XMLIterator iterator, StringBuilder output)
			{

			// Validate the opening tag

			if (!iterator.IsOnTag("see", TagForm.Opening) &&
				!iterator.IsOnTag("a", TagForm.Opening))
				{  return false;  }

			string keyword = iterator.TagType;
			string cref = iterator.TagProperty("cref");
			string href = iterator.TagProperty("href");

			// Code references: <see cref="">
			// URL and e-mail links: <a href="">, <see href="">
			// Ignore language links: <see langword="">
			bool success = ((cref != null && keyword == "see") || href != null);

			if (!success)
				{  return false;  }


			// Get the text between the opening and closing tags

			XMLIterator openingTag = iterator;
			iterator.Next();

			StringBuilder linkTextBuilder = new StringBuilder();
			TagStack tagStack = new TagStack();
			tagStack.OpenTag(keyword);

			GetUnformattedText(ref iterator, linkTextBuilder, tagStack);

			tagStack.CloseAllTags(linkTextBuilder);

			// GetUnformattedText returns entity-encoded chars.  We note this in the variable name so we don't accidentally
			// double-encode them.
			string entityEncodedLinkText = linkTextBuilder.ToString();


			// Move past the closing tag

			if (iterator.IsOnTag(keyword, TagForm.Closing))
				{  iterator.Next();  }


			// Generate the NDMarkup

			// Code references: <see cref="">

			if (cref != null && keyword == "see")
				{
				output.Append("<link type=\"naturaldocs\" originaltext=\"");

				if (entityEncodedLinkText != cref)
					{
					output.Append(entityEncodedLinkText);
					output.Append(" at ");
					}

				output.EntityEncodeAndAppend(cref);
				output.Append("\">");

				return true;
				}


			// URL and e-mail links: <a href="">, <see href="">

			if (href != null)
				{
				if (href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
					{
					string emailAddress = href.Substring(7);

					output.Append("<link type=\"email\" target=\"");
					output.EntityEncodeAndAppend(emailAddress);

					if (entityEncodedLinkText != emailAddress)
						{
						output.Append("\" text=\"");
						output.Append(entityEncodedLinkText);
						}

					output.Append("\">");
					}

				else
					{
					output.Append("<link type=\"url\" target=\"");
					output.EntityEncodeAndAppend(href);

					if (entityEncodedLinkText != href)
						{
						output.Append("\" text=\"");
						output.Append(entityEncodedLinkText);
						}

					output.Append("\">");
					}

				return true;
				}


			// Failsafe.  We shouldn't reach this code in practice.

			output.Append(entityEncodedLinkText);
			return true;
			}


		/* Function: GetText
		 * Converts a block of formatted text to NDMarkup and adds it to the output.  Entity chars will be encoded, recognized
		 * XML tags will be converted, and unrecognized XML tags will be stripped.  It ends when it reaches the closing tag for
		 * anything already on the tag stack.
		 */
		protected void GetText (ref XMLIterator iterator, StringBuilder output, TagStack tagStack)
			{
			output.Append("<p>");
			tagStack.OpenTag(null, "</p>");

			int surroundingPTagIndex = tagStack.Count - 1;

			while (iterator.IsInBounds)
				{

				if (iterator.IsOn(XMLElementType.Text))
					{
					// Send the text to ConvertUnformattedTextAndBareLinks.  However, if there are entity chars we have to convert them
					// into a continuous entity-decoded string or else raw URLs with entity chars like https://example.com&x=y won't get
					// recognized as links.

					string text = iterator.String;
					iterator.Next();

					if (iterator.IsOn(XMLElementType.Text) ||
						iterator.IsOn(XMLElementType.EntityChar))
						{
						StringBuilder textBuilder = new StringBuilder(text);

						for (;;)
							{
							if (iterator.IsOn(XMLElementType.Text))
								{  textBuilder.Append(iterator.String);  }
							else if (iterator.IsOn(XMLElementType.EntityChar))
								{  textBuilder.Append(iterator.EntityValue);  }
							else
								{  break;  }

							iterator.Next();
							}

						text = textBuilder.ToString();
						}

					ConvertUnformattedTextAndBareLinks(text, output);
					}

				else if (iterator.IsOn(XMLElementType.EntityChar))
					{
					output.EntityEncodeAndAppend(iterator.EntityValue);
					iterator.Next();
					}

				else if (iterator.IsOn(XMLElementType.LineBreak))
					{
					// Add a literal line break.  We'll replace these with spaces or double spaces later.  Right now we can't decide
					// which it should be because you can't run a regex directly on a StringBuilder and it would be inefficient to convert
					// it to a string on every line break.
					output.Append('\n');

					iterator.Next();
					}

				else if (iterator.IsOnTag("para"))
					{
					// Text can appear both inside and outside of <para> tags, and whitespace can appear between <para> tags that
					// can be mistaken for content, so rather than put in a lot of logic we handle it in a very dirty but simple way.  Every
					// <para> tag--opening, closing, standalone (technically invalid)--causes a paragraph break.  Normalize() will clean it
					// up for us afterwards.

					tagStack.CloseTag(surroundingPTagIndex + 1, output);  // Reuse our surrounding tag
					output.Append("</p><p>");
					iterator.Next();
					}

				else if (iterator.IsOnTag("code", TagForm.Opening))
					{
					output.Append("</p>");
					GetCode(ref iterator, output, tagStack);
					output.Append("<p>");
					}

				else if (iterator.IsOnTag("example", TagForm.Opening))
					{
					// <example> can be nested in addition to a top-level tag.
					output.Append("</p><h>");
					output.EntityEncodeAndAppend(
						Engine.Locale.Get("NaturalDocs.Engine", "XML.Heading.example")
						);
					output.Append("</h><p>");

					tagStack.OpenTag("example", "</p><p>");
					iterator.Next();
					}

				else if (iterator.IsOnTag("list", TagForm.Opening))
					{
					output.Append("</p>");
					GetList(ref iterator, output, tagStack);
					output.Append("<p>");
					}

				else if (iterator.IsOnTag("paramref") ||
						  iterator.IsOnTag("typeparamref"))
					{
					// Can't assume all the properties are set
					string name = iterator.TagProperty("name");

					if (name != null)
						{  output.EntityEncodeAndAppend(name);  }

					iterator.Next();
					}

				else if (TryToGetLink(ref iterator, output))
					{  }

				else if (iterator.IsOnTag(TagForm.Opening))
					{
					tagStack.OpenTag(iterator.TagType);
					iterator.Next();
					}

				else if (iterator.IsOnTag(TagForm.Closing))
					{
					int openingTagIndex = tagStack.FindTag(iterator.TagType);

					if (openingTagIndex == -1)
						{  }
					else if (openingTagIndex < surroundingPTagIndex)
						{  break;  }
					else
						{  tagStack.CloseTag(openingTagIndex, output);  }

					iterator.Next();
					}

				else
					{
					// Ignore indent.  Spaces between words will be handled by line breaks.
					// Ignore unrecognized standalone tags.
					iterator.Next();
					}
				}

			tagStack.CloseTag(surroundingPTagIndex, output);
			}


		/* Function: GetUnformattedText
		 * Converts a block of text to NDMarkup and adds it to the output, stripping out any formatting tags.  Entity chars will be
		 * encoded.  Unlike <GetText()> this will not surround the output in paragraph tags.  It ends when it reaches the closing
		 * tag for anything already on the tag stack.
		 */
		protected void GetUnformattedText (ref XMLIterator iterator, StringBuilder output, TagStack tagStack)
			{
			int surroundingTagCount = tagStack.Count;

			while (iterator.IsInBounds)
				{

				if (iterator.IsOn(XMLElementType.Text))
					{
					output.EntityEncodeAndAppend(iterator.String);
					iterator.Next();
					}

				else if (iterator.IsOn(XMLElementType.EntityChar))
					{
					output.EntityEncodeAndAppend(iterator.EntityValue);
					iterator.Next();
					}

				else if (iterator.IsOn(XMLElementType.LineBreak))
					{
					// Add a literal line break.  We'll replace these with spaces or double spaces later.  Right now we can't decide
					// which it should be because you can't run a regex directly on a StringBuilder and it would be inefficient to convert
					// it to a string on every line break.
					output.Append('\n');

					iterator.Next();
					}

				else if (iterator.IsOnTag("paramref") ||
						  iterator.IsOnTag("typeparamref"))
					{
					// Can't assume all the properties are set
					string name = iterator.TagProperty("name");

					if (name != null)
						{  output.Append(name);  }

					iterator.Next();
					}

				else if (iterator.IsOnTag(TagForm.Opening))
					{
					tagStack.OpenTag(iterator.TagType);
					iterator.Next();
					}

				else if (iterator.IsOnTag(TagForm.Closing))
					{
					int openingTagIndex = tagStack.FindTag(iterator.TagType);

					if (openingTagIndex == -1)
						{  }
					else if (openingTagIndex <= surroundingTagCount - 1)
						{  break;  }
					else
						{  tagStack.CloseTag(openingTagIndex, output);  }

					iterator.Next();
					}

				else
					{
					// Ignore indent.  Spaces between words will be handled by line breaks.
					// Ignore unrecognized standalone tags.
					iterator.Next();
					}
				}

			if (tagStack.Count > surroundingTagCount)
				{  tagStack.CloseTag(surroundingTagCount, output);  }
			}


		/* Function: GetCode
		 * Converts the contents of a code tag to NDMarkup and adds it to the output.  The iterator should be on an opening code tag
		 * and when it ends it will be past the closing tag.
		 */
		protected void GetCode (ref XMLIterator iterator, StringBuilder output, TagStack tagStack)
			{
			#if DEBUG
			if (iterator.IsOnTag("code", TagForm.Opening) == false)
				{  throw new Exception("GetCode() can only be called when the iterator is on an opening code tag.");  }
			#endif

			output.Append("<pre type=\"code\">");
			tagStack.OpenTag("code", "</pre>");

			int surroundingCodeTagIndex = tagStack.Count - 1;

			iterator.Next();

			List<CodeLine> lines = new List<CodeLine>();

			CodeLine currentLine = new CodeLine();
			currentLine.Indent = -1;  // Don't use text immediately following the code tag to figure out the shared indent.
			currentLine.Text = null;

			for (;;)
				{
				if (iterator.IsInBounds == false)
					{
					lines.Add(currentLine);
					break;
					}
				else if (iterator.IsOnTag(TagForm.Closing))
					{
					int openingTagIndex = tagStack.FindTag(iterator.TagType);

					if (openingTagIndex != -1 && openingTagIndex <= surroundingCodeTagIndex)
						{
						lines.Add(currentLine);
						break;
						}

					// Otherwise let it fall through to be treated as text.
					}

				if (iterator.IsOn(XMLElementType.LineBreak))
					{
					lines.Add(currentLine);

					currentLine = new CodeLine();
					currentLine.Indent = 0;
					currentLine.Text = null;
					}
				else if (iterator.IsOn(XMLElementType.Indent))
					{
					currentLine.Indent = iterator.Indent;
					}
				else // entity, unhandled tag, text
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

			tagStack.CloseTag(surroundingCodeTagIndex, output);
			}


		/* Function: GetList
		 * Converts the contents of a list tag to NDMarkup and adds it to the output.  The iterator should be on an opening list tag
		 * and when it ends it will be past the closing tag.
		 */
		protected void GetList (ref XMLIterator iterator, StringBuilder output, TagStack tagStack)
			{
			#if DEBUG
			if (iterator.IsOnTag("list", TagForm.Opening) == false)
				{  throw new Exception("GetList() can only be called when the iterator is on an opening list tag.");  }
			#endif

			tagStack.OpenTag("list");
			int surroundingListTagIndex = tagStack.Count - 1;

			iterator.Next();

			List<ListItem> items = new List<ListItem>();
			ListItem currentItem = new ListItem();
			StringBuilder stringBuilder = new StringBuilder();  // To reuse

			while (iterator.IsInBounds)
				{
				if (iterator.IsOnTag("list", TagForm.Closing))
					{
					iterator.Next();
					break;
					}

				else if (iterator.IsOnTag("item") ||
						  iterator.IsOnTag("listheader"))
					{
					if (iterator.TagForm == TagForm.Opening)
						{
						currentItem = new ListItem();
						currentItem.IsHeading = (iterator.TagType == "listheader");
						}

					else if (iterator.TagForm == TagForm.Closing)
						{
						if (currentItem.Term != null)
							{
							currentItem.Term = NormalizeNDMarkup(currentItem.Term.Trim());

							if (currentItem.Term == "")
								{  currentItem.Term = null;  }
							else if (currentItem.IsHeading)
								{  currentItem.Term = "<b>" + currentItem.Term + "</b>";  }
							}

						if (currentItem.Description != null)
							{
							currentItem.Description = NormalizeNDMarkup(currentItem.Description.Trim());

							if (currentItem.Description == "")
								{  currentItem.Description = null;  }
							else if (currentItem.IsHeading)
								{
								currentItem.Description = currentItem.Description.Replace("<p>", "<p><b>");
								currentItem.Description = currentItem.Description.Replace("</p>", "</b></p>");
								}
							}

						if (currentItem.Term != null || currentItem.Description != null)
							{  items.Add(currentItem);  }

						currentItem = new ListItem();
						}

					iterator.Next();
					}

				else if (iterator.IsOnTag("term", TagForm.Opening))
					{
					tagStack.OpenTag("term");
					iterator.Next();

					stringBuilder.Remove(0, stringBuilder.Length);
					GetUnformattedText(ref iterator, stringBuilder, tagStack);
					currentItem.Term = stringBuilder.ToString();

					if (iterator.TagType == "term" && iterator.TagForm == TagForm.Closing)
						{  iterator.Next();  }

					tagStack.CloseTag("term");
					}

				else if (iterator.IsOnTag("description", TagForm.Opening))
					{
					tagStack.OpenTag("description");
					iterator.Next();

					stringBuilder.Remove(0, stringBuilder.Length);
					GetText(ref iterator, stringBuilder, tagStack);
					currentItem.Description = stringBuilder.ToString();

					if (iterator.TagType == "description" && iterator.TagForm == TagForm.Closing)
						{  iterator.Next();  }

					tagStack.CloseTag("description");
					}

				else if (TryToSkipOpeningTagAndContents(ref iterator))
					{  }

				else
					{  iterator.Next();  }
				}

			tagStack.CloseTag(surroundingListTagIndex);

			if (items.Count > 0)
				{
				bool hasTerms = false;
				bool hasDescriptions = false;

				for (int i = 0; i < items.Count && (hasTerms == false || hasDescriptions == false); i++)
					{
					if (items[i].Term != null)
						{  hasTerms = true;  }
					if (items[i].Description != null)
						{  hasDescriptions = true;  }
					}

				if (hasTerms && hasDescriptions)
					{
					output.Append("<dl>");

					foreach (var item in items)
						{
						output.Append("<de>");

						if (item.Term != null)
							{  output.Append(item.Term);  }

						output.Append("</de><dd>");

						if (item.Description != null)
							{  output.Append(item.Description);  }

						output.Append("</dd>");
						}

					output.Append("</dl>");
					}

				else // doesn't have both
					{
					output.Append("<ul>");

					foreach (var item in items)
						{
						output.Append("<li>");

						// The format only allows for descriptions without terms, but we'll support terms without descriptions as well.
						if (item.Term != null)
							{  output.Append(item.Term);  }
						if (item.Description != null)
							{  output.Append(item.Description);  }

						output.Append("</li>");
						}

					output.Append("</ul>");
					}
				}
			}


		/* Function: ConvertUnformattedTextAndBareLinks
		 * Converts an unformatted string to NDMarkup and adds it to the output, finding any bare URLs or e-mail addresses and
		 * converting them to links.  Entity chars will be encoded.  No tags are expected in the string so any that appear will be
		 * entity encoded.
		 */
		protected void ConvertUnformattedTextAndBareLinks (string text, StringBuilder output)
			{
			int index = 0;
			var urlMatch = FindURLAnywhereInLineRegex().Match(text);
			var emailMatch = FindEMailAnywhereInLineRegex().Match(text);


			// Walk through the string, handling the thing with the lowest index: an e-mail address, an URL, or if the index is
			// lower than both of them, plain text.  Then advance to the next thing to handle until we're done.

			while (index < text.Length)
				{
				// The index is on an e-mail address
				if (emailMatch.Success && emailMatch.Index == index)
					{
					output.Append("<link type=\"email\" target=\"");
					output.EntityEncodeAndAppend(emailMatch.Groups[1].ToString());
					output.Append("\">");

					index += emailMatch.Length;
					}

				// The index is on an URL
				else if (urlMatch.Success && urlMatch.Index == index &&
						   Manager.NaturalDocsParser.IsURLProtocol(urlMatch.Groups[1].ToString()))
					{
					output.Append("<link type=\"url\" target=\"");
					output.EntityEncodeAndAppend(urlMatch.ToString());
					output.Append("\">");

					index += urlMatch.Length;
					}

				// The index is on plain text
				else
					{
					// Set endOfText to the end of the plain text stretch, which is either the end of the text altogether or
					// the index of the next e-mail address or URL.

					int endOfText = text.Length;

					// We have to check that the next index is greater than the current one to avoid an infinite loop when we
					// have a regex match but it was rejected as a link, such as if the URL protocol wasn't valid.
					if (emailMatch.Success && emailMatch.Index > index && emailMatch.Index < endOfText)
						{  endOfText = emailMatch.Index;  }
					if (urlMatch.Success && urlMatch.Index > index && urlMatch.Index < endOfText)
						{  endOfText = urlMatch.Index;  }

					output.EntityEncodeAndAppend(text, index, endOfText - index);
					index = endOfText;
					}

				// Now refresh our regular expressions if they've fallen behind the index
				if (urlMatch.Success && urlMatch.Index < index)
					{  urlMatch = FindURLAnywhereInLineRegex().Match(text, index);  }
				if (emailMatch.Success && emailMatch.Index < index)
					{  emailMatch = FindEMailAnywhereInLineRegex().Match(text, index);  }
				}
			}


		/* Function: GenerateTopic
		 * Creates and returns a <Topic> from the <XMLComment> and returns it.  If the comment contains no useful content
		 * it will return null.
		 */
		protected Topic GenerateTopic (XMLComment comment)
			{
			StringBuilder body = new StringBuilder();


			// For the first pass just add summary and similar sections to the body without a heading.  We want them to always
			// come first.

			foreach (var section in comment.Sections)
				{
				if (section.Name == "summary" || section.Name == "remark" || section.Name == "value")
					{
					string text = (section as XMLComment.TextSection).Content.ToString();
					text = NormalizeNDMarkup(text);

					if (text != null && text.Length > 0)
						{  body.Append(text);  }
					}
				}


			// The rest of the sections can be added afterwards with headings.

			foreach (var section in comment.Sections)
				{
				if (section.Name == "summary" || section.Name == "remark" || section.Name == "value")
					{  continue;  }

				else if (section is SectionedComment.TextSection)
					{
					SectionedComment.TextSection textSection = (SectionedComment.TextSection)section;

					string text = textSection.Content.ToString();
					text = NormalizeNDMarkup(text);

					if (text != null && text.Length > 0)
						{
						string heading = Engine.Locale.SafeGet("NaturalDocs.Engine", "XML.Heading." + textSection.Name, null);

						if (heading != null)
							{
							body.Append("<h>");
							body.EntityEncodeAndAppend(heading);
							body.Append("</h>");
							}

						body.Append(text);
						}
					}

				else if (section is SectionedComment.ListSection)
					{
					SectionedComment.ListSection listSection = (SectionedComment.ListSection)section;

					if (listSection.MemberCount > 0)
						{
						string heading = Engine.Locale.SafeGet("NaturalDocs.Engine", "XML.Heading." + listSection.Name + "(count)", null,
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
						bool addLinks = (listSection.Name == "exception" || listSection.Name == "permission" || listSection.Name == "seealso");

						if (useDefinitionList)
							{  body.Append("<dl>");  }
						else
							{  body.Append("<ul>");  }

						foreach (var listMember in listSection.Members)
							{
							if (useDefinitionList)
								{  body.Append("<de>");  }
							else
								{  body.Append("<li><p>");  }

							// Entries created by <see href=""> will already have a NDMarkup link.  Just use it as is.
							if (listMember.Name.StartsWith("<link type="))
								{  body.Append(listMember.Name);  }

							// Other entries such as <exception name=""> get a generated link if addLinks is set.
							else
								{
								if (addLinks)
									{  body.Append("<link type=\"naturaldocs\" originaltext=\"");  }

								body.EntityEncodeAndAppend(listMember.Name);

								if (addLinks)
									{  body.Append("\">");  }
								}

							if (useDefinitionList)
								{
								body.Append("</de><dd>");
								body.Append(listMember.Description);  // Should already be in NDMarkup
								body.Append("</dd>");
								}
							else
								{  body.Append("</p></li>");  }
							}

						if (useDefinitionList)
							{  body.Append("</dl>");  }
						else
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



		/* __________________________________________________________________________
		 *
		 * Struct: CodeClear.NaturalDocs.Engine.Comments.XML.Parser.ListItem
		 * __________________________________________________________________________
		 */
		private struct ListItem
			{
			public string Term;
			public string Description;
			public bool IsHeading;
			}

		}
	}
