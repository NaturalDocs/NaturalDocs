/* 
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.Topics_txt
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
 *		The configuration file that defines or overrides the comment type definitions for Natural Docs.  One version sits in Natural Docs'
 *		configuration folder, and another in the project configuration folder to add comment types or override their behavior.
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
 *			Ignores the keywords so that they're not recognized as Natural Docs comments anymore.  Can be specified as a list on 
 *			the same line and/or following like a normal Keywords section.
 *			
 *			> Tag[s]: [tag], [tag] ...
 *			>    [tag]
 *			>    [tag]
 *			>    ...
 *			
 *			Defines tags that can be applied to comment types.
 *			
 *			> Topic Type: [name]
 *			> Alter Topic Type: [name]
 *			>
 *			> (synonyms)
 *			> Edit Topic Type: [name]
 *			> Change Topic Type: [name]
 *			
 *			Creates a new comment type or alters an existing one.  The name isn't case sensitive.
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
 *		Comment Type Sections:
 *		
 *			> Display Name: [name]
 *			> Plural Display Name: [name]
 *			>
 *			> (synonyms)
 *			> Name: [name]
 *			> Plural: [name]
 *			
 *			Specifies the singular and plural display names of the comment type.  If Display Name isn't defined, it defaults to the 
 *			comment type name.  If Plural Display Name isn't defined, it defaults to the Display Name.  These are available so 
 *			someone can rename one of the required types in the output since they can't change the comment type name.
 *			
 *			> Display Name from Locale: [identifier]
 *			> Plural Display Name from Locale: [identifier]
 *			>
 *			> (synonyms)
 *			> Name from Locale: [identifier]
 *			> Plural from Locale: [identifier]
 *			
 *			Specifies the singular and plural display names of the comment type using an identifier from a translation file in the 
 *			Engine module.  A comment type does not store both a normal and a "from locale" version, one overwrites the other.  
 *			This means that a project's configuration file can override the system's "from locale" version with a regular version.
 *			
 *			> Simple Identifier: [name]
 *			
 *			Specifies the comment type name using only the letters A to Z.  No spaces, numbers, symbols, or Unicode
 *			allowed.  This is for use in situations when such things may not be allowed, such as when generating CSS class names.
 *			If it's not specified, it defaults to the comment type name stripped of all unacceptable characters.
 *			
 *			> Index: [yes|no|with [comment type]]
 *			> Index With: [comment type]
 *			
 *			Whether the comment type is indexed.  Defaults to yes.  If "with [comment type]" is specified, the type is indexed but only
 *			as part of the other comment type's index.
 *			
 *			> Scope: [normal|start|end|always global]
 *			
 *			How the comment affects scope.  Defaults to normal.
 *			
 *			normal - The comment stays within the current scope.
 *			start - The comment starts a new scope for all the comments beneath it, like class comments.
 *			end - The comment resets the scope back to global for all the comments beneath it, like section comments.
 *			always global - The comment is defined as a global symbol, but does not change the scope for any other comments.
 *			
 *			> Flags: [flag], [flag], ...
 *			
 *			Various flags that can be applied to the comment type.
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
 *			Whether list comments should be broken into individual topics in the output.  Defaults to no.
 *			
 *			> [Add] Keyword[s]:
 *			>    [keyword]
 *			>    [keyword], [plural keyword]
 *			>    ...
 *			
 *			A list of the comment type's keywords.  Each line after the heading is the keyword and optionally its plural form.  This 
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
 *			> Can Group With: [comment type], [comment type], ...
 *			
 *			The list of comment types the comment can possibly be grouped with.
 *			
 *			> Page Title if First: [yes|no]
 *			
 *			Whether the title of this comment becomes the page title if it is the first comment in a file.  Defaults to no.
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
 *			- Added "with [comment type]" value to Index property.
 *			- Replaced "Generic" as the default comment type with "Information".
 *			- Added Tags.
 *			
 *		1.3:
 *		
 *			The initial version of this file.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.CommentTypes
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
			alterCommentTypeRegex = new Regex.TopicTypes.AlterTopicType();
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
		 * Loads the passed configuration file and parses it.  Redundant information will be simplified out, such as an Alter Comment 
		 * Type section that applies to a comment type defined in the same file.
		 * 
		 * Parameters:
		 * 
		 *		filename - The <Path> where the file is located.
		 *		fileCommentTypes - Returns a list of <ConfigFileCommentTypes> in no particular order.
		 *		fileIgnoredKeywords - Returns any ignored keywords as a string array in the format of 
		 *										<ConfigFileCommentType.Keywords>.
		 *		fileTags - Returns any defined tags as a string array.
		 *		errorList - If it couldn't successfully parse the file it will add error messages to this list.
		 *		
		 * Returns:
		 * 
		 *		Whether it was able to successfully load and parse the file without any errors.
		 */
		public bool Load (Path filename, out List<ConfigFileCommentType> fileCommentTypes, 
								 out List<string> fileIgnoredKeywords, out List<string> fileTags,
								 Errors.ErrorList errorList)
			{
			fileCommentTypes = new List<ConfigFileCommentType>();
			StringTable<ConfigFileCommentType> fileCommentTypeNames = 
				new StringTable<ConfigFileCommentType>(Engine.CommentTypes.Manager.KeySettingsForCommentTypes);
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
				ConfigFileCommentType currentCommentType = null;
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
						currentCommentType = null;
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
						currentCommentType = null;
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
					// Comment Type
					//
						
					else if (identifier == "topic type")
						{
						if (fileCommentTypeNames.ContainsKey(value))
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Topics.txt.TopicTypeAlreadyExists(name)", value)
								);
							
							// Continue parsing.  We'll throw this into the existing type even though it shouldn't be overwriting
							// its values because we want to find any other errors there are in the file.
							currentCommentType = fileCommentTypeNames[value];
							}
							
						else
							{
							// There is no 1.5, but this covers all the 2.0 prereleases.
							if (file.Version < "1.5" && String.Compare(value, "generic", true) == 0)
								{  value = "Information";  }
								
							currentCommentType = new ConfigFileCommentType(value, false, file.LineNumber);
							fileCommentTypes.Add(currentCommentType);
							fileCommentTypeNames.Add(value, currentCommentType);
							}								
						}
						
						
					//
					// Alter Comment Type
					//
					
					else if (alterCommentTypeRegex.IsMatch(identifier))
						{
						// If this type already exists, collapse it into the current definition.
						if (fileCommentTypeNames.ContainsKey(value))
							{
							currentCommentType = fileCommentTypeNames[value];
							}
							
						// If it doesn't exist, create the new type anyway with the alter flag set because it may exist in another
						// file.
						else
							{
							currentCommentType = new ConfigFileCommentType(value, true, file.LineNumber);
							fileCommentTypes.Add(currentCommentType);
							fileCommentTypeNames.Add(value, currentCommentType);
							}								
						}


					//
					// (Plural) Display Name (From Locale)
					//
						
					else if (displayNameRegex.IsMatch(identifier))
						{
						if (currentCommentType == null)
							{  NeedsCommentTypeError(file, identifier);  }
						else if (currentCommentType.DisplayNameFromLocale != null)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Topics.txt.CannotDefineXWhenYIsDefined(x,y)", "Display Name", "Display Name from Locale")
								);
							}
						else
							{						
							currentCommentType.DisplayName = value;
							}
						}
					else if (pluralDisplayNameRegex.IsMatch(identifier))
						{
						if (currentCommentType == null)
							{  NeedsCommentTypeError(file, identifier);  }
						else if (currentCommentType.PluralDisplayNameFromLocale != null)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Topics.txt.CannotDefineXWhenYIsDefined(x,y)", "Plural Display Name", "Plural Display Name from Locale")
								);
							}
						else
							{						
							currentCommentType.PluralDisplayName = value;
							}
						}
					else if (displayNameFromLocaleRegex.IsMatch(identifier))
						{
						if (currentCommentType == null)
							{  NeedsCommentTypeError(file, identifier);  }
						else if (currentCommentType.DisplayName != null)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Topics.txt.CannotDefineXWhenYIsDefined(x,y)", "Display Name from Locale", "Display Name")
								);
							}
						else
							{						
							currentCommentType.DisplayNameFromLocale = value;
							}
						}
					else if (pluralDisplayNameFromLocaleRegex.IsMatch(identifier))
						{
						if (currentCommentType == null)
							{  NeedsCommentTypeError(file, identifier);  }
						else if (currentCommentType.PluralDisplayName != null)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Topics.txt.CannotDefineXWhenYIsDefined(x,y)", "Plural Display Name from Locale", "Plural Display Name")
								);
							}
						else
							{						
							currentCommentType.PluralDisplayNameFromLocale = value;
							}
						}
						
						
					//
					// Simple Identifier
					//
						
					else if (identifier == "simple identifier")
						{
						if (currentCommentType == null)
							{  NeedsCommentTypeError(file, identifier);  }
						else if (nonASCIILettersRegex.IsMatch(value))
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Topics.txt.SimpleIdentifierMustOnlyBeASCIILetters(name)", value)
								);
							}
						else
							{						
							currentCommentType.SimpleIdentifier = value;
							}
						}
						
						
					//
					// Index
					//
						
					else if (identifier == "index")
						{
						if (currentCommentType != null)
							{
							value = value.ToLower();
							
							if (yesRegex.IsMatch(value))
								{
								currentCommentType.Index = CommentType.IndexValue.Yes;
								}
							else if (noRegex.IsMatch(value))
								{
								currentCommentType.Index = CommentType.IndexValue.No;
								}
							else if (value.StartsWith("with "))
								{
								currentCommentType.Index = CommentType.IndexValue.IndexWith;
								
								// We hold off on validating this because it may be defined later in the file or in another file.
								currentCommentType.IndexWith = value.Substring(5);
								}
							else
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Topics.txt.UnrecognizedValue(keyword, value)", "Index", value)
									);
								}
							}
						else
							{  NeedsCommentTypeError(file, identifier);  }
						}
						
					else if (identifier == "index with")
						{
						if (currentCommentType != null)
							{
							currentCommentType.Index = CommentType.IndexValue.IndexWith;
							
							// We hold off on validating this because it may be defined later in the file or in another file.
							currentCommentType.IndexWith = value;
							}
						else
							{  NeedsCommentTypeError(file, identifier);  }
						}
						
						
					//
					// Scope
					//
					
					else if (identifier == "scope")
						{
						if (currentCommentType != null)
							{
							value = value.ToLower();
							
							if (value == "normal")
								{
								currentCommentType.Scope = CommentType.ScopeValue.Normal;
								}
							else if (startRegex.IsMatch(value))
								{
								currentCommentType.Scope = CommentType.ScopeValue.Start;
								}
							else if (endRegex.IsMatch(value))
								{
								currentCommentType.Scope = CommentType.ScopeValue.End;
								}
							else if (alwaysGlobalRegex.IsMatch(value))
								{
								currentCommentType.Scope = CommentType.ScopeValue.AlwaysGlobal;
								}
							else
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Topics.txt.UnrecognizedValue(keyword, value)", "Scope", value)
									);
								}
							}
						else
							{  NeedsCommentTypeError(file, identifier);  }
						}
						
						
					//
					// Flags
					//
					
					else if (flagsRegex.IsMatch(identifier))
						{
						if (currentCommentType != null)
							{
							value = value.ToLower();
							
							if (!string.IsNullOrEmpty(value))
								{
								string[] flagsArray = commaSeparatorRegex.Split(value);
							
								foreach (string flag in flagsArray)
									{
									if (flag == "code")
										{  currentCommentType.Flags.Code = true;  }
									else if (flag == "file")
										{  currentCommentType.Flags.File = true;  }
									else if (documentationRegex.IsMatch(flag))
										{  currentCommentType.Flags.Documentation = true;  }
									else if (variableTypeRegex.IsMatch(flag))
										{  currentCommentType.Flags.VariableType = true;  }
									else if (classHierarchyRegex.IsMatch(flag))
										{  currentCommentType.Flags.ClassHierarchy = true;  }
									else if (databaseHierarchyRegex.IsMatch(flag))
										{  currentCommentType.Flags.DatabaseHierarchy = true;  }
									else if (enumRegex.IsMatch(flag))
										{  currentCommentType.Flags.Enum = true;  }
									else if (string.IsNullOrEmpty(flag) == false)
										{  
										file.AddError(
											Locale.Get("NaturalDocs.Engine", "Topics.txt.UnrecognizedValue(keyword, value)", "Flags", flag)
											);
										}
									}

								List<string> flagErrors = currentCommentType.Flags.Validate(false, currentCommentType.Scope, "NaturalDocs.Engine");

								if (flagErrors != null)
									{
									foreach (string flagError in flagErrors)
										{  file.AddError(flagError);  }
									}
								}
							}
						else
							{  NeedsCommentTypeError(file, identifier);  }
						}
					
					
					// 
					// Class Hierarchy (deprecated, convert to flag)
					//
					
					else if (classHierarchyRegex.IsMatch(identifier))
						{
						if (currentCommentType != null)
							{
							value = value.ToLower();
							
							if (yesRegex.IsMatch(value))
								{
								currentCommentType.Flags.ClassHierarchy = true;
								}
							else if (noRegex.IsMatch(value))
								{
								currentCommentType.Flags.ClassHierarchy = false;
								}
							else
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Topics.txt.UnrecognizedValue(keyword, value)", "Class Hierarchy", value)
									);
								}
							}
						else
							{  NeedsCommentTypeError(file, identifier);  }
						}
						
						
					// 
					// Break Lists
					//
					
					else if (breakListsRegex.IsMatch(identifier))
						{
						if (currentCommentType != null)
							{
							value = value.ToLower();
							
							if (yesRegex.IsMatch(value))
								{
								currentCommentType.BreakLists = true;
								}
							else if (noRegex.IsMatch(value))
								{
								currentCommentType.BreakLists = false;
								}
							else
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Topics.txt.UnrecognizedValue(keyword, value)", "Break Lists", value)
									);
								}
							}
						else
							{  NeedsCommentTypeError(file, identifier);  }
						}
						
						
					//
					// Keywords
					//

					else if (keywordsRegex.IsMatch(identifier))
						{
						if (currentCommentType != null)
							{
							currentKeywordList = currentCommentType.Keywords;
							
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
							{  NeedsCommentTypeError(file, identifier);  }
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


			// Do a strict validation on all comment types that defined flags.

			foreach (var commentType in fileCommentTypes)
				{
				if (commentType.Flags.AllConfigurationProperties != 0)
					{
					// We don't want to duplicate errors we already found, so only do this if it passes the non-strict test.
					List<string> flagErrors = commentType.Flags.Validate(false, commentType.Scope, "NaturalDocs.Engine");

					if (flagErrors == null)
						{
						commentType.Flags.AddImpliedFlags();
						flagErrors = commentType.Flags.Validate(true, commentType.Scope, "NaturalDocs.Engine");

						if (flagErrors != null)
							{
							foreach (string flagError in flagErrors)
								{  errorList.Add(flagError, filename, commentType.LineNumber);  }
							}
						}
					}
				}
				
				
			if (errorList.Count == previousErrorCount)
				{  return true;  }
			else
				{  return false;  }
			}
			

		/* Function: NeedsCommentTypeError
		 * A shortcut function only used by <Load()> which adds an error stating that the passed keyword needs to appear
		 * in a comment type section.
		 */
		private void NeedsCommentTypeError (ConfigFile file, string identifier)
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
		 *		commentTypes - A list of <ConfigFileCommentTypes>.  
		 *		ignoredKeywords - A string array of ignored keywords in the format of <ConfigFileCommentType.Keywords>.
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
		public bool Save (Path filename, List<ConfigFileCommentType> commentTypes, 
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
			
			//output.Append( Locale.Get("NaturalDocs.Engine", "Topics.txt.TagsHeader.multiline") );
			//output.AppendLine();

			if (tags.Count > 0)
				{
				output.AppendLine("Tags:");
				foreach (string tag in tags)
					{  output.AppendLine("   " + tag);  }
				}
			//else
				//{
				//output.Append( Locale.Get("NaturalDocs.Engine", "Topics.txt.TagsReference.multiline") );
				//}

			//output.AppendLine();
			//output.AppendLine();
				
				
			// Comment Types
			
			output.Append( Locale.Get("NaturalDocs.Engine", "Topics.txt.TopicTypesHeader.multiline") );

			if (commentTypes.Count > 1)
				{  output.Append( Locale.Get("NaturalDocs.Engine", "Topics.txt.DeferredTopicTypesReference.multiline") );  }
				
			output.AppendLine();

			
			//
			// Create content
			//
			
			commentTypes.Sort( 
				delegate (ConfigFileCommentType a, ConfigFileCommentType b)
					{  return a.LineNumber - b.LineNumber;  } 
				);
				
			foreach (ConfigFileCommentType commentType in commentTypes)
				{
				if (isProjectFile && commentType.AlterType == true)
					{  output.Append("Alter ");  }
					
				output.AppendLine("Topic Type: " + commentType.Name);
				int oldGroupNumber = 0;
				
				if (commentType.DisplayName != null)
					{  
					LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Display Name: " + commentType.DisplayName);
					}
				if (commentType.PluralDisplayName != null)
					{
					LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Plural Display Name: " + commentType.PluralDisplayName);
					}
				if (commentType.DisplayNameFromLocale != null)
					{
					LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Display Name from Locale: " + commentType.DisplayNameFromLocale);
					}
				if (commentType.PluralDisplayNameFromLocale != null)
					{
					LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Plural Display Name from Locale: " + commentType.PluralDisplayNameFromLocale);
					}
				if (commentType.SimpleIdentifier != null)
					{
					LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					output.AppendLine("   Simple Identifier: " + commentType.SimpleIdentifier);
					}
					
				if (commentType.Index != null)
					{
					LineBreakOnGroupChange(2, ref oldGroupNumber, output);

					output.Append("   Index: ");
					
					if (commentType.Index == CommentType.IndexValue.Yes)
						{  output.AppendLine("Yes");  }
					else if (commentType.Index == CommentType.IndexValue.No)
						{  output.AppendLine("No");  }
					else  // IndexWith
						{  output.AppendLine("with " + commentType.IndexWith);  }
					}
					
				if (commentType.Scope != null)
					{
					LineBreakOnGroupChange(2, ref oldGroupNumber, output);

					output.Append("   Scope: ");
					
					if (commentType.Scope == CommentType.ScopeValue.Normal)
						{  output.AppendLine("Normal");  }
					else if (commentType.Scope == CommentType.ScopeValue.Start)
						{  output.AppendLine("Start");  }
					else if (commentType.Scope == CommentType.ScopeValue.End)
						{  output.AppendLine("End");  }
					else  // AlwaysGlobal
						{  output.AppendLine("Always Global");  }
					}
					
				if (commentType.Flags.AllConfigurationProperties != 0)
					{
					LineBreakOnGroupChange(2, ref oldGroupNumber, output);

					output.Append("   Flags: ");
					
					if (commentType.Flags.Code)
						{  output.Append("Code");  }
					else if (commentType.Flags.File)
						{  output.Append("File");  }
					else if (commentType.Flags.Documentation)
						{  output.Append("Documentation");  }

					if (commentType.Flags.VariableType)
						{  output.Append(", Variable Type");  }

					if (commentType.Flags.ClassHierarchy)
						{  output.Append(", Class Hierarchy");  }
					else if (commentType.Flags.DatabaseHierarchy)
						{  output.Append(", Database Hierarchy");  }

					if (commentType.Flags.Enum)
						{  output.Append(", Enum");  }

					output.AppendLine();
					}
					
				if (commentType.BreakLists != null)
					{
					LineBreakOnGroupChange(2, ref oldGroupNumber, output);

					output.Append("   Break Lists: ");
					
					if (commentType.BreakLists == true)
						{  output.AppendLine("Yes");  }
					else
						{  output.AppendLine("No");  }
					}
					
				if (commentType.Keywords.Count != 0)
					{
					LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					
					output.AppendLine("   Keywords:");
					AppendKeywordList(output, commentType.Keywords, "      ");
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
		protected Regex.TopicTypes.AlterTopicType alterCommentTypeRegex;
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