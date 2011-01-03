/* 
 * Interface: GregValure.NaturalDocs.Engine.CodeDB.IChangeWatcher
 * ____________________________________________________________________________
 * 
 * An interface for any class that wants to watch for changes in the code database.
 * 
 * Rationale:
 * 
 *		Why use lists of IChangeWatchers instead of events?  Mainly because it allows <CodeDB.Manager> to control
 *		the calling order so you can have priority watchers that get called before normal ones.
 * 
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		This interface is used for receiving notifications when the database has changed.  As such, these functions can
 *		be called from any possible thread.  Make sure any structures you interact with are thread safe.
 *		
 *		Do NOT attempt to access CodeDB except through the passed <EventAccessor>.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.CodeDB
	{
	public interface IChangeWatcher
		{
		
		/* Function: OnAddTopic
		 * Called after a topic is added to the database.
		 */
		void OnAddTopic (Topic topic, EventAccessor eventAccessor);
		
		/* Function: OnUpdateTopic
		 * Called after a topic has been update in the database.  The <Topic> object will be in the state it was before the
		 * change, and the parameters will contain the new values.
		 */
		void OnUpdateTopic (Topic oldTopic, int newCommentLineNumber, int newCodeLineNumber, string newBody, 
									 EventAccessor eventAccessor);
		
		/* Function: OnDeleteTopic
		 * Called after a topic is deleted from the database.
		 */
		void OnDeleteTopic (Topic topic, EventAccessor eventAccessor);
		
		}
	}