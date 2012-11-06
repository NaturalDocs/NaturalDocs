/* 
 * Class: GregValure.NaturalDocs.Engine.TopicTypes.Manager
 * ____________________________________________________________________________
 * 
 * A module to handle <Topics.txt> and all the topic settings within Natural Docs.
 * 
 * 
 * Topic: Usage
 * 
 *		- The static functions <LoadFile()> and <SaveFile()> can be used right away, regardless of program state.
 * 
 *		- Call <Engine.Instance.Start()> which will start this module.
 *		  
 *		
 * 
 * Group: Files
 * ____________________________________________________________________________
 * 
 * File: Topics.txt
 * 
 *		The configuration file that defines or overrides the topic definitions for Natural Docs.  One version sits in Natural Docs'
 *		configuration folder, and another in the project configuration folder to add topics or override their behavior.
 *		
 *		These files follows the standard conventions in <ConfigFile>.  Identifier and value whitespace is condensed.  Y, N, True, 
 *		and False can be substituted for Yes and No.
 *		
 *		Sections:
 *		
 *			> Ignore[d] Keyword[s]: [keyword], [keyword] ...
 *			>    [keyword]
 *			>    [keyword], [keyword]
 *			>    ...
 *		
 *			Ignores the keywords so that they're not recognized as Natural Docs topics anymore.  Can be specified as a list on 
 *			the same line and/or following like a normal Keywords section.
 *			
 *			> Tag[s]: [tag], [tag] ...
 *			>    [tag]
 *			>    [tag]
 *			>    ...
 *			
 *			Defines tags that can be applied to topic types.
 *			
 *			> Topic Type: [name]
 *			> Alter Topic Type: [name]
 *			>
 *			> (synonyms)
 *			> Edit Topic Type: [name]
 *			> Change Topic Type: [name]
 *			
 *			Creates a new topic type or alters an existing one.  The name isn't case sensitive.
 *			
 *			The name Information is reserved.  There are a number of default types that must be defined in the system config file 
 *			but those may be different for each individual release and aren't listed here.  The default types can have their 
 *			keywords or behaviors changed, though, either by editing the system config file or by overriding them in the project
 *			config file.
 *			
 *			Enumeration is a special type.  It is indexed with Types and its definition list members are indexed with Constants 
 *			according to the rules in <Languages.txt>.
 *			
 * 
 *		Topic Type Sections:
 *		
 *			> Display Name: [name]
 *			> Plural Display Name: [name]
 *			>
 *			> (synonyms)
 *			> Name: [name]
 *			> Plural: [name]
 *			
 *			Specifies the singular and plural display names of the topic type.  If Display Name isn't defined, it defaults to the 
 *			topic type name.  If Plural Display Name isn't defined, it defaults to the Display Name.  These are available so 
 *			someone can rename one of the required types in the output since they can't change the topic type name.
 *			
 *			> Display Name from Locale: [identifier]
 *			> Plural Display Name from Locale: [identifier]
 *			>
 *			> (synonyms)
 *			> Name from Locale: [identifier]
 *			> Plural from Locale: [identifier]
 *			
 *			Specifies the singular and plural display names of the topic type using an identifier from a translation file in the 
 *			Engine module.  A topic type does not store both a normal and a "from locale" version, one overwrites the other.  
 *			This means that a project's configuration file can override the system's "from locale" version with a regular version.
 *			
 *			> Simple Identifier: [name]
 *			
 *			Specifies the topic type name using only the letters A to Z.  No spaces, numbers, symbols, or Unicode
 *			allowed.  This is for use in situations when such things may not be allowed, such as when generating CSS class names.
 *			If it's not specified, it defaults to the topic type name stripped of all unacceptable characters.
 *			
 *			> Index: [yes|no|with [topic type]]
 *			> Index With: [topic type]
 *			
 *			Whether the topic type is indexed.  Defaults to yes.  If "with [topic type]" is specified, the type is indexed but only
 *			as part of the other topic type's index.
 *			
 *			> Scope: [normal|start|end|always global]
 *			
 *			How the topic affects scope.  Defaults to normal.
 *			
 *			normal - The topic stays within the current scope.
 *			start - The topic starts a new scope for all the topics beneath it, like class topics.
 *			end - The topic resets the scope back to global for all the topics beneath it, like section topics.
 *			always global - The topic is defined as a global symbol, but does not change the scope for any other topics.
 *			
 *			> Flags: [flag], [flag], ...
 *			
 *			Various flags that can be applied to the topic type.
 *			
 *			Code, File, Documentation - Whether it's used to describe a code element, a file, or is a standalone documentation 
 *																  comment.  Defaults to Code.
 *			Variable Type - Whether it describes a code element that can be used as a variable's type.
 *			Class Hierarchy, Database Hierarchy - Whether it describes a code element that should be included in the class or 
 *																						database hierarchy.  Requires Scope: Start.
 *			Enum - Whether it describes an enum.
 *			
 *			> Break List[s]: [yes|no]
 *			
 *			Whether list topics should be broken into individual topics in the output.  Defaults to no.
 *			
 *			> [Add] Keyword[s]:
 *			>    [keyword]
 *			>    [keyword], [plural keyword]
 *			>    ...
 *			
 *			A list of the topic type's keywords.  Each line after the heading is the keyword and optionally its plural form.  This 
 *			continues until the next line in "keyword: value" format.  "Add" isn't required.
 *			
 *			- Keywords cannot contain colons, commas, braces, or #.
 *			- Keywords are not case sensitive.
 *			- Subsequent keyword sections add to the list.  They don't replace it.
 *			- Keywords can be redefined by other keyword sections.
 *			
 * 
 *		Deprecated:
 *		
 *			These are no longer supported but are listed here as a reference for parsing earlier verisons of the file.
 *			
 *			> Can Group With: [topic type], [topic type], ...
 *			
 *			The list of topic types the topic can possibly be grouped with.
 *			
 *			> Page Title if First: [yes|no]
 *			
 *			Whether the title of this topic becomes the page title if it is the first topic in a file.  Defaults to no.
 *			
 *			> Class Hierarchy: [yes|no]
 *			
 *			No longer its own setting, this will be converted into the Flags value.
 *			
 * 
 *		Revisions:
 * 
 *		2.0:
 *		
 *			- Added Display Name, Plural Display Name, synonyms, and their "from Locale" variants.
 *			- Added Simple Identifier and Flags.
 *			- All values now support Unicode characters, except for Simple Identifier.
 *			- Can Group With and Page Title if First are deprecated.
 *			- Class Hierarchy is deprecated but will be converted into Flags.
 *			- Added "with [topic type]" value to Index property.
 *			- Replaced "Generic" as the default topic type with "Information".
 *			- Added Tags.
 *			
 *		1.3:
 *		
 *			The initial version of this file.
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

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
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
		
		
		/* Constants: Tolerance Constants
		 * 
		 * KeywordsIgnoreCase - Whether keywords ignore case.
		 * KeywordsAreNormalized - Whether Unicode normalization is applied to keywords.
		 * TopicTypeNamesIgnoreCase - Whether topic type names ignore case.
		 * TopicTypeNamesAreNormalized - Whether Unicode normalization is applied to topic type names.
		 * TagsIgnoreCase - Whether tags ignore case.
		 * TagsAreNormalized - Whether Unicode normalization is applied to tags.
		 */
		public const bool KeywordsIgnoreCase = true;
		public const bool KeywordsAreNormalized = true;
		public const bool TopicTypeNamesIgnoreCase = true;
		public const bool TopicTypeNamesAreNormalized = true;
		public const bool TagsIgnoreCase = true;
		public const bool TagsAreNormalized = true;
		
		
		
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Manager
		 */
		public Manager ()
			{
			// Topic type names aren't normalized because they're only referenced in other config files.  Tags and keywords are
			// referenced in source files so they should be more tolerant.
			topicTypes = new IDObjects.Manager<TopicType>(TopicTypeNamesIgnoreCase, TopicTypeNamesAreNormalized, false);
			tags = new IDObjects.Manager<Tag>(TagsIgnoreCase, TagsAreNormalized, false);
			
			singularKeywords = new StringTable<TopicType>(KeywordsIgnoreCase, KeywordsAreNormalized);
			pluralKeywords = new StringTable<TopicType>(KeywordsIgnoreCase, KeywordsAreNormalized);
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

			
			// Load the files.
			
			if (!LoadFile( systemFile, out systemTopicTypeList, out ignoredSystemKeywords, out systemTags, errorList ))
				{  
				success = false;  
				// Continue anyway because we want to show errors from both files.
				}
			
			if (System.IO.File.Exists(projectFile))
				{
				if (!LoadFile ( projectFile, out projectTopicTypeList, out ignoredProjectKeywords, out projectTags, errorList ))
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
			
			StringSet ignoredKeywords = new StringSet(KeywordsIgnoreCase, KeywordsAreNormalized);
			
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
			
			if (!SaveFile(projectFile, projectTopicTypeList, ignoredProjectKeywords, projectTags, errorList, true, false))
				{  success = false;  };
				
			if (!SaveFile(systemFile, systemTopicTypeList, ignoredSystemKeywords, systemTags, errorList, false, true))
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
		
		
		/* Function: LoadFile
		 * 
		 * Loads the passed configuration file and parses it.  Redundant information will be simplified out, such as an Alter Topic 
		 * Type section that applies to a Topic Type defined in the same file.
		 * 
		 * Parameters:
		 * 
		 *		filename - The <Path> where the file is located.
		 *		fileTopicTypes - Returns a list of <ConfigFileTopicTypes> in no particular order.
		 *		fileIgnoredKeywords - Returns any ignored keywords as a string array in the format of 
		 *										<ConfigFileTopicType.Keywords>.
		 *		fileTags - Returns any defined tags as a string array.
		 *		errorList - If it couldn't successfully parse the file it will add error messages to this list.
		 *		
		 * Returns:
		 * 
		 *		Whether it was able to successfully load and parse the file without any errors.
		 */
		public static bool LoadFile (Path filename, out List<ConfigFileTopicType> fileTopicTypes, 
											  out List<string> fileIgnoredKeywords, out List<string> fileTags,
											  Errors.ErrorList errorList)
			{
			fileTopicTypes = new List<ConfigFileTopicType>();
			StringTable<ConfigFileTopicType> fileTopicTypeNames = 
				new StringTable<ConfigFileTopicType>(TopicTypeNamesIgnoreCase, TopicTypeNamesAreNormalized);
			fileIgnoredKeywords = new List<string>();
			fileTags = new List<string>();
			int previousErrorCount = errorList.Count;

			using (ConfigFile file = new ConfigFile())
				{
				bool openResult = file.Open(filename, 
														 ConfigFile.FileFormatFlags.CondenseIdentifierWhitespace |
														 ConfigFile.FileFormatFlags.CondenseValueWhitespace | 
														 ConfigFile.FileFormatFlags.SupportsNullValueLines | 
														 ConfigFile.FileFormatFlags.SupportsRawValueLines |
														 ConfigFile.FileFormatFlags.MakeIdentifiersLowercase, 
														 errorList);
														 
				if (openResult == false)
					{  return false;  }
					
				string identifier, value;
				ConfigFileTopicType currentTopicType = null;
				List<string> currentKeywordList = null;
				bool inTags = false;
				
				var ignoreKeywordsRegex = new Regex.TopicTypes.IgnoreKeywords();
				var alterTopicTypeRegex = new Regex.TopicTypes.AlterTopicType();
				var displayNameRegex = new Regex.TopicTypes.DisplayName();
				var pluralDisplayNameRegex = new Regex.TopicTypes.PluralDisplayName();
				var displayNameFromLocaleRegex = new Regex.TopicTypes.DisplayNameFromLocale();
				var pluralDisplayNameFromLocaleRegex = new Regex.TopicTypes.PluralDisplayNameFromLocale();
				var flagsRegex = new Regex.TopicTypes.Flags();
				var documentationRegex = new Regex.TopicTypes.Documentation();
				var variableTypeRegex = new Regex.TopicTypes.VariableType();
				var classHierarchyRegex = new Regex.TopicTypes.ClassHierarchy();
				var databaseHierarchyRegex = new Regex.TopicTypes.DatabaseHierarchy();
				var enumRegex = new Regex.TopicTypes.Enum();
				var breakListsRegex = new Regex.TopicTypes.BreakLists();
				var keywordsRegex = new Regex.TopicTypes.Keywords();
				var commaSeparatorRegex = new Regex.CondensedWhitespaceCommaSeparator();
				var yesRegex = new Regex.Config.Yes();
				var noRegex = new Regex.Config.No();
				var startRegex = new Regex.TopicTypes.ScopeStart();
				var endRegex = new Regex.TopicTypes.ScopeEnd();
				var alwaysGlobalRegex = new Regex.TopicTypes.ScopeAlwaysGlobal();
				var nonASCIILettersRegex = new Regex.NonASCIILetters();
				var tagsRegex = new Regex.TopicTypes.Tags();

				while (file.Get(out identifier, out value))
					{

					//
					// Identifierless lines
					//
					
					if (identifier == null)
						{
						
						// Keywords or Ignored Keywords
						
						if (currentKeywordList != null)
							{
							string[] keywordsArray = commaSeparatorRegex.Split(value);
							
							if (keywordsArray.Length > 2)
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Topics.txt.NoMoreThanTwoKeywordsOnALine")
									);
								}
								
							for (int i = 0; i < keywordsArray.Length; i++)
								{
								int bannedChar = keywordsArray[i].IndexOfAny(BannedKeywordChars);
								if (bannedChar != -1)
									{
									file.AddError( 
										Locale.Get("NaturalDocs.Engine", "Topics.txt.KeywordsCannotContain(char)", keywordsArray[i][bannedChar]) 
										);
									// Continue parsing
									}
									
								// Validate all of them, only add the first two.
								else if (i < 2)
									{
									currentKeywordList.Add(keywordsArray[i]);
									}
								}
								
							// Add a null if there was only one.
							if (keywordsArray.Length == 1)
								{
								currentKeywordList.Add(null);
								}
							}
						
						
						// Tags
							
						else if (inTags)
							{
							if (value.IndexOf(',') != -1)
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Topics.txt.NoMoreThanOneTagOnALine")
									);
								}
								
							int bannedChar = value.IndexOfAny(BannedKeywordChars);
							if (bannedChar != -1)
								{
								file.AddError( 
									Locale.Get("NaturalDocs.Engine", "Topics.txt.TagsCannotContain(char)", value[bannedChar]) 
									);
								// Continue parsing
								}
							else
								{  fileTags.Add(value);  }
							}
							
							
						// Raw line
						
						else
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "ConfigFile.LineNotInIdentifierValueFormat")
								);
							}
							
						continue;
						}
						
					else
						{
						currentKeywordList = null;
						inTags = false;
						}
						

					//
					// Ignore Keywords
					//
					
					if (ignoreKeywordsRegex.IsMatch(identifier))
						{
						currentTopicType = null;
						currentKeywordList = fileIgnoredKeywords;
						
						if (!string.IsNullOrEmpty(value))
							{
							string[] ignoredKeywordsArray = commaSeparatorRegex.Split(value);
							
							foreach (string ignoredKeyword in ignoredKeywordsArray)
								{
								int bannedChar = ignoredKeyword.IndexOfAny(BannedKeywordChars);
								if (bannedChar != -1)
									{
									file.AddError( 
										Locale.Get("NaturalDocs.Engine", "Topics.txt.KeywordsCannotContain(char)", ignoredKeyword[bannedChar]) 
										);
									// Continue parsing
									}
								else
									{
									fileIgnoredKeywords.Add(ignoredKeyword);
									fileIgnoredKeywords.Add(null);
									}
								}
							}
						}
					
					
					//
					// Tags
					//
					
					else if (tagsRegex.IsMatch(identifier))
						{
						currentTopicType = null;
						inTags = true;
						
						if (!string.IsNullOrEmpty(value))
							{
							string[] tagsArray = commaSeparatorRegex.Split(value);
							
							foreach (string tag in tagsArray)
								{
								int bannedChar = tag.IndexOfAny(BannedKeywordChars);
								if (bannedChar != -1)
									{
									file.AddError( 
										Locale.Get("NaturalDocs.Engine", "Topics.txt.TagsCannotContain(char)", tag[bannedChar]) 
										);
									// Continue parsing
									}
								else
									{  fileTags.Add(tag);  }
								}
							}
						}
					
					
					//
					// Topic Type
					//
						
					else if (identifier == "topic type")
						{
						if (fileTopicTypeNames.ContainsKey(value))
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Topics.txt.TopicTypeAlreadyExists(name)", value)
								);
							
							// Continue parsing.  We'll throw this into the existing type even though it shouldn't be overwriting
							// its values because we want to find any other errors there are in the file.
							currentTopicType = fileTopicTypeNames[value];
							}
							
						else
							{
							// There is no 1.5, but this covers all the 2.0 prereleases.
							if (file.Version < "1.5" && String.Compare(value, "generic", true) == 0)
								{  value = "Information";  }
								
							currentTopicType = new ConfigFileTopicType(value, false, file.LineNumber);
							fileTopicTypes.Add(currentTopicType);
							fileTopicTypeNames.Add(value, currentTopicType);
							}								
						}
						
						
					//
					// Alter Topic Type
					//
					
					else if (alterTopicTypeRegex.IsMatch(identifier))
						{
						// If this type already exists, collapse it into the current definition.
						if (fileTopicTypeNames.ContainsKey(value))
							{
							currentTopicType = fileTopicTypeNames[value];
							}
							
						// If it doesn't exist, create the new type anyway with the alter flag set because it may exist in another
						// file.
						else
							{
							currentTopicType = new ConfigFileTopicType(value, true, file.LineNumber);
							fileTopicTypes.Add(currentTopicType);
							fileTopicTypeNames.Add(value, currentTopicType);
							}								
						}


					//
					// (Plural) Display Name (From Locale)
					//
						
					else if (displayNameRegex.IsMatch(identifier))
						{
						if (currentTopicType == null)
							{  LoadFile_NeedsTopicTypeError(file, identifier);  }
						else if (currentTopicType.DisplayNameFromLocale != null)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Topics.txt.CannotDefineXWhenYIsDefined(x,y)", "Display Name", "Display Name from Locale")
								);
							}
						else
							{						
							currentTopicType.DisplayName = value;
							}
						}
					else if (pluralDisplayNameRegex.IsMatch(identifier))
						{
						if (currentTopicType == null)
							{  LoadFile_NeedsTopicTypeError(file, identifier);  }
						else if (currentTopicType.PluralDisplayNameFromLocale != null)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Topics.txt.CannotDefineXWhenYIsDefined(x,y)", "Plural Display Name", "Plural Display Name from Locale")
								);
							}
						else
							{						
							currentTopicType.PluralDisplayName = value;
							}
						}
					else if (displayNameFromLocaleRegex.IsMatch(identifier))
						{
						if (currentTopicType == null)
							{  LoadFile_NeedsTopicTypeError(file, identifier);  }
						else if (currentTopicType.DisplayName != null)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Topics.txt.CannotDefineXWhenYIsDefined(x,y)", "Display Name from Locale", "Display Name")
								);
							}
						else
							{						
							currentTopicType.DisplayNameFromLocale = value;
							}
						}
					else if (pluralDisplayNameFromLocaleRegex.IsMatch(identifier))
						{
						if (currentTopicType == null)
							{  LoadFile_NeedsTopicTypeError(file, identifier);  }
						else if (currentTopicType.PluralDisplayName != null)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Topics.txt.CannotDefineXWhenYIsDefined(x,y)", "Plural Display Name from Locale", "Plural Display Name")
								);
							}
						else
							{						
							currentTopicType.PluralDisplayNameFromLocale = value;
							}
						}
						
						
					//
					// Simple Identifier
					//
						
					else if (identifier == "simple identifier")
						{
						if (currentTopicType == null)
							{  LoadFile_NeedsTopicTypeError(file, identifier);  }
						else if (nonASCIILettersRegex.IsMatch(value))
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Topics.txt.SimpleIdentifierMustOnlyBeASCIILetters(name)", value)
								);
							}
						else
							{						
							currentTopicType.SimpleIdentifier = value;
							}
						}
						
						
					//
					// Index
					//
						
					else if (identifier == "index")
						{
						if (currentTopicType != null)
							{
							value = value.ToLower();
							
							if (yesRegex.IsMatch(value))
								{
								currentTopicType.Index = TopicType.IndexValue.Yes;
								}
							else if (noRegex.IsMatch(value))
								{
								currentTopicType.Index = TopicType.IndexValue.No;
								}
							else if (value.StartsWith("with "))
								{
								currentTopicType.Index = TopicType.IndexValue.IndexWith;
								
								// We hold off on validating this because it may be defined later in the file or in another file.
								currentTopicType.IndexWith = value.Substring(5);
								}
							else
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Topics.txt.UnrecognizedValue(keyword, value)", "Index", value)
									);
								}
							}
						else
							{  LoadFile_NeedsTopicTypeError(file, identifier);  }
						}
						
					else if (identifier == "index with")
						{
						if (currentTopicType != null)
							{
							currentTopicType.Index = TopicType.IndexValue.IndexWith;
							
							// We hold off on validating this because it may be defined later in the file or in another file.
							currentTopicType.IndexWith = value;
							}
						else
							{  LoadFile_NeedsTopicTypeError(file, identifier);  }
						}
						
						
					//
					// Scope
					//
					
					else if (identifier == "scope")
						{
						if (currentTopicType != null)
							{
							value = value.ToLower();
							
							if (value == "normal")
								{
								currentTopicType.Scope = TopicType.ScopeValue.Normal;
								}
							else if (startRegex.IsMatch(value))
								{
								currentTopicType.Scope = TopicType.ScopeValue.Start;
								}
							else if (endRegex.IsMatch(value))
								{
								currentTopicType.Scope = TopicType.ScopeValue.End;
								}
							else if (alwaysGlobalRegex.IsMatch(value))
								{
								currentTopicType.Scope = TopicType.ScopeValue.AlwaysGlobal;
								}
							else
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Topics.txt.UnrecognizedValue(keyword, value)", "Scope", value)
									);
								}
							}
						else
							{  LoadFile_NeedsTopicTypeError(file, identifier);  }
						}
						
						
					//
					// Flags
					//
					
					else if (flagsRegex.IsMatch(identifier))
						{
						if (currentTopicType != null)
							{
							value = value.ToLower();
							
							if (!string.IsNullOrEmpty(value))
								{
								string[] flagsArray = commaSeparatorRegex.Split(value);
							
								foreach (string flag in flagsArray)
									{
									if (flag == "code")
										{  currentTopicType.Flags.Code = true;  }
									else if (flag == "file")
										{  currentTopicType.Flags.File = true;  }
									else if (documentationRegex.IsMatch(flag))
										{  currentTopicType.Flags.Documentation = true;  }
									else if (variableTypeRegex.IsMatch(flag))
										{  currentTopicType.Flags.VariableType = true;  }
									else if (classHierarchyRegex.IsMatch(flag))
										{  currentTopicType.Flags.ClassHierarchy = true;  }
									else if (databaseHierarchyRegex.IsMatch(flag))
										{  currentTopicType.Flags.DatabaseHierarchy = true;  }
									else if (enumRegex.IsMatch(flag))
										{  currentTopicType.Flags.Enum = true;  }
									else if (string.IsNullOrEmpty(flag) == false)
										{  
										file.AddError(
											Locale.Get("NaturalDocs.Engine", "Topics.txt.UnrecognizedValue(keyword, value)", "Flags", flag)
											);
										}
									}

								List<string> flagErrors = currentTopicType.Flags.Validate(false, currentTopicType.Scope, "NaturalDocs.Engine");

								if (flagErrors != null)
									{
									foreach (string flagError in flagErrors)
										{  file.AddError(flagError);  }
									}
								}
							}
						else
							{  LoadFile_NeedsTopicTypeError(file, identifier);  }
						}
					
					
					// 
					// Class Hierarchy (deprecated, convert to flag)
					//
					
					else if (classHierarchyRegex.IsMatch(identifier))
						{
						if (currentTopicType != null)
							{
							value = value.ToLower();
							
							if (yesRegex.IsMatch(value))
								{
								currentTopicType.Flags.ClassHierarchy = true;
								}
							else if (noRegex.IsMatch(value))
								{
								currentTopicType.Flags.ClassHierarchy = false;
								}
							else
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Topics.txt.UnrecognizedValue(keyword, value)", "Class Hierarchy", value)
									);
								}
							}
						else
							{  LoadFile_NeedsTopicTypeError(file, identifier);  }
						}
						
						
					// 
					// Break Lists
					//
					
					else if (breakListsRegex.IsMatch(identifier))
						{
						if (currentTopicType != null)
							{
							value = value.ToLower();
							
							if (yesRegex.IsMatch(value))
								{
								currentTopicType.BreakLists = true;
								}
							else if (noRegex.IsMatch(value))
								{
								currentTopicType.BreakLists = false;
								}
							else
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Topics.txt.UnrecognizedValue(keyword, value)", "Break Lists", value)
									);
								}
							}
						else
							{  LoadFile_NeedsTopicTypeError(file, identifier);  }
						}
						
						
					//
					// Keywords
					//

					else if (keywordsRegex.IsMatch(identifier))
						{
						if (currentTopicType != null)
							{
							currentKeywordList = currentTopicType.Keywords;
							
							if (!string.IsNullOrEmpty(value))
								{
								string[] keywordsArray = commaSeparatorRegex.Split(value);
								
								foreach (string keyword in keywordsArray)
									{
									int bannedChar = keyword.IndexOfAny(BannedKeywordChars);
									if (bannedChar != -1)
										{
										file.AddError( 
											Locale.Get("NaturalDocs.Engine", "Topics.txt.KeywordsCannotContain(char)", keyword[bannedChar]) 
											);
										// Continue parsing
										}
									else
										{
										currentKeywordList.Add(keyword);
										currentKeywordList.Add(null);
										}
									}
								}
							}
						else
							{  LoadFile_NeedsTopicTypeError(file, identifier);  }
						}
						
						
					//
					// Deprecated keywords: Can Group With, Page Title if First
					//
					
					else if (identifier == "can group with" || identifier == "page title if first")
						{
						// Ignore and continue
						}


					//
					// Unrecognized keywords
					//
					
					else
						{
						file.AddError(
							Locale.Get("NaturalDocs.Engine", "Topics.txt.UnrecognizedKeyword(keyword)", identifier)
							);
						}
										
					}  // while (file.Get)

				file.Close();				
				}


			// Do a strict validation on all topic types that defined flags.

			foreach (var topicType in fileTopicTypes)
				{
				if (topicType.Flags.AllConfigurationProperties != 0)
					{
					// We don't want to duplicate errors we already found, so only do this if it passes the non-strict test.
					List<string> flagErrors = topicType.Flags.Validate(false, topicType.Scope, "NaturalDocs.Engine");

					if (flagErrors == null)
						{
						topicType.Flags.AddImpliedFlags();
						flagErrors = topicType.Flags.Validate(true, topicType.Scope, "NaturalDocs.Engine");

						if (flagErrors != null)
							{
							foreach (string flagError in flagErrors)
								{  errorList.Add(flagError, filename, topicType.LineNumber);  }
							}
						}
					}
				}
				
				
			if (errorList.Count == previousErrorCount)
				{  return true;  }
			else
				{  return false;  }
			}
			

		/* Function: LoadFile_NeedsTopicTypeError
		 * A shortcut function only used by <LoadFile()> which adds an error stating that the passed keyword needs to appear
		 * in a topic type section.
		 */
		private static void LoadFile_NeedsTopicTypeError (ConfigFile file, string identifier)
			{
			file.AddError(
				Locale.Get("NaturalDocs.Engine", "Topics.txt.KeywordMustBeInTopicType(keyword)", identifier)
				);
			}
			
			
		/* Function: SaveFile
		 * 
		 * Saves the passed information into a configuration file if it's different from the one on disk.
		 * 
		 * Parameters:
		 * 
		 *		filename - The <Path> where the file is located.
		 *		topicTypes - A list of <ConfigFileTopicTypes>.  
		 *		ignoredKeywords - A string array of ignored keywords in the format of <ConfigFileTopicType.Keywords>.
		 *		tags - A list of defined tags.
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
		public static bool SaveFile (Path filename, List<ConfigFileTopicType> topicTypes, 
											  List<string> ignoredKeywords, List<string> tags,
											  Errors.ErrorList errorList, bool isProjectFile, bool noErrorOnFail)
			{
			System.Text.StringBuilder output = new System.Text.StringBuilder(1024);
			string projectSystem = (isProjectFile ? "Project" : "System");
			
			
			// Header
			
			output.AppendLine("Format: " + Engine.Instance.VersionString);
			output.AppendLine();
			output.Append( Locale.Get("NaturalDocs.Engine", "Topics.txt." + projectSystem + "Header.multiline") );
			output.AppendLine();
			output.AppendLine();
			
			
			// Ignored Keywords
			
			if (ignoredKeywords.Count > 0)
				{
				output.Append( Locale.Get("NaturalDocs.Engine", "Topics.txt.IgnoredKeywordsHeader.multiline") );
				output.AppendLine();
				output.AppendLine("Ignore Keywords:");
				SaveFile_AppendKeywordList(output, ignoredKeywords, "   ");
				output.AppendLine();
				output.AppendLine();
				}
			else if (isProjectFile)
				{
				output.Append( Locale.Get("NaturalDocs.Engine", "Topics.txt.IgnoredKeywordsHeader.multiline") );
				output.AppendLine();
				output.Append( Locale.Get("NaturalDocs.Engine", "Topics.txt.IgnoredKeywordsReference.multiline") );
				output.AppendLine();
				output.AppendLine();
				}
			// Add nothing for the system config file.
			
			
			// Tags
			
			output.Append( Locale.Get("NaturalDocs.Engine", "Topics.txt.TagsHeader.multiline") );
			output.AppendLine();

			if (tags.Count > 0)
				{
				output.AppendLine("Tags:");
				foreach (string tag in tags)
					{  output.AppendLine("   " + tag);  }
				}
			else
				{
				output.Append( Locale.Get("NaturalDocs.Engine", "Topics.txt.TagsReference.multiline") );
				}

			output.AppendLine();
			output.AppendLine();
				
				
			// Topic Types
			
			output.Append( Locale.Get("NaturalDocs.Engine", "Topics.txt.TopicTypesHeader.multiline") );

			if (topicTypes.Count > 1)
				{  output.Append( Locale.Get("NaturalDocs.Engine", "Topics.txt.DeferredTopicTypesReference.multiline") );  }
				
			output.AppendLine();

			
			//
			// Create content
			//
			
			topicTypes.Sort( 
				delegate (ConfigFileTopicType a, ConfigFileTopicType b)
					{  return a.LineNumber - b.LineNumber;  } 
				);
				
			foreach (ConfigFileTopicType topicType in topicTypes)
				{
				if (isProjectFile && topicType.AlterType == true)
					{  output.Append("Alter ");  }
					
				output.AppendLine("Topic Type: " + topicType.Name);
				int oldGroupNumber = 0;
				
				if (topicType.DisplayName != null)
					{  
					SaveFile_LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Display Name: " + topicType.DisplayName);
					}
				if (topicType.PluralDisplayName != null)
					{
					SaveFile_LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Plural Display Name: " + topicType.PluralDisplayName);
					}
				if (topicType.DisplayNameFromLocale != null)
					{
					SaveFile_LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Display Name from Locale: " + topicType.DisplayNameFromLocale);
					}
				if (topicType.PluralDisplayNameFromLocale != null)
					{
					SaveFile_LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Plural Display Name from Locale: " + topicType.PluralDisplayNameFromLocale);
					}
				if (topicType.SimpleIdentifier != null)
					{
					SaveFile_LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Simple Identifier: " + topicType.SimpleIdentifier);
					}
					
				if (topicType.Index != null)
					{
					SaveFile_LineBreakOnGroupChange(2, ref oldGroupNumber, output);

					output.Append("   Index: ");
					
					if (topicType.Index == TopicType.IndexValue.Yes)
						{  output.AppendLine("Yes");  }
					else if (topicType.Index == TopicType.IndexValue.No)
						{  output.AppendLine("No");  }
					else  // IndexWith
						{  output.AppendLine("with " + topicType.IndexWith);  }
					}
					
				if (topicType.Scope != null)
					{
					SaveFile_LineBreakOnGroupChange(2, ref oldGroupNumber, output);

					output.Append("   Scope: ");
					
					if (topicType.Scope == TopicType.ScopeValue.Normal)
						{  output.AppendLine("Normal");  }
					else if (topicType.Scope == TopicType.ScopeValue.Start)
						{  output.AppendLine("Start");  }
					else if (topicType.Scope == TopicType.ScopeValue.End)
						{  output.AppendLine("End");  }
					else  // AlwaysGlobal
						{  output.AppendLine("Always Global");  }
					}
					
				if (topicType.Flags.AllConfigurationProperties != 0)
					{
					SaveFile_LineBreakOnGroupChange(2, ref oldGroupNumber, output);

					output.Append("   Flags: ");
					
					if (topicType.Flags.Code)
						{  output.Append("Code");  }
					else if (topicType.Flags.File)
						{  output.Append("File");  }
					else if (topicType.Flags.Documentation)
						{  output.Append("Documentation");  }

					if (topicType.Flags.VariableType)
						{  output.Append(", Variable Type");  }

					if (topicType.Flags.ClassHierarchy)
						{  output.Append(", Class Hierarchy");  }
					else if (topicType.Flags.DatabaseHierarchy)
						{  output.Append(", Database Hierarchy");  }

					if (topicType.Flags.Enum)
						{  output.Append(", Enum");  }

					output.AppendLine();
					}
					
				if (topicType.BreakLists != null)
					{
					SaveFile_LineBreakOnGroupChange(2, ref oldGroupNumber, output);

					output.Append("   Break Lists: ");
					
					if (topicType.BreakLists == true)
						{  output.AppendLine("Yes");  }
					else
						{  output.AppendLine("No");  }
					}
					
				if (topicType.Keywords.Count != 0)
					{
					SaveFile_LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					
					output.AppendLine("   Keywords:");
					SaveFile_AppendKeywordList(output, topicType.Keywords, "      ");
					}

				output.AppendLine();
				output.AppendLine();
				}

			output.Append( Locale.Get("NaturalDocs.Engine", "Topics.txt." + projectSystem + "TopicTypesReference.multiline") );
				
				
			//
			// Compare with previous file and write to disk
			//
			
			return ConfigFile.SaveIfDifferent(filename, output.ToString(), noErrorOnFail, errorList);
			}
			
			
		/* Function: SaveFile_AppendKeywordList
		 * A function used only by <SaveFile()> that adds a keyword list to the passed StringBuilder.
		 */
		private static void SaveFile_AppendKeywordList (System.Text.StringBuilder output, List<string> keywords, string prefix)
			{
			for (int i = 0; i < keywords.Count; i += 2)
				{
				output.Append(prefix + keywords[i]);
				if (i + 1 < keywords.Count && keywords[i + 1] != null)
					{  output.Append(", " + keywords[i+1]);  }
				output.AppendLine();
				}
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
			

		
		// Group: Public Static Variables
		// __________________________________________________________________________
		

		/* var: BannedKeywordChars
		 * An array containing all the characters that cannot appear in keywords.  Best used with String.IndexOfAny().
		 */
		public static char[] BannedKeywordChars = { '{', '}', ',', '#', ':' };
		

	
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
		}
	}