/* 
 * Class: GregValure.NaturalDocs.Engine.Comments.Parsers.XML
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
 *			include - Ignored.  Natural Docs is not set up to handle extracting external XML via query.  This can possibly 
 *						 be added in the future assuming .NET has native functions to handle the query, but I don't know how 
 *						 often this is used in practice.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Comments.Components;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Comments.Parsers
	{
	public class XML : Parser
		{
				
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: XML
		 */
		public XML () : base ()
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
				if (TryToGetTopLevelTextBlock(ref iterator, xmlComment) ||
					TryToGetTopLevelListItem(ref iterator, xmlComment))
				    {  }
				else if (iterator.Type == XMLElementType.Tag && iterator.TagForm == XMLTagForm.Opening)
					{  SkipBlock(ref iterator);  }
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


		/* Function: TryToGetTopLevelTextBlock
		 * If the iterator is on a summary, remark, returns, or top-level example tag it will convert it to NDMarkup, add it to the comment 
		 * in a text block, move the iterator past it, and return true.  Otherwise it returns false and nothing is changed.
		 */
		protected bool TryToGetTopLevelTextBlock (ref XMLIterator iterator, XMLComment comment)
			{
			if ( iterator.Type != XMLElementType.Tag || 
				 iterator.TagForm != XMLTagForm.Opening ||
				(iterator.TagType != "summary" && iterator.TagType != "remark" && iterator.TagType != "example" && iterator.TagType != "returns" &&
				 iterator.TagType != "value") )
				{  return false;  }

			string keyword = iterator.TagType;
			XMLComment.TextBlock block = comment.GetTextBlock(keyword);

			TagStack tagStack = new TagStack();
			tagStack.OpenTag(keyword);

			iterator.Next();

			GetText(ref iterator, block.Text, tagStack);

			tagStack.CloseAllTags(block.Text);

			if (iterator.Type == XMLElementType.Tag &&
				iterator.TagType == keyword &&
				iterator.TagForm == XMLTagForm.Closing)
				{  iterator.Next();  }

			return true;
			}


		/* Function: TryToGetTopLevelListItem
		 * If the iterator is on a param, typeparam, exception, or permission tag it will convert it to NDMarkup, add it to the 
		 * comment in a list block, move the iterator past it, and return true.  Otherwise it returns false and nothing is changed.
		 */
		protected bool TryToGetTopLevelListItem (ref XMLIterator iterator, XMLComment comment)
			{
			if (iterator.Type != XMLElementType.Tag || iterator.TagForm == XMLTagForm.Closing)
				{  return false;  }
			if (iterator.TagForm == XMLTagForm.Opening &&
				(iterator.TagType != "param" && iterator.TagType != "exception" && iterator.TagType != "permission" &&
				  iterator.TagType != "typeparam" && iterator.TagType != "see" && iterator.TagType != "seealso") )
				{  return false;  }
			if (iterator.TagForm == XMLTagForm.Standalone &&
				(iterator.TagType != "see" && iterator.TagType != "seealso") )
				{  return false;  }

			string keyword = iterator.TagType;

			if (keyword == "see")
				{  keyword = "seealso";  }

			XMLComment.ListBlock block = comment.GetListBlock(keyword);

			string name = (keyword == "param" || keyword == "typeparam" ? iterator.TagProperty("name") : iterator.TagProperty("cref"));
			string description = null;

			if (iterator.TagForm == XMLTagForm.Opening)
				{
				TagStack tagStack = new TagStack();
				tagStack.OpenTag(keyword);

				iterator.Next();

				StringBuilder descriptionBuilder = new StringBuilder();
				GetText(ref iterator, descriptionBuilder, tagStack);
				tagStack.CloseAllTags(descriptionBuilder);

				description = Normalize(descriptionBuilder.ToString());

				if (iterator.Type == XMLElementType.Tag &&
					iterator.TagType == keyword &&
					iterator.TagForm == XMLTagForm.Closing)
					{  iterator.Next();  }
				}
			else
				{  iterator.Next();  }

			if (name != null)
				{  block.Add(name, description);  }

			return true;
			}


		/* Function: GetText
		 * Converts a block of formatted text to NDMarkup and adds it to the output.  It ends when it reaches the closing tag for anything 
		 * already on the tag stack.
		 */
		protected void GetText (ref XMLIterator iterator, StringBuilder output, TagStack tagStack)
			{
			output.Append("<p>");
			tagStack.OpenTag(null, "</p>");

			int surroundingPTagIndex = tagStack.Count - 1;

			while (iterator.IsInBounds)
				{

				if (iterator.Type == XMLElementType.Tag)
					{
					if (iterator.TagType == "para")
						{
						// Text can appear both inside and outside of <para> tags, and whitespace can appear between <para> tags that
						// can be mistaken for content, so rather than put in a lot of logic we handle it in a very dirty but simple way.  Every 
						// <para> tag--opening, closing, standalone (technically invalid)--causes a paragraph break.  Normalize() will clean it
						// up for us afterwards.

						tagStack.CloseTag(surroundingPTagIndex + 1, output);  // Reuse our surrounding tag
						output.Append("</p><p>");
						iterator.Next();
						}

					else if (iterator.TagType == "code" && iterator.TagForm == XMLTagForm.Opening)
						{  
						output.Append("</p>");
						GetCode(ref iterator, output, tagStack);  
						output.Append("<p>");
						}

					else if (iterator.TagType == "example" && iterator.TagForm == XMLTagForm.Opening)
						{
						// <example> can be nested in addition to a top-level tag.
						output.Append("</p><h>");
						output.EntityEncodeAndAppend(
							Engine.Locale.Get("NaturalDocs.Engine", "HTML.XMLHeading.example")
							);
						output.Append("</h><p>");

						tagStack.OpenTag("example", "</p><p>");
						iterator.Next();
						}

					else if (iterator.TagType == "list" && iterator.TagForm == XMLTagForm.Opening)
						{
						output.Append("</p>");
						GetList(ref iterator, output, tagStack);
						output.Append("<p>");
						}

					else if (iterator.TagType == "paramref" || iterator.TagType == "typeparamref")
						{
						output.EntityEncodeAndAppend(iterator.TagProperty("name"));
						iterator.Next();
						}

					else if (iterator.TagType == "see" && iterator.TagForm == XMLTagForm.Standalone)
						{
						output.Append("<link type=\"naturaldocs\" originaltext=\"");
						output.EntityEncodeAndAppend(iterator.TagProperty("cref"));
						output.Append("\">");

						iterator.Next();
						}

					else
						{
						if (iterator.TagForm == XMLTagForm.Opening)
							{  
							tagStack.OpenTag(iterator.TagType);  
							}

						else if (iterator.TagForm == XMLTagForm.Closing)
							{
							int openingTagIndex = tagStack.FindTag(iterator.TagType);

							if (openingTagIndex == -1)
								{  }
							else if (openingTagIndex < surroundingPTagIndex)
								{  break;  }
							else
								{  tagStack.CloseTag(openingTagIndex, output);  }
							}

						iterator.Next();
						}

					}

				else if (iterator.Type == XMLElementType.Text ||
						  iterator.Type == XMLElementType.EntityChar)
					{
					output.EntityEncodeAndAppend(iterator.String);
					iterator.Next();
					}

				else if (iterator.Type == XMLElementType.LineBreak)
					{
					// Add a literal line break.  We'll replace these with spaces or double spaces later.  Right now we can't decide 
					// which it should be because you can't run a regex directly on a StringBuilder and it would be inefficient to convert 
					// it to a string on every line break.
					output.Append('\n');

					iterator.Next();
					}

				else
					{
					// Ignore indent.  Spaces between words will be handled by line breaks.
					iterator.Next();
					}
				}

			tagStack.CloseTag(surroundingPTagIndex, output);
			}


		/* Function: GetSimpleText
		 * Converts a block of plain unformatted text to NDMarkup and adds it to the output.  Unlike <GetText()> this will not surrount the 
		 * output in paragraph tags.  It ends when it reaches the closing tag for anything already on the tag stack.
		 */
		protected void GetSimpleText (ref XMLIterator iterator, StringBuilder output, TagStack tagStack)
			{
			int surroundingTagCount = tagStack.Count;

			while (iterator.IsInBounds)
				{

				if (iterator.Type == XMLElementType.Tag)
					{
					if (iterator.TagType == "paramref" || iterator.TagType == "typeparamref")
						{
						output.Append(iterator.TagProperty("name"));
						iterator.Next();
						}

					else if (iterator.TagForm == XMLTagForm.Opening)
						{  
						tagStack.OpenTag(iterator.TagType);  
						}

					else if (iterator.TagForm == XMLTagForm.Closing)
						{
						int openingTagIndex = tagStack.FindTag(iterator.TagType);

						if (openingTagIndex == -1)
							{  }
						else if (openingTagIndex <= surroundingTagCount - 1)
							{  break;  }
						else
							{  tagStack.CloseTag(openingTagIndex, output);  }
						}

					iterator.Next();
					}

				else if (iterator.Type == XMLElementType.Text ||
						  iterator.Type == XMLElementType.EntityChar)
					{
					output.EntityEncodeAndAppend(iterator.String);
					iterator.Next();
					}

				else if (iterator.Type == XMLElementType.LineBreak)
					{
					// Add a literal line break.  We'll replace these with spaces or double spaces later.  Right now we can't decide 
					// which it should be because you can't run a regex directly on a StringBuilder and it would be inefficient to convert 
					// it to a string on every line break.
					output.Append('\n');

					iterator.Next();
					}

				else
					{
					// Ignore indent.  Spaces between words will be handled by line breaks.
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
			if (iterator.Type != XMLElementType.Tag ||
				iterator.TagType != "code" ||
				iterator.TagForm != XMLTagForm.Opening)
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
				if (iterator.Type == XMLElementType.OutOfBounds)
					{
					lines.Add(currentLine);
					break;
					}
				else if (iterator.Type == XMLElementType.Tag &&
						  iterator.TagForm == XMLTagForm.Closing)
					{
					int openingTagIndex = tagStack.FindTag(iterator.TagType);

					if (openingTagIndex != -1 && openingTagIndex <= surroundingCodeTagIndex)
						{
						lines.Add(currentLine);
						break;
						}

					// Otherwise let it fall through to be treated as text.
					}

				if (iterator.Type == XMLElementType.LineBreak)
					{
					lines.Add(currentLine);

					currentLine = new CodeLine();
					currentLine.Indent = 0;
					currentLine.Text = null;
					}
				else if (iterator.Type == XMLElementType.Indent)
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


			// Trim lines and make sure all blank lines are null with an indent of -1.  We'll be finding the shared indent later and
			// we don't want to count unindented blank lines against it.

			for (int i = 0; i < lines.Count; i++)
				{
				if (lines[i].Text != null)
					{  
					// Have to do it this way because CodeLine is a struct.
					CodeLine temp = lines[i];
					temp.Text = temp.Text.Trim();
					lines[i] = temp;
					}

				if (lines[i].Text == null || lines[i].Text.Length == 0)
					{
					CodeLine temp = new CodeLine();
					temp.Text = null;
					temp.Indent = -1;
					lines[i] = temp;
					}
				}


			// Remove blank lines at the end and the beginning of the block.

			for (int i = lines.Count - 1; i >= 0; i--)
				{
				if (lines[i].Text == null)
					{  lines.RemoveAt(i);  }
				else
					{  break;  }
				}

			while (lines.Count > 0 && lines[0].Text == null)
				{  lines.RemoveAt(0);  }


			// Find the smallest indent on lines with content.

			int sharedIndent = -1;

			foreach (var line in lines)
				{
				if (line.Indent != -1 && (sharedIndent == -1 || line.Indent < sharedIndent))
					{  sharedIndent = line.Indent;  }
				}

			if (sharedIndent == -1)
				{  sharedIndent = 0;  }


			// Build the output.

			for (int i = 0; i < lines.Count; i++)
				{
				if (lines[i].Indent != -1)
					{  output.Append(' ', lines[i].Indent - sharedIndent);  }

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
			if (iterator.Type != XMLElementType.Tag ||
				iterator.TagType != "list" ||
				iterator.TagForm != XMLTagForm.Opening)
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
				if (iterator.Type == XMLElementType.Tag)
					{
					if (iterator.TagType == "list" && iterator.TagForm == XMLTagForm.Closing)
						{  
						iterator.Next();
						break;  
						}

					else if (iterator.TagType == "item" || iterator.TagType == "listheader")
						{
						if (iterator.TagForm == XMLTagForm.Opening)
							{  
							currentItem = new ListItem();  
							currentItem.IsHeading = (iterator.TagType == "listheader");
							}

						else if (iterator.TagForm == XMLTagForm.Closing)
							{
							if (currentItem.Term != null)
								{
								currentItem.Term = Normalize(currentItem.Term.Trim());

								if (currentItem.Term == "")
									{  currentItem.Term = null;  }
								else if (currentItem.IsHeading)
									{  currentItem.Term = "<b>" + currentItem.Term + "</b>";  }
								}

							if (currentItem.Description != null)
								{
								currentItem.Description = Normalize(currentItem.Description.Trim());

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

					else if (iterator.TagType == "term" && iterator.TagForm == XMLTagForm.Opening)
						{
						tagStack.OpenTag("term");
						iterator.Next();

						stringBuilder.Remove(0, stringBuilder.Length);
						GetSimpleText(ref iterator, stringBuilder, tagStack);
						currentItem.Term = stringBuilder.ToString();

						if (iterator.TagType == "term" && iterator.TagForm == XMLTagForm.Closing)
							{  iterator.Next();  }

						tagStack.CloseTag("term");
						}

					else if (iterator.TagType == "description" && iterator.TagForm == XMLTagForm.Opening)
						{
						tagStack.OpenTag("description");
						iterator.Next();

						stringBuilder.Remove(0, stringBuilder.Length);
						GetText(ref iterator, stringBuilder, tagStack);
						currentItem.Description = stringBuilder.ToString();

						if (iterator.TagType == "description" && iterator.TagForm == XMLTagForm.Closing)
							{  iterator.Next();  }

						tagStack.CloseTag("description");
						}

					else if (iterator.TagForm == XMLTagForm.Opening)
						{  SkipBlock(ref iterator);  }
					else
						{  iterator.Next();  }
					}

				else // not a tag
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


		/* Function: SkipBlock
		 * If the iterator is on an opening tag it will move past it, all contained content, and its closing tag and return true.
		 * Otherwise returns false.
		 */
		protected void SkipBlock (ref XMLIterator iterator)
			{
			#if DEBUG
			if (iterator.Type != XMLElementType.Tag || iterator.TagForm != XMLTagForm.Opening)
				{  throw new Exception("Can only call SkipBlock() when the iterator is on an opening tag.");  }
			#endif

			TagStack tagStack = new TagStack();
			tagStack.OpenTag(iterator.TagType);
			iterator.Next();

			while (iterator.IsInBounds && !tagStack.IsEmpty)
				{
				if (iterator.Type == XMLElementType.Tag)
					{
					if (iterator.TagForm == XMLTagForm.Opening)
						{  tagStack.OpenTag(iterator.TagType);  }
					else if (iterator.TagForm == XMLTagForm.Closing)
						{  tagStack.CloseTag(iterator.TagType);  }
					// Ignore standalone tags
					}
				
				iterator.Next();
				}
			}


		/* Function: Normalize
		 * 
		 * Cleans up the generated NDMarkup.
		 * 
		 * - Replaces tab characters with spaces.
		 * - Any '\n' characters will be replaced with spaces or double spaces deepending on whether it appears to come at the end
		 *   of a sentence.
		 * - Empty paragraphs and extraneous whitespace will be removed.
		 * 
		 * If the generated NDMarkup is normalized down to nothing it will return null instead of an empty string.
		 */
		protected string Normalize (string ndMarkup)
			{
			ndMarkup = ndMarkup.Replace('\t', ' ');

			// Once to prepare for replacing line breaks
			ndMarkup = TrailingSpacesRegex.Replace(ndMarkup, "");

			ndMarkup = LineBreakWhichProbablyEndsSentenceRegex.Replace(ndMarkup, "  ");
			ndMarkup = ndMarkup.Replace('\n', ' ');

			ndMarkup = LeadingSpacesRegex.Replace(ndMarkup, "");
			ndMarkup = TrailingSpacesRegex.Replace(ndMarkup, "");  // Again since we added spaces
			ndMarkup = MultipleLineBreaksRegex.Replace(ndMarkup, "");

			ndMarkup = EmptyParagraphsRegex.Replace(ndMarkup, "");

			if (ndMarkup.Length == 0)
				{  ndMarkup = null;  }

			return ndMarkup;
			}


		/* Function: GenerateTopic
		 * Creates and returns a <Topic> from the <XMLComment> and returns it.  If the comment contains no useful content
		 * it will return null.
		 */
		protected Topic GenerateTopic (XMLComment comment)
			{
			StringBuilder body = new StringBuilder();


			// For the first pass just add summary and similar blocks to the body without a heading.  We want them to always
			// come first.

			foreach (var block in comment.Blocks)
				{
				if (block.Type == "summary" || block.Type == "remark" || block.Type == "value")
					{
					string text = (block as XMLComment.TextBlock).Text.ToString();
					text = Normalize(text);

					if (text != null && text.Length > 0)
						{  body.Append(text);  }
					}
				}


			// The rest of the blocks can be added afterwards with headings.

			foreach (var block in comment.Blocks)
				{
				if (block.Type == "summary" || block.Type == "remark" || block.Type == "value")
					{  continue;  }

				else if (block is XMLComment.TextBlock)
					{
					XMLComment.TextBlock textBlock = (XMLComment.TextBlock)block;

					string text = textBlock.Text.ToString();
					text = Normalize(text);

					if (text != null && text.Length > 0)
						{
						string heading = Engine.Locale.SafeGet("NaturalDocs.Engine", "HTML.XMLHeading." + textBlock.Type, null);

						if (heading != null)
							{
							body.Append("<h>");
							body.EntityEncodeAndAppend(heading);
							body.Append("</h>");
							}

						body.Append(text);
						}
					}

				else // XMLComment.ListBlock
					{
					XMLComment.ListBlock listBlock = (XMLComment.ListBlock)block;

					if (listBlock.List.Count > 0)
						{
						string heading = Engine.Locale.SafeGet("NaturalDocs.Engine", "HTML.XMLHeading." + listBlock.Type + "(count)", null, 
																			  listBlock.List.Count);

						if (heading != null)
							{
							if (listBlock.Type == "param")
								{  body.Append("<h type=\"parameters\">");  }
							else
								{  body.Append("<h>");  }

							body.EntityEncodeAndAppend(heading);
							body.Append("</h>");
							}

						// Parameters always get definition lists even if they don't have descriptions so that the type information can appear with 
						// them in HTML.
						bool useDefinitionList = (listBlock.Type == "param" || (listBlock.HasNames && listBlock.HasDescriptions));
						bool addLinks = (listBlock.Type == "exception" || listBlock.Type == "permission" || listBlock.Type == "seealso");

						if (useDefinitionList)
							{  body.Append("<dl>");  }
						else
							{  body.Append("<ul>");  }

						foreach (var listItem in listBlock.List)
							{
							if (useDefinitionList)
								{  body.Append("<de>");  }
							else
								{  body.Append("<li><p>");  }

							if (addLinks)
								{  body.Append("<link type=\"naturaldocs\" originaltext=\"");  }

							body.EntityEncodeAndAppend(listItem.Name);

							if (addLinks)
								{  body.Append("\">");  }

							if (useDefinitionList)
								{
								body.Append("</de><dd>");
								body.Append(listItem.Description);  // Should already be in NDMarkup
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
				}

			Topic topic = null;

			if (body.Length > 0)
				{
				topic = new Topic();
				topic.Body = body.ToString();

				MakeSummaryFromBody(topic);
				}

			return topic;
			}



		// Group: Static Variables
		// __________________________________________________________________________


		protected static Regex.Comments.XML.LeadingSpaces LeadingSpacesRegex = new Regex.Comments.XML.LeadingSpaces();
		protected static Regex.Comments.XML.TrailingSpaces TrailingSpacesRegex = new Regex.Comments.XML.TrailingSpaces();
		protected static Regex.Comments.XML.MultipleLineBreaks MultipleLineBreaksRegex = new Regex.Comments.XML.MultipleLineBreaks();

		protected static Regex.Comments.XML.EmptyParagraphs EmptyParagraphsRegex = new Regex.Comments.XML.EmptyParagraphs();

		protected static Regex.Comments.XML.LineBreakWhichProbablyEndsSentence LineBreakWhichProbablyEndsSentenceRegex = 
			new Regex.Comments.XML.LineBreakWhichProbablyEndsSentence();



		/* __________________________________________________________________________
		 * 
		 * Struct: GregValure.NaturalDocs.Engine.Comments.Parsers.XML.CodeLine
		 * __________________________________________________________________________
		 */
		private struct CodeLine
			{
			public int Indent;
			public string Text;
			}

		/* __________________________________________________________________________
		 * 
		 * Struct: GregValure.NaturalDocs.Engine.Comments.Parsers.XML.ListItem
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
