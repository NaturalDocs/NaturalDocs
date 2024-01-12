/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Config
 * ____________________________________________________________________________
 *
 * A class representing a complete configuration after all <Languages.txt> values have been combined.
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
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Languages
	{
	public class Config
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Config
		 */
		public Config ()
			{
			languages = new IDObjects.Manager<Language>(KeySettingsForLanguageName, false);
			aliases = new StringTable<int>(KeySettingsForLanguageName);
			fileExtensions = new StringTable<int>(KeySettingsForFileExtensions);
			shebangStrings = new SortedStringTable<int>(new ShebangStringComparer(), KeySettingsForShebangStrings);
			}



		// Group: Information Functions
		// __________________________________________________________________________


		/* Function: LanguageFromFileExtension
		 *
		 * Returns the <Language> associated with the passed file extension, or null if none.
		 *
		 * If you pass null or an empty string, it will return the language information for Shebang Script if it is defined, or null
		 * if it is not.
		 */
		public Language LanguageFromFileExtension (string extension)
			{
			if (String.IsNullOrEmpty(extension))
				{  return LanguageFromName("Shebang Script");  }
			else
				{  return LanguageFromID(fileExtensions[extension]);  }
			}

		/* Function: LanguageFromShebangLine
		 * Returns the <Language> associated with the passed shebang line, or null if none.  Pass the entire line; this function
		 * will handle picking out the substrings.
		 */
		public Language LanguageFromShebangLine (string shebangLine)
			{
			if (String.IsNullOrEmpty(shebangLine))
				{  return null;  }

			shebangLine = shebangLine.NormalizeKey(KeySettingsForShebangStrings);

			// shebangStrings is sorted so longer string come before shorter ones, so it will match against "php4" before "php".
			foreach (KeyValuePair<string, int> shebangStringKVP in shebangStrings)
				{
				if (shebangLine.Contains(shebangStringKVP.Key))
					{  return LanguageFromID(shebangStringKVP.Value);  }
				}

			return null;
			}

		/* Function: LanguageFromName
		 * Returns the <Language> associated with the passed name or alias, or null if none.
		 */
		public Language LanguageFromName (string name)
			{
			Language language = languages[name];

			if (language != null)
				{  return language; }

			int languageID = aliases[name];

			if (languageID != 0)
				{  return LanguageFromID(languageID);  }

			return null;
			}

		/* Function: LanguageFromID
		 * Returns the <Language> associated with the passed ID, or null if none.
		 */
		public Language LanguageFromID (int id)
			{
			return languages[id];
			}

		/* Function: UsedLanguageIDs
		 * Returns a set of all the used language IDs.
		 */
		public IDObjects.NumberSet UsedLanguageIDs ()
			{
			return languages.GetUsedIDs();
			}



		// Group: Action Functions
		// __________________________________________________________________________


		/* Function: AddLanguage
		 * Adds a <Language> to the configuration.
		 */
		public void AddLanguage (Language language)
			{
			languages.Add(language);
			}

		/* Function: AddAlias
		 * Adds an alias to the passed language ID to the configuration.  If the alias already existed it will be redefined.
		 */
		public void AddAlias (string alias, int languageID)
			{
			aliases[alias] = languageID;
			}

		/* Function: AddFileExtension
		 * Adds a file extension for the passed language ID to the configuration.  If the file extension already existed it will be
		 * redefined.
		 */
		public void AddFileExtension (string fileExtension, int languageID)
			{
			fileExtensions[fileExtension] = languageID;
			}

		/* Function: AddShebangString
		 * Adds a shebang string for the passed language ID to the configuration.  If the shebang string already existed it will
		 * be redefined.
		 */
		public void AddShebangString (string shebangString, int languageID)
			{
			shebangStrings[shebangString] = languageID;
			}



		// Group: Operators
		// __________________________________________________________________________


		/* Function: operator ==
		 * Returns whether the two configurations are exactly equal in all settings.
		 */
		public static bool operator== (Config config1, Config config2)
			{
			if ((object)config1 == null && (object)config2 == null)
				{  return true;  }
			else if ((object)config1 == null || (object)config2 == null)
				{  return false;  }


			// Comparing the counts is quick, so do that first

			if (config1.languages.Count != config2.languages.Count ||
				config1.aliases.Count != config2.aliases.Count ||
				config1.fileExtensions.Count != config2.fileExtensions.Count ||
				config1.shebangStrings.Count != config2.shebangStrings.Count)
				{  return false;  }


			// Welp, now we have to do a thorough comparison, though it's easier now that we know both sides have the
			// same number of items in each property.  That means we can iterate through each item in config1 to see if
			// it has a match in config2 and treat them as equal if they do.  We don't have to worry about there being an
			// extra item in config2 that this approach would miss.

			foreach (var language1 in config1.languages)
				{
				var language2 = config2.languages[language1.ID];

				if (language2 == null || language1 != language2)
					{  return false;  }
				}

			foreach (var alias1KVP in config1.aliases)
				{
				var alias2Value = config2.aliases[alias1KVP.Key];

				if (alias2Value != alias1KVP.Value)
					{  return false;  }
				}

			foreach (var fileExtension1KVP in config1.fileExtensions)
				{
				var fileExtension2Value = config2.fileExtensions[fileExtension1KVP.Key];

				if (fileExtension2Value != fileExtension1KVP.Value)
					{  return false;  }
				}

			foreach (var shebangString1KVP in config1.shebangStrings)
				{
				var shebangString2Value = config2.shebangStrings[shebangString1KVP.Key];

				if (shebangString2Value != shebangString1KVP.Value)
					{  return false;  }
				}

			return true;
			}


		/* Function: operator !=
		 * Returns whether any of the settings of the two configurations are different.
		 */
		public static bool operator!= (Config config1, Config config2)
			{
			return !(config1 == config2);
			}


		public override bool Equals (object o)
			{
			if (o is Config)
				{  return (this == (Config)o);  }
			else
				{  return false;  }
			}


		public override int GetHashCode ()
			{
			return ( (languages.Count) ^
						(aliases.Count << 8) ^
						(fileExtensions.Count << 16) ^
						(shebangStrings.Count << 24) );
			}



		// Group: Enumerable Properties
		// __________________________________________________________________________


		/* Property: Languages
		 * Returns an enumerator that returns every <Language> defined.  This property is usable with foreach.
		 */
		public IEnumerable<Language> Languages
			{
			get
				{  return languages;  }
			}

		/* Property: Aliases
		 * Returns an enumerator that returns every language alias defined and its corresponding language ID.  This property
		 * is usable with foreach.
		 */
		public IEnumerable<KeyValuePair<string, int>> Aliases
			{
			get
				{  return aliases;  }
			}

		/* Property: FileExtensions
		 * Returns an enumerator that returns every file extension defined and its corresponding language ID.  This property is
		 * usable with foreach.
		 */
		public IEnumerable<KeyValuePair<string, int>> FileExtensions
			{
			get
				{  return fileExtensions;  }
			}

		/* Property: ShebangStrings
		 * Returns an enumerator that returns every shebang string defined and its corresponding language ID.  This property
		 * is usable with foreach.
		 */
		public IEnumerable<KeyValuePair<string, int>> ShebangStrings
			{
			get
				{  return shebangStrings;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: languages
		 * Manages all the <Languages> by their case-insensitive name and ID number.
		 */
		protected IDObjects.Manager<Language> languages;

		/* var: aliases
		 * A <StringTable> mapping aliases to the language IDs they represent.
		 */
		protected StringTable<int> aliases;

		/* var: fileExtensions
		 * A <StringTable> mapping file extensions to the language IDs they represent.
		 */
		protected StringTable<int> fileExtensions;

		/* var: shebangStrings
		 * A <SortedStringTable> mapping shebang strings to the language IDs they represent.  Using
		 * <ShebangStringComparer> ensures that longer strings appear first when enumerating the entries.
		 */
		protected SortedStringTable<int> shebangStrings;



		// Group: Constants
		// __________________________________________________________________________

		public const KeySettings KeySettingsForLanguageName = KeySettings.IgnoreCase | KeySettings.NormalizeUnicode;
		public const KeySettings KeySettingsForFileExtensions = KeySettings.IgnoreCase;
		public const KeySettings KeySettingsForShebangStrings = KeySettings.IgnoreCase;



		/* ____________________________________________________________________________
		 *
		 * Class: ShebangStringComparer
		 * ____________________________________________________________________________
		 *
		 * An implementation of IComparer that incorporates string length.  Longer strings are less than shorter strings, and if
		 * two strings are equal lengths it does a regular string comparison.  This is done so when you iterate through a
		 * <Collections.SortedStringTable> of shebang strings the longer strings come first.  This is important because someone
		 * could conceivably define one language with shebang string "php5" and another with just "php".  We want the longer
		 * one to be tested against first.
		 */
		private class ShebangStringComparer : IComparer<string>
			{
			public int Compare (string a, string b)
				{
				int aLength, bLength;

				if (a == null)
					{  aLength = 0;  }
				else
					{  aLength = a.Length;  }

				if (b == null)
					{  bLength = 0;  }
				else
					{  bLength = b.Length;  }

				if (aLength != bLength)
					{  return bLength - aLength;  }
				else if (aLength == 0)  // Both null
					{  return 0;  }
				else
					{  return a.CompareTo(b);  }
				}
			}

		}
	}
