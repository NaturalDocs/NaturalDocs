/* 
 * Class: GregValure.NaturalDocs.CLI.StatusManager
 * ____________________________________________________________________________
 * 
 * A base class for all status managers, which handle posting status messages for long tasks.  They can post
 * beginning and/or ending messages, as well as updates on a set interval if the task takes long enough to require
 * it.
 * 
 * Deriving a Status Manager:
 * 
 *		- Create a class with this as a parent.
 *		- <Start()> should be overridden to capture the state of whatever you're tracking and post a starting
 *		  message if necessary.  It should always call the base class's Start() as well.
 *		- <End()> should be overridden to post an ending message if necessary.  It should always call the base
 *		  class's End() as well.
 *		- <Update()> should be overridden to check the state of whatever you're tracking, compare it against
 *		  its local copy, and post a status message if it's different.  Since this will be called by a timer, realize
 *		  that it will be executed in a different thread.
 *		  
 * Using a Status Manager:
 * 
 *		- Create the object.
 *		- Start the task you want to track and then call <Start()>.  It's only possible to start the status manager
 *		  first if it's capable of fully reading the task's state before it starts.
 *		- <Update()> will be called automatically by an internal timer.  You don't have to worry about it.
 *		- When the task finishes call <End()>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.CLI
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
		 *		updateInterval - The number of milliseconds between status updates if the task takes a long time.
		 */
		public StatusManager (int updateInterval)
			{
			timer = new System.Timers.Timer();
			timer.Enabled = false;
			timer.AutoReset = true;
			timer.Interval = updateInterval;
			timer.Elapsed += new System.Timers.ElapsedEventHandler(Update);
			}


		/* Function: Start
		 * 
		 * Displays the initial status message, if any, and starts monitoring the task.
		 * 
		 * If you override this function, make sure the new version calls the base class's version.
		 */
		public virtual void Start()
			{
			timer.Start();
			}
		

		/* Function: Update
		 * 
		 * Called periodically to update the status message.  This is handled automatically, you don't need to manually
		 * call it.
		 * 
		 * If you override this function, be aware that it executes in a separate system thread used by the timer.
		 */
		protected virtual void Update(Object sender, System.Timers.ElapsedEventArgs args)
			{
			}
			
		
		/* Function: End
		 * 
		 * Ends monitoring and displays a final status message if appropriate.
		 * 
		 * If you override this function, make sure the new version calls the base class's version.
		 */
		public virtual void End()
			{
			timer.Stop();
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
			
		
		
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: timer
		 * The timer used to call <Update()>.
		 */
		protected System.Timers.Timer timer;
		
		}
	}