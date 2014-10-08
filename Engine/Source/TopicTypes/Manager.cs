/* 
 * Class: GregValure.NaturalDocs.Engine.TopicTypes.Manager
 * ____________________________________________________________________________
 * 
 * A module to handle <Topics.txt> and all the topic settings within Natural Docs.
 * 
 * 
 * Topic: Usage
 * 
 *		- Call <Engine.Instance.Start()> which will start this module.
 *		  
 *		
 * 
 * Group: Files
 * ____________________________________________________________________________
 * 
 *			
 * 
 * 
 * File: Topics.nd
 * 
 *		A binary file which stores the combined results of the two versions of <Topics.txt> as of the last run, as well as storing
 *		the IDs of each type so they maintain their consistency between runs.
 *		
 *		Format:
 *		
 *			> [[Binary Header]]
 *		
 *			The file starts with the standard binary file header as managed by <BinaryFile>.
 *			
 *			> [String: Tag Name]
 *			> [Int32: ID]
 *			> ...
 *			> [String: null]
 *			
 *			The file then has pairs of tag names and IDs until it reaches a null string.
 *			
 *			> [String: Topic Type Name]
 *			> [[Topic Type Attributes]]
 *			> ...
 *			> [String: null]
 *			
 *			The file then encodes each topic type by its name string, followed by its attributes, and repeats until it reaches a null
 *			string instead of a new name string.
 *			
 *			> Topic Type Attributes:
 *			> [Int32: ID]
 *			> [String: Display Name]
 *			> [String: Plural Display Name]
 *			> [String: Simple Identifier]
 *			> [Byte: Index]
 *			> [Int32: Index With ID]?
 *			> [Byte: Scope]
 *			> [Byte: Break Lists]
 *			> [UInt16: Flags]
 *			
 *			The attributes include strings for the display and plural display names.  These are the computed strings, so if they
 *			weren't defined they'll still be here via whatever inheritance rules are in play.  If it's defined by the locale, it's the 
 *			resulting string that was retrieved from it.
 *			
 *			IndexWithID is the identifier of the topic type to index with and is only present if Index is set to 
 *			<TopicTypes.IndexValue.IndexWith>.
 *			
 *			> [String: Singular Keyword]
 *			> [Int32: Topic Type ID]
 *			> ...
 *			> [String: null]
 *			
 *			Next is a list of all the defined singular keywords and the IDs of the types they are mapped to.  They occur in pairs
 *			until a null string appears in place of the keyword.
 *			
 *			> [String: Plural Keyword]
 *			> [Int32: Topic Type ID]
 *			> ...
 *			> [String: null]
 *			
 *			Next is a similar list of plural keywords.
 *			
 *			> [String: Ignored Keyword]
 *			> ...
 *			> [String: null]
 *			
 *			Next is a similar list of ignored keywords, only the topic type ID is omitted.
 *			
 *		Revisions:
 *		
 *			2.0:
 *			
 *				- The file is introduced.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Collections;


namespace GregValure.NaturalDocs.Engine.TopicTypes
	{
	public class Manager
		{
		
		// Group: Constants
		// __________________________________________________________________________
		
		
		public const KeySettings KeySettingsForKeywords = KeySettings.IgnoreCase | KeySettings.NormalizeUnicode;
		public const KeySettings KeySettingsForTopicTypes = KeySettings.IgnoreCase | KeySettings.NormalizeUnicode;
		public const KeySettings KeySettingsForTags = KeySettings.IgnoreCase | KeySettings.NormalizeUnicode;
		
		
		
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Manager
		 */
		public Manager ()
			{
			// Topic type names aren't normalized because they're only referenced in other config files.  Tags and keywords are
			// referenced in source files so they should be more tolerant.
			topicTypes = new IDObjects.Manager<TopicType>(KeySettingsForTopicTypes, false);
			tags = new IDObjects.Manager<Tag>(KeySettingsForTags, false);
			
			singularKeywords = new StringTable<TopicType>(KeySettingsForKeywords);
			pluralKeywords = new StringTable<TopicType>(KeySettingsForKeywords);

			groupTopicTypeID = 0;
			}
			
			
		/* Function: Start
		 * 
		 * Loads and combines the two versions of <Topics.txt>, returning whether it was successful.  If there were any errors
		 * they will be added to errorList.
		 * 
		 * Dependencies:
		 * 
		 *		- <Config.Manager> must be started before this class can start.
		 */
		public bool Start (Errors.ErrorList errorList)
			{
			List<ConfigFileTopicType> systemTopicTypeList;
			List<ConfigFileTopicType> projectTopicTypeList;
			List<string> ignoredSystemKeywords;
			List<string> ignoredProjectKeywords;
			List<string> systemTags;
			List<string> projectTags;
			
			List<TopicType> binaryTopicTypes;
			List<Tag> binaryTags;
			List<KeyValuePair<string, int>> binarySingularKeywords;
			List<KeyValuePair<string, int>> binaryPluralKeywords;
			List<string> binaryIgnoredKeywords;
			
			// The return value, which is whether we were able to successfully load and parse the system Topics.txt, and if it exists,
			// the project Topics.txt.  The project Topics.txt not existing is not a failure.
			bool success = true;
			
			// Whether anything has changed since the last run, as determined by Topics.nd.  If Topics.nd doesn't exist or is corrupt,
			// we have to assume something changed.
			bool changed = false;


			// We need the ID numbers to stay consistent between runs, so we need to create all the topic types and tags from the
			// binary file first.  We'll worry about comparing their attributes and seeing if any were added or deleted later.

			if (Engine.Instance.Config.ReparseEverything == true)
				{
				binaryTopicTypes = new List<TopicType>();
				binaryTags = new List<Tag>();
				binarySingularKeywords = new List<KeyValuePair<string,int>>();
				binaryPluralKeywords = new List<KeyValuePair<string,int>>();
				binaryIgnoredKeywords = new List<string>();
				
				changed = true;
				}
				
			else if (LoadBinaryFile(Engine.Instance.Config.WorkingDataFolder + "/Topics.nd", out binaryTopicTypes, out binaryTags, 
											 out binarySingularKeywords, out binaryPluralKeywords, out binaryIgnoredKeywords) == false)
				{
				changed = true;
				// Even though it failed, LoadBinaryFile will still create valid empty objects for the variables.
				}
				
			else // LoadBinaryFile succeeded
				{
				// We use a try block so if anything screwy happens, like two things having the same ID number and thus causing 
				// an exception when added, we can continue as if the binary file didn't parse at all.
				try
					{
					foreach (TopicType binaryTopicType in binaryTopicTypes)
						{
						// We don't add the binary topic type itself because we only want those for comparison purposes.  We want 
						// the types in topicTypes to be at their default values because the Topics.txt versions will only set some attributes, 
						// not all, and we don't want the unset attributes influenced by the binary versions.
						TopicType newTopicType = new TopicType(binaryTopicType.Name);
						newTopicType.ID = binaryTopicType.ID;
						newTopicType.Flags.InBinaryFile = true;
						
						topicTypes.Add(newTopicType);
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
					topicTypes.Clear();
					tags.Clear();
					changed = true;
					
					// Clear them since they may be used later in this function.
					binaryTopicTypes.Clear();
					binarySingularKeywords.Clear();
					binaryPluralKeywords.Clear();
					binaryIgnoredKeywords.Clear();
					
					// Otherwise ignore the exception and continue.
					}
				}

			
			Path systemFile = Engine.Instance.Config.SystemConfigFolder + "/Topics.txt";
			Path projectFile = Engine.Instance.Config.ProjectConfigFolder + "/Topics.txt";

			Topics_txt topicsTxtParser = new Topics_txt();

			
			// Load the files.
			
			if (!topicsTxtParser.Load( systemFile, out systemTopicTypeList, out ignoredSystemKeywords, out systemTags, errorList ))
				{  
				success = false;  
				// Continue anyway because we want to show errors from both files.
				}
			
			if (System.IO.File.Exists(projectFile))
				{
				if (!topicsTxtParser.Load( projectFile, out projectTopicTypeList, out ignoredProjectKeywords, out projectTags, errorList ))
					{  success = false;  }
				}
			else
				{
				// The project file not existing is not an error condition.  Fill in the variables with empty structures.
				projectTopicTypeList = new List<ConfigFileTopicType>();
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
				
				
			// All the topic types have to exist in IDObjects.Manager before the properties are set because Index With will need their 
			// IDs.  This pass only creates the types that were not already created by the binary file.
			
			// We don't need to do separate passes for standard entries and alter entries because alter entries should only appear 
			// in the project file and only apply to types in the system file.  Anything else is either an error (system file can't alter a 
			// project entry) or would have been simplified out by LoadFile (a file with an alter entry applying to a type in the same 
			// file.)

			foreach (ConfigFileTopicType topicType in systemTopicTypeList)
				{  
				if (!Start_CreateType(topicType, systemFile, true, errorList))
					{  success = false;  }
				}

			foreach (ConfigFileTopicType topicType in projectTopicTypeList)
				{  
				if (!Start_CreateType(topicType, projectFile, false, errorList))
					{  success = false;  }
				}
				
			// Need to exit early because Start_ApplyProperties assumes all the types were created correctly.
			if (success == false)
				{  return false;  }


			// Now that everything's in topicTypes we can delete the ones that aren't in the text files, meaning they were in 
			// the binary file from the last run but were deleted since then.  We have to put them on a list and delete them in a 
			// second pass because deleting them while iterating through would screw up the iterator.
			
			List<int> deletedIDs = new List<int>();
			
			foreach (TopicType topicType in topicTypes)
				{
				if (topicType.Flags.InConfigFiles == false)
					{
					deletedIDs.Add(topicType.ID);
					changed = true;
					}
				}
				
			foreach (int deletedID in deletedIDs)
				{  topicTypes.Remove(deletedID);  }
				
				
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
			
			foreach (ConfigFileTopicType topicType in systemTopicTypeList)
				{  
				if (!Start_ApplyProperties(topicType, systemFile, ignoredKeywords, errorList))
					{  success = false;  }
				}

			foreach (ConfigFileTopicType topicType in projectTopicTypeList)
				{  
				if (!Start_ApplyProperties(topicType, projectFile, ignoredKeywords, errorList))
					{  success = false;  }
				}
				
			if (success == false)
				{  return false;  }
				
				
			// Make sure there are no circular dependencies in Index With.
			
			foreach (TopicType topicType in topicTypes)
				{
				if (topicType.Index == TopicType.IndexValue.IndexWith)
					{
					IDObjects.NumberSet ids = new IDObjects.NumberSet();
					TopicType currentType = topicType;
					
					do
						{
						ids.Add(currentType.ID);
						
						if (ids.Contains(currentType.IndexWith))
							{
							// Start the dependency message on the repeated topic type, not on the one the loop started with because
							// it could go A > B > C > B, in which case reporting A is irrelevant.
							
							int repeatedID = currentType.IndexWith;
							TopicType iterator = topicTypes[repeatedID];
							string repeatMessage = iterator.Name;

							// We want the error message to be on the repeated type only if that's the only one: A > A.  Otherwise we
							// want it to be the second to last one: C in A > B > C > B.
							TopicType errorMessageTarget = currentType;

							for (;;)
								{
								iterator = topicTypes[iterator.IndexWith];
								repeatMessage += " > " + iterator.Name;
								
								if (iterator.ID == repeatedID)
									{  break;  }

								errorMessageTarget = iterator;
								}
								
							Path errorMessageFile;
							List <ConfigFileTopicType> searchList;
							
							if (errorMessageTarget.Flags.InProjectFile)
								{
								errorMessageFile = projectFile;
								searchList = projectTopicTypeList;
								}
							else
								{
								errorMessageFile = systemFile;
								searchList = systemTopicTypeList;
								}
								
							int errorMessageLineNumber = 0;
							string lcErrorMessageTargetName = errorMessageTarget.Name.ToLower();
							
							foreach (ConfigFileTopicType searchListType in searchList)
								{  
								if (searchListType.Name.ToLower() == lcErrorMessageTargetName)
									{
									errorMessageLineNumber = searchListType.LineNumber;
									break;
									}
								}

							errorList.Add( 
								Locale.Get("NaturalDocs.Engine", "Topics.txt.CircularDependencyInIndexWith(list)", repeatMessage),
								errorMessageFile, errorMessageLineNumber 
								);
															
							return false;
							}
						
						currentType = topicTypes[currentType.IndexWith];
						}
					while (currentType.Index == TopicType.IndexValue.IndexWith);
					}
				}
				
				
			// Simplify Index With.  So A > B > C becomes A > C.  Also A > B = no indexing becomes A = no indexing.
			
			foreach (TopicType topicType in topicTypes)
				{
				if (topicType.Index == TopicType.IndexValue.IndexWith)
					{
					TopicType targetTopicType = topicTypes[topicType.IndexWith];
					
					while (targetTopicType.Index == TopicType.IndexValue.IndexWith)
						{  targetTopicType = topicTypes[targetTopicType.IndexWith];  }
						
					if (targetTopicType.Index == TopicType.IndexValue.No)
						{  topicType.Index = TopicType.IndexValue.No;  }
					else
						{  topicType.IndexWith = targetTopicType.ID;  }
					}
				}
				
				
			// Everything is okay at this point.  Save the files again to reformat them.  If the project file didn't exist, saving it 
			// with the empty structures we created will create it.
			
			Start_FixCapitalization(systemTopicTypeList);
			Start_FixCapitalization(projectTopicTypeList);
			
			if (!topicsTxtParser.Save(projectFile, projectTopicTypeList, ignoredProjectKeywords, projectTags, errorList, true, false))
				{  success = false;  };
				
			if (!topicsTxtParser.Save(systemFile, systemTopicTypeList, ignoredSystemKeywords, systemTags, errorList, false, true))
				{  success = false;  };
			
			
			// Compare the structures with the binary ones to see if anything changed.

			if (changed == false)
				{
				// First an easy comparison.
				
				if (binaryTopicTypes.Count != topicTypes.Count || 
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
				
				foreach (TopicType binaryTopicType in binaryTopicTypes)
					{
					TopicType topicType = topicTypes[binaryTopicType.ID];
					
					if (topicType == null || binaryTopicType != topicType)
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

				
			SaveBinaryFile(Engine.Instance.Config.WorkingDataFolder + "/Topics.nd", 
								   topicTypes, tags, singularKeywords, pluralKeywords, ignoredKeywords);
								   
			if (success == true && changed == true)
				{  Engine.Instance.Config.ReparseEverything = true;  }

			groupTopicTypeID = IDFromKeyword("group");
				
			return success;
			}
			
			
		/* Function: Start_CreateType
		 * A helper function that is used only by <Start()> to create an entry in <topicTypes> for a <ConfigFileTopicType>.
		 * Does not set any properties.  Returns whether it was able to do so without any errors.
		 */
		private bool Start_CreateType (ConfigFileTopicType configFileTopicType, Path sourceFile, bool isSystemFile,
													  Errors.ErrorList errorList)
			{
			if (configFileTopicType.AlterType == true)
				{
				// If altering a type that doesn't exist at all, or only exists in the binary files...
				if ( topicTypes.Contains(configFileTopicType.Name) == false ||
				     topicTypes[configFileTopicType.Name].Flags.InConfigFiles == false )
					{
					errorList.Add( 
						Locale.Get("NaturalDocs.Engine", "Topics.txt.AlteredTopicTypeDoesntExist(name)", configFileTopicType.Name),
						sourceFile, configFileTopicType.LineNumber 
						);
						
					return false;
					}
				}
				
			else // define type, not alter
				{
				// Error if defining a type that already exists in the config files.  Having it exist from the binary file is fine.
				if (topicTypes.Contains(configFileTopicType.Name))
					{
				    if (topicTypes[configFileTopicType.Name].Flags.InConfigFiles == true)
						{
						errorList.Add( 
							Locale.Get("NaturalDocs.Engine", "Topics.txt.TopicTypeAlreadyExists(name)", configFileTopicType.Name),
							sourceFile, configFileTopicType.LineNumber 
							);
							
						return false;
						}
					}
				else
					{					
					TopicType topicType = new TopicType(configFileTopicType.Name);
					topicTypes.Add(topicType);
					}
					
				if (isSystemFile)
					{  topicTypes[configFileTopicType.Name].Flags.InSystemFile = true;  }
				else
					{  topicTypes[configFileTopicType.Name].Flags.InProjectFile = true;  }
				}
				
			return true;
			}
			
			
			
		/* Function: Start_ApplyProperties
		 * A helper function that is used only by <Start()> to combine a <ConfigFileTopicType's> properties into <topicTypes> and
		 * its keywords into <singularKeywords> and <pluralKeywords>.  Assumes entries were already created for all of them by 
		 * <Start_CreateType()>.  Returns whether it was able to do so without causing an error.
		 */
		private bool Start_ApplyProperties (ConfigFileTopicType configFileTopicType, Path sourceFile, 
														    StringSet ignoredKeywords, Errors.ErrorList errorList)
			{
			TopicType topicType = topicTypes[configFileTopicType.Name];
			bool success = true;


			// Display names
			
			if (configFileTopicType.DisplayNameFromLocale != null)
				{  topicType.DisplayName = Locale.Get("NaturalDocs.Engine", configFileTopicType.DisplayNameFromLocale);  }
			else if (configFileTopicType.DisplayName != null)
				{  topicType.DisplayName = configFileTopicType.DisplayName;  }
				
			if (configFileTopicType.PluralDisplayNameFromLocale != null)
				{  topicType.PluralDisplayName = Locale.Get("NaturalDocs.Engine", configFileTopicType.PluralDisplayNameFromLocale);  }
			else if (configFileTopicType.PluralDisplayName != null)
				{  topicType.PluralDisplayName = configFileTopicType.PluralDisplayName;  }
				
				
			// Other properties
			
			if (configFileTopicType.SimpleIdentifier != null)
				{  topicType.SimpleIdentifier = configFileTopicType.SimpleIdentifier;  }
			
			if (configFileTopicType.Index != null)
				{
				topicType.Index = (TopicType.IndexValue)configFileTopicType.Index;
				
				if (topicType.Index == TopicType.IndexValue.IndexWith)
					{
					TopicType indexWithTopicType = topicTypes[ configFileTopicType.IndexWith ];
					
					if (indexWithTopicType == null)
						{
						errorList.Add( 
							Locale.Get("NaturalDocs.Engine", "Topics.txt.IndexWithTopicTypeDoesntExist(name)", configFileTopicType.IndexWith),
							sourceFile, configFileTopicType.LineNumber 
							);
							
						success = false;
						}
						
					else
						{
						topicType.IndexWith = indexWithTopicType.ID;
						}
					}
				}
				
			if (configFileTopicType.Scope != null)
				{  topicType.Scope = (TopicType.ScopeValue)configFileTopicType.Scope;  }
			if (configFileTopicType.Flags.AllConfigurationProperties != 0)
				{  topicType.Flags.AllConfigurationProperties = configFileTopicType.Flags.AllConfigurationProperties;  }
			if (configFileTopicType.BreakLists != null)
				{  topicType.BreakLists = (bool)configFileTopicType.BreakLists;  }
				
				
			// Keywords
			
			List<string> keywords = configFileTopicType.Keywords;
			
			for (int i = 0; i < keywords.Count; i += 2)
				{
				if (keywords[i] != null && !ignoredKeywords.Contains(keywords[i]))
					{
					singularKeywords.Add(keywords[i], topicType);
					pluralKeywords.Remove(keywords[i]);
					}
				if (keywords[i+1] != null && !ignoredKeywords.Contains(keywords[i+1]))
					{
					singularKeywords.Remove(keywords[i+1]);
					pluralKeywords.Add(keywords[i+1], topicType);
					}
				}
				
				
			return success;
			}


		/* Function: Start_FixCapitalization
		 * 
		 * A helper function used only by <Start()> which cleans up the capitalization of <ConfigFileTopicTypes> such as by 
		 * making Alter Topic Type and Index With entries match the original type.
		 * 
		 * Assumes <topicTypse> is already filled in and valid.
		 */
		public void Start_FixCapitalization (List<ConfigFileTopicType> configFileTopicTypes)
			{
			for (int i = 0; i < configFileTopicTypes.Count; i++)
				{
				if (configFileTopicTypes[i].AlterType == true)
					{
					configFileTopicTypes[i].FixNameCapitalization( topicTypes[ configFileTopicTypes[i].Name ].Name );
					}
					
				if (configFileTopicTypes[i].Index == TopicType.IndexValue.IndexWith)
					{
					configFileTopicTypes[i].IndexWith = topicTypes[ configFileTopicTypes[i].IndexWith ].Name;
					}
				}
			}


		/* Function: FromKeyword
		 * Returns the <TopicType> associated with the passed keyword, or null if none.
		 */
		public TopicType FromKeyword (string keyword)
			{
			bool ignore;
			return FromKeyword(keyword, out ignore);
			}


		/* Function: FromKeyword
		 * Returns the <TopicType> associated with the passed keyword, or null if none.  Also returns whether it was singular
		 * or plural.
		 */
		public TopicType FromKeyword (string keyword, out bool plural)
			{
			TopicType result = singularKeywords[keyword];
			
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
		 * Returns the <TopicType> associated with the passed name, or null if none.
		 */
		public TopicType FromName (string name)
			{
			return topicTypes[name];
			}
			
		/* Function: FromID
		 * Returns the <TopicType> associated with the passed ID, or null if none.
		 */
		public TopicType FromID (int id)
			{
			return topicTypes[id];
			}
			
		/* Function: IDFromKeyword
		 * Returns the topic type ID associated with the passed keyword, or zero if none.
		 */
		public int IDFromKeyword (string keyword)
			{
			TopicType topicType = FromKeyword(keyword);

			if (topicType != null)
				{  return topicType.ID;  }
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
			
			
			
			
		// Group: Static Functions
		// __________________________________________________________________________
		
		
			
			
			
			
		/* Function: LoadBinaryFile
		 * Loads the information in <Topics.nd>, which is the computed topic settings from the last time Natural Docs was run.
		 * Returns whether it was successful.  If not all the out parameters will still return objects, they will just be empty.  
		 */
		public static bool LoadBinaryFile (Path filename,
																		out List<TopicType> binaryTopicTypes, 
																		out List<Tag> binaryTags,
																	 	out List<KeyValuePair<string, int>> binarySingularKeywords,
																		out List<KeyValuePair<string, int>> binaryPluralKeywords, 
																		out List<string> binaryIgnoredKeywords)
			{
			binaryTopicTypes = new List<TopicType>();
			binaryTags = new List<Tag>();
			
			binarySingularKeywords = new List<KeyValuePair<string,int>>();
			binaryPluralKeywords = new List<KeyValuePair<string,int>>();
			binaryIgnoredKeywords = new List<string>();
			
			BinaryFile file = new BinaryFile();
			bool result = true;
			
			try
				{
				if (file.OpenForReading(filename, "2.0") == false)
					{
					result = false;
					}
				else
					{
					
					// [String: Tag Name]
					// [Int32: ID]
					// ...
					// [String: null]
					
					string tagName = file.ReadString();
					
					while (tagName != null)
						{
						Tag tag = new Tag(tagName);
						tag.ID = file.ReadInt32();
						binaryTags.Add(tag);

						tagName = file.ReadString();
						}
						

					// [String: Topic Type Name]
					// [Int32: ID]
					// [String: Display Name]
					// [String: Plural Display Name]
					// [String: Simple Identifier]
					// [Byte: Index]
					// [Int32: Index With ID]?
					// [Byte: Scope]
					// [Byte: Break Lists]
					// [UInt16: Flags]
					// ...
					// [String: null]
						
					string topicTypeName = file.ReadString();
					IDObjects.NumberSet topicTypeIDs = new IDObjects.NumberSet();
					
					while (topicTypeName != null)
						{
						TopicType topicType = new TopicType(topicTypeName);
						
						topicType.ID = file.ReadInt32();
						topicType.DisplayName = file.ReadString();
						topicType.PluralDisplayName = file.ReadString();
						topicType.SimpleIdentifier = file.ReadString();

						// We don't have to validate the enum and flag values because they're only used to compare to the config file 
						// versions, which are validated.  If these are invalid they'll just show up as changed.

						topicType.Index = (TopicType.IndexValue)file.ReadByte();

						if (topicType.Index == TopicType.IndexValue.IndexWith)
							{  topicType.IndexWith = file.ReadInt32();  }

						topicType.Scope = (TopicType.ScopeValue)file.ReadByte();
						topicType.BreakLists = (file.ReadByte() != 0);
						topicType.Flags.AllConfigurationProperties = (TopicTypeFlags.FlagValues)file.ReadUInt16();

						binaryTopicTypes.Add(topicType);
						topicTypeIDs.Add(topicType.ID);
						
						topicTypeName = file.ReadString();
						}
						
					// Check the Index With values after they're all entered in.
					foreach (TopicType topicType in binaryTopicTypes)
						{
						if (topicType.Index == TopicType.IndexValue.IndexWith && !topicTypeIDs.Contains(topicType.IndexWith))
							{  result = false;  }
						}

				
					// [String: Singular Keyword]
					// [Int32: Topic Type ID]
					// ...
					// [String: null]

					string keyword = file.ReadString();
					
					while (keyword != null)
						{
						int id = file.ReadInt32();
						
						binarySingularKeywords.Add( new KeyValuePair<string,int>(keyword, id) );
						if (!topicTypeIDs.Contains(id))
							{  result = false;  }
						
						keyword = file.ReadString();
						}	

				
					// [String: Plural Keyword]
					// [Int32: Topic Type ID]
					// ...
					// [String: null]

					keyword = file.ReadString();
					
					while (keyword != null)
						{
						int id = file.ReadInt32();
						
						binaryPluralKeywords.Add( new KeyValuePair<string, int>(keyword, id) );
						if (!topicTypeIDs.Contains(id))
							{  result = false;  }
						
						keyword = file.ReadString();
						}						

				
					// [String: Ignored Keyword]
					// ...
					// [String: null]

					keyword = file.ReadString();
					
					while (keyword != null)
						{
						binaryIgnoredKeywords.Add(keyword);
						
						keyword = file.ReadString();
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
				binaryTopicTypes.Clear();
				
				binarySingularKeywords.Clear();
				binaryPluralKeywords.Clear();
				binaryIgnoredKeywords.Clear();
				}
				
			return result;
			}
			
			
		/* Function: SaveBinaryFile
		 * Saves the current computed topic types into <Topics.nd>.  Throws an exception if unsuccessful.
		 */
		public static void SaveBinaryFile (Path filename, IDObjects.Manager<TopicType> topicTypes, IDObjects.Manager<Tag> tags,
																		StringTable<TopicType> singularKeywords, StringTable<TopicType> pluralKeywords,
																		StringSet ignoredKeywords)
			{
			BinaryFile file = new BinaryFile();
			file.OpenForWriting(filename);

			try
				{

				// [String: Tag Name]
				// [Int32: ID]
				// ...
				// [String: null]
				
				foreach (Tag tag in tags)
					{
					file.WriteString(tag.Name);
					file.WriteInt32(tag.ID);
					}
					
				file.WriteString(null);
				

				// [String: Topic Type Name]
				// [Int32: ID]
				// [String: Display Name]
				// [String: Plural Display Name]
				// [String: Simple Identifier]
				// [Byte: Index]
				// [Int32: Index With ID]?
				// [Byte: Scope]
				// [Byte: Break Lists]
				// [UInt16: Flags]
				// ...
				// [String: null]

				foreach (TopicType topicType in topicTypes)
					{
					file.WriteString( topicType.Name );
					file.WriteInt32( topicType.ID );
					file.WriteString( topicType.DisplayName );
					file.WriteString( topicType.PluralDisplayName );
					file.WriteString( topicType.SimpleIdentifier );
					file.WriteByte( (byte)topicType.Index );
					
					if (topicType.Index == TopicType.IndexValue.IndexWith)
						{  file.WriteInt32( topicType.IndexWith );  }

					file.WriteByte( (byte)topicType.Scope );
					file.WriteByte( (byte)(topicType.BreakLists ? 1 : 0) );
					file.WriteUInt16( (ushort)topicType.Flags.AllConfigurationProperties );
					}
					
				file.WriteString(null);
				
				
				// [String: Singular Keyword]
				// [Int32: Topic Type ID]
				// ...
				// [String: null]

				foreach (KeyValuePair<string, TopicType> pair in singularKeywords)
					{
					file.WriteString( pair.Key );
					file.WriteInt32( pair.Value.ID );
					}
					
				file.WriteString(null);
				
				
				// [String: Plural Keyword]
				// [Int32: Topic Type ID]
				// ...
				// [String: null]

				foreach (KeyValuePair<string, TopicType> pair in pluralKeywords)
					{
					file.WriteString( pair.Key );
					file.WriteInt32( pair.Value.ID );
					}
					
				file.WriteString(null);
				
				
				// [String: Ignored Keyword]
				// ...
				// [String: null]

				foreach (string keyword in ignoredKeywords)
					{
					file.WriteString( keyword );
					}
					
				file.WriteString(null);
				}
				
			finally
				{
				file.Close();
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: GroupTopicTypeID
		 * The ID of the "group" keyword, or zero if it isn't defined.
		 */
		public int GroupTopicTypeID
			{
			get
				{  return groupTopicTypeID;  }
			}
			

		
		// Group: Variables
		// __________________________________________________________________________


		/* var: topicTypes
		 * Manages all the <TopicType>s by their case-insensitive name or ID number.
		 */
		protected IDObjects.Manager<TopicType> topicTypes;
		
		
		/* var: tags
		 * Manages all the <Tags> by their case-insensitive name or ID number.
		 */
		protected IDObjects.Manager<Tag> tags;

		
		/* var: singularKeywords
		 * A <StringTable> mapping the singular keywords to the <TopicType>s they represent.
		 */
		protected StringTable<TopicType> singularKeywords;
		
		
		/* var: pluralKeywords
		 * A <StringTable> mapping the plural keywords to the <TopicType>s they represent.
		 */
		protected StringTable<TopicType> pluralKeywords;

		
		/* var: groupTopicTypeID
		 * The ID of the "group" keyword, or zero if it's not defined.
		 */
		protected int groupTopicTypeID;

		}
	}