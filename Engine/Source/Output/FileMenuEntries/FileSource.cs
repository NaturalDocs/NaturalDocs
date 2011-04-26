/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileMenuEntries.FileSource
 * ____________________________________________________________________________
 * 
 * Represents a file source in a <FileMenu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Output.FileMenuEntries
	{
	public class FileSource : Container
		{

		// Group: Functions
		// __________________________________________________________________________

		public FileSource (Files.FileSource newFileSource) : base ()
			{
			fileSource = newFileSource;
			pathFragment = null;
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: WrappedFileSource
		 * The <Files.FileSource> associated with this entry.
		 */
		public Files.FileSource WrappedFileSource
			{
			get
				{  return fileSource;  }
			}

		/* Property: PathFragment
		 * Part of the path associated with this entry beyond what is contained in <WrappedFileSource>, or null if none.
		 * This will only be used if the file source only contained one folder so that folder was condensed into it.
		 */
		public Path PathFragment
			{
			get
				{  return pathFragment;  }
			set
				{  pathFragment = value;  }
			}

		/* Property: IsDynamicFolder
		 * Whether this folder dynamically loads its members as opposed to storing them inline.  The folder will
		 * be dynamic if it only contains a single entry and it's a <RootFolder>.
		 */
		public bool IsDynamicFolder
			{
			get
				{  
				return (Members.Count == 1 && Members[0] is RootFolder);
				}
			}

		/* Property: IsInlineFolder
		 * Whether this folder stores its members inline as opposed to loading them dynamically.
		 */
		public bool IsInlineFolder
			{
			get
				{  return !IsDynamicFolder;  }
			}

		/* Property: DynamicMembersID
		 * If <IsDynamicFolder> is true, the ID number that should be used in place of the members.
		 */
		public int DynamicMembersID
			{
			get
				{  
				if (IsDynamicFolder)
					{  return (Members[0] as RootFolder).ID;  }
				else
					{  return 0;  }
				}
			}

		/* Property: SortString
		 * Returns the string that should be used to sort this entry in a list.
		 */
		override public string SortString
			{  
			get
				{  return (fileSource.Name ?? "Root");  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Files.FileSource fileSource;
		protected Path pathFragment;
		}
	}