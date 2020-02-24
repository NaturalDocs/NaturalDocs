/* 
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.Manager
 * ____________________________________________________________________________
 * 
 * A module to handle <Comments.txt> and all the comment type settings within Natural Docs.
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


namespace CodeClear.NaturalDocs.Engine.CommentTypes
	{
	public class Manager : Module
		{
		
		// Group: Constants
		// __________________________________________________________________________
		
		
		public const KeySettings KeySettingsForKeywords = KeySettings.IgnoreCase | KeySettings.NormalizeUnicode;
		public const KeySettings KeySettingsForCommentTypes = KeySettings.IgnoreCase | KeySettings.NormalizeUnicode;
		public const KeySettings KeySettingsForTags = KeySettings.IgnoreCase | KeySettings.NormalizeUnicode;
		
		
		
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Manager
		 */
		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			// Comment type names aren't normalized because they're only referenced in other config files.  Tags and keywords are
			// referenced in source files so they should be more tolerant.
			commentTypes = new IDObjects.Manager<CommentType>(KeySettingsForCommentTypes, false);
			tags = new IDObjects.Manager<Tag>(KeySettingsForTags, false);
			
			singularKeywords = new StringTable<CommentType>(KeySettingsForKeywords);
			pluralKeywords = new StringTable<CommentType>(KeySettingsForKeywords);

			groupCommentTypeID = 0;
			}


		protected override void Dispose (bool strictRulesApply)
			{
			}


		/* Function: Start
		 * 
		 * Loads and combines the two versions of <Comments.txt>, returning whether it was successful.  If there were any errors
		 * they will be added to errorList.
		 * 
		 * Dependencies:
		 * 
		 *		- <Config.Manager> must be started before this class can start.
		 */
		public bool Start (Errors.ErrorList errorList)
			{
			List<ConfigFileCommentType> systemCommentTypeList;
			List<ConfigFileCommentType> projectCommentTypeList;
			List<string> ignoredSystemKeywords;
			List<string> ignoredProjectKeywords;
			List<string> systemTags;
			List<string> projectTags;
			
			List<CommentType> binaryCommentTypes;
			List<Tag> binaryTags;
			List<KeyValuePair<string, int>> binarySingularKeywords;
			List<KeyValuePair<string, int>> binaryPluralKeywords;
			List<string> binaryIgnoredKeywords;
			
			// The return value, which is whether we were able to successfully load and parse the system Comments.txt, and if it exists,
			// the project Comments.txt.  The project Comments.txt not existing is not a failure.
			bool success = true;
			
			// Whether anything has changed since the last run, as determined by Comments.nd.  If Comments.nd doesn't exist or is corrupt,
			// we have to assume something changed.
			bool changed = false;

			Comments_nd commentsNDParser = new Comments_nd();


			// We need the ID numbers to stay consistent between runs, so we need to create all the comment types and tags from the
			// binary file first.  We'll worry about comparing their attributes and seeing if any were added or deleted later.

			if (EngineInstance.Config.ReparseEverything == true)
				{
				binaryCommentTypes = new List<CommentType>();
				binaryTags = new List<Tag>();
				binarySingularKeywords = new List<KeyValuePair<string,int>>();
				binaryPluralKeywords = new List<KeyValuePair<string,int>>();
				binaryIgnoredKeywords = new List<string>();
				
				changed = true;
				}
				
			else if (commentsNDParser.Load(EngineInstance.Config.WorkingDataFolder + "/Comments.nd", out binaryCommentTypes, out binaryTags, 
													out binarySingularKeywords, out binaryPluralKeywords, out binaryIgnoredKeywords) == false)
				{
				changed = true;
				// Even though it failed, LoadBinaryFile will still create valid empty objects for the variables.
				}
				
			else // Load binary file succeeded
				{
				// We use a try block so if anything screwy happens, like two things having the same ID number and thus causing 
				// an exception when added, we can continue as if the binary file didn't parse at all.
				try
					{
					foreach (CommentType binaryCommentType in binaryCommentTypes)
						{
						// We don't add the binary comment type itself because we only want those for comparison purposes.  We want 
						// the types in commentTypes to be at their default values because the Comments.txt versions will only set some attributes, 
						// not all, and we don't want the unset attributes influenced by the binary versions.
						CommentType newCommentType = new CommentType(binaryCommentType.Name);
						newCommentType.ID = binaryCommentType.ID;
						newCommentType.Flags.InBinaryFile = true;
						
						commentTypes.Add(newCommentType);
						}
					
					foreach (Tag binaryTag in binaryTags)
						{
						Tag newTag = new Tag(binaryTag.Name);
						newTag.ID = binaryTag.ID;
						newTag.InBinaryFile = true;
						
						tags.Add(newTag);
						}	
					}
				catch
					{
					commentTypes.Clear();
					tags.Clear();
					changed = true;
					
					// Clear them since they may be used later in this function.
					binaryCommentTypes.Clear();
					binarySingularKeywords.Clear();
					binaryPluralKeywords.Clear();
					binaryIgnoredKeywords.Clear();
					
					// Otherwise ignore the exception and continue.
					}
				}

			
			Path systemFile = EngineInstance.Config.SystemConfigFolder + "/Comments.txt";
			Path projectFile = EngineInstance.Config.ProjectConfigFolder + "/Comments.txt";
			Path oldProjectFile = EngineInstance.Config.ProjectConfigFolder + "/Topics.txt";

			Comments_txt commentsTxtParser = new Comments_txt();

			
			// Load the files.
			
			if (!commentsTxtParser.Load( systemFile, out systemCommentTypeList, out ignoredSystemKeywords, out systemTags, errorList ))
				{  
				success = false;  
				// Continue anyway because we want to show errors from both files.
				}
			
			if (System.IO.File.Exists(projectFile))
				{
				if (!commentsTxtParser.Load( projectFile, out projectCommentTypeList, out ignoredProjectKeywords, out projectTags, errorList ))
					{  success = false;  }
				}
			else if (System.IO.File.Exists(oldProjectFile))
				{
				if (!commentsTxtParser.Load( oldProjectFile, out projectCommentTypeList, out ignoredProjectKeywords, out projectTags, errorList ))
					{  success = false;  }
				}
			else
				{
				// The project file not existing is not an error condition.  Fill in the variables with empty structures.
				projectCommentTypeList = new List<ConfigFileCommentType>();
				ignoredProjectKeywords = new List<string>();
				projectTags = new List<string>();
				}
				
			if (success == false)
				{  return false;  }
				
				
			// Combine the ignored keywords.
			
			StringSet ignoredKeywords = new StringSet(KeySettingsForKeywords);
			
			foreach (string keyword in ignoredSystemKeywords)
				{
				if (keyword != null)
					{  ignoredKeywords.Add(keyword);  }
				}

			foreach (string keyword in ignoredProjectKeywords)
				{
				if (keyword != null)
					{  ignoredKeywords.Add(keyword);  }
				}
				
				
			// Combine the tags
			
			foreach (string tagName in systemTags)
				{
				Tag tag = tags[tagName];
				
				if (tag == null)
					{
					tag = new Tag(tagName);
					tag.InSystemFile = true;
					tags.Add(tag);
					}
				else
					{					
					tag.InSystemFile = true;
					
					// In case it changed since the binary version.
					tag.FixNameCapitalization(tagName);
					}
				}
				
			foreach (string tagName in projectTags)
				{
				Tag tag = tags[tagName];
				
				if (tag == null)
					{
					tag = new Tag(tagName);
					tag.InProjectFile = true;
					tags.Add(tag);
					}
				else
					{
					tag.InProjectFile = true;
					tag.FixNameCapitalization(tagName);
					}
				}
				
				
			// All the comment types have to exist in IDObjects.Manager before the properties are set because Index With will need their 
			// IDs.  This pass only creates the types that were not already created by the binary file.
			
			// We don't need to do separate passes for standard entries and alter entries because alter entries should only appear 
			// in the project file and only apply to types in the system file.  Anything else is either an error (system file can't alter a 
			// project entry) or would have been simplified out by LoadFile (a file with an alter entry applying to a type in the same 
			// file.)

			foreach (ConfigFileCommentType commentType in systemCommentTypeList)
				{  
				if (!Start_CreateType(commentType, systemFile, true, errorList))
					{  success = false;  }
				}

			foreach (ConfigFileCommentType commentType in projectCommentTypeList)
				{  
				if (!Start_CreateType(commentType, projectFile, false, errorList))
					{  success = false;  }
				}
				
			// Need to exit early because Start_ApplyProperties assumes all the types were created correctly.
			if (success == false)
				{  return false;  }


			// Now that everything's in commentTypes we can delete the ones that aren't in the text files, meaning they were in 
			// the binary file from the last run but were deleted since then.  We have to put them on a list and delete them in a 
			// second pass because deleting them while iterating through would screw up the iterator.
			
			List<int> deletedIDs = new List<int>();
			
			foreach (CommentType commentType in commentTypes)
				{
				if (commentType.Flags.InConfigFiles == false)
					{
					deletedIDs.Add(commentType.ID);
					changed = true;
					}
				}
				
			foreach (int deletedID in deletedIDs)
				{  commentTypes.Remove(deletedID);  }
				
				
			// Delete the tags that weren't in the text files as well.
			
			deletedIDs.Clear();
			
			foreach (Tag tag in tags)
				{
				if (tag.InConfigFiles == false)
					{
					deletedIDs.Add(tag.ID);
					changed = true;
					}
				}
				
			foreach (int deletedID in deletedIDs)
				{  tags.Remove(deletedID);  }
				
				
			// Fill in the properties
			
			foreach (ConfigFileCommentType commentType in systemCommentTypeList)
				{  
				if (!Start_ApplyProperties(commentType, systemFile, ignoredKeywords, errorList))
					{  success = false;  }
				}

			foreach (ConfigFileCommentType commentType in projectCommentTypeList)
				{  
				if (!Start_ApplyProperties(commentType, projectFile, ignoredKeywords, errorList))
					{  success = false;  }
				}
				
			if (success == false)
				{  return false;  }
				
				
			// Make sure there are no circular dependencies in Index With.
			
			foreach (CommentType commentType in commentTypes)
				{
				if (commentType.Index == CommentType.IndexValue.IndexWith)
					{
					IDObjects.NumberSet ids = new IDObjects.NumberSet();
					CommentType currentType = commentType;
					
					do
						{
						ids.Add(currentType.ID);
						
						if (ids.Contains(currentType.IndexWith))
							{
							// Start the dependency message on the repeated comment type, not on the one the loop started with because
							// it could go A > B > C > B, in which case reporting A is irrelevant.
							
							int repeatedID = currentType.IndexWith;
							CommentType iterator = commentTypes[repeatedID];
							string repeatMessage = iterator.Name;

							// We want the error message to be on the repeated type only if that's the only one: A > A.  Otherwise we
							// want it to be the second to last one: C in A > B > C > B.
							CommentType errorMessageTarget = currentType;

							for (;;)
								{
								iterator = commentTypes[iterator.IndexWith];
								repeatMessage += " > " + iterator.Name;
								
								if (iterator.ID == repeatedID)
									{  break;  }

								errorMessageTarget = iterator;
								}
								
							Path errorMessageFile;
							List <ConfigFileCommentType> searchList;
							
							if (errorMessageTarget.Flags.InProjectFile)
								{
								errorMessageFile = projectFile;
								searchList = projectCommentTypeList;
								}
							else
								{
								errorMessageFile = systemFile;
								searchList = systemCommentTypeList;
								}
								
							int errorMessageLineNumber = 0;
							string lcErrorMessageTargetName = errorMessageTarget.Name.ToLower();
							
							foreach (ConfigFileCommentType searchListType in searchList)
								{  
								if (searchListType.Name.ToLower() == lcErrorMessageTargetName)
									{
									errorMessageLineNumber = searchListType.LineNumber;
									break;
									}
								}

							errorList.Add( 
								Locale.Get("NaturalDocs.Engine", "Comments.txt.CircularDependencyInIndexWith(list)", repeatMessage),
								errorMessageFile, errorMessageLineNumber 
								);
															
							return false;
							}
						
						currentType = commentTypes[currentType.IndexWith];
						}
					while (currentType.Index == CommentType.IndexValue.IndexWith);
					}
				}
				
				
			// Simplify Index With.  So A > B > C becomes A > C.  Also A > B = no indexing becomes A = no indexing.
			
			foreach (CommentType commentType in commentTypes)
				{
				if (commentType.Index == CommentType.IndexValue.IndexWith)
					{
					CommentType targetCommentType = commentTypes[commentType.IndexWith];
					
					while (targetCommentType.Index == CommentType.IndexValue.IndexWith)
						{  targetCommentType = commentTypes[targetCommentType.IndexWith];  }
						
					if (targetCommentType.Index == CommentType.IndexValue.No)
						{  commentType.Index = CommentType.IndexValue.No;  }
					else
						{  commentType.IndexWith = targetCommentType.ID;  }
					}
				}
				
				
			// Everything is okay at this point.  Save the files again to reformat them.  If the project file didn't exist, saving it 
			// with the empty structures we created will create it.
			
			Start_FixCapitalization(systemCommentTypeList);
			Start_FixCapitalization(projectCommentTypeList);
			
			if (!commentsTxtParser.Save(projectFile, projectCommentTypeList, ignoredProjectKeywords, projectTags, errorList, true, false))
				{  success = false;  };
				
			if (!commentsTxtParser.Save(systemFile, systemCommentTypeList, ignoredSystemKeywords, systemTags, errorList, false, true))
				{  success = false;  };
			
			
			// Compare the structures with the binary ones to see if anything changed.

			if (changed == false)
				{
				// First an easy comparison.
				
				if (binaryCommentTypes.Count != commentTypes.Count || 
					binaryTags.Count != tags.Count ||
					binaryIgnoredKeywords.Count != ignoredKeywords.Count ||
					singularKeywords.Count != binarySingularKeywords.Count || 
					pluralKeywords.Count != binaryPluralKeywords.Count)
					{
					changed = true;
					}
				}
				
			if (changed == false)
				{
				// Next a detailed comparison if necessary.
				
				foreach (CommentType binaryCommentType in binaryCommentTypes)
					{
					CommentType commentType = commentTypes[binaryCommentType.ID];
					
					if (commentType == null || binaryCommentType != commentType)
						{  
						changed = true;
						break;
						}
					}
					
				if (changed == false)
					{
					foreach (Tag binaryTag in binaryTags)
						{
						Tag tag = tags[binaryTag.ID];
						
						if (tag == null || binaryTag != tag)
							{
							changed = true;
							break;
							}
						}
					}
					
				if (changed == false)
					{
					foreach (string binaryIgnoredKeyword in binaryIgnoredKeywords)
						{
						if (!ignoredKeywords.Contains(binaryIgnoredKeyword))
							{
							changed = true;
							break;
							}
						}
					}
					
				if (changed == false)
					{
					foreach (KeyValuePair<string, int> binarySingularKeywordPair in binarySingularKeywords)
						{
						// We can use ID instead of Name because we know they match now.
						if (singularKeywords.ContainsKey(binarySingularKeywordPair.Key) == false ||
							singularKeywords[binarySingularKeywordPair.Key].ID != binarySingularKeywordPair.Value)
							{
							changed = true;
							break;
							}
						}
					}

				if (changed == false)
					{
					foreach (KeyValuePair<string, int> binaryPluralKeywordPair in binaryPluralKeywords)
						{
						// We can use ID instead of Name because we know they match now.
						if (pluralKeywords.ContainsKey(binaryPluralKeywordPair.Key) == false ||
							pluralKeywords[binaryPluralKeywordPair.Key].ID != binaryPluralKeywordPair.Value)
							{
							changed = true;
							break;
							}
						}
					}
				}

				
			commentsNDParser.Save(EngineInstance.Config.WorkingDataFolder + "/Comments.nd", 
										  commentTypes, tags, singularKeywords, pluralKeywords, ignoredKeywords);
								   
			if (success == true && changed == true)
				{  EngineInstance.Config.ReparseEverything = true;  }

			groupCommentTypeID = IDFromKeyword("group");
				
			return success;
			}
			
			
		/* Function: Start_CreateType
		 * A helper function that is used only by <Start()> to create an entry in <commentTypes> for a <ConfigFileCommentType>.
		 * Does not set any properties.  Returns whether it was able to do so without any errors.
		 */
		private bool Start_CreateType (ConfigFileCommentType configFileCommentType, Path sourceFile, bool isSystemFile,
													  Errors.ErrorList errorList)
			{
			if (configFileCommentType.AlterType == true)
				{
				// If altering a type that doesn't exist at all, or only exists in the binary files...
				if ( commentTypes.Contains(configFileCommentType.Name) == false ||
				     commentTypes[configFileCommentType.Name].Flags.InConfigFiles == false )
					{
					errorList.Add( 
						Locale.Get("NaturalDocs.Engine", "Comments.txt.AlteredCommentTypeDoesntExist(name)", configFileCommentType.Name),
						sourceFile, configFileCommentType.LineNumber 
						);
						
					return false;
					}
				}
				
			else // define type, not alter
				{
				// Error if defining a type that already exists in the config files.  Having it exist from the binary file is fine.
				if (commentTypes.Contains(configFileCommentType.Name))
					{
				    if (commentTypes[configFileCommentType.Name].Flags.InConfigFiles == true)
						{
						errorList.Add( 
							Locale.Get("NaturalDocs.Engine", "Comments.txt.CommentTypeAlreadyExists(name)", configFileCommentType.Name),
							sourceFile, configFileCommentType.LineNumber 
							);
							
						return false;
						}
					}
				else
					{					
					CommentType commentType = new CommentType(configFileCommentType.Name);
					commentTypes.Add(commentType);
					}
					
				if (isSystemFile)
					{  commentTypes[configFileCommentType.Name].Flags.InSystemFile = true;  }
				else
					{  commentTypes[configFileCommentType.Name].Flags.InProjectFile = true;  }
				}
				
			return true;
			}
			
			
			
		/* Function: Start_ApplyProperties
		 * A helper function that is used only by <Start()> to combine a <ConfigFileCommentType's> properties into <commentTypes> and
		 * its keywords into <singularKeywords> and <pluralKeywords>.  Assumes entries were already created for all of them by 
		 * <Start_CreateType()>.  Returns whether it was able to do so without causing an error.
		 */
		private bool Start_ApplyProperties (ConfigFileCommentType configFileCommentType, Path sourceFile, 
														    StringSet ignoredKeywords, Errors.ErrorList errorList)
			{
			CommentType commentType = commentTypes[configFileCommentType.Name];
			bool success = true;


			// Display names
			
			if (configFileCommentType.DisplayNameFromLocale != null)
				{  commentType.DisplayName = Locale.Get("NaturalDocs.Engine", configFileCommentType.DisplayNameFromLocale);  }
			else if (configFileCommentType.DisplayName != null)
				{  commentType.DisplayName = configFileCommentType.DisplayName;  }
				
			if (configFileCommentType.PluralDisplayNameFromLocale != null)
				{  commentType.PluralDisplayName = Locale.Get("NaturalDocs.Engine", configFileCommentType.PluralDisplayNameFromLocale);  }
			else if (configFileCommentType.PluralDisplayName != null)
				{  commentType.PluralDisplayName = configFileCommentType.PluralDisplayName;  }
				
				
			// Other properties
			
			if (configFileCommentType.SimpleIdentifier != null)
				{  commentType.SimpleIdentifier = configFileCommentType.SimpleIdentifier;  }
			
			if (configFileCommentType.Index != null)
				{
				commentType.Index = (CommentType.IndexValue)configFileCommentType.Index;
				
				if (commentType.Index == CommentType.IndexValue.IndexWith)
					{
					CommentType indexWithCommentType = commentTypes[ configFileCommentType.IndexWith ];
					
					if (indexWithCommentType == null)
						{
						errorList.Add( 
							Locale.Get("NaturalDocs.Engine", "Comments.txt.IndexWithCommentTypeDoesntExist(name)", configFileCommentType.IndexWith),
							sourceFile, configFileCommentType.LineNumber 
							);
							
						success = false;
						}
						
					else
						{
						commentType.IndexWith = indexWithCommentType.ID;
						}
					}
				}
				
			if (configFileCommentType.Scope != null)
				{  commentType.Scope = (CommentType.ScopeValue)configFileCommentType.Scope;  }
			if (configFileCommentType.Flags.AllConfigurationProperties != 0)
				{  commentType.Flags.AllConfigurationProperties = configFileCommentType.Flags.AllConfigurationProperties;  }
			if (configFileCommentType.BreakLists != null)
				{  commentType.BreakLists = (bool)configFileCommentType.BreakLists;  }
				
				
			// Keywords
			
			List<string> keywords = configFileCommentType.Keywords;
			
			for (int i = 0; i < keywords.Count; i += 2)
				{
				if (keywords[i] != null && !ignoredKeywords.Contains(keywords[i]))
					{
					singularKeywords.Add(keywords[i], commentType);
					pluralKeywords.Remove(keywords[i]);
					}
				if (keywords[i+1] != null && !ignoredKeywords.Contains(keywords[i+1]))
					{
					singularKeywords.Remove(keywords[i+1]);
					pluralKeywords.Add(keywords[i+1], commentType);
					}
				}
				
				
			return success;
			}


		/* Function: Start_FixCapitalization
		 * 
		 * A helper function used only by <Start()> which cleans up the capitalization of <ConfigFileCommentTypes> such as by 
		 * making Alter Comment Type and Index With entries match the original type.
		 * 
		 * Assumes <commentTypes> is already filled in and valid.
		 */
		public void Start_FixCapitalization (List<ConfigFileCommentType> configFileCommentTypes)
			{
			for (int i = 0; i < configFileCommentTypes.Count; i++)
				{
				if (configFileCommentTypes[i].AlterType == true)
					{
					configFileCommentTypes[i].FixNameCapitalization( commentTypes[ configFileCommentTypes[i].Name ].Name );
					}
					
				if (configFileCommentTypes[i].Index == CommentType.IndexValue.IndexWith)
					{
					configFileCommentTypes[i].IndexWith = commentTypes[ configFileCommentTypes[i].IndexWith ].Name;
					}
				}
			}


		/* Function: FromKeyword
		 * Returns the <CommentType> associated with the passed keyword, or null if none.
		 */
		public CommentType FromKeyword (string keyword)
			{
			bool ignore;
			return FromKeyword(keyword, out ignore);
			}


		/* Function: FromKeyword
		 * Returns the <CommentType> associated with the passed keyword, or null if none.  Also returns whether it was singular
		 * or plural.
		 */
		public CommentType FromKeyword (string keyword, out bool plural)
			{
			CommentType result = singularKeywords[keyword];
			
			if (result != null)
				{
				plural = false;
				return result;
				}
				
			result = pluralKeywords[keyword];
			
			if (result != null)
				{
				plural = true;
				return result;
				}
				
			plural = false;
			return null;
			}
			
		/* Function: FromName
		 * Returns the <CommentType> associated with the passed name, or null if none.
		 */
		public CommentType FromName (string name)
			{
			return commentTypes[name];
			}
			
		/* Function: FromID
		 * Returns the <CommentType> associated with the passed ID, or null if none.
		 */
		public CommentType FromID (int id)
			{
			return commentTypes[id];
			}
			
		/* Function: IDFromKeyword
		 * Returns the comment type ID associated with the passed keyword, or zero if none.
		 */
		public int IDFromKeyword (string keyword)
			{
			CommentType commentType = FromKeyword(keyword);

			if (commentType != null)
				{  return commentType.ID;  }
			else
				{  return 0;  }
			}

		/* Function: TagFromName
		 * Returns the <Tag> associated with the passed name, or null if none.
		 */
		public Tag TagFromName (string name)
			{
			return tags[name];
			}
			
		/* Function: TagFromID
		 * Returns the <Tag> associated with the passed ID, or null if none.
		 */
		public Tag TagFromID (int id)
			{
			return tags[id];
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: GroupCommentTypeID
		 * The ID of the "group" keyword, or zero if it isn't defined.
		 */
		public int GroupCommentTypeID
			{
			get
				{  return groupCommentTypeID;  }
			}
			

		
		// Group: Variables
		// __________________________________________________________________________


		/* var: commentTypes
		 * Manages all the <CommentTypes> by their case-insensitive name or ID number.
		 */
		protected IDObjects.Manager<CommentType> commentTypes;
		
		
		/* var: tags
		 * Manages all the <Tags> by their case-insensitive name or ID number.
		 */
		protected IDObjects.Manager<Tag> tags;

		
		/* var: singularKeywords
		 * A <StringTable> mapping the singular keywords to the <CommentTypes> they represent.
		 */
		protected StringTable<CommentType> singularKeywords;
		
		
		/* var: pluralKeywords
		 * A <StringTable> mapping the plural keywords to the <CommentTypes>s they represent.
		 */
		protected StringTable<CommentType> pluralKeywords;

		
		/* var: groupCommentTypeID
		 * The ID of the "group" keyword, or zero if it's not defined.
		 */
		protected int groupCommentTypeID;

		}
	}