/*
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.KeywordDefinition
 * ____________________________________________________________________________
 *
 * A class encompassing a keyword definition.  A keyword may have more than one definition because there may be a
 * language agnostic one and/or multiple language-specific ones.
 *
 *
 * Multithreading: Not Thread Safe, Supports Multiple Readers
 *
 *		This object doesn't have any locking built in, and so it is up to the class managing it to provide thread safety if needed.
 *		However, it does support multiple concurrent readers.  This means it can be used in read-only mode with no locking or
 *		in read/write mode with a ReaderWriterLock.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.CommentTypes
	{
	public class KeywordDefinition
		{

		// Group: Functions
		// __________________________________________________________________________

		public KeywordDefinition (string keyword) : base()
			{
			this.keyword = keyword;
			plural = false;
			commentTypeID = 0;
			languageID = 0;
			}


		// Group: Operators
		// __________________________________________________________________________


		/* Function: operator ==
		 * Returns whether all the properties of the keyword definitions tags are equal.
		 */
		public static bool operator == (KeywordDefinition definition1, KeywordDefinition definition2)
			{
			if ((object)definition1 == null && (object)definition2 == null)
				{  return true;  }
			else if ((object)definition1 == null || (object)definition2 == null)
				{  return false;  }
			else
				{
				return (definition1.keyword == definition2.keyword &&
						   definition1.plural == definition2.plural &&
						   definition1.commentTypeID == definition2.commentTypeID &&
						   definition1.languageID == definition2.languageID);
				}
			}

		/* Function: operator !=
		 * Returns if any of the properties of the two tags are different.
		 */
		public static bool operator != (KeywordDefinition definition1, KeywordDefinition definition2)
			{
			return !(definition1 == definition2);
			}

		public override bool Equals (object o)
			{
			if (o is KeywordDefinition)
				{  return (this == (KeywordDefinition)o);  }
			else
				{  return false;  }
			}

		public override int GetHashCode ()
			{
			return keyword.GetHashCode();
			}



		// Group: Properties
		// __________________________________________________________________________

		public string Keyword
			{
			get
				{  return keyword;  }
			}

		public bool Plural
			{
			get
				{  return plural;  }
			set
				{  plural = value;  }
			}

		public int CommentTypeID
			{
			get
				{  return commentTypeID;  }
			set
				{  commentTypeID = value;  }
			}

		/* Property: IsLanguageAgnostic
		 * Whether this is a general keyword definition instead of a language-specific one.
		 */
		public bool IsLanguageAgnostic
			{
			get
				{  return !IsLanguageSpecific;  }
			}

		/* Property: IsLanguageSpecific
		 * Whether this is a language-specific keyword definition instead of a general one.
		 */
		public bool IsLanguageSpecific
			{
			get
				{  return (languageID != 0);  }
			}

		/* Property: LanguageID
		 * If this is a language-specific keyword, the language ID it is associated with.  Otherwise zero.
		 */
		public int LanguageID
			{
			get
				{  return languageID;  }
			set
				{  languageID = value;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected string keyword;
		protected bool plural;
		protected int commentTypeID;
		protected int languageID;

		}
	}
