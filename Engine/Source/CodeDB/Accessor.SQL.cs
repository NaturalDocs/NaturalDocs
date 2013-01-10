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
using GregValure.NaturalDocs.Engine.Topics;


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
		 *		whereClause - The SQL WHERE clause to apply to the query, such as "Topics.FileID=?".  It's recommended that you
		 *								  add the table name to fully qualify columns.
		 *		orderByClause - The SQL ORDER BY clause to apply to the query, such as "Topics.CommentLineNumber ASC", or null if 
		 *									 none.  It's recommended that you add the table name to fully qualify columns.
		 *		clauseParameters - Any parameters needed for question marks in the WHERE and ORDER BY clauses, or null if none.
		 *		cancelled - A <CancelDelegate> you can use to interrupt this process.  Pass <Delegates.NeverCancel> if you won't
		 *							 need to.
		 *		getTopicFlags - If you don't need every property in the <Topic> object you can use this to filter some out to save 
		 *									 memory or processing time.  In debug builds <Topic> will enforce these settings to prevent programming 
		 *									 errors.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		protected List<Topic> GetTopics (string whereClause, string orderByClause, object[] clauseParameters,
																	 CancelDelegate cancelled, GetTopicFlags getTopicFlags = GetTopicFlags.Everything)
			{
			#if DEBUG
			if (whereClause == null)
				{  throw new Exception ("You must define a WHERE clause when calling GetTopics().");  }
			#endif 

			RequireAtLeast(LockType.ReadOnly);

			List<Topic> topics = new List<Topic>();

			bool bodyLengthOnly = ((getTopicFlags & GetTopicFlags.BodyLengthOnly) != 0);
			bool lookupClasses = ((getTopicFlags & GetTopicFlags.DontLookupClasses) == 0);
			bool lookupContexts = ((getTopicFlags & GetTopicFlags.DontLookupContexts) == 0);
						
			StringBuilder queryText = new StringBuilder("SELECT TopicID, Title, Summary, Prototype, Symbol, SymbolDefinitionNumber, " +
																							  "Topics.ClassID, IsEmbedded, TopicTypeID, DeclaredAccessLevel, " +
																							  "EffectiveAccessLevel, Tags, CommentLineNumber, CodeLineNumber, " +
																							  "LanguageID, PrototypeContextID, BodyContextID, FileID ");

			if (bodyLengthOnly)
				{  queryText.Append(", length(Body) ");  }
			else
				{  queryText.Append(", Body ");  }

			if (lookupClasses)
				{  queryText.Append(", ifnull(Classes.ClassString, Classes.LookupKey) ");  }

			if (lookupContexts)
				{  queryText.Append(", PContexts.ContextString, BContexts.ContextString ");  }
																								
			queryText.Append("FROM Topics ");

			if (lookupClasses)
				{  queryText.Append("LEFT OUTER JOIN Classes ON Classes.ClassID = Topics.ClassID ");  }
			
			if (lookupContexts)
				{  
				queryText.Append("LEFT OUTER JOIN Contexts AS PContexts ON PContexts.ContextID = PrototypeContextID " +
													"LEFT OUTER JOIN Contexts AS BContexts ON BContexts.ContextID = BodyContextID ");  
				}
				
			queryText.Append("WHERE ");
			
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
					topic.ClassID = query.IntColumn(6);
					topic.IsEmbedded = (query.IntColumn(7) == 1);

					topic.TopicTypeID = query.IntColumn(8);
					topic.DeclaredAccessLevel = (Languages.AccessLevel)query.IntColumn(9);
					topic.EffectiveAccessLevel = (Languages.AccessLevel)query.IntColumn(10);
					topic.TagString = query.StringColumn(11);

					topic.CommentLineNumber = query.IntColumn(12);
					topic.CodeLineNumber = query.IntColumn(13);

					topic.LanguageID = query.IntColumn(14);
					topic.PrototypeContextID = query.IntColumn(15);
					topic.BodyContextID = query.IntColumn(16);
					topic.FileID = query.IntColumn(17);

					topic.IgnoredFields = Topic.IgnoreFields.None;

					if (bodyLengthOnly)
						{  
						topic.BodyLength = query.IntColumn(18);  
						topic.IgnoredFields |= Topic.IgnoreFields.Body;
						}
					else
						{  topic.Body = query.StringColumn(18);  }

					int nextColumn = 19;

					if (lookupClasses)
						{
						topic.ClassString = ClassString.FromExportedString( query.StringColumn(nextColumn) );
						nextColumn++;
						}
					else
						{  topic.IgnoredFields |= Topic.IgnoreFields.ClassString;  }

					if (lookupContexts)
						{
						topic.PrototypeContext = ContextString.FromExportedString( query.StringColumn(nextColumn) );
						topic.BodyContext = ContextString.FromExportedString( query.StringColumn(nextColumn + 1) );
						nextColumn += 2;
						}
					else
						{  topic.IgnoredFields |= Topic.IgnoreFields.PrototypeContext | Topic.IgnoreFields.BodyContext;  }

					topics.Add(topic);

					if (lookupClasses)
						{  classIDLookupCache.Add(topic.ClassString, topic.ClassID);  }
					if (lookupContexts)
						{
						contextIDLookupCache.Add(topic.PrototypeContext, topic.PrototypeContextID);
						contextIDLookupCache.Add(topic.BodyContext, topic.BodyContextID);
						}
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
		 * If you don't need every property in the <Topic> object you can use <GetTopicFlags> to filter some out to save
		 * memory or processing time.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Topic> GetTopicsInFile (int fileID, CancelDelegate cancelled, GetTopicFlags getTopicFlags = GetTopicFlags.Everything)
			{
			RequireAtLeast(LockType.ReadOnly);

			object[] clauseParams = new object[1];
			clauseParams[0] = fileID;

			return GetTopics("Topics.FileID=?", "Topics.CommentLineNumber ASC", clauseParams, cancelled, getTopicFlags);
			}
			
			
		/* Function: GetTopicsInClass
		 * 
		 * Retrieves a list of all the topics present in the passed class ID.  The topics will be grouped by file, and within each group
		 * they will be in comment line number order.  The files will be in no particular order.  If there are no topics it will return an 
		 * empty list.  Pass a <CancelDelegate> if you'd like to be able to interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * If you don't need every property in the <Topic> object you can use <GetTopicFlags> to filter some out to save memory
		 * or processing time.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Topic> GetTopicsInClass (int classID, CancelDelegate cancelled, GetTopicFlags getTopicFlags = GetTopicFlags.Everything)
			{
			RequireAtLeast(LockType.ReadOnly);

			object[] clauseParams = new object[1];
			clauseParams[0] = classID;

			string orderBy = "Topics.FileID, Topics.CommentLineNumber ASC";

			#if DEBUG
			// Oh, you thought we were fucking around when we said the files would be in no particular order, did you?  No no my friend,
			// we are not.
			System.Random random = new Random();
			orderBy = "Topics.FileID " + (random.Next(0, 2) == 0 ? "ASC" : "DESC") + ", Topics.CommentLineNumber ASC";

			// What the hell?  Okay, so it's possible for files to always get the same file ID relative to each other.  If Platform A always
			// returns the files in a folder in alphabetical order, file 1 could always have a lower ID than file 2 and thus always appear
			// first when ordering the query by file ID.  If this happens to be the desired order then it can lead to code which appears
			// to behave correctly but actually doesn't -- it's depending on a side effect that isn't guaranteed.  It's only when someone 
			// runs it on Platform B where the files are returned in any order that this screws up and leads to unpredictable output.  So
			// instead we guarantee it's your problem NOW to force you handle it correctly.
			#endif

			return GetTopics("Topics.ClassID=?", orderBy, clauseParams, cancelled, getTopicFlags);
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
																	 GetTopicFlags getTopicFlags = GetTopicFlags.Everything)
			{
			RequireAtLeast(LockType.ReadOnly);

			StringBuilder whereClause = new StringBuilder();
			List<object> clauseParameters = new List<object>();

			bool isFirst = true;
			foreach (int topicID in topicIDs)
				{
				if (isFirst)
					{  isFirst = false;  }
				else
					{  whereClause.Append("OR ");  }

				whereClause.Append("Topics.TopicID=? ");
				clauseParameters.Add(topicID);
				}

			if (clauseParameters.Count == 0)
				{  return new List<Topic>();  }

			return GetTopics(whereClause.ToString(), null, clauseParameters.ToArray(), cancelled, getTopicFlags);
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
																						 GetTopicFlags getTopicFlags = GetTopicFlags.Everything)
			{
			RequireAtLeast(LockType.ReadOnly);

			StringBuilder whereClause = new StringBuilder();
			List<object> clauseParameters = new List<object>();

			bool isFirst = true;
			foreach (EndingSymbol endingSymbol in endingSymbols)
				{
				if (isFirst)
					{  isFirst = false;  }
				else
					{  whereClause.Append("OR ");  }

				whereClause.Append("Topics.EndingSymbol=? ");
				clauseParameters.Add(endingSymbol.ToString());
				}

			return GetTopics(whereClause.ToString(), null, clauseParameters.ToArray(), cancelled, getTopicFlags);
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
		 *		ClassString - Can be null, which means it is global and doesn't define a class.
		 *		ClassID - Must be zero.  This will be automatically assigned and the <Topic> updated.
		 *		IsEmbedded - Must be set.
		 *		TopicTypeID - Must be set.
		 *		DeclaredAccessLevel - Optional.
		 *		EffectiveAccessLevel - Must be set to something other than Unknown.
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
			// ClassString
			RequireZero("AddTopic", "ClassID", topic.ClassID);
			// IsEmbedded
			RequireNonZero("AddTopic", "TopicTypeID", topic.TopicTypeID);
			// DeclaredAccessLevel
			RequireNotValue("AddTopic", "EffectiveAccessLevel", (int)topic.EffectiveAccessLevel, (int)Languages.AccessLevel.Unknown);
			// TagString
			RequireNonZero("AddTopic", "FileID", topic.FileID);
			RequireNonZero("AddTopic", "CommentLineNumber", topic.CommentLineNumber);
			RequireNonZero("AddTopic", "CodeLineNumber", topic.CodeLineNumber);
			RequireNonZero("AddTopic", "LanguageID", topic.LanguageID);
			// PrototypeContext
			RequireZero("AddTopic", "PrototypeContextID", topic.PrototypeContextID);
			// BodyContext
			RequireZero("AddTopic", "BodyContextID", topic.BodyContextID);
			
			var codeDB = Engine.Instance.CodeDB;

			RequireAtLeast(LockType.ReadWrite);
			BeginTransaction();

			try
				{
				topic.TopicID = codeDB.UsedTopicIDs.LowestAvailable;
				GetOrCreateClassID(topic);
				GetOrCreateContextIDs(topic);
			
				connection.Execute("INSERT INTO Topics (TopicID, Title, Body, Summary, Prototype, Symbol, SymbolDefinitionNumber, ClassID, " +
														"IsEmbedded, EndingSymbol, TopicTypeID, DeclaredAccessLevel, EffectiveAccessLevel, Tags, FileID, " +
														"CommentLineNumber, CodeLineNumber, LanguageID, PrototypeContextID, BodyContextID) " +
													"VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
													topic.TopicID, topic.Title, topic.Body, topic.Summary, topic.Prototype, topic.Symbol, topic.SymbolDefinitionNumber,
													topic.ClassID, (topic.IsEmbedded ? 1 : 0), topic.Symbol.EndingSymbol, topic.TopicTypeID, 
													(int)topic.DeclaredAccessLevel, (int)topic.EffectiveAccessLevel, topic.TagString, topic.FileID, topic.CommentLineNumber, 
													topic.CodeLineNumber, topic.LanguageID, topic.PrototypeContextID, topic.BodyContextID										 
													);
			
				codeDB.UsedTopicIDs.Add(topic.TopicID);

				IDObjects.SparseNumberSet newTopicsForEndingSymbol = codeDB.NewTopicsByEndingSymbol[topic.Symbol.EndingSymbol];
				if (newTopicsForEndingSymbol == null)
					{
					newTopicsForEndingSymbol = new IDObjects.SparseNumberSet();
					codeDB.NewTopicsByEndingSymbol.Add(topic.Symbol.EndingSymbol, newTopicsForEndingSymbol);
					}
				newTopicsForEndingSymbol.Add(topic.TopicID);

				codeDB.ClassIDReferenceChangeCache.AddReference(topic.ClassID);
				codeDB.ContextIDReferenceChangeCache.AddReference(topic.PrototypeContextID);
				codeDB.ContextIDReferenceChangeCache.AddReference(topic.BodyContextID);

				CommitTransaction();
				}
			catch
				{
				RollbackTransactionForException();
				throw;
				}
			

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

			bool classChanged = ((changeFlags & Topic.ChangeFlags.Class) != 0);
			bool contextsChanged = ( (changeFlags & (Topic.ChangeFlags.PrototypeContext | Topic.ChangeFlags.BodyContext)) != 0 );

			RequireAtLeast(LockType.ReadWrite);
			BeginTransaction();

			try
				{
				// DEPENDENCY: This must update all fields marked relevant in Topic.DatabaseCompare().  If that function changes this one
				// must change as well.

				newTopic.TopicID = oldTopic.TopicID;
			
				if (classChanged)
					{  GetOrCreateClassID(newTopic);  }
				else
					{  newTopic.ClassID = oldTopic.ClassID;  }

				if (contextsChanged)
					{  GetOrCreateContextIDs(newTopic);  }
				else
					{
					newTopic.PrototypeContextID = oldTopic.PrototypeContextID;
					newTopic.BodyContextID = oldTopic.BodyContextID;
					}

				connection.Execute("UPDATE Topics SET Summary=?, CommentLineNumber=?, CodeLineNumber=?, " +
														"ClassID=?, PrototypeContextID=?, BodyContextID=?, IsEmbedded=? " +
													"WHERE TopicID = ?",
													newTopic.Summary, newTopic.CommentLineNumber, newTopic.CodeLineNumber,
													newTopic.ClassID, newTopic.PrototypeContextID, newTopic.BodyContextID, (newTopic.IsEmbedded ? 1 : 0),
													oldTopic.TopicID);

				if (classChanged)
					{
					var referenceCache = Engine.Instance.CodeDB.ClassIDReferenceChangeCache;

					referenceCache.RemoveReference(oldTopic.ClassID);
					referenceCache.AddReference(newTopic.ClassID);
					}

				if (contextsChanged)
					{
					var referenceCache = Engine.Instance.CodeDB.ContextIDReferenceChangeCache;

					referenceCache.RemoveReference(oldTopic.PrototypeContextID);
					referenceCache.RemoveReference(oldTopic.BodyContextID);
					referenceCache.AddReference(newTopic.PrototypeContextID);
					referenceCache.AddReference(newTopic.BodyContextID);
					}

				CommitTransaction();
				}
			catch
				{
				RollbackTransactionForException();
				throw;
				}

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
		 *		ClassString - Can be null, which means it is global and doesn't define a class.
		 *		ClassID - Must be set.
		 *		IsEmbedded - Must be set.
		 *		TopicTypeID - Must be set.
		 *		DeclaredAccessLevel - Optional.
		 *		EffectiveAccessLevel - Must be set to something other than Unknown.
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
			// ClassString
			if (topic.ClassString != null)
				{  RequireNonZero("DeleteTopic", "ClassID", topic.ClassID);  }
			// IsEmbedded
			RequireNonZero("DeleteTopic", "TopicTypeID", topic.TopicTypeID);
			// DeclaredAccessLevel
			RequireNotValue("DeleteTopic", "EffectiveAccessLevel", (int)topic.EffectiveAccessLevel, (int)Languages.AccessLevel.Unknown);
			// TagString
			RequireNonZero("DeleteTopic", "FileID", topic.FileID);
			RequireNonZero("DeleteTopic", "CommentLineNumber", topic.CommentLineNumber);
			RequireNonZero("DeleteTopic", "CodeLineNumber", topic.CodeLineNumber);
			RequireNonZero("DeleteTopic", "LanguageID", topic.LanguageID);
			// PrototypeContext, null is a valid value
			if (topic.PrototypeContext != null)
				{  RequireNonZero("DeleteTopic", "PrototypeContextID", topic.PrototypeContextID);  }
			// BodyContext, null is a valid value
			if (topic.BodyContext != null)
				{  RequireNonZero("DeleteTopic", "BodyContextID", topic.BodyContextID);  }

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


			BeginTransaction();

			try
				{
				// Reset these links back to unresolved and add them to linksToResolve.

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

				codeDB.ClassIDReferenceChangeCache.RemoveReference(topic.ClassID);
				codeDB.ContextIDReferenceChangeCache.RemoveReference(topic.PrototypeContextID);
				codeDB.ContextIDReferenceChangeCache.RemoveReference(topic.BodyContextID);

				CommitTransaction();
				}
			catch
				{
				RollbackTransactionForException();
				throw;
				}
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
		 *		ClassString - Can be null, which means its global and does not define a class.
		 *		ClassID - Must be zero.  These will be automatically assigned and the <Topics> updated.
		 *		IsEmbedded - Must be set.
		 *		TopicTypeID - Must be set.
		 *		DeclaredAccessLevel - Optional.
		 *		EffectiveAccessLevel - Must be set to something other than Unknown.
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
							newTopic.ClassID = oldTopics[i].ClassID;
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
				if (madeChanges == true)
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
				
				try
					{	
					foreach (Topic topic in topics)
						{  
						DeleteTopic(topic);
					
						if (cancelled())
							{  break;  }
						}

					CommitTransaction();
					}
				catch
					{
					RollbackTransactionForException();
					throw;
					}
				}
			}



		// Group: Link Functions
		// __________________________________________________________________________


		/* Function: GetLinks
		 * 
		 * A generic function for retrieving all the <Links> that satisfy the passed WHERE clause.  If there are none it will return 
		 * an empty list.
		 * 
		 * Parameters:
		 * 
		 *		whereClause - The SQL WHERE clause to apply to the query, such as "Links.FileID=?".  It's recommended that you
		 *									  add the table name to fully qualify columns.
		 *		clauseParameters - Any parameters needed for question marks in the WHERE clause, or null if none.
		 *		cancelled - A <CancelDelegate> you can use to interrupt this process.  Pass <Delegates.NeverCancel> if you won't
		 *							 need to.
		 *		getLinkFlags - If you don't need every property in the <Link> object you can use this to filter some out to save 
		 *								  processing time.  In debug builds <Link> will enforce these settings to prevent programming errors.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		protected List<Link> GetLinks (string whereClause, object[] clauseParameters, CancelDelegate cancelled,
																GetLinkFlags getLinkFlags = GetLinkFlags.Everything)
			{
			#if DEBUG
			if (whereClause == null)
				{  throw new Exception ("You must define a WHERE clause when calling GetLinks().");  }
			#endif 

			RequireAtLeast(LockType.ReadOnly);

			List<Link> links = new List<Link>();

			bool lookupClasses = ((getLinkFlags & GetLinkFlags.DontLookupClasses) == 0);
			bool lookupContexts = ((getLinkFlags & GetLinkFlags.DontLookupContexts) == 0);

			StringBuilder queryText = new StringBuilder("SELECT LinkID, FileID, LanguageID, Type, TextOrSymbol, EndingSymbol, " +
																								"Links.ContextID, Links.ClassID, TargetTopicID, TargetScore ");

			if (lookupClasses)
				{  queryText.Append(", ifnull(Classes.ClassString, Classes.LookupKey) ");  }
			if (lookupContexts)
				{  queryText.Append(", Contexts.ContextString ");  }

			queryText.Append("FROM Links ");

			if (lookupClasses)
				{  queryText.Append("LEFT OUTER JOIN Classes ON Classes.ClassID = Links.ClassID ");  }
			if (lookupContexts)
				{  queryText.Append("LEFT OUTER JOIN Contexts ON Contexts.ContextID = Links.ContextID ");  }

			queryText.Append("WHERE ");
			
			queryText.Append('(');
			queryText.Append(whereClause);
			queryText.Append(')');

			using (SQLite.Query query = connection.Query(queryText.ToString(), clauseParameters))
				{
				while (query.Step() && !cancelled())
					{
					Link link = new Link();

					link.LinkID = query.IntColumn(0);
					link.FileID = query.IntColumn(1);
					link.LanguageID = query.IntColumn(2);
					link.Type = (LinkType)query.IntColumn(3);
					link.TextOrSymbol = query.StringColumn(4);
					link.EndingSymbol = EndingSymbol.FromExportedString( query.StringColumn(5) );
					link.ContextID = query.IntColumn(6);
					link.ClassID = query.IntColumn(7);
					link.TargetTopicID = query.IntColumn(8);
					link.TargetScore = query.LongColumn(9);

					link.IgnoredFields = Link.IgnoreFields.None;

					int nextColumn = 10;

					if (lookupClasses)
						{
						link.ClassString = ClassString.FromExportedString( query.StringColumn(nextColumn) );
						nextColumn++;
						}
					else
						{  link.IgnoredFields |= Link.IgnoreFields.ClassString;  }

					if (lookupContexts)
						{
						link.Context = ContextString.FromExportedString( query.StringColumn(nextColumn) );
						nextColumn++;
						}
					else
						{  link.IgnoredFields |= Link.IgnoreFields.Context;  }

					links.Add(link);

					if (lookupClasses)
						{  classIDLookupCache.Add(link.ClassString, link.ClassID);  }
					if (lookupContexts)
						{  contextIDLookupCache.Add(link.Context, link.ContextID);  }
					}
				}
			
			return links;
			}


		/* Function: GetLinkByID
		 * 
		 * Retrieves a link by its ID.  Assumes the link already exists.
		 * 
		 * If you don't need every property in the <Link> object you can use <GetLinkFlags> to filter some out and save 
		 * processing time.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public Link GetLinkByID (int linkID, GetLinkFlags getLinkFlags = GetLinkFlags.Everything)
			{
			RequireAtLeast(LockType.ReadOnly);
			
			object[] parameters = new object[1];
			parameters[0] = linkID;

			List<Link> links = GetLinks("Links.LinkID=?", parameters, Delegates.NeverCancel, getLinkFlags);
			
			#if DEBUG
			if (links.Count == 0)
				{  throw new Exception ("Tried to look up link ID " + linkID + " which doesn't exist.");  }
			#endif

			return links[0];
			}


		/* Function: GetLinksInFile
		 * 
		 * Retrieves a list of all the links present in the passed file ID.  If there are none it will return an empty list.  Pass a 
		 * <CancelDelegate> if you'd like to be able to interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * If you don't need every property in the <Link> object you can use <GetLinkFlags> to filter some out and save 
		 * processing time.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Link> GetLinksInFile (int fileID, CancelDelegate cancelled, GetLinkFlags getLinkFlags = GetLinkFlags.Everything)
			{
			RequireAtLeast(LockType.ReadOnly);
			
			object[] parameters = new object[1];
			parameters[0] = fileID;

			return GetLinks("Links.FileID=?", parameters, cancelled, getLinkFlags);
			}


		/* Function: GetLinksInClass
		 * 
		 * Retrieves a list of all the links present in the passed class ID.  If there are none it will return an empty list.  Pass a 
		 * <CancelDelegate> if you'd like to be able to interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * If you don't need every property in the <Link> object you can use <GetLinkFlags> to filter some out and save 
		 * processing time.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Link> GetLinksInClass (int classID, CancelDelegate cancelled, GetLinkFlags getLinkFlags = GetLinkFlags.Everything)
			{
			RequireAtLeast(LockType.ReadOnly);
			
			object[] parameters = new object[1];
			parameters[0] = classID;

			return GetLinks("Links.ClassID=?", parameters, cancelled, getLinkFlags);
			}


		/* Function: GetNaturalDocsLinksInFiles
		 * 
		 * Retrieves a list of all the Natural Docs links present in the passed file IDs.  If there are none it will return an empty list.  
		 * Pass a <CancelDelegate> if you'd like to be able to interrupt this process, or <Delegates.NeverCancel> if not.
		 * 
		 * If you don't need every property in the <Link> object you can use <GetLinkFlags> to filter some out and save 
		 * processing time.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Link> GetNaturalDocsLinksInFiles (IEnumerable<int> fileIDs, CancelDelegate cancelled, 
																						GetLinkFlags getLinkFlags = GetLinkFlags.Everything)
			{
			RequireAtLeast(LockType.ReadOnly);
			
			StringBuilder whereClause = new StringBuilder("Links.Type=? AND (");

			List<object> whereParams = new List<object>();
			whereParams.Add( (int)LinkType.NaturalDocs );

			bool isFirst = true;
			foreach (int fileID in fileIDs)
				{
				if (!isFirst)
					{  whereClause.Append(" OR ");  }
				else
					{  isFirst = false;  }

				whereClause.Append("Links.FileID=? ");
				whereParams.Add(fileID);
				}

			whereClause.Append(')');

			return GetLinks(whereClause.ToString(), whereParams.ToArray(), cancelled, getLinkFlags);
			}


		/* Function: GetLinksByEndingSymbol
		 * 
		 * Retrieves a list of all the <Links> present that use the passed <EndingSymbol>.  Note that this also searches 
		 * <CodeDB.AlternateLinkEndingSymbols> so the actual <Link> object may not have the passed <EndingSymbol> as a property.
		 * If there are none it will return an empty list.  Pass a <CancelDelegate> if you'd like to be able to interrupt this process, or 
		 * <Delegates.NeverCancel> if not.
		 * 
		 * If you don't need every property in the <Link> object you can use <GetLinkFlags> to filter some out and save 
		 * processing time.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<Link> GetLinksByEndingSymbol (EndingSymbol endingSymbol, CancelDelegate cancelled,
																					GetLinkFlags getLinkFlags = GetLinkFlags.Everything)
			{
			RequireAtLeast(LockType.ReadOnly);
			

			// Get links which have it as their primary ending symbol

			object[] parameters = new object[1];
			parameters[0] = endingSymbol.ToString();

			List<Link> links = GetLinks("Links.EndingSymbol=?", parameters, cancelled);


			// Find link IDs that have it as an alternate ending symbol

			IDObjects.SparseNumberSet alternateLinkIDs = new IDObjects.SparseNumberSet();
			
			using (SQLite.Query query = connection.Query("SELECT LinkID FROM AlternateLinkEndingSymbols " +
																										"WHERE EndingSymbol = ?", endingSymbol.ToString()))
				{
				while (query.Step() && !cancelled())
					{  alternateLinkIDs.Add( query.IntColumn(0) );  }
				}


			// Get the links that have it as an alternate ending symbol

			if (!alternateLinkIDs.IsEmpty)
				{
				StringBuilder whereClause = new StringBuilder();
				List<object> whereParams = new List<object>();

				bool isFirst = true;
				foreach (int alternateLinkID in alternateLinkIDs)
					{
					if (!isFirst)
						{  whereClause.Append("OR ");  }
					else
						{  isFirst = false;  }

					whereClause.Append("LinkID=? ");
					whereParams.Add(alternateLinkID);
					}

				List<Link> alternateLinks = GetLinks(whereClause.ToString(), whereParams.ToArray(), cancelled);
				links.AddRange(alternateLinks);
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
		 *		ClassString - Can be null, which means not part of a class.
		 *		ClassID - Must be zero.  This will be automatically assigned and the <Link> updated.
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
			// ClassString
			RequireZero("AddLink", "ClassID", link.ClassID);
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
			

			var codeDB = Engine.Instance.CodeDB;

			RequireAtLeast(LockType.ReadWrite);
			BeginTransaction();

			try
				{
				link.LinkID = codeDB.UsedLinkIDs.LowestAvailable;
				GetOrCreateContextIDs(link);
				GetOrCreateClassID(link);

				connection.Execute("INSERT INTO Links (LinkID, Type, TextOrSymbol, ContextID, FileID, ClassID, LanguageID, EndingSymbol, " +
														"TargetTopicID, TargetScore) " +
													"VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, 0)",
													link.LinkID, (int)link.Type, link.TextOrSymbol, link.ContextID, link.FileID, link.ClassID, link.LanguageID, 
													link.EndingSymbol.ToString(), UnresolvedTargetTopicID.NewLink
													);

				codeDB.UsedLinkIDs.Add(link.LinkID);
				codeDB.LinksToResolve.Add(link.LinkID);
				codeDB.ContextIDReferenceChangeCache.AddReference(link.ContextID);
				codeDB.ClassIDReferenceChangeCache.AddReference(link.ClassID);

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
			catch
				{
				RollbackTransactionForException();
				throw;
				}


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
		 *		ClassString - Can be null, which means not part of a class.
		 *		ClassID - Must be set.
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
			if (link.Context != null)
				{  RequireNonZero("DeleteLink", "ContextID", link.ContextID);  }
			RequireNonZero("DeleteLink", "FileID", link.FileID);
			// ClassString
			if (link.ClassString != null)
				{  RequireNonZero("DeleteLink", "ClassID", link.ClassID);  }
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

			try
				{
				connection.Execute("DELETE FROM Links WHERE LinkID=?", link.LinkID);
			
				codeDB.UsedLinkIDs.Remove(link.LinkID);
				codeDB.LinksToResolve.Remove(link.LinkID);  // Just in case, so there's no hanging references
				codeDB.ContextIDReferenceChangeCache.RemoveReference(link.ContextID);
				codeDB.ClassIDReferenceChangeCache.RemoveReference(link.ClassID);

				if (link.Type == LinkType.NaturalDocs)
					{
					connection.Execute("DELETE FROM AlternateLinkEndingSymbols WHERE LinkID=?", link.LinkID);
					}

				CommitTransaction();
				}
			catch
				{
				RollbackTransactionForException();
				throw;
				}
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
				if (madeChanges == true)
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
				
				try
					{	
					foreach (Link link in links)
						{  
						DeleteLink(link);
					
						if (cancelled())
							{  break;  }
						}

					CommitTransaction();
					}
				catch
					{
					RollbackTransactionForException();
					throw;
					}
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

			BeginTransaction();
			try
				{
				connection.Execute("UPDATE Links SET TargetTopicID=?, TargetScore=? " +
													  "WHERE LinkID = ?", link.TargetTopicID, link.TargetScore, link.LinkID);
				CommitTransaction();
				}
			catch
				{
				RollbackTransactionForException();
				throw;
				}


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



		// Group: Class Functions
		// __________________________________________________________________________


		/* Function: GetClassByID
		 * 
		 * Retrieves the class with the passed ID.  Even if a class has been deleted this will still return a <ClassString> until
		 * <Cleanup()> is called.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public ClassString GetClassByID (int classID)
			{
			RequireAtLeast(LockType.ReadOnly);


			// DEPENDENCY: This function assumes that CacheOrCreateClassIDs() immediately puts a record in the database for
			// new class IDs, and thus selecting it from the database will work even if the change cache hasn't been flushed yet.

			using (SQLite.Query query = connection.Query("SELECT ifnull(ClassString,LookupKey) " +
																										"FROM Classes WHERE ClassID=?", classID))
				{
				if (query.Step())
					{  return ClassString.FromExportedString(query.StringColumn(0));  }
				else
					{  return new ClassString();  }
				}
			}


		/* Function: GetClassesByID
		 * 
		 * Retrieves a list of all the classes defined in the database within the passed NumberSet.  Pass a <CancelDelegate> if you'd
		 * like to be able to interrupt this process, or <Delegates.NeverCancel> if not.  If some of the classes have been deleted this
		 * will still return values for them until <Cleanup()> is called.
		 * 
		 * Requirements:
		 * 
		 *		- You must have at least a read-only lock.
		 */
		public List<KeyValuePair<int, ClassString>> GetClassesByID (IDObjects.NumberSet ids, CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadOnly);

			List<KeyValuePair<int, ClassString>> classes = new List<KeyValuePair<int, ClassString>>();

			if (ids.IsEmpty)
				{  return classes;  }


			// DEPENDENCY: This function assumes that CacheOrCreateClassIDs() immediately puts a record in the database for
			// new class IDs, and thus we'll be able to get everything from the database even if the change cache hasn't been flushed 
			// yet.

			StringBuilder queryText = new StringBuilder("SELECT ClassID, ifnull(ClassString,LookupKey) " +
																								  "FROM Classes WHERE ");
			List<object> queryParams = new List<object>();

			AppendWhereClause_ColumnIsInNumberSet("ClassID", ids, queryText, queryParams);

			using (SQLite.Query query = connection.Query(queryText.ToString(), queryParams.ToArray()))
				{
				while (query.Step())
					{
					int classID = query.IntColumn(0);
					ClassString classString = ClassString.FromExportedString(query.StringColumn(1));

					classes.Add( new KeyValuePair<int, ClassString>(classID, classString));
					
					if (cancelled())
						{  break;  }
					}
				}

			return classes;
			}


		/* Function: GetOrCreateClassID
		 * 
		 * Retrieves the ID for <Topic.ClassString> if it's not already set.  If an existing ID cannot be found, it will be created.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If a new class ID is created, it will be upgraded automatically.
		 *		
		 * Topic Requirements:
		 * 
		 *		ClassString - Can be null, which means the topic is global and doesn't create a class.
		 *		ClassID - If this is zero and ClassString is not null, ClassID will be looked up or created.  If it's already non-zero this 
		 *						 is a no-op.
		 */
		public void GetOrCreateClassID (Topic topic)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);

			if (topic.ClassIDKnown == false)
				{  
				CacheOrCreateClassIDs(topic.ClassString);
				topic.ClassID = classIDLookupCache[topic.ClassString];
				}
			}


		/* Function: GetOrCreateClassIDs
		 * 
		 * Retrieves the class IDs for each <Topic's> ClassString if they are not already set.  If existing IDs cannot be found, 
		 * they will be created.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If new class IDs are created, it will be upgraded automatically.
		 *		
		 * Topic Requirements:
		 * 
		 *		ClassString - Can be null, which means the topic is global and doesn't create a class.
		 *		ClassID - If this is zero and ClassString is not null, ClassID will be looked up or created.  If it's already non-zero this 
		 *						 is a no-op.
		 */
		public void GetOrCreateClassIDs (IEnumerable<Topic> topics)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);


			// Cache or create any missing class IDs.  There may be none so create the HashSet on demand.

			HashSet<ClassString> classes = null;

			foreach (var topic in topics)
				{
				if (topic.ClassIDKnown == false)
					{  
					if (classes == null)
						{  classes = new HashSet<ClassString>();  }

					classes.Add(topic.ClassString);  
					}
				}

			if (classes != null)
				{  CacheOrCreateClassIDs(classes);  }
			else
				{  return;  }


			// Fill in the Topics.

			foreach (var topic in topics)
				{
				if (topic.ClassIDKnown == false)
					{  topic.ClassID = classIDLookupCache[topic.ClassString];  }
				}
			}


		/* Function: GetOrCreateClassID
		 * 
		 * Retrieves the ID for <Link.ClassString> if it's not already set.  If an existing ID cannot be found, it will be created.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If a new class ID is created, it will be upgraded automatically.
		 *		
		 * Topic Requirements:
		 * 
		 *		ClassString - Can be null, which means the link is global and doesn't create a class.
		 *		ClassID - If this is zero and ClassString is not null, ClassID will be looked up or created.  If it's already non-zero this 
		 *						 is a no-op.
		 */
		public void GetOrCreateClassID (Link link)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);

			if (link.ClassIDKnown == false)
				{  
				CacheOrCreateClassIDs(link.ClassString);
				link.ClassID = classIDLookupCache[link.ClassString];
				}
			}


		/* Function: CacheOrCreateClassIDs
		 * 
		 * Retrieves the IDs for each <ClassString> and stores them in <classIDLookupCache>.  If they don't exist in the 
		 * database they will be created.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If new classes are created, it will be upgraded automatically.
		 */
		protected void CacheOrCreateClassIDs (IEnumerable<ClassString> classStrings)
			{
			// Remember that classIDLookupCache is local to the accessor and doesn't need any locking.
			// ClassIDReferenceChangeCache is part of CodeDB.Manager and requires a database lock.

			RequireAtLeast(LockType.ReadPossibleWrite);

			
			// Create a list of all classStrings not already in the cache.  Since it's possible that they'll all be in the cache we
			// create the list object on demand.

			List<ClassString> uncachedClassStrings = null;

			foreach (var classString in classStrings)
				{
				if (classString != null && classIDLookupCache.Contains(classString) == false)
					{
					if (uncachedClassStrings == null)
						{  uncachedClassStrings = new List<ClassString>();  }

					uncachedClassStrings.Add(classString);  
					}
				}


			// Can we quit early?

			if (uncachedClassStrings == null)
				{  return;  }


			// Create a query to lookup the uncached classes in the database.  The IDs may already be assigned.

			System.Text.StringBuilder queryText = new System.Text.StringBuilder("SELECT ClassID, ifnull(ClassString, LookupKey) " + 
																																					 "FROM Classes WHERE");
			object[] queryParams = new object[uncachedClassStrings.Count];

			for (int i = 0; i < uncachedClassStrings.Count; i++)
				{
				if (i != 0)
					{  queryText.Append(" OR");  }

				queryText.Append(" LookupKey=?");
				queryParams[i] = uncachedClassStrings[i].LookupKey;
				}


			// Run the query to fill in the cache with whatever already exists in the database.

			using (SQLite.Query query = connection.Query(queryText.ToString(), queryParams))
				{
			   while (query.Step())
			      {  
					classIDLookupCache.Add( ClassString.FromExportedString(query.StringColumn(1)), query.IntColumn(0) );
					}
			   }


			// Pare down our list of uncached class strings.

			int j = 0;  // the compiler complains if we use i
			while (j < uncachedClassStrings.Count)
				{
				if (classIDLookupCache.Contains(uncachedClassStrings[j]))
					{  uncachedClassStrings.RemoveAt(j);  }
				else
					{  j++;  }
				}


			// Can we quit now?

			if (uncachedClassStrings.Count == 0)
				{  return;  }


			// Create anything we still need.

			// DEPENDENCY: GetClassByID() and GetClassesByID() assume *every* newly created class ID will have a record
			// in the database, even if the change cache hasn't been flushed yet.

			// DEPENDENCY: FlushClassIDReferenceChangeCache() assumes *every* newly created class ID will have an 
			// entry in CodeDB.ClassIDReferenceChangeCache with database references set to zero.

			RequireAtLeast(LockType.ReadWrite);
			BeginTransaction();

			try
				{
				using (SQLite.Query query = connection.Query("INSERT INTO Classes (ClassID, ClassString, LookupKey, ReferenceCount) " +
																									"VALUES (?, ?, ?, 0)") )
					{
					var codeDB = Engine.Instance.CodeDB;

					foreach (var classString in uncachedClassStrings)
						{
						int id = codeDB.UsedClassIDs.LowestAvailable;

						query.BindValues(id, (classString.ToString() == classString.LookupKey ? null : classString.ToString()), 
														  classString.LookupKey);
						query.Step();
						query.Reset(true);

						codeDB.UsedClassIDs.Add(id);
						classIDLookupCache.Add(classString, id);

						codeDB.ClassIDReferenceChangeCache.SetDatabaseReferenceCount(id, 0);
						}
					}

				CommitTransaction();
				}
			catch
				{
				RollbackTransactionForException();
				throw;
				}
			}


		/* Function: CacheOrCreateClassIDs
		 * 
		 * Retrieves the IDs for each <ClassString> and stores them in <classIDLookupCache>.  If they don't exist in the 
		 * database they will be created.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If new classes are created, it will be upgraded automatically.
		 */
		protected void CacheOrCreateClassIDs (params ClassString[] classStrings)
			{
			CacheOrCreateClassIDs((IEnumerable<ClassString>)classStrings);
			}


		/* Function: FlushClassIDReferenceChangeCache
		 * 
		 * Applies anything waiting in <CodeDB.Manager.ClassIDReferenceChangeCache> to the database.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If the database needs to be updated it will be upgraded automatically.
		 */
		public void FlushClassIDReferenceChangeCache (CancelDelegate cancelled)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);

			ReferenceChangeCache cache = Instance.CodeDB.ClassIDReferenceChangeCache;

			if (cache.Count == 0)
				{  return;  }


			// Figure out which IDs we need to get database counts for.

			// DEPENDENCY:
			//
			// - This assumes CacheOrCreateClassIDs() will create an entry in CodeDB.ClassIDReferenceChangeCache for *every*
			//    newly created class ID with the database reference count set to zero.
			//
			// - This also assumes that this function deletes every class ID record with zero references from the database, and
			//   the zero reference entry in ClassIDReferenceChangeCache will exist until then.
			//
			// - Therefore, we can assume that there will be no zero reference records in the database that aren't also represented
			//   in ClassIDReferenceChangeCache with a known database reference count of zero.
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

				// Also see if there are changes in the cache at all, since we can avoid waiting for a read/write lock if not.  A 
				// ReferenceChange of zero still counts if DatabaseReferenceCount is also zero because that's an empty record we 
				// have to remove.
				if (cacheEntry.ReferenceChange != 0 || 
					 (cacheEntry.DatabaseReferenceCountKnown && cacheEntry.DatabaseReferenceCount == 0) )
					{  hasChanges = true;  }
				}

			if (hasChanges == false)
				{  return;  }


			// We have to do this before filling in the cache because ClassIDReferenceChangeCache is governed by the same lock 
			// as the database, so we need read/write to change it even though we're not changing actual records yet.

			RequireAtLeast(LockType.ReadWrite);


			// Fill in the cache.

			if (idsToLookup.IsEmpty == false)
				{
				StringBuilder queryText = new StringBuilder("SELECT ClassID, ReferenceCount FROM Classes WHERE ");
				List<object> queryParams = new List<object>();

				AppendWhereClause_ColumnIsInNumberSet("ClassID", idsToLookup, queryText, queryParams);

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
				}


			// Update the database records that need it, but just collect the IDs of the database records to be deleted for a 
			// second pass.
			
			BeginTransaction();

			try
				{
				// Reuse the NumberSet object.
				IDObjects.NumberSet idsToDelete = idsToLookup;
				idsToLookup = null;
				idsToDelete.Clear();

				using (SQLite.Query updateQuery = connection.Query("UPDATE Classes SET ReferenceCount=? WHERE ClassID=?"))
					{
					foreach (var cacheEntry in cache)
						{
						// Sanity checks
						#if DEBUG
						if (cacheEntry.DatabaseReferenceCountKnown == false && cacheEntry.ReferenceChange != 0)
							{  
							throw new Exception("ClassIDReferenceChangeCache entry " + cacheEntry.ID + 
																  " was not properly filled in before flushing.");  
							}
						if (cacheEntry.DatabaseReferenceCountKnown == true && 
							 cacheEntry.DatabaseReferenceCount + cacheEntry.ReferenceChange < 0)
							{  
							throw new Exception("ClassIDReferenceChangeCache entry " + cacheEntry.ID + 
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


				// Delete zero-reference database records.
			
				if (idsToDelete.IsEmpty == false)
					{
					StringBuilder queryText = new StringBuilder("DELETE FROM Classes WHERE ");
					List<object> queryParams = new List<object>();

					AppendWhereClause_ColumnIsInNumberSet("ClassID", idsToDelete, queryText, queryParams);

					connection.Execute(queryText.ToString(), queryParams.ToArray());
					Engine.Instance.CodeDB.UsedClassIDs.Remove(idsToDelete);
					}


				CommitTransaction();
				}
			catch
				{
				RollbackTransactionForException();
				throw;
				}

			cache.Clear();
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
		 *		PrototypeContextID - If this is zero and PrototypeContext is not null, PrototypeContextID will be looked up or created.  
		 *												  If it's already non-zero this is a no-op.
		 *		BodyContext - Can be null, which means global with no "using" statements.
		 *		BodyContextID - If this is zero and BodyContext is not null, BodyContextID will be looked up or created.  If it's already 
		 *										 non-zero this is a no-op.
		 */
		public void GetOrCreateContextIDs (Topic topic)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);


			// Cache or create any missing context IDs.  Since there's only two fields to check we'll handle this by hand to minimize 
			// memory instead of creating a HashSet or List.

			if (topic.PrototypeContextIDKnown == false)
				{
				if (topic.BodyContextIDKnown == false && topic.BodyContext != topic.PrototypeContext)
					{  CacheOrCreateContextIDs(topic.PrototypeContext, topic.BodyContext);  }
				else
					{  CacheOrCreateContextIDs(topic.PrototypeContext);  }
				}
			else if (topic.BodyContextIDKnown == false)
				{  CacheOrCreateContextIDs(topic.BodyContext);  }
			else
				{  return;  }


			// Fill in the Topic.

			if (topic.PrototypeContextIDKnown == false)
				{  topic.PrototypeContextID = contextIDLookupCache[topic.PrototypeContext];  }

			if (topic.BodyContextIDKnown == false)
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
		 *		PrototypeContextID - If this is zero and PrototypeContext is not null, PrototypeContextID will be looked up or created.  
		 *												  If it's already non-zero this is a no-op.
		 *		BodyContext - Can be null, which means global with no "using" statements.
		 *		BodyContextID - If this is zero and BodyContext is not null, BodyContextID will be looked up or created.  If it's already 
		 *										 non-zero this is a no-op.
		 */
		public void GetOrCreateContextIDs (IEnumerable<Topic> topics)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);


			// Cache or create any missing context IDs.  There may be none so create the HashSet on demand.

			HashSet<ContextString> contexts = null;

			foreach (var topic in topics)
				{
				if (topic.PrototypeContextIDKnown == false)
					{  
					if (contexts == null)
						{  contexts = new HashSet<ContextString>();  }

					contexts.Add(topic.PrototypeContext);  
					}

				if (topic.BodyContextIDKnown == false)
					{  
					if (contexts == null)
						{  contexts = new HashSet<ContextString>();  }

					contexts.Add(topic.BodyContext);  
					}
				}

			if (contexts != null)
				{  CacheOrCreateContextIDs(contexts);  }
			else
				{  return;  }


			// Fill in the Topics.

			foreach (var topic in topics)
				{
				if (topic.PrototypeContextIDKnown == false)
					{  topic.PrototypeContextID = contextIDLookupCache[topic.PrototypeContext];  }

				if (topic.BodyContextIDKnown == false)
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
		 *		ContextID - If this is zero and Context is not null, ContextID will be looked up or created.  If it's already non-zero this
		 *								is a no-op.
		 */
		public void GetOrCreateContextIDs (Link link)
			{
			RequireAtLeast(LockType.ReadPossibleWrite);

			if (link.ContextIDKnown)
				{  return;  }

			CacheOrCreateContextIDs(link.Context);

			link.ContextID = contextIDLookupCache[link.Context];
			}


		/* Function: CacheOrCreateContextIDs
		 * 
		 * Retrieves the IDs for each <ContextString> and stores them in <contextIDLookupCache>.  If they don't exist in the 
		 * database they will be created.
		 * 
		 * Requirements:
		 * 
		 *		- Requires at least a read/possible write lock.  If new contexts are created, it will be upgraded automatically.
		 */
		protected void CacheOrCreateContextIDs (IEnumerable<ContextString> contextStrings)
			{
			// Remember that contextIDLookupCache is local to the accessor and doesn't need any locking.
			// ContextIDReferenceChangeCache is part of CodeDB.Manager and requires a database lock.

			RequireAtLeast(LockType.ReadPossibleWrite);

			
			// Create a list of all contextStrings not already in the cache.  Since it's possible that they'll all be in the cache we
			// create the list object on demand.

			List<ContextString> uncachedContextStrings = null;

			foreach (var contextString in contextStrings)
				{
				if (contextString != null && contextIDLookupCache.Contains(contextString) == false)
					{
					if (uncachedContextStrings == null)
						{  uncachedContextStrings = new List<ContextString>();  }

					uncachedContextStrings.Add(contextString);  
					}
				}


			// Can we quit early?

			if (uncachedContextStrings == null)
				{  return;  }


			// Create a query to lookup the uncached contexts in the database.  The IDs may already be assigned.

			System.Text.StringBuilder queryText = new System.Text.StringBuilder("SELECT ContextID, ContextString FROM Contexts WHERE");
			string[] queryParams = new string[uncachedContextStrings.Count];

			for (int i = 0; i < uncachedContextStrings.Count; i++)
				{
				if (i != 0)
					{  queryText.Append(" OR");  }

				queryText.Append(" ContextString=?");
				queryParams[i] = uncachedContextStrings[i].ToString();
				}


			// Run the query to fill in the cache with whatever already exists in the database.

			using (SQLite.Query query = connection.Query(queryText.ToString(), queryParams))
				{
			   while (query.Step())
			      {  contextIDLookupCache.Add( ContextString.FromExportedString(query.StringColumn(1)), query.IntColumn(0) );  }
			   }


			// Pare down our list of uncached context strings.

			int j = 0;  // the compiler complains if we use i
			while (j < uncachedContextStrings.Count)
				{
				if (contextIDLookupCache.Contains(uncachedContextStrings[j]))
					{  uncachedContextStrings.RemoveAt(j);  }
				else
					{  j++;  }
				}


			// Can we quit now?

			if (uncachedContextStrings.Count == 0)
				{  return;  }


			// Create anything we still need.

			// DEPENDENCY: FlushContextIDReferenceChangeCache() assumes *every* newly created context ID will have an 
			// entry in CodeDB.ContextIDReferenceChangeCache with database references set to zero.

			RequireAtLeast(LockType.ReadWrite);
			BeginTransaction();

			try
				{
				using (SQLite.Query query = connection.Query("INSERT INTO Contexts (ContextID, ContextString, ReferenceCount) " +
																									 "VALUES (?, ?, 0)") )
					{
					var codeDB = Engine.Instance.CodeDB;

					foreach (var contextString in uncachedContextStrings)
						{
						int id = codeDB.UsedContextIDs.LowestAvailable;

						query.BindValues(id, contextString);
						query.Step();
						query.Reset(true);

						codeDB.UsedContextIDs.Add(id);
						contextIDLookupCache.Add(contextString, id);

						codeDB.ContextIDReferenceChangeCache.SetDatabaseReferenceCount(id, 0);
						}
					}

				CommitTransaction();
				}
			catch
				{
				RollbackTransactionForException();
				throw;
				}
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
			CacheOrCreateContextIDs((IEnumerable<ContextString>)contextStrings);
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

			if (cache.Count == 0)
				{  return;  }


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

				// Also see if there are changes in the cache at all, since we can avoid waiting for a read/write lock if not.  A 
				// ReferenceChange of zero still counts if DatabaseReferenceCount is also zero because that's an empty record we
				// have to remove.
				if (cacheEntry.ReferenceChange != 0 || 
					 (cacheEntry.DatabaseReferenceCountKnown && cacheEntry.DatabaseReferenceCount == 0) )
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
				}


			// Update the database records that need it, but just collect the IDs of the database records to be deleted for a 
			// second pass.
			
			BeginTransaction();

			try
				{
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


				// Delete zero-reference database records.
			
				if (idsToDelete.IsEmpty == false)
					{
					StringBuilder queryText = new StringBuilder("DELETE FROM Contexts WHERE ");
					List<object> queryParams = new List<object>();

					AppendWhereClause_ColumnIsInNumberSet("ContextID", idsToDelete, queryText, queryParams);

					connection.Execute(queryText.ToString(), queryParams.ToArray());
					Engine.Instance.CodeDB.UsedContextIDs.Remove(idsToDelete);
					}


				CommitTransaction();
				}
			catch
				{
				RollbackTransactionForException();
				throw;
				}

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