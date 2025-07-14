﻿/*
 * Class: CodeClear.NaturalDocs.Engine.Locale
 * ____________________________________________________________________________
 *
 * The localization engine for all of Natural Docs.
 *
 * Unlike most other major engine components, this one is completely independent of <Engine.Instance>.  This allows it to
 * be used with external code both before and after <Engine.Instance> is available and in error conditions.
 *
 *
 * Topic: Requirements
 *
 *		- Because it's so fundamental to the functionality of the rest of the program, this class is mostly self-contained.  The
 *		  only other things it relies on are <Engine.Collections.StringToStringTable> and <Path>.
 *
 *		- <Translation Files> must be defined for the default locale for each module.
 *
 *
 * Topic: Usage
 *
 *		- Add strings to the <Translation Files> and reference them with <Get()>.
 *
 *		- Any code that could conceivably be used in error situations should use <SafeGet()>.  This class can reach error states
 *		  and/or throw its own exceptions, so any classes that could be used in error handling should only use <SafeGet()> to
 *		  prevent additional exceptions from being thrown and to be guaranteed a string.
 *
 *
 * Multithreading: Thread Safety Notes
 *
 *		This class is inherently thread safe.  All locks are managed internally, all are released before function calls return, and they
 *		do not depend on locks in other modules.  There is no risk of deadlock.
 *
 *
 * Architecture: Translation Files
 *
 *		This class depends on translation files.  These are UTF-8 text files stored in a Translations subfolder in the same location as
 *		the executing assembly.  They may be in any line break format and may appear with or without the Unicode BOM.
 *
 *		Each language file is named [module].[locale].txt with the locale in all lowercase, such as
 *		"NaturalDocs.Engine.en-us.txt".  If a language file doesn't exist for the requested locale, it first tries stripping the part
 *		after the dash, such as "NaturalDocs.Engine.en.txt".  This allows it to work with a more generalized language file if a
 *		more specific one isn't available.  If that file doesn't exist either, it tries a file with the locale name "default" such as
 *		"NaturalDocs.Engine.default.txt".  If that doesn't exist it throws an exception.
 *
 *
 * Architecture: Translation File Format
 *
 *		Comments:
 *
 *		Lines that start with #, except when within multi-line strings, are ignored.  Comments cannot appear
 *		on the same line as content since strings may need to use the # character.
 *
 *
 *		Identifiers:
 *
 *		Identifiers are case-sensitive.  All characters are allowed in the identifier except colons, #, {{{, and }}}.
 *
 *
 *		Single Line Strings:
 *
 *		Single line strings are in the format "[identifier]: [string]".  All characters are allowed in the string, including
 *		comment symbols and colons, with the exception of {{{ and }}}.
 *
 *
 *		Multi-Line Strings:
 *
 *		Multi-line strings start with a line in the format "[identifier] {{{" and continue until there is a line with
 *		nothing more than "}}}".  All the lines in between them are part of the string.  Part of the string can
 *		NOT be on the same line as the {{{ or }}}.  Leading whitespace is preserved, as is the line break after
 *		the last line of content.
 *
 *
 *		Composite Formatting:
 *
 *		Strings support .NET's composite formatting feature that allows variables to be inserted and formatted with
 *		"{0}" and similar symbols.  The identifiers should contain the name of the variables as a convention but are
 *		not required to do so.
 *
 *
 *		Plurals:
 *
 *		Locale adds an extension to .NET's composite formatting which allows you to specify parts of strings
 *		based on whether a parameter is plural or not.  "{0s?files:file}" will insert "files" if argument 0 is plural and
 *		"file" if not.  The singular is used if the argument is an integer and one, or boolean and false (so you can
 *		treat the argument as isPlural.)  Everything else is plural because "1 files" is less annoying than "23 file".
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine
	{
	static public partial class Locale
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Locale
		 */
		static Locale ()
			{
			localeCode = CultureInfo.CurrentCulture.Name;
			if (String.IsNullOrEmpty(localeCode))  // Will be empty for the invariant culture
				{  localeCode = "default";  }
			else
				{  localeCode = localeCode.ToLower(CultureInfo.InvariantCulture);  }

			translationFolders = new List<Path>(1);
			translationFolders.Add(System.AppContext.BaseDirectory + "/Translations");

			translations = new StringTable<StringToStringTable>(KeySettingsForLocaleNames);
			accessLock = new System.Threading.ReaderWriterLock();
			}


		/* Function: AddLocation
		 * Adds a location where translation files may be found.  It will be searched along with the system folder.
		 */
		static public void AddLocation (Path folder)
			{
			accessLock.AcquireWriterLock(-1);

			try
				{
				bool foundMatch = false;

				foreach (var translationFolder in translationFolders)
					{
					if (translationFolder == folder)
						{
						foundMatch = true;
						break;
						}
					}

				if (!foundMatch)
					{
					translationFolders.Add(folder);
					ClearTranslations();
					}
				}
			finally
				{  accessLock.ReleaseWriterLock();  }
			}


		/* Function: Get
		 *
		 * Gets the specified string for the current locale.
		 *
		 * Parameters:
		 *
		 *		module - The name of the module the string appears in.
		 *		identifier - The identifier of the string.  Case-sensitive.
		 *		arguments - Additional arguments to insert into the string, if any.  Arguments are inserted with
		 *							string.Format() so all of its formatting options are available to you.
		 */
		public static string Get (string module, string identifier, params object[] arguments)
			{
			string result = Lookup (module, identifier, localeCode);

			if (arguments.Length == 0)
				{  return result;  }
			else
				{  return Format(result, arguments);  }
			}


		/* Function: SafeGet
		 *
		 * Gets the specified string for the current locale, but will return the substitution string if any exception
		 * occurs.  It's therefore guaranteed to return a string and safe to use in error conditions or in classes
		 * otherwise used by this one.
		 *
		 * Parameters:
		 *
		 *		module - The name of the module the string appears in.
		 *		identifier - The identifier of the string.  Case-sensitive.
		 *		substitution - The string to return if an error occurs.
		 *		arguments - Additional arguments to insert into the string, if any.  Arguments are inserted with
		 *							string.Format() so all of its formatting options are available to you.
		 */
		public static string SafeGet (string module, string identifier, string substitution, params object[] arguments)
			{
			try
				{  return Get(module, identifier, arguments);  }
			catch
				{
				try
					{
					if (arguments.Length == 0 || substitution == null)
						{  return substitution;  }
					else
						{  return Format(substitution, arguments);  }
					}
				catch
					{
					// If string.Format() errored out too, just return it raw.  It's better than nothing.
					return substitution;
					}
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: LocaleCode
		 * Gets or sets the locale code, which is an all lowercase string in the form of "en-us".  By default it is set
		 * to the system locale but it can be overridden by setting this property.
		 */
		public static string LocaleCode
			{
			get
				{
				accessLock.AcquireReaderLock(-1);
				string result = localeCode;
				accessLock.ReleaseReaderLock();

				return result;
				}
			set
				{
				accessLock.AcquireWriterLock(-1);

				try
					{
					localeCode = value.ToLower(CultureInfo.InvariantCulture);
					ClearTranslations();
					}
				finally
					{  accessLock.ReleaseWriterLock();  }
				}
			}



		// Group: Private Functions
		// __________________________________________________________________________


		/* Function: Lookup
		 *
		 * Gets the specified string for the passed locale.  Will <Load()> language files as needed.
		 *
		 * Parameters:
		 *
		 *		module - The module string to load.
		 *		identifier - The identifier string to load.
		 *		localeCode - The locale string to load.
		 */
		private static string Lookup (string module, string identifier, string localeCode)
			{
			string moduleLocaleString = module + '.' + localeCode;

			accessLock.AcquireReaderLock(-1);
			try
				{
				if (!translations.ContainsKey(moduleLocaleString))
					{
					// Load will automatically upgrade it to a writer lock.
					Load(module, localeCode);
					}

				StringToStringTable translation = translations[moduleLocaleString];

				if (translation.ContainsKey(identifier))
					{
					// The "finally" section will still run.
					return translation[identifier];
					}


				// Check to see if we can strip the specific locale.  en-us -> en
				// We can't use recursion for these without losing the original locale, which we need for the exception.

				int index = localeCode.IndexOf('-');
				if (index != -1)
					{
					string parentLocale = localeCode.Substring(0, index);
					string parentModuleLocaleString = module + '.' + parentLocale;

					if (!translations.ContainsKey(parentModuleLocaleString))
						{  Load(module, parentLocale);  }

					translation = translations[parentModuleLocaleString];

					if (translation.ContainsKey(identifier))
						{  return translation[identifier];  }
					}


				// Otherwise check to see if we can go to the default.  en -> default

				if (localeCode != "default")
					{
					string defaultModuleLocaleString = module + ".default";

					if (!translations.ContainsKey(defaultModuleLocaleString))
						{  Load(module, "default");  }

					translation = translations[defaultModuleLocaleString];

					if (translation.ContainsKey(identifier))
						{  return translation[identifier];  }
					}


				// Okay, it doesn't exist.

				throw new Exception ("Identifier \"" + module + '.' + identifier +
												"\" does not exist in any translation files for the locale " + localeCode + '.');

				}

			finally
				{  accessLock.ReleaseReaderLock();  }
			}


		/* Function: Format
		 *
		 * Formats the specified string with the passed arguments.  Supports all .NET composite formatting plus
		 * Natural Docs' own plural format.
		 */
		public static string Format (string value, params object[] arguments)
			{
			string newValue = value;

			if (FindPluralConditionalSubstitutionRegex().IsMatch(value))
				{
				LocalePluralFormatReplacer replacer = new LocalePluralFormatReplacer(arguments);
				newValue = FindPluralConditionalSubstitutionRegex().Replace(newValue, replacer.MatchEvaluatorDelegate);
				}

			return string.Format(newValue, arguments);
			}


		/* Function: Load
		 *
		 * Loads the translation file for the specified module and locale and enters it into <translations>.  If it
		 * doesn't exist it will create an entry anyway with an empty table, except for the default locale in which case
		 * it will throw an exception.
		 *
		 * Parameters:
		 *
		 *		module - The module string to load.
		 *		locale - The locale string to load, such as "en-us".
		 */
		private static void Load (string module, string localeCode)
			{
			bool hadWriterLock = accessLock.IsWriterLockHeld;
			bool hadReaderLock = accessLock.IsReaderLockHeld;

			System.Threading.LockCookie lockCookie = default(System.Threading.LockCookie);

			if (!hadWriterLock)
				{
				if (hadReaderLock)
					{  lockCookie = accessLock.UpgradeToWriterLock(-1);  }
				else
					{  accessLock.AcquireWriterLock(-1);  }
				}

			try
				{
				string moduleLocaleString = module + '.' + localeCode;

				// Check to see if it's already loaded when we get the writer lock.  It's possible another thread got it
				// first and already loaded this file.
				if (translations.ContainsKey(moduleLocaleString))
					{  return;  }

				bool foundTranslationFile = false;
				Path translationFileName = null;

				foreach (Path translationFolder in translationFolders)
					{
					translationFileName = translationFolder + '/' + moduleLocaleString + ".txt";

					if (System.IO.File.Exists(translationFileName))
						{
						foundTranslationFile = true;
						break;
						}
					}

				if (foundTranslationFile)
					{
					translations[moduleLocaleString] = Load(translationFileName);
					}
				else
					{
					if (localeCode == "default")
						{
						throw new Exceptions.UserFriendly("Could not find translation file " + moduleLocaleString + ".txt.");
						}
					else
						{
						translations[moduleLocaleString] = new Collections.StringToStringTable(KeySettingsForIdentifiers);
						}
					}
				}

			finally
				{
				if (!hadWriterLock)
					{
					if (hadReaderLock)
						{  accessLock.DowngradeFromWriterLock(ref lockCookie);  }
					else
						{  accessLock.ReleaseWriterLock();  }
					}
				}
			}


		/* Function: Load
		 * Loads the translation file at the specified path and returns its data.
		 */
		private static StringToStringTable Load (Path fileName)
			{
			using (var fileReader = new System.IO.StreamReader(fileName, System.Text.Encoding.UTF8, true))
				{
				StringToStringTable fileContent = new StringToStringTable(KeySettingsForIdentifiers);

				string line, originalLine;
				string identifier = "";  // Not necessary to initialize, just need to shut the compiler up.
				string value = "";
				bool inMultiline = false;
				int lineNumber = 0;
				int index = -1;

				while ( (originalLine = fileReader.ReadLine()) != null )
					{
					line = originalLine.Trim();
					lineNumber++;

					if (inMultiline == true)
						{
						if (line == "}}}")
							{
							inMultiline = false;
							fileContent.Add(identifier, value);
							}
						else if (line.IndexOf("}}}") != -1)
							{  throw new Exceptions.UserFriendly("Error in translation file " + fileName + " line " + lineNumber + ": Nothing can be on the same line as }}}.");  }
						else
							{
							// We want to preserve any leading whitespace.
							value += originalLine.TrimEnd() + System.Environment.NewLine;
							}
						}

					else // not in multiline
						{
						if (line == "" || line[0] == '#')
							{  /* skip */  }

						else if ( (index = line.IndexOf("{{{")) != -1)
							{
							identifier = line.Substring(0, index).TrimEnd();

							if (String.IsNullOrEmpty(identifier))
								{  throw new Exceptions.UserFriendly("Error in translation file " + fileName + " line " + lineNumber + ": No identifier before {{{.");  }
							if (identifier.IndexOf(':') != -1)
								{  throw new Exceptions.UserFriendly("Error in translation file " + fileName + " line " + lineNumber + ": Line cannot have a colon and {{{.");  }
							if (line.EndsWith("{{{") == false)
								{  throw new Exceptions.UserFriendly("Error in translation file " + fileName + " line " + lineNumber + ": There cannot be content on the same line as {{{.");  }

							value = "";
							inMultiline = true;
							}

						else if ( (index = line.IndexOf(':')) != -1)
							{
							identifier = line.Substring(0, index).TrimEnd();

							if (index + 1 < line.Length)
								{  value = line.Substring(index + 1).TrimStart();  }
							else
								{  value = null;  }

							if (String.IsNullOrEmpty(identifier))
								{  throw new Exceptions.UserFriendly("Error in translation file " + fileName + " line " + lineNumber + ": No identifier before colon.");  }
							if (String.IsNullOrEmpty(value))
								{  throw new Exceptions.UserFriendly("Error in translation file " + fileName + " line " + lineNumber + ": No value after colon.");  }

							fileContent.Add(identifier, value);
							}

						else
							{  throw new Exceptions.UserFriendly("Error in translation file " + fileName + " line " + lineNumber + ": Unrecognized line format.");  }

						}
					}  // while ReadLine

				if (inMultiline == true)
					{  throw new Exceptions.UserFriendly("Error in translation file " + fileName + ": Unclosed multiline block.");  }

				return fileContent;
				}
			}


		/* Function: ClearTranslations
		 * Clears the loaded translations.  New ones will be loaded the next time they are accessed.
		 */
		private static void ClearTranslations ()
			{
			bool hadWriterLock = accessLock.IsWriterLockHeld;
			bool hadReaderLock = accessLock.IsReaderLockHeld;

			System.Threading.LockCookie lockCookie = default(System.Threading.LockCookie);

			if (!hadWriterLock)
				{
				if (hadReaderLock)
					{  lockCookie = accessLock.UpgradeToWriterLock(-1);  }
				else
					{  accessLock.AcquireWriterLock(-1);  }
				}

			try
				{
				translations.Clear();
				}
			finally
				{
				if (!hadWriterLock)
					{
					if (hadReaderLock)
						{  accessLock.DowngradeFromWriterLock(ref lockCookie);  }
					else
						{  accessLock.ReleaseWriterLock();  }
					}
				}
			}



		// Group: Constants
		// __________________________________________________________________________

		private const Collections.KeySettings KeySettingsForIdentifiers = KeySettings.Literal;
		private const Collections.KeySettings KeySettingsForLocaleNames = KeySettings.Literal;



		// Group: Variables
		// __________________________________________________________________________


		/* string: localeCode
		 * The default locale string in all lowercase, such as "en-us".  If the current locale is unknown or some
		 * system invariant locale, it will be "default".
		 */
		private static string localeCode;


		/* string: translationFolders
		 * A list of locations where translation files are stored.
		 */
		private static List<Path> translationFolders;


		/* object: translations
		 *
		 * A <StringTable> mapping module.locale strings to <StringToStringTables>.  These are demand-loaded so you can't
		 * assume an entry exists for any particular translation, even if the file exists.  If the file doesn't exist and an attempt
		 * was made at loading it, an entry will be created with an empty <StringToStringTable>.  The keys are in the format
		 * "[module].[locale]" and are case-insensitive.
		 *
		 * The <StringTable> class isn't thread safe but supports the reader/writer model.  Use <translationsLock> when
		 * accessing it.
		 */
		private static StringTable<StringToStringTable> translations;


		/* object: accessLock
		 * A reader/writer lock to control access to the class's variables.
		 */
		private static System.Threading.ReaderWriterLock accessLock;



		// Group: Regular Expressions
		// __________________________________________________________________________


		/* Regex: FindPluralConditionalSubstitutionRegex
		 *
		 * Will match instances of plural conditional subtitutions in strings, such as "{0s?files:file}", which allows us to use one
		 * substitution if a plural is appropriate for {0} and another if it is not.
		 *
		 * Capture Groups:
		 *
		 *		1 - The argument number, such as "0" in "{0s?files:file}".
		 *		2 - The plural substitution, such as "files" in "{0s?files:file}".
		 *		3 - The singular substitution, such as "file" in "{0s?files:file}".
		 */
		[GeneratedRegex("""\{([0-9]+)s\?([^\:\}]*)\:([^\:\}]*)\}""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex FindPluralConditionalSubstitutionRegex();

		}



	/*
	 * Class: CodeClear.NaturalDocs.Engine.LocalePluralFormatReplacer
	 * ___________________________________________________________________________
	 *
	 * An internal class to deal with replacing Natural Docs' plural formatting syntax with regular expressions.
	 */
	internal class LocalePluralFormatReplacer
		{

		internal LocalePluralFormatReplacer (object[] newArguments)
			{
			arguments = newArguments;
			}

		internal string MatchEvaluator (Match match)
			{
			int argumentIndex = int.Parse(match.Groups[1].Value);

			// Plural if it's out of range.
			if (argumentIndex >= arguments.Length)
				{  return match.Groups[2].Value;  }

			// Singular if it's 1 or false (for isPlural)
			 if ( (arguments[argumentIndex] is int && (int)arguments[argumentIndex] == 1) ||
				  (arguments[argumentIndex] is bool && (bool)arguments[argumentIndex] == false) )
				{  return match.Groups[3].Value;  }

			// Plural otherwise.
			return match.Groups[2].Value;
			}

		internal MatchEvaluator MatchEvaluatorDelegate
			{
			get
				{  return new MatchEvaluator(MatchEvaluator);  }
			}

		object[] arguments;
		}
	}
