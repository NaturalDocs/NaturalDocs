/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Components.JSSearchData
 * ____________________________________________________________________________
 * 
 * A reusable helper class to build JavaScript search data for <Output.Builders.HTML>.  See <JavaScript Search Data>
 * for the output format.
 * 
 * Topic: Usage
 *		
 *		- Create a JSSearchData object.
 *		- Call <BuildPrefixIndex()>.
 *		- Call <BuildPrefixDataFile()>.
 *		- The object can be reused with different files by calling <BuildPrefixDataFile()> again.
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.  Each thread should create its own object.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.TopicTypes;


namespace GregValure.NaturalDocs.Engine.Output.Components
	{
	public class JSSearchData
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: JSSearchData
		 */
		public JSSearchData (Builders.HTML htmlBuilder)
			{
			this.htmlBuilder = htmlBuilder;

			output = null;
			usedTopicTypes = null;
			}



		// Group: Prefix Index Functions
		// __________________________________________________________________________


		/* Function: BuildPrefixIndex
		 */
		public void BuildPrefixIndex ()
			{
			var prefixes = GetAllPrefixes();

			SortPrefixes(prefixes);

			BuildPrefixIndexJS(prefixes);

			try
				{  
				// This will create multiple subdirectories if needed, and will not throw an exception if it already exists.
				System.IO.Directory.CreateDirectory(htmlBuilder.SearchIndex_DataFolder);  
				}
			catch
				{
				throw new Exceptions.UserFriendly( 
					Locale.Get("NaturalDocs.Engine", "Error.CouldNotCreateOutputFolder(name)", htmlBuilder.SearchIndex_DataFolder) 
					);
				}

			System.IO.File.WriteAllText(htmlBuilder.SearchIndex_PrefixIndexFile, output.ToString());
			}


		/* Function: GetAllPrefixes
		 * Returns a list of all used prefixes in the search index.
		 */
		protected List<string> GetAllPrefixes ()
			{
			return Engine.Instance.SearchIndex.UsedPrefixes();
			}


		/* Function: SortPrefixes
		 */
		protected void SortPrefixes (List<string> prefixes)
			{
			prefixes.Sort(
				delegate (string a, string b)
					{
					// We need to use CompareOrdinal since this is what JavaScript uses.  The JavaScript wouldn't be able to do a
					// binary search on the list if we used a normal sort.
					return string.CompareOrdinal(a, b);
					}
				);
			}


		/* Function: BuildPrefixIndexJS
		 */
		protected void BuildPrefixIndexJS (List<string> prefixes)
			{
			if (output == null)	
				{  output = new StringBuilder();  }
			else
				{  output.Remove(0, output.Length);  }


			output.Append("NDSearch.OnPrefixIndexLoaded([");

			#if DONT_SHRINK_FILES
			char lastStartingLetter = '\0';
			int lineCount = 0;
			#endif

			bool isFirst = true;

			foreach (string prefix in prefixes)
				{
				if (isFirst)
					{  isFirst = false;  }
				else
					{  output.Append(',');  }

				#if DONT_SHRINK_FILES
					if (prefix[0] != lastStartingLetter)
						{
						output.Append("\n\n   ");
						lastStartingLetter = prefix[0];
						lineCount = 0;
						}
					else if (lineCount % 20 == 0)
						{
						output.Append("\n   ");
						}

					lineCount++;
				#endif

				output.Append('"');
				output.StringEscapeAndAppend(prefix);
				output.Append('"');
				}

			#if DONT_SHRINK_FILES
				output.Append("\n\n   ");
			#endif
			output.Append("]);");
			}



		// Group: Prefix Data File Functions
		// __________________________________________________________________________


		/* Function: BuildPrefixDataFile
		 */
		public void BuildPrefixDataFile (string prefix, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			var keywordEntries = GetPrefixKeywords(prefix, accessor, cancelDelegate);

			if (keywordEntries == null || keywordEntries.Count == 0)
				{
				htmlBuilder.DeleteOutputFileIfExists(htmlBuilder.SearchIndex_PrefixDataFile(prefix));
				return;
				}

			SortKeywordEntries(keywordEntries);

			foreach (var keywordEntry in keywordEntries)
				{  SortTopicEntries(keywordEntry);  }

			BuildPrefixDataFileJS(prefix, keywordEntries);

			Path path = htmlBuilder.SearchIndex_PrefixDataFile(prefix);

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


		/* Function: GetPrefixKeywords
		 */
		protected List<SearchIndex.KeywordEntry> GetPrefixKeywords (string prefix, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			return Engine.Instance.SearchIndex.GetKeywordEntries(prefix, accessor, cancelDelegate);
			}


		/* Function: SortKeywordEntries
		 */
		protected void SortKeywordEntries (List<SearchIndex.KeywordEntry> keywordEntries)
			{
			// Compare case-insensitively at first, then use case sensitivity to break ties.

			keywordEntries.Sort( 
				delegate (SearchIndex.KeywordEntry a, SearchIndex.KeywordEntry b)
					{
					int result = string.Compare(a.Keyword, b.Keyword, true);

					if (result != 0)
						{  return result;  }

					return string.Compare(a.Keyword, b.Keyword, false);
					}
				);
			}


		/* Function: SortTopicEntries
		 */
		protected void SortTopicEntries (SearchIndex.KeywordEntry keywordEntry)
			{
			List<SearchIndex.TopicEntry> topicEntries = keywordEntry.TopicEntries;


			// Sort by the non-qualifier part first, then by qualifiers since they'll be displayed in "Name, Class" format.  The below code is just 
			// an elaborate way of doing that without allocating intermediate strings for each comparison.  It's also case-insensitive at first 
			// and then case-sensitive to break ties.

			topicEntries.Sort(
				delegate (SearchIndex.TopicEntry a, SearchIndex.TopicEntry b)
					{
					int aNonQualifierLength = a.DisplayName.Length - a.EndOfDisplayNameQualifiers;
					int bNonQualifierLength = b.DisplayName.Length - b.EndOfDisplayNameQualifiers;
					int shorterNonQualifierLength = (aNonQualifierLength < bNonQualifierLength ? aNonQualifierLength : bNonQualifierLength);

					// Case-insensitive compare, non-qualifiers
					int result = string.Compare(a.DisplayName, a.EndOfDisplayNameQualifiers, b.DisplayName, b.EndOfDisplayNameQualifiers, shorterNonQualifierLength, true);

					if (result != 0)
						{  return result;  }

					result = (aNonQualifierLength - bNonQualifierLength);

					if (result != 0)
						{  return result;  }

					// Case-sensitive compare, non-qualifiers
					result = string.Compare(a.DisplayName, a.EndOfDisplayNameQualifiers, b.DisplayName, b.EndOfDisplayNameQualifiers, shorterNonQualifierLength, false);

					if (result != 0)
						{  return result;  }

					int shorterQualifierLength = (a.EndOfDisplayNameQualifiers < b.EndOfDisplayNameQualifiers ? a.EndOfDisplayNameQualifiers : b.EndOfDisplayNameQualifiers);

					// Case-insensitive compare, qualifiers
					result = string.Compare(a.DisplayName, 0, b.DisplayName, 0, shorterQualifierLength, true);

					if (result != 0)
						{  return result;  }

					result = (a.EndOfDisplayNameQualifiers - b.EndOfDisplayNameQualifiers);

					if (result != 0)
						{  return result;  }

					// Case-sensitive compare, qualifiers
					result = string.Compare(a.DisplayName, 0, b.DisplayName, 0, shorterQualifierLength, false);

					return result;
					}
				);


			// We're not done yet.  Now reorder the sorted list so that topics that begin with the keyword appear before those that
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


		/* Function: BuildPrefixDataFileJS
		 */
		protected void BuildPrefixDataFileJS (string prefix, List<SearchIndex.KeywordEntry> keywordEntries)
			{
			// Build the list of all used topic types

			if (usedTopicTypes == null)
				{  usedTopicTypes = new List<TopicType>();  }
			else
				{  usedTopicTypes.Clear();  }

			foreach (var keywordEntry in keywordEntries)
				{
				foreach (var topicEntry in keywordEntry.TopicEntries)
					{
					int topicTypeID = topicEntry.Topic.TopicTypeID;
					int topicTypeIndex = UsedTopicTypesIndex(topicTypeID);

					if (topicTypeIndex == -1)
						{  usedTopicTypes.Add( Engine.Instance.TopicTypes.FromID(topicTypeID) );  }
					}
				}


			// Build the output

			if (output == null)	
				{  output = new StringBuilder();  }
			else
				{  output.Remove(0, output.Length);  }


			output.Append("NDSearch.OnPrefixDataLoaded(\"");
			output.StringEscapeAndAppend(prefix);
			output.Append("\",");

			#if DONT_SHRINK_FILES
			output.Append("\n   ");
			#endif

			BuildTopicTypeList(keywordEntries);
			output.Append(',');

			#if DONT_SHRINK_FILES
			output.Append("\n   ");
			#endif

			output.Append('[');

			bool isFirstKeywordEntry = true;

			foreach (var keywordEntry in keywordEntries)
				{
				if (isFirstKeywordEntry)
					{  isFirstKeywordEntry = false;  }
				else
					{  output.Append(',');  }

				BuildKeywordEntry(keywordEntry);
				}

			#if DONT_SHRINK_FILES
			output.Append("\n\n");
			#endif

			output.Append("]);");
			}


		/* Function: BuildKeywordEntry
		 */
		protected void BuildKeywordEntry (SearchIndex.KeywordEntry keywordEntry)
			{
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

				BuildTopicEntry(topicEntry, keywordHTMLName);
				}

			#if DONT_SHRINK_FILES
			output.Append("\n   ");
			#endif

			output.Append("]]");
			}


		/* Function: BuildTopicEntry
		 */
		protected void BuildTopicEntry (SearchIndex.TopicEntry topicEntry, string keywordHTMLName)
			{
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

			output.Append(',');

			output.Append(UsedTopicTypesIndex(topicEntry.Topic.TopicTypeID));

			output.Append(",\"");
			Components.HTMLTopicPages.File filePage = new Components.HTMLTopicPages.File(htmlBuilder, topicEntry.Topic.FileID);
			output.StringEscapeAndAppend(filePage.OutputFileHashPath);
			output.Append(':');
			output.StringEscapeAndAppend(Builders.HTML.Source_TopicHashPath(topicEntry.Topic, true));
			output.Append('"');

			if (topicEntry.Topic.ClassID != 0)
				{
				output.Append(",\"");

				Components.HTMLTopicPages.Class classPage = 
					new Components.HTMLTopicPages.Class(htmlBuilder, topicEntry.Topic.ClassID, topicEntry.Topic.ClassString);
				output.StringEscapeAndAppend(classPage.OutputFileHashPath);
				output.Append(':');
				output.StringEscapeAndAppend(Builders.HTML.Source_TopicHashPath(topicEntry.Topic, false));
	
				output.Append('"');
				}

			output.Append(']');
			}


		/* Function: BuildTopicTypeList
		 */
		protected void BuildTopicTypeList (IList<SearchIndex.KeywordEntry> keywordEntries)
			{
			output.Append('[');

			for (int i = 0; i < usedTopicTypes.Count; i++)
				{
				if (i != 0)
					{  output.Append(',');  }

				output.Append('"');
				output.StringEscapeAndAppend(usedTopicTypes[i].SimpleIdentifier);
				output.Append('"');
				}

			output.Append(']');
			}


		/* Function: UsedTopicTypesIndex
		 * Returns the index into <usedTopicTypes> of the passed topic type ID, or -1 if it isn't in the list.
		 */
		protected int UsedTopicTypesIndex (int topicTypeID)
			{
			for (int i = 0; i < usedTopicTypes.Count; i++)
				{
				if (usedTopicTypes[i].ID == topicTypeID)
					{  return i;  }
				}

			return -1;
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: htmlBuilder
		 */
		protected Builders.HTML htmlBuilder;

		/* var: output
		 * The JavaScript being generated.
		 */
		protected StringBuilder output;

		/* var: usedTopicTypes
		 * A list of the topic types used in the search data.  The order in which they appear here will be the order in which they
		 * appear in the JavaScript array.
		 */
		protected List<TopicType> usedTopicTypes;

		}
	}

