/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.Accessor
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Symbols;


namespace GregValure.NaturalDocs.Engine.CodeDB
	{
	public partial class Accessor
		{
		
		// Group: Topic Functions
		// __________________________________________________________________________
		
		
		/* Function: GetTopicsInFile
		 * 
		 * Retrieves a list of all the topics present in the passed file ID.  The list will be in comment line number order.
		 * If there are none it will return an empty list.  Pass a <CancelDelegate> if you'd like to be able to interrupt this process,
		 * or <Delegates.NeverCancel> if not.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Topic> GetTopicsInFile (int fileID, CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadOnly);
			
			List<Topic> topics = new List<Topic>();
			
			using (SQLite.Query query = connection.Query("SELECT TopicID, Title, Body, Summary, Prototype, Symbol, Parameters, " +
																									"TopicTypeID, AccessLevel, Tags, CommentLineNumber, CodeLineNumber, " +
																									"LanguageID, PContexts.ContextString, PrototypeContextID, " +
																									"BContexts.ContextString, BodyContextID " +
																								"FROM Topics, Contexts AS PContexts, Contexts AS BContexts " +
																								"WHERE FileID = ? AND " +
																									"PContexts.ContextID = PrototypeContextID AND " +
																									"BContexts.ContextID = BodyContextID " +
																								"ORDER BY CommentLineNumber ASC", fileID))
				{
				while (query.Step() && !cancelled())
					{
					Topic topic = new Topic();
					
					topic.TopicID = query.IntColumn(0);
					topic.Title = query.StringColumn(1);
					topic.Body = query.StringColumn(2);
					topic.Summary = query.StringColumn(3);
					topic.Prototype = query.StringColumn(4);
					topic.Symbol = SymbolString.FromExportedString( query.StringColumn(5) );
					topic.Parameters = ParameterString.FromExportedString( query.StringColumn(6) );

					topic.TopicTypeID = query.IntColumn(7);
					topic.AccessLevel = (Languages.AccessLevel)query.IntColumn(8);
					topic.TagString = query.StringColumn(9);

					topic.FileID = fileID;
					topic.CommentLineNumber = query.IntColumn(10);
					topic.CodeLineNumber = query.IntColumn(11);

					topic.LanguageID = query.IntColumn(12);
					topic.PrototypeContext = ContextString.FromExportedString( query.StringColumn(13) );
					topic.PrototypeContextID = query.IntColumn(14);
					topic.BodyContext = ContextString.FromExportedString( query.StringColumn(15) );
					topic.BodyContextID = query.IntColumn(16);

					topics.Add(topic);

					contextIDCache.Add(topic.PrototypeContextID, topic.PrototypeContext);
					contextIDCache.Add(topic.BodyContextID, topic.BodyContext);
					}
				}
			
			return topics;
			}
			
			
		/* Function: AddTopic
		 * 
		 * Adds a <Topic> to the database.  Assumes it doesn't exist; if it does, you need to use <UpdateTopic()> or call <DeleteTopic()> 
		 * first.
		 * 
		 * Requirements:
		 * 
		 *		- Requires a read/write lock.  Read/possible write locks will be upgraded automatically.
		 * 
		 * Topic Requirements:
		 * 
		 *		TopicID - Must be zero.  This will be automatically assigned and the <Topic> updated.
		 *		Title - Must be set.
		 *		Body - Can be null.
		 *		Summary - Can be null.
		 *		Prototype - Can be null.
		 *		Symbol - Must be set.
		 *		Parameters - Can be null.  Will not be automatically generated though.
		 *		TopicTypeID - Must be set.
		 *		AccessLevel - Optional.  <Topic> gives it a default value if not set.
		 *		TagString - Can be null.
		 *		FileID - Must be set.
		 *		CommentLineNumber - Must be set.  If CodeLineNumber is set, <Topic> will automatically set this to it if it's not otherwise set.
		 *		CodeLineNumber - Must be set.  If CommentLineNumber is set, <Topic> will automatically set this to it if it's not otherwise set.
		 *		LanguageID - Must be set.
		 *		PrototypeContext - Can be null, which means global with no "using" statements.
		 *		PrototypeContextID - Must be zero.  This will be automatically assigned and the <Topic> updated.
		 *		BodyContext - Can be null, which means global with no "using" statements.
		 *		BodyContextID - Must be zero.  This will be automatically assigned and the <Topic> updated.
		 */
		public void AddTopic (Topic topic)
			{
			RequireZero("AddTopic", "TopicID", topic.TopicID);
			RequireContent("AddTopic", "Title", topic.Title);
			// Body
			// Summary
			// Prototype
			RequireContent("AddTopic", "Symbol", topic.Symbol);
			// Parameters
			RequireNonZero("AddTopic", "TopicTypeID", topic.TopicTypeID);
			// AccessLevel
			// TagString
			RequireNonZero("AddTopic", "FileID", topic.FileID);
			RequireNonZero("AddTopic", "CommentLineNumber", topic.CommentLineNumber);
			RequireNonZero("AddTopic", "CodeLineNumber", topic.CodeLineNumber);
			RequireNonZero("AddTopic", "LanguageID", topic.LanguageID);
			// PrototypeContext
			RequireZero("AddTopic", "PrototypeContextID", topic.PrototypeContextID);
			// BodyContext
			RequireZero("BodyContext", "BodyContextID", topic.BodyContextID);
			
			RequireAtLeast(LockType.ReadWrite);

			topic.TopicID = Engine.Instance.CodeDB.UsedTopicIDs.LowestAvailable;
			GetOrCreateContextIDs(topic);
			
			connection.Execute("INSERT INTO Topics (TopicID, Title, Body, Summary, Prototype, Symbol, Parameters, EndingSymbol, " +
													"TopicTypeID, AccessLevel, Tags, FileID, CommentLineNumber, CodeLineNumber, LanguageID, " +
													"PrototypeContextID, BodyContextID) " +
												"VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
												topic.TopicID, topic.Title, topic.Body, topic.Summary, topic.Prototype, topic.Symbol, topic.Parameters, 
												topic.Symbol.EndingSymbol, topic.TopicTypeID, (int)topic.AccessLevel, topic.TagString, topic.FileID, 
												topic.CommentLineNumber, topic.CodeLineNumber, topic.LanguageID, topic.PrototypeContextID,
												topic.BodyContextID										 
												);
			
			Engine.Instance.CodeDB.UsedTopicIDs.Add(topic.TopicID);
			
			
			IList<IChangeWatcher> changeWatchers = Engine.Instance.CodeDB.LockChangeWatchers();
			
			try
				{
				if (changeWatchers.Count > 0)
					{
					EventAccessor eventAccessor = new EventAccessor(this);
					
					foreach (IChangeWatcher changeWatcher in changeWatchers)
						{  changeWatcher.OnAddTopic(topic, eventAccessor);  }
					}
				}
			finally
				{
				Engine.Instance.CodeDB.ReleaseChangeWatchers();
				}
			}


		/* Function: UpdateTopic
		 * 
		 * Performs minor updates to an existing <Topic> in the database as determined by <Topic.DatabaseCompare()>.  If
		 * <Topic.DatabaseCompare()> returns <Topic.DatabaseCompareResult.Different> instead of
		 * <Topic.DatabaseCompareResult.Similar_WontAffectLinking> you cannot use this function.  You must delete the old topic
		 * and add it as a new one instead.
		 * 
		 * Requirements:
		 * 
		 *		- Requires a read/write lock.  Read/possible write locks will be upgraded automatically.
		 *		
		 * Topic Requirements:
		 * 
		 *		- newTopic must have all properties filled in *except* certain IDs, as in <AddTopic()>.
		 *		  - As in <AddTopic()>, these IDs will be filled in for you.
		 *		- oldTopic must have all properties filled in *including* certain IDs, as in <DeleteTopic()>.
		 *		- The topics must be similar enough that <Topic.DatabaseCompare()> returns 
		 *			<Topic.DatabaseCompareResult.Similar_WontAffectLinking>.
		 */
		public void UpdateTopic (Topic oldTopic, Topic newTopic, Topic.ChangeFlags changeFlags)
			{
			// Sanity check
			#if DEBUG
			Topic.ChangeFlags flags;
			if (oldTopic.DatabaseCompare(newTopic, out flags) != Topic.DatabaseCompareResult.Similar_WontAffectLinking)
				{  throw new InvalidOperationException("UpdateTopic can only be used with similar topics that won't affect linking.");  }
			#endif

			RequireAtLeast(LockType.ReadWrite);

			// DEPENDENCY: This must update all fields marked relevant in Topic.DatabaseCompare().  If that function changes this one
			// must change as well.

			newTopic.TopicID = oldTopic.TopicID;
			
			if ( (changeFlags & (Topic.ChangeFlags.PrototypeContext | Topic.ChangeFlags.BodyContext)) == 0)
				{
				newTopic.PrototypeContextID = oldTopic.PrototypeContextID;
				newTopic.BodyContextID = oldTopic.BodyContextID;
				}
			else
				{
				GetOrCreateContextIDs(newTopic);
				}

			connection.Execute("UPDATE Topics SET Summary=?, CommentLineNumber=?, CodeLineNumber=?, " +
													"PrototypeContextID=?, BodyContextID=? " +
												"WHERE TopicID = ?",
												newTopic.Summary, newTopic.CommentLineNumber, newTopic.CodeLineNumber,
												newTopic.PrototypeContextID, newTopic.BodyContextID, oldTopic.TopicID);

			IList<IChangeWatcher> changeWatchers = Engine.Instance.CodeDB.LockChangeWatchers();
			
			try
				{
				if (changeWatchers.Count > 0)
					{
					EventAccessor eventAccessor = new EventAccessor(this);
					
					foreach (IChangeWatcher changeWatcher in changeWatchers)
						{  changeWatcher.OnUpdateTopic(oldTopic, newTopic, changeFlags, eventAccessor);  }
					}
				}
			finally
				{
				Engine.Instance.CodeDB.ReleaseChangeWatchers();
				}
			}
			
			
		/* Function: DeleteTopic
		 * 
		 * Removes a topic from the database.
		 * 
		 * Requirements:
		 * 
		 *		- Requires a read/write lock.  Read/possible write locks will be upgraded automatically.
		 *		
		 * Topic Requirements:
		 * 
		 *		The topic must have been retrieved from the database, and thus have all its fields set.  It's important because
		 *		it will be passed to the change watchers which may need them.
		 *		
		 *		TopicID - Must be set.
		 *		Title - Must be set.
		 *		Body - Can be null.
		 *		Summary - Can be null.
		 *		Prototype - Can be null.
		 *		Symbol - Must be set.
		 *		Parameters - Can be null.
		 *		TopicTypeID - Must be set.
		 *		AccessLevel - Optional.  <Topic> gives it a default value if not set.
		 *		TagString - Can be null.
		 *		FileID - Must be set.
		 *		CommentLineNumber - Must be set.
		 *		CodeLineNumber - Must be set.
		 *		LanguageID - Must be set.
		 *		PrototypeContext - Can be null, which means global with no "using" statements.
		 *		PrototypeContextID - Must be set.
		 *		BodyContext - Can be null, which means global with no "using" statements.
		 *		BodyContextID - Must be set.
		 */
		public void DeleteTopic (Topic topic)
			{
			RequireNonZero("DeleteTopic", "TopicID", topic.TopicID);
			RequireContent("DeleteTopic", "Title", topic.Title);
			// Body
			// Summary
			// Prototype
			RequireContent("DeleteTopic", "Symbol", topic.Symbol);
			// Parameters
			RequireNonZero("DeleteTopic", "TopicTypeID", topic.TopicTypeID);
			// AccessLevel
			// TagString
			RequireNonZero("DeleteTopic", "FileID", topic.FileID);
			RequireNonZero("DeleteTopic", "CommentLineNumber", topic.CommentLineNumber);
			RequireNonZero("DeleteTopic", "CodeLineNumber", topic.CodeLineNumber);
			RequireNonZero("DeleteTopic", "LanguageID", topic.LanguageID);
			// PrototypeContext, null is a valid value
			RequireNonZero("DeleteTopic", "PrototypeContextID", topic.PrototypeContextID);
			// BodyContext, null is a valid value
			RequireNonZero("DeleteTopic", "BodyContextID", topic.BodyContextID);

			RequireAtLeast(LockType.ReadWrite);

			IList<IChangeWatcher> changeWatchers = Engine.Instance.CodeDB.LockChangeWatchers();
			
			try
				{
				if (changeWatchers.Count > 0)
					{
					EventAccessor eventAccessor = new EventAccessor(this);
					
					foreach (IChangeWatcher changeWatcher in changeWatchers)
						{  changeWatcher.OnDeleteTopic(topic, eventAccessor);  }
					}
				}
			finally
				{
				Engine.Instance.CodeDB.ReleaseChangeWatchers();
				}

			connection.Execute("DELETE FROM Topics WHERE TopicID = ?", topic.TopicID);
			Engine.Instance.CodeDB.UsedTopicIDs.Remove(topic.TopicID);

			// xxx context ids
			}
			
			
		/* Function: UpdateTopicsInFile
		 * 
		 * Replaces all the topics in the database under the passed file ID with the passed list.  It will query the existing topics itself,
		 * perform a comparison, and call <AddTopic()>, <UpdateTopic()>, and <DeleteTopic()> as necessary.  Pass a <CancelDelegate>
		 * if you'd like to be able to interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If any changes occur, it will be upgraded automatically.
		 * 
		 * Topic Requirements:
		 * 
		 *		TopicID - Must be zero.  These will be automatically assigned and the <Topics> updated.
		 *		Title - Must be set.
		 *		Body - Can be null.
		 *		Summary - Can be null.
		 *		Prototype - Can be null.
		 *		Symbol - Must be set.
		 *		Parameters - Can be null.  Will not be automatically generated though.
		 *		TopicTypeID - Must be set.
		 *		AccessLevel - Optional.  <Topic> gives it a default value if not set.
		 *		TagString - Can be null.
		 *		FileID - Must match the parameter.
		 *		CommentLineNumber - Must be set.  If CodeLineNumber is set, <Topic> will automatically set this to it if it's not otherwise set.
		 *		CodeLineNumber - Must be set.  If CommentLineNumber is set, <Topic> will automatically set this to it if it's not otherwise set.
		 *		LanguageID - Must be set.
		 *		PrototypeContext - Can be null, which means global with no "using" statements.
		 *		PrototypeContextID - Must be zero.  These will be automatically assigned and the <Topics> updated.
		 *		BodyContext - Can be null, which means global with no "using" statements.
		 *		BodyContextID - Must be zero.  These will be automatically assigned and the <Topics> updated.
		 */
		public void UpdateTopicsInFile (int fileID, IList<Topic> newTopics, CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);

			foreach (Topic newTopic in newTopics)
				{
				if (newTopic.FileID != fileID)
					{  throw new Exception ("Can't update topics in file if the file IDs don't match.");  }
				// We'll leave the rest of the topic field validation to AddTopic(), DeleteTopic(), and UpdateTopic().
				}
			
			List<Topic> oldTopics = GetTopicsInFile(fileID, cancelled);
			bool madeChanges = false;
			
			try
				{
				
				foreach (Topic newTopic in newTopics)
					{
					if (cancelled())
						{  break;  }
						
					bool foundMatch = false;
					for (int i = 0; foundMatch == false && i < oldTopics.Count; i++)
						{
						Topic.ChangeFlags changeFlags;
						Topic.DatabaseCompareResult result = newTopic.DatabaseCompare(oldTopics[i], out changeFlags);
						
						if (result == Topic.DatabaseCompareResult.Same)
							{
							foundMatch = true;
							newTopic.TopicID = oldTopics[i].TopicID;
							newTopic.PrototypeContextID = oldTopics[i].PrototypeContextID;
							newTopic.BodyContextID = oldTopics[i].BodyContextID;
							oldTopics.RemoveAt(i);
							}
						else if (result == Topic.DatabaseCompareResult.Similar_WontAffectLinking)
							{
							if (madeChanges == false)
								{
								RequireAtLeast(LockType.ReadWrite);
								BeginTransaction();
								madeChanges = true;
								}
								
							foundMatch = true;
							UpdateTopic(oldTopics[i], newTopic, changeFlags);
							oldTopics.RemoveAt(i);
							}
						}
						
					if (foundMatch == false)
						{
						if (madeChanges == false)
							{
							RequireAtLeast(LockType.ReadWrite);
							BeginTransaction();
							madeChanges = true;
							}
							
						AddTopic(newTopic);
						}
					}
					
				// All matches would have been removed, so anything left in oldTopics was deleted.
				if (oldTopics.Count > 0 && !cancelled())
					{
					if (madeChanges == false)
						{
						RequireAtLeast(LockType.ReadWrite);
						BeginTransaction();
						madeChanges = true;
						}
						
					foreach (Topic oldTopic in oldTopics)
						{  DeleteTopic(oldTopic);  }
					}
					
				if (madeChanges == true)
					{
					CommitTransaction();
					}
				}
			catch
				{
				if (madeChanges == true && inTransaction)
					{  RollbackTransactionForException();  }
					
				throw;
				}
			}
			
			
		/* Function: DeleteTopicsInFile
		 * 
		 * Deletes all the topics in the database under the passed file ID.  Pass a <CancelDelegate> if you'd like to be able to
		 * interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If any deletions occur, it will be upgraded automatically.
		 */
		public void DeleteTopicsInFile (int fileID, CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);
			
			List<Topic> topics = GetTopicsInFile(fileID, cancelled);
			
			if (topics.Count > 0 && !cancelled())
				{
				RequireAtLeast(LockType.ReadWrite);
				BeginTransaction();
					
				foreach (Topic topic in topics)
					{  
					DeleteTopic(topic);
					
					if (cancelled())
						{  break;  }
					}

				CommitTransaction();
				}
			}



		// Group: Context Functions
		// __________________________________________________________________________


		/* Function: GetOrCreateContextIDs
		 * 
		 * Retrieves the context IDs for <Topic.PrototypeContext> and <Topic.BodyContext> if they are not already set.
		 * If existing IDs cannot be found, they will be created.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If new contexts are created, it will be upgraded automatically.
		 *		
		 * Topic Requirements:
		 * 
		 *		PrototypeContext - Can be null, which means global with no "using" statements.
		 *		PrototypeContextID - If zero PrototypeContext will be looked up and an ID assigned.  If non-zero no lookup will occur.
		 *		BodyContext - Can be null, which means global with no "using" statements.
		 *		BodyContextID - If zero BodyContext will be looked up and an ID assigned.  If non-zero no lookup will occur.
		 */
		public void GetOrCreateContextIDs (Topic topic)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);


			// Cache or create any missing context IDs.  Since there's only two fields to check we'll handle this by hand to minimize 
			// memory instead of creating a HashSet or List.

			if (topic.PrototypeContextID == 0)
				{
				if (topic.BodyContextID == 0 && topic.BodyContext != topic.PrototypeContext)
					{
					ContextString[] contexts = new ContextString[2] { topic.PrototypeContext, topic.BodyContext };
					CacheOrCreateContextIDs(contexts);
					}
				else
					{
					ContextString[] contexts = new ContextString[1] { topic.PrototypeContext };
					CacheOrCreateContextIDs(contexts);
					}
				}
			else if (topic.BodyContextID == 0)
				{
				ContextString[] contexts = new ContextString[1] { topic.BodyContext };
				CacheOrCreateContextIDs(contexts);
				}
			else
				{  return;  }


			// Fill in the Topic.

			if (topic.PrototypeContextID == 0)
				{  topic.PrototypeContextID = contextIDCache[topic.PrototypeContext].ID;  }

			if (topic.BodyContextID == 0)
				{  topic.BodyContextID = contextIDCache[topic.BodyContext].ID;  }
			}


		/* Function: GetOrCreateContextIDs
		 * 
		 * Retrieves the context IDs for each <Topic's> PrototypeContext and BodyContext if they are not already set.  If existing
		 * IDs cannot be found, they will be created.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If new contexts are created, it will be upgraded automatically.
		 *		
		 * Topic Requirements:
		 * 
		 *		PrototypeContext - Can be null, which means global with no "using" statements.
		 *		PrototypeContextID - If zero PrototypeContext will be looked up and an ID assigned.  If non-zero no lookup will occur.
		 *		BodyContext - Can be null, which means global with no "using" statements.
		 *		BodyContextID - If zero BodyContext will be looked up and an ID assigned.  If non-zero no lookup will occur.
		 */
		public void GetOrCreateContextIDs (IEnumerable<Topic> topics)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);


			// Cache or create any missing context IDs.  There may be none so create the HashSet on demand.

			// HashSet handles null ContextStrings fine.
			HashSet<ContextString> contexts = null;

			foreach (Topic topic in topics)
				{
				if (contexts == null && (topic.PrototypeContextID == 0 || topic.BodyContextID == 0))
					{  contexts = new HashSet<ContextString>();  }

				if (topic.PrototypeContextID == 0)
					{  contexts.Add(topic.PrototypeContext);  }

				if (topic.BodyContextID == 0)
					{  contexts.Add(topic.BodyContext);  }
				}

			if (contexts != null)
				{  CacheOrCreateContextIDs(contexts);  }
			else
				{  return;  }


			// Fill in the Topics.

			foreach (Topic topic in topics)
				{
				if (topic.PrototypeContextID == 0)
					{  topic.PrototypeContextID = contextIDCache[topic.PrototypeContext].ID;  }

				if (topic.BodyContextID == 0)
					{  topic.BodyContextID = contextIDCache[topic.BodyContext].ID;  }
				}
			}


		/* Function: CacheOrCreateContextIDs
		 * 
		 * Retrieves the IDs for each <ContextString> and stores them in <contextIDCache>.  If they don't exist in the 
		 * database they will be created.
		 * 
		 * If the collection you pass in doesn't support null strings you can set plusNullContext to true and it will be included.
		 * If it does support them you are fine just including it in the collection and leaving plusNullContext false, even if there 
		 * may be one in the collection.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If new contexts are created, it will be upgraded automatically.
		 */
		protected void CacheOrCreateContextIDs (IEnumerable<ContextString> contextStrings, bool plusNullContext = false)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);

			
			// Create a list of all contextStrings not already in the cache.  Since it's possible that they'll all be in the cache we
			// create the list object on demand.

			List<string> uncachedContextStrings = null;

			foreach (string contextString in contextStrings)
				{
				if (contextIDCache.Contains(contextString) == false)
					{
					if (contextString == null)
						{  plusNullContext = true;  }
					else
						{  
						if (uncachedContextStrings == null)
							{  uncachedContextStrings = new List<string>();  }

						uncachedContextStrings.Add(contextString);  
						}
					}
				}

			if (plusNullContext && contextIDCache.Contains(null))
				{  plusNullContext = false;  }


			// Can we quit early?

			if (uncachedContextStrings == null && plusNullContext == false)
				{  return;  }


			// Create a query to lookup the uncached contexts in the database.  They may exist there.

			System.Text.StringBuilder queryText = new System.Text.StringBuilder("SELECT ContextID, ContextString FROM Contexts WHERE");
			string[] queryParams = null;
			bool firstWhere = true;

			if (uncachedContextStrings != null)
				{
				for (int i = 0; i < uncachedContextStrings.Count; i++)
					{
					if (!firstWhere)
						{  queryText.Append(" OR");  }

					queryText.Append(" ContextString=?");
					firstWhere = false;
					}

				queryParams = uncachedContextStrings.ToArray();
				}

			if (plusNullContext)
				{
				if (!firstWhere)
					{  queryText.Append(" OR");  }

				queryText.Append(" ContextString IS NULL");
				firstWhere = false;
				}


			// Run the query to fill in the cache with whatever already exists in the database.

			using (SQLite.Query query = (queryParams == null ? connection.Query(queryText.ToString()) :
																										 connection.Query(queryText.ToString(), queryParams) ))
				{
			   while (query.Step())
			      {  contextIDCache.Add( query.IntColumn(0), ContextString.FromExportedString(query.StringColumn(1)) );  }
			   }


			// Pare down our list of uncached context strings.

			if (uncachedContextStrings != null)
				{
				int i = 0;
				while (i < uncachedContextStrings.Count)
					{
					if (contextIDCache.Contains(uncachedContextStrings[i]))
						{  uncachedContextStrings.RemoveAt(i);  }
					else
						{  i++;  }
					}

				if (uncachedContextStrings.Count == 0)
					{  uncachedContextStrings = null;  }
				}

			if (plusNullContext && contextIDCache.Contains(null))
				{  plusNullContext = false;  }


			// Can we quit now?

			if (uncachedContextStrings == null && plusNullContext == false)
				{  return;  }


			// Create anything we still need.

			RequireAtLeast(LockType.ReadWrite);

			using (SQLite.Query query = connection.Query("INSERT INTO Contexts (ContextID, ContextString, ReferenceCount) " +
																								 "VALUES (?, ?, 0)") )
				{
				if (plusNullContext)
					{
					int id = Engine.Instance.CodeDB.UsedContextIDs.LowestAvailable;

					query.BindValues(id, null);
					query.Step();
					query.Reset(true);

					Engine.Instance.CodeDB.UsedContextIDs.Add(id);
					contextIDCache.Add(id, new ContextString());
					}

				if (uncachedContextStrings != null)
					{
					foreach (string contextString in uncachedContextStrings)
						{
						int id = Engine.Instance.CodeDB.UsedContextIDs.LowestAvailable;

						query.BindValues(id, contextString);
						query.Step();
						query.Reset(true);

						Engine.Instance.CodeDB.UsedContextIDs.Add(id);
						contextIDCache.Add(id, ContextString.FromExportedString(contextString));
						}
					}
				}
			}



		// Group: Transaction Functions
		// __________________________________________________________________________
		
		
		/* Function: BeginTransaction
		 * 
		 * Starts a new transaction.  Note that transactions MUST BE COMMITTED except in error conditions like exceptions.
		 * There are other changes to Natural Docs' state that occur with each change independently of transactions so they
		 * cannot be rolled back with the database.
		 * 
		 * Requirements:
		 * 
		 *		- You must have a read/write lock.  Read/possible write locks will be upgraded automatically.
		 */
		protected void BeginTransaction ()
			{
			RequireAtLeast(LockType.ReadWrite);
			
			if (inTransaction)
				{  throw new Exception("Tried to create a transaction when one was already in effect.");  }
				
			connection.Execute("BEGIN IMMEDIATE TRANSACTION");
			inTransaction = true;
			}
			
		/* Function: CommitTransaction
		 * 
		 * Commits an existing transaction to the database.
		 * 
		 * Requirements:
		 * 
		 *		- You must have a read/write lock.  Read/possible write locks will be upgraded automatically.
		 */
		protected void CommitTransaction ()
			{
			RequireAtLeast(LockType.ReadWrite);
			
			if (!inTransaction)
				{  throw new Exception("Tried to commit a transaction when one was not in effect.");  } 
			
			connection.Execute("COMMIT TRANSACTION");
			inTransaction = false;
			}

		/* Function: RollbackTransactionForException
		 * 
		 * Rolls back an existing transaction from the database because an exception occurred.  This is the ONLY reason
		 * you can roll back a transaction because there are other state changes within Natural Docs that occur and would
		 * not be reverted with the database.  This function only exists so that you can get out of the transaction if an
		 * exception occurs, which prevents an additional exception from occurring if you try to dispose of the Accessor
		 * while a transaction is still in effect.
		 * 
		 * Requirements:
		 * 
		 *		- You must have a read/write lock.  Read/possible write locks will be upgraded automatically.
		 */
		protected void RollbackTransactionForException ()
			{
			if (inTransaction)
				{  
				connection.Execute("ROLLBACK TRANSACTION");
				inTransaction = false;
				}
			}

		}
	}