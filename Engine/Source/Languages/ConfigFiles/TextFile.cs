/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.ConfigFiles.TextFile
 * ____________________________________________________________________________
 *
 * A class representing the contents of <Languages.txt>.
 *
 *
 * Multithreading: Not Thread Safe, Supports Multiple Readers
 *
 *		This object doesn't have any locking built in, and so it is up to the class managing it to provide thread safety if needed.
 *		However, it does support multiple concurrent readers.  This means it can be used in read-only mode with no locking or
 *		in read/write mode with a ReaderWriterLock.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.Languages.ConfigFiles
	{
	public class TextFile
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: TextFile
		 */
		public TextFile ()
			{
			ignoredFileExtensions = null;
			ignoredFileExtensionsPropertyLocation = default;

			languages = null;
			}



		// Group: Functions
		// __________________________________________________________________________


		/* Function: AddIgnoredFileExtensions
		 * Adds a set of ignored file extensions to the file.  There can be more than one.
		 */
		public void AddIgnoredFileExtensions (IList<string> fileExtensions, PropertyLocation propertyLocation)
			{
			if (ignoredFileExtensions == null)
				{  ignoredFileExtensions = new List<string>(fileExtensions.Count);  }

			ignoredFileExtensions.AddRange(fileExtensions);

			// Only use the first one
			if (!ignoredFileExtensionsPropertyLocation.IsDefined)
				{  ignoredFileExtensionsPropertyLocation = propertyLocation;  }
			}


		/* Function: AddLanguage
		 * Adds a language to the file.
		 */
		public void AddLanguage (TextFileLanguage language)
			{
			if (languages == null)
				{  languages = new List<TextFileLanguage>();  }

			languages.Add(language);
			}


		/* Function: FindLanguage
		 * Returns the language associated with the passed name if it's defined in this file, or null if it's not.
		 */
		public TextFileLanguage FindLanguage (string name)
			{
			if (languages == null)
				{  return null;  }

			string normalizedName = name.NormalizeKey(Config.KeySettingsForLanguageName);

			foreach (var language in languages)
				{
				if (normalizedName == language.Name.NormalizeKey(Config.KeySettingsForLanguageName))
					{  return language;  }
				}

			return null;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: HasIgnoredFileExtensions
		 * Whether any ignored file extensions are defined in this file.
		 */
		public bool HasIgnoredFileExtensions
			{
			get
				{  return (ignoredFileExtensions != null);  }
			}


		/* Property: IgnoredFileExtensions
		 * A list of the ignored file extensions in the order they appear in the text file, or null if there aren't any.
		 */
		public IList<string> IgnoredFileExtensions
			{
			get
				{  return ignoredFileExtensions;  }
			}


		/* Property: IgnoredFileExtensionsPropertyLocation
		 * The <PropertyLocation> where <IgnoredFileExtensions> are defined.
		 */
		public PropertyLocation IgnoredFileExtensionsPropertyLocation
			{
			get
				{  return ignoredFileExtensionsPropertyLocation;  }
			}


		/* Property: HasLanguages
		 * Returns whether this file has any languages defined.
		 */
		public bool HasLanguages
			{
			get
				{  return (languages != null);  }
			}


		/* Property: Languages
		 * A list <TextFileLanguages> in the order they appear in the text file, or null if there aren't any.
		 */
		public IList<TextFileLanguage> Languages
			{
			get
				{  return languages;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: ignoredFileExtensions
		 * A list of the ignored file extensions in the order they appear in the text file, or null if there aren't any.
		 */
		protected List<string> ignoredFileExtensions;

		/* var: ignoredFileExtensionsPropertyLocation
		 * The <PropertyLocation> where <ignoredFileExtensions> is defined.
		 */
		protected PropertyLocation ignoredFileExtensionsPropertyLocation;

		/* var: languages
		 * A list of <TextFileLanguages> in the order they appear in the text file, or null if there aren't any.
		 */
		protected List<TextFileLanguage> languages;

		}
	}
