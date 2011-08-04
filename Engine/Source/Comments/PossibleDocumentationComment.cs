/* 
 * Class: GregValure.NaturalDocs.Engine.Comments.PossibleDocumentationComment
 * ____________________________________________________________________________
 * 
 * A class representing a comment found in source code which could possibly contain documentation.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Comments
	{
	public class PossibleDocumentationComment
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: PossibleDocumentationComment
		 */
		public PossibleDocumentationComment ()
			{
			javadoc = false;
			xml = false;
			start = new Tokenization.LineIterator();
			end = new Tokenization.LineIterator();
			}
			
		
		
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Javadoc
		 * Whether the comment could possibly contain Javadoc content.  This doesn't mean that it does, just that
		 * it could.  It's possible for both this and <XML> to be true.
		 */
		public bool Javadoc
			{
			get
				{  return javadoc;  }
			set
				{  javadoc = value;  }
			}
			
			
		/* Property: XML
		 * Whether the comment could possibly contain XML content.  This doesn't mean that it does, just that
		 * it could.  It's possible for both this and <Javadoc> to be true.
		 */
		public bool XML
			{
			get
				{  return xml;  }
			set
				{  xml = value;  }
			}
			
			
		/* Property: Start
		 * The first line of the comment.
		 */
		public Tokenization.LineIterator Start
			{
			get
				{  return start;  }
			set
				{  start = value;  }
			}
			
			
		/* Property: End
		 * One past the last line of the comment.
		 */
		public Tokenization.LineIterator End
			{
			get
				{  return end;  }
			set
				{  end = value;  }
			}
			
			
			
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: javadoc
		 * Whether the comment could possibly contain Javadoc content.
		 */
		protected bool javadoc;

		/* var: xml
		 * Whether the comment could possibly contain XML content.
		 */
		protected bool xml;
		
		/* var: start
		 * The first line of the comment.
		 */
		protected Tokenization.LineIterator start;
		
		/* var: end
		 * One past the last line of the comment.
		 */
		protected Tokenization.LineIterator end;
		
		}
	}