/* 
 * Struct: CodeClear.NaturalDocs.Engine.CommentTypes.ConfigFiles.TextFileKeywordDefinition
 * ____________________________________________________________________________
 * 
 * A struct encapsulating information about a keyword parsed from a <ConfigFiles.TextFile>.
 * 
 * 
 * Multithreading: Thread Safe, Read-Only
 * 
 *		This object is read-only after it is created and thus is inherently thread safe.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.CommentTypes.ConfigFiles
	{
	public struct TextFileKeywordDefinition
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: TextFileKeywordDefinition
		 */
		public TextFileKeywordDefinition (string keyword, string plural = null)
			{
			this.keyword = keyword;
			this.plural = plural;
			}


		/* Function: Duplicate
		 */
		public TextFileKeywordDefinition Duplicate ()
			{
			return new TextFileKeywordDefinition(keyword, plural);
			}



		// Group: Properties
		// __________________________________________________________________________
			
			
		/* Property: Keyword
		 */
		public string Keyword
			{
			get
				{  return keyword;  }
			}

		/* Property: HasPlural
		 * Whether a plural form of the keyword is defined.
		 */
		public bool HasPlural
			{
			get
				{  return (plural != null);  }
			}

		/* Property: Plural
		 * The plural form of <Keyword>, or null if it's not defined.
		 */
		public string Plural
			{
			get
				{  return plural;  }
			}

				
		
		// Group: Variables
		// __________________________________________________________________________


		/* var: keyword
		 */
		private string keyword;

		/* var: plural
		 * The plural form of <keyword>, or null if it's not defined.
		 */
		private string plural;

		}
	}