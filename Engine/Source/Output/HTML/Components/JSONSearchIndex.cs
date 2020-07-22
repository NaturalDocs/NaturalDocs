/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.JSONSearchIndex
 * ____________________________________________________________________________
 * 
 * A helper class to build JavaScript search data for <Output.Builders.HTML>.  See <JavaScript Search Data>
 * for the output format.
 * 
 * Topic: Usage
 *		
 *		- Create a JSSearchData object.
 *		- Call <ConvertToJSON()>.
 *		- Call <BuildIndexDataFile()> and <BuildPrefixDataFile()> as necessary.
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


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class JSONSearchIndex : Component
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: JSONSearchIndex
		 */
		public JSONSearchIndex (Context context) : base (context)
			{
			usedCommentTypes = null;
			}


		/* Function: ConvertToJSON
		 * Converts the passed search index to JSON.  After this you can build the data files.
		 */
		public void ConvertToJSON (SearchIndex.Manager searchIndex, Context context)
			{
			this.context = context;

			// The searchIndex parameter is really just for API consistency with the other JSON builders.  We can get it from the context.
			#if DEBUG
			if ((object)searchIndex != (object)context.Builder.SearchIndex)
				{  throw new Exception("The search index passed to ConvertToJSON must be the same as the one in the context's builder.");  }
			#endif

			// That's it.  We're going to build the JSON for the data files on demand, so this function is also just for API consistency with
			// the other JSON builders.
			}


		/* Function: BuildIndexDataFile
		 * Creates the prefix index data file as described in <JavaScript Search Data>.
		 */
		public void BuildIndexDataFile ()
			{
			var prefixes = context.Builder.SearchIndex.UsedPrefixes();

			prefixes.Sort(
				delegate (string a, string b)
					{
					// We need to use CompareOrdinal since this is what JavaScript uses.  The JavaScript wouldn't be able to do a
					// binary search on the list if we used a normal sort.
					return string.CompareOrdinal(a, b);
					}
				);

			StringBuilder output = new StringBuilder();

			output.Append("NDSearch.OnPrefixIndexLoaded([");

			int prefixCountOnLine = 0;
			bool firstPrefixOnLine = true;
			char lastPrefixStartingLetter = '\0';
			bool addWhitespace = !EngineInstance.Config.ShrinkFiles;

			foreach (string prefix in prefixes)
				{
				if (!firstPrefixOnLine)
					{  output.Append(',');  }

				if (addWhitespace)
					{
					if (prefix[0] != lastPrefixStartingLetter)
						{
						output.Append("\n\n   ");
						prefixCountOnLine = 0;
						}
					else if (prefixCountOnLine == 20)
						{
						output.Append("\n   ");
						prefixCountOnLine = 0;
						}
					}

				output.Append('"');
				output.StringEscapeAndAppend(prefix);
				output.Append('"');

				prefixCountOnLine++;
				firstPrefixOnLine = false;
				lastPrefixStartingLetter = prefix[0];
				}

			if (addWhitespace)
				{  output.Append("\n\n   ");  }

			output.Append("]);");

			try
				{  
				// This will create multiple subdirectories if needed, and will not throw an exception if it already exists.
				System.IO.Directory.CreateDirectory(Paths.SearchIndex.OutputFolder(context.Builder.OutputFolder));  
				}
			catch (Exception e)
				{
				throw new Exceptions.UserFriendly( 
					Locale.Get("NaturalDocs.Engine", "Error.CouldNotCreateOutputFolder(name, exception)",
									Paths.SearchIndex.OutputFolder(context.Builder.OutputFolder), e.Message) 
					);
				}

			System.IO.File.WriteAllText(Paths.SearchIndex.IndexOutputFile(context.Builder.OutputFolder), output.ToString());
			}


		/* Function: BuildPrefixDataFile
		 * 
		 * Creates a data file for a single prefix as described in <JavaScript Search Data>.  It requires a <CodeDB.Accessor> to
		 * be able to get information about each search result, such as what type it is and the hash path needed to get t oit.
		 * 
		 * Pass a <CancelDelegate> if you need to be able to interrupt the process, or <Delegates.NeverCancel> if not.
		 */
		public void BuildPrefixDataFile (string prefix, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{

			// Get and sort the keywords and topics

			var keywordEntries = GetPrefixKeywords(prefix, accessor, cancelDelegate);

			if (keywordEntries == null || keywordEntries.Count == 0)
				{
				context.Builder.DeleteOutputFileIfExists( Paths.SearchIndex.PrefixOutputFile(context.Builder.OutputFolder, prefix) );
				return;
				}

			SortKeywordEntries(keywordEntries);

			foreach (var keywordEntry in keywordEntries)
				{  
				SortTopicEntries(keywordEntry);
				RemoveDuplicateTopics(keywordEntry);
				}

		
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

					if (UsedCommentTypeIndex(commentTypeID) == -1)
						{  usedCommentTypes.Add( EngineInstance.CommentTypes.FromID(commentTypeID));  }
					}
				}


			// Build the output

			StringBuilder output = new StringBuilder();
			bool addWhitespace = !EngineInstance.Config.ShrinkFiles;

			output.Append("NDSearch.OnPrefixDataLoaded(\"");
			output.StringEscapeAndAppend(prefix);
			output.Append("\",");

			if (addWhitespace)
				{  output.Append("\n   ");  }

			AppendUsedCommentTypes(output);
			output.Append(',');

			if (addWhitespace)
				{  output.Append("\n   ");  }

			output.Append('[');

			bool isFirstKeywordEntry = true;

			foreach (var keywordEntry in keywordEntries)
				{
				if (isFirstKeywordEntry)
					{  isFirstKeywordEntry = false;  }
				else
					{  output.Append(',');  }

				AppendKeyword(keywordEntry, output);
				}

			if (addWhitespace)
				{  output.Append("\n\n");  }

			output.Append("]);");


			// Write it to the file

			Path path = Paths.SearchIndex.PrefixOutputFile(context.Builder.OutputFolder, prefix);

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



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: GetPrefixKeywords
		 */
		protected List<SearchIndex.Entries.Keyword> GetPrefixKeywords (string prefix, CodeDB.Accessor accessor, 
																										CancelDelegate cancelDelegate)
			{
			return SearchIndex.GetKeywordEntries(prefix, accessor, cancelDelegate);
			}


		/* Function: SortKeywordEntries
		 */
		protected void SortKeywordEntries (List<SearchIndex.Entries.Keyword> keywordEntries)
			{
			// Compare case-insensitively at first, then use case sensitivity to break ties.

			keywordEntries.Sort( 
				delegate (SearchIndex.Entries.Keyword a, SearchIndex.Entries.Keyword b)
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
		protected void SortTopicEntries (SearchIndex.Entries.Keyword keywordEntry)
			{
			List<SearchIndex.Entries.Topic> topicEntries = keywordEntry.TopicEntries;

			topicEntries.Sort(
				delegate (SearchIndex.Entries.Topic a, SearchIndex.Entries.Topic b)
					{
					// Sort by the non-qualifier part first since they'll be displayed in "Name, Class" format.  The below code is just 
					// an elaborate way of doing that without allocating intermediate strings for each comparison.

					int aNonQualifierLength = a.DisplayName.Length - a.EndOfDisplayNameQualifiers;
					int bNonQualifierLength = b.DisplayName.Length - b.EndOfDisplayNameQualifiers;
					int shorterNonQualifierLength = (aNonQualifierLength < bNonQualifierLength ? 
																	aNonQualifierLength : bNonQualifierLength);


					// Compare non-qualifiers in a case-insensitive way first.

					int result = string.Compare(a.DisplayName, a.EndOfDisplayNameQualifiers, 
															b.DisplayName, b.EndOfDisplayNameQualifiers, shorterNonQualifierLength, true);

					if (result != 0)
						{  return result;  }

					result = (aNonQualifierLength - bNonQualifierLength);

					if (result != 0)
						{  return result;  }


					// Before comparing in a case-sensitive way, compare based on hierarchy membership.  We want class "Token"
					// to appear before variable "token" even though normally we want lowercase to go first.

					var aCommentType = EngineInstance.CommentTypes.FromID(a.WrappedTopic.CommentTypeID);
					var bCommentType = EngineInstance.CommentTypes.FromID(b.WrappedTopic.CommentTypeID);

					if (aCommentType.Flags.ClassHierarchy != bCommentType.Flags.ClassHierarchy)
						{  return (aCommentType.Flags.ClassHierarchy ? -1 : 1);  }

					if (aCommentType.Flags.DatabaseHierarchy != bCommentType.Flags.DatabaseHierarchy)
						{  return (aCommentType.Flags.DatabaseHierarchy ? -1 : 1);  }


					// Still equal, now compare the qualifiers in a case-sensitive way to break ties.

					result = string.Compare(a.DisplayName, a.EndOfDisplayNameQualifiers, 
														b.DisplayName, b.EndOfDisplayNameQualifiers, shorterNonQualifierLength, false);

					if (result != 0)
						{  return result;  }

					int shorterQualifierLength = (a.EndOfDisplayNameQualifiers < b.EndOfDisplayNameQualifiers ? 
															   a.EndOfDisplayNameQualifiers : b.EndOfDisplayNameQualifiers);


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
				int keywordIndex = topicEntries[i].SearchText.IndexOf(keywordEntry.SearchText, 
																								topicEntries[i].EndOfSearchTextQualifiers);

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
					int keywordIndex = topicEntries[i].SearchText.IndexOf(keywordEntry.SearchText, 
																									topicEntries[i].EndOfSearchTextQualifiers);

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
		 * Removes topics from the results which have the same letter for letter display names.  An exception is made if 
		 * they have different languages.
		 */
		protected void RemoveDuplicateTopics (SearchIndex.Entries.Keyword keywordEntry)
			{
			List<SearchIndex.Entries.Topic> topicEntries = keywordEntry.TopicEntries;

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


		/* Function: AppendKeyword
		 * Appends the keyword entry as a JSON array.
		 */
		protected void AppendKeyword (SearchIndex.Entries.Keyword keywordEntry, StringBuilder output)
			{
			bool addWhitespace = !EngineInstance.Config.ShrinkFiles;

			if (addWhitespace)
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

				if (addWhitespace)
					{  output.Append("\n      ");  }

				var topicEntry = keywordEntry.TopicEntries[i];
				bool includeLanguage = false;

				if (i < keywordEntry.TopicEntries.Count - 1)
					{
					var other = keywordEntry.TopicEntries[i + 1];

					if (topicEntry.DisplayName == other.DisplayName && 
						topicEntry.WrappedTopic.LanguageID != other.WrappedTopic.LanguageID)
						{  includeLanguage = true;  }
					}

				if (i > 0 && !includeLanguage)
					{
					var other = keywordEntry.TopicEntries[i - 1];

					if (topicEntry.DisplayName == other.DisplayName && 
						topicEntry.WrappedTopic.LanguageID != other.WrappedTopic.LanguageID)
						{  includeLanguage = true;  }
					}

				AppendTopic(topicEntry, keywordHTMLName, includeLanguage, output);
				}

			if (addWhitespace)
				{  output.Append("\n   ");  }

			output.Append("]]");
			}


		/* Function: AppendTopic
		 * Appends a topic entry as a JSON array.
		 */
		protected void AppendTopic (SearchIndex.Entries.Topic topicEntry, string keywordHTMLName, bool includeLanguage, 
												 StringBuilder output)
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

			output.Append(UsedCommentTypeIndex(topicEntry.WrappedTopic.CommentTypeID));

			output.Append(",\"");

			Context fileContext = new Context(context.Builder, topicEntry.WrappedTopic.FileID, topicEntry.WrappedTopic);
			output.StringEscapeAndAppend(fileContext.HashPath);

			output.Append('"');

			if (topicEntry.WrappedTopic.ClassID != 0)
				{
				output.Append(",\"");

				Context classContext = new Context(context.Builder, topicEntry.WrappedTopic.ClassID, topicEntry.WrappedTopic.ClassString, 
																	  topicEntry.WrappedTopic);
				output.StringEscapeAndAppend(classContext.HashPath);

				output.Append('"');
				}

			output.Append(']');
			}


		/* Function: AppendUsedCommentTypes
		 * Appends <usedCommentTypes> to the output as a JSON array.
		 */
		protected void AppendUsedCommentTypes (StringBuilder output)
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


		/* Function: UsedCommentTypeIndex
		 * Returns the index of the passed comment type ID in <usedCommentTypes>, or -1 if it isn't in the list.
		 */
		protected int UsedCommentTypeIndex (int commentTypeID)
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


		/* Property: SearchIndex
		 * The <SearchIndex.Manager> this is being built for.
		 */
		public SearchIndex.Manager SearchIndex
			{
			get
				{  return context.Builder.SearchIndex;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: usedCommentTypes
		 * A list of the <CommentTypes> used by the prefix currently being built.
		 */
		protected List<CommentType> usedCommentTypes;

		}
	}

