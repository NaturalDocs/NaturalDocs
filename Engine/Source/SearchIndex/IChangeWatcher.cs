/* 
 * Interface: GregValure.NaturalDocs.Engine.SearchIndex.IChangeWatcher
 * ____________________________________________________________________________
 * 
 * An interface for any class that wants to watch for changes in the search index.
 * 
 * 
 * Rationale:
 * 
 *		Why use lists of IChangeWatchers instead of events?  Mainly because it allows <SearchIndex.Manager> to control
 *		the calling order so you can have priority watchers that get called before normal ones.
 *		
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		This interface is used for receiving notifications when the search index has changed.  As such, these functions can
 *		be called from any possible thread.  Make sure any structures you interact with are thread safe.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.SearchIndex
	{
	public interface IChangeWatcher
		{
		
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