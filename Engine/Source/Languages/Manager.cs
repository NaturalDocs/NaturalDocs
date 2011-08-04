/* 
 * Class: GregValure.NaturalDocs.Engine.Languages.Manager
 * ____________________________________________________________________________
 * 
 * A module to handle <Languages.txt> and all the language parsers within Natural Docs.
 * 
 * 
 * Topic: Usage
 * 
 *		- The static functions <LoadFile()> and <SaveFile()> can be used right away, regardless of program state.
 * 
 *		- Call <Engine.Instance.Start()> which will start this module.
 * 
 * 
 * Group: Files
 * ____________________________________________________________________________
 * 
 * File: Languages.txt
 * 
 *		The configuration file that defines or overrides the language definitions for Natural Docs.  One version sits in Natural 
 *		Docs' configuration folder, and another in the project configuration folder to add languages or override their behavior.
 *		
 *		These files follows the standard conventions in <ConfigFile>.  Identifier and value whitespace is condensed.
 *		
 *		Sections:
 *		
 *			> Ignore[d] Extension[s]: [extension] [extension] ...
 *			
 *			Causes the listed file extensions to be ignored, even if they were previously defined to be part of a language.  The 
 *			list is space-separated.  Example: "Ignore Extensions: cvs txt"
 *			
 *			> Language: [name]
 *			> Alter Language: [name]
 *			>
 *			> (synonyms)
 *			> Edit Language: [name]
 *			> Change Language: [name]
 *			
 *			Creates a new language or alters an existing one.  Everything underneath applies to this language until the next 
 *			heading like this.  Names can use any characters.
 *			
 *			The languages "Text File" and "Shebang Script" have special meanings.  Text files are considered all comment and 
 *			don't have comment symbols.  Shebang scripts have their language determined by their shebang string and 
 *			automatically include files with no extension in addition to the extensions defined.
 *			
 *			If "Text File" doesn't define ignored prefixes, a member operator, or enum value behavior, those settings will be 
 *			copied from the language with the most files in the source tree.
 *			
 * 
 *		Core Language Properties:
 *		
 *			> Simple Identifier: [name]
 *			
 *			Specifies the language name using only the letters A to Z.  No spaces, numbers, symbols, or Unicode
 *			allowed.  This is for use in situations when such things may not be allowed, such as when generating CSS class 
 *			names.  If it's not specified, it defaults to the language name stripped of all unacceptable characters.
 *			
 *			> Alias[es]: [name] [name] ...
 *			> [Add/Replace] Alias[es]: ...
 *			
 *			Alternate names that can be used with (start [language] code).
 *			
 *			> Extension[s]: [extension] [extension] ...
 *			> [Add/Replace] Extension[s]: ...
 *			
 *			Defines file extensions for the language's source files.  The list is space-separated.  Example: "Extensions: c cpp".  
 *			You can use extensions that were previously used by another language to redefine them.
 *			
 *			> Shebang String[s]: [string] [string] ...
 *			> [Add/Replace] Shebang String[s]: ...
 *			
 *			Defines a list of strings that can appear in the shebang (#!) line to designate that it's part of this language.  They can 
 *			appear anywhere in the line, so "php" will work for "#!/user/bin/php4".  You can use strings that were previously 
 *			used by another language to redefine them.
 *			
 * 
 *		Basic Language Support Properties:
 *		
 *			These are used for languages with basic support.
 *			
 *			> Line Comment[s]: [symbol] [symbol] ...
 *			
 *			Defines a space-separated list of symbols that are used for line comments, if any.  Example: "Line Comment: //".
 *			
 *			> Block Comment[s]: [opening symbol] [closing symbol] [opening symbol] [closing symbol] ...
 *			
 *			Defines a space-separated list of symbol pairs that are used for block comments, if any.  
 *			Example: "Block Comment: (* *)".
 *			
 *			> Member Operator: [symbol]
 *			>
 *			> (synonyms)
 *			> Package Separator: [symbol]
 *			
 *			Defines the default member operator, such as . or ::.  This is for presentation only and will not affect how 
 *			Natural Docs links are parsed.  The default is a dot.
 *			
 *			> [Topic Type] Prototype Ender[s]: [symbol] [symbol] ...
 *			
 *			When defined, Natural Docs will attempt to collect prototypes from the code following the specified topic type.  It 
 *			grabs code until the first ender symbol or the next Natural Docs comment, and if it contains the topic name, it serves 
 *			as its prototype.  Use \n to specify a line break.  Example: "Function Prototype Enders: { ;", 
 *			"Variable Prototype Enders: = ;".
 *			
 *			> Line Extender: [symbol]
 *			
 *			Defines the symbol that allows a prototype to span multiple lines if normally a line break would end it.
 *			
 *			> Enum Values: [global|under type|under parent]
 *			
 *			Defines how enum values are referenced.  The default is global.
 *			
 *			global - Values are always global, referenced as 'value'.
 *			under type - Values are under the enum type, referenced as 'class.enum.value'.
 *			under parent - Values are under the enum's parent, referenced as 'class.value'.
 *			
 * 
 *		Deprecated Language Properties:
 *		
 *			These properties are no longer supported.  They will be silently ignored if they appear in the configuration files.
 *			
 *			> Ignore[d] Prefix[es] in Index: [prefix] [prefix] ...
 *			> Ignore[d] [Topic Type] Prefix[es] in Index: [prefix] [prefix] ...
 *			> [Add/Replace] Ignore[d] Prefix[es] in Index: ...
 *			> [Add/Replace] Ignore[d] [Topic Type] Prefix[es] in Index: ...
 *			
 *			Specifies prefixes that should be ignored when sorting symbols for an index.  Can be specified in general or for a 
 *			specific TopicType.  The prefixes will still appear, the symbols will just be sorted as if they're not there.  For example,
 *			specifying "ADO_" for functions will mean that "ADO_DoSomething" will appear under D instead of A.
 *
 *			> Perl Package: [perl package]
 *			
 *			Specifies the Perl package used to fine-tune the language behavior in ways too complex to do in this file.
 *			
 *			> Full Language Support: [perl package]
 *			
 *			Specifies the Perl package that has the parsing routines necessary for full language support.
 *			
 * 
 *		Revisions:
 *		
 *		2.0:
 *		
 *			- Ignore Prefixes, Perl Package, and Full Language Support properties are deprecated.
 *			- Package Separator was renamed Member Operator, although the original will still be accepted.
 *			- Added Simple Identifier, Alias.
 *		
 *		1.32:
 *		
 *			- Package Separator is now a basic language support only property.
 *			- Added Enum Values setting.
 *			
 *		1.3:
 *		
 *			- The file was introduced.
 *			
 * 
 * 
 * File: Languages.nd
 * 
 *		A binary file which stores the combined results of the two versions of <Languages.txt> as of the last run, as well as 
 *		storing the IDs of each type so they maintain their consistency between runs.
 *		
 *		> [[Binary Header]]
 *		
 *		The file starts with the standard binary file header as managed by <BinaryFile>.
 *		
 *		Languages:
 *			
 *			> [String: Language Name]
 *			> [[Language Attributes]]
 *			> ...
 *			> [String: null]
 *			
 *			The file then encodes each language by its name string, followed by its attributes, and repeats until it reaches a null
 *			string instead of a new name string.
 *			
 *			> Language Attributes:
 *			> [Int32: ID]
 *			> [Byte: Type]
 *			> [String: Simple Identifier]
 *			> [String: Alias] [] ... [String: null]
 *			> [Byte: Enum Values]
 *			> [String: Member Operator Symbol]
 *			> [String: Line Extender Symbol]
 *			> [String: Line Comment Symbol] [] ... [String: null]
 *			> [String: Opening Block Comment Symbol] [String: Closing Block Comment Symbo] [] [] ... [String: null]
 *			> [String: Javadoc Opening Line Comment Symbol] [String: Javadoc Remainder Line Comment Symbol] [] ... [String: null]
 *			> [String: Opening Javadoc Block Comment Symbol] [String: Closing Javadoc Block Comment Symbol] [] [] ... [String: null]
 *			> [String: XML Line Comment Symbol] [] ... [String: null]
 *			
 *			The attributes are self-explanitory.  The comment symbols repeat until a null string is reached.
 *			
 *			> [Int32: Topic Type ID]
 *			> [Byte: Include Line Breaks (1 or 0)]
 *			> [String: Prototype Ender Symbol] [] ... [String: null]
 *			> ...
 *			> [Int32: 0]
 *			
 *			Prototype ender sections repeat until a zero ID is reached.
 *			
 *		Other Data:
 *			
 *			> [String: Alias] [Int32: Language ID] [] [] ... [String: Null]
 *			> [String: Extension] [Int32: Language ID] [] [] ... [String: Null]
 *			> [String: Shebang String] [Int32: Language ID] [] [] ... [String: Null]
 *			> [String: Ignored Extension] [] ... [String: Null]
 *			
 *			File extensions, shebang strings, and aliases are paired with language IDs.  Ignored extensions aren't paired with anything.
 *			All repeat until they hit a null string.
 *			
 *		Revisions:
 *		
 *			2.0:
 *				
 *				- The file was introduced.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Collections;


namespace GregValure.NaturalDocs.Engine.Languages
	{
	public class Manager
		{
		
		// Group: Constants
		// __________________________________________________________________________
		
		
		/* Constants: Tolerance Constants
		 * 
		 * LanguageNamesIgnoreCase - Whether language names ignore case.
		 * LanguageNamesAreNormalized - Whether Unicode normalization is applied to language names.
		 * ExtensionsIgnoreCase - Whether file extensions ignore case.
		 * ExtensionsAreNormalized - Whether Unicode normalization is applied to file extensions.
		 * ShebangStringsIgnoreCase - Whether shebang strings ignore case.
		 * ShebangStringsAreNormalized - Whether Unicode normalization is applied to shebang strings.
		 */
		public const bool LanguageNamesIgnoreCase = true;
		public const bool LanguageNamesAreNormalized = true;
		public const bool ExtensionsIgnoreCase = true;
		public const bool ExtensionsAreNormalized = false;
		public const bool ShebangStringsIgnoreCase = true;
		public const bool ShebangStringsAreNormalized = false;		 
		 
		 
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Manager
		 */
		public Manager ()
			{
			languages = new IDObjects.Manager<Language>(LanguageNamesIgnoreCase, LanguageNamesAreNormalized);
			aliases = new StringTable<Language>(LanguageNamesIgnoreCase, LanguageNamesAreNormalized);
			extensions = new StringTable<Language>(ExtensionsIgnoreCase, ExtensionsAreNormalized);
			shebangStrings = new SortedStringTable<Language>(ShebangStringsIgnoreCase, ShebangStringsAreNormalized,
																					new ShebangStringComparer());
			
			predefinedLanguages = new Language[2];
			
			predefinedLanguages[0] = new Language("Text File");
			predefinedLanguages[0].Type = Language.LanguageType.TextFile;
			predefinedLanguages[0].Predefined = true;
			
			predefinedLanguages[1] = new Language("Shebang Script");
			predefinedLanguages[1].Type = Language.LanguageType.Container;
			predefinedLanguages[1].Predefined = true;
			}
			
			
		/* Function: Start
		 * 
		 * Loads and combines the two versions of <Languages.txt>, returning whether it was successful.  If there were any errors
		 * they will be added to errorList.
		 * 
		 * Dependencies:
		 * 
		 *		- <Config.Manager> and <TopicTypes.Manager> must be started before using the rest of the class.
 		 */
		public bool Start (Errors.ErrorList errorList)
			{
			List<ConfigFileLanguage> systemLanguageList;
			List<ConfigFileLanguage> projectLanguageList;
			List<string> ignoredSystemExtensions;
			List<string> ignoredProjectExtensions;
			
			List<Language> binaryLanguages;
			List<KeyValuePair<string, int>> binaryAliases;
			List<KeyValuePair<string, int>> binaryExtensions;
			List<KeyValuePair<string, int>> binaryShebangStrings;
			List<string> binaryIgnoredExtensions;
			
			// The return value, which is whether we were able to successfully load and parse the system Languages.txt, and if 
			// it exists, the project Languages.txt.  The project Languages.txt not existing is not a failure.
			bool success = true;
			
			// Whether anything has changed since the last run, as determined by Languages.nd.  If Languages.nd doesn't exist 
			// or is corrupt, we have to assume something changed.
			bool changed = false;
			
			
			// First add all the predefined languages, since they may be subclassed.
			
			foreach (Language language in predefinedLanguages)
				{  languages.Add(language);  }


			// We need the ID numbers to stay consistent between runs, so we create all the languages from the binary file
			// next.  We'll worry about comparing their attributes with the text files and seeing if any were added or deleted later.

			// Don't bother going through the effort if we're rebuilding everything anyway.
			if (Engine.Instance.Config.ReparseEverything == true)
				{
				binaryLanguages = new List<Language>();
				binaryAliases = new List<KeyValuePair<string,int>>();
				binaryExtensions = new List<KeyValuePair<string,int>>();
				binaryShebangStrings = new List<KeyValuePair<string,int>>();
				binaryIgnoredExtensions = new List<string>();
				
				changed = true;
				}
				
			else if (LoadBinaryFile(Engine.Instance.Config.WorkingDataFolder + "/Languages.nd", out binaryLanguages,
											 out binaryAliases, out binaryExtensions, out binaryShebangStrings, out binaryIgnoredExtensions) == false)
				{
				changed = true;
				// Even though it failed, LoadBinaryFiles will still have created valid empty objects for them.
				}
				
			else // LoadBinaryFile succeeded
				{
				// We use a try block so if anything screwy happens, like two languages having the same ID number and thus 
				// causing an exception when added, we can continue as if the binary file didn't parse at all.
				try
					{
					foreach (Language binaryLanguage in binaryLanguages)
						{
						// We don't add the binary language itself because we only want those for comparison purposes.  We otherwise 
						// want the languages to be at their default values because the Languages.txt versions will only set some 
						// attributes, not all.
			            
						// Check for predefined languages of the same name.  If any of the binary languages' IDs collide with the
						// predefined languages' ones, it will be taken care of by the exception handler.
						Language existingLanguage = languages[binaryLanguage.Name];
			            
						if (existingLanguage == null)
							{
							Language newLanguage = new Language(binaryLanguage.Name);
							newLanguage.ID = binaryLanguage.ID;
							newLanguage.InBinaryFile = true;
							
							languages.Add(newLanguage);
							}
						else
							{
							existingLanguage.InBinaryFile = true;
							}
						}
					}
				catch
					{
					languages.Clear();
					changed = true;
			        
					foreach (Language predefinedLanguage in predefinedLanguages)
						{  languages.Add(predefinedLanguage);  }
					
					// Clear them since they may be used later in this function.
					binaryLanguages.Clear();
					binaryAliases.Clear();
					binaryExtensions.Clear();
					binaryShebangStrings.Clear();
					binaryIgnoredExtensions.Clear();
					
					// Otherwise ignore the exception and continue.
					}
				}
			
			
			Path systemFile = Engine.Instance.Config.SystemConfigFolder + "/Languages.txt";
			Path projectFile = Engine.Instance.Config.ProjectConfigFolder + "/Languages.txt";

			
			// Load the files.
			
			if (!LoadFile( systemFile, out systemLanguageList, out ignoredSystemExtensions, errorList ))
			    {  
			    success = false;  
			    // Continue anyway because we want to show errors from both files.
			    }
			
			if (System.IO.File.Exists(projectFile))
			    {
			    if (!LoadFile ( projectFile, out projectLanguageList, out ignoredProjectExtensions, errorList ))
			        {  success = false;  }
			    }
			else
			    {
			    // The project file not existing is not an error condition.  Fill in the variables with empty structures.
			    projectLanguageList = new List<ConfigFileLanguage>();
			    ignoredProjectExtensions = new List<string>();
			    }
				
			if (success == false)
				{  return false;  }
				
				
			// Combine the ignored extensions.
			
			StringSet ignoredExtensions = new StringSet(ExtensionsIgnoreCase, ExtensionsAreNormalized);
			
			foreach (string extension in ignoredSystemExtensions)
				{  ignoredExtensions.Add(extension);  }

			foreach (string extension in ignoredProjectExtensions)
				{  ignoredExtensions.Add(extension);  }
				
				
			// Add the languages.  We don't need to do separate passes for standard entries and alter entries because alter 
			// entries should only appear in the project file and only apply to types in the system file.  Anything else is either an 
			// error (system file can't alter a project entry) or would have been simplified out by LoadFile (a file with an alter 
			// entry applying to a language in the same file.)  Start_AddLanguage() also prevents inappropriate properties from 
			// being set on languages, like Line Comment on one with full language support.

			foreach (ConfigFileLanguage configFileLanguage in systemLanguageList)
			    {  
			    if (!Start_AddLanguage(configFileLanguage, systemFile, true, ignoredExtensions, errorList))
			        {  success = false;  }
			    }

			foreach (ConfigFileLanguage configFileLanguage in projectLanguageList)
			    {  
			    if (!Start_AddLanguage(configFileLanguage, projectFile, false, ignoredExtensions, errorList))
			        {  success = false;  }
			    }
			    
			if (success == false)
				{  return false;  }
				
				
			// Now that everything's in languages we can delete the ones that weren't in the config files, such as predefined
			// languages that were removed or languages that were in the binary file from the last run but were deleted.  We 
			// have to put them on a list and delete them in a second pass because deleting them while iterating through would 
			// screw up the iterator.
			
			List<string> deletedLanguageNames = new List<string>();
			
			foreach (Language language in languages)
				{
				if (language.InConfigFiles == false)
					{
					deletedLanguageNames.Add(language.Name);
			        
					// Check this flag so we don't set it to changed if we're deleting a predefined language that wasn't in the binary
					// file.
					if (language.InBinaryFile == true)
						{  changed = true;  }
					}
				}
				
			foreach (string deletedLanguageName in deletedLanguageNames)
				{
				languages.Remove(deletedLanguageName);
				}
				
				
			// Everything is okay at this point.  Save the files again to reformat them.  If the project file didn't exist, saving it 
			// with the empty structures will create it.
			
			Start_FixCapitalization(systemLanguageList);
			Start_FixCapitalization(projectLanguageList);
			
			if (!SaveFile(projectFile, projectLanguageList, ignoredProjectExtensions, errorList, true, false))
			    {  success = false;  };
				
			if (!SaveFile(systemFile, systemLanguageList, ignoredSystemExtensions, errorList, false, true))
			    {  success = false;  };
			
			
			// Generate alternate comment styles.  We don't want these included in the config files but we do want them in the
			// binary files in case the generation method changes in a future version.

			foreach (Language language in languages)
				{  language.GenerateAlternateCommentStyles();  }

			
			// Compare the structures with the binary ones to see if anything changed.

			if (binaryLanguages.Count != languages.Count || 
				binaryAliases.Count != aliases.Count ||
			    binaryExtensions.Count != extensions.Count ||
			    binaryShebangStrings.Count != shebangStrings.Count || 
			    binaryIgnoredExtensions.Count != ignoredExtensions.Count)
			   {
			   changed = true;
			   }
			else if (changed == false)
			    {
			    // Do a detailed comparison now.
				
			    foreach (Language binaryLanguage in binaryLanguages)
			        {
			        Language language = languages[ binaryLanguage.Name ];
					
			        if (language == null || binaryLanguage != language)
			            {  
			            changed = true;
			            break;
			            }
			        }
					
			    if (changed == false)
			        {
			        foreach (string binaryIgnoredExtension in binaryIgnoredExtensions)
			            {
			            if (ignoredExtensions.Contains(binaryIgnoredExtension) == false)
			                {
			                changed = true;
			                break;
			                }
			            }
			        }
					
			    if (changed == false)
			        {
			        foreach (KeyValuePair<string, int> binaryAliasPair in binaryAliases)
			            {
			            // We can use ID instead of Name because we know they match now.
			            if (aliases.ContainsKey(binaryAliasPair.Key) == false ||
			                aliases[binaryAliasPair.Key].ID != binaryAliasPair.Value)
			                {
			                changed = true;
			                break;
			                }
			            }
			        }

			    if (changed == false)
			        {
			        foreach (KeyValuePair<string, int> binaryExtensionPair in binaryExtensions)
			            {
			            // We can use ID instead of Name because we know they match now.
			            if (extensions.ContainsKey(binaryExtensionPair.Key) == false ||
			                extensions[binaryExtensionPair.Key].ID != binaryExtensionPair.Value)
			                {
			                changed = true;
			                break;
			                }
			            }
			        }

			    if (changed == false)
			        {
			        foreach (KeyValuePair<string, int> binaryShebangStringPair in binaryShebangStrings)
			            {
			            // We can use ID instead of Name because we know they match now.
			            if (shebangStrings.ContainsKey(binaryShebangStringPair.Key) == false ||
			                shebangStrings[binaryShebangStringPair.Key].ID != binaryShebangStringPair.Value)
			                {
			                changed = true;
			                break;
			                }
			            }
			        }
			    }

				
			SaveBinaryFile(Engine.Instance.Config.WorkingDataFolder + "/Languages.nd",
								   languages, aliases, extensions, shebangStrings, ignoredExtensions);
								   
			
			if (success == true && changed == true)
			    {  Engine.Instance.Config.ReparseEverything = true;  }

			return success;
			}
			
			
		/* Function: Start_AddLanguage
		 * A helper function that is used only by <Start()> to add a <ConfigFileLanguage> into <languages>.
		 * Returns whether it was able to do so without any errors.
		 */
		private bool Start_AddLanguage (ConfigFileLanguage configFileLanguage, Path sourceFile, bool isSystemFile,
														StringSet ignoredExtensions, Errors.ErrorList errorList)
			{
			bool success = true;
			
			
			// Validate or create the language.
			
			if (configFileLanguage.AlterLanguage == true)
				{
				// If altering a language that doesn't exist at all, at least not in the config files...
				if ( languages.Contains(configFileLanguage.Name) == false ||
				     languages[configFileLanguage.Name].InConfigFiles == false )
					{
					errorList.Add( 
						Locale.Get("NaturalDocs.Engine", "Languages.txt.AlteredLanguageDoesntExist(name)", configFileLanguage.Name),
						sourceFile, configFileLanguage.LineNumber 
						);
						
					success = false;
					}
				}
				
			else // define language, not alter
				{
				// Error if defining a language that already exists in the config files.  Having it exist otherwise is fine.
				if (languages.Contains(configFileLanguage.Name))
					{
				    if (languages[configFileLanguage.Name].InConfigFiles == true)
						{
						errorList.Add( 
							Locale.Get("NaturalDocs.Engine", "Languages.txt.LanguageAlreadyExists(name)", configFileLanguage.Name),
							sourceFile, configFileLanguage.LineNumber 
							);
							
						success = false;
						}
					}
				else
					{					
					Language newLanguage = new Language(configFileLanguage.Name);
					languages.Add(newLanguage);
					}
					
				if (isSystemFile)
					{  languages[configFileLanguage.Name].InSystemFile = true;  }
				else
					{  languages[configFileLanguage.Name].InProjectFile = true;  }
				}
				
			if (success == false)
				{  return false;  }
				
				
			// Apply the properties.
			
			Language language = languages[configFileLanguage.Name];
			
			if (configFileLanguage.SimpleIdentifier != null)
				{  language.SimpleIdentifier = configFileLanguage.SimpleIdentifier;  }
			
			if (configFileLanguage.LineCommentStrings != null)
				{  
				if (language.Type != Language.LanguageType.BasicSupport)
					{  
					Start_CantDefinePropertyError(configFileLanguage, language.Type, sourceFile, "Line Comment", errorList);  
					success = false;
					}
				else
					{  language.LineCommentStrings = configFileLanguage.LineCommentStrings;  }
				}
				
			if (configFileLanguage.BlockCommentStringPairs != null)
				{
				if (language.Type != Language.LanguageType.BasicSupport)
					{  
					Start_CantDefinePropertyError(configFileLanguage, language.Type, sourceFile, "Block Comment", errorList);  
					success = false;
					}
				else
					{  language.BlockCommentStringPairs = configFileLanguage.BlockCommentStringPairs;  }
				}
				
			if (configFileLanguage.MemberOperator != null)
				{
				if (language.Type != Language.LanguageType.BasicSupport && language.Type != Language.LanguageType.TextFile)
					{  
					Start_CantDefinePropertyError(configFileLanguage, language.Type, sourceFile, "Member Operator", errorList);  
					success = false;
					}
				else
					{  language.MemberOperator = configFileLanguage.MemberOperator;  }
				}
				
			if (configFileLanguage.LineExtender != null)
				{
				if (language.Type != Language.LanguageType.BasicSupport)
					{  
					Start_CantDefinePropertyError(configFileLanguage, language.Type, sourceFile, "Line Extender", errorList);  
					success = false;
					}
				else
					{  language.LineExtender = configFileLanguage.LineExtender;  }
				}
				
			if (configFileLanguage.EnumValue != null)
				{
				if (language.Type != Language.LanguageType.BasicSupport && language.Type != Language.LanguageType.TextFile)
					{  
					Start_CantDefinePropertyError(configFileLanguage, language.Type, sourceFile, "Enum Value", errorList);  
					success = false;
					}
				else
					{  language.EnumValue = (Language.EnumValues)configFileLanguage.EnumValue;  }
				}
				
			string[] topicTypeNamesWithPrototypeEnders = configFileLanguage.GetTopicTypeNamesWithPrototypeEnders();
			
			if (topicTypeNamesWithPrototypeEnders != null)
				{
				if (language.Type != Language.LanguageType.BasicSupport)
					{	
					Start_CantDefinePropertyError(configFileLanguage, language.Type, sourceFile, "Prototype Enders", errorList);  
					success = false;
					}
				else
					{
					foreach (string topicTypeName in topicTypeNamesWithPrototypeEnders)
						{
						TopicTypes.TopicType topicType = Engine.Instance.TopicTypes.FromName(topicTypeName);
						
						if (topicType == null)
							{
							errorList.Add( 
								Locale.Get("NaturalDocs.Engine", "Languages.txt.PrototypeEnderTopicTypeDoesntExist(name)", topicTypeName),
								sourceFile, configFileLanguage.LineNumber 
								);
							
							success = false;
							}
						else
							{
							string[] prototypeEnderStrings = configFileLanguage.GetPrototypeEnderStrings(topicTypeName);
							PrototypeEnders prototypeEnders = new PrototypeEnders(prototypeEnderStrings);
							language.SetPrototypeEnders(topicType.ID, prototypeEnders);
							}
						}
					}
				}
				
				
			// Apply the aliases, extensions, and shebang strings.
				
			if (configFileLanguage.Aliases != null)
				{
				
				// If using Replace Aliases, find all existing aliases pointing to this language and remove them.
				if (configFileLanguage.AlterLanguage == true && configFileLanguage.AddAliases == false)
					{
					List<string> removedAliases = new List<string>();
					
					foreach (KeyValuePair<string, Language> pair in aliases)
						{
						if ((object)pair.Value == (object)language)
							{  removedAliases.Add(pair.Key);  }
						}
						
					foreach (string removedAlias in removedAliases)
						{  aliases.Remove(removedAlias);  }
					}
				
				// Add new aliases.	
				foreach (string alias in configFileLanguage.Aliases)
					{  aliases[alias] = language;  }

				}
				
			if (configFileLanguage.Extensions != null)
				{
				
				// If using Replace Extensions, find all existing extensions pointing to this language and remove them.
				if (configFileLanguage.AlterLanguage == true && configFileLanguage.AddExtensions == false)
					{
					List<string> removedExtensions = new List<string>();
					
					foreach (KeyValuePair<string, Language> pair in extensions)
						{
						if ((object)pair.Value == (object)language)
							{  removedExtensions.Add(pair.Key);  }
						}
						
					foreach (string removedExtension in removedExtensions)
						{  extensions.Remove(removedExtension);  }
					}
				
				// Add new extensions.	
				foreach (string extension in configFileLanguage.Extensions)
					{
					if (ignoredExtensions.Contains(extension) == false)
						{  extensions[extension] = language;  }
					}

				}
				
			if (configFileLanguage.ShebangStrings != null)
				{
				
				// If using Replace Shebang Strings, find all existing shebang strings pointing to this language and remove them.
				if (configFileLanguage.AlterLanguage == true && configFileLanguage.AddShebangStrings == false)
					{
					List<string> removedShebangStrings = new List<string>();
					
					foreach (KeyValuePair<string, Language> pair in shebangStrings)
						{
						if ((object)pair.Value == (object)language)
							{  removedShebangStrings.Add(pair.Key);  }
						}
						
					foreach (string removedShebangString in removedShebangStrings)
						{  shebangStrings.Remove(removedShebangString);  }
					}
				
				// Add new shebang strings.
				foreach (string shebangString in configFileLanguage.ShebangStrings)
					{  shebangStrings[shebangString] = language;  }

				}
			
				
			return success;
			}
			
			
		/* Function: Start_CantDefinePropertyError
		 * A helper function used only by <Start()> and its other helper functions which adds an error saying the passed
		 * property cannot be defined for the current language type.
		 */
		private void Start_CantDefinePropertyError (ConfigFileLanguage configFileLanguage, Language.LanguageType type,
																		 Path sourceFile, string propertyName, Errors.ErrorList errorList)
			{
			string typeString;
			
			if (type == Language.LanguageType.TextFile)
				{  typeString = "TextFiles";  }
			else if (type == Language.LanguageType.Container)
				{  typeString = "Containers";  }
			else if (type == Language.LanguageType.FullSupport)
				{  typeString = "FullLanguageSupport";  }
			else // BasicSupport
				{  typeString = "BasicLanguageSupport";  }
			
			errorList.Add(
				Locale.Get( "NaturalDocs.Engine", "Languages.txt.CantDefinePropertyFor" + typeString + "(property, language)",
								  propertyName, configFileLanguage.Name ),
				sourceFile, configFileLanguage.LineNumber
				);
			}
			

		/* Function: Start_FixCapitalization
		 * 
		 * A helper function used only by <Start()> which cleans up the capitalization of <ConfigFileLanguages> such as by 
		 * making Alter Language entries match the original language and making Topic Type Prototype Enders match the
		 * original topic type.
		 * 
		 * Assumes <languages> and <TopicTypes.Manager> are already filled in and valid.
		 */
		public void Start_FixCapitalization (List<ConfigFileLanguage> languageList)
			{
			foreach (ConfigFileLanguage configFileLanguage in languageList)
				{
				if (configFileLanguage.AlterLanguage == true)
					{
					configFileLanguage.FixNameCapitalization( languages[configFileLanguage.Name].Name );
					}
				
				string[] prototypeEnderTopicTypeNames = configFileLanguage.GetTopicTypeNamesWithPrototypeEnders();
				
				if (prototypeEnderTopicTypeNames != null)
					{
					for (int i = 0; i < prototypeEnderTopicTypeNames.Length; i++)
						{
						prototypeEnderTopicTypeNames[i] = Engine.Instance.TopicTypes.FromName( prototypeEnderTopicTypeNames[i] ).Name;
						}
						
					configFileLanguage.FixPrototypeEnderTopicTypeCapitalization( prototypeEnderTopicTypeNames );
					}
				}
			}

			
		/* Function: FromFileName
		 * 
		 * Returns the <Language> associated with the passed file name, or null if none.
		 * 
		 * Note that this will *not* open files to search for things like shebang strings.  If the file name indicates a container 
		 * language like Shebang Script, it will return that container's language information.
		 * 
		 * If the file name has no extension, it will return the language information for Shebang Script if it is defined, or null
		 * if it is not.
		 */
		public Language FromFileName (Path filename)
			{
			return FromExtension(filename.Extension);
			}
		 
		/* Function: FromExtension
		 * 
		 * Returns the <Language> associated with the passed extension, or null if none.
		 * 
		 * If you pass null or an empty string, it will return the language information for Shebang Script if it is defined, or null
		 * if it is not.
		 */
		public Language FromExtension (string extension)
			{
			if (String.IsNullOrEmpty(extension))
				{  return FromName("Shebang Script");  }
			else
				{  return extensions[extension];  }
			}
			
		/* Function: FromShebangLine
		 * Returns the <Language> associated with the passed shebang line, or null if none.  Pass the entire line; this function
		 * will handle picking out the substrings.
		 */
		public Language FromShebangLine (string shebangLine)
			{
			string lcShebangLine = shebangLine.ToLower();
			
			foreach (KeyValuePair<string, Language> shebangStringPair in shebangStrings)
				{
				if (lcShebangLine.Contains( shebangStringPair.Key.ToLower() ))
					{  return shebangStringPair.Value;  }
				}
				
			return null;
			}

		/* Function: FromName
		 * Returns the <Language> associated with the passed name or alias, or null if none.
		 */
		public Language FromName (string name)
			{
			Language result = languages[name];
			
			if (result == null)
				{  result = aliases[name];  }
				
			return result;
			}
			
		/* Function: FromID
		 * Returns the <Language> associated with the passed ID, or null if none.
		 */
		public Language FromID (int id)
			{
			return languages[id];
			}
			
			
			
			
		// Group: Static Functions
		// __________________________________________________________________________
		
		
		/* Function: LoadFile
		 * 
		 * Loads the passed configuration file and parses it.  Redundant information will be simplified out, such as an Alter
		 * Language section that applies to a language defined in the same file.
		 * 
		 * Parameters:
		 * 
		 *		filename - The <Path> where the file is located.
		 *		fileLanguages - Returns a list of <ConfigFileLanguages> in no particular order.
		 *		fileIgnoredExtensions - Returns any ignored extensions as a string array.
		 *		errorList - If it couldn't successfully parse the file it will add error messages to this list.
		 *		
		 * Returns:
		 * 
		 *		Whether it was able to successfully load and parse the file without any errors.
		 */
		public static bool LoadFile (Path filename, out List<ConfigFileLanguage> fileLanguages, 
											  out List<string> fileIgnoredExtensions, Errors.ErrorList errorList)
			{
			fileLanguages = new List<ConfigFileLanguage>();
			fileIgnoredExtensions = new List<string>();
			StringTable<ConfigFileLanguage> fileLanguageNames = 
				new StringTable<ConfigFileLanguage>(LanguageNamesIgnoreCase, LanguageNamesAreNormalized);
			int previousErrorCount = errorList.Count;

			using (ConfigFile file = new ConfigFile())
				{
				// Can't make identifiers lowercase here or we'd lose the case of the topic type in prototype ender lines.
				bool openResult = file.Open(filename, 
														 ConfigFile.FileFormatFlags.CondenseIdentifierWhitespace |
														 ConfigFile.FileFormatFlags.CondenseValueWhitespace,
														 errorList);
														 
				if (openResult == false)
					{  return false;  }
					
				string identifier, lcIdentifier, value;
				ConfigFileLanguage currentLanguage = null;
				
				// We need this in addition to ConfigFileLanguage.AlterLanguage because an entry altering a type defined in the 
				// same file would be combined into the original, yet we still need to know if that entry is Alter to properly
				// detect whether we need to use Add/Replace with certain properties.
				bool alterCurrentLanguage = false;
				
				char[] space = { ' ' };

				Regex.Languages.AddReplaceAliases addReplaceAliasesRegex = new Regex.Languages.AddReplaceAliases();
				Regex.Languages.AddReplaceExtensions addReplaceExtensionsRegex = new Regex.Languages.AddReplaceExtensions();
				Regex.Languages.AddReplaceShebangStrings addReplaceShebangStringsRegex = new Regex.Languages.AddReplaceShebangStrings();
				Regex.Languages.Aliases aliasesRegex = new Regex.Languages.Aliases();
				Regex.Languages.AlterLanguage alterLanguageRegex = new Regex.Languages.AlterLanguage();
				Regex.Languages.BlockComments blockCommentsRegex = new Regex.Languages.BlockComments();
				Regex.Languages.EnumValues enumValuesRegex = new Regex.Languages.EnumValues();
				Regex.Languages.Extensions extensionsRegex = new Regex.Languages.Extensions();
				Regex.Languages.IgnorePrefixes ignorePrefixesRegex = new Regex.Languages.IgnorePrefixes();
				Regex.Languages.IgnoreExtensions ignoreExtensionsRegex = new Regex.Languages.IgnoreExtensions();
				Regex.Languages.LineComments lineCommentsRegex = new Regex.Languages.LineComments();
				Regex.Languages.PrototypeEnders prototypeEndersRegex = new Regex.Languages.PrototypeEnders();
				Regex.Languages.ShebangStrings shebangStringsRegex = new Regex.Languages.ShebangStrings();
				Regex.Languages.MemberOperator memberOperatorRegex = new Regex.Languages.MemberOperator();
				Regex.NonASCIILetters nonASCIILettersRegex = new Regex.NonASCIILetters();
				
				System.Text.RegularExpressions.Match match;
				

				while (file.Get(out identifier, out value))
					{
					lcIdentifier = identifier.ToLower();

					//
					// Ignore Extensions
					//
					
					if (ignoreExtensionsRegex.IsMatch(lcIdentifier))
						{
						currentLanguage = null;
						
						string[] ignoredExtensionsArray = value.Split(space);
						fileIgnoredExtensions.AddRange(ignoredExtensionsArray);
						}
					
					
					//
					// Language
					//
						
					else if (lcIdentifier == "language")
						{
						if (fileLanguageNames.ContainsKey(value))
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.LanguageAlreadyExists(name)", value)
								);
							
							// Continue parsing.  We'll throw this into the existing language even though it shouldn't be overwriting
							// its values because we want to find any other errors there are in the file.
							currentLanguage = fileLanguageNames[value];
							alterCurrentLanguage = false;
							}
							
						else
							{
							currentLanguage = new ConfigFileLanguage(value, false, file.LineNumber);
							alterCurrentLanguage = false;
							fileLanguages.Add(currentLanguage);
							fileLanguageNames.Add(value, currentLanguage);
							}								
						}
						
						
						
					//
					// Alter Language
					//
					
					else if (alterLanguageRegex.IsMatch(lcIdentifier))
						{
						// If this language already exists, collapse it into the current definition.
						if (fileLanguageNames.ContainsKey(value))
							{
							currentLanguage = fileLanguageNames[value];
							alterCurrentLanguage = true;
							}
							
						// If it doesn't exist, create the new language anyway with the alter flag set because it may exist in another
						// file.
						else
							{
							currentLanguage = new ConfigFileLanguage(value, true, file.LineNumber);
							alterCurrentLanguage = true;
							fileLanguages.Add(currentLanguage);
							fileLanguageNames.Add(value, currentLanguage);
							}								
						}


					//
					// Aliases
					//
						
					else if (aliasesRegex.IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  LoadFile_NeedsLanguageError(file, identifier);  }
						else if (alterCurrentLanguage == true)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.NeedAddReplaceWhenAlteringLanguage(keyword)", "Aliases")
								);
							}
						else
							{
							currentLanguage.Aliases = value.Split(space);
							currentLanguage.AddAliases = false;
							}
						}
						
						
					//
					// Add/Replace Aliases
					//
					
					else if ( (match = addReplaceAliasesRegex.Match(lcIdentifier)) != null && match.Success )
						{
						if (currentLanguage == null)
							{  LoadFile_NeedsLanguageError(file, identifier);  }
							
						else if (alterCurrentLanguage == true && match.Groups[1].Value == "add" && 
								  currentLanguage.Aliases != null)
							{
							string[] addAliases = value.Split(space);
							string[] newAliases = new string[ addAliases.Length + currentLanguage.Aliases.Length ];
							
							currentLanguage.Aliases.CopyTo(newAliases, 0);
							addAliases.CopyTo(newAliases, currentLanguage.Aliases.Length);
							
							currentLanguage.Aliases = newAliases;
							currentLanguage.AddAliases = true;
							}
						
						// Covers "replace" when altering a language, "add" and "replace" when not altering a language (no point
						// in adding an error when we can just tolerate it, and "replace" when altering a language that doesn't have
						// anything defined.
						else
							{
							currentLanguage.Aliases = value.Split(space);
							currentLanguage.AddAliases = (match.Groups[1].Value == "add");
							}
						}
						


					//
					// Extensions
					//
						
					else if (extensionsRegex.IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  LoadFile_NeedsLanguageError(file, identifier);  }
						else if (alterCurrentLanguage == true)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.NeedAddReplaceWhenAlteringLanguage(keyword)", "Extensions")
								);
							}
						else
							{
							currentLanguage.Extensions = value.Split(space);
							currentLanguage.AddExtensions = false;
							}
						}
						
						
					//
					// Add/Replace Extensions
					//
					
					else if ( (match = addReplaceExtensionsRegex.Match(lcIdentifier)) != null && match.Success )
						{
						if (currentLanguage == null)
							{  LoadFile_NeedsLanguageError(file, identifier);  }
							
						else if (alterCurrentLanguage == true && match.Groups[1].Value == "add" && 
								  currentLanguage.Extensions != null)
							{
							string[] addExtensions = value.Split(space);
							string[] newExtensions = new string[ addExtensions.Length + currentLanguage.Extensions.Length ];
							
							currentLanguage.Extensions.CopyTo(newExtensions, 0);
							addExtensions.CopyTo(newExtensions, currentLanguage.Extensions.Length);
							
							currentLanguage.Extensions = newExtensions;
							currentLanguage.AddExtensions = true;
							}
						
						// Covers "replace" when altering a language, "add" and "replace" when not altering a language (no point
						// in adding an error when we can just tolerate it, and "replace" when altering a language that doesn't have
						// anything defined.
						else
							{
							currentLanguage.Extensions = value.Split(space);
							currentLanguage.AddExtensions = (match.Groups[1].Value == "add");
							}
						}
						


					//
					// Shebang Strings
					//
						
					else if (shebangStringsRegex.IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  LoadFile_NeedsLanguageError(file, identifier);  }
						else if (alterCurrentLanguage == true)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.NeedAddReplaceWhenAlteringLanguage(keyword)", "Shebang Strings")
								);
							}
						else
							{
							currentLanguage.ShebangStrings = value.Split(space);
							currentLanguage.AddShebangStrings = false;
							}
						}
						
						
					//
					// Add/Replace Shebang Strings
					//
					
					else if ( (match = addReplaceShebangStringsRegex.Match(lcIdentifier)) != null && match.Success )
						{
						if (currentLanguage == null)
							{  LoadFile_NeedsLanguageError(file, identifier);  }
							
						else if (alterCurrentLanguage == true && match.Groups[1].Value == "add" && 
								  currentLanguage.ShebangStrings != null)
							{
							string[] addShebangStrings = value.Split(space);
							string[] newShebangStrings = new string[ addShebangStrings.Length + 
																						 currentLanguage.ShebangStrings.Length ];
							
							currentLanguage.ShebangStrings.CopyTo(newShebangStrings, 0);
							addShebangStrings.CopyTo(newShebangStrings, currentLanguage.ShebangStrings.Length);
							
							currentLanguage.ShebangStrings = newShebangStrings;
							currentLanguage.AddShebangStrings = true;
							}
						
						// Covers "replace" when altering a language, "add" and "replace" when not altering a language (no point
						// in adding an error when we can just tolerate it, and "replace" when altering a language that doesn't have
						// anything defined.
						else
							{
							currentLanguage.ShebangStrings = value.Split(space);
							currentLanguage.AddShebangStrings = (match.Groups[1].Value == "add");
							}
						}
						


					//
					// Simple Identifier
					//
					
					else if (lcIdentifier == "simple identifier")
						{
						if (currentLanguage == null)
							{  LoadFile_NeedsLanguageError(file, identifier);  }
						else if (nonASCIILettersRegex.IsMatch(value))
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.SimpleIdentifierMustOnlyBeASCIILetters(name)", value)
								);
							}
						else
							{
							currentLanguage.SimpleIdentifier = value;
							}
						}
						


					//
					// Line Comments
					//
					
					else if (lineCommentsRegex.IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  LoadFile_NeedsLanguageError(file, identifier);  }
						else
							{
							currentLanguage.LineCommentStrings = value.Split(space);
							}
						}
						


					//
					// Block Comments
					//
					
					else if (blockCommentsRegex.IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  LoadFile_NeedsLanguageError(file, identifier);  }
						else
							{
							string[] newBlockCommentStrings = value.Split(space);
							
							if (newBlockCommentStrings.Length % 2 != 0)
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Languages.txt.BlockCommentsMustHaveAnEvenNumberOfSymbols")
									);
								}
							else
								{
								currentLanguage.BlockCommentStringPairs = newBlockCommentStrings;
								}
							}
						}
						


					//
					// Member Operator
					//
					
					else if (memberOperatorRegex.IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  LoadFile_NeedsLanguageError(file, identifier);  }
						else
							{  currentLanguage.MemberOperator = value;  }
						}
						
						
						
					//
					// Line Extender
					//
					
					else if (lcIdentifier == "line extender")
						{
						if (currentLanguage == null)
							{  LoadFile_NeedsLanguageError(file, identifier);  }
						else
							{  currentLanguage.LineExtender = value;  }
						}
						
						
						
					//
					// Enum Values
					//
					
					else if (enumValuesRegex.IsMatch(lcIdentifier))
						{
						string lcValue = value.ToLower();
						
						if (currentLanguage == null)
							{  LoadFile_NeedsLanguageError(file, identifier);  }
						else if (lcValue == "global")
							{  currentLanguage.EnumValue = Language.EnumValues.Global;  }
						else if (lcValue == "under type")
							{  currentLanguage.EnumValue = Language.EnumValues.UnderType;  }
						else if (lcValue == "under parent")
							{  currentLanguage.EnumValue = Language.EnumValues.UnderParent;  }
						else
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.InvalidEnumValue(value)", value)
								);
							}
						}
						
						
					//
					// Prototype Enders
					//
					
					// Use identifier and not lcIdentifier to keep the case of the topic type.  The regex will compensate.
					else if ( (match = prototypeEndersRegex.Match(identifier)) != null && match.Success )
						{
						if (currentLanguage == null)
							{  LoadFile_NeedsLanguageError(file, identifier);  }
						else
							{
							string topicType = match.Groups[1].Value;
							string[] enderStrings = value.Split(space);

							currentLanguage.SetPrototypeEnderStrings(topicType, enderStrings);
							}
						}
						
						
					//
					// Deprecated keywords
					//
					
					else if ( ignorePrefixesRegex.IsMatch(lcIdentifier) || 
								lcIdentifier == "perl package" || lcIdentifier == "full language support" )
						{
						// Ignore
						}
						
						
					//
					// Unrecognized keywords
					//
					
					else
					    {
					    file.AddError(
					        Locale.Get("NaturalDocs.Engine", "Languages.txt.UnrecognizedKeyword(keyword)", identifier)
					        );
					    }
										
					}  // while (file.Get)

				file.Close();				
				}
				
				
			if (errorList.Count == previousErrorCount)
				{  return true;  }
			else
				{  return false;  }
			}
			

		/* Function: LoadFile_NeedsLanguageError
		 * A shortcut function only used by <LoadFile()> which adds an error stating that the passed keyword needs to appear
		 * in a language section.
		 */
		private static void LoadFile_NeedsLanguageError (ConfigFile file, string identifier)
			{
			file.AddError(
				Locale.Get("NaturalDocs.Engine", "Languages.txt.KeywordMustBeInLanguage(keyword)", identifier)
				);
			}
			
			
		/* Function: SaveFile
		 * 
		 * Saves the passed information into a configuration file if it's different from the one on disk.
		 * 
		 * Parameters:
		 * 
		 *		filename - The <Path> where the file is located.
		 *		languages - A list of <ConfigFileLanguages>.  
		 *		ignoredExtensions - A string array of ignored extensions in the format of.
		 *		errorList - If it couldn't successfully save the file it will add error messages to this list.
		 *		isProjectFile - Whether the file is for a project configuration folder as opposed to the system folder.
		 *		noErrorOnFail - Prevents errors from being added to errorList if the function fails.  Used when a file may be in a
		 *							   shared, read-only location and it's not critical if it's saved.
		 *		
		 * Returns:
		 * 
		 *		Whether it was able to successfully save the file without any errors.  If the file didn't need saving because
		 *		the generated file was the same as the one on disk, this will still return true.
		 */
		public static bool SaveFile (Path filename, List<ConfigFileLanguage> languages, 
											  List<string> ignoredExtensions, Errors.ErrorList errorList, bool isProjectFile, bool noErrorOnFail)
			{
			System.Text.StringBuilder output = new System.Text.StringBuilder(1024);
			string projectSystem = (isProjectFile ? "Project" : "System");
			
			
			//
			// Create header
			//
			
			output.AppendLine("Format: " + Engine.Instance.VersionString);
			output.AppendLine();
			output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt." + projectSystem + "Header.multiline") );
			output.AppendLine();
			output.AppendLine();
			
			if (ignoredExtensions.Count > 0)
				{
				output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.IgnoredExtensionsHeader.multiline") );
				output.AppendLine();
				
				if (ignoredExtensions.Count == 1)
					{  output.AppendLine("Ignore Extension: " + ignoredExtensions[0]);  }
				else
					{  
					output.Append("Ignore Extensions:");

					foreach (string extension in ignoredExtensions)
						{  output.Append(" " + extension);  }
						
					output.AppendLine();
					}

				output.AppendLine();
				output.AppendLine();
				}
			else if (isProjectFile)
				{
				output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.IgnoredExtensionsHeader.multiline") );
				output.AppendLine();

				output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.IgnoredExtensionsReference.multiline") );
				output.AppendLine();
				output.AppendLine();
				}
			// Add nothing for the system config file.
			

			output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.LanguagesHeader.multiline") );

			if (languages.Count > 1)
				{  output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.DeferredLanguagesReference.multiline") );  }

			output.AppendLine();
				
			
			//
			// Create content
			//
			
			languages.Sort( 
				delegate (ConfigFileLanguage a, ConfigFileLanguage b)
					{  return a.LineNumber - b.LineNumber;  } 
				);
				
			foreach (ConfigFileLanguage language in languages)
				{
				if (language.AlterLanguage == true)
					{  output.Append("Alter ");  }
					
				output.AppendLine("Language: " + language.Name);
				
				int oldGroupNumber = 0;

				if (language.Extensions != null)
					{
					SaveFile_LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					
					if (language.AlterLanguage == true)
						{
						if (language.AddExtensions == true)
							{  
							if (language.Extensions.Length == 1)
								{  output.Append("   Add Extension: ");  }
							else
								{  output.Append("   Add Extensions: ");  }
							}
						else
							{  output.Append("   Replace Extensions: ");  }  // Regardless of singular or plural
						}
					else
						{  
						if (language.Extensions.Length == 1)
							{  output.Append("   Extension: ");  }
						else
							{  output.Append("   Extensions: ");  }
						}
					
					output.AppendLine(string.Join(" ", language.Extensions));
					}
					
				if (language.ShebangStrings != null)
					{
					SaveFile_LineBreakOnGroupChange(1, ref oldGroupNumber, output);

					if (language.AlterLanguage == true)
						{
						if (language.AddShebangStrings == true)
							{  
							if (language.ShebangStrings.Length == 1)
								{  output.Append("   Add Shebang String: ");  }
							else
								{  output.Append("   Add Shebang Strings: ");  }
							}
						else
							{  output.Append("   Replace Shebang Strings: ");  }  // Regardless of singular or plural
						}
					else
						{  
						if (language.ShebangStrings.Length == 1)
							{  output.Append("   Shebang String: ");  }
						else
							{  output.Append("   Shebang Strings: ");  }
						}

					output.AppendLine(string.Join(" ", language.ShebangStrings));
					}
					
				if (language.SimpleIdentifier != null)
					{  
					SaveFile_LineBreakOnGroupChange(2, ref oldGroupNumber, output);
					output.AppendLine("   Simple Identifier: " + language.SimpleIdentifier);  
					}
					
				if (language.Aliases != null)
					{
					SaveFile_LineBreakOnGroupChange(2, ref oldGroupNumber, output);

					if (language.AlterLanguage == true)
						{
						if (language.AddAliases == true)
							{  
							if (language.Aliases.Length == 1)
								{  output.Append("   Add Alias: ");  }
							else
								{  output.Append("   Add Aliases: ");  }
							}
						else
							{  output.Append("   Replace Aliases: ");  }  // Regardless of singular or plural
						}
					else
						{  
						if (language.Aliases.Length == 1)
							{  output.Append("   Alias: ");  }
						else
							{  output.Append("   Aliases: ");  }
						}

					output.AppendLine(string.Join(" ", language.Aliases));
					}
					
				if (language.LineCommentStrings != null)
					{  
					SaveFile_LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					output.AppendLine("   Line Comment: " + string.Join(" ", language.LineCommentStrings));  
					}
				if (language.BlockCommentStringPairs != null)
					{  
					SaveFile_LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					output.AppendLine("   Block Comment: " + string.Join(" ", language.BlockCommentStringPairs));  
					}
				if (language.MemberOperator != null)
					{  
					SaveFile_LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					output.AppendLine("   Member Operator: " + language.MemberOperator);  
					}
				if (language.LineExtender != null)
					{  
					SaveFile_LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					output.AppendLine("   Line Extender: " + language.LineExtender);  
					}
				if (language.EnumValue != null)
					{  
					SaveFile_LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					output.Append("   Enum Value: ");
					
					if (language.EnumValue == Language.EnumValues.Global)
						{  output.AppendLine("Global");  }
					else if (language.EnumValue == Language.EnumValues.UnderParent)
						{  output.AppendLine("Under Parent");  }
					else // Language.EnumValues.UnderType
						{  output.AppendLine("Under Type");  }
					}
				
				string[] topicTypeNamesWithPrototypeEnders = language.GetTopicTypeNamesWithPrototypeEnders();
				
				if (topicTypeNamesWithPrototypeEnders != null)
					{
					SaveFile_LineBreakOnGroupChange(4, ref oldGroupNumber, output);

					foreach (string topicTypeName in topicTypeNamesWithPrototypeEnders)
						{
						string[] prototypeEnderStrings = language.GetPrototypeEnderStrings(topicTypeName);
						
						if (prototypeEnderStrings.Length == 1)
							{  
							output.AppendLine( "   " + topicTypeName + " Prototype Ender: " + prototypeEnderStrings[0] );  
							}
						else
							{  
							output.AppendLine( "   " + topicTypeName + " Prototype Enders: " + string.Join(" ", prototypeEnderStrings) );  
							}
						}
					}

				output.AppendLine();
				output.AppendLine();
				}


			output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt." + projectSystem + "LanguagesReference.multiline") );

				
				
			//
			// Compare with previous file and write to disk
			//
			
			return ConfigFile.SaveIfDifferent(filename, output.ToString(), noErrorOnFail, errorList);
			}
			
			
		/* Function: SaveFile_LineBreakOnGroupChange
		 * A shortcut function used only by <SaveFile()> which inserts a line break between groups.  It will also update 
		 * oldGroupNumber automatically.
		 */
		private static void SaveFile_LineBreakOnGroupChange (int groupNumber, ref int oldGroupNumber,
																					    System.Text.StringBuilder output)
			{
			if (groupNumber != oldGroupNumber)
				{
				output.AppendLine();
				oldGroupNumber = groupNumber;
				}
			}
			
			
		/* Function: LoadBinaryFile
		 * Loads the information in <Languages.nd>, which is the computed language settings from the last time Natural Docs 
		 * was run.  Returns whether it was successful.  If not all the out parameters will still return objects, they will just be 
		 * empty.  
		 */
		public static bool LoadBinaryFile (Path filename,
																		out List<Language> languages, 
																		out List<KeyValuePair<string, int>> aliases,
																 		out List<KeyValuePair<string, int>> extensions,
																		out List<KeyValuePair<string, int>> shebangStrings, 
																		out List<string> ignoredExtensions)
			{
			languages = new List<Language>();
			
			aliases = new List<KeyValuePair<string,int>>();
			extensions = new List<KeyValuePair<string,int>>();
			shebangStrings = new List<KeyValuePair<string,int>>();
			ignoredExtensions = new List<string>();
			
			BinaryFile file = new BinaryFile();
			bool result = true;
			IDObjects.NumberSet usedLanguageIDs = new IDObjects.NumberSet();
			
			try
				{
				if (file.OpenForReading(filename, "2.0") == false)
					{
					result = false;
					}
				else
					{
						
					// [String: Language Name]
					// [Int32: ID]
					// [Byte: Type]
					// [String: Simple Identifier]
					// [Byte: Enum Values]
					// [String: Member Operator Symbol]
					// [String: Line Extender Symbol]
					// [String: Line Comment Symbol] [] ... [String: null]
					// [String: Opening Block Comment Symbol] [String: Closing Block Comment Symbo] [] [] ... [String: null]
					// [String: Opening Javadoc Line Comment Symbol] [String: Remainder Javadoc Line Comment Symbol [] ... [String: null]
					// [String: Opening Javadoc Block Comment Symbol] [String: Closing Javadoc Block Comment Symbol] [] [] ... [String: null]
					// [String: XML Line Comment Symbol] [] ... [String: null]
					
					// [Int32: Topic Type ID]
					// [Byte: Include Line Breaks (1 or 0)]
					// [String: Prototype Ender Symbol] [] ... [String: null]
					// ...
					// [Int32: 0]
					
					// ...
					// [String: null]
						
					for (string languageName = file.ReadString();
						  languageName != null;
						  languageName = file.ReadString())
						{
						Language language = new Language(languageName);
						
						language.ID = file.ReadInt32();
						
						byte rawTypeValue = file.ReadByte();
						if (Enum.IsDefined(typeof(Language.LanguageType), rawTypeValue))
							{  language.Type = (Language.LanguageType)rawTypeValue;  }
						else
							{  result = false;  }
							
						language.SimpleIdentifier = file.ReadString();
						
						byte rawEnumValue = file.ReadByte();
						if (Enum.IsDefined(typeof(Language.EnumValues), rawEnumValue))
							{  language.EnumValue = (Language.EnumValues)rawEnumValue;  }
						else
							{  result = false;  }
							
						
						language.MemberOperator = file.ReadString();
						language.LineExtender = file.ReadString();

						language.LineCommentStrings = LoadBinaryFile_ReadStringArray(file);
						language.BlockCommentStringPairs = LoadBinaryFile_ReadStringArray(file);
						language.JavadocLineCommentStringPairs = LoadBinaryFile_ReadStringArray(file);
						language.JavadocBlockCommentStringPairs = LoadBinaryFile_ReadStringArray(file);
						language.XMLLineCommentStrings = LoadBinaryFile_ReadStringArray(file);
							
						for (int topicTypeID = file.ReadInt32();
							  topicTypeID != 0;
							  topicTypeID = file.ReadInt32())
							{
							bool includeLineBreaks = (file.ReadByte() == 1);
							string[] enderSymbols = LoadBinaryFile_ReadStringArray(file);

							language.SetPrototypeEnders(topicTypeID, new PrototypeEnders(enderSymbols, includeLineBreaks));
							}
						
						languages.Add(language);
						usedLanguageIDs.Add(language.ID);
						}

						
					// [String: Alias] [Int32: Language ID] [] [] ... [String: Null]
					
					for (string alias = file.ReadString();
						  alias != null;
						  alias = file.ReadString())
						{
						int languageID = file.ReadInt32();
						
						if (usedLanguageIDs.Contains(languageID) == true)
							{
							aliases.Add( new KeyValuePair<string, int>(alias, languageID) );
							}
						else
							{
							result = false;
							}
						}
						
					// [String: Extension] [Int32: Language ID] [] [] ... [String: Null]
					
					for (string extension = file.ReadString();
						  extension != null;
						  extension = file.ReadString())
						{
						int languageID = file.ReadInt32();
						
						if (usedLanguageIDs.Contains(languageID) == true)
							{
							extensions.Add( new KeyValuePair<string, int>(extension, languageID) );
							}
						else
							{
							result = false;
							}
						}
						
					// [String: Shebang String] [Int32: Language ID] [] [] ... [String: Null]

					for (string shebangString = file.ReadString();
						  shebangString != null;
						  shebangString = file.ReadString())
						{
						int languageID = file.ReadInt32();
						
						if (usedLanguageIDs.Contains(languageID) == true)
							{
							shebangStrings.Add( new KeyValuePair<string, int>(shebangString, languageID) );
							}
						else
							{
							result = false;
							}
						}

					// [String: Ignored Extension] [] ... [String: Null]

					for (string ignoredExtension = file.ReadString();
						  ignoredExtension != null;
						  ignoredExtension = file.ReadString())
						{
						ignoredExtensions.Add(ignoredExtension);
						}
					}
				}
			catch
				{
				result = false;
				}
			finally
				{
				file.Close();
				}
				
			if (result == false)
				{
				// Reset all the objects to empty versions.
				languages.Clear();

				extensions.Clear();
				shebangStrings.Clear();
				ignoredExtensions.Clear();				
				}
				
			return result;
			}
			
			
		/* Function: LoadBinaryFile_ReadStringArray
		 * A helper function used only by <LoadBinaryFile()> which loads a sequence of strings into an array.  The sequence ends
		 * when a null string is encountered.  If there are no strings in the sequence (the first one is null) it returns null instead of
		 * an empty array.
		 */
		protected static string[] LoadBinaryFile_ReadStringArray (BinaryFile file)
			{
			string stringFromFile = file.ReadString();
			
			if (stringFromFile == null)
				{  return null;  }
				
			List<string> stringList = new List<string>();

			do			
				{
				stringList.Add(stringFromFile);
				stringFromFile = file.ReadString();
				}
			while (stringFromFile != null);
				
			return stringList.ToArray();
			}

			
		/* Function: SaveBinaryFile
		 * Saves the current computed languages into <Languages.nd>.  Throws an exception if unsuccessful.
		 */
		public static void SaveBinaryFile (Path filename, IDObjects.Manager<Language> languages,
																		StringTable<Language> aliases, StringTable<Language> extensions, 
																		SortedStringTable<Language> shebangStrings, StringSet ignoredExtensions)
			{
			BinaryFile file = new BinaryFile();
			file.OpenForWriting(filename);

			try
				{

				// [String: Language Name]
				// [Int32: ID]
				// [Byte: Type]
				// [String: Simple Identifier]
				// [Byte: Enum Values]
				// [String: Member Operator Symbol]
				// [String: Line Extender Symbol]
				// [String: Line Comment Symbol] [] ... [String: null]
				// [String: Opening Block Comment Symbol] [String: Closing Block Comment Symbo] [] [] ... [String: null]
				// [String: Opening Javadoc Line Comment Symbol] [String: Remainder Javadoc Line Comment Symbol [] ... [String: null]
				// [String: Opening Javadoc Block Comment Symbol] [String: Closing Javadoc Block Comment Symbol] [] [] ... [String: null]
				// [String: XML Line Comment Symbol] [] ... [String: null]
				
				// [Int32: Topic Type ID]
				// [Byte: Include Line Breaks (0 or 1)]
				// [String: Prototype Ender Symbol] [] ... [String: null]
				// ...
				// [Int32: 0]
				
				// ...
				// [String: null]

				foreach (Language language in languages)
					{
					file.WriteString( language.Name );
					file.WriteInt32( language.ID );
					file.WriteByte( (byte)language.Type );
					file.WriteString( language.SimpleIdentifier );
					file.WriteByte( (byte)language.EnumValue );
					file.WriteString( language.MemberOperator );
					file.WriteString( language.LineExtender );
					
					SaveBinaryFile_WriteStringArray(file, language.LineCommentStrings);
					SaveBinaryFile_WriteStringArray(file, language.BlockCommentStringPairs);
					SaveBinaryFile_WriteStringArray(file, language.JavadocLineCommentStringPairs);
					SaveBinaryFile_WriteStringArray(file, language.JavadocBlockCommentStringPairs);
					SaveBinaryFile_WriteStringArray(file, language.XMLLineCommentStrings);

					int[] topicTypes = language.GetTopicTypesWithPrototypeEnders();
					if (topicTypes != null)
						{
						foreach (int topicType in topicTypes)
							{
							PrototypeEnders prototypeEnders = language.GetPrototypeEnders(topicType);

							file.WriteInt32(topicType);
							file.WriteByte( (byte)(prototypeEnders.IncludeLineBreaks ? 1 : 0) );
							SaveBinaryFile_WriteStringArray(file, prototypeEnders.Symbols);
							}
						}
					file.WriteInt32(0);					
					}
					
				file.WriteString(null);
				
				
				// [String: Alias] [Int32: Language ID] [] [] ... [String: Null]

				foreach (KeyValuePair<string, Language> pair in aliases)
					{
					file.WriteString( pair.Key );
					file.WriteInt32( pair.Value.ID );
					}
					
				file.WriteString(null);
				
				
				// [String: Extension] [Int32: Language ID] [] [] ... [String: Null]

				foreach (KeyValuePair<string, Language> pair in extensions)
					{
					file.WriteString( pair.Key );
					file.WriteInt32( pair.Value.ID );
					}
					
				file.WriteString(null);
				
				
				// [String: Shebang String] [Int32: Language ID] [] [] ... [String: Null]

				foreach (KeyValuePair<string, Language> pair in shebangStrings)
					{
					file.WriteString( pair.Key );
					file.WriteInt32( pair.Value.ID );
					}
					
				file.WriteString(null);
				
				
				// [String: Ignored Extension] [] ... [String: Null]

				foreach (string ignoredExtension in ignoredExtensions)
					{
					file.WriteString( ignoredExtension );
					}
					
				file.WriteString(null);
				}
				
			finally
				{
				file.Close();
				}
			}
			
			
		/* Function: SaveBinaryFile_WriteStringArray
		 * A helper function used only by <SaveBinaryFile()> which writes a string array to the file.  The strings
		 * are written in sequence and followed by a null string.  It is okay to pass null to this function, it will be
		 * treated as an empty array.
		 */
		protected static void SaveBinaryFile_WriteStringArray (BinaryFile file, string[] stringArray)
			{
			if (stringArray != null)
				{
				foreach (string stringFromArray in stringArray)
					{  file.WriteString(stringFromArray);  }
				}
				
			file.WriteString(null);
			}


		
		// Group: Variables
		// __________________________________________________________________________
		
	
		/* var: languages
		 * Manages all the <Language>s by their case-insensitive name or ID number.
		 */
		protected IDObjects.Manager<Language> languages;
		
		
		/* var: aliases
		 * A <StringTable> mapping aliases to the <Language>s they represent.
		 */
		protected StringTable<Language> aliases;

		
		/* var: extensions
		 * A <StringTable> mapping file extensions to the <Language>s they represent.
		 */
		protected StringTable<Language> extensions;
		
		
		/* var: shebangStrings
		 * A <SortedStringTable> mapping shebang strings to the <Language>s they represent.  Using
		 * <ShebangStringComparer> ensures that longer strings appear first when enumerating the entries.
		 */
		protected SortedStringTable<Language> shebangStrings;
		
		
		/* var: predefinedLanguages
		 * An array of <Language>s predefined by the engine because they require special settings or objects.  These
		 * languages will not appear in the <languages> structure if they are not also defined in <Languages.txt>.
		 */
		protected Language[] predefinedLanguages;
		
		}
	}