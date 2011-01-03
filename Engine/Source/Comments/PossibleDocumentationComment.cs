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
			type = Type.Plain;
			start = new Tokenization.LineIterator();
			end = new Tokenization.LineIterator();
			}
			
		
		
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Type
		 * The comment type.  Note that this only refers to the comment symbols, not its actual content.
		 */
		public Type Type
			{
			get
				{  return type;  }
			set
				{  type = value;  }
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
		
		
		/* var: type
		 * The comment type.
		 */
		protected Type type;
		
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