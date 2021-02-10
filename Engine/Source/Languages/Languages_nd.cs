/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.Languages_nd
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <Languages.nd>.
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


namespace CodeClear.NaturalDocs.Engine.Languages
	{
	public class Languages_nd
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Languages_nd
		 */
		public Languages_nd (Languages.Manager manager)
			{
			languageManager = manager;
			}



		// Group: Loading Functions
		// __________________________________________________________________________


		/* Function: Load
		 * Loads the information in <Languages.nd>, which is the computed language settings from the last time Natural Docs 
		 * was run.  Returns whether it was successful.  If not all the out parameters will still return objects, they will just be 
		 * empty.  
		 */
		public bool Load (Path filename,
								 out List<Language> languages, 
								 out List<KeyValuePair<string, int>> aliases,
								 out List<KeyValuePair<string, int>> extensions,
								 out List<KeyValuePair<string, int>> shebangStrings, 
								 out List<string> ignoredExtensions)
			{
			languages = new List<Language>();
			
			aliases = new List<KeyValuePair<string,int>>();
			extensions = new List<KeyValuePair<string,int>>();
			shebangStrings = new List<KeyValuePair<string,int>>();
			ignoredExtensions = new List<string>();
			
			BinaryFile file = new BinaryFile();
			bool result = true;
			IDObjects.NumberSet usedLanguageIDs = new IDObjects.NumberSet();
			
			try
				{
				if (file.OpenForReading(filename, "2.0") == false)
					{
					result = false;
					}
				else
					{
						
					// [String: Language Name]
					// [Int32: ID]
					// [Byte: Type]
					// [String: Simple Identifier]
					// [Byte: Enum Values]
					// [Byte: Case Sensitive (1 or 0)]
					// [String: Member Operator Symbol]
					// [String: Line Extender Symbol]
					// [String: Line Comment Symbol] [] ... [String: null]
					// [String: Opening Block Comment Symbol] [String: Closing Block Comment Symbo] [] [] ... [String: null]
					// [String: Opening Javadoc Line Comment Symbol] [String: Remainder Javadoc Line Comment Symbol [] ... [String: null]
					// [String: Opening Javadoc Block Comment Symbol] [String: Closing Javadoc Block Comment Symbol] [] [] ... [String: null]
					// [String: XML Line Comment Symbol] [] ... [String: null]
					
					// [Int32: Comment Type ID]
					// [Byte: Include Line Breaks (1 or 0)]
					// [String: Prototype Ender Symbol] [] ... [String: null]
					// ...
					// [Int32: 0]
					
					// ...
					// [String: null]
						
					for (string languageName = file.ReadString();
						  languageName != null;
						  languageName = file.ReadString())
						{
						Language language = new Language(languageManager, languageName);
						
						language.ID = file.ReadInt32();
						
						byte rawTypeValue = file.ReadByte();
						if (Enum.IsDefined(typeof(Language.LanguageType), rawTypeValue))
							{  language.Type = (Language.LanguageType)rawTypeValue;  }
						else
							{  result = false;  }
							
						language.SimpleIdentifier = file.ReadString();
						
						byte rawEnumValue = file.ReadByte();
						if (Enum.IsDefined(typeof(Language.EnumValues), rawEnumValue))
							{  language.EnumValue = (Language.EnumValues)rawEnumValue;  }
						else
							{  result = false;  }
						
						language.CaseSensitive = (file.ReadByte() == 1);	
						
						language.MemberOperator = file.ReadString();
						language.LineExtender = file.ReadString();

						language.LineCommentStrings = ReadStringArray(file);
						language.BlockCommentStringPairs = ReadStringArray(file);
						language.JavadocLineCommentStringPairs = ReadStringArray(file);
						language.JavadocBlockCommentStringPairs = ReadStringArray(file);
						language.XMLLineCommentStrings = ReadStringArray(file);
							
						for (int commentTypeID = file.ReadInt32();
							  commentTypeID != 0;
							  commentTypeID = file.ReadInt32())
							{
							bool includeLineBreaks = (file.ReadByte() == 1);
							string[] enderSymbols = ReadStringArray(file);

							language.SetPrototypeEnders(commentTypeID, new PrototypeEnders(enderSymbols, includeLineBreaks));
							}
						
						languages.Add(language);
						usedLanguageIDs.Add(language.ID);
						}

						
					// [String: Alias] [Int32: Language ID] [] [] ... [String: Null]
					
					for (string alias = file.ReadString();
						  alias != null;
						  alias = file.ReadString())
						{
						int languageID = file.ReadInt32();
						
						if (usedLanguageIDs.Contains(languageID) == true)
							{
							aliases.Add( new KeyValuePair<string, int>(alias, languageID) );
							}
						else
							{
							result = false;
							}
						}
						
					// [String: Extension] [Int32: Language ID] [] [] ... [String: Null]
					
					for (string extension = file.ReadString();
						  extension != null;
						  extension = file.ReadString())
						{
						int languageID = file.ReadInt32();
						
						if (usedLanguageIDs.Contains(languageID) == true)
							{
							extensions.Add( new KeyValuePair<string, int>(extension, languageID) );
							}
						else
							{
							result = false;
							}
						}
						
					// [String: Shebang String] [Int32: Language ID] [] [] ... [String: Null]

					for (string shebangString = file.ReadString();
						  shebangString != null;
						  shebangString = file.ReadString())
						{
						int languageID = file.ReadInt32();
						
						if (usedLanguageIDs.Contains(languageID) == true)
							{
							shebangStrings.Add( new KeyValuePair<string, int>(shebangString, languageID) );
							}
						else
							{
							result = false;
							}
						}

					// [String: Ignored Extension] [] ... [String: Null]

					for (string ignoredExtension = file.ReadString();
						  ignoredExtension != null;
						  ignoredExtension = file.ReadString())
						{
						ignoredExtensions.Add(ignoredExtension);
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
				languages.Clear();

				extensions.Clear();
				shebangStrings.Clear();
				ignoredExtensions.Clear();				
				}
				
			return result;
			}
			
			
		/* Function: ReadStringArray
		 * A helper function used only by <Load()> which loads a sequence of strings into an array.  The sequence ends when a
		 * null string is encountered.  If there are no strings in the sequence (the first one is null) it returns null instead of an
		 * empty array.
		 */
		private string[] ReadStringArray (BinaryFile file)
			{
			string stringFromFile = file.ReadString();
			
			if (stringFromFile == null)
				{  return null;  }
				
			List<string> stringList = new List<string>();

			do			
				{
				stringList.Add(stringFromFile);
				stringFromFile = file.ReadString();
				}
			while (stringFromFile != null);
				
			return stringList.ToArray();
			}

			

		// Group: Saving Functions
		// __________________________________________________________________________


		/* Function: Save
		 * Saves the current computed languages into <Languages.nd>.  Throws an exception if unsuccessful.
		 */
		public void Save (Path filename, IDObjects.Manager<Language> languages,
								 StringTable<Language> aliases, StringTable<Language> extensions, 
								 SortedStringTable<Language> shebangStrings, StringSet ignoredExtensions)
			{
			BinaryFile file = new BinaryFile();
			file.OpenForWriting(filename);

			try
				{

				// [String: Language Name]
				// [Int32: ID]
				// [Byte: Type]
				// [String: Simple Identifier]
				// [Byte: Enum Values]
				// [Byte: Case Sensitive (1 or 0)]
				// [String: Member Operator Symbol]
				// [String: Line Extender Symbol]
				// [String: Line Comment Symbol] [] ... [String: null]
				// [String: Opening Block Comment Symbol] [String: Closing Block Comment Symbo] [] [] ... [String: null]
				// [String: Opening Javadoc Line Comment Symbol] [String: Remainder Javadoc Line Comment Symbol [] ... [String: null]
				// [String: Opening Javadoc Block Comment Symbol] [String: Closing Javadoc Block Comment Symbol] [] [] ... [String: null]
				// [String: XML Line Comment Symbol] [] ... [String: null]
				
				// [Int32: Comment Type ID]
				// [Byte: Include Line Breaks (0 or 1)]
				// [String: Prototype Ender Symbol] [] ... [String: null]
				// ...
				// [Int32: 0]
				
				// ...
				// [String: null]

				foreach (Language language in languages)
					{
					file.WriteString( language.Name );
					file.WriteInt32( language.ID );
					file.WriteByte( (byte)language.Type );
					file.WriteString( language.SimpleIdentifier );
					file.WriteByte( (byte)language.EnumValue );
					file.WriteByte( (byte)(language.CaseSensitive ? 1 : 0) );
					file.WriteString( language.MemberOperator );
					file.WriteString( language.LineExtender );
					
					WriteStringArray(file, language.LineCommentStrings);
					WriteStringArray(file, language.BlockCommentStringPairs);
					WriteStringArray(file, language.JavadocLineCommentStringPairs);
					WriteStringArray(file, language.JavadocBlockCommentStringPairs);
					WriteStringArray(file, language.XMLLineCommentStrings);

					int[] commentTypes = language.GetCommentTypesWithPrototypeEnders();
					if (commentTypes != null)
						{
						foreach (int commentType in commentTypes)
							{
							PrototypeEnders prototypeEnders = language.GetPrototypeEnders(commentType);

							file.WriteInt32(commentType);
							file.WriteByte( (byte)(prototypeEnders.IncludeLineBreaks ? 1 : 0) );
							WriteStringArray(file, prototypeEnders.Symbols);
							}
						}
					file.WriteInt32(0);					
					}
					
				file.WriteString(null);
				
				
				// [String: Alias] [Int32: Language ID] [] [] ... [String: Null]

				foreach (KeyValuePair<string, Language> pair in aliases)
					{
					file.WriteString( pair.Key );
					file.WriteInt32( pair.Value.ID );
					}
					
				file.WriteString(null);
				
				
				// [String: Extension] [Int32: Language ID] [] [] ... [String: Null]

				foreach (KeyValuePair<string, Language> pair in extensions)
					{
					file.WriteString( pair.Key );
					file.WriteInt32( pair.Value.ID );
					}
					
				file.WriteString(null);
				
				
				// [String: Shebang String] [Int32: Language ID] [] [] ... [String: Null]

				foreach (KeyValuePair<string, Language> pair in shebangStrings)
					{
					file.WriteString( pair.Key );
					file.WriteInt32( pair.Value.ID );
					}
					
				file.WriteString(null);
				
				
				// [String: Ignored Extension] [] ... [String: Null]

				foreach (string ignoredExtension in ignoredExtensions)
					{
					file.WriteString( ignoredExtension );
					}
					
				file.WriteString(null);
				}
				
			finally
				{
				file.Close();
				}
			}
			
			
		/* Function: WriteStringArray
		 * A helper function used only by <Save()> which writes a string array to the file.  The strings are written
		 * in sequence and followed by a null string.  It is okay to pass null to this function, it will be treated as an
		 * empty array.
		 */
		private void WriteStringArray (BinaryFile file, string[] stringArray)
			{
			if (stringArray != null)
				{
				foreach (string stringFromArray in stringArray)
					{  file.WriteString(stringFromArray);  }
				}
				
			file.WriteString(null);
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Languages.Manager languageManager;
		}
	}