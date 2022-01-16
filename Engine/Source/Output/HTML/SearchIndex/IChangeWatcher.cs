/*
 * Interface: CodeClear.NaturalDocs.Engine.Output.HTML.SearchIndex.IChangeWatcher
 * ____________________________________________________________________________
 *
 * An interface for any class that wants to watch for changes in the search index.
 *
 * Multithreading: Thread Safety Notes
 *
 *		This interface is used for receiving notifications when the search index has changed.  As such, these functions can
 *		be called from any possible thread.  Make sure any structures you interact with are thread safe.
 *
 *		Because event handlers will be called while holding locks, you should keep interaction with other modules to a
 *		minimum to prevent deadlocks.  You don't want to access another thread-safe module causing your thread to wait
 *		for it to lock while another thread is holding that lock and waiting for one held here.  The event handler should
 *		ideally just collect the prefixes for work that needs to be done and return, letting other code do the actual work
 *		later.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.SearchIndex
	{
	public interface IChangeWatcher
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: OnAddPrefix
		 * Called after a prefix is used in the index for the first time.  Note that you will hold a lock on both <CodeDB.Manager>
		 * and <SearchIndex.Manager> when this is called.
		 */
		void OnAddPrefix (string prefix, CodeDB.EventAccessor eventAccessor);

		/* Function: OnUpdatePrefix
		 * Called after a change was made that affected an existing prefix.  Note that you will hold a lock on both <CodeDB.Manager>
		 * and <SearchIndex.Manager> when this is called.
		 */
		void OnUpdatePrefix (string prefix, CodeDB.EventAccessor eventAccessor);

		/* Function: OnDeletePrefix
		 * Called before the last reference to a prefix was removed from the index.  Note that you will hold a lock on both
		 * <CodeDB.Manager> and <SearchIndex.Manager> when this is called.
		 */
		void OnDeletePrefix (string prefix, CodeDB.EventAccessor eventAccessor);

		}
	}
