/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.Accessor
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Symbols;


namespace GregValure.NaturalDocs.Engine.CodeDB
	{
	public partial class Accessor
		{
		
		// Group: Topic Functions
		// __________________________________________________________________________
		
		
		/* Function: GetTopics
		 * 
		 * A generic function for retrieving all the <Topics> that satisfy the passed WHERE clause.  If there are none it will return 
		 * an empty list.
		 * 
		 * Parameters:
		 * 
		 *		whereClause - The SQL WHERE clause to apply to the query, such as "FileID=?".
		 *		orderByClause - The SQL ORDER BY clause to apply to the query, such as "CommentLineNumber ASC", or null if none.
		 *		clauseParameters - Any parameters needed for question marks in the WHERE and ORDER BY clauses, or null if none.
		 *		cancelled - A <CancelDelegate> you can use to interrupt this process.  Pass <Delegates.NeverCancel> if you won't
		 *							 need to.
		 *		ignoreFields - If you don't need every property in the <Topic> object you can set this to filter some out.  Not every
		 *								 flag will be respected by the query but some that will save a lot of memory or processing time may be.
		 *								 In debug builds <Topic> will enforce these settings regardless of whether the query filled them in or not 
		 *								 to prevent programming errors.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		protected List<Topic> GetTopics (string whereClause, string orderByClause, object[] clauseParameters,
																	 CancelDelegate cancelled, Topic.IgnoreFields ignoreFields = Topic.IgnoreFields.None)
			{
			RequireAtLeast(LockType.ReadOnly);

			List<Topic> topics = new List<Topic>();

			bool ignoreBody = ((ignoreFields & Topic.IgnoreFields.Body) != 0);
			bool ignoreContexts = ((ignoreFields & (Topic.IgnoreFields.BodyContext | Topic.IgnoreFields.PrototypeContext)) != 0);
						
			StringBuilder queryText = new StringBuilder("SELECT TopicID, Title, Summary, Prototype, Symbol, SymbolDefinitionNumber, " +
																						  "IsEmbedded, TopicTypeID, AccessLevel, Tags, CommentLineNumber, CodeLineNumber, " +
																						  "LanguageID, PrototypeContextID, BodyContextID, FileID ");

			if (!ignoreBody)
				{  queryText.Append(", Body ");  }
			else
				{  queryText.Append(", length(Body) ");  }

			if (!ignoreContexts)
				{  queryText.Append(", PContexts.ContextString, BContexts.ContextString ");  }
																								
			queryText.Append("FROM Topics ");
			
			if (!ignoreContexts)
				{  queryText.Append(", Contexts AS PContexts, Contexts AS BContexts ");  }
				
			queryText.Append("WHERE ");
			
			if (!ignoreContexts)
				{
				queryText.Append("PContexts.ContextID = PrototypeContextID AND " +
												 "BContexts.ContextID = BodyContextID AND ");
				}

			queryText.Append('(');
			queryText.Append(whereClause);
			queryText.Append(')');

			if (orderByClause != null)
				{
				queryText.Append(" ORDER BY ");
				queryText.Append(orderByClause);
				}

			using (SQLite.Query query = connection.Query(queryText.ToString(), clauseParameters))
				{
				while (query.Step() && !cancelled())
					{
					Topic topic = new Topic();
					
					topic.TopicID = query.IntColumn(0);
					topic.Title = query.StringColumn(1);
					topic.Summary = query.StringColumn(2);
					topic.Prototype = query.StringColumn(3);
					topic.Symbol = SymbolString.FromExportedString( query.StringColumn(4) );
					topic.SymbolDefinitionNumber = query.IntColumn(5);
					topic.IsEmbedded = (query.IntColumn(6) == 1);

					topic.TopicTypeID = query.IntColumn(7);
					topic.AccessLevel = (Languages.AccessLevel)query.IntColumn(8);
					topic.TagString = query.StringColumn(9);

					topic.CommentLineNumber = query.IntColumn(10);
					topic.CodeLineNumber = query.IntColumn(11);

					topic.LanguageID = query.IntColumn(12);
					topic.PrototypeContextID = query.IntColumn(13);
					topic.BodyContextID = query.IntColumn(14);
					topic.FileID = query.IntColumn(15);

					if (!ignoreBody)
						{  topic.Body = query.StringColumn(16);  }
					else
						{  topic.BodyLength = query.IntColumn(16);  }

					if (!ignoreContexts)
						{
						topic.PrototypeContext = ContextString.FromExportedString( query.StringColumn(17) );
						topic.BodyContext = ContextString.FromExportedString( query.StringColumn(18) );
						}

					topics.Add(topic);

					if (!ignoreContexts)
						{
						contextIDLookupCache.Add(topic.PrototypeContextID, topic.PrototypeContext);
						contextIDLookupCache.Add(topic.BodyContextID, topic.BodyContext);
						}

					// Set this last so that we don't cause exceptions by filling in fields that should have been ignored.  From
					// this point forward it will be enforced, including preventing access to ones we filled in unnecessarily.
					topic.IgnoredFields = ignoreFields;
					}
				}
			
			return topics;
			}
			
			
		/* Function: GetTopicsInFile
		 * 
		 * Retrieves a list of all the topics present in the passed file ID.  The list will be in comment line number order.
		 * If there are none it will return an empty list.  Pass a <CancelDelegate> if you'd like to be able to interrupt this process,
		 * or <Delegates.NeverCancel> if not.
		 * 
		 * If you don't need every property in the <Topic> object you can set <Topic.IgnoreFields> to filter some out.  Not every
		 * flag will be respected by the query but some that will save a lot of memory or processing time may be.  In debug builds
		 * <Topic> will enforce these settings regardless of whether the query filled them in or not to prevent programming errors.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Topic> GetTopicsInFile (int fileID, CancelDelegate cancelled, Topic.IgnoreFields ignoreFields = Topic.IgnoreFields.None)
			{
			object[] clauseParams = new object[1];
			clauseParams[0] = fileID;

			return GetTopics("FileID=?", "CommentLineNumber ASC", clauseParams, cancelled, ignoreFields);
			}
			
			
		/* Function: GetTopicsByID
		 * 
		 * Retrieves all the <Topics> present in a list of topic IDs.  If there are none it will return an empty list.  Pass a <CancelDelegate> if 
		 * you'd like to be able to interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * If you don't need every property in the <Topic> object you can set <Topic.IgnoreFields> to filter some out.  Not every
		 * flag will be respected by the query but some that will save a lot of memory or processing time may be.  In debug builds
		 * <Topic> will enforce these settings regardless of whether the query filled them in or not to prevent programming errors.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Topic> GetTopicsByID (IEnumerable<int> topicIDs, CancelDelegate cancelled, 
																	 Topic.IgnoreFields ignoreFields = Topic.IgnoreFields.None)
			{
			StringBuilder whereClause = new StringBuilder();
			List<object> clauseParameters = new List<object>();

			bool isFirst = true;
			foreach (int topicID in topicIDs)
				{
				if (isFirst)
					{  isFirst = false;  }
				else
					{  whereClause.Append("OR ");  }

				whereClause.Append("TopicID=? ");
				clauseParameters.Add(topicID);
				}

			if (clauseParameters.Count == 0)
				{  return new List<Topic>();  }

			return GetTopics(whereClause.ToString(), null, clauseParameters.ToArray(), cancelled, ignoreFields);
			}
			
			
		/* Function: GetTopicsByEndingSymbol
		 * 
		 * Retrieves a list of all the topics which use one of the passed <EndingSymbols>.  If there are none it will return an empty
		 * list.  Pass a <CancelDelegate> if you'd like to be able to interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * If you don't need every property in the <Topic> object you can set <Topic.IgnoreFields> to filter some out.  Not every
		 * flag will be respected by the query but some that will save a lot of memory or processing time may be.  In debug builds
		 * <Topic> will enforce these settings regardless of whether the query filled them in or not to prevent programming errors.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Topic> GetTopicsByEndingSymbol (IEnumerable<EndingSymbol> endingSymbols, CancelDelegate cancelled, 
																						 Topic.IgnoreFields ignoreFields = Topic.IgnoreFields.None)
			{
			StringBuilder whereClause = new StringBuilder();
			List<object> clauseParameters = new List<object>();

			bool isFirst = true;
			foreach (EndingSymbol endingSymbol in endingSymbols)
				{
				if (isFirst)
					{  isFirst = false;  }
				else
					{  whereClause.Append("OR ");  }

				whereClause.Append("EndingSymbol=? ");
				clauseParameters.Add(endingSymbol.ToString());
				}

			return GetTopics(whereClause.ToString(), null, clauseParameters.ToArray(), cancelled, ignoreFields);
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
		 *		SymbolDefinitionNumber - Must be set.
		 *		IsEmbedded - Must be set.
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
			RequireNonZero("AddTopic", "SymbolDefinitionNumber", topic.SymbolDefinitionNumber);
			// IsEmbedded
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
			RequireZero("AddTopic", "BodyContextID", topic.BodyContextID);
			
			RequireAtLeast(LockType.ReadWrite);
			BeginTransaction();

			var codeDB = Engine.Instance.CodeDB;

			topic.TopicID = codeDB.UsedTopicIDs.LowestAvailable;
			GetOrCreateContextIDs(topic);
			
			connection.Execute("INSERT INTO Topics (TopicID, Title, Body, Summary, Prototype, Symbol, SymbolDefinitionNumber, " +
													"IsEmbedded, EndingSymbol, TopicTypeID, AccessLevel, Tags, FileID, CommentLineNumber, CodeLineNumber, " +
													"LanguageID, PrototypeContextID, BodyContextID) " +
												"VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
												topic.TopicID, topic.Title, topic.Body, topic.Summary, topic.Prototype, topic.Symbol, topic.SymbolDefinitionNumber,
												(topic.IsEmbedded ? 1 : 0), topic.Symbol.EndingSymbol, topic.TopicTypeID, (int)topic.AccessLevel, topic.TagString, 
												topic.FileID, topic.CommentLineNumber, topic.CodeLineNumber, topic.LanguageID, topic.PrototypeContextID,
												topic.BodyContextID										 
												);
			
			codeDB.UsedTopicIDs.Add(topic.TopicID);

			IDObjects.SparseNumberSet newTopicsForEndingSymbol = codeDB.NewTopicsByEndingSymbol[topic.Symbol.EndingSymbol];
			if (newTopicsForEndingSymbol == null)
				{
				newTopicsForEndingSymbol = new IDObjects.SparseNumberSet();
				codeDB.NewTopicsByEndingSymbol.Add(topic.Symbol.EndingSymbol, newTopicsForEndingSymbol);
				}
			newTopicsForEndingSymbol.Add(topic.TopicID);

			codeDB.ContextIDReferenceChangeCache.AddReference(topic.PrototypeContextID);
			codeDB.ContextIDReferenceChangeCache.AddReference(topic.BodyContextID);

			CommitTransaction();
			

			// Notify change watchers
			
			IList<IChangeWatcher> changeWatchers = codeDB.LockChangeWatchers();
			
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
				codeDB.ReleaseChangeWatchers();
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
			BeginTransaction();

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
													"PrototypeContextID=?, BodyContextID=?, IsEmbedded=? " +
												"WHERE TopicID = ?",
												newTopic.Summary, newTopic.CommentLineNumber, newTopic.CodeLineNumber,
												newTopic.PrototypeContextID, newTopic.BodyContextID, (newTopic.IsEmbedded ? 1 : 0),
												oldTopic.TopicID);

			if ( (changeFlags & (Topic.ChangeFlags.PrototypeContext | Topic.ChangeFlags.BodyContext)) == 0)
				{
				var referenceCache = Engine.Instance.CodeDB.ContextIDReferenceChangeCache;

				referenceCache.RemoveReference(oldTopic.PrototypeContextID);
				referenceCache.RemoveReference(oldTopic.BodyContextID);
				referenceCache.AddReference(newTopic.PrototypeContextID);
				referenceCache.AddReference(newTopic.BodyContextID);
				}

			CommitTransaction();


			// Notify change watchers

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
		 *		SymbolDefinitionNumber - Must be set.
		 *		IsEmbedded - Must be set.
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
			RequireNonZero("DeleteTopic", "SymbolDefinitionNumber", topic.SymbolDefinitionNumber);
			// IsEmbedded
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

			var codeDB = Engine.Instance.CodeDB;


			// Notify the change watchers BEFORE we actually perform the deletion.

			IList<IChangeWatcher> changeWatchers = codeDB.LockChangeWatchers();
			
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
				codeDB.ReleaseChangeWatchers();
				}


			// Find any links that resolve to this topic.

			IDObjects.NumberSet linksAffected = new IDObjects.NumberSet();

			using (SQLite.Query query = connection.Query("SELECT LinkID FROM Links WHERE TargetTopicID=?", topic.TopicID))
				{
				while (query.Step())
					{  linksAffected.Add( query.IntColumn(0) );  }
				}


			// Reset these links back to unresolved and add them to linksToResolve.

			BeginTransaction();

			if (linksAffected.IsEmpty == false)
				{
				StringBuilder queryText = new StringBuilder("UPDATE Links SET TargetTopicID=?, TargetScore=0 WHERE ");
				List<object> queryParams = new List<object>();
				queryParams.Add(UnresolvedTargetTopicID.TargetDeleted);

				AppendWhereClause_ColumnIsInNumberSet("LinkID", linksAffected, queryText, queryParams);

				connection.Execute(queryText.ToString(), queryParams.ToArray());

				codeDB.LinksToResolve.Add(linksAffected);
				}


			// Delete the actual topic.

			connection.Execute("DELETE FROM Topics WHERE TopicID = ?", topic.TopicID);
			
			codeDB.UsedTopicIDs.Remove(topic.TopicID);

			// Check CodeDB.NewTopicsByEndingSymbol just in case.  We don't want to leave any references to a deleted topic.
			IDObjects.SparseNumberSet newTopicsForEndingSymbol = codeDB.NewTopicsByEndingSymbol[topic.Symbol.EndingSymbol];
			if (newTopicsForEndingSymbol != null)
				{  newTopicsForEndingSymbol.Remove(topic.TopicID);  }

			codeDB.ContextIDReferenceChangeCache.RemoveReference(topic.PrototypeContextID);
			codeDB.ContextIDReferenceChangeCache.RemoveReference(topic.BodyContextID);

			CommitTransaction();
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
		 *		- The new topics must be in comment line order.
		 * 
		 * Topic Requirements:
		 * 
		 *		TopicID - Must be zero.  These will be automatically assigned and the <Topics> updated.
		 *		Title - Must be set.
		 *		Body - Can be null.
		 *		Summary - Can be null.
		 *		Prototype - Can be null.
		 *		Symbol - Must be set.
		 *		SymbolDefinitionNumber - Can be zero.  These will be regenerated regardless of whether they were previously set.
		 *		IsEmbedded - Must be set.
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
			#if DEBUG
			int previousCommentLineNumber = 0;

			foreach (Topic newTopic in newTopics)
				{
				if (newTopic.FileID != fileID)
					{  throw new Exception ("Can't update topics in file if the file IDs don't match.");  }
				if (newTopic.CommentLineNumber < previousCommentLineNumber)
					{  throw new Exception ("Topics passed to UpdateTopicsInFile() must be in comment line order.");  }
				// We'll leave the rest of the topic field validation to AddTopic(), DeleteTopic(), and UpdateTopic().

				previousCommentLineNumber = newTopic.CommentLineNumber;
				}


			// Validate that embedded topics match bodies with definition lists that define symbols.

			Topic lastNonEmbeddedTopic = null;
			int lastNonEmbeddedTopicSymbolCount = 0;
			int embeddedTopicsInARow = 0;

			foreach (Topic newTopic in newTopics)
				{
				if (newTopic.IsEmbedded)
					{
					if (lastNonEmbeddedTopic == null)
						{  throw new Exception ("Embedded topics must follow non-embedded topics in a file.");  }

					embeddedTopicsInARow++;

					if (embeddedTopicsInARow > lastNonEmbeddedTopicSymbolCount)
						{  
						throw new Exception ("Topic " + lastNonEmbeddedTopic.Title + " has " + lastNonEmbeddedTopicSymbolCount + " embedded " +
																  "topics but more than that followed it in the list.");  
						}
					}
				else // not embedded
					{
					if (lastNonEmbeddedTopic != null && embeddedTopicsInARow < lastNonEmbeddedTopicSymbolCount)
						{  
						throw new Exception ("Topic " + lastNonEmbeddedTopic.Title + " has " + lastNonEmbeddedTopicSymbolCount + " embedded " +
																  "topics but " + embeddedTopicsInARow + " followed it in the list.");  
						}

					lastNonEmbeddedTopic = newTopic;
					lastNonEmbeddedTopicSymbolCount = 0;
					embeddedTopicsInARow = 0;

					if (newTopic.Body != null)
						{
						int index = newTopic.Body.IndexOf("<ds>");

						while (index != -1)
							{
							lastNonEmbeddedTopicSymbolCount++;
							index = newTopic.Body.IndexOf("<ds>", index + 1);
							}
						}
					}
				}

			if (lastNonEmbeddedTopic != null && embeddedTopicsInARow < lastNonEmbeddedTopicSymbolCount)
				{  
				throw new Exception ("Topic " + lastNonEmbeddedTopic.Title + " has " + lastNonEmbeddedTopicSymbolCount + " embedded " +
															"topics but " + embeddedTopicsInARow + " followed it in the list.");  
				}
			#endif

			// Generate new symbol definition numbers
			for (int i = 0; i < newTopics.Count; i++)
				{
				newTopics[i].SymbolDefinitionNumber = 1;

				for (int prev = i - 1; prev >= 0; prev--)
					{
					if (newTopics[prev].Symbol == newTopics[i].Symbol)
						{
						newTopics[i].SymbolDefinitionNumber = newTopics[prev].SymbolDefinitionNumber + 1;
						break;
						}
					}
				}
			
			RequireAtLeast(LockType.ReadPossibleWrite);

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
						{  
						if (cancelled())
							{  break;  }

						DeleteTopic(oldTopic);  
						}
					}
					
				if (madeChanges == true)
					{
					CommitTransaction();
					}
				}
			catch
				{
				if (madeChanges == true && transactionLevel > 0)
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



		// Group: Link Functions
		// __________________________________________________________________________


		/* Function: GetLinkByID
		 * 
		 * Retrieves a link by its ID.  Assumes the link already exists.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public Link GetLinkByID (int linkID)
			{
			RequireAtLeast(LockType.ReadOnly);
			
			Link link = new Link();
			
			using (SQLite.Query query = connection.Query("SELECT FileID, Type, TextOrSymbol, Links.ContextID, Contexts.ContextString, " +
																									"LanguageID, EndingSymbol, TargetTopicID, TargetScore " +
																								"FROM Links, Contexts " +
																								"WHERE Links.LinkID = ? AND " +
																									"Contexts.ContextID = Links.ContextID ",
																								linkID))
				{
				if (query.Step())
					{
					link.FileID = query.IntColumn(0);
					link.Type = (LinkType)query.IntColumn(1);
					link.TextOrSymbol = query.StringColumn(2);
					link.ContextID = query.IntColumn(3);
					link.Context = ContextString.FromExportedString( query.StringColumn(4) );
					link.LanguageID = query.IntColumn(5);
					link.EndingSymbol = EndingSymbol.FromExportedString( query.StringColumn(6) );
					link.TargetTopicID = query.IntColumn(7);
					link.TargetScore = query.LongColumn(8);

					link.LinkID = linkID;

					contextIDLookupCache.Add(link.ContextID, link.Context);
					}
				}
			
			return link;
			}


		/* Function: GetLinksInFile
		 * 
		 * Retrieves a list of all the links present in the passed file ID.  If there are none it will return an empty list.  Pass a 
		 * <CancelDelegate> if you'd like to be able to interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Link> GetLinksInFile (int fileID, CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadOnly);
			
			List<Link> links = new List<Link>();
			
			using (SQLite.Query query = connection.Query("SELECT LinkID, Type, TextOrSymbol, Links.ContextID, Contexts.ContextString, " +
																									"LanguageID, EndingSymbol, TargetTopicID, TargetScore " +
																								"FROM Links, Contexts " +
																								"WHERE Links.FileID = ? AND " +
																									"Contexts.ContextID = Links.ContextID ",
																								fileID))
				{
				while (query.Step() && !cancelled())
					{
					Link link = new Link();
					
					link.LinkID = query.IntColumn(0);
					link.Type = (LinkType)query.IntColumn(1);
					link.TextOrSymbol = query.StringColumn(2);
					link.ContextID = query.IntColumn(3);
					link.Context = ContextString.FromExportedString( query.StringColumn(4) );
					link.LanguageID = query.IntColumn(5);
					link.EndingSymbol = EndingSymbol.FromExportedString( query.StringColumn(6) );
					link.TargetTopicID = query.IntColumn(7);
					link.TargetScore = query.LongColumn(8);

					link.FileID = fileID;

					links.Add(link);

					contextIDLookupCache.Add(link.ContextID, link.Context);
					}
				}
			
			return links;
			}


		/* Function: GetNaturalDocsLinksInFiles
		 * 
		 * Retrieves a list of all the Natural Docs links present in the passed file IDs.  If there are none it will return an empty list.  
		 * Pass a  <CancelDelegate> if you'd like to be able to interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Link> GetNaturalDocsLinksInFiles (IEnumerable<int> fileIDs, CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadOnly);
			
			List<Link> links = new List<Link>();
			
			StringBuilder queryText = new StringBuilder("SELECT LinkID, Type, TextOrSymbol, Links.ContextID, Contexts.ContextString, " +
																								"FileID, LanguageID, EndingSymbol, TargetTopicID, TargetScore " +
																							"FROM Links, Contexts " +
																							"WHERE Links.Type=? " +
																								"AND Contexts.ContextID = Links.ContextID " +
																								"AND (");
			List<object> queryParams = new List<object>();
			queryParams.Add((int)LinkType.NaturalDocs);

			bool isFirst = true;
			foreach (int fileID in fileIDs)
				{
				if (!isFirst)
					{  queryText.Append(" OR ");  }
				else
					{  isFirst = false;  }

				queryText.Append("Links.FileID=? ");
				queryParams.Add(fileID);
				}

			queryText.Append(')');

			using (SQLite.Query query = connection.Query(queryText.ToString(), queryParams.ToArray()))
				{
				while (query.Step() && !cancelled())
					{
					Link link = new Link();
					
					link.LinkID = query.IntColumn(0);
					link.Type = (LinkType)query.IntColumn(1);
					link.TextOrSymbol = query.StringColumn(2);
					link.ContextID = query.IntColumn(3);
					link.Context = ContextString.FromExportedString( query.StringColumn(4) );
					link.FileID = query.IntColumn(5);
					link.LanguageID = query.IntColumn(6);
					link.EndingSymbol = EndingSymbol.FromExportedString( query.StringColumn(7) );
					link.TargetTopicID = query.IntColumn(8);
					link.TargetScore = query.LongColumn(9);

					links.Add(link);

					contextIDLookupCache.Add(link.ContextID, link.Context);
					}
				}
			
			return links;
			}


		/* Function: GetLinksByEndingSymbol
		 * 
		 * Retrieves a list of all the <Links> present that use the passed <EndingSymbol>.  Note that this also searches 
		 * <CodeDB.AlternateLinkEndingSymbols> so the actual <Link> object may not have the passed <EndingSymbol> as a property.
		 * If there are none it will return an empty list.  Pass a <CancelDelegate> if you'd like to be able to interrupt this process, or 
		 * <Delegates.NeverCancel> if not.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Link> GetLinksByEndingSymbol (EndingSymbol endingSymbol, CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadOnly);
			
			List<Link> links = new List<Link>();
			
			using (SQLite.Query query = connection.Query("SELECT LinkID, Type, TextOrSymbol, Links.ContextID, Contexts.ContextString, " +
																									"LanguageID, FileID, TargetTopicID, TargetScore " +
																								"FROM Links, Contexts " +
																								"WHERE Links.EndingSymbol = ? AND " +
																									"Contexts.ContextID = Links.ContextID ",
																								endingSymbol.ToString()))
				{
				while (query.Step() && !cancelled())
					{
					Link link = new Link();
					
					link.LinkID = query.IntColumn(0);
					link.Type = (LinkType)query.IntColumn(1);
					link.TextOrSymbol = query.StringColumn(2);
					link.ContextID = query.IntColumn(3);
					link.Context = ContextString.FromExportedString( query.StringColumn(4) );
					link.LanguageID = query.IntColumn(5);
					link.FileID = query.IntColumn(6);
					link.TargetTopicID = query.IntColumn(7);
					link.TargetScore = query.LongColumn(8);

					link.EndingSymbol = endingSymbol;

					links.Add(link);

					contextIDLookupCache.Add(link.ContextID, link.Context);
					}
				}

			IDObjects.SparseNumberSet alternateLinkIDs = new IDObjects.SparseNumberSet();
			
			using (SQLite.Query query = connection.Query("SELECT LinkID FROM AlternateLinkEndingSymbols " +
																								"WHERE EndingSymbol = ?", endingSymbol.ToString()))
				{
				while (query.Step() && !cancelled())
					{  alternateLinkIDs.Add( query.IntColumn(0) );  }
				}

			if (!alternateLinkIDs.IsEmpty)
				{
				StringBuilder queryText = new StringBuilder("SELECT LinkID, Type, TextOrSymbol, Links.ContextID, Contexts.ContextString, " +
																									"LanguageID, FileID, EndingSymbol, TargetTopicID, TargetScore " +
																								"FROM Links, Contexts " +
																								"WHERE Contexts.ContextID = Links.ContextID AND (");
				List<object> queryParameters = new List<object>();

				bool isFirst = true;
				foreach (int alternateLinkID in alternateLinkIDs)
					{
					if (!isFirst)
						{  queryText.Append("OR ");  }
					else
						{  isFirst = false;  }

					queryText.Append("LinkID=? ");
					queryParameters.Add(alternateLinkID);
					}

				queryText.Append(')');

				using (SQLite.Query query = connection.Query(queryText.ToString(), queryParameters.ToArray()))
					{
					while (query.Step() && !cancelled())
						{
						Link link = new Link();
					
						link.LinkID = query.IntColumn(0);
						link.Type = (LinkType)query.IntColumn(1);
						link.TextOrSymbol = query.StringColumn(2);
						link.ContextID = query.IntColumn(3);
						link.Context = ContextString.FromExportedString( query.StringColumn(4) );
						link.LanguageID = query.IntColumn(5);
						link.FileID = query.IntColumn(6);
						link.EndingSymbol = EndingSymbol.FromExportedString( query.StringColumn(7) );
						link.TargetTopicID = query.IntColumn(8);
						link.TargetScore = query.LongColumn(9);

						links.Add(link);

						contextIDLookupCache.Add(link.ContextID, link.Context);
						}
					}
				}

			return links;
			}


		/* Function: AddLink
		 * 
		 * Adds a <Link> to the database.  Assumes it doesn't already exist.
		 * 
		 * Requirements:
		 * 
		 *		- Requires a read/write lock.  Read/possible write locks will be upgraded automatically.
		 * 
		 * Link Requirements:
		 * 
		 *		LinkID - Must be zero.  This will be automatically assigned and the <Link> updated.
		 *		Type - Must be set.
		 *		TextOrSymbol - Must be set.
		 *		Context - Can be null, which means global with no "using" statements.
		 *		ContextID - Must be zero.  This will be automatically assigned and the <Link> updated.
		 *		FileID - Must be set.
		 *		LanguageID - Must be set.
		 *		EndingSymbol - Ignored.  For <LinkType.Type> and <LinkType.ClassParent> it will be filled in.
		 *		TargetTopicID - Must be <UnresolvedTargetTopicID.NewLink>.
		 *		TargetScore - Must be zero.
		 */
		public void AddLink (Link link)
			{
			RequireZero("AddLink", "LinkID", link.LinkID);
			// Type - enum so will always be set
			RequireContent("AddLink", "TextOrSymbol", link.TextOrSymbol);
			// Context
			RequireZero("AddLink", "ContextID", link.ContextID);
			RequireNonZero("AddLink", "FileID", link.FileID);
			RequireNonZero("AddLink", "LanguageID", link.LanguageID);

			#if DEBUG
			// Allow EndingSymbol to be null, but if it's not, it must match.
			if ( (link.Type == LinkType.Type || link.Type == LinkType.ClassParent) &&
				  link.EndingSymbol != null && link.EndingSymbol != link.Symbol.EndingSymbol)
				{  throw new Exception("Link.EndingSymbol didn't match Link.Symbol.EndingSymbol in AddLink");  }
			#endif

			RequireValue("AddLink", "TargetTopicID", link.TargetTopicID, UnresolvedTargetTopicID.NewLink);
			RequireZero("AddLink", "TargetScore", link.TargetScore);

			StringSet alternateEndingSymbols = null;

			if (link.Type == LinkType.NaturalDocs)
				{
				string parentheses;

				// Since we're not setting Flags.ExcludeLiteral the list will always have at least one entry.
				// Set Flags.AllowPluralsAndPossessives to get alternate ending symbols (children = children, child)
				// We don't need to set Flags.AllowNamedLinks because we only need the ending symbols, which will be the same either way.
				// For example, <x at y> and <x: y> will still always end up with y as the ending symbol regardless of whether you search for
				// named links or not, so we can avoid the extra processing.
				List<LinkInterpretation> linkInterpretations = 
					Engine.Instance.Comments.NaturalDocsParser.LinkInterpretations(link.Text, 
																												Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowPluralsAndPossessives |
																												Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowNamedLinks |
																												Comments.Parsers.NaturalDocs.LinkInterpretationFlags.FromOriginalText,
																												out parentheses);

				alternateEndingSymbols = new StringSet(false, false);

				foreach (LinkInterpretation linkInterpretation in linkInterpretations)
					{
					SymbolString symbol = SymbolString.FromPlainText_ParenthesesAlreadyRemoved(linkInterpretation.Target);
					alternateEndingSymbols.Add(symbol.EndingSymbol);
					}

				link.EndingSymbol = SymbolString.FromPlainText_ParenthesesAlreadyRemoved(linkInterpretations[0].Target).EndingSymbol;
				alternateEndingSymbols.Remove(link.EndingSymbol);
				}
			else
				{  link.EndingSymbol = link.Symbol.EndingSymbol;  }
			

			RequireAtLeast(LockType.ReadWrite);
			BeginTransaction();

			var codeDB = Engine.Instance.CodeDB;

			link.LinkID = codeDB.UsedLinkIDs.LowestAvailable;
			GetOrCreateContextIDs(link);
			
			connection.Execute("INSERT INTO Links (LinkID, Type, TextOrSymbol, ContextID, FileID, LanguageID, EndingSymbol, " +
													"TargetTopicID, TargetScore) " +
			                           "VALUES (?, ?, ?, ?, ?, ?, ?, ?, 0)",
			                           link.LinkID, (int)link.Type, link.TextOrSymbol, link.ContextID, link.FileID, link.LanguageID, link.EndingSymbol,
												UnresolvedTargetTopicID.NewLink
			                           );
			
			codeDB.UsedLinkIDs.Add(link.LinkID);
			codeDB.LinksToResolve.Add(link.LinkID);
			codeDB.ContextIDReferenceChangeCache.AddReference(link.ContextID);

			if (alternateEndingSymbols != null && alternateEndingSymbols.Count > 0)
				{
				foreach (string alternateEndingSymbol in alternateEndingSymbols)
				   {
				   connection.Execute("INSERT INTO AlternateLinkEndingSymbols (LinkID, EndingSymbol) VALUES (?, ?)",
				                              link.LinkID, alternateEndingSymbol
				                              );
				   }
				}

			CommitTransaction();


			// Notify change watchers
			
			IList<IChangeWatcher> changeWatchers = codeDB.LockChangeWatchers();
			
			try
				{
				if (changeWatchers.Count > 0)
					{
					EventAccessor eventAccessor = new EventAccessor(this);

					foreach (IChangeWatcher changeWatcher in changeWatchers)
						{  changeWatcher.OnAddLink(link, eventAccessor);  }
					}
				}
			finally
				{
				codeDB.ReleaseChangeWatchers();
				}
			}
			
			
		/* Function: DeleteLink
		 * 
		 * Removes a <Link> from the database.
		 * 
		 * Requirements:
		 * 
		 *		- Requires a read/write lock.  Read/possible write locks will be upgraded automatically.
		 * 
		 * Link Requirements:
		 * 
		 *		The link must have been retrieved from the database, and thus have all its fields set.
		 * 
		 *		LinkID - Must be set.
		 *		Type - Must be set.
		 *		TextOrSymbol - Must be set.
		 *		Context - Can be null, which means global with no "using" statements.
		 *		ContextID - Must be set.
		 *		FileID - Must be set.
		 *		LanguageID - Must be set.
		 *		EndingSymbol - Must be set.
		 *		TargetTopicID - Can be any value.
		 *		TargetScore - Can be any value.
		 */
		public void DeleteLink (Link link)
			{
			RequireNonZero("DeleteLink", "LinkID", link.LinkID);
			// Type - enum so will always be set
			RequireContent("DeleteLink", "TextOrSymbol", link.TextOrSymbol);
			// Context
			RequireNonZero("DeleteLink", "ContextID", link.ContextID);
			RequireNonZero("DeleteLink", "FileID", link.FileID);
			RequireNonZero("DeleteLink", "LanguageID", link.LanguageID);
			RequireContent("DeleteLink", "EndingSymbol", link.EndingSymbol);
			// TargetTopicID
			// TargetScore

			RequireAtLeast(LockType.ReadWrite);

			var codeDB = Engine.Instance.CodeDB;


			// Notify the change watchers BEFORE we actually perform the deletion.

			IList<IChangeWatcher> changeWatchers = codeDB.LockChangeWatchers();
			
			try
				{
				if (changeWatchers.Count > 0)
					{
					EventAccessor eventAccessor = new EventAccessor(this);

					foreach (IChangeWatcher changeWatcher in changeWatchers)
						{  changeWatcher.OnDeleteLink(link, eventAccessor);  }
					}
				}
			finally
				{
				codeDB.ReleaseChangeWatchers();
				}


			// Perform the deletion.

			BeginTransaction();

			connection.Execute("DELETE FROM Links WHERE LinkID=?", link.LinkID);
			
			codeDB.UsedLinkIDs.Remove(link.LinkID);
			codeDB.LinksToResolve.Remove(link.LinkID);  // Just in case, so there's no hanging references
			codeDB.ContextIDReferenceChangeCache.RemoveReference(link.ContextID);

			if (link.Type == LinkType.NaturalDocs)
				{
				connection.Execute("DELETE FROM AlternateLinkEndingSymbols WHERE LinkID=?", link.LinkID);
				}

			CommitTransaction();
			}
			
			
		/* Function: UpdateLinksInFile
		 * 
		 * Replaces all the links in the database under the passed file ID with the passed list.  It will query the existing links 
		 * itself, perform a comparison, and call <AddLink()> and <DeleteLink()> as necessary.  Pass a <CancelDelegate> if
		 * you'd like to be able to interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If any changes occur, it will be upgraded automatically.
		 * 
		 * Link Requirements:
		 * 
		 *		LinkID - Must be zero.  This will be automatically assigned and the <Link> updated.
		 *		Type - Must be set.
		 *		TextOrSymbol - Must be set.
		 *		Context - Can be null, which means global with no "using" statements.
		 *		ContextID - Must be zero.  This will be automatically assigned and the <Link> updated.
		 *		FileID - Must be set.
		 *		LanguageID - Must be set.
		 *		EndingSymbol - Ignored.  For <LinkType.Type> and <LinkType.ClassParent> it will be filled in.
		 *		TargetTopicID - Must be <UnresolvedTargetTopicID.NewLink>.
		 *		TargetScore - Must be zero.
		 */
		public void UpdateLinksInFile (int fileID, IEnumerable<Link> newLinks, CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);

			foreach (Link newLink in newLinks)
				{
				if (newLink.FileID != fileID)
					{  throw new Exception ("Can't update links in file if the file IDs don't match.");  }
				// We'll leave the rest of the topic field validation to AddLink() and DeleteLink().
				}
			
			List<Link> oldLinks = GetLinksInFile(fileID, cancelled);
			bool madeChanges = false;
			
			try
				{
				
				foreach (Link newLink in newLinks)
					{
					if (cancelled())
						{  break;  }
						
					bool foundMatch = false;
					for (int i = 0; foundMatch == false && i < oldLinks.Count; i++)
						{
						if (newLink.SameIDPropertiesAs(oldLinks[i]))
							{
							foundMatch = true;
							newLink.CopyNonIDPropertiesFrom(oldLinks[i]);
							oldLinks.RemoveAt(i);
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
							
						AddLink(newLink);
						}
					}
					
				// All matches would have been removed, so anything left in oldLinks was deleted.
				if (oldLinks.Count > 0 && !cancelled())
					{
					if (madeChanges == false)
						{
						RequireAtLeast(LockType.ReadWrite);
						BeginTransaction();
						madeChanges = true;
						}
						
					foreach (Link oldLink in oldLinks)
						{  
						if (cancelled())
							{  break;  }

						DeleteLink(oldLink);  
						}
					}
					
				if (madeChanges == true)
					{
					CommitTransaction();
					}
				}
			catch
				{
				if (madeChanges == true && transactionLevel > 0)
					{  RollbackTransactionForException();  }
					
				throw;
				}
			}


		/* Function: DeleteLinksInFile
		 * 
		 * Deletes all the links in the database under the passed file ID.  Pass a <CancelDelegate> if you'd like to be able to
		 * interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If any deletions occur, it will be upgraded automatically.
		 */
		public void DeleteLinksInFile (int fileID, CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);
			
			List<Link> links = GetLinksInFile(fileID, cancelled);
			
			if (links.Count > 0 && !cancelled())
				{
				RequireAtLeast(LockType.ReadWrite);
				BeginTransaction();
					
				foreach (Link link in links)
					{  
					DeleteLink(link);
					
					if (cancelled())
						{  break;  }
					}

				CommitTransaction();
				}
			}


		/* Function: GetAlternateLinkEndingSymbols
		 * 
		 * Retrieves the list of alternate <EndingSymbols> for a link ID.  It will not include the <EndingSymbol> stored in the <Link>
		 * itself.  If there are no alternate symbols it will return null.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<EndingSymbol> GetAlternateLinkEndingSymbols (int linkID)
			{
			RequireAtLeast(LockType.ReadOnly);

			List<EndingSymbol> endingSymbols = null;

			using (SQLite.Query query = connection.Query("SELECT EndingSymbol " +
																								"FROM AlternateLinkEndingSymbols " +
																								"WHERE LinkID = ?", 
																								linkID))
				{
				while (query.Step())
					{
					if (endingSymbols == null)
						{  endingSymbols = new List<EndingSymbol>();  }

					endingSymbols.Add( EndingSymbol.FromExportedString(query.StringColumn(0)) );
					}
				}

			return endingSymbols;
			}


		/* Function: UpdateLinkTarget
		 * 
		 * Updates the score and interpretation of a link in the database.  Assumes both IDs already exist.
		 * 
		 * Requirements:
		 * 
		 *		- Requires a read/write lock.  Read/possible write locks will be upgraded automatically.
		 */
		public void UpdateLinkTarget (Link link, int oldTargetTopicID)
			{
			RequireAtLeast(LockType.ReadWrite);

			var codeDB = Engine.Instance.CodeDB;

			connection.Execute("UPDATE Links SET TargetTopicID=?, TargetScore=? " +
												  "WHERE LinkID = ?", link.TargetTopicID, link.TargetScore, link.LinkID);


			// Notify change watchers
			
			IList<IChangeWatcher> changeWatchers = codeDB.LockChangeWatchers();
			
			try
				{
				if (changeWatchers.Count > 0)
					{
					EventAccessor eventAccessor = new EventAccessor(this);

					foreach (IChangeWatcher changeWatcher in changeWatchers)
						{  changeWatcher.OnChangeLinkTarget(link, oldTargetTopicID, eventAccessor);  }
					}
				}
			finally
				{
				codeDB.ReleaseChangeWatchers();
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
					{  CacheOrCreateContextIDs(topic.PrototypeContext, topic.BodyContext);  }
				else
					{  CacheOrCreateContextIDs(topic.PrototypeContext);  }
				}
			else if (topic.BodyContextID == 0)
				{  CacheOrCreateContextIDs(topic.BodyContext);  }
			else
				{  return;  }


			// Fill in the Topic.

			if (topic.PrototypeContextID == 0)
				{  topic.PrototypeContextID = contextIDLookupCache[topic.PrototypeContext];  }

			if (topic.BodyContextID == 0)
				{  topic.BodyContextID = contextIDLookupCache[topic.BodyContext];  }
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
					{  topic.PrototypeContextID = contextIDLookupCache[topic.PrototypeContext];  }

				if (topic.BodyContextID == 0)
					{  topic.BodyContextID = contextIDLookupCache[topic.BodyContext];  }
				}
			}


		/* Function: GetOrCreateContextIDs
		 * 
		 * Retrieves the context ID for <Link.Context> if it's not already set.  If an existing ID cannot be found, it will be created.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If a new context is created, it will be upgraded automatically.
		 *		
		 * Link Requirements:
		 * 
		 *		Context - Can be null, which means global with no "using" statements.
		 *		ContextID - If zero Context will be looked up and an ID assigned.  If non-zero no lookup will occur.
		 */
		public void GetOrCreateContextIDs (Link link)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);

			if (link.ContextID != 0)
				{  return;  }

			CacheOrCreateContextIDs(link.Context);

			link.ContextID = contextIDLookupCache[link.Context];
			}


		/* Function: CacheOrCreateContextIDs
		 * 
		 * Retrieves the IDs for each <ContextString> and stores them in <contextIDLookupCache>.  If they don't exist in the 
		 * database they will be created.
		 * 
		 * If the collection you pass in doesn't support null strings you can set plusNullContext to true and it will be included.
		 * If it does support them you are fine just including it in the collection and leaving plusNullContext false, even if there 
		 * might be one in the collection.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If new contexts are created, it will be upgraded automatically.
		 */
		protected void CacheOrCreateContextIDs (IEnumerable<ContextString> contextStrings, bool plusNullContext = false)
			{
			// Remember that contextIDLookupCache is local to the accessor and doesn't need any locking.
			// ContextIDReferenceChangeCache is part of CodeDB.Manager and requires a database lock.

			RequireAtLeast(LockType.ReadPossibleWrite);

			
			// Create a list of all contextStrings not already in the cache.  Since it's possible that they'll all be in the cache we
			// create the list object on demand.

			List<ContextString> uncachedContextStrings = null;

			foreach (var contextString in contextStrings)
				{
				if (contextIDLookupCache.Contains(contextString) == false)
					{
					if (contextString == null)
						{  plusNullContext = true;  }
					else
						{  
						if (uncachedContextStrings == null)
							{  uncachedContextStrings = new List<ContextString>();  }

						uncachedContextStrings.Add(contextString);  
						}
					}
				}

			if (plusNullContext && contextIDLookupCache.Contains(null))
				{  plusNullContext = false;  }


			// Can we quit early?

			if (uncachedContextStrings == null && plusNullContext == false)
				{  return;  }


			// Create a query to lookup the uncached contexts in the database.  The IDs may already exist there.

			System.Text.StringBuilder queryText = new System.Text.StringBuilder("SELECT ContextID, ContextString FROM Contexts WHERE");
			string[] queryParams = null;
			bool firstWhere = true;

			if (uncachedContextStrings != null)
				{
				queryParams = new string[uncachedContextStrings.Count];

				for (int i = 0; i < uncachedContextStrings.Count; i++)
					{
					if (!firstWhere)
						{  queryText.Append(" OR");  }

					queryText.Append(" ContextString=?");
					firstWhere = false;

					queryParams[i] = uncachedContextStrings[i].ToString();
					}
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
			      {  contextIDLookupCache.Add( query.IntColumn(0), ContextString.FromExportedString(query.StringColumn(1)) );  }
			   }


			// Pare down our list of uncached context strings.

			if (uncachedContextStrings != null)
				{
				int i = 0;
				while (i < uncachedContextStrings.Count)
					{
					if (contextIDLookupCache.Contains(uncachedContextStrings[i]))
						{  uncachedContextStrings.RemoveAt(i);  }
					else
						{  i++;  }
					}

				if (uncachedContextStrings.Count == 0)
					{  uncachedContextStrings = null;  }
				}

			if (plusNullContext && contextIDLookupCache.Contains(null))
				{  plusNullContext = false;  }


			// Can we quit now?

			if (uncachedContextStrings == null && plusNullContext == false)
				{  return;  }


			// Create anything we still need.

			// DEPENDENCY: FlushContextIDReferenceChangeCache() assumes *every* newly created context ID will have an 
			// entry in CodeDB.ContextIDReferenceChangeCache with database references set to zero.

			RequireAtLeast(LockType.ReadWrite);
			BeginTransaction();

			using (SQLite.Query query = connection.Query("INSERT INTO Contexts (ContextID, ContextString, ReferenceCount) " +
																								 "VALUES (?, ?, 0)") )
				{
				var codeDB = Engine.Instance.CodeDB;

				if (plusNullContext)
					{
					int id = codeDB.UsedContextIDs.LowestAvailable;

					query.BindValues(id, null);
					query.Step();
					query.Reset(true);

					codeDB.UsedContextIDs.Add(id);
					contextIDLookupCache.Add(id, new ContextString());

					codeDB.ContextIDReferenceChangeCache.SetDatabaseReferenceCount(id, 0);
					}

				if (uncachedContextStrings != null)
					{
					foreach (string contextString in uncachedContextStrings)
						{
						int id = codeDB.UsedContextIDs.LowestAvailable;

						query.BindValues(id, contextString);
						query.Step();
						query.Reset(true);

						codeDB.UsedContextIDs.Add(id);
						contextIDLookupCache.Add(id, ContextString.FromExportedString(contextString));

						codeDB.ContextIDReferenceChangeCache.SetDatabaseReferenceCount(id, 0);
						}
					}
				}

			CommitTransaction();
			}


		/* Function: CacheOrCreateContextIDs
		 * 
		 * Retrieves the IDs for each <ContextString> and stores them in <contextIDLookupCache>.  If they don't exist in
		 * the database they will be created.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If new contexts are created, it will be upgraded automatically.
		 */
		protected void CacheOrCreateContextIDs (params ContextString[] contextStrings)
			{
			CacheOrCreateContextIDs(contextStrings, false);
			}


		/* Function: FlushContextIDReferenceChangeCache
		 * 
		 * Applies anything waiting in <CodeDB.Manager.ContextIDReferenceChangeCache> to the database.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If the database needs to be updated it will be upgraded automatically.
		 */
		public void FlushContextIDReferenceChangeCache (CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);

			ReferenceChangeCache cache = Instance.CodeDB.ContextIDReferenceChangeCache;


			// Figure out which IDs we need to get database counts for.

			// DEPENDENCY:
			//
			// - This assumes CacheOrCreateContextIDs() will create an entry in CodeDB.ContextIDReferenceChangeCache for *every*
			//    newly created context ID with the database reference count set to zero.
			//
			// - This also assumes that this function deletes every context ID record with zero references from the database, and
			//   the zero reference entry in ContextIDReferenceChangeCache will exist until then.
			//
			// - Therefore, we can assume that there will be no zero reference records in the database that aren't also represented
			//   in ContextIDReferenceChangeCache with a known database reference count of zero.
			//
			// - Therefore, any cache entry with an unknown number of database references has a non-zero number in the database.
			//
			// - Therefore, we can ignore cache entries where the reference count change is zero (equal numbers of references 
			//   were added and removed) and the database reference count is unknown.  This will not result it any zero reference
			//   records getting stranded in the database.

			IDObjects.NumberSet idsToLookup = new IDObjects.NumberSet();
			bool hasChanges = false;

			foreach (var cacheEntry in cache)
				{
				if (cacheEntry.DatabaseReferenceCountKnown == false && cacheEntry.ReferenceChange != 0)
					{  idsToLookup.Add(cacheEntry.ID);  }

				// Also see if there are changes in the cache at all, since we can avoid a read/write lock if not.  A ReferenceChange
				// of zero still counts if DatabaseReferenceCount is also zero because that's an empty record we have to remove.
				if (cacheEntry.ReferenceChange != 0 || cacheEntry.DatabaseReferenceCount == 0)
					{  hasChanges = true;  }
				}

			if (hasChanges == false)
				{  return;  }


			// We have to do this before filling in the cache because ContextIDReferenceChangeCache is governed by the same lock 
			// as the database, so we need read/write to change it even though we're not changing actual records yet.

			RequireAtLeast(LockType.ReadWrite);


			// Fill in the cache.

			if (idsToLookup.IsEmpty == false)
				{
				StringBuilder queryText = new StringBuilder("SELECT ContextID, ReferenceCount FROM Contexts WHERE ");
				List<object> queryParams = new List<object>();

				AppendWhereClause_ColumnIsInNumberSet("ContextID", idsToLookup, queryText, queryParams);

				if (cancelled())
					{  return;  }
			
				using (SQLite.Query query = connection.Query(queryText.ToString(), queryParams.ToArray()))
					{
					while (query.Step())
						{
						cache.SetDatabaseReferenceCount(query.IntColumn(0), query.IntColumn(1));

						if (cancelled())
							{  return;  }
						}
					}

				} // if idsToLookup isn't empty


			// Update the database records that need it, but just collect the IDs of the database records to be deleted for a 
			// second pass.
			
			BeginTransaction();

			// Reuse the NumberSet object.
			IDObjects.NumberSet idsToDelete = idsToLookup;
			idsToLookup = null;
			idsToDelete.Clear();

			using (SQLite.Query updateQuery = connection.Query("UPDATE Contexts SET ReferenceCount=? WHERE ContextID=?"))
				{
				foreach (var cacheEntry in cache)
					{
					// Sanity checks
					#if DEBUG
					if (cacheEntry.DatabaseReferenceCountKnown == false && cacheEntry.ReferenceChange != 0)
						{  
						throw new Exception("ContextIDReferenceChangeCache entry " + cacheEntry.ID + 
															  " was not properly filled in before flushing.");  
						}
					if (cacheEntry.DatabaseReferenceCountKnown == true && 
						 cacheEntry.DatabaseReferenceCount + cacheEntry.ReferenceChange < 0)
						{  
						throw new Exception("ContextIDReferenceChangeCache entry " + cacheEntry.ID + 
															  " led to a negative reference count.");  
						}
					#endif

					if (cacheEntry.DatabaseReferenceCountKnown)
						{
						if (cacheEntry.DatabaseReferenceCount + cacheEntry.ReferenceChange == 0)
							{
							idsToDelete.Add(cacheEntry.ID);
							}
						else if (cacheEntry.ReferenceChange != 0)
							{
							updateQuery.BindValues(cacheEntry.DatabaseReferenceCount + cacheEntry.ReferenceChange, cacheEntry.ID);
							updateQuery.Step();
							updateQuery.Reset(true);

							// Update the cache entry so it stays valid in case the operation is cancelled before the cache is emptied.  We 
							// can't remove the entry from the set while we're iterating through it.
							cacheEntry.DatabaseReferenceCount += cacheEntry.ReferenceChange;
							cacheEntry.ReferenceChange = 0;
							}

						if (cancelled())
							{  
							CommitTransaction();
							return;
							}
						}
					}
				}


			// Delete the database records that need it.  Why a second pass?  It avoids these problems of doing it in one:
			//
			//    - Operation is cancelled, you have entries in the cache that aren't in the database anymore because you couldn't
			//      remove them while iterating through it.
			//    - Operation is cancelled, you took the ID out of CodeDB.Manager.UsedContextIDs but there are still entries in the
			//      cache that reference it because you couldn't remove them while iterating through it.
			//    - Operation is cancelled, you didn't take the ID out of CodeDB.Manager.UsedContextIDs to avoid above but now
			//      you have unused IDs marked as used.
			//
			// All of the above could be coded around, it's just more complicated.  Also, it's more efficient to pack it all into one
			// query rather than crossing the boundaries between C# and SQLite for every record individually.
			
			if (idsToDelete.IsEmpty == false)
				{
				StringBuilder queryText = new StringBuilder("DELETE FROM Contexts WHERE ");
				List<object> queryParams = new List<object>();

				AppendWhereClause_ColumnIsInNumberSet("ContextID", idsToDelete, queryText, queryParams);

				connection.Execute(queryText.ToString(), queryParams.ToArray());
				Engine.Instance.CodeDB.UsedContextIDs.Remove(idsToDelete);

				} // if idsToDelete isn't empty


			CommitTransaction();
			cache.Clear();
			}



		// Group: Transaction Functions
		// __________________________________________________________________________
		
		
		/* Function: BeginTransaction
		 * 
		 * Starts a new transaction.  Transactions can be nested within one another.
		 * 
		 * All transactions MUST BE COMMITTED except in error conditions like exceptions.  There are other changes to 
		 * Natural Docs' state that occur with each change independent of transactions so they would not be rolled back 
		 * with the database.  Also, SQLite doesn't support nested transactions, that's an abstraction implemented by this
		 * class.
		 * 
		 * Requirements:
		 * 
		 *		- You must have a read/write lock.  Read/possible write locks will be upgraded automatically.
		 */
		protected void BeginTransaction ()
			{
			if (transactionLevel == -1)
				{  throw new Exception("Cannot start a new transaction after one was broken by an exception.");  }

			else if (transactionLevel == 0)
				{
				RequireAtLeast(LockType.ReadWrite);
				connection.Execute("BEGIN IMMEDIATE TRANSACTION");
				}

			transactionLevel++;
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
			if (transactionLevel == -1)
				{  throw new Exception("Cannot commit a transaction after one was broken by an exception.");  }

			else if (transactionLevel == 0)
				{  throw new Exception("Tried to commit a non-existent transaction.");  }

			else if (transactionLevel == 1)
				{
				RequireAtLeast(LockType.ReadWrite);
				connection.Execute("COMMIT TRANSACTION");
				}

			transactionLevel--;
			}

		/* Function: RollbackTransactionForException
		 * 
		 * Rolls back an existing transaction from the database because an exception occurred.  This is the ONLY reason
		 * you can roll back a transaction because there are other state changes within Natural Docs that occur and would
		 * not be reverted with the database.  This function only exists so that you can get out of the transaction if an
		 * exception occurs, which prevents an additional exception from occurring if you try to dispose of the Accessor
		 * while a transaction is still in effect.  You cannot start a new transaction after this occurs, you should be exiting
		 * the program.
		 * 
		 * Requirements:
		 * 
		 *		- You must have a read/write lock.  Read/possible write locks will be upgraded automatically.
		 */
		protected void RollbackTransactionForException ()
			{
			if (transactionLevel >= 1)
				{  connection.Execute("ROLLBACK TRANSACTION");  }

			transactionLevel = -1;
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: AppendWhereClause_ColumnIsInNumberSet
		 * 
		 * Generates a SQL WHERE clause for testing that a column's value is contained in a number set.  It will be appended to 
		 * the passed StringBuilder and list of parameters.
		 * 
		 * For example, calling this with the NumberSet {2,5-8} would add 
		 * 
		 *		> (Column=? OR (Column >= ? AND Column <= ?))
		 * 
		 * to the end of the query and 2, 5, and 8 to the list of parameters.
		 * 
		 * 
		 * Parameters:
		 * 
		 *		columnName - The name of the column to test against the NumberSet.
		 *		numberSet - The <IDObjects.NumberSet> to use in the query.
		 *		queryText - The query being built.  The new text is appended to it, so it must already contain a query up to the
		 *							  WHERE clause, including already having the WHERE keyword.
		 *		queryParams - The parameter list for the query being built.  The new numbers are appended to it, so it must already
		 *									contain the parameters for any question marks appearing earlier in the query.
		 */
		protected void AppendWhereClause_ColumnIsInNumberSet (string columnName, IDObjects.NumberSet numberSet, 
																												  StringBuilder queryText, List<object> queryParams)
			{
			#if DEBUG
			if (queryText.ToString().IndexOf(" WHERE", StringComparison.CurrentCultureIgnoreCase) == -1)
				{  throw new Exception("The query text must already have a WHERE keyword before callling AppendWhereClause_ functions.");  }
			#endif

			// Surround the entire clause with parentheses and spaces to be safe.
			queryText.Append(" (");

			bool firstParam = true;

			foreach (IDObjects.NumberRange range in numberSet.Ranges)
				{
				if (firstParam)
					{  firstParam = false;  }
				else
					{  queryText.Append(" OR ");  }

				if (range.Low == range.High)
					{
					queryText.Append(columnName + "=?");
					queryParams.Add(range.Low);
					}
				else
					{
					queryText.Append("(" + columnName + ">=? AND " + columnName + "<=?)");
					queryParams.Add(range.Low);
					queryParams.Add(range.High);
					}
				}

			queryText.Append(") ");
			}

		}
	}