/* 
 * Class: GregValure.NaturalDocs.Engine.Comments.Parser
 * ____________________________________________________________________________
 * 
 * A base class for Natural Docs comment parsers.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Comments
	{
	public class Parser
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Parser
		 */
		public Parser ()
			{
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



		// Group: Static Variables
		// __________________________________________________________________________


		protected static Regex.Comments.LeadingSpaces LeadingSpacesRegex = new Regex.Comments.LeadingSpaces();
		protected static Regex.Comments.TrailingSpaces TrailingSpacesRegex = new Regex.Comments.TrailingSpaces();
		protected static Regex.Comments.MultipleLineBreaks MultipleLineBreaksRegex = new Regex.Comments.MultipleLineBreaks();

		protected static Regex.Comments.EmptyParagraphs EmptyParagraphsRegex = new Regex.Comments.EmptyParagraphs();

		protected static Regex.Comments.LineBreakWhichProbablyEndsSentence LineBreakWhichProbablyEndsSentenceRegex = 
			new Regex.Comments.LineBreakWhichProbablyEndsSentence();

		}
	}