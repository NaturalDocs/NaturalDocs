/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.Manager
 * ____________________________________________________________________________
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.Languages
	{
	public partial class Manager : Module
		{
		
		// Group: Initialization Functions
		// __________________________________________________________________________
		

		/* Function: Start_Stage1
		 * 
		 * Loads and combines the two versions of <Languages.txt>, returning whether it was successful.  If there were any 
		 * errors they will be added to errorList.
		 * 
		 * Only the settings which don't depend on <Comments.txt> will be loaded.  Call <Start_Stage2()> after
		 * <CommentTypes.Manager.Start_Stage1()> has been called to complete the process.
		 * 
		 * Dependencies:
		 * 
		 *		- <Config.Manager.Start_Stage1()> must be started before this class can start.
		 */
		public bool Start_Stage1 (Errors.ErrorList errorList)
			{
			StartupIssues newStartupIssues = StartupIssues.None;
			bool success = true;
			

			//
			// Languages.txt
			//

			Path systemTextConfigPath = EngineInstance.Config.SystemConfigFolder + "/Languages.txt";
			Path projectTextConfigPath = EngineInstance.Config.ProjectConfigFolder + "/Languages.txt";

			ConfigFiles.TextFileParser textConfigFileParser = new ConfigFiles.TextFileParser();

			// Load the system Languages.txt.
			if (!textConfigFileParser.Load(systemTextConfigPath, PropertySource.SystemLanguagesFile, errorList, out systemTextConfig))
				{  success = false;  }
			
			// Load the project Languages.txt.  We want to do this even if the system Languages.txt failed so we get the error messages
			// from both.
			if (System.IO.File.Exists(projectTextConfigPath))
				{
				if (!textConfigFileParser.Load(projectTextConfigPath, PropertySource.ProjectLanguagesFile, errorList, out projectTextConfig))
					{  success = false;  }
				}
			// If it doesn't exist it's not an error.  Just create a blank config.
			else
				{  projectTextConfig = new ConfigFiles.TextFile();  }
				
			if (!success)
				{  return false;  }
				
			if (!ValidateLanguages(systemTextConfig, errorList))
				{  success = false;  }
			if (!ValidateLanguages(projectTextConfig, errorList))
				{  success = false;  }

			if (!success)
				{  return false;  }

			// Merge them into one combined config.  Note that this doesn't do file extensions, aliases, or shebang strings.  Only the 
			// languages and their other properties will exist in the merged config.
			if (!MergeLanguages(systemTextConfig, projectTextConfig, out mergedTextConfig, errorList))
				{  return false;  }
			if (!ValidateLanguages(mergedTextConfig, errorList))
				{  return false;  }


			//
			// Languages.nd
			//

			Path lastRunConfigPath = EngineInstance.Config.WorkingDataFolder + "/Languages.nd";
			ConfigFiles.BinaryFileParser binaryConfigFileParser = new ConfigFiles.BinaryFileParser();

			// If we need to start fresh anyway we can skip loading the file and create a blank config
			if (EngineInstance.HasIssues( StartupIssues.NeedToStartFresh |
														StartupIssues.CommentIDsInvalidated |
														StartupIssues.CodeIDsInvalidated ) ||

				// though if that wasn't the case but we failed at loading the file, the result is the same
				!binaryConfigFileParser.Load(lastRunConfigPath, out lastRunConfig))
				{
				lastRunConfig = new Config();

				newStartupIssues |= StartupIssues.NeedToReparseAllFiles |
											   StartupIssues.CodeIDsInvalidated;
				}


			//
			// Create the final config
			//

			config = new Config();


			// We go through the languages present in the merged text config files and convert them into the final config.  We 
			// need the contents of Comments.nd so we can keep the language IDs consistent from one run to the next if possible.

			IDObjects.NumberSet usedLanguageIDs = new IDObjects.NumberSet();
			IDObjects.NumberSet lastRunUsedLanguageIDs = lastRunConfig.UsedLanguageIDs();

			if (mergedTextConfig.HasLanguages)
				{
				foreach (var textLanguage in mergedTextConfig.Languages)
					{
					// First we need our base language object.  If there's a predefined language matching the name we'll use that so
					// we get its settings and parser.  If not we'll create a new object.  We shouldn't have to worry about more than one
					// language using the same predefined language object because validation should have taken care of that.
					var finalLanguage = FindPredefinedLanguage(textLanguage.Name);

					if (finalLanguage == null)
						{  finalLanguage = new Language(textLanguage.Name);  }

					// Apply the properties.  Keep going if there are errors since we want to find them all.
					if (!FinalizeLanguage(ref finalLanguage, textLanguage, errorList))
						{  success = false;  }

					// We still need to set the ID.  See if a language of the same name existed in the previous run.
					var lastRunLanguage = lastRunConfig.LanguageFromName(textLanguage.Name);

					// If there wasn't one we can assign a new ID, but pick one that isn't used in this run or the last run so there's no 
					// conflicts.
					if (lastRunLanguage == null)
						{
						int id = lastRunUsedLanguageIDs.LowestAvailable;
						
						if (usedLanguageIDs.Contains(id))
							{  id = Math.Max(usedLanguageIDs.Highest + 1, lastRunUsedLanguageIDs.Highest + 1);  }

						finalLanguage.ID = id;
						config.AddLanguage(finalLanguage);

						usedLanguageIDs.Add(finalLanguage.ID);
						lastRunUsedLanguageIDs.Add(finalLanguage.ID);
						}
					// If the language did exist but we haven't used its ID yet, we can keep it.
					else if (!usedLanguageIDs.Contains(lastRunLanguage.ID))
						{
						finalLanguage.ID = lastRunLanguage.ID;
						config.AddLanguage(finalLanguage);

						usedLanguageIDs.Add(finalLanguage.ID);
						}
					// However, if the language did exist and we assigned that ID already, then we have a conflict and have to tell the
					// engine that the IDs from the last run were invalidated.  We can just assign anything unused at this point since it
					// no longer matters.
					else
						{
						finalLanguage.ID = usedLanguageIDs.LowestAvailable;
						config.AddLanguage(finalLanguage);

						usedLanguageIDs.Add(finalLanguage.ID);
						newStartupIssues |= StartupIssues.CodeIDsInvalidated;
						}

					// Now that we have a final ID, set the simple identifier for any type that still needs it.  Some types may have it null if
					// it wasn't manually defined and the name didn't contain A-Z characters.
					if (finalLanguage.SimpleIdentifier == null)
						{  finalLanguage.SimpleIdentifier = "LanguageID" + finalLanguage.ID;  }

					// Also create a generic parser object if one wasn't inherited from a predefined language.
					if (!finalLanguage.HasParser)
						{  finalLanguage.Parser = new Parser(EngineInstance, finalLanguage);  }
					}
				}

			if (!success)
				{  return false;  }


			// Apply file extensions, aliases, and shebang strings now.

			MergeLanguageIdentifiersInto(ref config, systemTextConfig, projectTextConfig);


			// That's it for stage one.  Everything else is in stage 2.

			if (newStartupIssues != StartupIssues.None)
				{  EngineInstance.AddStartupIssues(newStartupIssues);  }
				
			return true;
			}


		/* Function: Start_Stage2
		 * 
		 * Finishes loading and and combing the two versions of <Languages.txt>, returning whether it was successful.  If there
		 * were any errors they will be added to errorList.
		 * 
		 * This must be called after <Start_Stage1()> has been called, and also <CommentTypes.Manager.Start_Stage1()>.  This 
		 * finalizes any settings which also depend on <Comments.txt>.
		 * 
		 * Dependencies:
		 * 
		 *		- <Config.Manager> must be started before this class can start.
		 *		- <Start_Stage1()> must be called and return true before this function can be called.
		 *		- <CommentTypes.Manager.Start_Stage1()> must be called and return true before this function can be called.
		 */
		public bool Start_Stage2 (Errors.ErrorList errorList)
			{
			bool success = true;

		
			// Go through the lists and apply prototype enders, since CommentTypes.Manager should have all their names now.

			if (!MergePrototypeEndersInto_Stage2(ref config, systemTextConfig, errorList))
				{  success = false;  }
			if (!MergePrototypeEndersInto_Stage2(ref config, projectTextConfig, errorList))
				{  success = false;  }

			if (!success)
				{  return false;  }


			// Now we have our final configuration and everything is okay.  Save the text files again to reformat them.
			
			TouchUp_Stage2(ref systemTextConfig, config);
			TouchUp_Stage2(ref projectTextConfig, config);

			Path systemTextConfigPath = EngineInstance.Config.SystemConfigFolder + "/Languages.txt";
			Path projectTextConfigPath = EngineInstance.Config.ProjectConfigFolder + "/Languages.txt";

			ConfigFiles.TextFileParser textConfigFileParser = new ConfigFiles.TextFileParser();

			// If the project Languages.txt didn't exist, saving the blank structure that was created will create a default one.
			if (!textConfigFileParser.Save(projectTextConfigPath, PropertySource.ProjectLanguagesFile, projectTextConfig, errorList))
				{  return false;  };
				
			// We don't care if we're not able to resave the system Languages.txt since it may be in a protected location.
			textConfigFileParser.Save(systemTextConfigPath, PropertySource.SystemLanguagesFile, systemTextConfig, errorList: null);


			// Save Languages.nd as well.			
			
			Path lastRunConfigPath = EngineInstance.Config.WorkingDataFolder + "/Languages.nd";
			ConfigFiles.BinaryFileParser binaryConfigFileParser = new ConfigFiles.BinaryFileParser();

			binaryConfigFileParser.Save(lastRunConfigPath, config);


			// Compare the config against the previous one and reparse everything if there are changes.  Changes that would invalidate
			// IDs have already been handled.

			if (config != lastRunConfig)
				{  EngineInstance.AddStartupIssues(StartupIssues.NeedToReparseAllFiles);  }


			// We're done with these variables which were only needed between start stages 1 and 2.

			systemTextConfig = null;
			projectTextConfig = null;
			mergedTextConfig = null;
			lastRunConfig = null;

			started = true;
			return true;
			}

			
			
		// Group: Initialization Support Functions
		// __________________________________________________________________________
		//
		// These functions are only used by <Start_Stage1()> and <Start_Stage2()>.
		//


		/* Function: ValidateLanguages
		 * Validates all the language settings in a <ConfigFiles.TextFile>.  Returns whether it is valid, and adds any errors it 
		 * finds to errorList.
		 */
		protected bool ValidateLanguages (ConfigFiles.TextFile configFile, Errors.ErrorList errorList)
			{
			bool success = true;

			if (configFile.HasLanguages)
				{
				foreach (var language in configFile.Languages)
					{
					if (!ValidateLanguage(language, errorList))
						{  
						success = false;  
						// Continue anyway so we can report the errors in all of them.
						}
					}
				}

			return success;
			}


		/* Function: ValidateLanguage
		 * Validates all the settings in a <ConfigFiles.TextFileLanguage>.  Returns whether it is valid, and adds any errors it finds to
		 * errorList.
		 */
		protected bool ValidateLanguage (ConfigFiles.TextFileLanguage language, Errors.ErrorList errorList)
			{

			// TextFileParser should have already normalized file extensions, so entering ".txt" or "*.txt" is converted to just "txt".

			// TextFileParser should have already validated that Simple Identifier only contains acceptable characters.

			// We'll check for Alter Language entries not matching an existing one when we merge languages.

			// We'll check for basic language support entries being applied to languages with full support when we merge languages.

			// So I guess there's nothing to do then!
			return true;
			}


		/* Function: MergeLanguages
		 * 
		 * Merges two <ConfigFiles.TextFiles> into a new one, putting all the languages into one list and applying any alter entries.
		 * This does NOT cover file extensions, aliases, or shebang strings; those will be blank in the result.  Returns the new
		 * list and whether it was successful.
		 * 
		 * Any errors will be added to errorList, such as defining a duplicate entry that doesn't use alter, or an alter entry for a 
		 * non-existent language.  All alter entries will be applied, including any appearing in the base config, so there will only be
		 * non-alter entries in the returned list.
		 */
		protected bool MergeLanguages (ConfigFiles.TextFile baseConfig, ConfigFiles.TextFile overridingConfig, 
														out ConfigFiles.TextFile combinedConfig, Errors.ErrorList errorList)
			{
			combinedConfig = new ConfigFiles.TextFile();

			// We merge the base config into the empty config instead of just copying it so any alter entries it has are applied
			if (!MergeLanguagesInto(ref combinedConfig, baseConfig, errorList) ||
				!MergeLanguagesInto(ref combinedConfig, overridingConfig, errorList))
				{
				combinedConfig = null;
				return false;
				}

			return true;
			}


		/* Function: MergeLanguagesInto
		 * Merges the languages of the second <ConfigFiles.TextFile> into the first, adding new types and applying any alter entries.
		 * This does NOT merge file extensions, aliases, or shebang strings.  The base config will be changed, even if there are errors.
		 * Returns false if there were any errors and adds them to errorList.
		 */
		protected bool MergeLanguagesInto (ref ConfigFiles.TextFile baseConfig, ConfigFiles.TextFile overridingConfig, 
															 Errors.ErrorList errorList)
			{
			bool success = true;

			if (overridingConfig.HasLanguages)
				{
				foreach (var overridingLanguage in overridingConfig.Languages)
					{
					var matchingLanguage = baseConfig.FindLanguage(overridingLanguage.Name);

					if (matchingLanguage != null)
						{
						if (overridingLanguage.AlterLanguage == false)
							{
							errorList.Add(Locale.Get("NaturalDocs.Engine", "Languages.txt.LanguageAlreadyExists(name)", overridingLanguage.Name),
												overridingLanguage.PropertyLocation);
							success = false;
							}
						else
							{
							MergeLanguageInto(ref matchingLanguage, overridingLanguage);
							}
						}

					else // no match
						{
						if (overridingLanguage.AlterLanguage == true)
							{
							errorList.Add(Locale.Get("NaturalDocs.Engine", "Languages.txt.AlteredLanguageDoesntExist(name)", overridingLanguage.Name),
												overridingLanguage.NamePropertyLocation);
							success = false;
							}
						else
							{
							baseConfig.AddLanguage( overridingLanguage.Duplicate() );
							}
						}

					}
				}

			return success;
			}


		/* Function: MergeLanguageInto
		 * Merges the settings of a <ConfigFiles.TextFileLanguage> into another one, overriding the settings of the first.  This does
		 * NOT cover file extensions, aliases, or shebang strings.  The base object will be altered.
		 */
		protected void MergeLanguageInto (ref ConfigFiles.TextFileLanguage baseLanguage, 
															ConfigFiles.TextFileLanguage overridingLanguage)
			{
			// Leave Name and PropertyLocation alone.  We'll keep the base's.

			// Leave AlterLanguage alone.  The base should be false and the overriding should be true, and we want the end
			// result to be false.

			if (overridingLanguage.HasSimpleIdentifier)
				{
				baseLanguage.SetSimpleIdentifier(overridingLanguage.SimpleIdentifier, 
																  overridingLanguage.SimpleIdentifierPropertyLocation);
				}

			// Ignore Aliases

			// Ignore FileExtensions

			// Ignore ShebangStrings

			if (overridingLanguage.HasLineCommentSymbols)
				{
				baseLanguage.SetLineCommentSymbols(overridingLanguage.LineCommentSymbols,
																		   overridingLanguage.LineCommentSymbolsPropertyLocation);
				}

			if (overridingLanguage.HasBlockCommentSymbols)
				{
				baseLanguage.SetBlockCommentSymbols(overridingLanguage.BlockCommentSymbols,
																			 overridingLanguage.BlockCommentSymbolsPropertyLocation);
				}

			if (overridingLanguage.HasMemberOperator)
				{
				baseLanguage.SetMemberOperator(overridingLanguage.MemberOperator,
																   overridingLanguage.MemberOperatorPropertyLocation);
				}

			if (overridingLanguage.HasPrototypeEnders)
				{
				foreach (var prototypeEnders in overridingLanguage.PrototypeEnders)
					{ 
					// This will automatically overwrite any ender with the same comment type.
					baseLanguage.AddPrototypeEnders(prototypeEnders);
					}
				}

			if (overridingLanguage.HasLineExtender)
				{
				baseLanguage.SetLineExtender(overridingLanguage.LineExtender,
															  overridingLanguage.LineExtenderPropertyLocation);
				}

			if (overridingLanguage.HasEnumValues)
				{
				baseLanguage.SetEnumValues(overridingLanguage.EnumValues,
															 overridingLanguage.EnumValuesPropertyLocation);
				}

			if (overridingLanguage.HasCaseSensitive)
				{
				baseLanguage.SetCaseSensitive(overridingLanguage.CaseSensitive,
																overridingLanguage.CaseSensitivePropertyLocation);
				}
			}


		/* Function: FindPredefinedLanguage
		 * Returns a <Language> from <predefinedLanguages> which matches the passed name, or null if none.
		 */
		protected Language FindPredefinedLanguage (string name)
			{
			if (predefinedLanguages == null)
				{  return null;  }

			string normalizedName = name.NormalizeKey(Config.KeySettingsForLanguageName);

			foreach (var predefinedLanguage in predefinedLanguages)
				{
				if (predefinedLanguage.Name.NormalizeKey(Config.KeySettingsForLanguageName) == normalizedName)
					{  return predefinedLanguage;  }
				}

			return null;
			}


		/* Function: FinalizeLanguage
		 * 
		 * Merges the settings of a <ConfigFiles.TextFileLanguage> into a <Language>, returning whether it was successful.  If there
		 * are any errors it will add them to the error list.
		 * 
		 * - This does not handle file extensions, aliases, or shebang strings as <Language> does not store them.
		 * 
		 * - This does not handle prototype enders as comment type names aren't available in <CommentTypes.Manager> until
		 *   stage 2.
		 * 
		 * - This will check for errors such as defining comment symbols on text files, shebang strings, or languages with full support.
		 * 
		 * - This will generate Javadoc and XML comment symbols from the line and block comment symbols for languages with
		 *   basic support, assuming they were not already set as part of a predefined language object.
		 *   
		 * - This will generate <Language.SimpleIdentifier> if one is not defined, though it may still be null if there are no acceptable
		 *   characters in the name, such as if the language name only used Japanese characters.
		 */
		protected bool FinalizeLanguage (ref Language baseLanguage, ConfigFiles.TextFileLanguage overridingLanguage,
														Errors.ErrorList errorList)
			{
			#if DEBUG
			if (baseLanguage.Name.NormalizeKey(Config.KeySettingsForLanguageName) !=
				overridingLanguage.Name.NormalizeKey(Config.KeySettingsForLanguageName))
				{  throw new Exception("Can't finalize language " + baseLanguage.Name + " with settings for " + overridingLanguage.Name + ".");  }
			#endif

			if (!ApplyProperties(ref baseLanguage, overridingLanguage, errorList))
				{  return false;  }

			GenerateJavadocCommentSymbols(baseLanguage);
			GenerateXMLCommentSymbols(baseLanguage);

			if (baseLanguage.SimpleIdentifier == null)
				{
				// This may end up as an empty string if there's no A-Z characters, such as if the name is in Japanese.  In this case
				// we want it to be "LanguageID[number]" but the number isn't determind yet, so leave it as null for now.
				string simpleIdentifier = baseLanguage.Name.OnlyAToZ();

				if (!string.IsNullOrEmpty(simpleIdentifier))
					{  baseLanguage.SimpleIdentifier = simpleIdentifier;  }
				}

			return true;
			}


		/* Function: ApplyProperties
		 * 
		 * Merges the settings of a <ConfigFiles.TextFileLanguage> into a <Language>, returning whether it was successful.  If there
		 * are any errors it will add them to the error list.
		 * 
		 * - This does not handle file extensions, aliases, or shebang strings as <Language> does not store them.
		 * 
		 * - This does not handle prototype enders as comment type names aren't available in <CommentTypes.Manager> until
		 *   stage 2.
		 * 
		 * - This will check for errors such as defining comment symbols on text files, shebang strings, or languages with full support.
		 */
		protected bool ApplyProperties (ref Language baseLanguage, ConfigFiles.TextFileLanguage overridingLanguage,
													  Errors.ErrorList errorList)
			{
			int originalErrorCount = errorList.Count;

			if (overridingLanguage.HasSimpleIdentifier)
				{  baseLanguage.SimpleIdentifier = overridingLanguage.SimpleIdentifier;  }

			if (overridingLanguage.HasLineCommentSymbols &&
				CheckForBasicLanguageSupport(baseLanguage, "Line Comments", 
															   overridingLanguage.LineCommentSymbolsPropertyLocation, errorList))
				{
				baseLanguage.LineCommentSymbols = overridingLanguage.LineCommentSymbols;
				}

			if (overridingLanguage.HasBlockCommentSymbols &&
				CheckForBasicLanguageSupport(baseLanguage, "Block Comments", 
															   overridingLanguage.BlockCommentSymbolsPropertyLocation, errorList))
				{
				baseLanguage.BlockCommentSymbols = overridingLanguage.BlockCommentSymbols;
				}

			if (overridingLanguage.HasMemberOperator &&
				( baseLanguage.Type == Language.LanguageType.TextFile ||
				  CheckForBasicLanguageSupport(baseLanguage, "Member Operator", 
																 overridingLanguage.MemberOperatorPropertyLocation, errorList) ))
				{
				baseLanguage.MemberOperator = overridingLanguage.MemberOperator;
				}

			// Skip prototype enders in stage 1

			if (overridingLanguage.HasLineExtender &&
				CheckForBasicLanguageSupport(baseLanguage, "LineExtender", 
															   overridingLanguage.LineExtenderPropertyLocation, errorList))
				{
				baseLanguage.LineExtender = overridingLanguage.LineExtender;
				}

			if (overridingLanguage.HasEnumValues &&
				( baseLanguage.Type == Language.LanguageType.TextFile ||
				  CheckForBasicLanguageSupport(baseLanguage, "Enum Values", 
																 overridingLanguage.EnumValuesPropertyLocation, errorList) ))
				{
				baseLanguage.EnumValue = (Language.EnumValues)overridingLanguage.EnumValues;
				}

			if (overridingLanguage.HasCaseSensitive &&
				( baseLanguage.Type == Language.LanguageType.TextFile ||
				  CheckForBasicLanguageSupport(baseLanguage, "Case Sensitive", 
																 overridingLanguage.CaseSensitivePropertyLocation, errorList) ))
				{
				baseLanguage.CaseSensitive = (bool)overridingLanguage.CaseSensitive;
				}

			return (errorList.Count == originalErrorCount);
			}


		/* Function: CheckForBasicLanguageSupport
		 * States that the passed property only applies to languages with basic support.  If the language is set to
		 * <Language.LanguageType.BasicSupport> then this returns true.  If not, it returns false and adds an error to the error list.
		 */
		protected bool CheckForBasicLanguageSupport (Language language, string propertyName, PropertyLocation propertyLocation, 
																			  Errors.ErrorList errorList)
			{
			if (language.Type == Language.LanguageType.BasicSupport)
				{  
				return true;  
				}
			else if (language.Type == Language.LanguageType.FullSupport)
				{
				errorList.Add(Locale.Get("NaturalDocs.Engine", "Languages.txt.CantDefinePropertyForFullLanguageSupport(property, language)", propertyName, language.Name),
									propertyLocation, propertyName);
				return false;
				}
			else if (language.Type == Language.LanguageType.TextFile)
				{
				errorList.Add(Locale.Get("NaturalDocs.Engine", "Languages.txt.CantDefinePropertyForTextFiles(property, language)", propertyName, language.Name),
									propertyLocation, propertyName);
				return false;
				}
			else if (language.Type == Language.LanguageType.Container)
				{
				errorList.Add(Locale.Get("NaturalDocs.Engine", "Languages.txt.CantDefinePropertyForContainers(property, language)", propertyName, language.Name),
									propertyLocation, propertyName);
				return false;
				}
			else
				{  throw new NotImplementedException();  }
			}


		/* Function: GenerateJavadocCommentSymbols
		 * If they're not already defined, generate <Language.JavadocLineCommentSymbols> and 
		 * <Language.JavadocBlockCommentSymbols> from <Language.LineCommentSymbols> and 
		 * <Language.BlockCommentSymbols>.
		 */
		protected void GenerateJavadocCommentSymbols (Language language)
			{
			if (!language.HasJavadocBlockCommentSymbols && language.HasBlockCommentSymbols)
				{
				List<BlockCommentSymbols> javadocBlockCommentSymbols = null;

				foreach (var blockCommentSymbols in language.BlockCommentSymbols)
					{
					// We only accept strings like /* */ and (* *).  Anything else doesn't get it.
					if (blockCommentSymbols.OpeningSymbol.Length == 2 && 
						blockCommentSymbols.ClosingSymbol.Length == 2 &&
						blockCommentSymbols.OpeningSymbol[1] == '*' &&
						blockCommentSymbols.ClosingSymbol[0] == '*')
						{  
						if (javadocBlockCommentSymbols == null)
							{  javadocBlockCommentSymbols = new List<BlockCommentSymbols>();  }

						// Just add another asterisk to make /** */ and (** *)
						javadocBlockCommentSymbols.Add (
							new BlockCommentSymbols(
								blockCommentSymbols.OpeningSymbol + "*",
								blockCommentSymbols.ClosingSymbol
								)
							);
						}
					}

				if (javadocBlockCommentSymbols != null)
					{  language.JavadocBlockCommentSymbols = javadocBlockCommentSymbols;  }
				}

			if (!language.HasJavadocLineCommentSymbols && language.HasLineCommentSymbols)
				{
				List<LineCommentSymbols> javadocLineCommentSymbols = 
					new List<LineCommentSymbols>(language.LineCommentSymbols.Count);

				foreach (var lineCommentSymbol in language.LineCommentSymbols)
					{
					// Just duplicate the last character to make // into /// //.
					javadocLineCommentSymbols.Add(
						new LineCommentSymbols(
							lineCommentSymbol + lineCommentSymbol[lineCommentSymbol.Length - 1],
							lineCommentSymbol
							)
						);
					}

				language.JavadocLineCommentSymbols = javadocLineCommentSymbols;
				}
			}


		/* Function: GenerateXMLCommentSymbols
		 * If they're not already defined, generate <Language.XMLLineCommentSymbols> from <Language.LineCommentSymbols>.
		 */
		protected void GenerateXMLCommentSymbols (Language language)
			{
			if (!language.HasXMLLineCommentSymbols && language.HasLineCommentSymbols)
				{
				List<string> xmlLineCommentSymbols = new List<string>(language.LineCommentSymbols.Count);

				foreach (var lineCommentSymbol in language.LineCommentSymbols)
					{
					// If it's only one character, turn it to three like ''' in Visual Basic.
					if (lineCommentSymbol.Length == 1)
						{  xmlLineCommentSymbols.Add( lineCommentSymbol + lineCommentSymbol + lineCommentSymbol );  }

					// Otherwise just duplicate the last charater like /// in C#.
					else
						{  xmlLineCommentSymbols.Add( lineCommentSymbol + lineCommentSymbol[lineCommentSymbol.Length - 1] );  }
					}

				language.XMLLineCommentSymbols = xmlLineCommentSymbols;
				}
			}


		/* Function: MergeLanguageIdentifiersInto
		 * Merges the file extensions, aliases, and shebang strings from the <ConfigFiles.TextFiles> into the <Config>.  It assumes all
		 * <ConfigFiles.TextFileLanguages> in textConfig have corresponding <Language> in outputConfig.
		 */
		protected void MergeLanguageIdentifiersInto (ref Config outputConfig, ConfigFiles.TextFile systemTextConfig,
																		  ConfigFiles.TextFile projectTextConfig)
			{
			// First collect our ignored extensions

			StringSet ignoredFileExtensions = new StringSet(Config.KeySettingsForFileExtensions);

			if (systemTextConfig.HasIgnoredFileExtensions)
				{
				foreach (var ignoredFileExtension in systemTextConfig.IgnoredFileExtensions)
					{  ignoredFileExtensions.Add(ignoredFileExtension);  }
				}
			if (projectTextConfig.HasIgnoredFileExtensions)
				{
				foreach (var ignoredFileExtension in projectTextConfig.IgnoredFileExtensions)
					{  ignoredFileExtensions.Add(ignoredFileExtension);  }
				}


			// Now turn our language lists into one big combined one, but not in a way that merges any of its entries like
			// mergedTextConfig did.  Just put them all one after the other.

			int languageEntryCount = (systemTextConfig.HasLanguages ? systemTextConfig.Languages.Count : 0) +
												  (projectTextConfig.HasLanguages ? projectTextConfig.Languages.Count : 0);

			List<ConfigFiles.TextFileLanguage> languages = new List<ConfigFiles.TextFileLanguage>(languageEntryCount);

			if (systemTextConfig.HasLanguages)
				{  languages.AddRange(systemTextConfig.Languages);  }
			if (projectTextConfig.HasLanguages)
				{  languages.AddRange(projectTextConfig.Languages);  }


			// Now apply file extensions, aliases, and shebang strings.  We do it from this list instead of mergedTextConfig so
			// so everything happens in the proper order.  For example:
			//
			// Language: LanguageA
			//    Extensions: langA
			//
			// Language: LanguageB
			//    Extensions: langB
			//
			// Alter Language: LanguageA
			//    Replace Extensions: langB
			//
			// In this case langB should actually map to LanguageA.  Not only that, langA should not be applied at all because
			// we used Replace instead of Add.

			for (int i = 0; i < languageEntryCount; i++)
				{
				// We don't need to check whether they're defined for the first time, added, or replaced here.  In all cases we would
				// apply them unless there's a future entry that says Replace.
				bool applyFileExtensions = languages[i].HasFileExtensions;
				bool applyAliases = languages[i].HasAliases;
				bool applyShebangStrings = languages[i].HasShebangStrings;

				// Check for future Replace entries.
				string normalizedLanguageName = languages[i].Name.NormalizeKey(Config.KeySettingsForLanguageName);

				for (int j = i + 1; j < languageEntryCount; j++)
					{
					if (!applyFileExtensions && !applyAliases && !applyShebangStrings)
						{  break;  }

					if (languages[j].Name.NormalizeKey(Config.KeySettingsForLanguageName) == normalizedLanguageName)
						{
						if (languages[j].HasFileExtensions && 
							languages[j].FileExtensionsPropertyChange == ConfigFiles.TextFileLanguage.PropertyChange.Replace)
							{  applyFileExtensions = false;  }

						if (languages[j].HasAliases && 
							languages[j].AliasesPropertyChange == ConfigFiles.TextFileLanguage.PropertyChange.Replace)
							{  applyAliases = false;  }

						if (languages[j].HasShebangStrings && 
							languages[j].ShebangStringsPropertyChange == ConfigFiles.TextFileLanguage.PropertyChange.Replace)
							{  applyShebangStrings = false;  }
						}
					}

				// Apply what's left.
				int languageID = outputConfig.LanguageFromName(languages[i].Name).ID;

				#if DEBUG
				if (languageID == 0)
					{  throw new InvalidOperationException();  }
				#endif

				if (applyFileExtensions)
					{
					foreach (var fileExtension in languages[i].FileExtensions)
						{
						if (!ignoredFileExtensions.Contains(fileExtension))
							{  outputConfig.AddFileExtension(fileExtension, languageID);  }
						}
					}

				if (applyAliases)
					{
					foreach (var alias in languages[i].Aliases)
						{  outputConfig.AddAlias(alias, languageID);  }
					}

				if (applyShebangStrings)
					{
					foreach (var shebangString in languages[i].ShebangStrings)
						{  outputConfig.AddShebangString(shebangString, languageID);  }
					}
				}
			}


		/* Function: MergePrototypeEndersInto_Stage2
		 * Merges the prototype enders found in the <ConfigFiles.TextFile> into the <Languages> of the <Config>, returning whether
		 * it was successful.  If not it will add any errors into the error list.  This must be done in <Start_Stage2()> because we need
		 * <CommentTypes.Manager> to have loaded all the comment type names.
		 */
		protected bool MergePrototypeEndersInto_Stage2 (ref Config config, ConfigFiles.TextFile textFileConfig, 
																				 Errors.ErrorList errorList)
			{
			bool success = true;

			if (textFileConfig.HasLanguages)
				{
				foreach (var textFileLanguage in textFileConfig.Languages)
					{
					if (textFileLanguage.HasPrototypeEnders)
						{
						var matchingLanguage = config.LanguageFromName(textFileLanguage.Name);

						#if DEBUG
						if (matchingLanguage == null)
							{  throw new InvalidOperationException();  }
						#endif

						foreach (var textFilePrototypeEnders in textFileLanguage.PrototypeEnders)
							{
							var matchingCommentType = 
								EngineInstance.CommentTypes.FromName(textFilePrototypeEnders.CommentType);

							if (matchingCommentType == null)
								{
								errorList.Add(Locale.Get("NaturalDocs.Engine", "Languages.txt.PrototypeEnderCommentTypeDoesntExist(name)", textFilePrototypeEnders.CommentType),
													textFilePrototypeEnders.PropertyLocation);
								success = false;
								}
							else
								{
								matchingLanguage.AddPrototypeEnders(
									new PrototypeEnders(matchingCommentType.ID, textFilePrototypeEnders.EnderStrings)
									);
								}
							}
						}
					}
				}

			return success;
			}
		

		/* Function: TouchUp_Stage2
		 * Applies some minor improvements to the <ConfigFiles.TextFile>, such as making sure the capitalization of Alter Language
		 * and [Comment Type] Prototype Enders match the original definition.  Assumes everything is valid, meaning all Alter 
		 * Language entries have corresponding entries in finalConfig and all [Comment Type] Prototype Enders entries have 
		 * corresponding languages in <CommentTypes.Manager>.
		 */
		 protected void TouchUp_Stage2 (ref ConfigFiles.TextFile textConfig, Config finalConfig)
			{
			if (textConfig.HasLanguages)
				{
				foreach (var textFileLanguage in textConfig.Languages)
					{

					// Fix "Alter Language: [name]" capitalization

					if (textFileLanguage.AlterLanguage)
						{
						var originalLanguage = finalConfig.LanguageFromName(textFileLanguage.Name);
						textFileLanguage.FixNameCapitalization(originalLanguage.Name);

						// We don't also check to see if the language we're altering exists in the same file and merge their definitions
						// into one.  Why?  Consider this:
						//
						// Language: Language A
						//    Extensions: langA
						//
						// Language: Language B
						//    Extensions: langB
						//
						// Alter Language: Language A
						//    Add Extensions: langB
						//
						// File extensions B should be part of Language A.  However, if we merged the definitions it would appear first
						// and be overridden by Language B.  So we just leave the two language entries for A instead.
						}


					if (textFileLanguage.HasPrototypeEnders)
						{
						foreach (var textFilePrototypeEnders in textFileLanguage.PrototypeEnders)
							{

							// Fix "[Comment Type] Prototype Enders" capitalization

							var originalCommentType = EngineInstance.CommentTypes.FromName(textFilePrototypeEnders.CommentType);
							textFilePrototypeEnders.CommentType = originalCommentType.Name;
							}
						}
					}
				}
			}

		}
	}