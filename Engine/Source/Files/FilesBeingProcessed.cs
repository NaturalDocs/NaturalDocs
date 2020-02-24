/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.FilesBeingProcessed
 * ____________________________________________________________________________
 * 
 * A class that handles the list of files being processed by <Files.Processor>.
 * 
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		This class is not thread safe.  It is assumed that <Files.Processor> will manage thread safety itself.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public class FilesBeingProcessed
		{
		
		// Group: Functions
		// __________________________________________________________________________
		

		/* Function: FilesBeingProcessed
		 */
		public FilesBeingProcessed ()
			{
			filesBeingProcessed = new List<File>();
			}


		public void Add (File file)
			{
			if (Contains(file))
				{  throw new InvalidOperationException();  }

			filesBeingProcessed.Add( file.CreateSnapshotOfProperties() );
			}


		public void Remove (File file)
			{
			Remove(file.ID);
			}


		public void Remove (int fileID)
			{
			for (int i = 0; i < filesBeingProcessed.Count; i++)
				{
				if (filesBeingProcessed[i].ID == fileID)
					{
					filesBeingProcessed.RemoveAt(i);
					return;
					}
				}

			// Couldn't find it
			throw new InvalidOperationException();
			}


		public File GetPropertiesWhenAdded (File file)
			{
			return GetPropertiesWhenAdded(file.ID);
			}


		public File GetPropertiesWhenAdded (int fileID)
			{
			for (int i = 0; i < filesBeingProcessed.Count; i++)
				{
				if (filesBeingProcessed[i].ID == fileID)
					{  return filesBeingProcessed[i];  }
				}

			// Couldn't find it
			throw new InvalidOperationException();
			}


		public bool Contains (File file)
			{
			return Contains(file.ID);
			}


		public bool Contains (int fileID)
			{
			for (int i = 0; i < filesBeingProcessed.Count; i++)
				{
				if (filesBeingProcessed[i].ID == fileID)
					{  return true;  }
				}

			return false;
			}



		// Group: Properties
		// __________________________________________________________________________
		

		public int Count
			{
			get
				{  return filesBeingProcessed.Count;  }
			}


		public bool IsEmpty
			{
			get
				{  return (filesBeingProcessed.Count == 0);  }
			}



		// Group: Variables
		// __________________________________________________________________________
		

		/* var: filesBeingProcessed
		 * 
		 * A list of <File> objects currently being worked on.  The <File> objects are snapshots of the file's properties
		 * as of the time processing began, so when finishing processing it may be compared to the current state in case
		 * it changed.
		 * 
		 * We assume this list will never be very large since the number of concurrent worker threads will never be very 
		 * large, and thus using List is fine even though we have to iterate through it to find matches sometimes.
		 */
		protected List<File> filesBeingProcessed;

		}
	}