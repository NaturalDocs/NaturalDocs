/* 
 * Class: CodeClear.NaturalDocs.CLI.StatusManager
 * ____________________________________________________________________________
 * 
 * A base class for all status managers, which handle posting status messages for long tasks.  They can post
 * beginning and/or ending messages, as well as updates on a set interval if the task takes long enough to require
 * it.
 * 
 * Deriving a Status Manager:
 * 
 *		- Create a class with this as a parent.
 *		
 *		- If you're not using hideIfShorterThan you can override <ShowStartMessage()> to capture the initial state of 
 *		  whatever you're tracking and also show the start message.
 *		  
 *		- If you are using hideIfShorterThan, override <Start()> to capture the initial state and override <ShowStartMessage()>
 *		  to show the starting message, as the call to <ShowStartMessage()> will be delayed.  Remember to call the base
 *		  class's <Start()> from your own.
 *		  
 *		- <ShowUpdateMessage()> can be overridden to check the state of whatever you're tracking, compare it against 
 *		  its local copy, and post a status message if it's different.  Since this will be called by a timer, realize that it will be 
 *		  executed in a different thread.
 *		  
 *		- <ShowEndMessage()> can be overridden to post an ending message.
 *		
 * Using a Status Manager:
 * 
 *		- Create the object.
 *		
 *		- Start the task you want to track and then call <Start()>.  It's only possible to start the status manager
 *		  first if it's capable of fully reading the task's state before it starts.
 *		  
 *		- <Update()> will be called automatically by an internal timer.  You don't have to worry about it.
 *		
 *		- When the task finishes call <End()>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.CLI
	{
	public class StatusManager : IDisposable
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: StatusManager
		 * 
		 * Creates a new status manager.
		 * 
		 * Parameters:
		 * 
		 *		updateInterval - The number of milliseconds between status updates if the task takes a long time.  If this
		 *									  is zero, only start and end messages will be displayed.
		 *		hideIfShorterThan - The number of milliseconds where if the task starts and finishes within this time, no
		 *											status is displayed at all.  Zero means always show a status.
		 */
		public StatusManager (int updateInterval, int hideIfShorterThan = 0)
			{
			this.updateInterval = updateInterval;
			this.hideIfShorterThan = hideIfShorterThan;

			if (updateInterval > 0 || hideIfShorterThan > 0)
				{
				timer = new System.Timers.Timer();
				timer.Elapsed += new System.Timers.ElapsedEventHandler(Update);
				timer.AutoReset = false;

				if (hideIfShorterThan > 0)
					{  timer.Interval = hideIfShorterThan;  }
				else // updateInterval > 0
					{  timer.Interval = updateInterval;  }
				}
			else
				{  timer = null;  }
			}


		/* Function: Start
		 * Starts monitoring the task.  If you override this function make sure to call the base class's version.
		 */
		virtual public void Start()
			{
			if (hideIfShorterThan == 0)
				{  ShowStartMessage();  }

			if (timer != null)
				{  timer.Start();  }
			}
		

		/* Function: Update
		 * Called periodically to update the status message.  This is handled automatically, you don't need to manually
		 * call it.
		 */
		protected void Update(Object sender, System.Timers.ElapsedEventArgs args)
			{
			if (hideIfShorterThan > 0)
				{
				ShowStartMessage();
				hideIfShorterThan = 0;

				if (updateInterval > 0)
					{
					timer.Interval = updateInterval;
					timer.Start();
					}
				}
			else
				{
				ShowUpdateMessage();
				timer.Start();
				}
			}
			
		
		/* Function: End
		 * Ends monitoring.
		 */
		public void End()
			{
			if (timer != null)
				{  timer.Stop();  }

			if (hideIfShorterThan == 0)
				{  ShowEndMessage();  }
			}
			
			
		/* Function: Dispose
		 */
		public void Dispose ()
			{
			if (timer != null)
				{
				timer.Stop();
				timer.Dispose();
				timer = null;
				}
			}
			

		/* Function: ShowStartMessage
		 * Override this function to display a message at the start of a task.  If hideIfShorterThan was specified, this will
		 * only be called if the task runs too long.  Otherwise it will always be called.
		 */
		protected virtual void ShowStartMessage ()
			{
			}


		/* Function: ShowUpdateMessage
		 * Override this function to display a progress message if the task runs too long.  This can be called many times or
		 * not at all.
		 */
		protected virtual void ShowUpdateMessage ()
			{
			}


		/* Function: ShowEndMessage
		 * Override this function to display a message at the end of a task.  If hideIfShorterThan was specified, this will only
		 * be called if the task ran too long.  Otherwise it will always be called.
		 */
		protected virtual void ShowEndMessage ()
			{
			}

		
		
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: timer
		 * The timer used to call <Update()>.
		 */
		protected System.Timers.Timer timer;

		/* var: updateInterval
		 * The number of milliseconds between status updates if the task takes a long time.  If this is zero, only
		 * start and end messages will be displayed.
		 */
		protected int updateInterval;

		/* var: hideIfShorterThan
		 * The number of milliseconds where if the task starts and completes within this interval, no status is
		 * displayed at all.  This will also be set to zero if it was specified and the task ran long.
		 */
		protected int hideIfShorterThan;
		
		}
	}