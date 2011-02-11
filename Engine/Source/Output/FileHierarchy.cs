/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileHierarchy
 * ____________________________________________________________________________
 * 
 * A class for generating a tree of all the files to be used in output.
 * 
 * Usage:
 * 
 *		- Add all <Files.FileSources> with <AddFileSource()>.  This must be done before adding files with <AddFile()>.
 *		- Add all <Files.Files> with <AddFile()>.
 *		- If desired, condense unnecessary folder levels with <Condense()>.
 *		- If desired, sort the members with <Sort()>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Output
	{
	public class FileHierarchy
		{

		// Group: Functions
		// __________________________________________________________________________

		public FileHierarchy ()
			{
			fileSourceEntries = new List<FileHierarchyEntries.FileSource>();
			}


		/* Function: AddFileSource
		 * Adds a <Files.FileSource> to the tree.  This must be done before calling <AddFile()>.
		 */
		public void AddFileSource (Files.FileSource fileSource)
			{
			fileSourceEntries.Add( new FileHierarchyEntries.FileSource(fileSource) );
			}


		/* Function: AddFile
		 * Adds a <Files.File> to the tree.  Its corresponding <File.FileSource> must have been added with <AddFileSource()>
		 * before calling this function.
		 */
		public void AddFile (Files.File file)
			{
			// Find which file source owns this file and generate a relative path to it.

			FileHierarchyEntries.FileSource fileSourceEntry = null;
			Path relativePath = null;

			foreach (FileHierarchyEntries.FileSource possibleFileSourceEntry in fileSourceEntries)
				{
				// We don't need a separate test for whether the file source contains the file name since we'll get the same 
				// information by checking whether the result of MakeRelative is null.  This is more efficient too.
				relativePath = possibleFileSourceEntry.WrappedFileSource.MakeRelative(file.FileName);

				if (relativePath != null)
					{
					fileSourceEntry = possibleFileSourceEntry;
					break;
					}
				}

			if (fileSourceEntry == null)
				{  throw new InvalidOperationException();  }


			// Split off the file name and split the rest into individual folder names.

			string prefix;
			List<string> pathSegments;
			relativePath.Split(out prefix, out pathSegments);
			string filename = pathSegments[pathSegments.Count - 1];
			pathSegments.RemoveAt(pathSegments.Count - 1);


			// Create the file entry and find out where it goes.  Create new folder levels as necessary.

			FileHierarchyEntries.File fileEntry = new FileHierarchyEntries.File(filename);
			FileHierarchyEntries.Container container = fileSourceEntry;

			foreach (Path pathSegment in pathSegments)
				{
				FileHierarchyEntries.Folder folderEntry = null;

				foreach (FileHierarchyEntries.Entry member in container.Members)
					{
					if (member is FileHierarchyEntries.Folder && 
						 (member as FileHierarchyEntries.Folder).PathFragment == pathSegment)
						{  
						folderEntry = (FileHierarchyEntries.Folder)member;
						break;
						}
					}

				if (folderEntry == null)
					{
					folderEntry = new FileHierarchyEntries.Folder(pathSegment);
					folderEntry.Parent = container;
					container.Members.Add(folderEntry);
					}

				container = folderEntry;
				}

			fileEntry.Parent = container;
			container.Members.Add(fileEntry);
			}


		/* Function: Condense
		 * Condenses unnecessary folder levels, turning "FolderA" and "FolderB" into "FolderA/FolderB" if A contains nothing
		 * other than B.
		 */
		public void Condense ()
			{
			foreach (var fileSourceEntry in fileSourceEntries)
				{
				for (int i = 0; i < fileSourceEntry.Members.Count; i++)
					{
					var member = fileSourceEntry.Members[i];

					if (member is FileHierarchyEntries.Folder)
						{  
						var replacement = CondenseFolder((FileHierarchyEntries.Folder)member);

						if (replacement != null)
							{  
							replacement.Parent = fileSourceEntry;
							fileSourceEntry.Members[i] = replacement;  
							}
						}
					}

				if (fileSourceEntry.Members.Count == 1 && fileSourceEntry.Members[0] is FileHierarchyEntries.Folder)
					{  
					fileSourceEntry.Members = (fileSourceEntry.Members[0] as FileHierarchyEntries.Folder).Members;  

					foreach (var member in fileSourceEntry.Members)
						{  member.Parent = fileSourceEntry;  }

					fileSourceEntry.PathFragment = (fileSourceEntry.Members[0] as FileHierarchyEntries.Folder).PathFragment;
					}
				}
			}

		/* Function: CondenseFolder
		 * A support function for <Condense()>, this first tries to recursively condense any subfolders in the passed entry, 
		 * and then if it itself needs to be condensed it will return the entry it should be replaced with.  If the entry should 
		 * stay in the hierarchy it will return null.
		 */
		protected FileHierarchyEntries.Folder CondenseFolder (FileHierarchyEntries.Folder folderEntry)
			{
			for (int i = 0; i < folderEntry.Members.Count; i++)
				{
				var member = folderEntry.Members[i];

				if (member is FileHierarchyEntries.Folder)
					{  
					var replacement = CondenseFolder((FileHierarchyEntries.Folder)member);

					if (replacement != null)
						{  
						replacement.Parent = folderEntry;
						folderEntry.Members[i] = replacement;  
						}
					}
				}

			if (folderEntry.Members.Count == 1 && folderEntry.Members[0] is FileHierarchyEntries.Folder)
				{
				var member = (FileHierarchyEntries.Folder)folderEntry.Members[0];
				member.PathFragment = folderEntry.PathFragment + "/" + member.PathFragment;
				return member;
				}
			else
				{  return null;  }
			}


		/* Function: Sort
		 * Sorts the <FileSourceEntries> and all folders contained within them.
		 */
		public void Sort ()
			{
			fileSourceEntries.Sort();

			foreach (FileHierarchyEntries.FileSource fileSourceEntry in fileSourceEntries)
				{  fileSourceEntry.Sort();  }
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: FileSourceEntries
		 */
		public List<FileHierarchyEntries.FileSource> FileSourceEntries
			{
			get
				{  return fileSourceEntries;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected List<FileHierarchyEntries.FileSource> fileSourceEntries;

		}
	}