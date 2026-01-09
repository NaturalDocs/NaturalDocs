/*
 * Interface: CodeClear.NaturalDocs.Engine.Files.IChangeWatcher
 * ____________________________________________________________________________
 *
 * An interface for any class that wants to watch for changes in the file list.
 *
 * Multithreading: Thread Safety Notes
 *
 *		This interface is used for receiving notifications when files have changed.  As such, these functions can be called
 *		from any possible thread.  Make sure any structures you interact with are thread safe.
 *
 *		Because event handlers will be called while holding a lock on <Files.Manager>, you should keep interaction with other
 *		modules to a minimum to prevent deadlocks.  You don't want to access another thread-safe module causing your
 *		thread to wait for it to lock while another thread is holding that lock and waiting to access <Files.Manager>.  The
 *		event handler should ideally just collect the file IDs for work that needs to be done and return, letting other code do
 *		the actual work later.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public interface IChangeWatcher
		{

		/* Function: OnAddFile
		 * Called after a new file is added to the project.
		 */
		void OnAddFile (File file);

		/* Function: OnFileChanged
		 * Called after a changed file has been detected in the project.
		 */
		void OnFileChanged (File file);

		/* Function: OnDeleteFile
		 * Called when it's detected that a file was deleted from the project.
		 */
		void OnDeleteFile (File file);

		}
	}
