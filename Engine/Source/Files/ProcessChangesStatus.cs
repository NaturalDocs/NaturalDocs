/* 
 * Class: GregValure.NaturalDocs.Engine.Files.ProcessChangesStatus
 * ____________________________________________________________________________
 * 
 * Statistics on the progress of <Files.Manager.WorkOnProcessingChanges()>.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2008 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Files
	{
	public class ProcessChangesStatus
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: ProcessChangesStatus
		 */
		public ProcessChangesStatus ()
			{
			Reset();
			}
			
		/* Function: Reset
		 * Reset all values back to their initial state.
		 */
		public void Reset ()
			{
			TotalFiles = 0;
			FilesBeingProcessed = 0;
			ChangedFilesRemaining = 0;
			DeletedFilesRemaining = 0;
			}
		


		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Completed
		 * Whether all files are processed.
		 */
		public bool Completed 
			{
			get
				{
				return (FilesBeingProcessed == 0 && 
						  ChangedFilesRemaining == 0 &&
						  DeletedFilesRemaining == 0);
				}
			}
			
			
		// Group: Public Variables
		// __________________________________________________________________________
		
		
		/* Variable: TotalFiles
		 * The total number of known files, including deleted ones.
		 */
		public int TotalFiles;
		
		/* Variable: FilesBeingProcessed
		 * The number of files currently being processed.  This does not distinguish between those that were
		 * changed and those that were deleted.
		 */
		public int FilesBeingProcessed;
		
		/* Variable: ChangedFilesRemaining
		 * The number of changed files left to be processed.  This does not include any counted in <FilesBeingProcessed>.
		 */
		public int ChangedFilesRemaining;
		
		/* Variable: DeletedFilesRemaining
		 * The number of deleted files left to be processed.  This does not include any counted in <FilesBeingProcessed>.
		 */
		public int DeletedFilesRemaining;
		
		}
	}