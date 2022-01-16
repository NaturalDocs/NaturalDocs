/*
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.ConfigFiles.TextFileKeywordGroup
 * ____________________________________________________________________________
 *
 * A class encapsulating information about a group of keywords parsed from a <ConfigFiles.TextFile>.  They can represent
 * the general keywords, a group of language-specific keywords, or the ignored keywords list.
 *
 *
 * Multithreading: Not Thread Safe, Supports Multiple Readers
 *
 *		This object doesn't have any locking built in, and so it is up to the class managing it to provide thread safety if needed.
 *		However, it does support multiple concurrent readers.  This means it can be used in read-only mode with no locking or
 *		in read/write mode with a ReaderWriterLock.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.CommentTypes.ConfigFiles
	{
	public class TextFileKeywordGroup
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: TextFileKeywordGroup
		 */
		public TextFileKeywordGroup (PropertyLocation propertyLocation, string languageName = null)
			{
			this.propertyLocation = propertyLocation;
			this.languageName = languageName;
			this.keywordDefinitions = new List<TextFileKeywordDefinition>();
			}


		/* Function: Duplicate
		 * Creates an independent copy of the keyword group and all its properties.
		 */
		public TextFileKeywordGroup Duplicate ()
			{
			TextFileKeywordGroup copy = new TextFileKeywordGroup(propertyLocation, languageName);

			copy.keywordDefinitions = new List<TextFileKeywordDefinition>(keywordDefinitions.Count);
			copy.keywordDefinitions.AddRange(keywordDefinitions);  // this works because it's a struct

			return copy;
			}


		/* Function: Add
		 * Adds a keyword to the list, and optionally its plural form.
		 */
		public void Add (string keyword, string plural = null)
			{
			keywordDefinitions.Add( new TextFileKeywordDefinition(keyword, plural) );
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: PropertyLocation
		 * The <PropertyLocation> where the keywords are defined.
		 */
		public PropertyLocation PropertyLocation
			{
			get
				{  return propertyLocation;  }
			}

		/* Property: IsLanguageAgnostic
		 * Whether this is a general keyword group instead of a language-specific one.
		 */
		public bool IsLanguageAgnostic
			{
			get
				{  return !IsLanguageSpecific;  }
			}

		/* Property: IsLanguageSpecific
		 * Whether this is a language-specific keyword group instead of a general one.
		 */
		public bool IsLanguageSpecific
			{
			get
				{  return (languageName != null);  }
			}

		/* Property: LanguageName
		 * The name of the language if this is for language-specific keywords, or null if not.
		 */
		public string LanguageName
			{
			get
				{  return languageName;  }
			set
				{  languageName = value;  }
			}

		/* Property: KeywordDefinitions
		 * All the keywords defined by the group, including their optional plural forms.
		 */
		public IList<TextFileKeywordDefinition> KeywordDefinitions
			{
			get
				{  return keywordDefinitions;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		/* var: propertyLocation
		 * The <PropertyLocation> where the keywords are defined.
		 */
		protected PropertyLocation propertyLocation;

		/* var: languageName
		 * The name of the language if this is for language-specific keywords, or null if not.
		 */
		protected string languageName;

		/* var: keywordDefinitions
		 * All the keywords defined by the group, including their optional plural forms.
		 */
		protected List<TextFileKeywordDefinition> keywordDefinitions;

		}
	}
