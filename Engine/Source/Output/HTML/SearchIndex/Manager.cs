/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.SearchIndex.Manager
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
 *		> [String: Prefix]
 *		> [NumberSet: Prefix Topic IDs]
 *		> ...
 *		> [String: null]
 *		
 *		The file stores each prefix as a string followed by a NumberSet of its associated topic IDs.  The String-NumberSet pairs 
 *		continue in no particular order until it reaches a null ID.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Errors;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.IDObjects;
using CodeClear.NaturalDocs.Engine.Output.HTML.SearchIndex.Entries;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.SearchIndex
	{
	public class Manager : Module, CodeDB.IChangeWatcher
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Manager
		 */
		public Manager (HTML.Builder builder) : base (builder.EngineInstance)
			{
			this.builder = builder;
			accessLock = new System.Threading.ReaderWriterLockSlim(System.Threading.LockRecursionPolicy.SupportsRecursion);
			changeWatchers = new List<IChangeWatcher>();
			prefixTopicIDs = null;
			}


		/* Function: Dispose
		 */
		protected override void Dispose(bool strictRulesApply)
			{
			if (!strictRulesApply)
				{
				try
					{
					SaveBinaryFile(Builder.WorkingDataFolder + "/SearchIndex.nd", prefixTopicIDs);

					// Prior to Natural Docs 2.1 SearchIndex.nd was saved in the main working data folder instead of the output target's.
					// Clean it up if it still exists there.
					if (System.IO.File.Exists(EngineInstance.Config.WorkingDataFolder + "/SearchIndex.nd"))
						{  System.IO.File.Delete(EngineInstance.Config.WorkingDataFolder + "/SearchIndex.nd");  }
					}
				catch 
					{  }
				}
			}


		/* Function: Start
		 */
		public bool Start (ErrorList errorList)
			{

			// Load SearchIndex.nd

			bool hasBinaryFile;

			if (!EngineInstance.Config.ReparseEverything)
				{
				hasBinaryFile = LoadBinaryFile(Builder.WorkingDataFolder + "/SearchIndex.nd", out prefixTopicIDs);
				}
			else
				{
				prefixTopicIDs = new StringTable<NumberSet>(KeySettingsForPrefixes);
				hasBinaryFile = false;
				}

			if (!hasBinaryFile)
				{  EngineInstance.Config.ReparseEverything = true;  }


			// Watch CodeDB for topic changes

			EngineInstance.CodeDB.AddChangeWatcher(this);


			return true;
			}


		/* Function: PrefixTopicIDs
		 * Returns a <IDObjects.NumberSet> of all the topic IDs that are associated with the passed prefix.  If there are none, it will
		 * return null.  Do not change the NumberSet.
		 */
		public IDObjects.NumberSet PrefixTopicIDs (string prefix)
			{
			accessLock.EnterReadLock();

			try
				{  return prefixTopicIDs[prefix];  }
			finally
				{  accessLock.ExitReadLock();  }
			}


		/* Function: UsedPrefixes
		 * Returns a list of all prefixes used in the index.
		 */
		public List<string> UsedPrefixes ()
			{
			List<string> usedPrefixes = new List<string>(prefixTopicIDs.Keys.Count);

			accessLock.EnterReadLock();
			
			try
				{
				foreach (var prefixPair in prefixTopicIDs)
					{  usedPrefixes.Add(prefixPair.Key);  }
				}
			finally
				{  accessLock.ExitReadLock();  }

			return usedPrefixes;
			}


		/* Function: GetKeywordEntries
		 * Returns a list of all the <KeywordEntries> for a prefix, complete with all their <TopicEntries>.  If there are none it will return
		 * null.  The returned list will not be in any particular order, it is up to the calling code to sort them as desired.
		 */
		public List<Keyword> GetKeywordEntries (string prefix, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{

			// Retrieve the topics from the database

			IDObjects.NumberSet topicIDs = PrefixTopicIDs(prefix);

			if (topicIDs == null || topicIDs.IsEmpty)
				{  return null;  }

			List<Engine.Topics.Topic> topics = null;
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


			// Convert the topics into entries

			StringTable<Keyword> keywordEntryTable = new StringTable<Keyword>(KeySettings.IgnoreCase);

			foreach (var topic in topics)
				{
				var topicEntry = new SearchIndex.Entries.Topic(topic, this);

				foreach (var keyword in topicEntry.Keywords)
					{
					if (KeywordMatchesPrefix(keyword, prefix))
						{
						var keywordEntry = keywordEntryTable[keyword];

						if (keywordEntry == null)
							{
							keywordEntry = new Keyword(keyword);
							keywordEntryTable[keyword] = keywordEntry;
							}
						else if (keywordEntry.DisplayName != keyword)
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

							if (keywordEntry.DisplayName == keywordEntry.DisplayName.ToLower())
								{  keywordEntryCase = 'l';  }
							else if (keywordEntry.DisplayName == keywordEntry.DisplayName.ToUpper())
								{  keywordEntryCase = 'u';  }
							else
								{  keywordEntryCase = 'm';  }

							if ( (keywordCase == 'm' && keywordEntryCase != 'm') ||
								 (keywordCase == 'l' && keywordEntryCase == 'u') )
								{  
								keywordEntry.DisplayName = keyword;  
								}
							else if (keywordCase == 'm' && keywordEntryCase == 'm')
								{
								// If they're both mixed, use the sort order.  This lets SomeValue be used instead of someValue.
								if (string.Compare(keyword, keywordEntry.DisplayName) > 0)
									{  keywordEntry.DisplayName = keyword;  }
								}
							}

						keywordEntry.TopicEntries.Add(topicEntry);
						}
					}
				}

			List<Keyword> keywordEntries = new List<Keyword>(keywordEntryTable.Count);

			foreach (var keywordEntryTablePair in keywordEntryTable)
				{  keywordEntries.Add(keywordEntryTablePair.Value);  }


			return keywordEntries;
			}


		/* Function: IncludeInIndex
		 * Whether the passed <Engine.Topics.Topic> should be included in the search index.
		 */
		protected bool IncludeInIndex (Engine.Topics.Topic topic)
			{
			var commentType = EngineInstance.CommentTypes.FromID(topic.CommentTypeID);

			if (commentType.ID == EngineInstance.CommentTypes.IDFromKeyword("group"))
				{  return false;  }

			// If it's a code topic and it ends with a duplicate it's most likely a constructor.  Don't index it since the user almost
			// certainly want the class and this just pollutes the results by forcing them under a keyword heading.
			else if (commentType.Flags.Code && commentType.Flags.ClassHierarchy == false && topic.Symbol.EndsWithDuplicate())
			    {  return false;  }

			else
				{  return true;  }
			}



		// Group: Prefix Functions
		// __________________________________________________________________________


		/* Function: KeywordPrefix
		 */
		public string KeywordPrefix (string keyword)
			{
			if (keyword.Length > 3)
				{  keyword = keyword.Substring(0, 3);  }

			return keyword.ToLower();
			}

		/* Function: KeywordMatchesPrefix
		 */
		public bool KeywordMatchesPrefix (string keyword, string prefix)
			{
			return (keyword.Length >= prefix.Length && string.Compare(keyword, 0, prefix, 0, prefix.Length, true) == 0);
			}



		// Group: File Functions
		// __________________________________________________________________________


		/* Function: LoadBinaryFile
		 * Loads the information in <SearchIndex.nd> and returns whether it was successful.  If not all the out parameters will still 
		 * return objects, they will just be empty.  
		 */
		public static bool LoadBinaryFile (Path filename, out StringTable<IDObjects.NumberSet> prefixTopicIDs)
			{
			prefixTopicIDs = new StringTable<IDObjects.NumberSet>(KeySettingsForPrefixes);

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


		/* Function: SaveBinaryFile
		 * Saves the passed information in <SearchIndex.nd>.
		 */
		public static void SaveBinaryFile (Path filename, StringTable<IDObjects.NumberSet> prefixTopicIDs)
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


		public void OnAddTopic (Engine.Topics.Topic topic, CodeDB.EventAccessor eventAccessor)
			{
			if (!IncludeInIndex(topic))
				{  return;  }

			var entry =  new SearchIndex.Entries.Topic(topic, this);

			accessLock.EnterWriteLock();

			try
				{
				foreach (string keyword in entry.Keywords)
					{
					string prefix = KeywordPrefix(keyword);
					var topicIDs = prefixTopicIDs[prefix];

					if (topicIDs == null)
						{
						topicIDs = new IDObjects.NumberSet();
						topicIDs.Add(topic.TopicID);
						prefixTopicIDs[prefix] = topicIDs;

						foreach (var changeWatcher in changeWatchers)
							{  changeWatcher.OnAddPrefix(prefix, eventAccessor);  }
						}
					else
						{
						topicIDs.Add(topic.TopicID);

						foreach (var changeWatcher in changeWatchers)
							{  changeWatcher.OnUpdatePrefix(prefix, eventAccessor);  }
						}
					}
				}
			finally
				{  accessLock.ExitWriteLock();  }
			}


		public void OnUpdateTopic (Engine.Topics.Topic oldTopic, Engine.Topics.Topic newTopic, Engine.Topics.Topic.ChangeFlags changeFlags, 
											   CodeDB.EventAccessor eventAccessor)
			{
			#if DEBUG
			if (IncludeInIndex(newTopic) != IncludeInIndex(oldTopic))
				{  throw new Exception ("SearchIndex incorrectly assumes IncludeInIndex() will be the same for both the old and new topics in OnUpdateTopic().");  }
			#endif

			if (!IncludeInIndex(newTopic))
				{  return;  }

			if ((changeFlags & (Engine.Topics.Topic.ChangeFlags.Title |
										Engine.Topics.Topic.ChangeFlags.CommentTypeID | 
										Engine.Topics.Topic.ChangeFlags.SymbolDefinitonNumber |
										Engine.Topics.Topic.ChangeFlags.Symbol | 
										Engine.Topics.Topic.ChangeFlags.LanguageID | 
										Engine.Topics.Topic.ChangeFlags.FileID | 
										Engine.Topics.Topic.ChangeFlags.EffectiveAccessLevel | 
										Engine.Topics.Topic.ChangeFlags.Class)) == 0)
				{  return;  }


			// We assume that if the topics are similar enough to use OnUpdateTopic() instead of OnAdd/RemoveTopic() then they'll generate the exact 
			// same keyword list, and they'll even be in the same order.  This allows for a nice optimization here, but test it in debug builds in case these
			// assumptions are wrong in the future.

			#if DEBUG

				if (newTopic.TopicID != oldTopic.TopicID)
					{  throw new Exception ("SearchIndex incorrectly assumes both the old and new topics in OnUpdateTopic() have the same topic IDs.");  }

				var newEntry = new SearchIndex.Entries.Topic(newTopic, this);
				var oldEntry = new SearchIndex.Entries.Topic(oldTopic, this);

				if (newEntry.Keywords.Count != oldEntry.Keywords.Count)
					{  throw new Exception ("SearchIndex incorrectly assumes both the old and new topics in OnUpdateTopic() have the same keywords.");  }

				for (int i = 0; i < newEntry.Keywords.Count; i++)
					{
					if (newEntry.Keywords[i] != oldEntry.Keywords[i])
						{  throw new Exception ("SearchIndex incorrectly assumes both the old and new topics in OnUpdateTopic() have the same keywords.");  }
					}

			#endif

			var entry = new SearchIndex.Entries.Topic(newTopic, this);

			// We use upgradeable in case one of the change handlers needs to do something that requires a write lock.
			accessLock.EnterUpgradeableReadLock();

			try
				{
				foreach (var keyword in entry.Keywords)
					{
					string prefix = KeywordPrefix(keyword);

					foreach (var changeWatcher in changeWatchers)
						{  changeWatcher.OnUpdatePrefix(prefix, eventAccessor);  }
					}
				}
			finally
				{
				// We don't have to test to see if it was upgraded because if it was the write lock should have been released
				// by the change handler and we should recursively be back to an upgradeable read lock.
				accessLock.ExitUpgradeableReadLock();
				}
			}


		public void OnDeleteTopic (Engine.Topics.Topic topic, IDObjects.NumberSet linksAffected, CodeDB.EventAccessor eventAccessor)
			{
			if (!IncludeInIndex(topic))
				{  return;  }

			var entry =  new SearchIndex.Entries.Topic(topic, this);

			accessLock.EnterWriteLock();

			try
				{
				foreach (string keyword in entry.Keywords)
					{
					string prefix = KeywordPrefix(keyword);
					var numberSet = prefixTopicIDs[prefix];

					if (numberSet != null)
						{
						numberSet.Remove(topic.TopicID);

						if (numberSet.IsEmpty)
							{
							prefixTopicIDs.Remove(prefix);

							foreach (var changeWatcher in changeWatchers)
								{  changeWatcher.OnDeletePrefix(prefix, eventAccessor);  }
							}
						else
							{
							foreach (var changeWatcher in changeWatchers)
								{  changeWatcher.OnUpdatePrefix(prefix, eventAccessor);  }
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

		public void OnAddImageLink (ImageLink imageLink, CodeDB.EventAccessor eventAccessor)
			{
			// We don't care about image links.
			}

		public void OnChangeImageLinkTarget (ImageLink imageLink, int oldTargetFileID, CodeDB.EventAccessor eventAccessor)
			{
			// We don't care about image links.
			}

		public void OnDeleteImageLink (ImageLink imageLink, CodeDB.EventAccessor eventAccessor)
			{
			// We don't care about image links.
			}



		// Group: Properties
		// __________________________________________________________________________

		
		/* Property: Builder
		 * The <HTML.Builder> this seach index is associated with.
		 */
		public HTML.Builder Builder
			{
			get
				{  return builder;  }
			}



		// Group: Constants
		// __________________________________________________________________________

		protected const KeySettings KeySettingsForPrefixes = KeySettings.Literal;



		// Group: Variables
		// __________________________________________________________________________

		protected HTML.Builder builder;

		protected System.Threading.ReaderWriterLockSlim accessLock;

		protected List<IChangeWatcher> changeWatchers;

		protected StringTable<IDObjects.NumberSet> prefixTopicIDs;

		}
	}