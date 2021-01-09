/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.File
 * ____________________________________________________________________________
 * 
 * A class containing information about a file.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public class File : IDObjects.Base
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: File
		 */
		public File (Path fileName, FileType type, DateTime lastModified) : base()
			{
			this.fileName = fileName;
			this.type = type;
			this.lastModified = lastModified;
			this.deleted = false;
			}


		/* Function: CreateSnapshotOfProperties
		 * Creates a duplicate File object that contains all the file's properties at the time this function was called.
		 * The duplicate will not change so it can be used to compare to the original File object later to see if any 
		 * of the properties have changed.
		 */
		virtual public File CreateSnapshotOfProperties ()
			{
			File duplicate = new File (fileName, type, lastModified);
			duplicate.ID = ID;
			duplicate.deleted = deleted;

			return duplicate;
			}
			
			
			
		// Group: Properties
		// __________________________________________________________________________
		

		/* Property: FileName
		 * The file name as a <Path>.
		 */
		public Path FileName
			{
			get
				{  return fileName;  }
			}
			
		/* Property: Type
		 * The <FileType>.
		 */
		public FileType Type
			{
			get
				{  return type;  }
			}
			
		/* Property: LastModified
		 * The timestamp of when the file was last modified.
		 */
		public DateTime LastModified
			{
			get
				{  return lastModified;  }
			set
				{  lastModified = value;  }
			}
			
		/* Property: Deleted
		 * Whether this file is deleted.  The File object will continue to exist until the deletion is fully processed.
		 */
		public bool Deleted
			{
			get
				{  return deleted;  }
			set
				{  deleted = value;  }
			}
			
		/* Property: Name
		 * The name of the object, which is the same as <FileName>.
		 */
		public override string Name
			{
			get 
				{  return fileName.ToString();  }
			}
			
			

		// Group: Variables
		// __________________________________________________________________________
		
			
		/* var: fileName
		 * The absolute <Path> to the current file.
		 */
		protected Path fileName;
		
		/* var: type
		 * The <FileType>.
		 */
		protected FileType type;
		
		/* var: lastModified
		 * The timestamp of when the file was last modified.
		 */
		protected DateTime lastModified;
		
		/* var: deleted
		 * Whether this file was deleted, since the File object will persist until it's fully processed.
		 */
		protected bool deleted;

		}
	}