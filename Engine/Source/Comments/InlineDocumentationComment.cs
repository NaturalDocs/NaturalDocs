/*
 * Class: CodeClear.NaturalDocs.Engine.Comments.InlineDocumentationComment
 * ____________________________________________________________________________
 *
 * A class representing an inline documenation comment, such as for comments trailing enum values.  Normal
 * documentation comments would be represented by <PossibleDocumentationComment> instead.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Comments
	{
	public class InlineDocumentationComment
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: InlineDocumentationComment
		 */
		public InlineDocumentationComment ()
			{
			start = new Tokenization.TokenIterator();
			end = new Tokenization.TokenIterator();
			}


		/* Function: Duplicate
		 */
		public InlineDocumentationComment Duplicate ()
			{
			var copy = new InlineDocumentationComment();

			copy.start = start;
			copy.end = end;

			return copy;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Start
		 * The first token of the comment.
		 */
		public Tokenization.TokenIterator Start
			{
			get
				{  return start;  }
			set
				{  start = value;  }
			}


		/* Property: End
		 * One past the last token of the comment.
		 */
		public Tokenization.TokenIterator End
			{
			get
				{  return end;  }
			set
				{  end = value;  }
			}


		/* Property: String
		 * The entire comment as a string.  Primarily used to aid debugging.
		 */
		public string String
			{
			get
				{  return start.TextBetween(end);  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: start
		 * The first token of the comment.
		 */
		protected Tokenization.TokenIterator start;

		/* var: end
		 * One past the last token of the comment.
		 */
		protected Tokenization.TokenIterator end;

		}
	}
