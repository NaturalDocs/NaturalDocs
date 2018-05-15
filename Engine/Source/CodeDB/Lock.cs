/* 
 * Class: CodeClear.NaturalDocs.Engine.CodeDB.Lock
 * ____________________________________________________________________________
 * 
 * A struct to encapsulate the CodeDB database lock.
 * 
 * The lock behaves somewhat similarly to a ReaderWriterLock, except that the two types of locks initially acquired 
 * are ReadOnly and ReadPossibleWrite.  It is assumed that any thread that may need to write to the database will
 * also need to read from it first.  As such, it can share the database with other readers up until it actually starts
 * writing.  Also, it may end up not needing to write at all.  The only critical point is that nothing else can change
 * the database while it has a ReadPossibleWrite lock so the data that was read is still valid by the time an actual
 * write lock is acquired.
 * 
 * The class also implements priority versions of both those locks.  This allows database access related to the 
 * interface to jump ahead of any background threads that may also be using it.
 * 
 * IMPORTANT: This class assumes the code using it knows what it's doing.  It does not check to see if you're
 * releasing a lock you had, releasing the correct number of times, releasing the correct type of lock, etc.  This
 * logic is meant to be encapsulated in another class, so this one omits it for efficiency.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Threading;


namespace CodeClear.NaturalDocs.Engine.CodeDB
	{
	public class Lock
		{
		
		public Lock ()
			{
			activeLocks = 0;
			waitingPriorityLocks = 0;
			state = State.Unlocked;
			monitor = new object();
			}
			
			
		/* Function: GetReadOnlyLock
		 * Acquires a lock for reading.  This lock can NOT be upgraded to a write lock.  If you need the possibility of writing 
		 * as well you must use <GetReadPossibleWriteLock()> instead.
		 */
		public void GetReadOnlyLock (bool priority)
			{
			Monitor.Enter(monitor);

			try
				{
				if (!priority)
					{
					while (waitingPriorityLocks > 0 ||
							 state > State.ReadOnlyAndReadPossibleWrite)
						{  Monitor.Wait(monitor);  }
					}
				else // priority
					{
					waitingPriorityLocks++;
					
					while (state > State.ReadOnlyAndReadPossibleWrite)
						{  Monitor.Wait(monitor);  }
						
					waitingPriorityLocks--;
					
					if (waitingPriorityLocks == 0)
						{  Monitor.PulseAll(monitor);  }
					}
					
				activeLocks++;
				
				if (state == State.Unlocked)
					{  state = State.ReadOnly;  } 
				}
				
			finally
				{  Monitor.Exit(monitor);  }
			}
			
			
		/* Function: GetReadPossibleWriteLock
		 * Acquires a lock for reading with the possibility of upgrading it to a read/write lock in the future.  The database state
		 * cannot change between when this lock is acquired and when it is upgraded.
		 */
		public void GetReadPossibleWriteLock (bool priority)
			{
			Monitor.Enter(monitor);
			
			try
				{
				if (!priority)
					{
					while (waitingPriorityLocks > 0 ||
							 state > State.ReadOnly)
						{  Monitor.Wait(monitor);  }
					}
				else // priority
					{
					waitingPriorityLocks++;
					
					while (state > State.ReadOnly)
						{  Monitor.Wait(monitor);  }
						
					waitingPriorityLocks--;
					
					if (waitingPriorityLocks == 0)
						{  Monitor.PulseAll(monitor);  }
					}
					
				activeLocks++;
				state = State.ReadOnlyAndReadPossibleWrite;
				}
				
			finally
				{  Monitor.Exit(monitor);  }
			}
			
			
		/* Function: UpgradeToReadWriteLock
		 * Upgrades a read/possible write lock to a read/write lock.  You can NOT use this function if you originally acquired a
		 * read-only lock.  The database state is guaranteed not to change between when the read/possible write lock was
		 * acquired and when this function returns.
		 */
		public void UpgradeToReadWriteLock (bool priority)
			{
			Monitor.Enter(monitor);

			try
				{
				state = State.WaitingForReadWrite;

				while (activeLocks > 1)
					{  Monitor.Wait(monitor);  }
					
				state = State.ReadWrite;
				}
				
			finally
				{  Monitor.Exit(monitor);  }
			}
			
		
		/* Function: DowngradeToReadPossibleWriteLock
		 * Downgrades a read/write lock to a read/possible write lock.  This allows other threads to read from the database
		 * again, but still doesn't allow anything that could change its state because this lock may need to be upgraded to
		 * a reader/writer lock again.
		 */
		public void DowngradeToReadPossibleWriteLock (bool priority)
			{
			Monitor.Enter(monitor);

			try
				{
				state = State.ReadOnlyAndReadPossibleWrite;
				Monitor.PulseAll(monitor);
				}
			
			finally
				{  Monitor.Exit(monitor);  }
			}
			
			
		/* Function: ReleaseReadOnlyLock
		 * Releases a read-only lock.
		 */
		public void ReleaseReadOnlyLock (bool priority)
			{
			Monitor.Enter(monitor);

			try
				{
				activeLocks--;
				
				if (activeLocks == 0)
					{  state = State.Unlocked;  }
				else if (activeLocks == 1 && state == State.WaitingForReadWrite)
					{  Monitor.PulseAll(monitor);  }
				}
				
			finally
				{  Monitor.Exit(monitor);  }
			}
			
			
		/* Function: ReleaseReadPossibleWriteLock
		 * Releases a read/possible write lock that is not upgraded to a read/write lock.  If you upgraded it and didn't downgrade
		 * it back, use <ReleaseReadWriteLock()> instead.
		 */
		public void ReleaseReadPossibleWriteLock (bool priority)
			{
			Monitor.Enter(monitor);

			try
				{
				activeLocks--;
				
				if (activeLocks == 0)
					{  
					state = State.Unlocked;
					Monitor.PulseAll(monitor);
					}
				else
					{  
					state = State.ReadOnly;  
					Monitor.PulseAll(monitor);
					}
				}
				
			finally
				{  Monitor.Exit(monitor);  }
			}
			
			
		/* Function: ReleaseReadWriteLock
		 * Releases a read/write lock.
		 */
		public void ReleaseReadWriteLock (bool priority)
			{
			Monitor.Enter(monitor);

			try
				{
				activeLocks = 0;
				state = State.Unlocked;
				Monitor.PulseAll(monitor);
				}
				
			finally
				{  Monitor.Exit(monitor);  }
			}
			
			
			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: IsLocked
		 * 
		 * Whether there are any locks currently held.
		 * 
		 * This is for information only.  It is NOT thread safe to use this to determine whether or not to try to acquire a lock,
		 * as that would be a race condition.  A different thread could acquire a lock between when you checked this value
		 * and when you attempted to acquire it yourself.
		 */
		public bool IsLocked
			{
			get
				{  return (activeLocks > 0);  }
			}
			
			
			
		// Group: Types
		// __________________________________________________________________________
		
		
		/* Enum: State
		 * 
		 * The state of the lock.  Each value is higher than the previous so you can use operators like <=.
		 * 
		 * Unlocked - There are no active locks.
		 * ReadOnly - There are one or more ReadOnly locks active.
		 * ReadOnlyAndReadPossibleWrite - There is one ReadPossibleWrite lock active and zero or more ReadOnly locks as well.
		 * WaitingForReadWrite - There is one ReadPossibleWrite lock active that wants to upgrade to a ReadWrite lock, but
		 *								   there are also one or more ReadOnly locks preventing it.
		 *	ReadWrite - There is a single ReadWrite lock active.
		 */
		protected enum State : byte
			{
			Unlocked = 0,
			ReadOnly = 1,
			ReadOnlyAndReadPossibleWrite = 2,
			WaitingForReadWrite = 3,
			ReadWrite = 4
			}
			
			
			
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: activeLocks
		 * The number of active locks, regardless of type.
		 */
		protected int activeLocks;
		
		/* var: waitingPriorityLocks
		 * The number of priority locks in the waiting state.
		 */
		protected int waitingPriorityLocks;
		
		/* var: state
		 * The <State> of the lock.
		 */
		protected State state;

		/* var: monitor
		 * The locking object used with the monitor functions.
		 */
		protected object monitor;
		
		}
	}