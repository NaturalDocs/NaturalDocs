/*
 * Class: CodeClear.NaturalDocs.Engine.Comments.InlineDocumentationComment
 * ____________________________________________________________________________
 *
 * A class representing an inline documenation comment, such as for comments trailing enum values.  Regular
 * documentation comments appearing on their own lines would be represented by <DocumentationComment>
 * instead.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Diagnostics;


namespace CodeClear.NaturalDocs.Engine.Comments
	{
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
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


		/* Property: DebuggerDisplay
		 * Shows the comment content when debugging Natural Docs.
		 */
		internal string DebuggerDisplay
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
