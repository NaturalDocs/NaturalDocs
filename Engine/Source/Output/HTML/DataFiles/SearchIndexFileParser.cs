/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.DataFiles.SeachIndexFileParser
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <SearchIndex.nd>.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.IDObjects;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.DataFiles
	{
	public class SearchIndexFileParser
		{

		public SearchIndexFileParser ()
			{
			}


		/* Function: Load
		 * Loads the information in <SearchIndex.nd> and returns whether it was successful.  If not all the out parameters will still 
		 * return objects, they will just be empty.  
		 */
		public bool Load (Path filename, out StringTable<IDObjects.NumberSet> prefixTopicIDs)
			{
			prefixTopicIDs = new StringTable<IDObjects.NumberSet>(SearchIndex.Manager.KeySettingsForPrefixes);

			BinaryFile binaryFile = new BinaryFile();
			bool result = true;

			try
				{
				if (binaryFile.OpenForReading(filename, "2.0") == false)
					{  result = false;  }
				else
					{
					// [String: Prefix]
					// [NumberSet: Prefix Topic IDs]
					// ...
					// [String: null]

					for (;;)
						{
						string prefix = binaryFile.ReadString();

						if (prefix == null)
							{  break;  }

						IDObjects.NumberSet topicIDs = binaryFile.ReadNumberSet();
						prefixTopicIDs.Add(prefix, topicIDs);
						}
					}
				}
			catch
				{  result = false;  }
			finally
				{  binaryFile.Dispose();  }

			if (result == false)
				{  prefixTopicIDs.Clear();  }

			return result;
			}


		/* Function: Save
		 * Saves the passed information in <SearchIndex.nd>.
		 */
		public void Save (Path filename, StringTable<IDObjects.NumberSet> prefixTopicIDs)
			{
			using (BinaryFile binaryFile = new BinaryFile())
				{
				binaryFile.OpenForWriting(filename);

				// [String: Prefix]
				// [NumberSet: Prefix Topic IDs]
				// ...
				// [String: null]

				foreach (var prefixPair in prefixTopicIDs)
					{
					binaryFile.WriteString(prefixPair.Key);
					binaryFile.WriteNumberSet(prefixPair.Value);
					}

				binaryFile.WriteString(null);
				}
			}

		}
	}