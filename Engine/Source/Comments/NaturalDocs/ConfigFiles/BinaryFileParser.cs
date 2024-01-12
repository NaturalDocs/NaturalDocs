/*
 * Class: CodeClear.NaturalDocs.Engine.Comments.NaturalDocs.ConfigFiles.BinaryFileParser
 * ____________________________________________________________________________
 *
 * A class to handle loading and saving <Parser.nd>.
 *
 *
 * Multithreading: Not Thread Safe
 *
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Comments.NaturalDocs.ConfigFiles
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
		 * Loads the information in <Parser.nd> into a <Config> object, returning whether it was successful.  If it was not config
		 * will be null.
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
		        else if (binaryFile.Version.IsAtLeastRelease("2.2") == false &&
						   binaryFile.Version.IsSamePreRelease(Engine.Instance.Version) == false)
		            {
					binaryFile.Close();
		            config = null;
					return false;
		            }
		        else
		            {
					config = new Config();

					// Sets:
					// - StartBlockKeywords
					// - EndBlockKeywords
					// - SeeImageKeywords
					// - AtLinkKeywords
					// - URLProtocols
					// - AcceptableLinkSuffixes

					LoadSet(binaryFile, config.StartBlockKeywords);
					LoadSet(binaryFile, config.EndBlockKeywords);
					LoadSet(binaryFile, config.SeeImageKeywords);
					LoadSet(binaryFile, config.AtLinkKeywords);
					LoadSet(binaryFile, config.URLProtocols);
					LoadSet(binaryFile, config.AcceptableLinkSuffixes);


					// Tables:
					// 	- BlockTypes
					// - SpecialHeadings
					// - AccessLevel

					LoadBlockTypesTable(binaryFile, config.BlockTypes);
					LoadHeadingTypesTable(binaryFile, config.SpecialHeadings);
					LoadAccessLevelTable(binaryFile, config.AccessLevel);


					// Conversion Lists:
					// - PluralConversions
					// - PossessiveConversions

					LoadConversionList(binaryFile, config.PluralConversions);
					LoadConversionList(binaryFile, config.PossessiveConversions);

					return true;
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
		    }


		/* Function: LoadSet
		 * Loads values into the passed <StringSet> until it reaches a null string.
		 */
		protected void LoadSet (BinaryFile binaryFile, StringSet set)
		    {
			//	[String: value] [] ... [String: null]

		    for (;;)
		        {
		        string value = binaryFile.ReadString();

		        if (value == null)
		            {  return;  }

		        set.Add(value);
		        }
		    }


		/* Function: LoadBlockTypesTable
		 * Loads values into the passed <StringTable> until it reaches a null string.
		 */
		protected void LoadBlockTypesTable (BinaryFile file, StringTable<NaturalDocs.Parser.BlockType> table)
		    {
			// [String: key] [Byte: value] [] [] ... [String: null]

		    for (;;)
		        {
		        string key = file.ReadString();

		        if (key == null)
		            {  return;  }

		        NaturalDocs.Parser.BlockType value = (NaturalDocs.Parser.BlockType)file.ReadByte();

		        table.Add(key, value);
		        }
		    }


		/* Function: LoadHeadingTypesTable
		 * Loads values into the passed <StringTable> until it reaches a null string.
		 */
		protected void LoadHeadingTypesTable (BinaryFile file, StringTable<NaturalDocs.Parser.HeadingType> table)
		    {
			// [String: key] [Byte: value] [] [] ... [String: null]

		    for (;;)
		        {
		        string key = file.ReadString();

		        if (key == null)
		            {  return;  }

		        NaturalDocs.Parser.HeadingType value = (NaturalDocs.Parser.HeadingType)file.ReadByte();

		        table.Add(key, value);
		        }
		    }


		/* Function: LoadAccessLevelTable
		 * Loads values into the passed <StringTable> until it reaches a null string.
		 */
		protected void LoadAccessLevelTable (BinaryFile file, StringTable<Languages.AccessLevel> table)
		    {
			// [String: key] [Byte: value] [] [] ... [String: null]

		    for (;;)
		        {
		        string key = file.ReadString();

		        if (key == null)
		            {  return;  }

		        Languages.AccessLevel value = (Languages.AccessLevel)file.ReadByte();

		        table.Add(key, value);
		        }
		    }


		/* Function: LoadConversionList
		 * Loads values into the passed conversion list until it reaches a null string.
		 */
		protected void LoadConversionList (BinaryFile file, List<KeyValuePair<string, string>> conversionList)
		    {
			// [String: key] [String: value] [] [] ... [String: null]

		    for (;;)
		        {
		        string key = file.ReadString();

		        if (key == null)
		            {  return;  }

		        string value = file.ReadString();

		        conversionList.Add( new KeyValuePair<string, string>(key, value));
		        }
		    }



		// Group: Saving Functions
		// __________________________________________________________________________


		/* Function: Save
		 * Saves the passed <Config> into <Parser.nd>.  Throws an exception if unsuccessful.
		 */
		public void Save (Path filename, Config config)
		    {
		    BinaryFile binaryFile = new BinaryFile();
		    binaryFile.OpenForWriting(filename);

		    try
		        {
				// Sets:
				// - StartBlockKeywords
				// - EndBlockKeywords
				// - SeeImageKeywords
				// - AtLinkKeywords
				// - URLProtocols
				// - AcceptableLinkSuffixes

				SaveSet(binaryFile, config.StartBlockKeywords);
				SaveSet(binaryFile, config.EndBlockKeywords);
				SaveSet(binaryFile, config.SeeImageKeywords);
				SaveSet(binaryFile, config.AtLinkKeywords);
				SaveSet(binaryFile, config.URLProtocols);
				SaveSet(binaryFile, config.AcceptableLinkSuffixes);


				// Tables:
				// 	- BlockTypes
				// - SpecialHeadings
				// - AccessLevel

				SaveBlockTypesTable(binaryFile, config.BlockTypes);
				SaveHeadingTypesTable(binaryFile, config.SpecialHeadings);
				SaveAccessLevelTable(binaryFile, config.AccessLevel);


				// Conversion Lists:
				// - PluralConversions
				// - PossessiveConversions

				SaveConversionList(binaryFile, config.PluralConversions);
				SaveConversionList(binaryFile, config.PossessiveConversions);
		        }

		    finally
		        {
		        binaryFile.Close();
		        }
		    }


		/* Function: SaveSet
		 * Writes the <StringSet> values followed by a null string.
		 */
		protected void SaveSet (BinaryFile file, StringSet set)
		    {
		    foreach (string value in set)
		        {  file.WriteString(value);  }

		    file.WriteString(null);
		    }


		/* Function: SaveBlockTypesTable
		 * Writes the <StringTable> values followed by a null string.
		 */
		protected void SaveBlockTypesTable (BinaryFile file, StringTable<NaturalDocs.Parser.BlockType> table)
		    {
		    foreach (KeyValuePair<string, NaturalDocs.Parser.BlockType> pair in table)
		        {
		        file.WriteString(pair.Key);
		        file.WriteByte((byte)pair.Value);
		        }

		    file.WriteString(null);
		    }


		/* Function: SaveHeadingTypesTable
		 * Writes the <StringTable> values followed by a null string.
		 */
		protected void SaveHeadingTypesTable (BinaryFile file, StringTable<NaturalDocs.Parser.HeadingType> table)
		    {
		    foreach (KeyValuePair<string, NaturalDocs.Parser.HeadingType> pair in table)
		        {
		        file.WriteString(pair.Key);
		        file.WriteByte((byte)pair.Value);
		        }

		    file.WriteString(null);
		    }


		/* Function: SaveAccessLevelTable
		 * Writes the <StringTable> values followed by a null string.
		 */
		protected void SaveAccessLevelTable (BinaryFile file, StringTable<Languages.AccessLevel> table)
		    {
		    foreach (KeyValuePair<string, Languages.AccessLevel> pair in table)
		        {
		        file.WriteString(pair.Key);
		        file.WriteByte((byte)pair.Value);
		        }

		    file.WriteString(null);
		    }


		/* Function: SaveConversionList
		 * Writes the conversion list values followed by a null string.
		 */
		protected void SaveConversionList (BinaryFile file, List<KeyValuePair<string, string>> conversionList)
		    {
		    foreach (KeyValuePair<string, string> pair in conversionList)
		        {
				file.WriteString(pair.Key);
				file.WriteString(pair.Value);
				}

		    file.WriteString(null);
		    }

		}
	}
