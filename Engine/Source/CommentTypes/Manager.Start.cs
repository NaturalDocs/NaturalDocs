/* 
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.Manager
 * ____________________________________________________________________________
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.CommentTypes
	{
	public partial class Manager : Module
		{
		
		// Group: Initialization Functions
		// __________________________________________________________________________
		
		
		/* Function: Start_Stage1
		 * 
		 * Loads and combines the two versions of <Comments.txt>, returning whether it was successful.  If there were any 
		 * errors they will be added to errorList.
		 * 
		 * Only the settings which don't depend on <Languages.txt> will be loaded.  Call <Start_Stage2()> after
		 * <Languages.Manager.Start_Stage1()> has been called to complete the process.
		 * 
		 * Dependencies:
		 * 
		 *		- <Config.Manager> must be started before this class can start.
		 */
		public bool Start_Stage1 (Errors.ErrorList errorList)
			{
			StartupIssues newStartupIssues = StartupIssues.None;
			bool success = true;
			

			//
			// Comments.txt
			//

			Path systemTextConfigPath = EngineInstance.Config.SystemConfigFolder + "/Comments.txt";
			Path projectTextConfigPath = EngineInstance.Config.ProjectConfigFolder + "/Comments.txt";
			Path oldTopicsFilePath = EngineInstance.Config.ProjectConfigFolder + "/Topics.txt";

			ConfigFiles.TextFileParser textConfigFileParser = new ConfigFiles.TextFileParser();

			// Load the system Comments.txt.
			if (!textConfigFileParser.Load(systemTextConfigPath, PropertySource.SystemCommentsFile, errorList, out systemTextConfig))
				{  success = false;  }
			
			// Load the project Comments.txt.  We want to do this even if the system Comments.txt failed so we get the error messages
			// from both.
			if (System.IO.File.Exists(projectTextConfigPath))
				{
				if (!textConfigFileParser.Load(projectTextConfigPath, PropertySource.ProjectCommentsFile, errorList, out projectTextConfig))
					{  success = false;  }
				}
			// If the project Comments.txt doesn't exist, try loading Topics.txt, which is what the file was called prior to 2.0.
			else if (System.IO.File.Exists(oldTopicsFilePath))
				{
				if (!textConfigFileParser.Load(oldTopicsFilePath, PropertySource.ProjectCommentsFile, errorList, out projectTextConfig))
					{  success = false;  }
				}
			// If neither file exists just create a blank config.  The project Comments.txt not existing is not an error.
			else
				{  projectTextConfig = new ConfigFiles.TextFile();  }
				
			if (!success)
				{  return false;  }
				
			if (!ValidateCommentTypes(systemTextConfig, errorList))
				{  success = false;  }
			if (!ValidateCommentTypes(projectTextConfig, errorList))
				{  success = false;  }

			if (!success)
				{  return false;  }

			// Merge them into one combined config.  Note that this doesn't do keywords, ignored keywords, or tags.  Only the comment
			// types and their non-keyword properties will exist in the merged config.
			if (!MergeCommentTypes(systemTextConfig, projectTextConfig, out mergedTextConfig, errorList))
				{  return false;  }
			if (!ValidateCommentTypes(mergedTextConfig, errorList))
				{  return false;  }


			//
			// Comments.nd
			//

			Path lastRunConfigPath = EngineInstance.Config.WorkingDataFolder + "/Comments.nd";
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
											   StartupIssues.CommentIDsInvalidated;
				}


			//
			// Create the final config
			//

			config = new Config();


			// We go through the comment types present in the merged text config files and convert them into the final config.  We 
			// need the contents of Comments.nd so we can keep the comment type IDs consistent from one run to the next if possible.

			IDObjects.NumberSet usedCommentTypeIDs = new IDObjects.NumberSet();
			IDObjects.NumberSet lastRunUsedCommentTypeIDs = lastRunConfig.UsedCommentTypeIDs();

			if (mergedTextConfig.HasCommentTypes)
				{
				foreach (var textCommentType in mergedTextConfig.CommentTypes)
					{
					var finalCommentType = FinalizeCommentType(textCommentType);

					// We still need to set the ID.  See if a comment type of the same name existed in the previous run.
					var lastRunCommentType = lastRunConfig.CommentTypeFromName(textCommentType.Name);

					// If there wasn't one we can assign a new ID, but pick one that isn't used in this run or the last run so there's no 
					// conflicts.
					if (lastRunCommentType == null)
						{
						int id = lastRunUsedCommentTypeIDs.LowestAvailable;
						
						if (usedCommentTypeIDs.Contains(id))
							{  id = Math.Max(usedCommentTypeIDs.Highest + 1, lastRunUsedCommentTypeIDs.Highest + 1);  }

						finalCommentType.ID = id;
						config.AddCommentType(finalCommentType);

						usedCommentTypeIDs.Add(finalCommentType.ID);
						lastRunUsedCommentTypeIDs.Add(finalCommentType.ID);
						}
					// If the type did exist but we haven't used its ID yet, we can keep it.
					else if (!usedCommentTypeIDs.Contains(lastRunCommentType.ID))
						{
						finalCommentType.ID = lastRunCommentType.ID;
						config.AddCommentType(finalCommentType);

						usedCommentTypeIDs.Add(finalCommentType.ID);
						}
					// However, if the type did exist and we assigned that ID already, then we have a conflict and have to tell the engine
					// that the IDs from the last run were invalidated.  We can just assign anything unused at this point since it no longer
					// matters.
					else
						{
						finalCommentType.ID = usedCommentTypeIDs.LowestAvailable;
						config.AddCommentType(finalCommentType);

						usedCommentTypeIDs.Add(finalCommentType.ID);
						newStartupIssues |= StartupIssues.CommentIDsInvalidated;
						}

					// Now that we have a final ID, set the simple identifier for any type that still needs it.  Some types may have it null if
					// it wasn't manually defined and the name didn't contain A-Z characters.

					if (finalCommentType.SimpleIdentifier == null)
						{  finalCommentType.SimpleIdentifier = "CommentTypeID" + finalCommentType.ID;  }
					}
				}


			// Now do the same thing for tags.

			List<string> mergedTagList = null;
			int mergedTagCount = (systemTextConfig.HasTags ? systemTextConfig.Tags.Count : 0) +
											 (projectTextConfig.HasTags ? projectTextConfig.Tags.Count : 0);

			if (mergedTagCount > 0)
				{
				mergedTagList = new List<string>(mergedTagCount);

				if (systemTextConfig.HasTags)
					{  mergedTagList.AddRange(systemTextConfig.Tags);  }
				if (projectTextConfig.HasTags)
					{  mergedTagList.AddRange(projectTextConfig.Tags);  }
				}

			IDObjects.NumberSet usedTagIDs = new IDObjects.NumberSet();
			IDObjects.NumberSet lastRunUsedTagIDs = lastRunConfig.UsedTagIDs();

			if (mergedTagList != null)
				{
				foreach (var tagString in mergedTagList)
					{
					// Just skip it if it already exists
					if (config.TagFromName(tagString) != null)
						{  continue;  }

					var tag = new Tag(tagString);

					// We still need to set the ID.  See if a tag of the same name existed in the previous run.
					var lastRunTag = lastRunConfig.TagFromName(tagString);

					// If there wasn't one we can assign a new ID, but pick one that isn't used in this run or the last run so there's no 
					// conflicts.
					if (lastRunTag == null)
						{
						int id = lastRunUsedTagIDs.LowestAvailable;
						
						if (usedTagIDs.Contains(id))
							{  id = Math.Max(usedTagIDs.Highest + 1, lastRunUsedTagIDs.Highest + 1);  }

						tag.ID = id;
						config.AddTag(tag);

						usedTagIDs.Add(tag.ID);
						lastRunUsedTagIDs.Add(tag.ID);
						}
					// If the tag did exist but we haven't used its ID yet, we can keep it.
					else if (!usedTagIDs.Contains(lastRunTag.ID))
						{
						tag.ID = lastRunTag.ID;
						config.AddTag(tag);

						usedTagIDs.Add(tag.ID);
						}
					// However, if the tag did exist and we assigned that ID already, then we have a conflict and have to tell the engine
					// that the IDs from the last run were invalidated.  We can just assign anything unused at this point since it no longer
					// matters.
					else
						{
						tag.ID = usedTagIDs.LowestAvailable;
						config.AddTag(tag);

						usedTagIDs.Add(tag.ID);
						newStartupIssues |= StartupIssues.CommentIDsInvalidated;
						}
					}
				}


			// That's it for stage one.  Everything else is in stage 2.

			if (newStartupIssues != StartupIssues.None)
				{  EngineInstance.AddStartupIssues(newStartupIssues);  }
				
			return true;
			}


		/* Function: Start_Stage2
		 * 
		 * Finishes loading and and combing the two versions of <Comments.txt>, returning whether it was successful.  If there
		 * were any errors they will be added to errorList.
		 * 
		 * This must be called after <Start_Stage1()> has been called, and also <Languages.Manager.Start_Stage1()>.  This 
		 * finalizes any settings which also depend on <Languages.txt>.
		 * 
		 * Dependencies:
		 * 
		 *		- <Config.Manager> must be started before this class can start.
		 *		- <Start_Stage1()> must be called and return true before this function can be called.
		 *		- <Languages.Manager.Start_Stage1()> must be called and return true before this function can be called.
		 */
		public bool Start_Stage2 (Errors.ErrorList errorList)
			{
		
			// First we collect all the ignored keywords.

			StringSet ignoredKeywords = new StringSet(Config.KeySettingsForKeywords);

			// Remember that mergedTextConfig only has comment types, not keywords, ignored keywords, or tags.  So we have to
			// go back to the system and project configs.
			MergeIgnoredKeywordsInto(ref ignoredKeywords, systemTextConfig);
			MergeIgnoredKeywordsInto(ref ignoredKeywords, projectTextConfig);


			// Now add all keywords that aren't ignored, validating the language names along the way.

			if (!MergeKeywordsInto_Stage2(ref config, systemTextConfig, ignoredKeywords, errorList) ||
				!MergeKeywordsInto_Stage2(ref config, projectTextConfig, ignoredKeywords, errorList))
				{  return false;  }

				
			// Now we have our final configuration and everything is okay.  Save the text files again to reformat them.
			
			TouchUp_Stage2(ref systemTextConfig, config);
			TouchUp_Stage2(ref projectTextConfig, config);

			Path systemTextConfigPath = EngineInstance.Config.SystemConfigFolder + "/Comments.txt";
			Path projectTextConfigPath = EngineInstance.Config.ProjectConfigFolder + "/Comments.txt";

			ConfigFiles.TextFileParser textConfigFileParser = new ConfigFiles.TextFileParser();

			// If the project Comments.txt didn't exist, saving the blank structure that was created will create a default one.
			if (!textConfigFileParser.Save(projectTextConfigPath, PropertySource.ProjectCommentsFile, projectTextConfig, errorList))
				{  return false;  };
				
			// We don't care if we're not able to resave the system Comments.txt since it may be in a protected location.
			textConfigFileParser.Save(systemTextConfigPath, PropertySource.SystemCommentsFile, systemTextConfig, errorList: null);


			// Save Comments.nd as well.			
			
			Path lastRunConfigPath = EngineInstance.Config.WorkingDataFolder + "/Comments.nd";
			ConfigFiles.BinaryFileParser binaryConfigFileParser = new ConfigFiles.BinaryFileParser();

			binaryConfigFileParser.Save(lastRunConfigPath, config);


			// Look up our Group comment type ID since it's used often and we want to cache it.

			groupCommentTypeID = IDFromKeyword("group", 0);


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


		/* Function: ValidateCommentTypes
		 * Validates all the comment type settings in a <ConfigFiles.TextFile>.  Returns whether it is valid, and adds any errors it 
		 * finds to errorList.
		 */
		protected bool ValidateCommentTypes (ConfigFiles.TextFile configFile, Errors.ErrorList errorList)
			{
			bool success = true;

			if (configFile.HasCommentTypes)
				{
				foreach (var commentType in configFile.CommentTypes)
					{
					if (!ValidateCommentType(commentType, errorList))
						{  
						success = false;  
						// Continue anyway so we can report the errors in all of them.
						}
					}
				}

			return success;
			}


		/* Function: ValidateCommentType
		 * Validates all the settings in a <ConfigFiles.TextFileCommentType>.  Returns whether it is valid, and adds any errors it finds
		 * to errorList.
		 */
		protected bool ValidateCommentType (ConfigFiles.TextFileCommentType commentType, Errors.ErrorList errorList)
			{
			int startingErrorCount = errorList.Count;


			// Validate names

			if (commentType.DisplayNamePropertyLocation.IsDefined &&
				commentType.DisplayNameFromLocalePropertyLocation.IsDefined)
				{
				// Put them in the proper order so the error appears on the second one
				string first, second;
				PropertyLocation secondPropertyLocation;

				if (commentType.DisplayNamePropertyLocation.LineNumber < 
					commentType.DisplayNameFromLocalePropertyLocation.LineNumber)
					{
					first = "Display Name";
					second = "Display Name From Locale";
					secondPropertyLocation = commentType.DisplayNameFromLocalePropertyLocation;
					}
				else
					{
					first = "Display Name From Locale";
					second = "Display Name";
					secondPropertyLocation = commentType.DisplayNamePropertyLocation;
					}

				errorList.Add(Locale.Get("NaturalDocs.Engine", "Comments.txt.CannotDefineXWhenYIsDefined(x,y)", second, first),
									secondPropertyLocation);
				}

			if (commentType.PluralDisplayNamePropertyLocation.IsDefined &&
				commentType.PluralDisplayNameFromLocalePropertyLocation.IsDefined)
				{
				string first, second;
				PropertyLocation secondPropertyLocation;

				if (commentType.PluralDisplayNamePropertyLocation.LineNumber < 
					commentType.PluralDisplayNameFromLocalePropertyLocation.LineNumber)
					{
					first = "Plural Display Name";
					second = "Plural Display Name From Locale";
					secondPropertyLocation = commentType.PluralDisplayNameFromLocalePropertyLocation;
					}
				else
					{
					first = "Plural Display Name From Locale";
					second = "Plural Display Name";
					secondPropertyLocation = commentType.PluralDisplayNamePropertyLocation;
					}

				errorList.Add(Locale.Get("NaturalDocs.Engine", "Comments.txt.CannotDefineXWhenYIsDefined(x,y)", second, first),
									secondPropertyLocation);
				}


			// Validate flags

			if (commentType.Flags != null)
				{
				var flags = (CommentType.FlagValue)commentType.Flags;

				bool codeFlag = ((flags & CommentType.FlagValue.Code) != 0);
				bool fileFlag = ((flags & CommentType.FlagValue.File) != 0);
				bool documentationFlag = ((flags & CommentType.FlagValue.Documentation) != 0);
				bool variableTypeFlag = ((flags & CommentType.FlagValue.VariableType) != 0);
				bool classHierarchyFlag = ((flags & CommentType.FlagValue.ClassHierarchy) != 0);
				bool databaseHierarchyFlag = ((flags & CommentType.FlagValue.DatabaseHierarchy) != 0);
				bool enumFlag = ((flags & CommentType.FlagValue.Enum) != 0);


				// Check that only one of Code, File, and Documentation are defined

				if (codeFlag)
					{
					if (fileFlag && documentationFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.CantCombine(a,b,c)", "Code", "File", "Documentation"),
											commentType.FlagsPropertyLocation);
						}
					else if (fileFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.CantCombine(a,b)", "Code", "File"),
											commentType.FlagsPropertyLocation);
						}
					else if (documentationFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.CantCombine(a,b)", "Code", "Documentation"),
											commentType.FlagsPropertyLocation);
						}
					}
				else if (fileFlag)
					{
					if (documentationFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.CantCombine(a,b)", "File", "Documentation"),
											commentType.FlagsPropertyLocation);
						}
					}


				// Check that File and Documentation aren't used with any of the other flags

				if (fileFlag)
					{
					if (variableTypeFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.CantCombine(a,b)", "File", "Variable Type"),
											commentType.FlagsPropertyLocation);
						}
					if (classHierarchyFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.CantCombine(a,b)", "File", "Class Hierarchy"),
											commentType.FlagsPropertyLocation);
						}
					if (databaseHierarchyFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.CantCombine(a,b)", "File", "Database Hierarchy"),
											commentType.FlagsPropertyLocation);
						}
					if (enumFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.CantCombine(a,b)", "File", "Enum"),
											commentType.FlagsPropertyLocation);
						}
					}

				if (documentationFlag)
					{
					if (variableTypeFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.CantCombine(a,b)", "Documentation", "Variable Type"),
											commentType.FlagsPropertyLocation);
						}
					if (classHierarchyFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.CantCombine(a,b)", "Documentation", "Class Hierarchy"),
											commentType.FlagsPropertyLocation);
						}
					if (databaseHierarchyFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.CantCombine(a,b)", "Documentation", "Database Hierarchy"),
											commentType.FlagsPropertyLocation);
						}
					if (enumFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.CantCombine(a,b)", "Documentation", "Enum"),
											commentType.FlagsPropertyLocation);
						}
					}


				// Check that Class Hierarchy and Database Hierarchy aren't both defined

				if (classHierarchyFlag && databaseHierarchyFlag)
					{  
					errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.CantCombine(a,b)", "Class Hierarchy", "Database Hierarchy"),
										commentType.FlagsPropertyLocation);
					}


				// Check that Class Hierarchy and Database Hierarchy comment types also have Scope: Start

				if (commentType.Scope != CommentType.ScopeValue.Start)
					{
					// This is an error if Scope isn't defined because it's too big a behavior change to just make it implied.

					if (classHierarchyFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.FlagRequiresScope(flag,scope)", "Class Hierarchy", "Start"),
											commentType.FlagsPropertyLocation);
						}
					if (databaseHierarchyFlag)
						{  
						errorList.Add(Locale.Get("NaturalDocs.Engine", "CommentTypeFlags.FlagRequiresScope(flag,scope)", "Database Hierarchy", "Start"),
											commentType.FlagsPropertyLocation);
						}
					}

				// Variable Type and Enum are okay.  We already know they aren't being used with File or Documentation, and they're safe 
				// to use with Class Hierarchy and Database Hierarchy.

				}

			return (errorList.Count == startingErrorCount);
			}


		/* Function: MergeCommentTypes
		 * 
		 * Merges two <ConfigFiles.TextFiles> into a new one, putting all the comment types into one list and applying any alter 
		 * entries.  This does NOT cover keywords, ignored keywords, or tags; those will be blank in the result.  Returns the new
		 * list and whether it was successful.
		 * 
		 * Any errors will be added to errorList, such as defining a duplicate entry that doesn't use alter, or an alter entry for a 
		 * non-existent comment type.  All alter entries will be applied, including any appearing in the base config, so there will
		 * only be non-alter entries in the returned list.
		 */
		protected bool MergeCommentTypes (ConfigFiles.TextFile baseConfig, ConfigFiles.TextFile overridingConfig, 
															  out ConfigFiles.TextFile combinedConfig, Errors.ErrorList errorList)
			{
			combinedConfig = new ConfigFiles.TextFile();

			// We merge the base config into the empty config instead of just copying it so any alter entries it has are applied
			if (!MergeCommentTypesInto(ref combinedConfig, baseConfig, errorList) ||
				!MergeCommentTypesInto(ref combinedConfig, overridingConfig, errorList))
				{
				combinedConfig = null;
				return false;
				}

			return true;
			}


		/* Function: MergeCommentTypesInto
		 * Merges the comment types of the second <ConfigFiles.TextFile> into the first, adding new types and applying any 
		 * alter entries.  This does NOT merge keywords, ignored keywords, or tags.  The base config will be changed, even if
		 * there are errors.  Returns false if there were any errors and adds them to errorList.
		 */
		protected bool MergeCommentTypesInto (ref ConfigFiles.TextFile baseConfig, ConfigFiles.TextFile overridingConfig, 
																	Errors.ErrorList errorList)
			{
			bool success = true;

			if (overridingConfig.HasCommentTypes)
				{
				foreach (var overridingCommentType in overridingConfig.CommentTypes)
					{
					var matchingCommentType = baseConfig.FindCommentType(overridingCommentType.Name);

					if (matchingCommentType != null)
						{
						if (overridingCommentType.AlterType == false)
							{
							errorList.Add(Locale.Get("NaturalDocs.Engine", "Comments.txt.CommentTypeAlreadyExists(name)", overridingCommentType.Name),
												overridingCommentType.PropertyLocation);
							success = false;
							}
						else
							{
							MergeCommentTypeInto(ref matchingCommentType, overridingCommentType);
							}
						}

					else // no match
						{
						if (overridingCommentType.AlterType == true)
							{
							errorList.Add(Locale.Get("NaturalDocs.Engine", "Comments.txt.AlteredCommentTypeDoesntExist(name)", overridingCommentType.Name),
												overridingCommentType.NamePropertyLocation);
							success = false;
							}
						else
							{
							baseConfig.AddCommentType( overridingCommentType.Duplicate() );
							}
						}

					}
				}

			return success;
			}


		/* Function: MergeCommentTypeInto
		 * Merges the settings of a <ConfigFiles.TextFileCommentType> into another one, overriding the settings of the first.  This 
		 * does NOT cover keywords.  The base object will be altered.
		 */
		protected void MergeCommentTypeInto (ref ConfigFiles.TextFileCommentType baseCommentType, 
																   ConfigFiles.TextFileCommentType overridingCommentType)
			{
			if (overridingCommentType.HasDisplayName)
				{  
				baseCommentType.SetDisplayName(overridingCommentType.DisplayName, 
																	 overridingCommentType.DisplayNamePropertyLocation);  
				baseCommentType.SetDisplayNameFromLocale(null, default);
				}
			if (overridingCommentType.HasDisplayNameFromLocale)
				{  
				baseCommentType.SetDisplayNameFromLocale(overridingCommentType.DisplayNameFromLocale, 
																					 overridingCommentType.DisplayNameFromLocalePropertyLocation);  
				baseCommentType.SetDisplayName(null, default);
				}
			if (overridingCommentType.HasPluralDisplayName)
				{
				baseCommentType.SetPluralDisplayName(overridingCommentType.PluralDisplayName, 
																			 overridingCommentType.PluralDisplayNamePropertyLocation);
				baseCommentType.SetPluralDisplayNameFromLocale(null, default);
				}
			if (overridingCommentType.HasPluralDisplayNameFromLocale)
				{
				baseCommentType.SetPluralDisplayNameFromLocale(overridingCommentType.PluralDisplayNameFromLocale,
																							 overridingCommentType.PluralDisplayNameFromLocalePropertyLocation);
				baseCommentType.SetPluralDisplayName(null, default);
				}

			if (overridingCommentType.HasSimpleIdentifier)
				{
				baseCommentType.SetSimpleIdentifier(overridingCommentType.SimpleIdentifier, 
																		 overridingCommentType.SimpleIdentifierPropertyLocation);
				}
			if (overridingCommentType.HasScope)
				{
				baseCommentType.SetScope(overridingCommentType.Scope, 
														  overridingCommentType.ScopePropertyLocation);
				}

			// Ignore keywods

			if (overridingCommentType.HasFlags)
				{
				baseCommentType.SetFlags(overridingCommentType.Flags, 
														 overridingCommentType.FlagsPropertyLocation);
				}
			}


		/* Function: AddImpliedFlags
		 * Updates the passed <CommentType.FlagValue> with any that are implied, such as how Class Hierarchy implies Variable 
		 * Type and if none of Code, File, Documentation are defined it defaults to Code.  Assumes the flags are valid.
		 */
		protected CommentType.FlagValue AddImpliedFlags (CommentType.FlagValue flags)
			{
			// Default to Code if neither Code, File, nor Documentation are defined.
			if ( (flags & (CommentType.FlagValue.Code | 
							   CommentType.FlagValue.File | 
							   CommentType.FlagValue.Documentation)) == 0)
				{  flags |= CommentType.FlagValue.Code;  }

			// Add Code if Variable Type, Class Hierarchy, Database Hierarchy, or Enum are defined.
			if ( (flags & (CommentType.FlagValue.VariableType | 
							   CommentType.FlagValue.ClassHierarchy | 
							   CommentType.FlagValue.DatabaseHierarchy | 
							   CommentType.FlagValue.Enum)) != 0)
				{  flags |= CommentType.FlagValue.Code;  }

			// Add Variable Type if Class Hierarchy or Enum are defined.
			if ( (flags & (CommentType.FlagValue.ClassHierarchy | 
							   CommentType.FlagValue.Enum)) != 0)
				{  flags |= CommentType.FlagValue.VariableType;  }

			return flags;
			}


		/* Function: FinalizeCommentType
		 * Converts a <ConfigFiles.TextFileCommentType> into a <CommentType>.  This does NOT cover keywords.  Also,
		 * <CommentType.SimpleIdentifier> may still be null if one wasn't defined and it cannot be generated automatically.
		 */
		protected CommentType FinalizeCommentType (ConfigFiles.TextFileCommentType textCommentType)
			{
			CommentType final = new CommentType(textCommentType.Name);

			if (textCommentType.HasDisplayNameFromLocale)
				{  final.DisplayName = Locale.Get("NaturalDocs.Engine", textCommentType.DisplayNameFromLocale);  }
			else if (textCommentType.HasDisplayName)
				{  final.DisplayName = textCommentType.DisplayName;  }
			else
				{  final.DisplayName = textCommentType.Name;  }

			if (textCommentType.HasPluralDisplayNameFromLocale)
				{  final.PluralDisplayName = Locale.Get("NaturalDocs.Engine", textCommentType.PluralDisplayNameFromLocale);  }
			else if (textCommentType.HasPluralDisplayName)
				{  final.PluralDisplayName = textCommentType.PluralDisplayName;  }
			else
				{  final.PluralDisplayName = final.DisplayName;  }

			if (textCommentType.HasSimpleIdentifier)
				{  final.SimpleIdentifier = textCommentType.SimpleIdentifier;  }
			else
				{  
				// This may end up as an empty string if there's no A-Z characters, such as if the name is in Japanese.  In this case
				// we want it to be "CommentTypeID[number]" but the number isn't determind yet, so leave it as null for now.
				string simpleIdentifier = final.Name.OnlyAToZ();

				if (!string.IsNullOrEmpty(simpleIdentifier))
					{  final.SimpleIdentifier = simpleIdentifier;  }
				}

			if (textCommentType.HasScope)
				{  final.Scope = (CommentType.ScopeValue)textCommentType.Scope;  }
			else
				{  final.Scope = CommentType.ScopeValue.Normal;  }

			if (textCommentType.HasFlags)
				{  final.Flags = AddImpliedFlags( (CommentType.FlagValue)textCommentType.Flags );  }
			else
				{  final.Flags = CommentType.FlagValue.Code;  }

			return final;
			}


		/* Function: MergeIgnoredKeywordsInto
		 * Merges the ignored keywords from the <ConfigFiles.TextFile> into a <StringSet>.
		 */
		protected void MergeIgnoredKeywordsInto (ref StringSet ignoredKeywords, ConfigFiles.TextFile textConfig)
			{
			if (textConfig.HasIgnoredKeywords)
				{
				foreach (var ignoredKeywordGroup in textConfig.IgnoredKeywordGroups)
					{
					foreach (var ignoredKeywordDefinition in ignoredKeywordGroup.KeywordDefinitions)
						{
						ignoredKeywords.Add(ignoredKeywordDefinition.Keyword);

						if (ignoredKeywordDefinition.HasPlural)
							{  ignoredKeywords.Add(ignoredKeywordDefinition.Plural);  }
						}
					}
				}
			}


		/* Function: MergeKeywordsInto_Stage2
		 * Merges the keywords from the <ConfigFiles.TextFile> into the <Config>, returning whether it was successful.  It 
		 * assumes all <ConfigFiles.TextCommentTypes> in textConfig have corresponding <CommentTypes> in outputConfig.  
		 * Any errors will be added to errorList, such as having a language-specific keyword that doesn't match a name in
		 * <Languages.Manager>.
		 */
		protected bool MergeKeywordsInto_Stage2 (ref Config outputConfig, ConfigFiles.TextFile textConfig, 
																		StringSet ignoredKeywords, Errors.ErrorList errorList)
			{
			bool success = true;

			if (textConfig.HasCommentTypes)
				{
				foreach (var commentType in textConfig.CommentTypes)
					{
					int commentTypeID = outputConfig.CommentTypeFromName(commentType.Name).ID;

					#if DEBUG
					if (commentTypeID == 0)
						{  throw new InvalidOperationException();  }
					#endif

					if (commentType.HasKeywordGroups)
						{
						foreach (var keywordGroup in commentType.KeywordGroups)
							{
							int languageID = 0;

							if (keywordGroup.IsLanguageSpecific)
								{
								var language = EngineInstance.Languages.FromName(keywordGroup.LanguageName);

								if (language == null)
									{
									errorList.Add( 
										Locale.Get("NaturalDocs.Engine", "Comments.txt.UnrecognizedKeywordLanguage(name)", keywordGroup.LanguageName),
										keywordGroup.PropertyLocation										
										);

									success = false;
									}
								else
									{  languageID = language.ID;  }
								}

							foreach (var keywordDefinition in keywordGroup.KeywordDefinitions)
								{
								if (!ignoredKeywords.Contains(keywordDefinition.Keyword))
									{
									var outputKeywordDefinition = new KeywordDefinition(keywordDefinition.Keyword);
									outputKeywordDefinition.CommentTypeID = commentTypeID;

									if (languageID != 0)
										{  outputKeywordDefinition.LanguageID = languageID;  }

									// AddKeywordDefinition will handle overwriting definitions with the same keyword and language
									outputConfig.AddKeywordDefinition(outputKeywordDefinition);
									}

								if (keywordDefinition.HasPlural && !ignoredKeywords.Contains(keywordDefinition.Plural))
									{
									var outputKeywordDefinition = new KeywordDefinition(keywordDefinition.Plural);
									outputKeywordDefinition.CommentTypeID = commentTypeID;
									outputKeywordDefinition.Plural = true;

									if (languageID != 0)
										{  outputKeywordDefinition.LanguageID = languageID;  }

									outputConfig.AddKeywordDefinition(outputKeywordDefinition);
									}
								}
							}
						}
					}
				}

			return success;
			}


		/* Function: TouchUp_Stage2
		 * Applies some minor improvements to the <ConfigFiles.TextFile>, such as making sure the capitalization of Alter Topic Type
		 * and [Language] Keywords match the original definition.  Assumes everything is valid, meaning all Alter Topic Type entries
		 * have corresponding entries in finalConfig and all [Language] Keyword entries have corresponding languages in
		 * <Languages.Manager>.
		 */
		 protected void TouchUp_Stage2 (ref ConfigFiles.TextFile textConfig, Config finalConfig)
			{
			if (textConfig.HasCommentTypes)
				{
				foreach (var commentType in textConfig.CommentTypes)
					{

					// Fix "Alter Comment Type: [name]" capitalization

					if (commentType.AlterType)
						{
						var originalType = finalConfig.CommentTypeFromName(commentType.Name);
						commentType.FixNameCapitalization(originalType.Name);

						// We don't also check to see if the comment type we're altering exists in the same file and merge their 
						// definitions into one.  Why?  Consider this:
						//
						// Comment Type: Comment Type A
						//    Keyword: Keyword A
						//
						// Comment Type: Comment Type B
						//    Keyword: Keyword B
						//
						// Alter Comment Type: Comment Type A
						//    Keyword: Keyword B
						//
						// Keyword B should be part of Comment Type A.  However, if we merged the definitions it would appear
						// first and be overridden by Comment Type B.  So we just leave the two comment type entries for A instead.
						}


					if (commentType.HasKeywordGroups)
						{
						foreach (var keywordGroup in commentType.KeywordGroups)
							{

							// Fix "[Language] Keywords" capitalization

							if (keywordGroup.IsLanguageSpecific)
								{
								var originalLanguage = EngineInstance.Languages.FromName(keywordGroup.LanguageName);
								keywordGroup.LanguageName = originalLanguage.Name;
								}
							}
						}
					}
				}
			}

		}
	}