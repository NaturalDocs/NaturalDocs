/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.Accessor
 * ____________________________________________________________________________
 * 
 * A class threads can use to access the code database.
 * 
 * 
 * Topic: Usage
 * 
 *		- Retrieve this object from <CodeDB.Manager.GetAccessor()> or <CodeDB.Manager.GetPriorityAccessor()>.
 *		
 *		- Use the <Lock Functions> and the <Topic Functions> to manipulate the database.
 * 
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		Each thread must have its own accessor.  One cannot be used by multiple threads at the same time.
 *		
 *		Threads must acquire the appropriate locks using functions like <GetReadOnlyLock()> before calling data
 *		functions, and then release them afterwards with <ReleaseLock()>.  The only thing that's done automatically
 *		is upgrading a read/possible write lock to a read/write lock.  Otherwise, an exception will be thrown if you
 *		do not have the appropriate lock for a function.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.CodeDB
	{
	public partial class Accessor : IDisposable
		{
		
		// Group: Types
		// __________________________________________________________________________
		
		
		/* Enum: LockType
		 * 
		 * The type of lock held.  These have increasing values so you can use operators like >=.
		 * 
		 * None - No lock held.
		 * ReadOnly - A read-only lock is held which cannot be upgraded to read/write.
		 * ReadPossibleWrite - A read-only lock is held which can be upgraded to read/write.  The database is
		 *								 guaranteed not to change between when this is acquired and when an upgrade
		 *								 is successful.
		 *	ReadWrite - A read/write lock is held.
		 */
		public enum LockType : byte
			{
			None = 0,
			ReadOnly = 1,
			ReadPossibleWrite = 2,
			ReadWrite = 3
			}
			
			
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Accessor
		 */
		internal Accessor (SQLite.Connection connection, bool priority)
			{
			this.connection = connection;
			lockHeld = LockType.None;
			this.priority = priority;
			transactionLevel = 0;
			contextIDCache = new ContextIDCache();
			}
			
			
		/* Function: Dispose
		 */
		public void Dispose ()
			{
			if (connection != null)
				{
				if (transactionLevel > 0)
					{  throw new Exception ("Attempted to dispose of an Accessor while in a transaction.");  }

				if (lockHeld != LockType.None)
					{  throw new Exception ("Attempted to dispose of an Accessor while holding a lock.");  }

				connection.Dispose();
				connection = null;
				}
			}


			
		// Group: Lock Functions
		// __________________________________________________________________________
		
		
		/* Function: GetReadOnlyLock
		 * Acquires a read-only lock on the database which cannot be upgraded to a read/write lock.
		 */
		public void GetReadOnlyLock ()
			{
			if (lockHeld != LockType.None)
				{  throw new Exceptions.BadLockChange(lockHeld, LockType.ReadOnly, LockType.None);  }
				
			Engine.Instance.CodeDB.DatabaseLock.GetReadOnlyLock(priority);
			lockHeld = LockType.ReadOnly;
			}
			
			
		/* Function: GetReadPossibleWriteLock
		 * Acquires a read-only lock on the database which can be upgraded to a read/write lock.  The database is
		 * guaranteed not to change between when this lock is acquired and when the lock is successfully upgraded.
		 */
		public void GetReadPossibleWriteLock ()
			{
			if (lockHeld != LockType.None)
				{  throw new Exceptions.BadLockChange(lockHeld, LockType.ReadPossibleWrite, LockType.None);  }
				
			Engine.Instance.CodeDB.DatabaseLock.GetReadPossibleWriteLock(priority);
			lockHeld = LockType.ReadPossibleWrite;
			}
			
			
		/* Function: UpgradeToReadWriteLock
		 * Upgrades a read/possible write lock to a read/write lock.  This is safe to call multiple times, and you still only need
		 * to call <DowngradeToReadPossibleWriteLock()> or <ReleaseLock()> once.
		 */
		public void UpgradeToReadWriteLock ()
			{
			if (lockHeld == LockType.ReadWrite)
				{  return;  }
			if (lockHeld != LockType.ReadPossibleWrite)
				{  throw new Exceptions.BadLockChange(lockHeld, LockType.ReadWrite, LockType.ReadPossibleWrite);  }
				
			Engine.Instance.CodeDB.DatabaseLock.UpgradeToReadWriteLock(priority);
			lockHeld = LockType.ReadWrite;
			}
			
			
		/* Function: DowngradeToReadPossibleWriteLock
		 * Downgrades a read/write lock to a read/possible write lock so that other readers may access the database again.
		 * This is safe to call multiple times.
		 */
		public void DowngradeToReadPossibleWriteLock ()
			{
			if (lockHeld == LockType.ReadPossibleWrite)
				{  return;  }
			if (lockHeld != LockType.ReadWrite)
				{  throw new Exceptions.BadLockChange(lockHeld, LockType.ReadPossibleWrite, LockType.ReadWrite);  }
				
			Engine.Instance.CodeDB.DatabaseLock.DowngradeToReadPossibleWriteLock(priority);
			lockHeld = LockType.ReadPossibleWrite;
			}
			

		/* Function: ReleaseLock
		 * Releases any locks held, regardless of type.  Is safe to call when no locks are held.
		 */
		public void ReleaseLock ()
			{
			switch (lockHeld)
				{
				case LockType.ReadOnly:
					Engine.Instance.CodeDB.DatabaseLock.ReleaseReadOnlyLock(priority);
					break;
				case LockType.ReadPossibleWrite:
					Engine.Instance.CodeDB.DatabaseLock.ReleaseReadPossibleWriteLock(priority);
					break;
				case LockType.ReadWrite:
					Engine.Instance.CodeDB.DatabaseLock.ReleaseReadWriteLock(priority);
					break;
				}
				
			lockHeld = LockType.None;
			
			// No longer valid because another accessor may change the values while we don't hold a lock.
			contextIDCache.Clear();
			}

			
			
			
		// Group: Validation Functions
		// __________________________________________________________________________
			
			
		/* Function: RequireAtLeast
		 * Enforces a minimum lock level for an operation.  If a read/write lock is required and a read/possible write lock is held, it will be 
		 * upgraded automatically.  Otherwise if the lock held does not meet the minimum it will throw an exception.
		 */
		protected void RequireAtLeast (LockType minimum)
			{
			if (lockHeld == LockType.ReadPossibleWrite && minimum == LockType.ReadWrite)
				{  UpgradeToReadWriteLock();  }
			else if (lockHeld < minimum)
				{  throw new Exceptions.LockMinimumNotMet(lockHeld, minimum);  }
			}
			
		/* Function: RequireZero
		 * Throws an exception if the value is not zero.
		 */
		protected void RequireZero (string operation, string fieldName, int value)
			{
			if (value != 0)
				{  throw new Exceptions.FieldMustBeZero(operation, fieldName);  }
			}
			
		/* Function: RequireNonZero
		 * Throws an exception if the value is zero.
		 */
		protected void RequireNonZero (string operation, string fieldName, int value)
			{
			if (value == 0)
				{  throw new Exceptions.FieldMustBeNonZero(operation, fieldName);  }
			}
			
		/* Function: RequireNull
		 * Throws an exception if the value is not null.
		 */
		protected void RequireNull (string operation, string fieldName, string value)
			{
			if (value != null)
				{  throw new Exceptions.FieldMustBeNull(operation, fieldName);  }
			}			

		/* Function: RequireContent
		 * Throws an exception if the value is null or an empty string.
		 */
		protected void RequireContent (string operation, string fieldName, string value)
			{
			if (String.IsNullOrEmpty(value))
				{  throw new Exceptions.FieldMustHaveContent(operation, fieldName);  }
			}			



		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: LockHeld
		 * The type of lock currently held, if any.
		 */
		public LockType LockHeld
			{
			get
				{  return lockHeld;  }
			}



		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: connection
		 * This accessor's connection to the database.
		 */
		protected SQLite.Connection connection;
		
		/* var: lockHeld
		 * The lock currently held by this accessor, if any.
		 */
		protected LockType lockHeld;
		
		/* var: priority
		 * Whether this is a priority accessor or not.
		 */
		protected bool priority;
		
		/* var: transactionLevel
		 * How many nested transactions we are in, or zero if none.
		 */
		protected int transactionLevel;

		/* var: contextIDCache
		 * A cache mapping contexts to their IDs.  This is useful because contexts are going to be reused many times
		 * throughout a file and this way we don't have to hit the database for each individual Topic added.  This is only
		 * valid while we have a database lock so it's cleared automatically by <ReleaseLock()>.
		 */
		protected ContextIDCache contextIDCache;

		}
	}