/* 
 * Class: GregValure.NaturalDocs.Engine.Files.AddAllFilesStatus
 * ____________________________________________________________________________
 * 
 * Statistics on the progress of <FileSource.AddAllFiles()>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2008 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Files
	{
	public class AddAllFilesStatus
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: AddAllFilesStatus
		 */
		public AddAllFilesStatus ()
			{
			Reset();
			}
			
		/* Function: Reset
		 * Reset all values back to their initial state.
		 */
		public void Reset ()
			{
			Completed = false;

			SourceFilesFound = 0;
			SourceFoldersFound = 0;
			}
		
		/* Function: CopyFrom
		 * Copies all the variables from the passed one.
		 */
		public void CopyFrom (AddAllFilesStatus other)
			{
			Completed = other.Completed;

			SourceFilesFound = other.SourceFilesFound;
			SourceFoldersFound = other.SourceFoldersFound;
			}
			
		/* Function: Add
		 * Adds the statistics of the passed status to this one.
		 */
		public void Add (AddAllFilesStatus other)
			{
			if (other.Completed == false)
				{  Completed = false;  }
				
			SourceFilesFound += other.SourceFilesFound;
			SourceFoldersFound += other.SourceFoldersFound;
			}
			
			
			
		// Group: Public Variables
		// __________________________________________________________________________
		
		
		/* Variable: SourceFilesFound
		 * The number of source files found by the function.  This does not include image or style files.
		 */
		public int SourceFilesFound;
		
		/* Variable: SourceFoldersFound
		 * The number of source folders found by the function, if appropriate.  This does not include image or style folders.
		 */
		public int SourceFoldersFound;
		
		/* Variable: Completed
		 * Whether the function was successfully completed on this file source.
		 */
		public bool Completed;
		
		}
	}