/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.File
 * ____________________________________________________________________________
 * 
 * A class containing information about a file.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
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
		public File (Path newFileName, FileType newType, DateTime newLastModified) : base()
			{
			fileName = newFileName;
			type = newType;
			lastModified = newLastModified;
			flags = 0;
			}


		/* Function: CreateSnapshotOfProperties
		 * Creates a duplicate File object that contains all the file's properties at the time this function was called.
		 * The duplicate will not change so it can be used to compare to the original File object later to see if any 
		 * of the properties have changed.
		 */
		public File CreateSnapshotOfProperties ()
			{
			File duplicate = new File (fileName, type, lastModified);
			duplicate.ID = ID;
			duplicate.flags = flags;

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
			
		/* Property: InBinaryFile
		 * Whether this file is defined in <Files.nd>.
		 */
		public bool InBinaryFile
			{
			get
				{  return ( (flags & FileFlags.InBinaryFile) != 0 );  }
			set
				{
				if (value == true)
					{  flags |= FileFlags.InBinaryFile;  }
				else
					{  flags &= ~FileFlags.InBinaryFile;  }
				}
			}
			
		/* Property: InFileSource
		 * Whether this file was found in one of <Engine.Files.Manager's> file sources.
		 */
		public bool InFileSource
			{
			get
				{  return ( (flags & FileFlags.InFileSource) != 0 );  }
			set
				{
				if (value == true)
					{  flags |= FileFlags.InFileSource;  }
				else
					{  flags &= ~FileFlags.InFileSource;  }
				}
			}
			
		/* Property: Deleted
		 * Whether this file is deleted.  The File object will continue to exist until the deletion is processed.
		 */
		public bool Deleted
			{
			get
				{  return ( (flags & FileFlags.Deleted) != 0 );  }
			set
				{
				if (value == true)
					{  flags |= FileFlags.Deleted;  }
				else
					{  flags &= ~FileFlags.Deleted;  }
				}
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
		
		/* var: flags
		 * Informational flags about the file.
		 */
		protected FileFlags flags;

		}
	}