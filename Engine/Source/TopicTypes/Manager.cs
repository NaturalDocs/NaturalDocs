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

			Topics_nd topicsNDParser = new Topics_nd();


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
				
			else if (topicsNDParser.Load(Engine.Instance.Config.WorkingDataFolder + "/Topics.nd", out binaryTopicTypes, out binaryTags, 
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

				
			topicsNDParser.Save(Engine.Instance.Config.WorkingDataFolder + "/Topics.nd", 
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