/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.Manager
 * ____________________________________________________________________________
 * 
 * A module to handle <Languages.txt> and all the language parsers within Natural Docs.
 * 
 * 
 * Topic: Usage
 * 
 *		- Call <Engine.Instance.Start()> which will start this module.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Languages
	{
	public class Manager : Module
		{
		
		// Group: Constants
		// __________________________________________________________________________
		
		
		public const KeySettings KeySettingsForLanguageName = KeySettings.IgnoreCase | KeySettings.NormalizeUnicode;
		public const KeySettings KeySettingsForExtensions = KeySettings.IgnoreCase;
		public const KeySettings KeySettingsForShebangStrings = KeySettings.IgnoreCase;
		 
		 
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Manager
		 */
		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			languages = new IDObjects.Manager<Language>(KeySettingsForLanguageName, false);
			aliases = new StringTable<Language>(KeySettingsForLanguageName);
			extensions = new StringTable<Language>(KeySettingsForExtensions);
			shebangStrings = new SortedStringTable<Language>(new ShebangStringComparer(), KeySettingsForShebangStrings);
			
			predefinedLanguages = new Language[9];
			
			predefinedLanguages[0] = new Language(this, "Text File");
			predefinedLanguages[0].Type = Language.LanguageType.TextFile;
			predefinedLanguages[0].Predefined = true;
			
			predefinedLanguages[1] = new Languages.Parsers.ShebangScript(this);
			predefinedLanguages[1].Predefined = true;

			predefinedLanguages[2] = new Languages.Parsers.CSharp(this);
			predefinedLanguages[2].Predefined = true;

			predefinedLanguages[3] = new Languages.Parsers.Perl(this);
			predefinedLanguages[3].Predefined = true;

			predefinedLanguages[4] = new Languages.Parsers.Python(this);
			predefinedLanguages[4].Predefined = true;
			
			predefinedLanguages[5] = new Languages.Parsers.Ruby(this);
			predefinedLanguages[5].Predefined = true;

			predefinedLanguages[6] = new Languages.Parsers.SQL(this);
			predefinedLanguages[6].Predefined = true;

			predefinedLanguages[7] = new Languages.Parsers.Java(this);
			predefinedLanguages[7].Predefined = true;

			predefinedLanguages[8] = new Languages.Parsers.Lua(this);
			predefinedLanguages[8].Predefined = true;
			}


		protected override void Dispose (bool strictRulesApply)
			{
			}


		/* Function: Start
		 * 
		 * Loads and combines the two versions of <Languages.txt>, returning whether it was successful.  If there were any errors
		 * they will be added to errorList.
		 * 
		 * Dependencies:
		 * 
		 *		- <Config.Manager> and <CommentTypes.Manager> must be started before using the rest of the class.
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

			Languages_nd languagesNDParser = new Languages_nd(this);

			// Don't bother going through the effort if we're rebuilding everything anyway.
			if (EngineInstance.Config.ReparseEverything == true)
				{
				binaryLanguages = new List<Language>();
				binaryAliases = new List<KeyValuePair<string,int>>();
				binaryExtensions = new List<KeyValuePair<string,int>>();
				binaryShebangStrings = new List<KeyValuePair<string,int>>();
				binaryIgnoredExtensions = new List<string>();
				
				changed = true;
				}
				
			else if (languagesNDParser.Load(EngineInstance.Config.WorkingDataFolder + "/Languages.nd", out binaryLanguages,
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
							Language newLanguage = new Language(this, binaryLanguage.Name);
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
			
			
			Path systemFile = EngineInstance.Config.SystemConfigFolder + "/Languages.txt";
			Path projectFile = EngineInstance.Config.ProjectConfigFolder + "/Languages.txt";

			Languages_txt languagesTxtParser = new Languages_txt();

			
			// Load the files.
			
			if (!languagesTxtParser.Load( systemFile, out systemLanguageList, out ignoredSystemExtensions, errorList ))
			    {  
			    success = false;  
			    // Continue anyway because we want to show errors from both files.
			    }
			
			if (System.IO.File.Exists(projectFile))
			    {
			    if (!languagesTxtParser.Load( projectFile, out projectLanguageList, out ignoredProjectExtensions, errorList ))
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
			
			StringSet ignoredExtensions = new StringSet(KeySettingsForExtensions);
			
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
			
			if (!languagesTxtParser.Save(projectFile, projectLanguageList, ignoredProjectExtensions, errorList, true, false))
			    {  success = false;  };
				
			if (!languagesTxtParser.Save(systemFile, systemLanguageList, ignoredSystemExtensions, errorList, false, true))
			    {  success = false;  };
			
			
			// Generate alternate comment styles.  We don't want these included in the config files but we do want them in the
			// binary files in case the generation method changes in a future version.

			foreach (Language language in languages)
				{
				if (language.Type == Language.LanguageType.BasicSupport)
					{  
					language.GenerateJavadocCommentStrings();
					language.GenerateXMLCommentStrings();
					}
				}

			
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

			
			languagesNDParser.Save(EngineInstance.Config.WorkingDataFolder + "/Languages.nd",
												languages, aliases, extensions, shebangStrings, ignoredExtensions);
								   
			
			if (success == true && changed == true)
			    {  EngineInstance.Config.ReparseEverything = true;  }

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
					Language newLanguage = new Language(this, configFileLanguage.Name);
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

			if (configFileLanguage.CaseSensitive != null)
				{
				if (language.Type != Language.LanguageType.BasicSupport && language.Type != Language.LanguageType.TextFile)
					{  
					Start_CantDefinePropertyError(configFileLanguage, language.Type, sourceFile, "Case Sensitive", errorList);  
					success = false;
					}
				else
					{  language.CaseSensitive = (bool)configFileLanguage.CaseSensitive;  }
				}
				
			string[] commentTypeNamesWithPrototypeEnders = configFileLanguage.GetCommentTypeNamesWithPrototypeEnders();
			
			if (commentTypeNamesWithPrototypeEnders != null)
				{
				if (language.Type != Language.LanguageType.BasicSupport)
					{	
					Start_CantDefinePropertyError(configFileLanguage, language.Type, sourceFile, "Prototype Enders", errorList);  
					success = false;
					}
				else
					{
					foreach (string commentTypeName in commentTypeNamesWithPrototypeEnders)
						{
						CommentTypes.CommentType commentType = EngineInstance.CommentTypes.FromName(commentTypeName);
						
						if (commentType == null)
							{
							errorList.Add( 
								Locale.Get("NaturalDocs.Engine", "Languages.txt.PrototypeEnderCommentTypeDoesntExist(name)", commentTypeName),
								sourceFile, configFileLanguage.LineNumber 
								);
							
							success = false;
							}
						else
							{
							string[] prototypeEnderStrings = configFileLanguage.GetPrototypeEnderStrings(commentTypeName);
							PrototypeEnders prototypeEnders = new PrototypeEnders(prototypeEnderStrings);
							language.SetPrototypeEnders(commentType.ID, prototypeEnders);
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
		 * making Alter Language entries match the original language and making Comment Type Prototype Enders match the
		 * original comment type.
		 * 
		 * Assumes <languages> and <CommentTypes.Manager> are already filled in and valid.
		 */
		public void Start_FixCapitalization (List<ConfigFileLanguage> languageList)
			{
			foreach (ConfigFileLanguage configFileLanguage in languageList)
				{
				if (configFileLanguage.AlterLanguage == true)
					{
					configFileLanguage.FixNameCapitalization( languages[configFileLanguage.Name].Name );
					}
				
				string[] prototypeEnderCommentTypeNames = configFileLanguage.GetCommentTypeNamesWithPrototypeEnders();
				
				if (prototypeEnderCommentTypeNames != null)
					{
					for (int i = 0; i < prototypeEnderCommentTypeNames.Length; i++)
						{
						prototypeEnderCommentTypeNames[i] = EngineInstance.CommentTypes.FromName( prototypeEnderCommentTypeNames[i] ).Name;
						}
						
					configFileLanguage.FixPrototypeEnderCommentTypeCapitalization( prototypeEnderCommentTypeNames );
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