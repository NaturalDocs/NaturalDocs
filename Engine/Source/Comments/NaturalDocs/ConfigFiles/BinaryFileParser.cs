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

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
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
			binaryFile = null;
			}



		// Group: Loading Functions
		// __________________________________________________________________________


		/* Function: Load
		 * Loads the information in <Parser.nd> into a <Config> object, returning whether it was successful.  If it was not config
		 * will be null.
		 */
		public bool Load (Path filename, out Config config)
		    {
		    binaryFile = new BinaryFile();

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

					LoadSet(config.StartBlockKeywords);
					LoadSet(config.EndBlockKeywords);
					LoadSet(config.SeeImageKeywords);
					LoadSet(config.AtLinkKeywords);
					LoadSet(config.URLProtocols);
					LoadSet(config.AcceptableLinkSuffixes);


					// Tables:
					// 	- BlockTypes
					// - SpecialHeadings
					// - AccessLevel

					LoadBlockTypesTable(config.BlockTypes);
					LoadHeadingTypesTable(config.SpecialHeadings);
					LoadAccessLevelTable(config.AccessLevel);


					// Conversion Lists:
					// - PluralConversions
					// - PossessiveConversions

					LoadConversionList(config.PluralConversions);
					LoadConversionList(config.PossessiveConversions);

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

				binaryFile = null;
				}
		    }


		/* Function: LoadSet
		 * Loads values into the passed <StringSet> until it reaches a null string.
		 */
		protected void LoadSet (StringSet set)
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
		protected void LoadBlockTypesTable (StringTable<NaturalDocs.Parser.BlockType> table)
		    {
			// [String: key] [Byte: value] [] [] ... [String: null]

		    for (;;)
		        {
		        string key = binaryFile.ReadString();

		        if (key == null)
		            {  return;  }

		        NaturalDocs.Parser.BlockType value = (NaturalDocs.Parser.BlockType)binaryFile.ReadByte();

		        table.Add(key, value);
		        }
		    }


		/* Function: LoadHeadingTypesTable
		 * Loads values into the passed <StringTable> until it reaches a null string.
		 */
		protected void LoadHeadingTypesTable (StringTable<NaturalDocs.Parser.HeadingType> table)
		    {
			// [String: key] [Byte: value] [] [] ... [String: null]

		    for (;;)
		        {
		        string key = binaryFile.ReadString();

		        if (key == null)
		            {  return;  }

		        NaturalDocs.Parser.HeadingType value = (NaturalDocs.Parser.HeadingType)binaryFile.ReadByte();

		        table.Add(key, value);
		        }
		    }


		/* Function: LoadAccessLevelTable
		 * Loads values into the passed <StringTable> until it reaches a null string.
		 */
		protected void LoadAccessLevelTable (StringTable<Languages.AccessLevel> table)
		    {
			// [String: key] [Byte: value] [] [] ... [String: null]

		    for (;;)
		        {
		        string key = binaryFile.ReadString();

		        if (key == null)
		            {  return;  }

		        Languages.AccessLevel value = (Languages.AccessLevel)binaryFile.ReadByte();

		        table.Add(key, value);
		        }
		    }


		/* Function: LoadConversionList
		 * Loads values into the passed conversion list until it reaches a null string.
		 */
		protected void LoadConversionList (List<KeyValuePair<string, string>> conversionList)
		    {
			// [String: key] [String: value] [] [] ... [String: null]

		    for (;;)
		        {
		        string key = binaryFile.ReadString();

		        if (key == null)
		            {  return;  }

		        string value = binaryFile.ReadString();

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
		    binaryFile = new BinaryFile();

		    try
		        {
			    binaryFile.OpenForWriting(filename);

				// Sets:
				// - StartBlockKeywords
				// - EndBlockKeywords
				// - SeeImageKeywords
				// - AtLinkKeywords
				// - URLProtocols
				// - AcceptableLinkSuffixes

				SaveSet(config.StartBlockKeywords);
				SaveSet(config.EndBlockKeywords);
				SaveSet(config.SeeImageKeywords);
				SaveSet(config.AtLinkKeywords);
				SaveSet(config.URLProtocols);
				SaveSet(config.AcceptableLinkSuffixes);


				// Tables:
				// 	- BlockTypes
				// - SpecialHeadings
				// - AccessLevel

				SaveBlockTypesTable(config.BlockTypes);
				SaveHeadingTypesTable(config.SpecialHeadings);
				SaveAccessLevelTable(config.AccessLevel);


				// Conversion Lists:
				// - PluralConversions
				// - PossessiveConversions

				SaveConversionList(config.PluralConversions);
				SaveConversionList(config.PossessiveConversions);
		        }

		    finally
		        {
				if (binaryFile.IsOpen)
					{  binaryFile.Close();  }

				binaryFile = null;
		        }
		    }


		/* Function: SaveSet
		 * Writes the <StringSet> values followed by a null string.
		 */
		protected void SaveSet (StringSet set)
		    {
		    foreach (string value in set)
		        {  binaryFile.WriteString(value);  }

		    binaryFile.WriteString(null);
		    }


		/* Function: SaveBlockTypesTable
		 * Writes the <StringTable> values followed by a null string.
		 */
		protected void SaveBlockTypesTable (StringTable<NaturalDocs.Parser.BlockType> table)
		    {
		    foreach (KeyValuePair<string, NaturalDocs.Parser.BlockType> pair in table)
		        {
		        binaryFile.WriteString(pair.Key);
		        binaryFile.WriteByte((byte)pair.Value);
		        }

		    binaryFile.WriteString(null);
		    }


		/* Function: SaveHeadingTypesTable
		 * Writes the <StringTable> values followed by a null string.
		 */
		protected void SaveHeadingTypesTable (StringTable<NaturalDocs.Parser.HeadingType> table)
		    {
		    foreach (KeyValuePair<string, NaturalDocs.Parser.HeadingType> pair in table)
		        {
		        binaryFile.WriteString(pair.Key);
		        binaryFile.WriteByte((byte)pair.Value);
		        }

		    binaryFile.WriteString(null);
		    }


		/* Function: SaveAccessLevelTable
		 * Writes the <StringTable> values followed by a null string.
		 */
		protected void SaveAccessLevelTable (StringTable<Languages.AccessLevel> table)
		    {
		    foreach (KeyValuePair<string, Languages.AccessLevel> pair in table)
		        {
		        binaryFile.WriteString(pair.Key);
		        binaryFile.WriteByte((byte)pair.Value);
		        }

		    binaryFile.WriteString(null);
		    }


		/* Function: SaveConversionList
		 * Writes the conversion list values followed by a null string.
		 */
		protected void SaveConversionList (List<KeyValuePair<string, string>> conversionList)
		    {
		    foreach (KeyValuePair<string, string> pair in conversionList)
		        {
				binaryFile.WriteString(pair.Key);
				binaryFile.WriteString(pair.Value);
				}

		    binaryFile.WriteString(null);
		    }



		// Group: Variables
		// __________________________________________________________________________

		protected BinaryFile binaryFile;

		}
	}
