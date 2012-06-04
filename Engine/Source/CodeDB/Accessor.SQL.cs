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
			RequireAtLeast(LockType.ReadOnly);
			
			List<Topic> topics = new List<Topic>();
			
			using (SQLite.Query query = connection.Query("SELECT TopicID, Title, Body, Summary, Prototype, Symbol, SymbolDefinitionNumber, " +
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
					topic.SymbolDefinitionNumber = query.IntColumn(6);

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

					// Set this last so that we don't cause exceptions by filling in fields that should have been ignored.  From
					// this point forward it will be enforced, including preventing access to ones we filled in unnecessarily.
					topic.IgnoredFields = ignoreFields;
					}
				}
			
			return topics;
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
			RequireAtLeast(LockType.ReadOnly);
			
			List<Topic> topics = new List<Topic>();
			
			StringBuilder queryText = new StringBuilder("SELECT TopicID, Title, Body, Summary, Prototype, Symbol, SymbolDefinitionNumber, " +
																								"TopicTypeID, AccessLevel, Tags, CommentLineNumber, CodeLineNumber, " +
																								"LanguageID, PContexts.ContextString, PrototypeContextID, " +
																								"BContexts.ContextString, BodyContextID, FileID " +
																							"FROM Topics, Contexts AS PContexts, Contexts AS BContexts " +
																							"WHERE " +
																								"PContexts.ContextID = PrototypeContextID AND " +
																								"BContexts.ContextID = BodyContextID AND (");
			List<object> queryParameters = new List<object>();

			bool isFirst = true;
			foreach (int topicID in topicIDs)
				{
				if (!isFirst)
					{  queryText.Append("OR ");  }
				else
					{  isFirst = false;  }

				queryText.Append("TopicID=? ");
				queryParameters.Add(topicID);
				}

			queryText.Append(')');

			if (queryParameters.Count == 0)
				{  return topics;  }

			using (SQLite.Query query = connection.Query(queryText.ToString(), queryParameters.ToArray()))
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
					topic.SymbolDefinitionNumber = query.IntColumn(6);

					topic.TopicTypeID = query.IntColumn(7);
					topic.AccessLevel = (Languages.AccessLevel)query.IntColumn(8);
					topic.TagString = query.StringColumn(9);

					topic.CommentLineNumber = query.IntColumn(10);
					topic.CodeLineNumber = query.IntColumn(11);

					topic.LanguageID = query.IntColumn(12);
					topic.PrototypeContext = ContextString.FromExportedString( query.StringColumn(13) );
					topic.PrototypeContextID = query.IntColumn(14);
					topic.BodyContext = ContextString.FromExportedString( query.StringColumn(15) );
					topic.BodyContextID = query.IntColumn(16);
					topic.FileID = query.IntColumn(17);

					topics.Add(topic);

					contextIDCache.Add(topic.PrototypeContextID, topic.PrototypeContext);
					contextIDCache.Add(topic.BodyContextID, topic.BodyContext);

					// Set this last so that we don't cause exceptions by filling in fields that should have been ignored.  From
					// this point forward it will be enforced, including preventing access to ones we filled in unnecessarily.
					topic.IgnoredFields = ignoreFields;
					}
				}
			
			return topics;
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
			RequireAtLeast(LockType.ReadOnly);
			
			List<Topic> topics = new List<Topic>();
			
			StringBuilder queryText = new StringBuilder("SELECT TopicID, Title, Body, Summary, Prototype, Symbol, SymbolDefinitionNumber, " +
																								"TopicTypeID, AccessLevel, Tags, CommentLineNumber, CodeLineNumber, " +
																								"LanguageID, PContexts.ContextString, PrototypeContextID, " +
																								"BContexts.ContextString, BodyContextID, FileID " +
																							"FROM Topics, Contexts AS PContexts, Contexts AS BContexts " +
																							"WHERE " +
																								"PContexts.ContextID = PrototypeContextID AND " +
																								"BContexts.ContextID = BodyContextID AND (");
			List<object> queryParameters = new List<object>();

			bool isFirst = true;
			foreach (EndingSymbol endingSymbol in endingSymbols)
				{
				if (!isFirst)
					{  queryText.Append("OR ");  }
				else
					{  isFirst = false;  }

				queryText.Append("EndingSymbol=? ");
				queryParameters.Add(endingSymbol.ToString());
				}

			queryText.Append(')');

			using (SQLite.Query query = connection.Query(queryText.ToString(), queryParameters.ToArray()))
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
					topic.SymbolDefinitionNumber = query.IntColumn(6);

					topic.TopicTypeID = query.IntColumn(7);
					topic.AccessLevel = (Languages.AccessLevel)query.IntColumn(8);
					topic.TagString = query.StringColumn(9);

					topic.CommentLineNumber = query.IntColumn(10);
					topic.CodeLineNumber = query.IntColumn(11);

					topic.LanguageID = query.IntColumn(12);
					topic.PrototypeContext = ContextString.FromExportedString( query.StringColumn(13) );
					topic.PrototypeContextID = query.IntColumn(14);
					topic.BodyContext = ContextString.FromExportedString( query.StringColumn(15) );
					topic.BodyContextID = query.IntColumn(16);
					topic.FileID = query.IntColumn(17);

					topics.Add(topic);

					contextIDCache.Add(topic.PrototypeContextID, topic.PrototypeContext);
					contextIDCache.Add(topic.BodyContextID, topic.BodyContext);

					// Set this last so that we don't cause exceptions by filling in fields that should have been ignored.  From
					// this point forward it will be enforced, including preventing access to ones we filled in unnecessarily.
					topic.IgnoredFields = ignoreFields;
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
		 *		SymbolDefinitionNumber - Must be set.
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
													"EndingSymbol, TopicTypeID, AccessLevel, Tags, FileID, CommentLineNumber, CodeLineNumber, " +
													"LanguageID, PrototypeContextID, BodyContextID) " +
												"VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
												topic.TopicID, topic.Title, topic.Body, topic.Summary, topic.Prototype, topic.Symbol, topic.SymbolDefinitionNumber,
												topic.Symbol.EndingSymbol, topic.TopicTypeID, (int)topic.AccessLevel, topic.TagString, topic.FileID, 
												topic.CommentLineNumber, topic.CodeLineNumber, topic.LanguageID, topic.PrototypeContextID,
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

			codeDB.ContextReferenceCache.AddReference(topic.PrototypeContextID, topic.PrototypeContext);
			codeDB.ContextReferenceCache.AddReference(topic.BodyContextID, topic.BodyContext);

			CommitTransaction();
			

			// Notify change watchers
			
			IList<IChangeWatcher> changeWatchers = codeDB.LockChangeWatchers();
			
			try
				{
				if (changeWatchers.Count > 0)
					{
					foreach (IChangeWatcher changeWatcher in changeWatchers)
						{  changeWatcher.OnAddTopic(topic);  }
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
													"PrototypeContextID=?, BodyContextID=? " +
												"WHERE TopicID = ?",
												newTopic.Summary, newTopic.CommentLineNumber, newTopic.CodeLineNumber,
												newTopic.PrototypeContextID, newTopic.BodyContextID, oldTopic.TopicID);

			if ( (changeFlags & (Topic.ChangeFlags.PrototypeContext | Topic.ChangeFlags.BodyContext)) == 0)
				{
				var referenceCache = Engine.Instance.CodeDB.ContextReferenceCache;

				referenceCache.RemoveReference(oldTopic.PrototypeContextID, oldTopic.PrototypeContext);
				referenceCache.RemoveReference(oldTopic.BodyContextID, oldTopic.BodyContext);
				referenceCache.AddReference(newTopic.PrototypeContextID, newTopic.PrototypeContext);
				referenceCache.AddReference(newTopic.BodyContextID, newTopic.BodyContext);
				}

			CommitTransaction();


			// Notify change watchers

			IList<IChangeWatcher> changeWatchers = Engine.Instance.CodeDB.LockChangeWatchers();
			
			try
				{
				if (changeWatchers.Count > 0)
					{
					foreach (IChangeWatcher changeWatcher in changeWatchers)
						{  changeWatcher.OnUpdateTopic(oldTopic, newTopic, changeFlags);  }
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
					foreach (IChangeWatcher changeWatcher in changeWatchers)
						{  changeWatcher.OnDeleteTopic(topic);  }
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
				StringBuilder queryText = new StringBuilder("UPDATE Links SET TargetTopicID=0, TargetScore=0 WHERE ");
				List<object> queryParams = new List<object>();

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

			codeDB.ContextReferenceCache.RemoveReference(topic.PrototypeContextID, topic.PrototypeContext);
			codeDB.ContextReferenceCache.RemoveReference(topic.BodyContextID, topic.BodyContext);

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

					contextIDCache.Add(link.ContextID, link.Context);
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

					contextIDCache.Add(link.ContextID, link.Context);
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

					contextIDCache.Add(link.ContextID, link.Context);
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

					contextIDCache.Add(link.ContextID, link.Context);
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

						contextIDCache.Add(link.ContextID, link.Context);
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
		 *		TargetTopicID - Must be zero.
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

			RequireZero("AddLink", "TargetTopicID", link.TargetTopicID);
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
			                           "VALUES (?, ?, ?, ?, ?, ?, ?, 0, 0)",
			                           link.LinkID, (int)link.Type, link.TextOrSymbol, link.ContextID, link.FileID, link.LanguageID, link.EndingSymbol
			                           );
			
			codeDB.UsedLinkIDs.Add(link.LinkID);
			codeDB.LinksToResolve.Add(link.LinkID);
			codeDB.ContextReferenceCache.AddReference(link.ContextID, link.Context);

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
		 *		TargetTopicID - Can be zero, which means the link is unresolved.
		 *		TargetScore - Can be zero, which means the link is unresolved.
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
			BeginTransaction();

			var codeDB = Engine.Instance.CodeDB;

			connection.Execute("DELETE FROM Links WHERE LinkID=?", link.LinkID);
			
			codeDB.UsedLinkIDs.Remove(link.LinkID);
			codeDB.LinksToResolve.Remove(link.LinkID);  // Just in case, so there's no hanging references
			codeDB.ContextReferenceCache.RemoveReference(link.ContextID, link.Context);

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
		 *		TargetTopicID - Must be zero.
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
		public void UpdateLinkTarget (Link link)
			{
			RequireAtLeast(LockType.ReadWrite);

			connection.Execute("UPDATE Links SET TargetTopicID=?, TargetScore=? " +
												  "WHERE LinkID = ?", link.TargetTopicID, link.TargetScore, link.LinkID);
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

			link.ContextID = contextIDCache[link.Context].ID;
			}


		/* Function: CacheOrCreateContextIDs
		 * 
		 * Retrieves the IDs for each <ContextString> and stores them in <contextIDCache>.  If they don't exist in the 
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
			// Remember that contextIDCache is local to the accessor and doesn't need any locking.
			// ContextReferenceCache is part of CodeDB.Manager and requires a database lock.

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


			// Create a query to lookup the uncached contexts in the database.  The IDs may already exist there.

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

			// DEPENDENCY: FlushContextReferenceCache() assumes *every* newly created context ID will have an entry in 
			// CodeDB.ContextReferenceCache with database references set to zero.

			RequireAtLeast(LockType.ReadWrite);
			BeginTransaction();

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

					Engine.Instance.CodeDB.ContextReferenceCache.SetDatabaseReferences(id, new ContextString(), 0);
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

						Engine.Instance.CodeDB.ContextReferenceCache.SetDatabaseReferences(id, ContextString.FromExportedString(contextString), 0);
						}
					}
				}

			CommitTransaction();
			}


		/* Function: CacheOrCreateContextIDs
		 * 
		 * Retrieves the IDs for each <ContextString> and stores them in <contextIDCache>.  If they don't exist in the 
		 * database they will be created.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If new contexts are created, it will be upgraded automatically.
		 */
		protected void CacheOrCreateContextIDs (params ContextString[] contextStrings)
			{
			CacheOrCreateContextIDs(contextStrings, false);
			}


		/* Function: FlushContextReferenceCache
		 * 
		 * Applies anything waiting in <CodeDB.Manager.ContextReferenceCache> to the database.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If the database needs to be updated it will be upgraded automatically.
		 */
		public void FlushContextReferenceCache (CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);

			ContextReferenceCache cache = Instance.CodeDB.ContextReferenceCache;


			// Figure out which IDs we need to get database counts for.

			// DEPENDENCY:
			//
			// - This assumes CacheOrCreateContextIDs() will create an entry in CodeDB.ContextReferenceCache for *every* newly
			//   created context ID with the database reference count set to zero.
			//
			// - This also assumes that this function deletes every context ID record with zero references from the database, and
			//   the zero reference entry in ContextReferenceCache will exist until then.
			//
			// - Therefore, we can assume that there will be no zero reference records in the database that aren't also represented
			//   in ContextReferenceCache with a known database reference count of zero.
			//
			// - Therefore, any cache entry with an unknown number of database references has a non-zero number in the database.
			//
			// - Therefore, we can ignore cache entries where the reference count change is zero (equal numbers of references 
			//   were added and removed) and the database reference count is unknown.  This will not result it any zero reference
			//   records getting stranded in the database.

			IDObjects.NumberSet idsToLookup = new IDObjects.NumberSet();

			foreach (ContextReferenceCacheEntry cacheEntry in cache)
				{
				if (cacheEntry.DatabaseCountKnown == false && cacheEntry.Change != 0)
					{  idsToLookup.Add(cacheEntry.ID);  }
				}


			// Fill in the cache.

			if (idsToLookup.IsEmpty == false)
				{
				StringBuilder queryText = new StringBuilder("SELECT ContextID, ContextString, ReferenceCount FROM Contexts WHERE ");
				List<object> queryParams = new List<object>();

				AppendWhereClause_ColumnIsInNumberSet("ContextID", idsToLookup, queryText, queryParams);

				if (cancelled())
					{  return;  }
			
				// ContextReferenceCache is governed by the same lock as the database, so we need read/write to change it even
				// though we're not changing records yet.
				RequireAtLeast(LockType.ReadWrite);

				using (SQLite.Query query = connection.Query(queryText.ToString(), queryParams.ToArray()))
					{
					while (query.Step())
						{
						cache.SetDatabaseReferences(query.IntColumn(0), ContextString.FromExportedString(query.StringColumn(1)),
																				query.IntColumn(2));

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
				foreach (ContextReferenceCacheEntry cacheEntry in cache)
					{
					// Sanity checks
					#if DEBUG
					if (cacheEntry.DatabaseCountKnown == false && cacheEntry.Change != 0)
						{  
						throw new Exception("ContextReferenceCache entry " + cacheEntry.ID + ", \"" + cacheEntry.String + 
																"\" not properly filled in before flushing.");  
						}
					if (cacheEntry.DatabaseCountKnown == true && cacheEntry.DatabaseCount + cacheEntry.Change < 0)
						{  
						throw new Exception("ContextReferenceCache entry " + cacheEntry.ID + ", \"" + cacheEntry.String +
																"\" led to a negative reference count.");  
						}
					#endif

					if (cacheEntry.DatabaseCountKnown)
						{
						if (cacheEntry.DatabaseCount + cacheEntry.Change == 0)
							{
							idsToDelete.Add(cacheEntry.ID);
							}
						else if (cacheEntry.Change != 0)
							{
							updateQuery.BindValues(cacheEntry.DatabaseCount + cacheEntry.Change, cacheEntry.ID);
							updateQuery.Step();
							updateQuery.Reset(true);

							// Update the cache entry so it stays valid in case the operation is cancelled before the cache is emptied.  We 
							// can't remove the entry from the set while we're iterating through it.
							cacheEntry.DatabaseCount += cacheEntry.Change;
							cacheEntry.Change = 0;
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