/*
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.ConfigFiles.TextFileParser
 * ____________________________________________________________________________
 *
 * A class to handle loading and saving <Comments.txt> and Topics.txt.
 *
 *
 * Multithreading: Not Thread Safe
 *
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.CommentTypes.ConfigFiles
	{
	public class TextFileParser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: TextFileParser
		 */
		public TextFileParser ()
			{
			yesRegex = new Regex.Config.Yes();
			noRegex = new Regex.Config.No();
			startRegex = new Regex.CommentTypes.ScopeStart();
			endRegex = new Regex.CommentTypes.ScopeEnd();

			nonASCIILettersRegex = new Regex.NonASCIILetters();

			ignoreKeywordsRegex = new Regex.CommentTypes.IgnoreKeywords();
			commentTypeRegex = new Regex.CommentTypes.CommentType();
			alterCommentTypeRegex = new Regex.CommentTypes.AlterCommentType();
			displayNameRegex = new Regex.CommentTypes.DisplayName();
			pluralDisplayNameRegex = new Regex.CommentTypes.PluralDisplayName();
			displayNameFromLocaleRegex = new Regex.CommentTypes.DisplayNameFromLocale();
			pluralDisplayNameFromLocaleRegex = new Regex.CommentTypes.PluralDisplayNameFromLocale();
			hierarchyNameRegex = new Regex.CommentTypes.HierarchyName();
			flagsRegex = new Regex.CommentTypes.Flags();
			documentationRegex = new Regex.CommentTypes.Documentation();
			variableTypeRegex = new Regex.CommentTypes.VariableType();
			classHierarchyRegex = new Regex.CommentTypes.ClassHierarchy();
			enumRegex = new Regex.CommentTypes.Enum();
			breakListsRegex = new Regex.CommentTypes.BreakLists();
			keywordsRegex = new Regex.CommentTypes.Keywords();
			languageSpecificKeywordsRegex = new Regex.CommentTypes.LanguageSpecificKeywords();
			commaSeparatorRegex = new Regex.CondensedWhitespaceCommaSeparator();
			alwaysGlobalRegex = new Regex.CommentTypes.ScopeAlwaysGlobal();
			tagsRegex = new Regex.CommentTypes.Tags();
			}



		// Group: Loading Functions
		// __________________________________________________________________________


		/* Function: Load
		 *
		 * Loads the contents of a <Comments.txt> file into a <ConfigFiles.TextFile>, returning whether it was successful.  If it
		 * was unsuccessful config will be null and it will place errors on the errorList.
		 *
		 * Parameters:
		 *
		 *		filename - The <Path> where the file is located.
		 *		propertySource - The <Engine.Config.PropertySource> associated with the file.
		 *		errorList - If it couldn't successfully parse the file it will add error messages to this list.
		 *		config - The contents of the file as a <ConfigFiles.TextFile>.
		 */
		public bool Load (Path filename, Engine.Config.PropertySource propertySource, Errors.ErrorList errorList,
								 out ConfigFiles.TextFile config)
			{
			int previousErrorCount = errorList.Count;

			using (ConfigFile file = new ConfigFile())
				{
				bool openResult = file.Open(filename,
														 propertySource,
														 ConfigFile.FileFormatFlags.CondenseIdentifierWhitespace |
														 ConfigFile.FileFormatFlags.CondenseValueWhitespace |
														 ConfigFile.FileFormatFlags.SupportsNullValueLines |
														 ConfigFile.FileFormatFlags.SupportsRawValueLines |
														 ConfigFile.FileFormatFlags.MakeIdentifiersLowercase,
														 errorList);

				if (openResult == false)
					{
					config = null;
					return false;
					}

				config = new ConfigFiles.TextFile();

				TextFileCommentType currentCommentType = null;
				TextFileKeywordGroup currentKeywordGroup = null;
				bool inKeywords = false;
				bool inTags = false;

				while (file.Get(out string identifier, out string value))
					{

					//
					// Identifierless lines
					//

					if (identifier == null)
						{


						// Keywords

						if (inKeywords)
							{

							// Separate keywords

							string keyword, pluralKeyword;
							int commaIndex = value.IndexOf(',');

							if (commaIndex == -1)
								{
								keyword = value;
								pluralKeyword = null;
								}
							else
								{
								keyword = value.Substring(0, commaIndex).TrimEnd();
								pluralKeyword = value.Substring(commaIndex + 1).TrimStart();

								if (pluralKeyword.IndexOf(',') != -1)
									{
									file.AddError(
										Locale.Get("NaturalDocs.Engine", "Comments.txt.NoMoreThanTwoKeywordsOnALine")
										);
									}
								}


							// Check for banned characters

							int bannedCharIndex = keyword.IndexOfAny(BannedKeywordChars);
							char bannedChar = '\0';

							if (bannedCharIndex != -1)
								{  bannedChar = keyword[bannedCharIndex];  }
							else if (pluralKeyword != null)
								{
								bannedCharIndex = pluralKeyword.IndexOfAny(BannedKeywordChars);

								if (bannedCharIndex != -1)
									{  bannedChar = pluralKeyword[bannedCharIndex];  }
								}

							if (bannedChar != '\0')
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Comments.txt.KeywordsCannotContain(char)", bannedChar)
									);
								// Continue parsing
								}


							// Add to config

							currentKeywordGroup.Add(keyword, pluralKeyword);
							}


						// Tags, only a single value allowed

						else if (inTags)
							{
							if (value.IndexOf(',') != -1)
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Comments.txt.NoMoreThanOneTagOnALine")
									);
								}

							int bannedChar = value.IndexOfAny(BannedKeywordChars);
							if (bannedChar != -1)
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Comments.txt.TagsCannotContain(char)", value[bannedChar])
									);
								// Continue parsing
								}

							config.AddTag(value, file.PropertyLocation);
							}


						// Raw line

						else
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "ConfigFile.LineNotInIdentifierValueFormat")
								);
							}

						// Continue so we don't need to put all the identifier handling code in an else.
						continue;
						}


					// If we're here the line has an identifier
					currentKeywordGroup = null;
					inKeywords = false;
					inTags = false;


					//
					// Ignore Keywords
					//

					if (ignoreKeywordsRegex.IsMatch(identifier))
						{
						currentCommentType = null;

						currentKeywordGroup = new TextFileKeywordGroup(file.PropertyLocation);
						config.AddIgnoredKeywordGroup(currentKeywordGroup);
						inKeywords = true;

						if (!string.IsNullOrEmpty(value))
							{
							string[] ignoredKeywordsArray = commaSeparatorRegex.Split(value);

							foreach (string ignoredKeyword in ignoredKeywordsArray)
								{
								int bannedChar = ignoredKeyword.IndexOfAny(BannedKeywordChars);
								if (bannedChar != -1)
									{
									file.AddError(
										Locale.Get("NaturalDocs.Engine", "Comments.txt.KeywordsCannotContain(char)", ignoredKeyword[bannedChar])
										);
									// Continue parsing
									}

								currentKeywordGroup.Add(ignoredKeyword);
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
										Locale.Get("NaturalDocs.Engine", "Comments.txt.TagsCannotContain(char)", tag[bannedChar])
										);
									// Continue parsing
									}

								config.AddTag(tag, file.PropertyLocation);
								}
							}
						}


					//
					// Comment Type
					//

					else if (commentTypeRegex.IsMatch(identifier))
						{
						var existingCommentType = config.FindCommentType(value);

						if (existingCommentType != null)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Comments.txt.CommentTypeAlreadyExists(name)", value)
								);

							// Continue parsing.  We'll throw this into the existing type even though it shouldn't be overwriting
							// its values because we want to find any other errors there are in the file.
							currentCommentType = existingCommentType;
							}

						else
							{
							// There is no 1.6, but this covers all the 2.0 prereleases.
							if (file.Version < "1.6" && String.Compare(value, "generic", true) == 0)
								{  value = "Information";  }

							currentCommentType = new TextFileCommentType(value, file.PropertyLocation);
							config.AddCommentType(currentCommentType);
							}
						}


					//
					// Alter Comment Type
					//

					else if (alterCommentTypeRegex.IsMatch(identifier))
						{
						// We don't check if the name exists because it may exist in a different file.  We also don't check if it exists
						// in the current file because using Alter is valid (if unnecessary) in that case and we don't want to combine
						// their definitions.  Why?  Consider this:
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
						// first and be overridden by Comment Type B.  So we just create two comment type entries for A instead.

						currentCommentType = new TextFileCommentType(value, file.PropertyLocation, alterType: true);
						config.AddCommentType(currentCommentType);
						}


					//
					// (Plural) Display Name (From Locale)
					//

					else if (displayNameRegex.IsMatch(identifier))
						{
						if (currentCommentType == null)
							{  AddNeedsCommentTypeError(file, identifier);  }
						else if (currentCommentType.HasDisplayNameFromLocale)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Comments.txt.CannotDefineXWhenYIsDefined(x,y)", "Display Name", "Display Name from Locale")
								);
							}
						else
							{
							currentCommentType.SetDisplayName(value, file.PropertyLocation);
							}
						}
					else if (pluralDisplayNameRegex.IsMatch(identifier))
						{
						if (currentCommentType == null)
							{  AddNeedsCommentTypeError(file, identifier);  }
						else if (currentCommentType.HasPluralDisplayNameFromLocale)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Comments.txt.CannotDefineXWhenYIsDefined(x,y)", "Plural Display Name", "Plural Display Name from Locale")
								);
							}
						else
							{
							currentCommentType.SetPluralDisplayName(value, file.PropertyLocation);
							}
						}
					else if (displayNameFromLocaleRegex.IsMatch(identifier))
						{
						if (currentCommentType == null)
							{  AddNeedsCommentTypeError(file, identifier);  }
						else if (currentCommentType.HasDisplayName)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Comments.txt.CannotDefineXWhenYIsDefined(x,y)", "Display Name from Locale", "Display Name")
								);
							}
						else
							{
							currentCommentType.SetDisplayNameFromLocale(value, file.PropertyLocation);
							}
						}
					else if (pluralDisplayNameFromLocaleRegex.IsMatch(identifier))
						{
						if (currentCommentType == null)
							{  AddNeedsCommentTypeError(file, identifier);  }
						else if (currentCommentType.HasPluralDisplayName)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Comments.txt.CannotDefineXWhenYIsDefined(x,y)", "Plural Display Name from Locale", "Plural Display Name")
								);
							}
						else
							{
							currentCommentType.SetPluralDisplayNameFromLocale(value, file.PropertyLocation);
							}
						}


					//
					// Simple Identifier
					//

					else if (identifier == "simple identifier")
						{
						if (currentCommentType == null)
							{  AddNeedsCommentTypeError(file, identifier);  }
						else if (nonASCIILettersRegex.IsMatch(value))
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Comments.txt.SimpleIdentifierMustOnlyBeASCIILetters(name)", value)
								);
							}
						else
							{
							currentCommentType.SetSimpleIdentifier(value, file.PropertyLocation);
							}
						}


					//
					// Scope
					//

					else if (identifier == "scope")
						{
						if (currentCommentType == null)
							{  AddNeedsCommentTypeError(file, identifier);  }
						else
							{
							value = value.ToLower();

							if (value == "normal")
								{  currentCommentType.SetScope(CommentType.ScopeValue.Normal, file.PropertyLocation);  }
							else if (startRegex.IsMatch(value))
								{  currentCommentType.SetScope(CommentType.ScopeValue.Start, file.PropertyLocation);  }
							else if (endRegex.IsMatch(value))
								{  currentCommentType.SetScope(CommentType.ScopeValue.End, file.PropertyLocation);  }
							else if (alwaysGlobalRegex.IsMatch(value))
								{  currentCommentType.SetScope(CommentType.ScopeValue.AlwaysGlobal, file.PropertyLocation);  }
							else
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Comments.txt.UnrecognizedValue(keyword, value)", "Scope", value)
									);
								}
							}
						}


					//
					// Flags and Hierarchy
					//

					else if (flagsRegex.IsMatch(identifier))
						{
						if (currentCommentType == null)
							{  AddNeedsCommentTypeError(file, identifier);  }
						else
							{
							value = value.ToLower();

							if (!string.IsNullOrEmpty(value))
								{
								string[] flagStrings = commaSeparatorRegex.Split(value);
								CommentType.FlagValue flagsValue = default;

								foreach (string flagString in flagStrings)
									{
									if (flagString == "code")
										{  flagsValue |= CommentType.FlagValue.Code;  }
									else if (flagString == "file")
										{  flagsValue |= CommentType.FlagValue.File;  }
									else if (documentationRegex.IsMatch(flagString))
										{  flagsValue |= CommentType.FlagValue.Documentation;  }
									else if (variableTypeRegex.IsMatch(flagString))
										{  flagsValue |= CommentType.FlagValue.VariableType;  }
									else if (enumRegex.IsMatch(flagString))
										{  flagsValue |= CommentType.FlagValue.Enum;  }
									else
										{
										var hierarchyMatch = hierarchyNameRegex.Match(flagString);

										if (hierarchyMatch.Success)
											{
											currentCommentType.SetHierarchyName(hierarchyMatch.Groups[1].ToString(), file.PropertyLocation);
											}
										else if (string.IsNullOrEmpty(flagString) == false)
											{
											file.AddError(
												Locale.Get("NaturalDocs.Engine", "Comments.txt.UnrecognizedValue(keyword, value)", "Flags", flagString)
												);
											}
										}
									}

								currentCommentType.SetFlags(flagsValue, file.PropertyLocation);
								}
							}
						}


					//
					// Class Hierarchy (deprecated, convert to flag)
					//

					else if (classHierarchyRegex.IsMatch(identifier))
						{
						if (currentCommentType == null)
							{  AddNeedsCommentTypeError(file, identifier);  }
						else
							{
							value = value.ToLower();

							if (yesRegex.IsMatch(value))
								{  currentCommentType.SetHierarchyName("Class", file.PropertyLocation);  }
							else if (noRegex.IsMatch(value))
								{
								if (currentCommentType.HierarchyName != null &&
									currentCommentType.HierarchyName.ToLower() == "class")
									{  currentCommentType.SetHierarchyName(null, file.PropertyLocation);  }
								}
							else
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Comments.txt.UnrecognizedValue(keyword, value)", "Class Hierarchy", value)
									);
								}
							}
						}


					//
					// Keywords
					//

					else if (keywordsRegex.IsMatch(identifier))
						{
						if (currentCommentType == null)
							{  AddNeedsCommentTypeError(file, identifier);  }
						else
							{
							currentKeywordGroup = new TextFileKeywordGroup(file.PropertyLocation);
							currentCommentType.AddKeywordGroup(currentKeywordGroup);
							inKeywords = true;

							if (!string.IsNullOrEmpty(value))
								{
								string[] keywordsArray = commaSeparatorRegex.Split(value);

								foreach (string keyword in keywordsArray)
									{
									int bannedChar = keyword.IndexOfAny(BannedKeywordChars);
									if (bannedChar != -1)
										{
										file.AddError(
											Locale.Get("NaturalDocs.Engine", "Comments.txt.KeywordsCannotContain(char)", keyword[bannedChar])
											);
										// Continue parsing
										}

									currentKeywordGroup.Add(keyword);
									}
								}
							}
						}


					//
					// Language-Specific Keywords
					//

					// This must be tested after ignored and language-general keywords so that their modifiers ("add" or "ignore") don't
					// get mistaken for language names.
					else if (languageSpecificKeywordsRegex.IsMatch(identifier))
						{
						if (currentCommentType == null)
							{  AddNeedsCommentTypeError(file, identifier);  }
						else
							{
							var match = languageSpecificKeywordsRegex.Match(identifier);
							var languageName = match.Groups[1].ToString();

							currentKeywordGroup = new TextFileKeywordGroup(file.PropertyLocation, languageName);
							currentCommentType.AddKeywordGroup(currentKeywordGroup);
							inKeywords = true;

							if (!string.IsNullOrEmpty(value))
								{
								string[] keywordsArray = commaSeparatorRegex.Split(value);

								foreach (string keyword in keywordsArray)
									{
									int bannedChar = keyword.IndexOfAny(BannedKeywordChars);
									if (bannedChar != -1)
										{
										file.AddError(
											Locale.Get("NaturalDocs.Engine", "Comments.txt.KeywordsCannotContain(char)", keyword[bannedChar])
											);
										// Continue parsing
										}

									currentKeywordGroup.Add(keyword);
									}
								}
							}
						}


					//
					// Deprecated keywords: Can Group With, Page Title if First
					//

					else if (identifier == "index" ||
							   identifier == "index with" ||
							   breakListsRegex.IsMatch(identifier) ||
							   identifier == "can group with" ||
							   identifier == "page title if first")
						{
						// Ignore and continue
						}


					//
					// Unrecognized keywords
					//

					else
						{
						file.AddError(
							Locale.Get("NaturalDocs.Engine", "Comments.txt.UnrecognizedKeyword(keyword)", identifier)
							);
						}

					}  // while (file.Get)

				file.Close();
				}


			if (errorList.Count == previousErrorCount)
				{
				return true;
				}
			else
				{
				config = null;
				return false;
				}
			}


		/* Function: AddNeedsCommentTypeError
		 * A shortcut function only used by <Load()> which adds an error stating that the passed keyword needs to appear
		 * in a comment type section.
		 */
		private void AddNeedsCommentTypeError (ConfigFile file, string identifier)
			{
			file.AddError(
				Locale.Get("NaturalDocs.Engine", "Comments.txt.KeywordMustBeInCommentType(keyword)", identifier)
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
		 *		filename - The <Path> where the file should be saved.
		 *		propertySource - The <Engine.Config.PropertySource> associated with the file.  It must be
		 *								 <Engine.Config.PropertySource.ProjectCommentsFile> or
		 *								 <Engine.Config.PropertySource.SystemCommentsFile>.
		 *		config - The configuration to be saved.
		 *		errorList - If it couldn't successfully save the file it will add error messages to this list.  This list may be null if
		 *						you don't need the error.
		 *
		 * Returns:
		 *
		 *		Whether it was able to successfully save the file without any errors.  If the file didn't need saving because
		 *		the generated file was the same as the one on disk, this will still return true.
		 */
		public bool Save (Path filename, Engine.Config.PropertySource propertySource, ConfigFiles.TextFile config,
								 Errors.ErrorList errorList = null)
			{
			System.Text.StringBuilder output = new System.Text.StringBuilder(1024);

			string projectOrSystem;

			if (propertySource == Engine.Config.PropertySource.ProjectCommentsFile)
				{  projectOrSystem = "Project";  }
			else if (propertySource == Engine.Config.PropertySource.SystemCommentsFile)
				{  projectOrSystem = "System";  }
			else
				{  throw new InvalidOperationException();  }


			// Header

			output.AppendLine("Format: " + Engine.Instance.VersionString);
			output.AppendLine();
			output.Append( Locale.Get("NaturalDocs.Engine", "Comments.txt." + projectOrSystem + "Header.multiline") );
			output.AppendLine();
			output.AppendLine();


			// Ignored Keywords

			if (config.HasIgnoredKeywords)
				{
				output.Append( Locale.Get("NaturalDocs.Engine", "Comments.txt.IgnoredKeywordsHeader.multiline") );
				output.AppendLine();
				output.AppendLine("Ignore Keywords:");

				foreach (var ignoredKeywordGroup in config.IgnoredKeywordGroups)
					{  AppendKeywordGroup(output, ignoredKeywordGroup, "   ");  }

				output.AppendLine();
				output.AppendLine();
				}
			else
				{
				if (propertySource == Engine.Config.PropertySource.ProjectCommentsFile)
					{
					output.Append( Locale.Get("NaturalDocs.Engine", "Comments.txt.IgnoredKeywordsHeader.multiline") );
					output.AppendLine();
					output.Append( Locale.Get("NaturalDocs.Engine", "Comments.txt.IgnoredKeywordsReference.multiline") );
					output.AppendLine();
					output.AppendLine();
					}
				// Add nothing for the system config file.
				}


			// Tags

			// Tags are undocumented so don't include any of the syntax reference.  Do include the content if it's there though.

			//output.Append( Locale.Get("NaturalDocs.Engine", "Comments.txt.TagsHeader.multiline") );
			//output.AppendLine();

			if (config.HasTags)
				{
				output.AppendLine("Tags:");

				foreach (var tag in config.Tags)
					{  output.AppendLine("   " + tag);  }

				output.AppendLine();
				output.AppendLine();
				}
			//else
				//{
				//output.Append( Locale.Get("NaturalDocs.Engine", "Comments.txt.TagsReference.multiline") );
				//}

			//output.AppendLine();
			//output.AppendLine();


			// Comment Types

			output.Append( Locale.Get("NaturalDocs.Engine", "Comments.txt.CommentTypesHeader.multiline") );

			if (config.HasCommentTypes)
				{
				output.Append( Locale.Get("NaturalDocs.Engine", "Comments.txt.DeferredCommentTypesReference.multiline") );
				output.AppendLine();

				foreach (var commentType in config.CommentTypes)
					{
					if (commentType.AlterType == true)
						{  output.Append("Alter ");  }

					output.AppendLine("Comment Type: " + commentType.Name);
					int oldGroupNumber = 0;

					if (commentType.HasDisplayName)
						{
						AppendLineBreakOnGroupChange(1, ref oldGroupNumber, output);
						output.AppendLine("   Display Name: " + commentType.DisplayName);
						}
					if (commentType.HasPluralDisplayName)
						{
						AppendLineBreakOnGroupChange(1, ref oldGroupNumber, output);
						output.AppendLine("   Plural Display Name: " + commentType.PluralDisplayName);
						}
					if (commentType.HasDisplayNameFromLocale)
						{
						AppendLineBreakOnGroupChange(1, ref oldGroupNumber, output);
						output.AppendLine("   Display Name from Locale: " + commentType.DisplayNameFromLocale);
						}
					if (commentType.HasPluralDisplayNameFromLocale)
						{
						AppendLineBreakOnGroupChange(1, ref oldGroupNumber, output);
						output.AppendLine("   Plural Display Name from Locale: " + commentType.PluralDisplayNameFromLocale);
						}
					if (commentType.HasSimpleIdentifier)
						{
						AppendLineBreakOnGroupChange(1, ref oldGroupNumber, output);
						output.AppendLine("   Simple Identifier: " + commentType.SimpleIdentifier);
						}

					if (commentType.HasScope)
						{
						AppendLineBreakOnGroupChange(2, ref oldGroupNumber, output);

						output.Append("   Scope: ");

						switch ((CommentType.ScopeValue)commentType.Scope)
							{
							case CommentType.ScopeValue.Normal:
								output.AppendLine("Normal");
								break;
							case CommentType.ScopeValue.Start:
								output.AppendLine("Start");
								break;
							case CommentType.ScopeValue.End:
								output.AppendLine("End");
								break;
							case CommentType.ScopeValue.AlwaysGlobal:
								output.AppendLine("Always Global");
								break;
							default:
								throw new NotImplementedException();
							}
						}

					if (commentType.HasFlags || commentType.HasHierarchyName)
						{
						AppendLineBreakOnGroupChange(2, ref oldGroupNumber, output);

						output.Append("   Flags: ");
						List<string> flagStrings = new List<string>(4);

						if (commentType.HasFlags)
							{
							var flagsValue = (CommentType.FlagValue)commentType.Flags;

							if ( (flagsValue & CommentType.FlagValue.Code) != 0)
								{  flagStrings.Add("Code");  }
							if ( (flagsValue & CommentType.FlagValue.File) != 0)
								{  flagStrings.Add("File");  }
							if ( (flagsValue & CommentType.FlagValue.Documentation) != 0)
								{  flagStrings.Add("Documentation");  }

							if ( (flagsValue & CommentType.FlagValue.VariableType) != 0)
								{  flagStrings.Add("Variable Type");  }

							if ( (flagsValue & CommentType.FlagValue.Enum) != 0)
								{  flagStrings.Add("Enum");  }
							}

						if (commentType.HasHierarchyName)
							{  flagStrings.Add( commentType.HierarchyName + " Hierarchy" );  }

						for (int i = 0; i < flagStrings.Count; i++)
							{
							if (i > 0)
								{  output.Append(", ");  }

							output.Append(flagStrings[i]);
							}

						output.AppendLine();
						}

					if (commentType.HasKeywordGroups)
						{
						foreach (var keywordGroup in commentType.KeywordGroups)
							{
							output.AppendLine();

							if (keywordGroup.IsLanguageAgnostic)
								{  output.AppendLine("   Keywords:");  }
							else
								{  output.AppendLine("   " + keywordGroup.LanguageName + " Keywords:");  }

							AppendKeywordGroup(output, keywordGroup, "      ");
							}
						}

					output.AppendLine();
					output.AppendLine();
					}
				}
			else // no comment types
				{  output.AppendLine();  }

			output.Append( Locale.Get("NaturalDocs.Engine", "Comments.txt." + projectOrSystem + "CommentTypesReference.multiline") );


			// Compare with previous file and write to disk

			return ConfigFile.SaveIfDifferent(filename, output.ToString(), noErrorOnFail: (errorList == null), errorList);
			}


		/* Function: AppendKeywordGroup
		 * A function used only by <Save()> that adds a keyword group to the passed StringBuilder.
		 */
		private void AppendKeywordGroup (System.Text.StringBuilder output, TextFileKeywordGroup keywordGroup, string linePrefix)
			{
			foreach (var keywordDefinition in keywordGroup.KeywordDefinitions)
				{
				output.Append(linePrefix + keywordDefinition.Keyword);

				if (keywordDefinition.HasPlural)
					{  output.Append(", " + keywordDefinition.Plural);  }

				output.AppendLine();
				}
			}


		/* Function: AppendLineBreakOnGroupChange
		 * A shortcut function used only by <Save()> which inserts a line break between groups.  It will also update
		 * oldGroupNumber automatically.
		 */
		private void AppendLineBreakOnGroupChange (int groupNumber, ref int oldGroupNumber, System.Text.StringBuilder output)
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
		protected Regex.CommentTypes.ScopeStart startRegex;
		protected Regex.CommentTypes.ScopeEnd endRegex;

		protected Regex.NonASCIILetters nonASCIILettersRegex;

		protected Regex.CommentTypes.IgnoreKeywords ignoreKeywordsRegex;
		protected Regex.CommentTypes.CommentType commentTypeRegex;
		protected Regex.CommentTypes.AlterCommentType alterCommentTypeRegex;
		protected Regex.CommentTypes.DisplayName displayNameRegex;
		protected Regex.CommentTypes.PluralDisplayName pluralDisplayNameRegex;
		protected Regex.CommentTypes.DisplayNameFromLocale displayNameFromLocaleRegex;
		protected Regex.CommentTypes.PluralDisplayNameFromLocale pluralDisplayNameFromLocaleRegex;
		protected Regex.CommentTypes.HierarchyName hierarchyNameRegex;
		protected Regex.CommentTypes.Flags flagsRegex;
		protected Regex.CommentTypes.Documentation documentationRegex;
		protected Regex.CommentTypes.VariableType variableTypeRegex;
		protected Regex.CommentTypes.ClassHierarchy classHierarchyRegex;
		protected Regex.CommentTypes.Enum enumRegex;
		protected Regex.CommentTypes.BreakLists breakListsRegex;
		protected Regex.CommentTypes.Keywords keywordsRegex;
		protected Regex.CommentTypes.LanguageSpecificKeywords languageSpecificKeywordsRegex;
		protected Regex.CondensedWhitespaceCommaSeparator commaSeparatorRegex;
		protected Regex.CommentTypes.ScopeAlwaysGlobal alwaysGlobalRegex;
		protected Regex.CommentTypes.Tags tagsRegex;


		// Group: Static Variables
		// __________________________________________________________________________

		/* var: BannedKeywordChars
		 * An array containing all the characters that cannot appear in keywords.  Best used with String.IndexOfAny().
		 */
		protected static char[] BannedKeywordChars = { '{', '}', ',', '#', ':' };


		}
	}
