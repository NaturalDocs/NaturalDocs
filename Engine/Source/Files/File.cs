/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.File
 * ____________________________________________________________________________
 * 
 * A class containing information about a file.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
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
			
		/* Property: Status
		 * The file's status.  Will be <FileFlags.Unchanged>, <FileFlags.NewOrChanged>, or <FileFlags.Deleted>.
		 */
		public FileFlags Status
			{
			get
				{  return (flags & FileFlags.StatusMask);  }
			set
				{
				flags &= ~FileFlags.StatusMask;
				flags |= value;
				}
			}
			
		/* Property: Claimed
		 * Whether the file is currently claimed.
		 */
		public bool Claimed
			{
			get
				{  return ( (flags & FileFlags.Claimed) != 0 );  }
			set
				{
				if (value == true)
					{  flags |= FileFlags.Claimed;  }
				else
					{  flags &= ~FileFlags.Claimed;  }
				}
			}
			
		/* Property: StatusSinceClaimed
		 * The file's status since it was claimed.  Will be <FileFlags.UnchangedSinceClaimed>, <FileFlags.NewOrChangedSinceClaimed>, or
		 * <FileFlags.DeletedSinceClaimed>.  This value is undefined when <Claimed> is false.
		 */
		public FileFlags StatusSinceClaimed
			{
			get
				{  return (flags & FileFlags.StatusSinceClaimedMask);  }
			set
				{
				flags &= ~FileFlags.StatusSinceClaimedMask;
				flags |= value;
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