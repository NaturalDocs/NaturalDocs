/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries.Folder
 * ____________________________________________________________________________
 * 
 * Represents a folder or group of folders in a <FileHierarchy>.  It will only represent a group of folders
 * ("FolderA/FolderB") if the parent folder contains nothing other than the child folder.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries
	{
	public class Folder : Container
		{

		// Group: Functions
		// __________________________________________________________________________

		public Folder (Path newPathFragment) : base ()
			{
			pathFragment = newPathFragment;
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: PathFragment
		 * Part of the path associated with this entry.  It is not an absolute path nor is it the path from the
		 * file source.  Rather, it is the part of the path between its parent folder entry and its members.  In
		 * most cases this will be a single folder name ("FolderA").  However, it may also be multiple folder
		 * levels ("FolderA/FolderB") if the parent folder contains nothing but the child folder.
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
				{  return pathFragment;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Path pathFragment;
		}
	}