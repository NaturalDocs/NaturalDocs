/*
 * Interface: CodeClear.NaturalDocs.Engine.CodeDB.IChangeWatcher
 * ____________________________________________________________________________
 *
 * An interface for any class that wants to watch for changes in the code database.
 *
 * Multithreading: Thread Safety Notes
 *
 *		This interface is used for receiving notifications when the database has changed.  As such, these functions can
 *		be called from any possible thread.  Make sure any structures you interact with are thread safe.
 *
 *		Because event handlers will be called while holding a database lock, you should keep interaction with other modules
 *		to a minimum to prevent deadlocks.  You don't want to access another thread-safe module causing your thread to
 *		wait for it to lock while another thread is holding that lock and waiting to access the database.  The event handler
 *		should ideally just collect the topic and class IDs for work that needs to be done and return, letting other code do
 *		the actual work later.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.CodeDB
	{
	public interface IChangeWatcher
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: OnAddTopic
		 * Called after a topic is added to the database.
		 */
		void OnAddTopic (Topic topic, EventAccessor eventAccessor);

		/* Function: OnUpdateTopic
		 * Called after a topic has been updated in the database.
		 */
		void OnUpdateTopic (Topic oldTopic, Topic newTopic, Topic.ChangeFlags changeFlags, EventAccessor eventAccessor);

		/* Function: OnDeleteTopic
		 * Called before a topic is deleted from the database.  It will include the IDs of the links that previously resolved to this
		 * topic, but you can wait for <OnChangeLinkTarget()> to handle that.
		 */
		void OnDeleteTopic (Topic topic, IDObjects.NumberSet linksAffected, EventAccessor eventAccessor);

		/* Function: OnAddLink
		 * Called after a link is added to the database.
		 */
		void OnAddLink (Link link, EventAccessor eventAccessor);

		/* Function: OnChangeLinkTarget
		 * Called after a link's target has been changed in the database.  Note that this will also be called for new links, as they
		 * are added to the database as unresolved during the parsing stage, and then changed to their targets during the
		 * resolving stage.
		 */
		void OnChangeLinkTarget (Link link, int oldTargetTopicID, int oldTargetClassID, EventAccessor eventAccessor);

		/* Function: OnDeleteLink
		 * Called before a link is deleted from the database.
		 */
		void OnDeleteLink (Link link, EventAccessor eventAccessor);

		/* Function: OnAddImageLink
		 * Called after an image link is added to the database.
		 */
		void OnAddImageLink (ImageLink imageLink, EventAccessor eventAccessor);

		/* Function: OnChangeImageLinkTarget
		 * Called after an image link's target has been changed in the database.  Note that this will also be called for new image
		 * links, as they are added to the database as unresolved during the parsing stage, and then changed to their targets
		 * during the resolving stage.
		 */
		void OnChangeImageLinkTarget (ImageLink imageLink, int oldTargetFileID, EventAccessor eventAccessor);

		/* Function: OnDeleteImageLink
		 * Called before an image link is deleted from the database.
		 */
		void OnDeleteImageLink (ImageLink imageLink, EventAccessor eventAccessor);

		}
	}
