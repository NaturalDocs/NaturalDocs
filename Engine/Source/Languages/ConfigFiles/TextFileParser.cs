/* 
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

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Languages.ConfigFiles
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

			nonASCIILettersRegex = new Regex.NonASCIILetters();

			addReplaceAliasesRegex = new Regex.Languages.AddReplaceAliases();
			addReplaceExtensionsRegex = new Regex.Languages.AddReplaceExtensions();
			addReplaceShebangStringsRegex = new Regex.Languages.AddReplaceShebangStrings();
			aliasesRegex = new Regex.Languages.Aliases();
			alterLanguageRegex = new Regex.Languages.AlterLanguage();
			blockCommentsRegex = new Regex.Languages.BlockComments();
			enumValuesRegex = new Regex.Languages.EnumValues();
			extensionsRegex = new Regex.Languages.Extensions();
			ignorePrefixesRegex = new Regex.Languages.IgnorePrefixes();
			ignoreExtensionsRegex = new Regex.Languages.IgnoreExtensions();
			lineCommentsRegex = new Regex.Languages.LineComments();
			prototypeEndersRegex = new Regex.Languages.PrototypeEnders();
			shebangStringsRegex = new Regex.Languages.ShebangStrings();
			memberOperatorRegex = new Regex.Languages.MemberOperator();
			caseSensitiveRegex = new Regex.Languages.CaseSensitive();
			}



		// Group: Loading Functions
		// __________________________________________________________________________


		/* Function: Load
		 * 
		 * Loads the configuration file and parses it.  Redundant information will be simplified out, such as an Alter
		 * Language section that applies to a language defined in the same file.
		 * 
		 * Parameters:
		 * 
		 *		filename - The <Path> where the file is located.
		 *		propertySource - The <Config.PropertySource> associated with the file.
		 *		fileLanguages - Returns a list of <ConfigFileLanguages> in no particular order.
		 *		fileIgnoredExtensions - Returns any ignored extensions as a string array.
		 *		errorList - If it couldn't successfully parse the file it will add error messages to this list.
		 *		
		 * Returns:
		 * 
		 *		Whether it was able to successfully load and parse the file without any errors.
		 */
		public bool Load (Path filename, Config.PropertySource propertySource,
								 out List<TextFileLanguage> fileLanguages, out List<string> fileIgnoredExtensions, 
								 Errors.ErrorList errorList)
			{
			fileLanguages = new List<TextFileLanguage>();
			fileIgnoredExtensions = new List<string>();
			StringTable<TextFileLanguage> fileLanguageNames = 
				new StringTable<TextFileLanguage>(Engine.Languages.Manager.KeySettingsForLanguageName);

			int previousErrorCount = errorList.Count;

			using (ConfigFile file = new ConfigFile())
				{
				// Can't make identifiers lowercase here or we'd lose the case of the comment type in prototype ender lines.
				bool openResult = file.Open(filename, 
														 propertySource,
														 ConfigFile.FileFormatFlags.CondenseIdentifierWhitespace |
														 ConfigFile.FileFormatFlags.CondenseValueWhitespace,
														 errorList);
														 
				if (openResult == false)
					{  return false;  }
					
				string identifier, lcIdentifier, value;
				TextFileLanguage currentLanguage = null;
				
				// We need this in addition to ConfigFileLanguage.AlterLanguage because an entry altering a type defined in the 
				// same file would be combined into the original, yet we still need to know if that entry is Alter to properly
				// detect whether we need to use Add/Replace with certain properties.
				bool alterCurrentLanguage = false;
				
				char[] space = { ' ' };
				
				System.Text.RegularExpressions.Match match;
				

				while (file.Get(out identifier, out value))
					{
					lcIdentifier = identifier.ToLower();

					//
					// Ignore Extensions
					//
					
					if (ignoreExtensionsRegex.IsMatch(lcIdentifier))
						{
						currentLanguage = null;
						
						string[] ignoredExtensionsArray = value.Split(space);
						fileIgnoredExtensions.AddRange(ignoredExtensionsArray);
						}
					
					
					//
					// Language
					//
						
					else if (lcIdentifier == "language")
						{
						if (fileLanguageNames.ContainsKey(value))
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.LanguageAlreadyExists(name)", value)
								);
							
							// Continue parsing.  We'll throw this into the existing language even though it shouldn't be overwriting
							// its values because we want to find any other errors there are in the file.
							currentLanguage = fileLanguageNames[value];
							alterCurrentLanguage = false;
							}
							
						else
							{
							currentLanguage = new TextFileLanguage(value, false, file.LineNumber);
							alterCurrentLanguage = false;
							fileLanguages.Add(currentLanguage);
							fileLanguageNames.Add(value, currentLanguage);
							}								
						}
						
						
						
					//
					// Alter Language
					//
					
					else if (alterLanguageRegex.IsMatch(lcIdentifier))
						{
						// If this language already exists, collapse it into the current definition.
						if (fileLanguageNames.ContainsKey(value))
							{
							currentLanguage = fileLanguageNames[value];
							alterCurrentLanguage = true;
							}
							
						// If it doesn't exist, create the new language anyway with the alter flag set because it may exist in another
						// file.
						else
							{
							currentLanguage = new TextFileLanguage(value, true, file.LineNumber);
							alterCurrentLanguage = true;
							fileLanguages.Add(currentLanguage);
							fileLanguageNames.Add(value, currentLanguage);
							}								
						}


					//
					// Aliases
					//
						
					else if (aliasesRegex.IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
						else if (alterCurrentLanguage == true)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.NeedAddReplaceWhenAlteringLanguage(keyword)", "Aliases")
								);
							}
						else
							{
							currentLanguage.Aliases = value.Split(space);
							currentLanguage.AddAliases = false;
							}
						}
						
						
					//
					// Add/Replace Aliases
					//
					
					else if ( (match = addReplaceAliasesRegex.Match(lcIdentifier)) != null && match.Success )
						{
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
							
						else if (alterCurrentLanguage == true && match.Groups[1].Value == "add" && 
								  currentLanguage.Aliases != null)
							{
							string[] addAliases = value.Split(space);
							string[] newAliases = new string[ addAliases.Length + currentLanguage.Aliases.Length ];
							
							currentLanguage.Aliases.CopyTo(newAliases, 0);
							addAliases.CopyTo(newAliases, currentLanguage.Aliases.Length);
							
							currentLanguage.Aliases = newAliases;
							currentLanguage.AddAliases = true;
							}
						
						// Covers "replace" when altering a language, "add" and "replace" when not altering a language (no point
						// in adding an error when we can just tolerate it, and "replace" when altering a language that doesn't have
						// anything defined.
						else
							{
							currentLanguage.Aliases = value.Split(space);
							currentLanguage.AddAliases = (match.Groups[1].Value == "add");
							}
						}
						


					//
					// Extensions
					//
						
					else if (extensionsRegex.IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
						else if (alterCurrentLanguage == true)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.NeedAddReplaceWhenAlteringLanguage(keyword)", "Extensions")
								);
							}
						else
							{
							currentLanguage.Extensions = value.Split(space);
							currentLanguage.AddExtensions = false;
							}
						}
						
						
					//
					// Add/Replace Extensions
					//
					
					else if ( (match = addReplaceExtensionsRegex.Match(lcIdentifier)) != null && match.Success )
						{
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
							
						else if (alterCurrentLanguage == true && match.Groups[1].Value == "add" && 
								  currentLanguage.Extensions != null)
							{
							string[] addExtensions = value.Split(space);
							string[] newExtensions = new string[ addExtensions.Length + currentLanguage.Extensions.Length ];
							
							currentLanguage.Extensions.CopyTo(newExtensions, 0);
							addExtensions.CopyTo(newExtensions, currentLanguage.Extensions.Length);
							
							currentLanguage.Extensions = newExtensions;
							currentLanguage.AddExtensions = true;
							}
						
						// Covers "replace" when altering a language, "add" and "replace" when not altering a language (no point
						// in adding an error when we can just tolerate it, and "replace" when altering a language that doesn't have
						// anything defined.
						else
							{
							currentLanguage.Extensions = value.Split(space);
							currentLanguage.AddExtensions = (match.Groups[1].Value == "add");
							}
						}
						


					//
					// Shebang Strings
					//
						
					else if (shebangStringsRegex.IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
						else if (alterCurrentLanguage == true)
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.NeedAddReplaceWhenAlteringLanguage(keyword)", "Shebang Strings")
								);
							}
						else
							{
							currentLanguage.ShebangStrings = value.Split(space);
							currentLanguage.AddShebangStrings = false;
							}
						}
						
						
					//
					// Add/Replace Shebang Strings
					//
					
					else if ( (match = addReplaceShebangStringsRegex.Match(lcIdentifier)) != null && match.Success )
						{
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
							
						else if (alterCurrentLanguage == true && match.Groups[1].Value == "add" && 
								  currentLanguage.ShebangStrings != null)
							{
							string[] addShebangStrings = value.Split(space);
							string[] newShebangStrings = new string[ addShebangStrings.Length + 
																						 currentLanguage.ShebangStrings.Length ];
							
							currentLanguage.ShebangStrings.CopyTo(newShebangStrings, 0);
							addShebangStrings.CopyTo(newShebangStrings, currentLanguage.ShebangStrings.Length);
							
							currentLanguage.ShebangStrings = newShebangStrings;
							currentLanguage.AddShebangStrings = true;
							}
						
						// Covers "replace" when altering a language, "add" and "replace" when not altering a language (no point
						// in adding an error when we can just tolerate it, and "replace" when altering a language that doesn't have
						// anything defined.
						else
							{
							currentLanguage.ShebangStrings = value.Split(space);
							currentLanguage.AddShebangStrings = (match.Groups[1].Value == "add");
							}
						}
						


					//
					// Simple Identifier
					//
					
					else if (lcIdentifier == "simple identifier")
						{
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
						else if (nonASCIILettersRegex.IsMatch(value))
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.SimpleIdentifierMustOnlyBeASCIILetters(name)", value)
								);
							}
						else
							{
							currentLanguage.SimpleIdentifier = value;
							}
						}
						


					//
					// Line Comments
					//
					
					else if (lineCommentsRegex.IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
						else
							{
							currentLanguage.LineCommentStrings = value.Split(space);
							}
						}
						


					//
					// Block Comments
					//
					
					else if (blockCommentsRegex.IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
						else
							{
							string[] newBlockCommentStrings = value.Split(space);
							
							if (newBlockCommentStrings.Length % 2 != 0)
								{
								file.AddError(
									Locale.Get("NaturalDocs.Engine", "Languages.txt.BlockCommentsMustHaveAnEvenNumberOfSymbols")
									);
								}
							else
								{
								currentLanguage.BlockCommentStringPairs = newBlockCommentStrings;
								}
							}
						}
						


					//
					// Member Operator
					//
					
					else if (memberOperatorRegex.IsMatch(lcIdentifier))
						{
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
						else
							{  currentLanguage.MemberOperator = value;  }
						}
						
						
						
					//
					// Line Extender
					//
					
					else if (lcIdentifier == "line extender")
						{
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
						else
							{  currentLanguage.LineExtender = value;  }
						}
						
						
						
					//
					// Enum Values
					//
					
					else if (enumValuesRegex.IsMatch(lcIdentifier))
						{
						string lcValue = value.ToLower();
						
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
						else if (lcValue == "global")
							{  currentLanguage.EnumValue = Language.EnumValues.Global;  }
						else if (lcValue == "under type")
							{  currentLanguage.EnumValue = Language.EnumValues.UnderType;  }
						else if (lcValue == "under parent")
							{  currentLanguage.EnumValue = Language.EnumValues.UnderParent;  }
						else
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.InvalidEnumValue(value)", value)
								);
							}
						}
						
						
					//
					// Case Sensitive
					//
					
					else if (caseSensitiveRegex.IsMatch(lcIdentifier))
						{
						string lcValue = value.ToLower();
						
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
						else if (yesRegex.IsMatch(lcValue))
							{  currentLanguage.CaseSensitive = true;  }
						else if (noRegex.IsMatch(lcValue))
							{  currentLanguage.CaseSensitive = false;  }
						else
							{
							file.AddError(
								Locale.Get("NaturalDocs.Engine", "Languages.txt.UnrecognizedValue(keyword, value)", "Case Sensitive", value)
								);
							}
						}
						
						
					//
					// Prototype Enders
					//
					
					// Use identifier and not lcIdentifier to keep the case of the comment type.  The regex will compensate.
					else if ( (match = prototypeEndersRegex.Match(identifier)) != null && match.Success )
						{
						if (currentLanguage == null)
							{  NeedsLanguageError(file, identifier);  }
						else
							{
							string commentType = match.Groups[1].Value;
							string[] enderStrings = value.Split(space);

							currentLanguage.SetPrototypeEnderStrings(commentType, enderStrings);
							}
						}
						
						
					//
					// Deprecated keywords
					//
					
					else if ( ignorePrefixesRegex.IsMatch(lcIdentifier) || 
								lcIdentifier == "perl package" || lcIdentifier == "full language support" )
						{
						// Ignore
						}
						
						
					//
					// Unrecognized keywords
					//
					
					else
					    {
					    file.AddError(
					        Locale.Get("NaturalDocs.Engine", "Languages.txt.UnrecognizedKeyword(keyword)", identifier)
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
			

		/* Function: NeedsLanguageError
		 * A shortcut function only used by <Load()> which adds an error stating that the passed keyword needs to appear
		 * in a language section.
		 */
		private void NeedsLanguageError (ConfigFile file, string identifier)
			{
			file.AddError(
				Locale.Get("NaturalDocs.Engine", "Languages.txt.KeywordMustBeInLanguage(keyword)", identifier)
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
		 *		languages - A list of <ConfigFileLanguages>.  
		 *		ignoredExtensions - A string array of ignored extensions in the format of.
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
		public bool Save (Path filename, List<TextFileLanguage> languages, 
								 List<string> ignoredExtensions, Errors.ErrorList errorList, bool isProjectFile, bool noErrorOnFail)
			{
			System.Text.StringBuilder output = new System.Text.StringBuilder(1024);
			string projectSystem = (isProjectFile ? "Project" : "System");
			
			
			//
			// Create header
			//
			
			output.AppendLine("Format: " + Engine.Instance.VersionString);
			output.AppendLine();
			output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt." + projectSystem + "Header.multiline") );
			output.AppendLine();
			output.AppendLine();
			
			if (ignoredExtensions.Count > 0)
				{
				output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.IgnoredExtensionsHeader.multiline") );
				output.AppendLine();
				
				if (ignoredExtensions.Count == 1)
					{  output.AppendLine("Ignore Extension: " + ignoredExtensions[0]);  }
				else
					{  
					output.Append("Ignore Extensions:");

					foreach (string extension in ignoredExtensions)
						{  output.Append(" " + extension);  }
						
					output.AppendLine();
					}

				output.AppendLine();
				output.AppendLine();
				}
			else if (isProjectFile)
				{
				output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.IgnoredExtensionsHeader.multiline") );
				output.AppendLine();

				output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.IgnoredExtensionsReference.multiline") );
				output.AppendLine();
				output.AppendLine();
				}
			// Add nothing for the system config file.
			

			output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.LanguagesHeader.multiline") );

			if (languages.Count > 1)
				{  output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt.DeferredLanguagesReference.multiline") );  }

			output.AppendLine();
				
			
			//
			// Create content
			//
			
			languages.Sort( 
				delegate (TextFileLanguage a, TextFileLanguage b)
					{  return a.LineNumber - b.LineNumber;  } 
				);
				
			foreach (var language in languages)
				{
				if (language.AlterLanguage == true)
					{  output.Append("Alter ");  }
					
				output.AppendLine("Language: " + language.Name);
				
				int oldGroupNumber = 0;

				if (language.Extensions != null)
					{
					LineBreakOnGroupChange(1, ref oldGroupNumber, output);
					
					if (language.AlterLanguage == true)
						{
						if (language.AddExtensions == true)
							{  
							if (language.Extensions.Length == 1)
								{  output.Append("   Add Extension: ");  }
							else
								{  output.Append("   Add Extensions: ");  }
							}
						else
							{  output.Append("   Replace Extensions: ");  }  // Regardless of singular or plural
						}
					else
						{  
						if (language.Extensions.Length == 1)
							{  output.Append("   Extension: ");  }
						else
							{  output.Append("   Extensions: ");  }
						}
					
					output.AppendLine(string.Join(" ", language.Extensions));
					}
					
				if (language.ShebangStrings != null)
					{
					LineBreakOnGroupChange(1, ref oldGroupNumber, output);

					if (language.AlterLanguage == true)
						{
						if (language.AddShebangStrings == true)
							{  
							if (language.ShebangStrings.Length == 1)
								{  output.Append("   Add Shebang String: ");  }
							else
								{  output.Append("   Add Shebang Strings: ");  }
							}
						else
							{  output.Append("   Replace Shebang Strings: ");  }  // Regardless of singular or plural
						}
					else
						{  
						if (language.ShebangStrings.Length == 1)
							{  output.Append("   Shebang String: ");  }
						else
							{  output.Append("   Shebang Strings: ");  }
						}

					output.AppendLine(string.Join(" ", language.ShebangStrings));
					}
					
				if (language.SimpleIdentifier != null)
					{  
					LineBreakOnGroupChange(2, ref oldGroupNumber, output);
					output.AppendLine("   Simple Identifier: " + language.SimpleIdentifier);  
					}
					
				if (language.Aliases != null)
					{
					LineBreakOnGroupChange(2, ref oldGroupNumber, output);

					if (language.AlterLanguage == true)
						{
						if (language.AddAliases == true)
							{  
							if (language.Aliases.Length == 1)
								{  output.Append("   Add Alias: ");  }
							else
								{  output.Append("   Add Aliases: ");  }
							}
						else
							{  output.Append("   Replace Aliases: ");  }  // Regardless of singular or plural
						}
					else
						{  
						if (language.Aliases.Length == 1)
							{  output.Append("   Alias: ");  }
						else
							{  output.Append("   Aliases: ");  }
						}

					output.AppendLine(string.Join(" ", language.Aliases));
					}
					
				if (language.LineCommentStrings != null)
					{  
					LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					output.AppendLine("   Line Comment: " + string.Join(" ", language.LineCommentStrings));  
					}
				if (language.BlockCommentStringPairs != null)
					{  
					LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					output.AppendLine("   Block Comment: " + string.Join(" ", language.BlockCommentStringPairs));  
					}
				if (language.MemberOperator != null)
					{  
					LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					output.AppendLine("   Member Operator: " + language.MemberOperator);  
					}
				if (language.LineExtender != null)
					{  
					LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					output.AppendLine("   Line Extender: " + language.LineExtender);  
					}
				if (language.EnumValue != null)
					{  
					LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					output.Append("   Enum Values: ");
					
					if (language.EnumValue == Language.EnumValues.Global)
						{  output.AppendLine("Global");  }
					else if (language.EnumValue == Language.EnumValues.UnderParent)
						{  output.AppendLine("Under Parent");  }
					else // Language.EnumValues.UnderType
						{  output.AppendLine("Under Type");  }
					}
				if (language.CaseSensitive != null)
					{  
					LineBreakOnGroupChange(3, ref oldGroupNumber, output);
					output.AppendLine("   Case Sensitive: " + ((language.CaseSensitive != null && (bool)language.CaseSensitive == true) ? "Yes" : "No"));  
					}
				
				string[] commentTypeNamesWithPrototypeEnders = language.GetCommentTypeNamesWithPrototypeEnders();
				
				if (commentTypeNamesWithPrototypeEnders != null)
					{
					LineBreakOnGroupChange(4, ref oldGroupNumber, output);

					foreach (string commentTypeName in commentTypeNamesWithPrototypeEnders)
						{
						string[] prototypeEnderStrings = language.GetPrototypeEnderStrings(commentTypeName);
						
						if (prototypeEnderStrings.Length == 1)
							{  
							output.AppendLine( "   " + commentTypeName + " Prototype Ender: " + prototypeEnderStrings[0] );  
							}
						else
							{  
							output.AppendLine( "   " + commentTypeName + " Prototype Enders: " + string.Join(" ", prototypeEnderStrings) );  
							}
						}
					}

				output.AppendLine();
				output.AppendLine();
				}


			output.Append( Locale.Get("NaturalDocs.Engine", "Languages.txt." + projectSystem + "LanguagesReference.multiline") );

				
				
			//
			// Compare with previous file and write to disk
			//
			
			return ConfigFile.SaveIfDifferent(filename, output.ToString(), noErrorOnFail, errorList);
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

		protected Regex.NonASCIILetters nonASCIILettersRegex;

		protected Regex.Languages.AddReplaceAliases addReplaceAliasesRegex;
		protected Regex.Languages.AddReplaceExtensions addReplaceExtensionsRegex;
		protected Regex.Languages.AddReplaceShebangStrings addReplaceShebangStringsRegex;
		protected Regex.Languages.Aliases aliasesRegex;
		protected Regex.Languages.AlterLanguage alterLanguageRegex;
		protected Regex.Languages.BlockComments blockCommentsRegex;
		protected Regex.Languages.EnumValues enumValuesRegex;
		protected Regex.Languages.Extensions extensionsRegex;
		protected Regex.Languages.IgnorePrefixes ignorePrefixesRegex;
		protected Regex.Languages.IgnoreExtensions ignoreExtensionsRegex;
		protected Regex.Languages.LineComments lineCommentsRegex;
		protected Regex.Languages.PrototypeEnders prototypeEndersRegex;
		protected Regex.Languages.ShebangStrings shebangStringsRegex;
		protected Regex.Languages.MemberOperator memberOperatorRegex;
		protected Regex.Languages.CaseSensitive caseSensitiveRegex;
		 
		}
	}