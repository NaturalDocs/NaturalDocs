/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileMenu
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
 *		
 * Tree Structure:
 * 
 *		The tree starts out with a <FileMenuEntries.RootFolder>.  It will only contain <FileMenuEntries.FileSource> 
 *		members.
 *		
 *		The file sources can have <FileMenuEntries.File> and <FileMenuEntries.Folder> entries, and the folders
 *		may contain further entries of these types.
 *		
 *		In order to support dynamic folders, additional root folders may be introduced between a folder and its members.
 *		The folder will have all its members replaced with a single root folder entry, and the root folder will contain its
 *		members instead.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Output
	{
	public class FileMenu
		{

		// Group: Types
		// __________________________________________________________________________

		/* Enum: ForEachMethod
		 * 
		 * The ways to traverse the menu with <ForEach()>.
		 * 
		 * Linear - ForEach will walk through the menu as if it were displaying a fully expanded tree line by
		 *					line.  The parent will be done, then all its children, then back to the parent's next sibling.
		 * ChildrenFirst - ForEach will walk through all children before performing the action on any parent, which
		 *							  allows things like output generation where the parent's output depends on the children's
		 *							  output being already made.
		 */
		public enum ForEachMethod : byte
			{  Linear, ChildrenFirst  }



		// Group: Functions
		// __________________________________________________________________________

		public FileMenu ()
			{
			rootFolder = MakeRootFolderEntry();
			rootFolderIDs = new IDObjects.NumberSet();

			rootFolder.ID = 1;
			rootFolderIDs.Add(1);
			}


		/* Function: AddFileSource
		 * Adds a <Files.FileSource> to the tree.  This must be done before calling <AddFile()>.
		 */
		public void AddFileSource (Files.FileSource fileSource)
			{
			FileMenuEntries.FileSource fileSourceEntry = MakeFileSourceEntry(fileSource);

			fileSourceEntry.Parent = rootFolder;
			rootFolder.Members.Add(fileSourceEntry);
			}


		/* Function: AddFile
		 * Adds a <Files.File> to the tree.  Its corresponding <File.FileSource> must have been added with <AddFileSource()>
		 * before calling this function.
		 */
		public void AddFile (Files.File file)
			{
			// Find which file source owns this file and generate a relative path to it.

			FileMenuEntries.FileSource fileSourceEntry = null;
			Path relativePath = null;

			foreach (FileMenuEntries.FileSource possibleFileSourceEntry in rootFolder.Members)
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

			FileMenuEntries.File fileEntry = MakeFileEntry(filename);
			FileMenuEntries.Container container = fileSourceEntry;

			foreach (Path pathSegment in pathSegments)
				{
				FileMenuEntries.Folder folderEntry = null;

				foreach (FileMenuEntries.Entry member in container.Members)
					{
					if (member is FileMenuEntries.Folder && 
						 (member as FileMenuEntries.Folder).PathFragment == pathSegment)
						{  
						folderEntry = (FileMenuEntries.Folder)member;
						break;
						}
					}

				if (folderEntry == null)
					{
					folderEntry = MakeFolderEntry(pathSegment);
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
		 * other than B.  Will also remove file source entries that do not have content.
		 */
		public void Condense ()
			{
			int fileSourceIndex = 0;
			while (fileSourceIndex < rootFolder.Members.Count)
				{
				FileMenuEntries.FileSource fileSourceEntry = (FileMenuEntries.FileSource)rootFolder.Members[fileSourceIndex];

				if (fileSourceEntry.Members.Count == 0)
					{  rootFolder.Members.RemoveAt(fileSourceIndex);  }
				else
					{
					for (int i = 0; i < fileSourceEntry.Members.Count; i++)
						{
						var member = fileSourceEntry.Members[i];

						if (member is FileMenuEntries.Folder)
							{  
							var replacement = CondenseFolder((FileMenuEntries.Folder)member);

							if (replacement != null)
								{  
								replacement.Parent = fileSourceEntry;
								fileSourceEntry.Members[i] = replacement;  
								}
							}
						}

					if (fileSourceEntry.Members.Count == 1 && fileSourceEntry.Members[0] is FileMenuEntries.Folder)
						{  
						fileSourceEntry.PathFragment = (fileSourceEntry.Members[0] as FileMenuEntries.Folder).PathFragment;
						fileSourceEntry.Members = (fileSourceEntry.Members[0] as FileMenuEntries.Folder).Members;  

						foreach (var member in fileSourceEntry.Members)
							{  member.Parent = fileSourceEntry;  }
						}

					fileSourceIndex++;
					}
				}
			}

		/* Function: CondenseFolder
		 * A support function for <Condense()>, this first tries to recursively condense any subfolders in the passed entry, 
		 * and then if it itself needs to be condensed it will return the entry it should be replaced with.  If the entry should 
		 * stay in the menu it will return null.
		 */
		protected FileMenuEntries.Folder CondenseFolder (FileMenuEntries.Folder folderEntry)
			{
			for (int i = 0; i < folderEntry.Members.Count; i++)
				{
				var member = folderEntry.Members[i];

				if (member is FileMenuEntries.Folder)
					{  
					var replacement = CondenseFolder((FileMenuEntries.Folder)member);

					if (replacement != null)
						{  
						replacement.Parent = folderEntry;
						folderEntry.Members[i] = replacement;  
						}
					}
				}

			if (folderEntry.Members.Count == 1 && folderEntry.Members[0] is FileMenuEntries.Folder)
				{
				var member = (FileMenuEntries.Folder)folderEntry.Members[0];
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
			rootFolder.Sort();
			}


		/* Function: ForEach
		 * Performs an action on every element in the menu.
		 */
		public void ForEach (Action<FileMenuEntries.Entry> action, ForEachMethod method = ForEachMethod.Linear)
			{
			ForEach(action, method, rootFolder);
			}

		/* Function: ForEach
		 * A recursive helper function for the public ForEach function.
		 */
		protected void ForEach (Action<FileMenuEntries.Entry> action, ForEachMethod method, 
													FileMenuEntries.Container container)
			{
			if (method == ForEachMethod.Linear)
				{  action(container);  }

			foreach (var member in container.Members)
				{
				if (member is FileMenuEntries.Container)
					{  ForEach(action, method, (FileMenuEntries.Container)member);  }
				else
					{  action(member);  }
				}

			if (method == ForEachMethod.ChildrenFirst)
				{  action(container);  }
			}

		/* Function: MakeRootFolderEntry
		 * An overridable function to create a root folder entry.  Is overridable so that you can subclass the entries.
		 */
		protected virtual FileMenuEntries.RootFolder MakeRootFolderEntry ()
			{
			return new FileMenuEntries.RootFolder();
			}

		/* Function: MakeFileSourceEntry
		 * An overridable function to create a file source entry.  Is overridable so that you can subclass the entries.
		 */
		protected virtual FileMenuEntries.FileSource MakeFileSourceEntry (Files.FileSource fileSource)
			{
			return new FileMenuEntries.FileSource(fileSource);
			}

		/* Function: MakeFolderEntry
		 * An overridable function to create a folder entry.  Is overridable so that you can subclass the entries.
		 */
		protected virtual FileMenuEntries.Folder MakeFolderEntry (Path pathSegment)
			{
			return new FileMenuEntries.Folder(pathSegment);
			}

		/* Function: MakeFileEntry
		 * An overridable function to create a file entry.  Is overridable so that you can subclass the entries.
		 */
		protected virtual FileMenuEntries.File MakeFileEntry (Path filename)
			{
			return new FileMenuEntries.File(filename);
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: RootFolder
		 * The root folder at the base of the menu.
		 */
		public FileMenuEntries.RootFolder RootFolder
			{
			get
				{  return rootFolder;  }
			}

		/* Property: RootFolderIDs
		 * A number set of all the used root folder IDs.
		 */
		public IDObjects.NumberSet RootFolderIDs
			{
			get
				{  return rootFolderIDs;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected FileMenuEntries.RootFolder rootFolder;
		protected IDObjects.NumberSet rootFolderIDs;

		}
	}