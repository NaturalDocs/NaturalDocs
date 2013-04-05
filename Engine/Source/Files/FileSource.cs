/* 
 * Class: GregValure.NaturalDocs.Engine.Files.FileSource
 * ____________________________________________________________________________
 * 
 * The base class for a file source.
 * 
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		<AddAllFiles()> is the only function that is allowed to alter <addAllFilesStatus>.  However, other threads may read its 
 *		values at any time.  Since they are all integer counters and are only used for informational purposes, no thread 
 *		synchronization is necessary.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine.Collections;


namespace GregValure.NaturalDocs.Engine.Files
	{
	abstract public class FileSource
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: FileSource
		 */
		public FileSource ()
			{
			addAllFilesStatus = new AddAllFilesStatus();
			claimed = false;
			}
			
		
		/* Function: Validate
		 * Checks whether the file source is valid and adds an error to the list if not.  The default implementation simply
		 * returns true.
		 */
		virtual public bool Validate (Errors.ErrorList errors)
			{  
			return true;  
			}

			
		/* Function: Contains
		 * Returns whether this file source contains the passed file.
		 */
		abstract public bool Contains (Path file);
		
		
		/* Function: MakeRelative
		 * Converts the passed filename to one relative to this source.  If this source doesn't contain the file, it will return null.
		 */
		abstract public Path MakeRelative (Path file);
		
		
		/* Function: AddAllFiles
		 * Iterates through every file in the source and calls <Files.Manager.AddOrUpdateFile()> for each one.
		 */
		abstract public void AddAllFiles (CancelDelegate cancelDelegate);
		
		
		/* Function: GetAddAllFilesStatus
		 * Fills the passed object with the current status of <AddAllFiles()>.  The passed object will contain a snapshot of the status,
		 * not a continuously updating object, so the values won't change out from under you.
		 */
		public void GetAddAllFilesStatus (ref AddAllFilesStatus statusTarget)
			{
			statusTarget.CopyFrom(addAllFilesStatus);
			}
			
			
		/* Function: CombineAddAllFilesStatus
		 * Adds the current status numbers to the passed object.
		 */
		public void CombineAddAllFilesStatus (ref AddAllFilesStatus statusTarget)
			{
			statusTarget.Add(addAllFilesStatus);
			}
			
		
		
		// Group: Properties
		// __________________________________________________________________________
		

		/* Property: UniqueIDString
		 * A string that uniquely identifies this FileSource among all others of its <Type>, including ones based on different
		 * classes.  For example, "Folder:[path]" or "VSProject:[file]".
		 */
		abstract public string UniqueIDString
			{  get;  }
		
		/* Property: Type
		 * The type of files this FileSource provides.
		 */
		abstract public InputType Type
			{  get;  }
						
		/* Property: Number
		 * The number assigned to this FileSource.  Only necessary to implement if <Type> is <InputType.Source> or
		 * <InputType.Image>.
		 */
		virtual public int Number
			{
			get
				{  
				if (Type == InputType.Source || Type == InputType.Image)
					{  throw new NotImplementedException();  }
				else
					{  return 0;  }
				}
			}
						
		/* Property: Name
		 * The name assigned to this FileSource, or null if one hasn't been set.  Only relevant if <Type> is <InputType.Source>.
		 */
		virtual public string Name
			{
			get
				{  return null;  }
			}
			
			
		/* Property: AllFilesAdded
		 * Whether all files have been added via <AddAllFiles()>.
		 */
		public bool AllFilesAdded
			{
			get
				{  return addAllFilesStatus.Completed;  }
			}
			
			
		/* Property: Claimed
		 * Whether the file source is claimed by a thread for processing with <AddAllFiles()>.
		 */
		public bool Claimed
			{
			get
				{  return claimed;  }
			set
				{  claimed = value;  }
			}



		// Group: Variables
		// __________________________________________________________________________
		
		
		protected AddAllFilesStatus addAllFilesStatus;
		
		protected bool claimed;

		}
	}