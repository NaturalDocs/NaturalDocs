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

			keywordSegmentIDs.Sort(
				delegate (string a, string b)
					{
					return string.CompareOrdinal(a, b);
					}
				);

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


			// Sort the keywords.  Compare case-insensitively at first, then use case sensitivity to break ties.

			keywordEntries.Sort( 
				delegate (SearchIndex.KeywordEntry a, SearchIndex.KeywordEntry b)
					{
					int result = string.Compare(a.Keyword, b.Keyword, true);

					if (result != 0)
						{  return result;  }

					return string.Compare(a.Keyword, b.Keyword, false);
					}
				);


			// Also sort the topic entries in each keyword entry.  We sort by the non-qualifier part first, then by qualifiers since they'll be
			// displayed in "Name, Class" format.  The below code is just an elaborate way of doing that without allocating intermediate
			// strings for each comparison.  This is also case-insensitive at first and then sensitive to break ties.

			foreach (var keywordEntry in keywordEntries)
				{
				var topicEntries = keywordEntry.TopicEntries;

				topicEntries.Sort(
					delegate (SearchIndex.TopicEntry a, SearchIndex.TopicEntry b)
						{
						int aNonQualifierLength = a.DisplayName.Length - a.EndOfDisplayNameQualifiers;
						int bNonQualifierLength = b.DisplayName.Length - b.EndOfDisplayNameQualifiers;
						int shorterNonQualifierLength = (aNonQualifierLength < bNonQualifierLength ? aNonQualifierLength : bNonQualifierLength);

						int result = string.Compare(a.DisplayName, a.EndOfDisplayNameQualifiers, b.DisplayName, b.EndOfDisplayNameQualifiers, shorterNonQualifierLength, true);

						if (result != 0)
							{  return result;  }

						result = string.Compare(a.DisplayName, a.EndOfDisplayNameQualifiers, b.DisplayName, b.EndOfDisplayNameQualifiers, shorterNonQualifierLength, false);

						if (result != 0)
							{  return result;  }

						result = (aNonQualifierLength - bNonQualifierLength);

						if (result != 0)
							{  return result;  }

						int shorterQualifierLength = (a.EndOfDisplayNameQualifiers < b.EndOfDisplayNameQualifiers ? a.EndOfDisplayNameQualifiers : b.EndOfDisplayNameQualifiers);

						result = string.Compare(a.DisplayName, 0, b.DisplayName, 0, shorterQualifierLength, true);

						if (result != 0)
							{  return result;  }

						result = string.Compare(a.DisplayName, 0, b.DisplayName, 0, shorterQualifierLength, false);

						if (result != 0)
							{  return result;  }

						result = (a.EndOfDisplayNameQualifiers - b.EndOfDisplayNameQualifiers);

						return result;
						}
					);


				// Oh we're not done yet.  Now reorder the sorted list so that topics that begin with the keyword appear before those that
				// don't, even if they're otherwise ahead of it in the sort order.  Right now we have this:
				//
				// Thread
				// - Not Thread Safe
				// - thread
				// - Thread
				// - Thread Safe
				// - Thread Safety Notes
				// - Window Threads
				//
				// but most of the time someone typing in "thread" will want the Thread class, so convert it into this:
				//
				// Thread
				// - thread
				// - Thread
				// - Thread Safe
				// - Thread Safety Notes
				// - Not Thread Safe
				// - Window Threads

				int firstKeywordStart = -1;

				for (int i = 0; i < topicEntries.Count; i++)
					{
					int keywordIndex = topicEntries[i].SearchText.IndexOf(keywordEntry.SearchText, topicEntries[i].EndOfSearchTextQualifiers);

					if (keywordIndex == topicEntries[i].EndOfSearchTextQualifiers)
						{
						firstKeywordStart = i;
						break;
						}
					}

				// If the first one already starts with the keyword, or none of them do, there's nothing we need to do.
				if (firstKeywordStart > 0)
					{
					int endOfKeywordStart = topicEntries.Count;

					for (int i = topicEntries.Count - 1; i >= 0; i--)
						{
						int keywordIndex = topicEntries[i].SearchText.IndexOf(keywordEntry.SearchText, topicEntries[i].EndOfSearchTextQualifiers);

						if (keywordIndex == topicEntries[i].EndOfSearchTextQualifiers)
							{  break;  }
						else
							{  endOfKeywordStart = i;  }
						}

					var tempList = topicEntries.GetRange(0, firstKeywordStart);
					topicEntries.RemoveRange(0, firstKeywordStart);

					// end - first because it shifted down after we removed the range
					topicEntries.InsertRange(endOfKeywordStart - firstKeywordStart, tempList);
					}
				}


			// Generate the output file contents

			StringBuilder output = new StringBuilder("NDSearch.OnKeywordSegmentLoaded(\"");
			output.StringEscapeAndAppend(keywordSegmentID);
			output.Append("\",[");

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

				string keywordHTMLName = keywordEntry.Keyword.ToHTML();
				string keywordSearchText = keywordEntry.SearchText;

				output.Append("[\"");
				output.StringEscapeAndAppend(keywordHTMLName);
				output.Append("\",");

				if (keywordSearchText != keywordHTMLName.ToLower())
					{
					output.Append('"');
					output.StringEscapeAndAppend(keywordSearchText);
					output.Append('"');
					}
				// Otherwise leave an empty spot before the comma.  We don't have to write out "undefined".

				output.Append(",[");

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

					string topicHTMLPrefix, topicHTMLName, topicSearchText;

					if (topicEntry.EndOfDisplayNameQualifiers == 0)
						{
						topicHTMLPrefix = null;
						topicHTMLName = topicEntry.DisplayName.ToHTML();
						topicSearchText = topicEntry.SearchText;
						}
					else
						{
						topicHTMLPrefix = topicEntry.DisplayName.Substring(0, topicEntry.EndOfDisplayNameQualifiers);

						if (topicHTMLPrefix[ topicHTMLPrefix.Length - 1 ] == '.')
							{  topicHTMLPrefix = topicHTMLPrefix.Substring(0, topicHTMLPrefix.Length - 1);  }
						else if (topicHTMLPrefix.EndsWith("::") || topicHTMLPrefix.EndsWith("->"))
							{  topicHTMLPrefix = topicHTMLPrefix.Substring(0, topicHTMLPrefix.Length - 2);  }
						
						topicHTMLPrefix = topicHTMLPrefix.ToHTML();
						topicHTMLName = topicEntry.DisplayName.Substring(topicEntry.EndOfDisplayNameQualifiers).ToHTML();
						topicSearchText = topicEntry.SearchText.Substring(topicEntry.EndOfSearchTextQualifiers);
						}

					output.Append('[');

					if (topicHTMLPrefix != null)
						{
						output.Append('"');
						output.StringEscapeAndAppend(topicHTMLPrefix);
						output.Append('"');
						}

					output.Append(',');

					if (topicHTMLName != keywordHTMLName)
						{
						output.Append('"');
						output.StringEscapeAndAppend(topicHTMLName);
						output.Append('"');
						}

					output.Append(',');

					if (topicSearchText != topicHTMLName.ToLower())
						{
						output.Append('"');
						output.StringEscapeAndAppend(topicSearchText);
						output.Append('"');
						}

					output.Append(",\"");
					Components.HTMLTopicPages.File filePage = new Components.HTMLTopicPages.File(this, topicEntry.Topic.FileID);
					output.StringEscapeAndAppend(filePage.OutputFileHashPath);
					output.Append(':');
					output.StringEscapeAndAppend(Source_TopicHashPath(topicEntry.Topic, true));
					output.Append('"');

					if (topicEntry.Topic.ClassID != 0)
						{
						output.Append(",\"");

						Components.HTMLTopicPages.Class classPage = 
							new Components.HTMLTopicPages.Class(this, topicEntry.Topic.ClassID, topicEntry.Topic.ClassString);
						output.StringEscapeAndAppend(classPage.OutputFileHashPath);
						output.Append(':');
						output.StringEscapeAndAppend(Source_TopicHashPath(topicEntry.Topic, false));
	
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

