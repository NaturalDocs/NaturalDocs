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

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
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
			BinaryFile binaryFile = new BinaryFile();

			try
				{
				if (binaryFile.OpenForReading(filename) == false)
					{
					config = null;
					return false;
					}
				else if (binaryFile.Version.IsAtLeastRelease("2.2") == false &&  // can handle changes in 2.3.1
						   binaryFile.Version.IsSamePreRelease(Engine.Instance.Version) == false)
					{
					binaryFile.Close();
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

					string languageName = binaryFile.ReadString();

					while (languageName != null)
						{
						Language language = new Language(languageName);

						// [Int32: ID]
						// [Byte: Type]
						// [String: Simple Identifier]
						// [Byte: Enum Values]
						// [Byte: Case Sensitive (1 or 0)]
						// [Byte: Block Comments Nest (1 or 0)]
						// [String: Member Operator Symbol]
						// [String: Line Extender Symbol]

						language.ID = binaryFile.ReadInt32();

						byte type = binaryFile.ReadByte();
						if (Enum.IsDefined(typeof(Language.LanguageType), type))
							{  language.Type = (Language.LanguageType)type;  }
						else
							{
							config = null;
							return false;
							}

						language.SimpleIdentifier = binaryFile.ReadString();

						byte enumValues = binaryFile.ReadByte();
						if (Enum.IsDefined(typeof(Language.EnumValues), enumValues))
							{  language.EnumValue = (Language.EnumValues)enumValues;  }
						else
							{
							config = null;
							return false;
							}

						language.CaseSensitive = (binaryFile.ReadByte() == 1);

						if (binaryFile.Version.IsAtLeastRelease("2.3.1") ||
							binaryFile.Version.IsSamePreRelease(Engine.Instance.Version))
							{
							language.BlockCommentsNest = (binaryFile.ReadByte() == 1);
							}
						else
							{
							language.BlockCommentsNest = false;
							}

						language.MemberOperator = binaryFile.ReadString();
						language.LineExtender = binaryFile.ReadString();

						// [String: Line Comment Symbol] [] ... [String: null]
						// [String: Opening Block Comment Symbol] [String: Closing Block Comment Symbol] [] [] ... [String: null]
						// [String: Javadoc First Line Comment Symbol] [String: Javadoc Following Lines Comment Symbol [] [] ... [String: null]
						// [String: Javadoc Opening Block Comment Symbol] [String: Javadoc Closing Block Comment Symbol] [] [] ... [String: null]
						// [String: XML Line Comment Symbol] [] ... [String: null]

						var lineCommentSymbols = ReadSymbolList(binaryFile);
						if (lineCommentSymbols != null)
							{  language.LineCommentSymbols = lineCommentSymbols;  }

						var blockCommentSymbols = ReadBlockCommentSymbolsList(binaryFile);
						if (blockCommentSymbols != null)
							{  language.BlockCommentSymbols = blockCommentSymbols;  }

						var javadocLineCommentSymbols = ReadLineCommentSymbolsList(binaryFile);
						if (javadocLineCommentSymbols != null)
							{  language.JavadocLineCommentSymbols = javadocLineCommentSymbols;  }

						var javadocBlockCommentSymbols = ReadBlockCommentSymbolsList(binaryFile);
						if (javadocBlockCommentSymbols != null)
							{  language.JavadocBlockCommentSymbols = javadocBlockCommentSymbols;  }

						var xmlLineCommentSymbols = ReadSymbolList(binaryFile);
						if (xmlLineCommentSymbols != null)
							{  language.XMLLineCommentSymbols = xmlLineCommentSymbols;  }

						// Prototype Enders:
						// [Int32: Comment Type ID]
						// [Byte: Include Line Breaks (1 or 0)]
						// [String: Prototype Ender Symbol] [] ... [String: null]
						// ...
						// [Int32: 0]

						int commentTypeID = binaryFile.ReadInt32();

						while (commentTypeID != 0)
							{
							bool includeLineBreaks = (binaryFile.ReadByte() == 1);
							var enderSymbols = ReadSymbolList(binaryFile);

							language.AddPrototypeEnders( new PrototypeEnders(commentTypeID, enderSymbols, includeLineBreaks) );

							commentTypeID = binaryFile.ReadInt32();
							}

						config.AddLanguage(language);

						languageName = binaryFile.ReadString();
						}

					// [String: Alias] [Int32: Language ID] [] [] ... [String: Null]

					string alias = binaryFile.ReadString();

					while (alias != null)
						{
						int languageID = binaryFile.ReadInt32();
						config.AddAlias(alias, languageID);

						alias = binaryFile.ReadString();
						}

					// [String: File Extension] [Int32: Language ID] [] [] ... [String: Null]

					string fileExtension = binaryFile.ReadString();

					while (fileExtension != null)
						{
						int languageID = binaryFile.ReadInt32();
						config.AddFileExtension(fileExtension, languageID);

						fileExtension = binaryFile.ReadString();
						}

					// [String: Shebang String] [Int32: Language ID] [] [] ... [String: Null]

					string shebangString = binaryFile.ReadString();

					while (shebangString != null)
						{
						int languageID = binaryFile.ReadInt32();
						config.AddShebangString(shebangString, languageID);

						shebangString = binaryFile.ReadString();
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
				if (binaryFile.IsOpen)
					{  binaryFile.Close();  }
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
			BinaryFile binaryFile = new BinaryFile();
			binaryFile.OpenForWriting(filename);

			try
				{

				// [String: Language Name]
				// [[Language Attributes]]
				// ...
				// [String: null]

				foreach (var language in config.Languages)
					{
					binaryFile.WriteString( language.Name );

					// [Int32: ID]
					// [Byte: Type]
					// [String: Simple Identifier]
					// [Byte: Enum Values]
					// [Byte: Case Sensitive (1 or 0)]
					// [Byte: Block Comments Nest (1 or 0)]
					// [String: Member Operator Symbol]
					// [String: Line Extender Symbol]

					binaryFile.WriteInt32( language.ID );
					binaryFile.WriteByte( (byte)language.Type );
					binaryFile.WriteString( language.SimpleIdentifier );
					binaryFile.WriteByte( (byte)language.EnumValue );
					binaryFile.WriteByte( (byte)(language.CaseSensitive ? 1 : 0) );
					binaryFile.WriteByte( (byte)(language.BlockCommentsNest ? 1 : 0) );
					binaryFile.WriteString( language.MemberOperator );
					binaryFile.WriteString( language.LineExtender );

					// [String: Line Comment Symbol] [] ... [String: null]
					// [String: Opening Block Comment Symbol] [String: Closing Block Comment Symbo] [] [] ... [String: null]
					// [String: Javadoc First Line Comment Symbol] [String: Javadoc Following Lines Comment Symbol [] ... [String: null]
					// [String: Javadoc Opening Block Comment Symbol] [String: Javadoc Closing Block Comment Symbol] [] [] ... [String: null]
					// [String: XML Line Comment Symbol] [] ... [String: null]

					WriteSymbolList(binaryFile, language.LineCommentSymbols);
					WriteBlockCommentSymbolsList(binaryFile, language.BlockCommentSymbols);
					WriteLineCommentSymbolsList(binaryFile, language.JavadocLineCommentSymbols);
					WriteBlockCommentSymbolsList(binaryFile, language.JavadocBlockCommentSymbols);
					WriteSymbolList(binaryFile, language.XMLLineCommentSymbols);

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
							binaryFile.WriteInt32( prototypeEnders.CommentTypeID );
							binaryFile.WriteByte( (byte)(prototypeEnders.IncludeLineBreaks ? 1 : 0) );
							WriteSymbolList(binaryFile, prototypeEnders.Symbols);
							}
						}

					binaryFile.WriteInt32(0);
					}

				binaryFile.WriteString(null);


				// [String: Alias] [Int32: Language ID] [] [] ... [String: Null]

				foreach (KeyValuePair<string, int> aliasKVP in config.Aliases)
					{
					binaryFile.WriteString( aliasKVP.Key );
					binaryFile.WriteInt32( aliasKVP.Value );
					}

				binaryFile.WriteString(null);


				// [String: File Extension] [Int32: Language ID] [] [] ... [String: Null]

				foreach (KeyValuePair<string, int> fileExtensionKVP in config.FileExtensions)
					{
					binaryFile.WriteString( fileExtensionKVP.Key );
					binaryFile.WriteInt32( fileExtensionKVP.Value );
					}

				binaryFile.WriteString(null);


				// [String: Shebang String] [Int32: Language ID] [] [] ... [String: Null]

				foreach (KeyValuePair<string, int> shebangStringKVP in config.ShebangStrings)
					{
					binaryFile.WriteString( shebangStringKVP.Key );
					binaryFile.WriteInt32( shebangStringKVP.Value );
					}

				binaryFile.WriteString(null);
				}

			finally
				{
				binaryFile.Close();
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
