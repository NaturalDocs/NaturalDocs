/*
 * Class: CodeClear.NaturalDocs.Engine.Files.ChangeProcessorDetailedStatus
 * ____________________________________________________________________________
 *
 * A deeper look on the progress of <Files.ChangeProcessor.WorkOnProcessingChanges()>, primarily used for
 * debugging.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public class ChangeProcessorDetailedStatus
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: ChangeProcessorDetailedStatus
		 */
		public ChangeProcessorDetailedStatus ()
			{
			FileIDsBeingProcessed = null;
			NewOrChangedFileIDsRemaining = null;
			DeletedFileIDsRemaining = null;
			}



		// Group: Public Variables
		// __________________________________________________________________________


		/* Variable: FileIDsBeingProcessed
		 * The IDs of files currently being processed.  This does not distinguish between those that were changed and
		 * those that were deleted.
		 */
		public IDObjects.NumberSet FileIDsBeingProcessed;

		/* Variable: NewOrChangedFileIDsRemaining
		 * The IDs of new or changed files left to be processed.  This does not include any in <FileIDsBeingProcessed>.
		 */
		public IDObjects.NumberSet NewOrChangedFileIDsRemaining;

		/* Variable: DeletedFileIDsRemaining
		 * The IDs of deleted files left to be processed.  This does not include any in <FileIDsBeingProcessed>.
		 */
		public IDObjects.NumberSet DeletedFileIDsRemaining;

		}
	}
