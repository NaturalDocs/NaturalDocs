/*
 * Class: CodeClear.NaturalDocs.Engine.Comments.Parser
 * ____________________________________________________________________________
 *
 * A base class for Natural Docs comment parsers.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Comments
	{
	public class Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: Parser
		 */
		public Parser (Comments.Manager manager)
			{
			this.manager = manager;
			}


		/* Function: Start
		 */
		virtual public bool Start (Errors.ErrorList errors)
			{
			return true;
			}


		/* Function: MakeSummaryFromBody
		 * If the <Topic> has a body, attempts to extract a summary from it and set <Topic.Summary>.
		 */
		public bool MakeSummaryFromBody (Topic topic)
			{
			if (topic.Body == null)
				{  return false;  }

			NDMarkup.Iterator iterator = new NDMarkup.Iterator(topic.Body);

			while (iterator.IsInBounds)
				{
				// Allow headings to come before the opening paragraph.
				// We can assume the NDMarkup is valid, so we can assume this is an opening tag and we'll hit a closing tag.
				if (iterator.Type == NDMarkup.Iterator.ElementType.HeadingTag)
					{
					do
						{  iterator.Next();  }
					while (iterator.Type != NDMarkup.Iterator.ElementType.HeadingTag);

					iterator.Next();
					}

				// Also allow prototypes to come before the opening paragraph.
				else if (iterator.Type == NDMarkup.Iterator.ElementType.PreTag && iterator.Property("type") == "prototype")
					{
					do
						{  iterator.Next();  }
					while (iterator.Type != NDMarkup.Iterator.ElementType.PreTag);

					iterator.Next();
					}

				// Also allow standalone image links to come before the opening paragraph.
				else if (iterator.Type == NDMarkup.Iterator.ElementType.ImageTag && iterator.Property("type") == "standalone")
					{
					iterator.Next();
					}

				// Extract the entire openng paragraph for the summary, unlike Natural Docs 1.x which only used the first sentence.
				else if (iterator.Type == NDMarkup.Iterator.ElementType.ParagraphTag)
					{
					// Don't include the opening <p> in the summary.
					iterator.Next();

					int startingIndex = iterator.RawTextIndex;

					while (iterator.Type != NDMarkup.Iterator.ElementType.ParagraphTag)
						{  iterator.Next();  }

					// Iterator is now on the closing </p>.

					topic.Summary = topic.Body.Substring(startingIndex, iterator.RawTextIndex - startingIndex);
					return true;
					}

				// If we hit any other tag before a paragraph, there is no summary.
				else
					{  break;  }
				}

			return false;
			}


		/* Function: ExtractEmbeddedTopics
		 * Goes through the topic body to find any definition list symbols and adds them to the list as separate topics.
		 * Since embedded topics must appear immediately after their parent topic, this must be called while the passed
		 * topic is at the end of the list.
		 */
		public void ExtractEmbeddedTopics (Topic topic, IList<Topic> topicList)
			{
			if (topic.Body == null)
				{  return;  }

			if (topic.IsList == false && topic.IsEnum == false)
				{
				#if DEBUG
				if (topic.Body.IndexOf("<ds>") != -1)
					{
					throw new Exception ("ExtractEmbeddedTopics found definition symbols in topic " + topic.Title + " even though it isn't " +
															"a list or an enum.");
					}
				#endif

				return;
				}

			int symbolIndex = topic.Body.IndexOf("<ds>");

			int embeddedCommentTypeID = 0;

			if (topic.IsEnum)
				{  embeddedCommentTypeID = EngineInstance.CommentTypes.IDFromKeyword("constant", topic.LanguageID);  }

			// We do it this way in case there is no type that uses the "constant" keyword.
			if (embeddedCommentTypeID == 0)
				{  embeddedCommentTypeID = topic.CommentTypeID;  }

			while (symbolIndex != -1)
				{
				int endSymbolIndex = topic.Body.IndexOf("</ds>", symbolIndex + 4);
				int definitionIndex = endSymbolIndex + 5;

				#if DEBUG
				if (topic.Body.Substring(definitionIndex, 4) != "<dd>")
					{  throw new Exception ("The assumption that a <dd> would appear immediately after a </ds> failed for some reason.");  }
				#endif

				int endDefinitionIndex = topic.Body.IndexOf("</dd>", definitionIndex + 4);

				Topic embeddedTopic = new Topic(EngineInstance.CommentTypes);
				embeddedTopic.Title = topic.Body.Substring(symbolIndex + 4, endSymbolIndex - (symbolIndex + 4)).EntityDecode();
				embeddedTopic.Body = topic.Body.Substring(definitionIndex + 4, endDefinitionIndex - (definitionIndex + 4));
				embeddedTopic.IsEmbedded = true;
				embeddedTopic.CommentTypeID = embeddedCommentTypeID;
				embeddedTopic.DeclaredAccessLevel = topic.DeclaredAccessLevel;
				embeddedTopic.TagString = topic.TagString;
				embeddedTopic.CommentLineNumber = topic.CommentLineNumber;

				MakeSummaryFromBody(embeddedTopic);

				topicList.Add(embeddedTopic);

				symbolIndex = topic.Body.IndexOf("<ds>", endDefinitionIndex + 5);
				}
			}


		/* Function: NormalizeNDMarkup
		 *
		 * Cleans up the generated NDMarkup.
		 *
		 * - Replaces tab characters with spaces.
		 * - Any '\n' characters will be replaced with spaces or double spaces depending on whether it appears to come at the end
		 *   of a sentence.
		 * - Empty paragraphs and extraneous whitespace will be removed.
		 *
		 * If the generated NDMarkup is normalized down to nothing it will return null instead of an empty string.
		 */
		protected string NormalizeNDMarkup (string ndMarkup)
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

			ndMarkup = ndMarkup.Trim();

			if (ndMarkup.Length == 0)
				{  ndMarkup = null;  }

			return ndMarkup;
			}


		/* Function: NormalizeCodeLines
		 *
		 * Cleans up the list of <CodeLines>.
		 *
		 * - Trims whitespace from all lines.
		 * - Replaces empty lines with Text null, Indent -1.
		 * - Removes empty lines from the beginning and end of the list.
		 * - Removes shared indent.
		 */
		protected void NormalizeCodeLines (List<CodeLine> lines)
			{
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


			// Remove shared indent from all lines

			if (sharedIndent >= 1)
				{
				for (int i = 0; i < lines.Count; i++)
					{
					if (lines[i].Indent != -1)
						{
						CodeLine temp = lines[i];
						temp.Indent -= sharedIndent;
						lines[i] = temp;
						}
					}
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		public Comments.Manager Manager
			{
			get
				{  return manager;  }
			}

		public Engine.Instance EngineInstance
			{
			get
				{  return Manager.EngineInstance;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		protected Comments.Manager manager;



		// Group: Static Variables
		// __________________________________________________________________________


		protected static Regex.Comments.LeadingSpaces LeadingSpacesRegex = new Regex.Comments.LeadingSpaces();
		protected static Regex.Comments.TrailingSpaces TrailingSpacesRegex = new Regex.Comments.TrailingSpaces();
		protected static Regex.Comments.MultipleLineBreaks MultipleLineBreaksRegex = new Regex.Comments.MultipleLineBreaks();

		protected static Regex.Comments.EmptyParagraphs EmptyParagraphsRegex = new Regex.Comments.EmptyParagraphs();

		protected static Regex.Comments.LineBreakWhichProbablyEndsSentence LineBreakWhichProbablyEndsSentenceRegex =
			new Regex.Comments.LineBreakWhichProbablyEndsSentence();



		/* __________________________________________________________________________
		 *
		 * Struct: CodeClear.NaturalDocs.Engine.Comments.Parser.CodeLine
		 * __________________________________________________________________________
		 */
		protected struct CodeLine
			{
			public int Indent;
			public string Text;
			}

		}
	}
