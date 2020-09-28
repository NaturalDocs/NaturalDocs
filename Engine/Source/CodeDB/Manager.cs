/* 
 * Class: CodeClear.NaturalDocs.Engine.CodeDB.Manager
 * ____________________________________________________________________________
 * 
 * A class to manage information about various aspects of the code and its documentation.
 * 
 * 
 * Topic: Usage
 * 
 *		- Register any change watching objects you desire with <AddChangeWatcher()>.
 * 
 *		- Call <Engine.Instance.Start()> which will start this module.
 *		
 *		- Call <GetAccessor()> or <GetPriorityAccessor()> to create objects which will be used to manipulate the database.
 *		  Each thread must have their own.
 *		  
 *		- The change watchers will receive notifications of any modifications the accessors perform.  They can be added and
 *		  removed while the module is running.
 *		  
 *		- Each <Accessor> must be disposed before disposing of the database manager.
 *		
 *		- Disposing of the manager will automatically call <Cleanup()>, though if you have some idle time in which the 
 *		  documentation is completely updated you may call it ahead of time.
 *		  
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		> DatabaseLock -> ChangeWatchers
 * 
 *		Externally, this class is thread safe so long as each thread uses its own <Accessor>.
 *		
 *		For the <Accessor> implementation, all uses of the database connection must be managed by <DatabaseLock>.  
 *		<UsedTopicIDs> and <UsedContextIDs> are only relevant when making changes to the database, so they are 
 *		managed by <DatabaseLock> as well.
 *		
 *		The change watchers, on the other hand, have their own lock since they may be accessed independently.  You may 
 *		attempt to acquire the list with <LockChangeWatchers()> while holding <DatabaseLock>, but not vice versa.
 *		
 * 
 * Topic: Used IDs and Transactions
 * 
 *		At the moment, ID tracking number sets such as <UsedTopicIDs> don't support transactions correctly.  If you were to
 *		add a topic to the database as part of a transaction and then roll it back instead of committing it, the IDs would still
 *		be marked as used.  This has the potential to eat up all the available IDs if a database is used over a long period of time 
 *		without a full rebuild ever being performed.
 *		
 *		This is not being fixed, however, because it's assumed that rolling back transactions never happens in Natural Docs as 
 *		part of a normal path of execution.  Transactions are used mostly for performance and just as good practice in case
 *		this assumption should change in the future.  The only time it should occur is if the program crashes and it's triggered
 *		automatically.  However, in this case the database will be completely rebuilt on the next execution anyway so we don't 
 *		need to worry about it.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.CodeDB
	{
	public partial class Manager : Module, IStartupWatcher
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Manager
		 */
		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			connection = null;
			databaseLock = new Lock();

			usedTopicIDs = new IDObjects.NumberSet();
			usedLinkIDs = new IDObjects.NumberSet();
			usedImageLinkIDs = new IDObjects.NumberSet();
			usedClassIDs = new IDObjects.NumberSet();
			usedContextIDs = new IDObjects.NumberSet();

			classIDReferenceChangeCache = new ReferenceChangeCache();
			contextIDReferenceChangeCache = new ReferenceChangeCache();
			
			changeWatchers = new List<IChangeWatcher>();
			}
			
			
		/* Function: AddChangeWatcher
		 * Adds an object to be notified about changes to the database.  This can be called both before and after
		 * <Start()>.
		 */
		public void AddChangeWatcher (IChangeWatcher watcher)
			{
			lock (changeWatchers)
				{
				changeWatchers.Add(watcher);
				}
			}
			
			
		/* Function: AddPriorityChangeWatcher
		 * Adds an object to be notified about changes to the database.  Ones added with this function will receive
		 * change notifications before ones that aren't.  This can be called both before and after <Start()>.
		 */
		public void AddPriorityChangeWatcher (IChangeWatcher watcher)
			{
			lock (changeWatchers)
				{
				changeWatchers.Insert(0, watcher);
				}
			}
			
			
		/* Function: RemoveChangeWatcher
		 * Removes a watcher so that they're no longer notified of changes to the database.  It doesn't matter which
		 * function you used to add it with.  This can be called both before and after <Start()>.
		 */
		public void RemoveChangeWatcher (IChangeWatcher watcher)
			{
			lock (changeWatchers)
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
			}
			
			
		/* Function: Start
		 * 
		 * Dependencies:
		 * 
		 *		- <Config.Manager> must be started before using the rest of the class.
		 */
		public bool Start (Errors.ErrorList errors)
			{
			EngineInstance.AddStartupWatcher(this);

			SQLite.API.Result sqliteResult = SQLite.API.Initialize();
			
			if (sqliteResult != SQLite.API.Result.OK)
			    {  throw new SQLite.Exceptions.UnexpectedResult("Could not initialize SQLite.", sqliteResult);  }

			Path databaseFile = EngineInstance.Config.WorkingDataFolder + "/CodeDB.nd";
			connection = new SQLite.Connection();
			bool success = false;
			
			if (!EngineInstance.HasIssues( StartupIssues.NeedToStartFresh |
														 StartupIssues.FileIDsInvalidated |
														 StartupIssues.CodeIDsInvalidated |
														 StartupIssues.CommentIDsInvalidated ))
				{
				try
					{
					connection.Open(databaseFile, false);
					
					Version version = GetVersion();
					
					if (BinaryFile.IsCompatible(version, Engine.Instance.Version, "2.0.2") == true)
						{  
						LoadSystemVariables();
						success = true;
						}
					}
				catch { }
				}
			
			if (!success)
				{
				connection.Dispose();
				
				if (System.IO.File.Exists(databaseFile))
					{  System.IO.File.Delete(databaseFile);  }
					
				connection.Open(databaseFile, true);
				CreateDatabase();

				EngineInstance.AddStartupIssues( StartupIssues.CodeIDsInvalidated |
																   StartupIssues.CommentIDsInvalidated |
																   StartupIssues.NeedToReparseAllFiles,
																   dontNotify: this);
				}
				
			return true;
			}
			
			
		/* Function: GetAccessor
		 * Creates an <Accessor> for manipulating the database.  Each thread must have its own.
		 */
		public Accessor GetAccessor ()
			{
			return new Accessor(this, connection.CreateAnotherConnection(), false);
			}
			
			
		/* Function: GetPriorityAccessor
		 * Creates an <Accessor> for manipulating the database which takes priority over other Accessors whenever possible.  This
		 * is useful for interface related threads that should have greater priority than background workers.
		 */
		public Accessor GetPriorityAccessor ()
			{
			return new Accessor(this, connection.CreateAnotherConnection(), true);
			}


		/* Function: Dispose
		 */
		protected override void Dispose (bool strictRulesApply)
			{
			// If strict rules apply then the connection object will have to dispose of itself.  We can't do it here.
			if (!strictRulesApply && connection != null && connection.IsOpen)
				{
				if (databaseLock.IsLocked)
					{  throw new Exception("Attempted to dispose of database when there were still locks held.");  }
				
				Cleanup(Delegates.NeverCancel);
				SaveSystemVariablesAndVersion();
					
				connection.Dispose();
				connection = null;

				usedTopicIDs.Clear();
				usedLinkIDs.Clear();
				usedImageLinkIDs.Clear();
				usedClassIDs.Clear();
				usedContextIDs.Clear();

				classIDReferenceChangeCache.Clear();
				contextIDReferenceChangeCache.Clear();
				
				SQLite.API.Result shutdownResult = SQLite.API.ShutDown();

				if (shutdownResult != SQLite.API.Result.OK)
					{  throw new SQLite.Exceptions.UnexpectedResult("Could not shut down SQLite.", shutdownResult);  }
				}
			}
			
			
		/* Function: Cleanup
		 * 
		 * Cleans up any stray data associated with the database, assuming all documentation is up to date.  You can pass a
		 * <CancelDelegate> if you'd like to interrupt this process early.
		 * 
		 * <Dispose()> will call this function automatically so it's not strictly necessary to call it manually, though it's good
		 * practice to.  If you have idle time in which the documentation is completely up to date, calling this then instead of
		 * leaving it for <Dispose()> will allow the engine to shut down faster.
		 */
		public void Cleanup (CancelDelegate cancelDelegate)
			{
			using (Accessor accessor = GetAccessor())
				{
				accessor.GetReadPossibleWriteLock();
				accessor.FlushClassIDReferenceChangeCache(cancelDelegate);
				accessor.FlushContextIDReferenceChangeCache(cancelDelegate);
				accessor.ReleaseLock();
				}
			}


		/* Function: OnStartupIssues
		 * Called whenever new startup issues occur.  Includes both what's new for this call and the total for the engine initialization
		 * thus far.  Multiple new issues can be combined into a single notification, but you will only be notified of each new issue once.
		 */
		public void OnStartupIssues (StartupIssues newIssues, StartupIssues allIssues)
			{
			if ( (newIssues & ( StartupIssues.NeedToStartFresh |
										StartupIssues.CodeIDsInvalidated |
										StartupIssues.CommentIDsInvalidated |
										StartupIssues.FileIDsInvalidated )) != 0)
				{
				ResetDatabase();
				EngineInstance.AddStartupIssues( StartupIssues.NeedToReparseAllFiles, 
																   dontNotify: this );
				}
			}

		public void OnStartPossiblyLongOperation (string operationName)
			{  }
		
		public void OnEndPossiblyLongOperation ()
			{  }


			
		// Group: Accessor Properties
		// These properties are internal and are only meant for use by <Accessor>.
		// __________________________________________________________________________
	
		
		/* Property: DatabaseLock
		 * The <CodeDB.Lock> used to manage access to this database.  It covers properties like <UsedTopicIDs> in addition
		 * to the SQLite database itself.
		 */
		internal Lock DatabaseLock
			{
			get
				{  return databaseLock;  }
			}
			
		/* Property: UsedTopicIDs
		 * An <IDObjects.NumberSet> of all the used topic IDs in <CodeDB.Topics>.  Its use is governed by <DatabaseLock>.
		 */
		internal IDObjects.NumberSet UsedTopicIDs
			{
			get
				{  return usedTopicIDs;  }
			}
			
		/* Property: UsedLinkIDs
		 * An <IDObjects.NumberSet> of all the used link IDs in <CodeDB.Links>.  Its use is governed by <DatabaseLock>.
		 */
		internal IDObjects.NumberSet UsedLinkIDs
			{
			get
				{  return usedLinkIDs;  }
			}
			
		/* Property: UsedImageLinkIDs
		 * An <IDObjects.NumberSet> of all the used image link IDs in <CodeDB.ImageLinks>.  Its use is governed by <DatabaseLock>.
		 */
		internal IDObjects.NumberSet UsedImageLinkIDs
			{
			get
				{  return usedImageLinkIDs;  }
			}
			
		/* Property: UsedClassIDs
		 * An <IDObjects.NumberSet> of all the used class IDs in <CodeDB.Classes>.  Its use is governed by <DatabaseLock>.
		 */
		internal IDObjects.NumberSet UsedClassIDs
			{
			get
				{  return usedClassIDs;  }
			}

		/* Property: UsedContextIDs
		 * An <IDObjects.NumberSet> of all the used context IDs in <CodeDB.Contexts>.  Its use is governed by <DatabaseLock>.
		 */
		internal IDObjects.NumberSet UsedContextIDs
			{
			get
				{  return usedContextIDs;  }
			}

		/* Property: ClassIDReferenceChangeCache
		 * A cache of all the reference count changes to <CodeDB.Classes>.  Its use is governed by <DatabaseLock>.
		 */
		internal ReferenceChangeCache ClassIDReferenceChangeCache
			{
			get
				{  return classIDReferenceChangeCache;  }
			}

		/* Property: ContextIDReferenceChangeCache
		 * A cache of all the reference count changes to <CodeDB.Contexts>.  Its use is governed by <DatabaseLock>.
		 */
		internal ReferenceChangeCache ContextIDReferenceChangeCache
			{
			get
				{  return contextIDReferenceChangeCache;  }
			}
			
			
			
		// Group: Accessor Functions
		// These functions are internal and are only meant for use by <Accessor>.
		// __________________________________________________________________________
			
			
		/* Function: LockChangeWatchers
		 * Gets the list of objects watching the database for changes, which requires a lock.  The list will never be null.  You can 
		 * attempt to get this lock while holding <DatabaseLock>, but never the other way around.  Release it with
		 * <ReleaseChangeWatchers()>, after which the object can no longer be used in a thread safe manner.
		 */
		internal IList<IChangeWatcher> LockChangeWatchers ()
			{
			System.Threading.Monitor.Enter(changeWatchers);
			
			// The list can only be changed by the functions directly in the module.
			return changeWatchers.AsReadOnly();
			}
			
			
		/* Function: ReleaseChangeWatchers
		 * Releases the lock on the list obtained with <LockChangeWatchers()>.
		 */
		internal void ReleaseChangeWatchers ()
			{
			System.Threading.Monitor.Exit(changeWatchers);
			}
			
			
						
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: connection
		 */
		protected SQLite.Connection connection;
		
		/* var: databaseLock
		 */
		protected Lock databaseLock;
		
		/* var: usedTopicIDs
		 */
		protected IDObjects.NumberSet usedTopicIDs;

		/* var: usedLinkIDs
		 */
		protected IDObjects.NumberSet usedLinkIDs;

		/* var: usedImageLinkIDs
		 */
		protected IDObjects.NumberSet usedImageLinkIDs;

		/* var: usedClassIDs
		 */
		protected IDObjects.NumberSet usedClassIDs;

		/* var: usedContextIDs
		 */
		protected IDObjects.NumberSet usedContextIDs;

		/* var: classIDReferenceChangeCache
		 * A cache of all the reference count changes to be applied to <CodeDB.Classes>.
		 */
		protected ReferenceChangeCache classIDReferenceChangeCache;
		
		/* var: contextIDReferenceChangeCache
		 * A cache of all the reference count changes to be applied to <CodeDB.Contexts>.
		 */
		protected ReferenceChangeCache contextIDReferenceChangeCache;
		
		/* var: changeWatchers
		 * A list of objects that are watching the database for changes.  If there are none, the list will be empty
		 * rather than null.
		 */
		protected List<IChangeWatcher> changeWatchers;
		
		}
	}