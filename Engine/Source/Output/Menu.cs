/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Menu
 * ____________________________________________________________________________
 * 
 * A class for generating a menu tree.
 * 
 * Usage:
 * 
 *		- Add all <Files.FileSources> with <AddFileSource()>.  This must be done before adding files with <AddFile()>.
 *		- Add all <Files.Files> with <AddFile()>.
 *		- If desired, condense unnecessary folder levels with <Condense()>.
 *		- If desired, sort the members with <Sort()>.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Output
	{
	public class Menu
		{

		//// Group: Types
		//// __________________________________________________________________________

		///* Enum: ForEachMethod
		// * 
		// * The ways to traverse the menu with <ForEach()>.
		// * 
		// * Linear - ForEach will walk through the menu as if it were displaying a fully expanded tree line by
		// *					line.  The parent will be done, then all its children, then back to the parent's next sibling.
		// * ChildrenFirst - ForEach will walk through all children before performing the action on any parent, which
		// *							  allows things like output generation where the parent's output depends on the children's
		// *							  output being already made.
		// */
		//public enum ForEachMethod : byte
		//   {  Linear, ChildrenFirst  }



		// Group: Functions
		// __________________________________________________________________________

		public Menu ()
		   {
			rootFileMenu = null;

			isCondensed = false;
		   }


		/* Function: AddFileSource
		 * Adds a <Files.FileSource> to the tree.  This must be done for all FileSources before calling <AddFile()>.
		 */
		public void AddFileSource (Files.FileSource fileSource)
		   {
			#if DEBUG
			if (isCondensed)
				{  throw new Exception("Cannot add a new FileSource to the menu once it's been condensed.");  }
			#endif

			if (rootFileMenu == null)
				{
				rootFileMenu = new MenuEntries.Base.Container();
				rootFileMenu.Title = Engine.Locale.Get("NaturalDocs.Engine", "Menu.Files");
				}

		   MenuEntries.File.FileSource fileSourceEntry = new MenuEntries.File.FileSource(fileSource);
		   fileSourceEntry.Parent = rootFileMenu;

		   rootFileMenu.Members.Add(fileSourceEntry);
		   }


		/* Function: FindFileSourceOf
		 * Finds the FileSource entry that contains the passed file.  This can only be used after all FileSources have
		 * been added with <AddFileSource()>.
		 */
		public MenuEntries.File.FileSource FindFileSourceOf (Files.File file)
			{
			#if DEBUG
			if (rootFileMenu == null || rootFileMenu.Members == null || rootFileMenu.Members.Count == 0)
				{  throw new Exception("Tried to use FindFileSourceOf() before any FileSources were added to the menu.");  }
			#endif

			// If the menu only had one file source and it was condensed, the root file entry may have been replaced
			// by that file source.
			if (rootFileMenu is MenuEntries.File.FileSource)
				{
				MenuEntries.File.FileSource fileSourceEntry = (MenuEntries.File.FileSource)rootFileMenu;

				#if DEBUG
				if (fileSourceEntry.WrappedFileSource.Contains(file.FileName) == false)
					{  throw new Exception("Could not find the file source of file " + file.FileName + " in the menu.");  }
				#endif

				return fileSourceEntry;
				}

			// We're assuming that the only other possibility is a container with a flat list of FileSources.  If we later allow 
			// FileSources to be put in nested groups this will need to be updated.
			else
				{
				foreach (var member in rootFileMenu.Members)
					{
					if (member is MenuEntries.File.FileSource)
						{
						MenuEntries.File.FileSource fileSourceEntry = (MenuEntries.File.FileSource)member;
						
						if (fileSourceEntry.WrappedFileSource.Contains(file.FileName))
							{  return fileSourceEntry;  }
						}
					}

				#if DEBUG
				throw new Exception("Could not find the file source of file " + file.FileName + " in the menu.");
				#else
				return null;  // This should never happen if the class was used correctly.
				#endif
				}
			}


		/* Function: AddFile
		 * Adds a <Files.File> to the tree.  Its corresponding <File.FileSource> must have been added with <AddFileSource()>
		 * before calling this function.
		 */
		public void AddFile (Files.File file)
		   {
			#if DEBUG
			if (isCondensed)
				{  throw new Exception("Cannot add a file to the menu once it's been condensed.");  }
			#endif


		   // Find which file source owns this file and generate a relative path to it.

		   MenuEntries.File.FileSource fileSourceEntry = FindFileSourceOf(file);

			#if DEBUG
			if (fileSourceEntry == null)
				{  throw new Exception("Couldn't find the file source of " + file.FileName + " in the menu.");  }
			#endif

		   Path relativePath = fileSourceEntry.WrappedFileSource.MakeRelative(file.FileName);


		   // Split off the file name and split the rest into individual folder names.

		   string prefix;
		   List<string> pathSegments;
		   relativePath.Split(out prefix, out pathSegments);

		   string fileName = pathSegments[pathSegments.Count - 1];
		   pathSegments.RemoveAt(pathSegments.Count - 1);


		   // Create the file entry and find out where it goes.  Create new folder levels as necessary.

		   MenuEntries.File.File fileEntry = new MenuEntries.File.File(file);
		   MenuEntries.Base.Container container = fileSourceEntry;

		   foreach (string pathSegment in pathSegments)
		      {
				Path pathFromFileSource;

				if (container == fileSourceEntry)
					{  pathFromFileSource = pathSegment;  }
				else
					{  pathFromFileSource = (container as MenuEntries.File.Folder).PathFromFileSource + '/' + pathSegment;  }

		      MenuEntries.File.Folder folderEntry = null;

		      foreach (MenuEntries.Base.Entry member in container.Members)
		         {
		         if (member is MenuEntries.File.Folder && 
		             (member as MenuEntries.File.Folder).PathFromFileSource == pathFromFileSource)
		            {  
		            folderEntry = (MenuEntries.File.Folder)member;
		            break;
		            }
		         }

		      if (folderEntry == null)
		         {
		         folderEntry = new MenuEntries.File.Folder(pathFromFileSource);
		         folderEntry.Parent = container;
		         container.Members.Add(folderEntry);
		         }

		      container = folderEntry;
		      }

		   fileEntry.Parent = container;
		   container.Members.Add(fileEntry);
		   }


		///* Function: Condense
		// * Condenses unnecessary folder levels, turning "FolderA" and "FolderB" into "FolderA/FolderB" if A contains nothing
		// * other than B.  Will also remove file source entries that do not have content.
		// */
		//public void Condense ()
		//   {
		//   int fileSourceIndex = 0;
		//   while (fileSourceIndex < rootFolder.Members.Count)
		//      {
		//      FileMenuEntries.FileSource fileSourceEntry = (FileMenuEntries.FileSource)rootFolder.Members[fileSourceIndex];

		//      if (fileSourceEntry.Members.Count == 0)
		//         {  rootFolder.Members.RemoveAt(fileSourceIndex);  }
		//      else
		//         {
		//         for (int i = 0; i < fileSourceEntry.Members.Count; i++)
		//            {
		//            var member = fileSourceEntry.Members[i];

		//            if (member is FileMenuEntries.Folder)
		//               {  
		//               var replacement = CondenseFolder((FileMenuEntries.Folder)member);

		//               if (replacement != null)
		//                  {  
		//                  replacement.Parent = fileSourceEntry;
		//                  fileSourceEntry.Members[i] = replacement;  
		//                  }
		//               }
		//            }

		//         if (fileSourceEntry.Members.Count == 1 && fileSourceEntry.Members[0] is FileMenuEntries.Folder)
		//            {  
		//            fileSourceEntry.PathFragment = (fileSourceEntry.Members[0] as FileMenuEntries.Folder).PathFragment;
		//            fileSourceEntry.Members = (fileSourceEntry.Members[0] as FileMenuEntries.Folder).Members;  

		//            foreach (var member in fileSourceEntry.Members)
		//               {  member.Parent = fileSourceEntry;  }
		//            }

		//         fileSourceIndex++;
		//         }
		//      }
		//   }

		///* Function: CondenseFolder
		// * A support function for <Condense()>, this first tries to recursively condense any subfolders in the passed entry, 
		// * and then if it itself needs to be condensed it will return the entry it should be replaced with.  If the entry should 
		// * stay in the menu it will return null.
		// */
		//protected FileMenuEntries.Folder CondenseFolder (FileMenuEntries.Folder folderEntry)
		//   {
		//   for (int i = 0; i < folderEntry.Members.Count; i++)
		//      {
		//      var member = folderEntry.Members[i];

		//      if (member is FileMenuEntries.Folder)
		//         {  
		//         var replacement = CondenseFolder((FileMenuEntries.Folder)member);

		//         if (replacement != null)
		//            {  
		//            replacement.Parent = folderEntry;
		//            folderEntry.Members[i] = replacement;  
		//            }
		//         }
		//      }

		//   if (folderEntry.Members.Count == 1 && folderEntry.Members[0] is FileMenuEntries.Folder)
		//      {
		//      var member = (FileMenuEntries.Folder)folderEntry.Members[0];
		//      member.PathFragment = folderEntry.PathFragment + "/" + member.PathFragment;
		//      return member;
		//      }
		//   else
		//      {  return null;  }
		//   }


		///* Function: Sort
		// * Sorts the <FileSourceEntries> and all folders contained within them.
		// */
		//public void Sort ()
		//   {
		//   rootFolder.Sort();
		//   }


		///* Function: ForEach
		// * Performs an action on every element in the menu.
		// */
		//public void ForEach (Action<FileMenuEntries.Entry> action, ForEachMethod method = ForEachMethod.Linear)
		//   {
		//   ForEach(action, method, rootFolder);
		//   }

		///* Function: ForEach
		// * A recursive helper function for the public ForEach function.
		// */
		//protected void ForEach (Action<FileMenuEntries.Entry> action, ForEachMethod method, 
		//                                 FileMenuEntries.Container container)
		//   {
		//   if (method == ForEachMethod.Linear)
		//      {  action(container);  }

		//   foreach (var member in container.Members)
		//      {
		//      if (member is FileMenuEntries.Container)
		//         {  ForEach(action, method, (FileMenuEntries.Container)member);  }
		//      else
		//         {  action(member);  }
		//      }

		//   if (method == ForEachMethod.ChildrenFirst)
		//      {  action(container);  }
		//   }



		// Group: Properties
		// __________________________________________________________________________


		/* Property: RootFileMenu
		 * 
		 * The root container of all file-based menu entries, or null if none.
		 * 
		 * Before condensation this will be a container with only <MenuEntries.File.FileSources> as its members.  However,
		 * after condensation it may be a file source if there was only one.
		 */
		public MenuEntries.Base.Container RootFileMenu
		   {
		   get
		      {  return rootFileMenu;  }
		   }
			


		// Group: Variables
		// __________________________________________________________________________


		/* var: rootFileMenu
		 * 
		 * The root container of all file-based menu entries, or null if none.
		 * 
		 * Before condensation this will be a container with only <MenuEntries.File.FileSources> as its members.  However,
		 * after condensation it may be a file source if there was only one.
		 */
		protected MenuEntries.Base.Container rootFileMenu;

		/* var: isCondensed
		 * Whether the menu tree has been condensed.
		 */
		protected bool isCondensed;

		}
	}