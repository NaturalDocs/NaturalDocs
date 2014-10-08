/* 
 * Class: GregValure.NaturalDocs.Engine.TopicTypes.Topics_txt
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <Topics.txt>.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *		
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
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Collections;


namespace GregValure.NaturalDocs.Engine.TopicTypes
	{
	public class Topics_txt
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Topics_txt
		 */
		public Topics_txt ()
			{
			yesRegex = new Regex.Config.Yes();
			noRegex = new Regex.Config.No();
			startRegex = new Regex.TopicTypes.ScopeStart();
			endRegex = new Regex.TopicTypes.ScopeEnd();

			nonASCIILettersRegex = new Regex.NonASCIILetters();

			ignoreKeywordsRegex = new Regex.TopicTypes.IgnoreKeywords();
			alterTopicTypeRegex = new Regex.TopicTypes.AlterTopicType();
			displayNameRegex = new Regex.TopicTypes.DisplayName();
			pluralDisplayNameRegex = new Regex.TopicTypes.PluralDisplayName();
			displayNameFromLocaleRegex = new Regex.TopicTypes.DisplayNameFromLocale();
			pluralDisplayNameFromLocaleRegex = new Regex.TopicTypes.PluralDisplayNameFromLocale();
			flagsRegex = new Regex.TopicTypes.Flags();
			documentationRegex = new Regex.TopicTypes.Documentation();
			variableTypeRegex = new Regex.TopicTypes.VariableType();
			classHierarchyRegex = new Regex.TopicTypes.ClassHierarchy();
			databaseHierarchyRegex = new Regex.TopicTypes.DatabaseHierarchy();
			enumRegex = new Regex.TopicTypes.Enum();
			breakListsRegex = new Regex.TopicTypes.BreakLists();
			keywordsRegex = new Regex.TopicTypes.Keywords();
			commaSeparatorRegex = new Regex.CondensedWhitespaceCommaSeparator();
			alwaysGlobalRegex = new Regex.TopicTypes.ScopeAlwaysGlobal();
			tagsRegex = new Regex.TopicTypes.Tags();
			}



		// Group: Loading Functions
		// __________________________________________________________________________


		/* Function: Load
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
		public bool Load (Path filename, out List<ConfigFileTopicType> fileTopicTypes, 
								 out List<string> fileIgnoredKeywords, out List<string> fileTags,
								 Errors.ErrorList errorList)
			{
			fileTopicTypes = new List<ConfigFileTopicType>();
			StringTable<ConfigFileTopicType> fileTopicTypeNames = 
				new StringTable<ConfigFileTopicType>(Engine.TopicTypes.Manager.KeySettingsForTopicTypes);
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
							{  NeedsTopicTypeError(file, identifier);  }
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
							{  NeedsTopicTypeError(file, identifier);  }
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
							{  NeedsTopicTypeError(file, identifier);  }
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
							{  NeedsTopicTypeError(file, identifier);  }
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
							{  NeedsTopicTypeError(file, identifier);  }
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
							{  NeedsTopicTypeError(file, identifier);  }
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
							{  NeedsTopicTypeError(file, identifier);  }
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
							{  NeedsTopicTypeError(file, identifier);  }
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
							{  NeedsTopicTypeError(file, identifier);  }
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
							{  NeedsTopicTypeError(file, identifier);  }
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
							{  NeedsTopicTypeError(file, identifier);  }
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
							{  NeedsTopicTypeError(file, identifier);  }
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
			

		/* Function: NeedsTopicTypeError
		 * A shortcut function only used by <Load()> which adds an error stating that the passed keyword needs to appear
		 * in a topic type section.
		 */
		private void NeedsTopicTypeError (ConfigFile file, string identifier)
			{
			file.AddError(
				Locale.Get("NaturalDocs.Engine", "Topics.txt.KeywordMustBeInTopicType(keyword)", identifier)
				);
			}



		// Group: Saving Functions
		// __________________________________________________________________________


		/* Function: Save
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
		public bool Save (Path filename, List<ConfigFileTopicType> topicTypes, 
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
				AppendKeywordList(output, ignoredKeywords, "   ");
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
					LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Display Name: " + topicType.DisplayName);
					}
				if (topicType.PluralDisplayName != null)
					{
					LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Plural Display Name: " + topicType.PluralDisplayName);
					}
				if (topicType.DisplayNameFromLocale != null)
					{
					LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Display Name from Locale: " + topicType.DisplayNameFromLocale);
					}
				if (topicType.PluralDisplayNameFromLocale != null)
					{
					LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Plural Display Name from Locale: " + topicType.PluralDisplayNameFromLocale);
					}
				if (topicType.SimpleIdentifier != null)
					{
					LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Simple Identifier: " + topicType.SimpleIdentifier);
					}
					
				if (topicType.Index != null)
					{
					LineBreakOnGroupChange(2, ref oldGroupNumber, output);

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
					LineBreakOnGroupChange(2, ref oldGroupNumber, output);

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
					LineBreakOnGroupChange(2, ref oldGroupNumber, output);

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
					LineBreakOnGroupChange(2, ref oldGroupNumber, output);

					output.Append("   Break Lists: ");
					
					if (topicType.BreakLists == true)
						{  output.AppendLine("Yes");  }
					else
						{  output.AppendLine("No");  }
					}
					
				if (topicType.Keywords.Count != 0)
					{
					LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					
					output.AppendLine("   Keywords:");
					AppendKeywordList(output, topicType.Keywords, "      ");
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
			
			
		/* Function: AppendKeywordList
		 * A function used only by <Save()> that adds a keyword list to the passed StringBuilder.
		 */
		private void AppendKeywordList (System.Text.StringBuilder output, List<string> keywords, string prefix)
			{
			for (int i = 0; i < keywords.Count; i += 2)
				{
				output.Append(prefix + keywords[i]);
				if (i + 1 < keywords.Count && keywords[i + 1] != null)
					{  output.Append(", " + keywords[i+1]);  }
				output.AppendLine();
				}
			}
			
		/* Function: LineBreakOnGroupChange
		 * A shortcut function used only by <Save()> which inserts a line break between groups.  It will also update 
		 * oldGroupNumber automatically.
		 */
		private void LineBreakOnGroupChange (int groupNumber, ref int oldGroupNumber, System.Text.StringBuilder output)
			{
			if (groupNumber != oldGroupNumber)
				{
				output.AppendLine();
				oldGroupNumber = groupNumber;
				}
			}



		// Group: Regular Expressions
		// __________________________________________________________________________

		protected Regex.Config.Yes yesRegex;
		protected Regex.Config.No noRegex;
		protected Regex.TopicTypes.ScopeStart startRegex;
		protected Regex.TopicTypes.ScopeEnd endRegex;

		protected Regex.NonASCIILetters nonASCIILettersRegex;

		protected Regex.TopicTypes.IgnoreKeywords ignoreKeywordsRegex;
		protected Regex.TopicTypes.AlterTopicType alterTopicTypeRegex;
		protected Regex.TopicTypes.DisplayName displayNameRegex;
		protected Regex.TopicTypes.PluralDisplayName pluralDisplayNameRegex;
		protected Regex.TopicTypes.DisplayNameFromLocale displayNameFromLocaleRegex;
		protected Regex.TopicTypes.PluralDisplayNameFromLocale pluralDisplayNameFromLocaleRegex;
		protected Regex.TopicTypes.Flags flagsRegex;
		protected Regex.TopicTypes.Documentation documentationRegex;
		protected Regex.TopicTypes.VariableType variableTypeRegex;
		protected Regex.TopicTypes.ClassHierarchy classHierarchyRegex;
		protected Regex.TopicTypes.DatabaseHierarchy databaseHierarchyRegex;
		protected Regex.TopicTypes.Enum enumRegex;
		protected Regex.TopicTypes.BreakLists breakListsRegex;
		protected Regex.TopicTypes.Keywords keywordsRegex;
		protected Regex.CondensedWhitespaceCommaSeparator commaSeparatorRegex;
		protected Regex.TopicTypes.ScopeAlwaysGlobal alwaysGlobalRegex;
		protected Regex.TopicTypes.Tags tagsRegex;


		// Group: Static Variables
		// __________________________________________________________________________

		/* var: BannedKeywordChars
		 * An array containing all the characters that cannot appear in keywords.  Best used with String.IndexOfAny().
		 */
		protected static char[] BannedKeywordChars = { '{', '}', ',', '#', ':' };

		 
		}
	}