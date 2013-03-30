/* 
 * Class: GregValure.NaturalDocs.Engine.Comments.Parser
 * ____________________________________________________________________________
 * 
 * A base class for Natural Docs comment parsers.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
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
			
		}
	}