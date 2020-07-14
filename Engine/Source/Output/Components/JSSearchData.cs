/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Components.JSSearchData
 * ____________________________________________________________________________
 * 
 * A helper class to build JavaScript search data for <Output.Builders.HTML>.  See <JavaScript Search Data>
 * for the output format.
 * 
 * Topic: Usage
 *		
 *		- Create a JSSearchData object.
 *		- Call <BuildPrefixIndex()>.
 *		- Call <BuildPrefixDataFile()>.
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.  Each thread should create its own object.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.CommentTypes;


namespace CodeClear.NaturalDocs.Engine.Output.Components
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
			usedCommentTypes = null;
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
				System.IO.Directory.CreateDirectory(HTMLBuilder.SearchIndex_DataFolder);  
				}
			catch (Exception e)
				{
				throw new Exceptions.UserFriendly( 
					Locale.Get("NaturalDocs.Engine", "Error.CouldNotCreateOutputFolder(name, exception)",
									HTMLBuilder.SearchIndex_DataFolder, e.Message) 
					);
				}

			System.IO.File.WriteAllText(HTMLBuilder.SearchIndex_PrefixIndexFile, output.ToString());
			}


		/* Function: GetAllPrefixes
		 * Returns a list of all used prefixes in the search index.
		 */
		protected List<string> GetAllPrefixes ()
			{
			return EngineInstance.SearchIndex.UsedPrefixes();
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

			char lastStartingLetter = '\0';
			int lineCount = 0;
			bool isFirst = true;

			foreach (string prefix in prefixes)
				{
				if (isFirst)
					{  isFirst = false;  }
				else
					{  output.Append(',');  }

				if (!EngineInstance.Config.ShrinkFiles)
					{
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
					}

				output.Append('"');
				output.StringEscapeAndAppend(prefix);
				output.Append('"');
				}

			if (!EngineInstance.Config.ShrinkFiles)
				{  output.Append("\n\n   ");  }

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
				HTMLBuilder.DeleteOutputFileIfExists(HTMLBuilder.SearchIndex_PrefixDataFile(prefix));
				return;
				}

			SortKeywordEntries(keywordEntries);

			foreach (var keywordEntry in keywordEntries)
				{  
				SortTopicEntries(keywordEntry);
				RemoveDuplicateTopics(keywordEntry);
				}

			BuildPrefixDataFileJS(prefix, keywordEntries);

			Path path = HTMLBuilder.SearchIndex_PrefixDataFile(prefix);

			try
				{  
				// This will create multiple subdirectories if needed, and will not throw an exception if it already exists.
				// We can't use SearchIndex_DataFolder because we may need a subfolder of it.
				System.IO.Directory.CreateDirectory(path.ParentFolder);
				}
			catch (Exception e)
				{
				throw new Exceptions.UserFriendly( 
					Locale.Get("NaturalDocs.Engine", "Error.CouldNotCreateOutputFolder(name, exception)", 
									path.ParentFolder, e.Message) 
					);
				}

			System.IO.File.WriteAllText(path, output.ToString());
			}


		/* Function: GetPrefixKeywords
		 */
		protected List<HTML.SearchIndex.Entries.Keyword> GetPrefixKeywords (string prefix, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			return EngineInstance.SearchIndex.GetKeywordEntries(prefix, accessor, cancelDelegate);
			}


		/* Function: SortKeywordEntries
		 */
		protected void SortKeywordEntries (List<HTML.SearchIndex.Entries.Keyword> keywordEntries)
			{
			// Compare case-insensitively at first, then use case sensitivity to break ties.

			keywordEntries.Sort( 
				delegate (HTML.SearchIndex.Entries.Keyword a, HTML.SearchIndex.Entries.Keyword b)
					{
					int result = string.Compare(a.DisplayName, b.DisplayName, true);

					if (result != 0)
						{  return result;  }

					return string.Compare(a.DisplayName, b.DisplayName, false);
					}
				);
			}


		/* Function: SortTopicEntries
		 */
		protected void SortTopicEntries (HTML.SearchIndex.Entries.Keyword keywordEntry)
			{
			List<HTML.SearchIndex.Entries.Topic> topicEntries = keywordEntry.TopicEntries;

			topicEntries.Sort(
				delegate (HTML.SearchIndex.Entries.Topic a, HTML.SearchIndex.Entries.Topic b)
					{
					// Sort by the non-qualifier part first since they'll be displayed in "Name, Class" format.  The below code is just an elaborate 
					// way of doing that without allocating intermediate strings for each comparison.

					int aNonQualifierLength = a.DisplayName.Length - a.EndOfDisplayNameQualifiers;
					int bNonQualifierLength = b.DisplayName.Length - b.EndOfDisplayNameQualifiers;
					int shorterNonQualifierLength = (aNonQualifierLength < bNonQualifierLength ? aNonQualifierLength : bNonQualifierLength);


					// Compare non-qualifiers in a case-insensitive way first.

					int result = string.Compare(a.DisplayName, a.EndOfDisplayNameQualifiers, b.DisplayName, b.EndOfDisplayNameQualifiers, shorterNonQualifierLength, true);

					if (result != 0)
						{  return result;  }

					result = (aNonQualifierLength - bNonQualifierLength);

					if (result != 0)
						{  return result;  }


					// Before comparing in a case-sensitive way, compare based on hierarchy membership.  We want class "Token" to appear before
					// variable "token" even though normally we want lowercase to go first.

					var aCommentType = EngineInstance.CommentTypes.FromID(a.WrappedTopic.CommentTypeID);
					var bCommentType = EngineInstance.CommentTypes.FromID(b.WrappedTopic.CommentTypeID);

					if (aCommentType.Flags.ClassHierarchy != bCommentType.Flags.ClassHierarchy)
						{  return (aCommentType.Flags.ClassHierarchy ? -1 : 1);  }

					if (aCommentType.Flags.DatabaseHierarchy != bCommentType.Flags.DatabaseHierarchy)
						{  return (aCommentType.Flags.DatabaseHierarchy ? -1 : 1);  }


					// Still equal, now compare the qualifiers in a case-sensitive way to break ties.

					result = string.Compare(a.DisplayName, a.EndOfDisplayNameQualifiers, b.DisplayName, b.EndOfDisplayNameQualifiers, shorterNonQualifierLength, false);

					if (result != 0)
						{  return result;  }

					int shorterQualifierLength = (a.EndOfDisplayNameQualifiers < b.EndOfDisplayNameQualifiers ? a.EndOfDisplayNameQualifiers : b.EndOfDisplayNameQualifiers);


					// Still equal so do a case-insensitive comparison of qualifiers, so "Name, ClassA" comes before "Name, ClassB".

					result = string.Compare(a.DisplayName, 0, b.DisplayName, 0, shorterQualifierLength, true);

					if (result != 0)
						{  return result;  }

					result = (a.EndOfDisplayNameQualifiers - b.EndOfDisplayNameQualifiers);

					if (result != 0)
						{  return result;  }


					// Case-sensitive comparison of qualifiers to break ties.

					result = string.Compare(a.DisplayName, 0, b.DisplayName, 0, shorterQualifierLength, false);

					if (result != 0)
						{  return result;  }


					// So now we have two symbols that are equal letter for letter.  Sort by language name first.

					if (a.WrappedTopic.LanguageID != b.WrappedTopic.LanguageID)
						{
						return string.Compare(EngineInstance.Languages.FromID(a.WrappedTopic.LanguageID).Name, 
														 EngineInstance.Languages.FromID(b.WrappedTopic.LanguageID).Name, true);
						}


					// and by file name next.

					if (a.WrappedTopic.FileID != b.WrappedTopic.FileID)
						{
						return string.Compare(EngineInstance.Files.FromID(a.WrappedTopic.FileID).FileName, 
														 EngineInstance.Files.FromID(b.WrappedTopic.FileID).FileName, true);
						}


					// If we're here then they're two overloaded functions in the same source file.  Go by symbol definition number.

					return (a.WrappedTopic.SymbolDefinitionNumber - b.WrappedTopic.SymbolDefinitionNumber);
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


		/* Function: RemoveDuplicateTopics
		 * Removes topics from the results which have the same letter for letter display names.  An exception is made if they
		 * have different languages.
		 */
		protected void RemoveDuplicateTopics (HTML.SearchIndex.Entries.Keyword keywordEntry)
			{
			List<HTML.SearchIndex.Entries.Topic> topicEntries = keywordEntry.TopicEntries;

			for (int i = 1; i < topicEntries.Count; /* no auto-increment */)
				{
				var current = topicEntries[i];
				var previous = topicEntries[i -1];

				if (current.DisplayName == previous.DisplayName &&
					current.WrappedTopic.LanguageID == previous.WrappedTopic.LanguageID)
					{  topicEntries.RemoveAt(i);  }
				else
					{  i++;  }
				}
			}


		/* Function: BuildPrefixDataFileJS
		 */
		protected void BuildPrefixDataFileJS (string prefix, List<HTML.SearchIndex.Entries.Keyword> keywordEntries)
			{
			// Build the list of all used comment types

			if (usedCommentTypes == null)
				{  usedCommentTypes = new List<CommentType>();  }
			else
				{  usedCommentTypes.Clear();  }

			foreach (var keywordEntry in keywordEntries)
				{
				foreach (var topicEntry in keywordEntry.TopicEntries)
					{
					int commentTypeID = topicEntry.WrappedTopic.CommentTypeID;
					int commentTypeIndex = UsedCommentTypesIndex(commentTypeID);

					if (commentTypeIndex == -1)
						{  usedCommentTypes.Add( EngineInstance.CommentTypes.FromID(commentTypeID) );  }
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

			if (!EngineInstance.Config.ShrinkFiles)
				{  output.Append("\n   ");  }

			BuildCommentTypeList(keywordEntries);
			output.Append(',');

			if (!EngineInstance.Config.ShrinkFiles)
				{  output.Append("\n   ");  }

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

			if (!EngineInstance.Config.ShrinkFiles)
				{  output.Append("\n\n");  }

			output.Append("]);");
			}


		/* Function: BuildKeywordEntry
		 */
		protected void BuildKeywordEntry (HTML.SearchIndex.Entries.Keyword keywordEntry)
			{
			if (!EngineInstance.Config.ShrinkFiles)
				{  output.Append("\n\n   ");  }

			string keywordHTMLName = keywordEntry.DisplayName.ToHTML();
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

			for (int i = 0; i < keywordEntry.TopicEntries.Count; i++)
				{
				if (i > 0)
					{  output.Append(',');  }

				if (!EngineInstance.Config.ShrinkFiles)
					{  output.Append("\n      ");  }

				var topicEntry = keywordEntry.TopicEntries[i];
				bool includeLanguage = false;

				if (i < keywordEntry.TopicEntries.Count - 1)
					{
					var other = keywordEntry.TopicEntries[i + 1];

					if (topicEntry.DisplayName == other.DisplayName && topicEntry.WrappedTopic.LanguageID != other.WrappedTopic.LanguageID)
						{  includeLanguage = true;  }
					}

				if (i > 0 && !includeLanguage)
					{
					var other = keywordEntry.TopicEntries[i - 1];

					if (topicEntry.DisplayName == other.DisplayName && topicEntry.WrappedTopic.LanguageID != other.WrappedTopic.LanguageID)
						{  includeLanguage = true;  }
					}

				BuildTopicEntry(topicEntry, keywordHTMLName, includeLanguage);
				}

			if (!EngineInstance.Config.ShrinkFiles)
				{  output.Append("\n   ");  }

			output.Append("]]");
			}


		/* Function: BuildTopicEntry
		 */
		protected void BuildTopicEntry (HTML.SearchIndex.Entries.Topic topicEntry, string keywordHTMLName, bool includeLanguage)
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

			if (includeLanguage)
				{
				output.Append('"');
				output.StringEscapeAndAppend( EngineInstance.Languages.FromID(topicEntry.WrappedTopic.LanguageID).Name );
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

			output.Append(UsedCommentTypesIndex(topicEntry.WrappedTopic.CommentTypeID));

			output.Append(",\"");
			Components.HTMLTopicPages.File filePage = new Components.HTMLTopicPages.File(HTMLBuilder, topicEntry.WrappedTopic.FileID);
			output.StringEscapeAndAppend(filePage.OutputFileHashPath);

			string topicHashPath = HTMLBuilder.Source_TopicHashPath(topicEntry.WrappedTopic, true);

			if (topicHashPath != null)
				{
				output.Append(':');
				output.StringEscapeAndAppend(topicHashPath);
				}

			output.Append('"');

			if (topicEntry.WrappedTopic.ClassID != 0)
				{
				output.Append(",\"");

				Components.HTMLTopicPages.Class classPage = 
					new Components.HTMLTopicPages.Class(HTMLBuilder, topicEntry.WrappedTopic.ClassID, topicEntry.WrappedTopic.ClassString);
				output.StringEscapeAndAppend(classPage.OutputFileHashPath);

				string classTopicHashPath = HTMLBuilder.Source_TopicHashPath(topicEntry.WrappedTopic, false);

				if (classTopicHashPath != null)
					{
					output.Append(':');
					output.StringEscapeAndAppend(classTopicHashPath);
					}
	
				output.Append('"');
				}

			output.Append(']');
			}


		/* Function: BuildCommentTypeList
		 */
		protected void BuildCommentTypeList (IList<HTML.SearchIndex.Entries.Keyword> keywordEntries)
			{
			output.Append('[');

			for (int i = 0; i < usedCommentTypes.Count; i++)
				{
				if (i != 0)
					{  output.Append(',');  }

				output.Append('"');
				output.StringEscapeAndAppend(usedCommentTypes[i].SimpleIdentifier);
				output.Append('"');
				}

			output.Append(']');
			}


		/* Function: UsedCommentTypesIndex
		 * Returns the index into <usedCommentTypes> of the passed comment type ID, or -1 if it isn't in the list.
		 */
		protected int UsedCommentTypesIndex (int commentTypeID)
			{
			for (int i = 0; i < usedCommentTypes.Count; i++)
				{
				if (usedCommentTypes[i].ID == commentTypeID)
					{  return i;  }
				}

			return -1;
			}



		// Group: Properties
		// __________________________________________________________________________


		public Builders.HTML HTMLBuilder
			{
			get
				{  return htmlBuilder;  }
			}

		public Engine.Instance EngineInstance
			{
			get
				{  return HTMLBuilder.EngineInstance;  }
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

		/* var: usedCommentTypes
		 * A list of the comment types used in the search data.  The order in which they appear here will be the order in which they
		 * appear in the JavaScript array.
		 */
		protected List<CommentType> usedCommentTypes;

		}
	}

