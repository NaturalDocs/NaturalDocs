/* 
 * Class: GregValure.NaturalDocs.Engine.Thread
 * ____________________________________________________________________________
 * 
 * A base class for all engine threads.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
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
		public Thread ()
			{
			thread = new System.Threading.Thread ( this.InternalStart );
			task = null;
			cancelDelegate = Delegates.NeverCancel;
			exception = null;
			}


		/* Function: Start
		 * Starts the thread, just like System.Threading.Thread.Start().  If the thread was previously cancelled, it clears
		 * the cancelled flag and restarts it.  *Do not override this function.*  You need to override <Run()> instead.
		 */
		public void Start ()
			{
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


		/* Function: ThrowExceptions
		 * If the thread terminated with an exception, throw it.  Otherwise it does nothing.  This should only be 
		 * called by the parent thread.
		 */
		public void ThrowExceptions ()
			{
			if (exception != null)
				{  throw exception;  }
			}



		// Group: Properties
		// __________________________________________________________________________

			
		/* Property: Task
		 */
		public CancellableTask Task
			{
			get
				{  return task;  }
			set
				{  task = value;  }
			}


		/* Property: CancelDelegate
		 * The function that determines whether the thread should be cancelled.  The <Task> will call this periodically to see if it
		 * should stop, and if so will do so gracefully.  As such it does not immediately halt the thread's execution and depends on
		 * the <Task> honoring it.
		 */
		public CancelDelegate CancelDelegate
			{
			get
				{  return cancelDelegate;  }
			set
				{  cancelDelegate = value;  }
			}
			
			
		/* Property: Name
		 * The thread name.  Can only be set by the constructor.
		 */
		public string Name
			{
			get
				{  return thread.Name;  }
			set
				{  thread.Name = value;  }
			}


		/* Property: Priority
		 */
		public ThreadPriority Priority
			{
			get
				{  return thread.Priority;  }
			set
				{  thread.Priority = value;  }
			}


		/* Property: Exception
		 * The exception the thread terminated on, or null if none.
		 */
		public Engine.Exceptions.Thread Exception
			{
			get
				{  return exception;  }
			}
			
			
			

		// Group: Private Functions
		// __________________________________________________________________________
		
		
		/* Function: InternalStart
		 * The function that's mapped as the starting function for <thread>.  This executes <task> and traps exceptions.
		 */
		private void InternalStart ()
			{
			exception = null;

			try
				{
				if (task != null)
					{  task(cancelDelegate);  }
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
			
			

		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: thread
		 * The thread this class is wrapping.
		 */
		protected System.Threading.Thread thread;

		/* var: task
		 * The task being run by the thread.
		 */
		protected CancellableTask task;

		/* var: cancelDelegate
		 * The delegate that determines whether the task should be cancelled.
		 */
		protected CancelDelegate cancelDelegate;
	
		/* var: exception
		 * If the thread terminated because of an exception it will be stored here.
		 */
		protected Engine.Exceptions.Thread exception;

		}
	}