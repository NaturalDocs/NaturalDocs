/* 
 * Class: GregValure.NaturalDocs.Engine.Thread
 * ____________________________________________________________________________
 * 
 * A base class for all engine threads.
 * 
 * 
 * Topic: Usage
 * 
 *		- Derive a class from this one and override <Run()> to do what you want.
 *		
 *		- The code in <Run()> should periodically check <Cancelled> and abort what it's doing if set.
 *		
 *		- If the code in <Run()> needs to sleep on a synchronization object, it should also sleep on <WhenCancelled>
 *		  so that it will wake up if it's cancelled.
 *		
 *		- External code should create the object from a derived class, call <Start()> to begin, and <Join()> to wait
 *		  for it to end.  The external code should call <ThrowExceptions()> after the thread has ended so that any
 *		  exceptions it causes get passed on to the parent thread.
 *		  
 *		- The parent code can call <SendCancelMessage()> to cause it to stop early, provided the worker code
 *		  checks it.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Threading;


namespace GregValure.NaturalDocs.Engine
	{
	public class Thread
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Thread
		 */
		public Thread (string threadName, ThreadPriority priority, bool isBackground)
			{
			cancelled = false;
			WhenCancelled = new System.Threading.ManualResetEvent(false);
			exception = null;
			
			thread = new System.Threading.Thread ( this.InternalStart );
			thread.Name = threadName;
			thread.Priority = priority;
			thread.IsBackground = isBackground;
			}
			
			
		/* Function: Start
		 * Starts the thread, just like System.Threading.Thread.Start().  If the thread was previously cancelled, it clears
		 * the cancelled flag and restarts it.  *Do not override this function.*  You need to override <Run()> instead.
		 */
		public void Start ()
			{
			cancelled = false;
			WhenCancelled.Reset();
			exception = null;
			
			thread.Start();
			}


		/* Function: Join
		 * The calling thread waits for this thread to join, just like System.Threading.Join().  Is safe to call if the thread
		 * was never started.
		 */
		public void Join ()
			{
			if (thread.IsAlive)
				{  thread.Join();  }
			}


		/* Function: SendCancelMessage
		 * 
		 * Signals that the thread's operation should be cancelled.  This does not result in the immediate suspension
		 * of the thread.  Rather, it sets <Cancelled> so that the next time the thread checks it, it will stop.  This 
		 * allows a more graceful shutdown but risks the thread continuing for a significant period of time if the 
		 * worker code does not check the flag often enough.
		 * 
		 * Why a separate function instead of making <Cancelled> writable?  To make it much more clear in the 
		 * calling code that it does not result in an immediate cancellation.
		 */
		public void SendCancelMessage ()
			{
			cancelled = true;
			WhenCancelled.Set();
			}
			
			
		/* Property: Cancelled
		 * Whether a cancel message has been sent to this thread.  Does *not* return whether the thread is still
		 * running.
		 */
		public bool Cancelled
			{
			get
				{  return cancelled;  }
			}
			
			
		/* Property: Name
		 * The thread name.  Can only be set by the constructor.
		 */
		public string Name
			{
			get
				{  return thread.Name;  }
			}
			
			
		/* Property: Exception
		 * The exception the thread terminated on, or null if none.
		 */
		public Engine.Exceptions.Thread Exception
			{
			get
				{  return exception;  }
			}
			
			
		/* Property: ThrowExceptions
		 * If the thread terminated with an exception, throw it.  Otherwise it does nothing.  This should only be 
		 * called by the parent thread.
		 */
		public void ThrowExceptions ()
			{
			if (exception != null)
				{  throw exception;  }
			}
		
	
				
			

		// Group: Protected Functions
		// __________________________________________________________________________
		
		
		/* Function: InternalStart
		 * The function that's mapped as the starting function for <thread>.  This wraps <Run()> to provide
		 * exception handling.
		 */
		protected void InternalStart ()
			{
			try
				{
				Run();
				}
			catch (System.Exception e)
				{
				// Wrap it only if we need to.
				if (e is Engine.Exceptions.Thread)
					{  exception = (Engine.Exceptions.Thread)e;  }
				else
					{  exception = new Engine.Exceptions.Thread (e, this);  }
				}
			}
			
			
		/* Function: Run
		 * The virtual function you should override to do whatever you need the thread to do.
		 */
		protected virtual void Run ()
			{
			}
			
			
			
			
		// Group: Thread Synchronization Objects
		// __________________________________________________________________________
			
			
		/* var: WhenCancelled
		 * 
		 * A synchronization object that triggers when the thread is cancelled.  Sleep on this in addition to external 
		 * synchronization objects so the thread will wake up if it's cancelled while asleep.
		 * 
		 * This is protected because external code should not set it directly.  Rather, it should call <SendCancelMessage()>
		 * which will set this as well as <Cancelled>.
		 */
		protected System.Threading.ManualResetEvent WhenCancelled;
			
			
			
			
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* object: thread
		 * The thread this class is wrapping.
		 */
		protected System.Threading.Thread thread;
	
		
		/* bool: cancelled
		 * The flag that specifies if the thread's operation is to be cancelled.  This allows the thread to check its status quickly
		 * during long operations that may need to be cancelled while in progress.
		 */
		protected volatile bool cancelled;
		
		
		/* object: exception
		 * If the thread terminated because of an exception it will be stored here.
		 */
		protected Engine.Exceptions.Thread exception;
		}
	}