﻿/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.ConfigFiles.TextFileParser
 * ____________________________________________________________________________
 *
 * A class to handle loading and saving <Languages.txt>.
 *
 *
 * Threading: Not Thread Safe
 *
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;


namespace CodeClear.NaturalDocs.Engine.Languages.ConfigFiles
	{
	public partial class TextFileParser
		{

		// Group: Loading Functions
		// __________________________________________________________________________


		/* Function: Load
		 *
		 * Loads the contents of a <Languages.txt> file into a <ConfigFiles.Textfile>, returning whether it was successful.  If it
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
														 ConfigFile.FileFormatFlags.MakeIdentifiersLowercase,
														 errorList);

				if (openResult == false)
					{
					config = null;
					return false;
					}

				config = new ConfigFiles.TextFile();

				TextFileLanguage currentLanguage = null;
				char[] space = { ' ' };
				System.Text.RegularExpressions.Match match;


				while (file.Get(out string lcIdentifier, out string value))
					{

					//
					// Ignore Extensions
					//

					if (IsIgnoreExtensionsRegexLC().IsMatch(lcIdentifier))
						{
						currentLanguage = null;

						var ignoredExtensions = value.Split(space);
						NormalizeFileExtensions(ignoredExtensions);

						config.AddIgnoredFileExtensions(ignoredExtensions, file.PropertyLocation);
						}


					//
					// Language
					//

					else if (lcIdentifier == "language")
						{
						var existingLanguage = config.FindLanguage(value);

						if (existingLanguage != null)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.LanguageAlreadyExists(name)", value)
								);

							// Continue parsing.  We'll throw this into the existing language even though it shouldn't be overwriting
							// its values because we want to find any other errors there are in the file.
							currentLanguage = existingLanguage;
							}

						else
							{
							currentLanguage = new TextFileLanguage(value, file.PropertyLocation);
							config.AddLanguage(currentLanguage);
							}
						}


					//
					// Alter Language
					//

					else if (IsAlterLanguageRegexLC().IsMatch(lcIdentifier))
						{
						// We don't check if the name exists because it may exist in a different file.  We also don't check if it exists
						// in the current file because using Alter is valid (if unnecessary) in that case and we don't want to combine
						// their definitions.  Why?  Consider this:
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
						// Extension langB should be part of Language A.  However, if we merged the definitions it would appear
						// first and be overridden by Language B.  So we just create two language entries for A instead.

						currentLanguage = new TextFileLanguage(value, file.PropertyLocation, alterLanguage: true);
						config.AddLanguage(currentLanguage);
						}


					//
					// Aliases
					//

					else if (IsAliasesRegexLC().IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else if (currentLanguage.AlterLanguage)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.NeedAddReplaceWhenAlteringLanguage(keyword)", "Aliases")
								);
							}
						else
							{
							var aliases = value.Split(space);
							currentLanguage.SetAliases(aliases, file.PropertyLocation);
							}
						}


					//
					// Add/Replace Aliases
					//

					else if ( (match = IsAddOrReplaceAliasesRegexLC().Match(lcIdentifier)) != null && match.Success )
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else
							{
							TextFileLanguage.PropertyChange propertyChange;

							if (match.Groups[1].Value == "add")
								{  propertyChange = TextFileLanguage.PropertyChange.Add;  }
							else if (match.Groups[1].Value == "replace")
								{  propertyChange = TextFileLanguage.PropertyChange.Replace;  }
							else
								{  throw new NotImplementedException();  }

							// If we're adding to a language that already has them, we need to combine the properties.
							if (propertyChange == TextFileLanguage.PropertyChange.Add &&
								currentLanguage.HasAliases)
								{
								var oldAliases = currentLanguage.Aliases;
								var newAliases = value.Split(space);

								List<string> combinedAliases = new List<string>(oldAliases.Count + newAliases.Length);
								combinedAliases.AddRange(oldAliases);
								combinedAliases.AddRange(newAliases);

								currentLanguage.SetAliases(combinedAliases, file.PropertyLocation, propertyChange);
								}

							// Otherwise we can just add them as is.
							else
								{
								var aliases = value.Split(space);
								currentLanguage.SetAliases(aliases, file.PropertyLocation, propertyChange);
								}
							}
						}



					//
					// File Extensions
					//

					else if (IsExtensionsRegexLC().IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else if (currentLanguage.AlterLanguage)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.NeedAddReplaceWhenAlteringLanguage(keyword)", "Extensions")
								);
							}
						else
							{
							var extensions = value.Split(space);
							NormalizeFileExtensions(extensions);

							currentLanguage.SetFileExtensions(extensions, file.PropertyLocation);
							}
						}


					//
					// Add/Replace File Extensions
					//

					else if ( (match = IsAddOrReplaceExtensionsRegexLC().Match(lcIdentifier)) != null && match.Success )
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else
							{
							TextFileLanguage.PropertyChange propertyChange;

							if (match.Groups[1].Value == "add")
								{  propertyChange = TextFileLanguage.PropertyChange.Add;  }
							else if (match.Groups[1].Value == "replace")
								{  propertyChange = TextFileLanguage.PropertyChange.Replace;  }
							else
								{  throw new NotImplementedException();  }

							// If we're adding to a language that already has them, we need to combine the properties.
							if (propertyChange == TextFileLanguage.PropertyChange.Add &&
								currentLanguage.HasFileExtensions)
								{
								var oldExtensions = currentLanguage.FileExtensions;
								var newExtensions = value.Split(space);
								NormalizeFileExtensions(newExtensions);

								List<string> combinedExtensions = new List<string>(oldExtensions.Count + newExtensions.Length);
								combinedExtensions.AddRange(oldExtensions);
								combinedExtensions.AddRange(newExtensions);

								currentLanguage.SetFileExtensions(combinedExtensions, file.PropertyLocation, propertyChange);
								}

							// Otherwise we can just add them as is.
							else
								{
								var extensions = value.Split(space);
								NormalizeFileExtensions(extensions);

								currentLanguage.SetFileExtensions(extensions, file.PropertyLocation, propertyChange);
								}
							}
						}



					//
					// Shebang Strings
					//

					else if (IsShebangStringsRegexLC().IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else if (currentLanguage.AlterLanguage)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.NeedAddReplaceWhenAlteringLanguage(keyword)", "Shebang Strings")
								);
							}
						else
							{
							var shebangStrings = value.Split(space);
							currentLanguage.SetShebangStrings(shebangStrings, file.PropertyLocation);
							}
						}


					//
					// Add/Replace Shebang Strings
					//

					else if ( (match = IsAddOrReplaceShebangStringsRegexLC().Match(lcIdentifier)) != null && match.Success )
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else
							{
							TextFileLanguage.PropertyChange propertyChange;

							if (match.Groups[1].Value == "add")
								{  propertyChange = TextFileLanguage.PropertyChange.Add;  }
							else if (match.Groups[1].Value == "replace")
								{  propertyChange = TextFileLanguage.PropertyChange.Replace;  }
							else
								{  throw new NotImplementedException();  }

							// If we're adding to a language that already has them, we need to combine the properties.
							if (propertyChange == TextFileLanguage.PropertyChange.Add &&
								currentLanguage.HasShebangStrings)
								{
								var oldShebangStrings = currentLanguage.ShebangStrings;
								var newShebangStrings = value.Split(space);

								List<string> combinedShebangStrings = new List<string>(oldShebangStrings.Count + newShebangStrings.Length);
								combinedShebangStrings.AddRange(oldShebangStrings);
								combinedShebangStrings.AddRange(newShebangStrings);

								currentLanguage.SetShebangStrings(combinedShebangStrings, file.PropertyLocation, propertyChange);
								}

							// Otherwise we can just add them as is.
							else
								{
								var shebangStrings = value.Split(space);
								currentLanguage.SetShebangStrings(shebangStrings, file.PropertyLocation, propertyChange);
								}
							}
						}



					//
					// Simple Identifier
					//

					else if (lcIdentifier == "simple identifier")
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else if (!value.IsOnlyAToZ())
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.SimpleIdentifierMustOnlyBeASCIILetters(name)", value)
								);
							}
						else
							{
							currentLanguage.SetSimpleIdentifier(value, file.PropertyLocation);
							}
						}



					//
					// Line Comments
					//

					else if (IsLineCommentsRegexLC().IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else
							{
							var lineCommentSymbols = value.Split(space);
							currentLanguage.SetLineCommentSymbols(lineCommentSymbols, file.PropertyLocation);
							}
						}



					//
					// Block Comments
					//

					else if (IsBlockCommentsRegexLC().IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else
							{
							var blockCommentStrings = value.Split(space);

							if (blockCommentStrings.Length % 2 != 0)
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Languages.txt.BlockCommentsMustHaveAnEvenNumberOfSymbols")
									);
								}
							else
								{
								List<BlockCommentSymbols> blockCommentSymbols =
									new List<BlockCommentSymbols>(blockCommentStrings.Length / 2);

								for (int i = 0; i < blockCommentStrings.Length; i += 2)
									{
									blockCommentSymbols.Add(
										new BlockCommentSymbols(blockCommentStrings[i], blockCommentStrings[i+1])
										);
									}

								currentLanguage.SetBlockCommentSymbols(blockCommentSymbols, file.PropertyLocation);
								}
							}
						}



					//
					// Member Operator
					//

					else if (IsMemberOperatorRegexLC().IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else
							{  currentLanguage.SetMemberOperator(value, file.PropertyLocation);  }
						}



					//
					// Line Extender
					//

					else if (lcIdentifier == "line extender")
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else
							{  currentLanguage.SetLineExtender(value, file.PropertyLocation);  }
						}



					//
					// Enum Values
					//

					else if (IsEnumValuesRegexLC().IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else
							{
							string lcValue = value.ToLower(CultureInfo.InvariantCulture);

							if (lcValue == "global")
								{  currentLanguage.SetEnumValues(Language.EnumValues.Global, file.PropertyLocation);  }
							else if (lcValue == "under type")
								{  currentLanguage.SetEnumValues(Language.EnumValues.UnderType, file.PropertyLocation);  }
							else if (lcValue == "under parent")
								{  currentLanguage.SetEnumValues(Language.EnumValues.UnderParent, file.PropertyLocation);  }
							else
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Languages.txt.InvalidEnumValue(value)", value)
									);
								}
							}
						}


					//
					// Case Sensitive
					//

					else if (IsCaseSensitiveRegexLC().IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else
							{
							string lcValue = value.ToLower(CultureInfo.InvariantCulture);

							if (ConfigFile.IsYes(lcValue))
								{  currentLanguage.SetCaseSensitive(true, file.PropertyLocation);  }
							else if (ConfigFile.IsNo(lcValue))
								{  currentLanguage.SetCaseSensitive(false, file.PropertyLocation);  }
							else
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Languages.txt.UnrecognizedValue(keyword, value)", "Case Sensitive", value)
									);
								}
							}
						}


					//
					// Block Comments Nest
					//

					else if (IsBlockCommentsNestRegexLC().IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else
							{
							string lcValue = value.ToLower(CultureInfo.InvariantCulture);

							if (ConfigFile.IsYes(lcValue))
								{  currentLanguage.SetBlockCommentsNest(true, file.PropertyLocation);  }
							else if (ConfigFile.IsNo(lcValue))
								{  currentLanguage.SetBlockCommentsNest(false, file.PropertyLocation);  }
							else
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Languages.txt.UnrecognizedValue(keyword, value)", "Block Comments Nest", value)
									);
								}
							}
						}


					//
					// Prototype Enders
					//

					// Use identifier and not lcIdentifier to keep the case of the comment type.  The regex will compensate.
					else if ( (match = IsPrototypeEndersRegexLC().Match(lcIdentifier)) != null && match.Success )
						{
						if (currentLanguage == null)
							{  AddNeedsLanguageError(file, lcIdentifier);  }
						else
							{
							string commentType = match.Groups[1].Value;
							string[] enderStrings = value.Split(space);

							var enders = new TextFilePrototypeEnders(commentType, file.PropertyLocation);
							enders.AddEnderStrings(enderStrings);

							currentLanguage.AddPrototypeEnders(enders);
							}
						}


					//
					// Deprecated keywords
					//

					else if ( IsIgnorePrefixesRegexLC().IsMatch(lcIdentifier) ||
								lcIdentifier == "perl package" ||
								lcIdentifier == "full language support" )
						{
						// Ignore
						}


					//
					// Unrecognized keywords
					//

					else
					    {
					    file.AddError(
					        Locale.Get("NaturalDocs.Engine", "Languages.txt.UnrecognizedKeyword(keyword)", lcIdentifier)
					        );
					    }

					}  // while (file.Get)

				file.Close();
				}


			if (errorList.Count == previousErrorCount)
				{  return true;  }
			else
				{  return false;  }
			}


		/* Function: AddNeedsLanguageError
		 * A shortcut function only used by <Load()> which adds an error stating that the passed keyword needs to appear
		 * in a language section.
		 */
		private void AddNeedsLanguageError (ConfigFile file, string identifier)
			{
			file.AddError(
				Locale.Get("NaturalDocs.Engine", "Languages.txt.KeywordMustBeInLanguage(keyword)", identifier)
				);
			}


		/* Function: NormalizeFileExtensions
		 * Takes a list of file extensions and removes any leading dots and stars, so ".txt" and "*.txt" are both accepted as just "txt".
		 */
		private void NormalizeFileExtensions (IList<string> extensions)
			{
			for (int i = 0; i < extensions.Count; i++)
				{  extensions[i] = extensions[i].TrimStart('*', '.');  }
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
		 *		propertySource - The <Engine.Config.PropertySource> associated with the file.  It must be
		 *								 <Engine.Config.PropertySource.ProjectLanguagesFile> or
		 *								 <Engine.Config.PropertySource.SystemLanguagesFile>.
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

			if (propertySource == Engine.Config.PropertySource.ProjectLanguagesFile)
				{  projectOrSystem = "Project";  }
			else if (propertySource == Engine.Config.PropertySource.SystemLanguagesFile)
				{  projectOrSystem = "System";  }
			else
				{  throw new InvalidOperationException();  }


			// Header

			output.AppendLine("Format: " + Engine.Instance.VersionString);
			output.AppendLine();
			output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt." + projectOrSystem + "Header.multiline") );
			output.AppendLine();
			output.AppendLine();


			// Ignored Extensions

			if (config.HasIgnoredFileExtensions)
				{
				output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.IgnoredExtensionsHeader.multiline") );
				output.AppendLine();

				if (config.IgnoredFileExtensions.Count == 1)
					{  output.AppendLine("Ignore Extension: " + config.IgnoredFileExtensions[0]);  }
				else
					{
					output.Append("Ignore Extensions:");

					foreach (string ignoredExtension in config.IgnoredFileExtensions)
						{
						output.Append(' ');
						output.Append(ignoredExtension);
						}

					output.AppendLine();
					}

				output.AppendLine();
				output.AppendLine();
				}
			else
				{
				if (propertySource == Engine.Config.PropertySource.ProjectLanguagesFile)
					{
					output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.IgnoredExtensionsHeader.multiline") );
					output.AppendLine();

					output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.IgnoredExtensionsReference.multiline") );
					output.AppendLine();
					output.AppendLine();
					}
				// Add nothing for the system config file.
				}


			// Languages

			output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.LanguagesHeader.multiline") );

			if (config.HasLanguages)
				{
				output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.DeferredLanguagesReference.multiline") );
				output.AppendLine();

				foreach (var language in config.Languages)
					{
					if (language.AlterLanguage)
						{  output.Append("Alter ");  }

					output.AppendLine("Language: " + language.Name);

					int oldGroupNumber = 0;

					if (language.HasFileExtensions)
						{
						AppendLineBreakOnGroupChange(1, ref oldGroupNumber, output);
						AppendProperty("Extension", "Extensions", language.FileExtensionsPropertyChange,
												language.FileExtensions, output);
						}
					if (language.HasShebangStrings)
						{
						AppendLineBreakOnGroupChange(1, ref oldGroupNumber, output);
						AppendProperty("Shebang String", "Shebang Strings", language.ShebangStringsPropertyChange,
												language.ShebangStrings, output);
						}

					if (language.HasSimpleIdentifier)
						{
						AppendLineBreakOnGroupChange(2, ref oldGroupNumber, output);
						output.AppendLine("   Simple Identifier: " + language.SimpleIdentifier);
						}

					if (language.HasAliases)
						{
						AppendLineBreakOnGroupChange(2, ref oldGroupNumber, output);
						AppendProperty("Alias", "Aliases", language.AliasesPropertyChange,
												language.Aliases, output);
						}

					if (language.HasLineCommentSymbols)
						{
						AppendLineBreakOnGroupChange(3, ref oldGroupNumber, output);
						AppendProperty("Line Comment", language.LineCommentSymbols, output);
						}
					if (language.HasBlockCommentSymbols)
						{
						AppendLineBreakOnGroupChange(3, ref oldGroupNumber, output);
						AppendProperty("Block Comment", language.BlockCommentSymbols, output);
						}
					if (language.HasMemberOperator)
						{
						AppendLineBreakOnGroupChange(3, ref oldGroupNumber, output);
						output.AppendLine("   Member Operator: " + language.MemberOperator);
						}
					if (language.HasLineExtender)
						{
						AppendLineBreakOnGroupChange(3, ref oldGroupNumber, output);
						output.AppendLine("   Line Extender: " + language.LineExtender);
						}
					if (language.HasEnumValues)
						{
						AppendLineBreakOnGroupChange(3, ref oldGroupNumber, output);
						output.Append("   Enum Values: ");

						switch (language.EnumValues)
							{
							case Language.EnumValues.Global:
								output.AppendLine("Global");
								break;
							case Language.EnumValues.UnderParent:
								output.AppendLine("Under Parent");
								break;
							case Language.EnumValues.UnderType:
								output.AppendLine("Under Type");
								break;
							default:
								throw new NotImplementedException();
							}
						}
					if (language.HasCaseSensitive)
						{
						AppendLineBreakOnGroupChange(3, ref oldGroupNumber, output);
						output.Append("   Case Sensitive: ");

						switch (language.CaseSensitive)
							{
							case true:
								output.AppendLine("Yes");
								break;
							case false:
								output.AppendLine("No");
								break;
							default:
								throw new NotImplementedException();
							}
						}
					if (language.HasBlockCommentsNest)
						{
						AppendLineBreakOnGroupChange(3, ref oldGroupNumber, output);
						output.Append("   Block Comments Nest: ");

						switch (language.BlockCommentsNest)
							{
							case true:
								output.AppendLine("Yes");
								break;
							case false:
								output.AppendLine("No");
								break;
							default:
								throw new NotImplementedException();
							}
						}

					if (language.HasPrototypeEnders)
						{
						foreach (var enderGroup in language.PrototypeEnders)
							{
							AppendLineBreakOnGroupChange(4, ref oldGroupNumber, output);
							AppendProperty(enderGroup.CommentType + " Prototype Ender", enderGroup.CommentType + " Prototype Enders",
													enderGroup.EnderStrings, output);
							}
						}


					output.AppendLine();
					output.AppendLine();
					}
				}
			else // no languages
				{  output.AppendLine();  }


			output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt." + projectOrSystem + "LanguagesReference.multiline") );



			//
			// Compare with previous file and write to disk
			//

			return ConfigFile.SaveIfDifferent(filename, output.ToString(), noErrorOnFail: (errorList == null), errorList);
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


		/* Function: AppendProperty
		 * A shortcut function used only by <Save()> which appends a property which has a space separated list of values.
		 */
		private void AppendProperty (string propertyName, IList<string> values, System.Text.StringBuilder output)
			{
			output.Append("   " + propertyName + ":");

			foreach (var value in values)
				{
				output.Append(' ');
				output.Append(value);
				}

			output.AppendLine();
			}


		/* Function: AppendProperty
		 * A shortcut function used only by <Save()> which appends a property which has a space separated list of
		 * <BlockCommentSymbols>.
		 */
		private void AppendProperty (string propertyName, IList<BlockCommentSymbols> values, System.Text.StringBuilder output)
			{
			output.Append("   " + propertyName + ":");

			foreach (var value in values)
				{
				output.Append(' ');
				output.Append(value.OpeningSymbol);
				output.Append(' ');
				output.Append(value.ClosingSymbol);
				}

			output.AppendLine();
			}


		/* Function: AppendProperty
		 * A shortcut function used only by <Save()> which appends a property which has a space separated list of values and the
		 * property name has singular and plural forms.
		 */
		private void AppendProperty (string singularPropertyName, string pluralPropertyName,
												   IList<string> values, System.Text.StringBuilder output)
			{
			if (values.Count > 1)
				{  output.Append("   " + pluralPropertyName + ":");  }
			else
				{  output.Append("   " + singularPropertyName + ":");  }

			foreach (var value in values)
				{
				output.Append(' ');
				output.Append(value);
				}

			output.AppendLine();
			}


		/* Function: AppendProperty
		 * A shortcut function used only by <Save()> which appends a property which has a space separated list of values, the
		 * property name has singular and plural forms, and it also has "Add" and "Change" variants.
		 */
		private void AppendProperty (string singularPropertyName, string pluralPropertyName,
												   TextFileLanguage.PropertyChange propertyChange,
												   IList<string> values, System.Text.StringBuilder output)
			{
			string prefix;
			bool alwaysPlural = false;

			switch (propertyChange)
				{
				case TextFileLanguage.PropertyChange.None:
					prefix = "";
					break;
				case TextFileLanguage.PropertyChange.Add:
					prefix = "Add ";
					break;
				case TextFileLanguage.PropertyChange.Replace:
					prefix = "Replace ";
					alwaysPlural = true;
					break;
				default:
					throw new NotImplementedException();
				}

			if (alwaysPlural || values.Count > 1)
				{  output.Append("   " + prefix + pluralPropertyName + ":");  }
			else
				{  output.Append("   " + prefix + singularPropertyName + ":");  }

			foreach (var value in values)
				{
				output.Append(' ');
				output.Append(value);
				}

			output.AppendLine();
			}



		// Group: Regular Expressions
		// __________________________________________________________________________


		/* Regex: IsIgnoreExtensionsRegexLC
		 * Will match if the entire string is the property name "ignore extensions" or one of its acceptable variants.
		 * Assumes the input string is already in lowercase.
		 */
		[GeneratedRegex("""^ignored? (?:file )?extensions?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsIgnoreExtensionsRegexLC();


		/* Regex: IsAlterLanguageRegexLC
		 * Will match if the entire string is the property name "alter language" or one of its acceptable variants.  Assumes
		 * the input string is already in lowercase.
		 */
		[GeneratedRegex("""^(?:alter|edit|change) language$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsAlterLanguageRegexLC();


		/* Regex: IsAliasesRegexLC
		 * Will match if the entire string is the property name "aliases" or one of its acceptable variants.  Assumes the input
		 * string is already in lowercase.
		 */
		[GeneratedRegex("""^alias(?:es)?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsAliasesRegexLC();


		/* Regex: IsAddOrReplaceAliasesRegexLC
		 *
		 * Will match if the entire string is the property name "add alias", "replace alias", or one of their acceptable variants.
		 * Assumes the input string is already in lowercase.
		 *
		 * Capture Groups:
		 *
		 *		1 - The string "add" or "replace".  Since the input string is in all lowercase these will be as well.
		 */
		[GeneratedRegex("""^(add|replace) alias(?:es)?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsAddOrReplaceAliasesRegexLC();


		/* Regex: IsExtensionsRegexLC
		 * Will match if the entire string is the property name "extensions" or one of its acceptable variants.  Assumes the
		 * input string is already in lowercase.
		 */
		[GeneratedRegex("""^(?:file )?extensions?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsExtensionsRegexLC();


		/* Regex: IsAddOrReplaceExtensionsRegexLC
		 *
		 * Will match if the entire string is the property name "add extensions", "replace extensions", or one of their
		 * acceptable variants. Assumes the input string is already in lowercase.
		 *
		 * Capture Groups:
		 *
		 *		1 - The string "add" or "replace".  Since the input string is in all lowercase these will be as well.
		 */
		[GeneratedRegex("""^(add|replace) (?:file )?extensions?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsAddOrReplaceExtensionsRegexLC();


		/* Regex: IsShebangStringsRegexLC
		 * Will match if the entire string is the property name "shebang strings" or one of its acceptable variants.  Assumes
		 * the input string is already in lowercase.
		 */
		[GeneratedRegex("""^shebang strings?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsShebangStringsRegexLC();


		/* Regex: IsAddOrReplaceShebangStringsRegexLC
		 *
		 * Will match if the entire string is the property name "add shebang strings", "replace shebang strings", or one of
		 * their acceptable variants.  Assumes the input string is already in lowercase.
		 *
		 * Capture Groups:
		 *
		 *		1 - The string "add" or "replace".  Since the input string is in all lowercase these will be as well.
		 */
		[GeneratedRegex("""^(add|replace) shebang strings?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsAddOrReplaceShebangStringsRegexLC();


		/* Regex: IsLineCommentsRegexLC
		 * Will match if the entire string is the property name "line comments" or one of its acceptable variants.  Assumes
		 * the input string is already in lowercase.
		 */
		[GeneratedRegex("""^line comments?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsLineCommentsRegexLC();


		/* Regex: IsBlockCommentsRegexLC
		 * Will match if the entire string is the property name "block comments" or one of its acceptable variants.  Assumes
		 * the input string is already in lowercase.
		 */
		[GeneratedRegex("""^block comments?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsBlockCommentsRegexLC();


		/* Regex: IsMemberOperatorRegexLC
		 * Will match if the entire string is the property name "member operator" or one of its acceptable variants.  Assumes
		 * the input string is already in lowercase.
		 */
		[GeneratedRegex("""^(?:member operator|package separator)$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsMemberOperatorRegexLC();


		/* Regex: IsEnumValuesRegexLC
		 * Will match if the entire string is the property name "enum values" or one of its acceptable variants.  Assumes the
		 * input string is already in lowercase.
		 */
		[GeneratedRegex("""^enum values?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsEnumValuesRegexLC();


		/* Regex: IsCaseSensitiveRegexLC
		 * Will match if the entire string is the property name "case sensitive" or one of its acceptable variants.  Assumes the
		 * input string is already in lowercase.
		 */
		[GeneratedRegex("""^case[ -]sensitiv(?:e|ity)$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsCaseSensitiveRegexLC();


		/* Regex: IsBlockCommentsNestRegexLC
		 * Will match if the entire string is the property name "block comments nest" or one of its acceptable variants.
		 * Assumes the input string is already in lowercase.
		 */
		[GeneratedRegex("""^(?:block comments (?:can )?nest)|(?:nest(?:ed)? block comments)$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsBlockCommentsNestRegexLC();


		/* Regex: IsPrototypeEndersRegexLC
		 *
		 * Will match if the entire string is the property name "[type] prototype enders" or one of its acceptable variants.
		 * Assumes the input string is already in lowercase.
		 *
		 * Capture Groups:
		 *
		 *		1 - The name of the comment type.  Since the input string is in all lowercase these will be as well.
		 */
		[GeneratedRegex("""^(.+?) prototype enders?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsPrototypeEndersRegexLC();


		/* Regex: IsIgnorePrefixesRegexLC
		 * Will match if the entire string is the deprecated property name "ignore prefixes" or one of its acceptable variants.
		 * Assumes the input string is already in lowercase.
		 */
		[GeneratedRegex("""^(?:add |replace )?ignored?.*?prefix(?:es)? in index$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsIgnorePrefixesRegexLC();

		}
	}
