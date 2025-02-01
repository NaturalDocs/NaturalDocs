/*
 * Class: CodeClear.NaturalDocs.Engine.Files.ChangeProcessorStatus
 * ____________________________________________________________________________
 *
 * Statistics on the progress of <Files.ChangeProcessor.WorkOnProcessingChanges()>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public class ChangeProcessorStatus
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: ChangeProcessorStatus
		 */
		public ChangeProcessorStatus ()
			{
			FilesBeingProcessed = 0;
			NewOrChangedFilesRemaining = 0;
			DeletedFilesRemaining = 0;
			}



		// Group: Public Variables
		// __________________________________________________________________________


		/* Variable: FilesBeingProcessed
		 * The number of files currently being processed.  This does not distinguish between those that were
		 * changed and those that were deleted.
		 */
		public int FilesBeingProcessed;

		/* Variable: NewOrChangedFilesRemaining
		 * The number of new or changed files left to be processed.  This does not include any counted in
		 * <FilesBeingProcessed>.
		 */
		public int NewOrChangedFilesRemaining;

		/* Variable: DeletedFilesRemaining
		 * The number of deleted files left to be processed.  This does not include any counted in <FilesBeingProcessed>.
		 */
		public int DeletedFilesRemaining;

		}
	}
