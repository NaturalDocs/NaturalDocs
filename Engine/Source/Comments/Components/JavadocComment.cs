/* 
 * Class: CodeClear.NaturalDocs.Engine.Comments.Components.JavadocComment
 * ____________________________________________________________________________
 * 
 * A class to handle the generated output of an Javadoc comment.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Comments.Components
	{
	public class JavadocComment : BlockComment
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: JavadocComment
		 */
		public JavadocComment () : base ()
			{
			description = null;
			deprecated = null;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Description
		 * The text before the block tags.
		 */
		public string Description
			{
			get
				{  return description;  }
			set
				{  description = value;  }
			}

		/* Property: Deprecated
		 * The text stating if this element is deprecated, or null if none.
		 */
		public string Deprecated
			{
			get
				{  return deprecated;  }
			set
				{  deprecated = value;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		protected string description;
		protected string deprecated;

		}
	}