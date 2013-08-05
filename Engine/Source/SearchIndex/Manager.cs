/* 
 * Class: GregValure.NaturalDocs.Engine.SearchIndex.Manager
 * ____________________________________________________________________________
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		Externally, this class is thread safe.  All locking is handled internally so you may access it from multiple threads.
 *		Internally all access is handled by <accessLock>.
 *		
 * 
 * File: SearchIndex.nd
 * 
 *		A file used to store the state of the search index.
 *		
 *		> [String: Keyword Segment ID]
 *		> [NumberSet: Topic IDs in Keyword Segment]
 *		> ...
 *		> [String: null]
 *		
 *		The file stores each keyword segment as a string ID followed by a NumberSet of its associated topic IDs.  The
 *		String-NumberSet pairs continue in no particular order until it reaches a null ID.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Errors;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.IDObjects;
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.SearchIndex
	{
	public class Manager : IDisposable, CodeDB.IChangeWatcher
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Manager
		 */
		public Manager ()
			{
			accessLock = new System.Threading.ReaderWriterLockSlim(System.Threading.LockRecursionPolicy.SupportsRecursion);
			changeWatchers = new List<IChangeWatcher>();
			keywordSegments = null;
			}
			
			
		/* Function: Dispose
		 */
		public void Dispose ()
			{
			try
				{
				SaveBinaryFile(Engine.Instance.Config.WorkingDataFolder + "/SearchIndex.nd", keywordSegments);
				}
			catch 
				{  }
			}


		/* Function: Start
		 */
		public bool Start (ErrorList errorList)
			{

			// Load SearchIndex.nd

			bool hasBinaryFile;

			if (!Engine.Instance.Config.ReparseEverything)
				{
				hasBinaryFile = LoadBinaryFile(Engine.Instance.Config.WorkingDataFolder + "/SearchIndex.nd", out keywordSegments);
				}
			else
				{
				keywordSegments = new StringTable<NumberSet>(KeySettingsForKeywordSegmentIDs);
				hasBinaryFile = false;
				}

			if (!hasBinaryFile)
				{  Engine.Instance.Config.ReparseEverything = true;  }


			// Watch CodeDB for topic changes

			Engine.Instance.CodeDB.AddChangeWatcher(this);


			return true;
			}


		/* Function: TopicIDsInKeywordSegment
		 * Returns a <IDObjects.NumberSet> of all the topic IDs that are associated with the passed segment ID.  If there are none,
		 * it will return null.  Do not change the NumberSet.
		 */
		public IDObjects.NumberSet TopicIDsInKeywordSegment (string keywordSegmentID)
			{
			accessLock.EnterReadLock();

			try
				{  return keywordSegments[keywordSegmentID];  }
			finally
				{  accessLock.ExitReadLock();  }
			}


		/* Function: KeywordSegmentIDs
		 * Returns a list of all keyword segment IDs in the index.
		 */
		public List<string> KeywordSegmentIDs ()
			{
			List<string> keywordSegmentIDs = new List<string>(keywordSegments.Keys.Count);

			accessLock.EnterReadLock();
			
			try
				{
				foreach (var keywordSegment in keywordSegments)
					{  keywordSegmentIDs.Add(keywordSegment.Key);  }
				}
			finally
				{  accessLock.ExitReadLock();  }

			return keywordSegmentIDs;
			}


		/* Function: BuildKeywordSegment
		 * Returns a list of all the <KeywordEntries> in a segment ID, complete with all their <TopicEntries>.  If there are none it will return
		 * null.
		 */
		public List<KeywordEntry> BuildKeywordSegment (string keywordSegmentID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{

			// Retrieve the topics from the database

			IDObjects.NumberSet topicIDs = TopicIDsInKeywordSegment(keywordSegmentID);

			if (topicIDs == null || topicIDs.IsEmpty)
				{  return null;  }

			List<Topic> topics = null;
			bool releaseDBLock = false;

			if (accessor.LockHeld == CodeDB.Accessor.LockType.None)
				{
				accessor.GetReadOnlyLock();
				releaseDBLock = true;
				}

			try
				{
				// Need to lookup class strings to be able to build hash paths
				topics = accessor.GetTopicsByID(topicIDs, cancelDelegate, CodeDB.Accessor.GetTopicFlags.BodyLengthOnly | 
																								CodeDB.Accessor.GetTopicFlags.DontLookupContexts | 
																								CodeDB.Accessor.GetTopicFlags.DontIncludeSummary | 
																								CodeDB.Accessor.GetTopicFlags.DontIncludePrototype);
				}
			finally
				{
				if (releaseDBLock)
					{  accessor.ReleaseLock();  }
				}

			if (cancelDelegate())
				{  return null;  }


			// Convert the topics into entries and sort them by keyword

			StringTable<KeywordEntry> keywordEntryTable = new StringTable<KeywordEntry>(KeySettings.IgnoreCase);

			foreach (var topic in topics)
				{
				TopicEntry topicEntry = new TopicEntry(topic);

				foreach (var keyword in topicEntry.Keywords)
					{
					if (KeywordIsInSegmentID(keyword, keywordSegmentID))
						{
						var keywordEntry = keywordEntryTable[keyword];

						if (keywordEntry == null)
							{
							keywordEntry = new KeywordEntry(keyword);
							keywordEntryTable[keyword] = keywordEntry;
							}
						else if (keywordEntry.Keyword != keyword)
							{
							// If they differ in case we still want to combine them, but we have to choose which case will be in the
							// results.  Prioritize in this order: mixed case ('m'), all lowercase ('l'), all uppercase ('u').

							char keywordCase, keywordEntryCase;

							if (keyword == keyword.ToLower())
								{  keywordCase = 'l';  }
							else if (keyword == keyword.ToUpper())
								{  keywordCase = 'u';  }
							else
								{  keywordCase = 'm';  }

							if (keywordEntry.Keyword == keywordEntry.Keyword.ToLower())
								{  keywordEntryCase = 'l';  }
							else if (keywordEntry.Keyword == keywordEntry.Keyword.ToUpper())
								{  keywordEntryCase = 'u';  }
							else
								{  keywordEntryCase = 'm';  }

							if ( (keywordCase == 'm' && keywordEntryCase != 'm') ||
								 (keywordCase == 'l' && keywordEntryCase == 'u') )
								{  
								keywordEntry.Keyword = keyword;  
								}
							else if (keywordCase == 'm' && keywordEntryCase == 'm')
								{
								// If they're both mixed, use the sort order.  This lets SomeValue be used instead of someValue.
								if (string.Compare(keyword, keywordEntry.Keyword) > 0)
									{  keywordEntry.Keyword = keyword;  }
								}
							}

						keywordEntry.TopicEntries.Add(topicEntry);
						}
					}
				}

			List<KeywordEntry> keywordEntries = new List<KeywordEntry>(keywordEntryTable.Count);

			foreach (var keywordEntryTablePair in keywordEntryTable)
				{  keywordEntries.Add(keywordEntryTablePair.Value);  }

			keywordEntries.Sort( 
				delegate (KeywordEntry a, KeywordEntry b)
					{
					int result = string.Compare(a.Keyword, b.Keyword, true);

					if (result != 0)
						{  return result;  }

					return string.Compare(a.Keyword, b.Keyword, false);
					}
				);


			// Also sort the topic entries in each keyword entry

			foreach (var keywordEntry in keywordEntries)
				{
				keywordEntry.TopicEntries.Sort(
					delegate (TopicEntry a, TopicEntry b)
						{
						int result = string.Compare(a.DisplayName, b.DisplayName, true);

						if (result != 0)
							{  return result;  }

						return string.Compare(a.DisplayName, b.DisplayName, false);
						}
					);
				}


			return keywordEntries;
			}


		/* Function: IncludeInIndex
		 * Whether the passed <Topic> should be included in the search index.
		 */
		protected bool IncludeInIndex (Topic topic)
			{
			var topicType = Engine.Instance.TopicTypes.FromID(topic.TopicTypeID);

			if (topicType.ID == Engine.Instance.TopicTypes.IDFromKeyword("group"))
				{  return false;  }
			else
				{  return true;  }
			}



		// Group: Segment ID Functions
		// __________________________________________________________________________


		/* Function: KeywordSegmentID
		 */
		public string KeywordSegmentID (string keyword)
			{
			if (keyword.Length > 3)
				{  keyword = keyword.Substring(0, 3);  }

			return keyword.ToLower();
			}

		/* Function: KeywordIsInSegmentID
		 */
		public bool KeywordIsInSegmentID (string keyword, string keywordSegmentID)
			{
			return (keyword.Length >= keywordSegmentID.Length &&
						  string.Compare(keyword, 0, keywordSegmentID, 0, keywordSegmentID.Length, true) == 0);
			}



		// Group: File Functions
		// __________________________________________________________________________


		/* Function: LoadBinaryFile
		 * Loads the information in <SearchIndex.nd> and returns whether it was successful.  If not all the out parameters will still 
		 * return objects, they will just be empty.  
		 */
		public static bool LoadBinaryFile (Path filename, out StringTable<IDObjects.NumberSet> keywordSegments)
			{
			keywordSegments = new StringTable<IDObjects.NumberSet>(KeySettingsForKeywordSegmentIDs);

			BinaryFile binaryFile = new BinaryFile();
			bool result = true;

			try
				{
				if (binaryFile.OpenForReading(filename, "2.0") == false)
					{  result = false;  }
				else
					{
					// [String: Keyword Segment ID]
					// [NumberSet: Topic IDs in Keyword Segment]
					// ...
					// [String: null]

					for (;;)
						{
						string keywordSegmentID = binaryFile.ReadString();

						if (keywordSegmentID == null)
							{  break;  }

						IDObjects.NumberSet topicIDs = binaryFile.ReadNumberSet();
						keywordSegments.Add(keywordSegmentID, topicIDs);
						}
					}
				}
			catch
				{  result = false;  }
			finally
				{  binaryFile.Dispose();  }

			if (result == false)
				{  keywordSegments.Clear();  }

			return result;
			}


		/* Function: SaveBinaryFile
		 * Saves the passed information in <SearchIndex.nd>.
		 */
		public static void SaveBinaryFile (Path filename, StringTable<IDObjects.NumberSet> keywordSegments)
			{
			using (BinaryFile binaryFile = new BinaryFile())
				{
				binaryFile.OpenForWriting(filename);

				// [String: Keyword Segment ID]
				// [NumberSet: Topic IDs in Keyword Segment]
				// ...
				// [String: null]

				foreach (var keywordSegment in keywordSegments)
					{
					binaryFile.WriteString(keywordSegment.Key);
					binaryFile.WriteNumberSet(keywordSegment.Value);
					}

				binaryFile.WriteString(null);
				}
			}



		// Group: SearchIndex.IChangeWatcher Functions
		// __________________________________________________________________________


		/* Function: AddChangeWatcher
		 * Adds an object to be notified about changes to the index.  This can be called both before and after <Start()>.
		 */
		public void AddChangeWatcher (IChangeWatcher watcher)
			{
			accessLock.EnterWriteLock();

			try
				{  changeWatchers.Add(watcher);  }
			finally
				{  accessLock.ExitWriteLock();  }
			}
			
			
		/* Function: AddPriorityChangeWatcher
		 * Adds an object to be notified about changes to the index.  Ones added with this function will receive change
		 * notifications before ones that aren't.  This can be called both before and after <Start()>.
		 */
		public void AddPriorityChangeWatcher (IChangeWatcher watcher)
			{
			accessLock.EnterWriteLock();

			try
				{  changeWatchers.Insert(0, watcher);  }
			finally
				{  accessLock.ExitWriteLock();  }
			}
			
			
		/* Function: RemoveChangeWatcher
		 * Removes a watcher so that they're no longer notified of changes to the index.  It doesn't matter which function
		 * you used to add it with.  This can be called both before and after <Start()>.
		 */
		public void RemoveChangeWatcher (IChangeWatcher watcher)
			{
			accessLock.EnterWriteLock();

			try
				{
				for (int i = 0; i < changeWatchers.Count; i++)
					{
					if ((object)watcher == (object)changeWatchers[i])
						{
						changeWatchers.RemoveAt(i);
						return;
						}
					}
				}
			finally
				{  accessLock.ExitWriteLock();  }
			}



		// Group: CodeDB.IChangeWatcher Functions
		// __________________________________________________________________________


		public void OnAddTopic (Topic topic, CodeDB.EventAccessor eventAccessor)
			{
			if (!IncludeInIndex(topic))
				{  return;  }

			TopicEntry entry =  new TopicEntry(topic);

			accessLock.EnterWriteLock();

			try
				{
				foreach (string keyword in entry.Keywords)
					{
					string keywordSegmentID = KeywordSegmentID(keyword);
					var keywordSegment = keywordSegments[keywordSegmentID];

					if (keywordSegment == null)
						{
						keywordSegment = new IDObjects.NumberSet();
						keywordSegment.Add(topic.TopicID);
						keywordSegments[keywordSegmentID] = keywordSegment;

						foreach (var changeWatcher in changeWatchers)
							{  changeWatcher.OnAddSegment(keywordSegmentID, eventAccessor);  }
						}
					else
						{
						keywordSegment.Add(topic.TopicID);

						foreach (var changeWatcher in changeWatchers)
							{  changeWatcher.OnUpdateSegment(keywordSegmentID, eventAccessor);  }
						}
					}
				}
			finally
				{  accessLock.ExitWriteLock();  }
			}


		public void OnUpdateTopic (Topic oldTopic, Topic newTopic, Topic.ChangeFlags changeFlags, CodeDB.EventAccessor eventAccessor)
			{
			#if DEBUG
			if (IncludeInIndex(newTopic) != IncludeInIndex(oldTopic))
				{  throw new Exception ("SearchIndex incorrectly assumes IncludeInIndex() will be the same for both the old and new topics in OnUpdateTopic().");  }
			#endif

			if (!IncludeInIndex(newTopic))
				{  return;  }

			if ((changeFlags & (Topic.ChangeFlags.Title | Topic.ChangeFlags.TopicTypeID | Topic.ChangeFlags.SymbolDefinitonNumber |
									  Topic.ChangeFlags.Symbol | Topic.ChangeFlags.LanguageID | Topic.ChangeFlags.FileID | 
									  Topic.ChangeFlags.EffectiveAccessLevel | Topic.ChangeFlags.Class)) == 0)
				{  return;  }


			// We assume that if the topics are similar enough to use OnUpdateTopic() instead of OnAdd/RemoveTopic() then they'll generate the exact 
			// same keyword list, and they'll even be in the same order.  This allows for a nice optimization here, but test it in debug builds in case these
			// assumptions are wrong in the future.

			#if DEBUG

				if (newTopic.TopicID != oldTopic.TopicID)
					{  throw new Exception ("SearchIndex incorrectly assumes both the old and new topics in OnUpdateTopic() have the same topic IDs.");  }

				TopicEntry newEntry = new TopicEntry(newTopic);
				TopicEntry oldEntry = new TopicEntry(oldTopic);

				if (newEntry.Keywords.Count != oldEntry.Keywords.Count)
					{  throw new Exception ("SearchIndex incorrectly assumes both the old and new topics in OnUpdateTopic() have the same keywords.");  }

				for (int i = 0; i < newEntry.Keywords.Count; i++)
					{
					if (newEntry.Keywords[i] != oldEntry.Keywords[i])
						{  throw new Exception ("SearchIndex incorrectly assumes both the old and new topics in OnUpdateTopic() have the same keywords.");  }
					}

			#endif

			// We use upgradeable in case one of the change handlers needs to do something that requires a write lock.
			accessLock.EnterUpgradeableReadLock();

			try
				{
				foreach (var keyword in newEntry.Keywords)
					{
					string keywordSegmentID = KeywordSegmentID(keyword);

					foreach (var changeWatcher in changeWatchers)
						{  changeWatcher.OnUpdateSegment(keywordSegmentID, eventAccessor);  }
					}
				}
			finally
				{
				// We don't have to test to see if it was upgraded because if it was the write lock should have been released
				// by the change handler and we should recursively be back to an upgradeable read lock.
				accessLock.ExitUpgradeableReadLock();
				}
			}


		public void OnDeleteTopic (Topic topic, CodeDB.EventAccessor eventAccessor)
			{
			if (!IncludeInIndex(topic))
				{  return;  }

			TopicEntry entry =  new TopicEntry(topic);

			accessLock.EnterWriteLock();

			try
				{
				foreach (string keyword in entry.Keywords)
					{
					string keywordSegmentID = KeywordSegmentID(keyword);
					var keywordSegment = keywordSegments[keywordSegmentID];

					if (keywordSegment != null)
						{
						keywordSegment.Remove(topic.TopicID);

						if (keywordSegment.IsEmpty)
							{
							keywordSegments.Remove(keywordSegmentID);

							foreach (var changeWatcher in changeWatchers)
								{  changeWatcher.OnDeleteSegment(keywordSegmentID, eventAccessor);  }
							}
						else
							{
							foreach (var changeWatcher in changeWatchers)
								{  changeWatcher.OnUpdateSegment(keywordSegmentID, eventAccessor);  }
							}
						}
					}
				}
			finally
				{  accessLock.ExitWriteLock();  }
			}


		public void OnAddLink (Link link, CodeDB.EventAccessor eventAccessor)
			{
			// We don't care about links.
			}

		public void OnChangeLinkTarget (Link link,	int oldTargetTopicID, int oldTargetClassID, CodeDB.EventAccessor eventAccessor)
			{
			// We don't care about links.
			}

		public void OnDeleteLink (Link link, CodeDB.EventAccessor eventAccessor)
			{
			// We don't care about links.
			}



		// Group: Constants
		// __________________________________________________________________________

		protected const KeySettings KeySettingsForKeywordSegmentIDs = KeySettings.Literal;



		// Group: Variables
		// __________________________________________________________________________

		protected System.Threading.ReaderWriterLockSlim accessLock;

		protected List<IChangeWatcher> changeWatchers;

		protected StringTable<IDObjects.NumberSet> keywordSegments;

		}
	}