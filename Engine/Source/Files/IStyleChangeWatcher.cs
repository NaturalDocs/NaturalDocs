/* 
 * Interface: CodeClear.NaturalDocs.Engine.Files.IStyleChangeWatcher
 * ____________________________________________________________________________
 * 
 * An interface for any class that wants to watch for changes in the style files.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public interface IStyleChangeWatcher
		{
		
		/* Function: OnAddOrChangeFile
		 * Called to handle a file that was added or changed.
		 */
		Manager.ReleaseClaimedFileReason OnAddOrChangeFile (Path file);
		
		/* Function: OnDeleteFile
		 * Called to handle a file that was deleted since the last run.
		 */
		Manager.ReleaseClaimedFileReason OnDeleteFile (Path file);
		
		}
	}