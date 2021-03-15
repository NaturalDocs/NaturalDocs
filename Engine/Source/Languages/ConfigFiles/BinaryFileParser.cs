/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.ConfigFiles.BinaryFileParser
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <Languages.nd>.
 * 
 * 
 * Multithreading: Not Thread Safe
 * 
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Languages.ConfigFiles
	{
	public class BinaryFileParser
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: BinaryFileParser
		 */
		public BinaryFileParser ()
			{
			}



		// Group: Loading Functions
		// __________________________________________________________________________


		/* Function: Load
		 * Loads the information in <Languages.nd> into a <Config> object, returning whether it was successful.  If it was not
		 * config will be null.
		 */
		public bool Load (Path filename, out Config config)
			{
			BinaryFile file = new BinaryFile();
			
			try
				{
				if (file.OpenForReading(filename, "2.2") == false)
					{
					config = null;
					return false;
					}
				else
					{
					config = new Config();
						
					// [String: Language Name]
					// [[Language Attributes]]
					// ...
					// [String: null]
						
					string languageName = file.ReadString();

					while (languageName != null)
						{
						Language language = new Language(languageName);
						
						// [Int32: ID]
						// [Byte: Type]
						// [String: Simple Identifier]
						// [Byte: Enum Values]
						// [Byte: Case Sensitive (1 or 0)]
						// [String: Member Operator Symbol]
						// [String: Line Extender Symbol]
					
						language.ID = file.ReadInt32();
						
						byte type = file.ReadByte();
						if (Enum.IsDefined(typeof(Language.LanguageType), type))
							{  language.Type = (Language.LanguageType)type;  }
						else
							{  
							config = null;
							return false;
							}
							
						language.SimpleIdentifier = file.ReadString();
						
						byte enumValues = file.ReadByte();
						if (Enum.IsDefined(typeof(Language.EnumValues), enumValues))
							{  language.EnumValue = (Language.EnumValues)enumValues;  }
						else
							{  
							config = null;
							return false;
							}
						
						language.CaseSensitive = (file.ReadByte() == 1);	
						
						language.MemberOperator = file.ReadString();
						language.LineExtender = file.ReadString();

						// [String: Line Comment Symbol] [] ... [String: null]
						// [String: Opening Block Comment Symbol] [String: Closing Block Comment Symbol] [] [] ... [String: null]
						// [String: Javadoc First Line Comment Symbol] [String: Javadoc Following Lines Comment Symbol [] [] ... [String: null]
						// [String: Javadoc Opening Block Comment Symbol] [String: Javadoc Closing Block Comment Symbol] [] [] ... [String: null]
						// [String: XML Line Comment Symbol] [] ... [String: null]
					
						var lineCommentSymbols = ReadSymbolList(file);
						if (lineCommentSymbols != null)
							{  language.LineCommentSymbols = lineCommentSymbols;  }

						var blockCommentSymbols = ReadBlockCommentSymbolsList(file);
						if (blockCommentSymbols != null)
							{  language.BlockCommentSymbols = blockCommentSymbols;  }

						var javadocLineCommentSymbols = ReadLineCommentSymbolsList(file);
						if (javadocLineCommentSymbols != null)
							{  language.JavadocLineCommentSymbols = javadocLineCommentSymbols;  }

						var javadocBlockCommentSymbols = ReadBlockCommentSymbolsList(file);
						if (javadocBlockCommentSymbols != null)
							{  language.JavadocBlockCommentSymbols = javadocBlockCommentSymbols;  }

						var xmlLineCommentSymbols = ReadSymbolList(file);
						if (xmlLineCommentSymbols != null)
							{  language.XMLLineCommentSymbols = xmlLineCommentSymbols;  }

						// Prototype Enders:
						// [Int32: Comment Type ID]
						// [Byte: Include Line Breaks (1 or 0)]
						// [String: Prototype Ender Symbol] [] ... [String: null]
						// ...
						// [Int32: 0]

						int commentTypeID = file.ReadInt32();

						while (commentTypeID != 0)
							{
							bool includeLineBreaks = (file.ReadByte() == 1);
							var enderSymbols = ReadSymbolList(file);

							language.AddPrototypeEnders( new PrototypeEnders(commentTypeID, enderSymbols, includeLineBreaks) );

							commentTypeID = file.ReadInt32();
							}
						
						config.AddLanguage(language);

						languageName = file.ReadString();
						}
						
					// [String: Alias] [Int32: Language ID] [] [] ... [String: Null]
					
					string alias = file.ReadString();
					
					while (alias != null)
						{
						int languageID = file.ReadInt32();
						config.AddAlias(alias, languageID);

						alias = file.ReadString();
						}
						
					// [String: File Extension] [Int32: Language ID] [] [] ... [String: Null]
					
					string fileExtension = file.ReadString();

					while (fileExtension != null)
						{
						int languageID = file.ReadInt32();
						config.AddFileExtension(fileExtension, languageID);

						fileExtension = file.ReadString();
						}
						
					// [String: Shebang String] [Int32: Language ID] [] [] ... [String: Null]

					string shebangString = file.ReadString();

					while (shebangString != null)
						{
						int languageID = file.ReadInt32();
						config.AddShebangString(shebangString, languageID);

						shebangString = file.ReadString();
						}
					}
				}
			catch
				{
				config = null;
				return false;
				}
			finally
				{
				file.Close();
				}
				
			return true;
			}
			
			
		/* Function: ReadSymbolList
		 * A helper function used only by <Load()> which reads a sequence of string symbols.  The sequence ends when a null 
		 * string is encountered.  If there are no strings in the sequence (the first one is null) it returns null instead of an empty 
		 * list.
		 */
		protected List<string> ReadSymbolList (BinaryFile file)
			{
			string symbol = file.ReadString();
			
			if (symbol == null)
				{  return null;  }
				
			List<string> symbolList = new List<string>();

			do			
				{
				symbolList.Add(symbol);
				symbol = file.ReadString();
				}
			while (symbol != null);
				
			return symbolList;
			}


		/* Function: ReadLineCommentSymbolsList
		 * A helper function used only by <Load()> which reads a sequence of <LineCommentSymbols>.  The sequence ends 
		 * when a null string is encountered.  If there are no strings in the sequence (the first one is null) it returns null instead
		 * of an empty list.
		 */
		protected List<LineCommentSymbols> ReadLineCommentSymbolsList (BinaryFile file)
			{
			string symbol = file.ReadString();
			
			if (symbol == null)
				{  return null;  }
				
			List<LineCommentSymbols> symbolList = new List<LineCommentSymbols>();

			do			
				{
				symbolList.Add( new LineCommentSymbols(symbol, file.ReadString()) );
				symbol = file.ReadString();
				}
			while (symbol != null);
				
			return symbolList;
			}

			
		/* Function: ReadBlockCommentSymbolsList
		 * A helper function used only by <Load()> which reads a sequence of <BlockCommentSymbols>.  The sequence ends 
		 * when a null string is encountered.  If there are no strings in the sequence (the first one is null) it returns null instead
		 * of an empty list.
		 */
		protected List<BlockCommentSymbols> ReadBlockCommentSymbolsList (BinaryFile file)
			{
			string symbol = file.ReadString();
			
			if (symbol == null)
				{  return null;  }
				
			List<BlockCommentSymbols> symbolList = new List<BlockCommentSymbols>();

			do			
				{
				symbolList.Add( new BlockCommentSymbols(symbol, file.ReadString()) );
				symbol = file.ReadString();
				}
			while (symbol != null);
				
			return symbolList;
			}

			

		// Group: Saving Functions
		// __________________________________________________________________________


		/* Function: Save
		 * Saves the current computed languages into <Languages.nd>.  Throws an exception if unsuccessful.
		 */
		public void Save (Path filename, Config config)
			{
			BinaryFile file = new BinaryFile();
			file.OpenForWriting(filename);

			try
				{

				// [String: Language Name]
				// [[Language Attributes]]
				// ...
				// [String: null]

				foreach (var language in config.Languages)
					{
					file.WriteString( language.Name );

					// [Int32: ID]
					// [Byte: Type]
					// [String: Simple Identifier]
					// [Byte: Enum Values]
					// [Byte: Case Sensitive (1 or 0)]
					// [String: Member Operator Symbol]
					// [String: Line Extender Symbol]
				
					file.WriteInt32( language.ID );
					file.WriteByte( (byte)language.Type );
					file.WriteString( language.SimpleIdentifier );
					file.WriteByte( (byte)language.EnumValue );
					file.WriteByte( (byte)(language.CaseSensitive ? 1 : 0) );
					file.WriteString( language.MemberOperator );
					file.WriteString( language.LineExtender );
					
					// [String: Line Comment Symbol] [] ... [String: null]
					// [String: Opening Block Comment Symbol] [String: Closing Block Comment Symbo] [] [] ... [String: null]
					// [String: Javadoc First Line Comment Symbol] [String: Javadoc Following Lines Comment Symbol [] ... [String: null]
					// [String: Javadoc Opening Block Comment Symbol] [String: Javadoc Closing Block Comment Symbol] [] [] ... [String: null]
					// [String: XML Line Comment Symbol] [] ... [String: null]
				
					WriteSymbolList(file, language.LineCommentSymbols);
					WriteBlockCommentSymbolsList(file, language.BlockCommentSymbols);
					WriteLineCommentSymbolsList(file, language.JavadocLineCommentSymbols);
					WriteBlockCommentSymbolsList(file, language.JavadocBlockCommentSymbols);
					WriteSymbolList(file, language.XMLLineCommentSymbols);

					// Prototype Enders:
					// [Int32: Comment Type ID]
					// [Byte: Include Line Breaks (0 or 1)]
					// [String: Prototype Ender Symbol] [] ... [String: null]
					// ...
					// [Int32: 0]

					if (language.HasPrototypeEnders)
						{
						foreach (var prototypeEnders in language.PrototypeEnders)
							{
							file.WriteInt32( prototypeEnders.CommentTypeID );
							file.WriteByte( (byte)(prototypeEnders.IncludeLineBreaks ? 1 : 0) );
							WriteSymbolList(file, prototypeEnders.Symbols);
							}
						}

					file.WriteInt32(0);					
					}
					
				file.WriteString(null);
				
				
				// [String: Alias] [Int32: Language ID] [] [] ... [String: Null]

				foreach (KeyValuePair<string, int> aliasKVP in config.Aliases)
					{
					file.WriteString( aliasKVP.Key );
					file.WriteInt32( aliasKVP.Value );
					}
					
				file.WriteString(null);
				
				
				// [String: File Extension] [Int32: Language ID] [] [] ... [String: Null]

				foreach (KeyValuePair<string, int> fileExtensionKVP in config.FileExtensions)
					{
					file.WriteString( fileExtensionKVP.Key );
					file.WriteInt32( fileExtensionKVP.Value );
					}
					
				file.WriteString(null);
				
				
				// [String: Shebang String] [Int32: Language ID] [] [] ... [String: Null]

				foreach (KeyValuePair<string, int> shebangStringKVP in config.ShebangStrings)
					{
					file.WriteString( shebangStringKVP.Key );
					file.WriteInt32( shebangStringKVP.Value );
					}
					
				file.WriteString(null);
				}
				
			finally
				{
				file.Close();
				}
			}
			
			
		/* Function: WriteSymbolList
		 * A helper function used only by <Save()> which writes a list of symbol strings to the file.  The strings are written in
		 * sequence and followed by a null string.  It is okay to pass null to this function, it will be treated as an empty list.
		 */
		private void WriteSymbolList (BinaryFile file, IList<string> symbolList)
			{
			if (symbolList != null)
				{
				foreach (var symbol in symbolList)
					{  file.WriteString(symbol);  }
				}
				
			file.WriteString(null);
			}

		/* Function: WriteLineCommentSymbolsList
		 * A helper function used only by <Save()> which writes a list of <LineCommentStrings> to the file.  The strings
		 * are written in sequence and followed by a null string.  It is okay to pass null to this function, it will be treated 
		 * as an empty array.
		 */
		private void WriteLineCommentSymbolsList (BinaryFile file, IList<LineCommentSymbols> lineCommentSymbolsList)
			{
			if (lineCommentSymbolsList != null)
				{
				foreach (var lineCommentSymbols in lineCommentSymbolsList)
					{  
					file.WriteString(lineCommentSymbols.FirstLineSymbol);
					file.WriteString(lineCommentSymbols.FollowingLinesSymbol);
					}
				}
				
			file.WriteString(null);
			}

		/* Function: WriteBlockCommentSymbolsList
		 * A helper function used only by <Save()> which writes a list of <BlockCommentSymbols> to the file.  The strings
		 * are written in sequence and followed by a null string.  It is okay to pass null to this function, it will be treated 
		 * as an empty array.
		 */
		private void WriteBlockCommentSymbolsList (BinaryFile file, IList<BlockCommentSymbols> blockCommentSymbolsList)
			{
			if (blockCommentSymbolsList != null)
				{
				foreach (var blockCommentSymbols in blockCommentSymbolsList)
					{  
					file.WriteString(blockCommentSymbols.OpeningSymbol);
					file.WriteString(blockCommentSymbols.ClosingSymbol);
					}
				}
				
			file.WriteString(null);
			}

		}
	}