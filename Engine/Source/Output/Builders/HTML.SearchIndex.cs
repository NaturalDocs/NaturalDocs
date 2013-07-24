/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: BuildMainSearchIndexDataFile
		 */
		protected void BuildMainSearchIndexDataFile (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			StringBuilder output = new StringBuilder("NDSearch.OnIndexLoaded([");

			List<string> keywordSegmentIDs = Engine.Instance.SearchIndex.KeywordSegmentIDs();
			keywordSegmentIDs.Sort();

			#if DONT_SHRINK_FILES
			char lastStartingLetter = '\0';
			int lineCount = 0;
			#endif

			bool isFirst = true;

			foreach (string keywordSegmentID in keywordSegmentIDs)
				{
				if (isFirst)
					{  isFirst = false;  }
				else
					{  output.Append(',');  }

				#if DONT_SHRINK_FILES
					if (keywordSegmentID[0] != lastStartingLetter)
						{
						output.Append("\n\n   ");
						lastStartingLetter = keywordSegmentID[0];
						lineCount = 0;
						}
					else if (lineCount % 20 == 0)
						{
						output.Append("\n   ");
						}

					lineCount++;
				#endif

				output.Append('"');
				output.StringEscapeAndAppend(keywordSegmentID);
				output.Append('"');
				}

			#if DONT_SHRINK_FILES
				output.Append("\n\n   ");
			#endif
			output.Append("]);");


			try
				{  
				// This will create multiple subdirectories if needed, and will not throw an exception if it already exists.
				System.IO.Directory.CreateDirectory(SearchIndex_DataFolder);  
				}
			catch
				{
				throw new Exceptions.UserFriendly( 
					Locale.Get("NaturalDocs.Engine", "Error.CouldNotCreateOutputFolder(name)", SearchIndex_DataFolder) 
					);
				}

			System.IO.File.WriteAllText(SearchIndex_IndexDataFile, output.ToString());
			}


		/* Function: BuildKeywordSegmentDataFile
		 */
		protected void BuildKeywordSegmentDataFile (string keywordSegmentID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			var keywordEntries = Engine.Instance.SearchIndex.BuildKeywordSegment(keywordSegmentID, accessor, cancelDelegate);

			if (keywordEntries == null || keywordEntries.Count == 0)
				{
				DeleteOutputFileIfExists(SearchIndex_KeywordSegmentDataFile(keywordSegmentID));
				return;
				}


			// Generate the output file contents

			StringBuilder output = new StringBuilder("OnKeywordSegmentLoaded(\"");
			output.StringEscapeAndAppend(keywordSegmentID);
			output.Append("\",");
			output.Append( (int)Engine.Instance.SearchIndex.KeywordSegmentType(keywordSegmentID) );
			output.Append(",[");

			bool isFirstKeywordEntry = true;

			foreach (var keywordEntry in keywordEntries)
				{
				if (isFirstKeywordEntry)
					{  isFirstKeywordEntry = false;  }
				else
					{  output.Append(',');  }

				#if DONT_SHRINK_FILES
				output.Append("\n\n   ");
				#endif

				output.Append("[\"");
				output.StringEscapeAndAppend(keywordEntry.Keyword);
				output.Append("\",[");

				bool isFirstTopicEntry = true;

				foreach (var topicEntry in keywordEntry.TopicEntries)
					{
					if (isFirstTopicEntry)
						{  isFirstTopicEntry = false;  }
					else
						{  output.Append(',');  }

					#if DONT_SHRINK_FILES
					output.Append("\n      ");
					#endif

					output.Append("[\"");
					output.StringEscapeAndAppend(topicEntry.DisplayName);
					output.Append("\",\"");

					Components.HTMLTopicPages.File filePage = new Components.HTMLTopicPages.File(this, topicEntry.Topic.FileID);
					output.StringEscapeAndAppend(filePage.OutputFileHashPath);

					output.Append('"');

					if (topicEntry.Topic.ClassID != 0)
						{
						output.Append(",\"");

						Components.HTMLTopicPages.Class classPage = 
							new Components.HTMLTopicPages.Class(this, topicEntry.Topic.ClassID, topicEntry.Topic.ClassString);
						output.StringEscapeAndAppend(classPage.OutputFileHashPath);

						output.Append('"');
						}

					output.Append(']');
					}

				#if DONT_SHRINK_FILES
				output.Append("\n   ");
				#endif

				output.Append("]]");
				}

			#if DONT_SHRINK_FILES
			output.Append("\n\n");
			#endif

			output.Append("]);");


			// Save the output file

			Path path = SearchIndex_KeywordSegmentDataFile(keywordSegmentID);

			try
				{  
				// This will create multiple subdirectories if needed, and will not throw an exception if it already exists.
				// We can't use SearchIndex_DataFolder because we may need a subfolder of it.
				System.IO.Directory.CreateDirectory(path.ParentFolder);
				}
			catch
				{
				throw new Exceptions.UserFriendly( 
					Locale.Get("NaturalDocs.Engine", "Error.CouldNotCreateOutputFolder(name)", path.ParentFolder) 
					);
				}

			System.IO.File.WriteAllText(path, output.ToString());
			}



		// Group: SearchIndex.IChangeWatcher Functions
		// __________________________________________________________________________


		public void OnAddSegment (string segmentID, CodeDB.EventAccessor accessor)
			{
			lock (accessLock)
				{
				buildState.NeedToBuildMainSearchIndex = true;
				buildState.SearchIndexKeywordSegmentsToRebuild.Add(segmentID);  
				}
			}

		public void OnUpdateSegment (string segmentID, CodeDB.EventAccessor accessor)
			{
			lock (accessLock)
				{
				buildState.SearchIndexKeywordSegmentsToRebuild.Add(segmentID);
				}
			}

		public void OnDeleteSegment (string segmentID, CodeDB.EventAccessor accessor)
			{
			lock (accessLock)
				{
				buildState.NeedToBuildMainSearchIndex = true;
				buildState.SearchIndexKeywordSegmentsToRebuild.Add(segmentID);  
				}
			}



		// Group: Path Functions
		// __________________________________________________________________________


		/* Property: SearchIndex_DataFolder
		 * The folder that holds all the search index JavaScript files.
		 */
		public Path SearchIndex_DataFolder
			{
			get
				{  return OutputFolder + "/search";  }
			}

		/* Function: SearchIndex_IndexDataFileNameOnly
		 * Returns the file name of the main search index data file.
		 */
		public Path SearchIndex_IndexDataFileNameOnly
			{
			get
				{  return "index.js";  }
			}

		/* Function: SearchIndex_IndexDataFile
		 * Returns the full path of the main search index data file.
		 */
		public Path SearchIndex_IndexDataFile
			{
			get
				{  return OutputFolder + "/search/index.js";  }
			}

		/* Function: SearchIndex_KeywordSegmentDataFileNameOnly
		 * Returns the file name of the search index keyword segment file for the passed ID.
		 */
		public Path SearchIndex_KeywordSegmentDataFileNameOnly (string keywordSegmentID)
			{
			#if DEBUG
			if (keywordSegmentID.Length > 3)
				{  throw new Exception ("SearchIndex_SegmentDataFileNameOnly assumes keywordSegmentIDs will be 3 characters or less.");  }
			#endif

			if (keywordSegmentID.Length == 1)
			    {  
				return string.Format("{0:x4}.js", (uint)char.ToLower(keywordSegmentID[0]));  
				}
			else if (keywordSegmentID.Length == 2)
			    {  
				return string.Format("{0:x4}{1:x4}.js", 
					(uint)char.ToLower(keywordSegmentID[0]), 
					(uint)char.ToLower(keywordSegmentID[1]));  
				}
			else
			    {  
				return string.Format("{0:x4}{1:x4}{2:x4}.js", 
					(uint)char.ToLower(keywordSegmentID[0]), 
					(uint)char.ToLower(keywordSegmentID[1]), 
					(uint)char.ToLower(keywordSegmentID[2]));
				}
			}

		/* Function: SearchIndex_KeywordSegmentDataFile
		 * Returns the full path of the search index keyword segment file for the passed ID.
		 */
		public Path SearchIndex_KeywordSegmentDataFile (string segmentID)
			{
			return OutputFolder + "/search/keywords/" + SearchIndex_KeywordSegmentDataFileNameOnly(segmentID);
			}

		}
	}

