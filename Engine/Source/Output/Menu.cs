/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Menu
 * ____________________________________________________________________________
 * 
 * A class for generating a menu tree.
 * 
 * Usage:
 * 
 *		- Add all files with <AddFile()>.
 *		- If desired, condense unnecessary folder levels with <Condense()>.  You cannot add more entries after calling
 *		  this.
 *		- Sort the members with <Sort()>.
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

		// Group: Functions
		// __________________________________________________________________________

		public Menu ()
		   {
			rootFileMenu = null;

			isCondensed = false;
		   }


		/* Function: AddFile
		 * Adds a file to the menu tree.
		 */
		public void AddFile (Files.File file)
		   {
			#if DEBUG
			if (isCondensed)
				{  throw new Exception("Cannot add a file to the menu once it's been condensed.");  }
			#endif


		   // Find which file source owns this file and generate a relative path to it.

		   MenuEntries.File.FileSource fileSourceEntry = FindOrCreateFileSourceEntryOf(file);
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


		/* Function: Condense
		 *	 Removes unnecessary levels in the menu.  Only call this function after everything has been added.
		 */
		public void Condense ()
			{
			isCondensed = true;

			rootFileMenu.Condense();

			// If there's only one file source we can remove the top level container.
			if (rootFileMenu.Members.Count == 1)
				{  
				rootFileMenu.Members[0].Title = rootFileMenu.Title;
				rootFileMenu = (MenuEntries.Base.Container)rootFileMenu.Members[0];
				}
			}


		/* Function: Sort
		 * Sorts the menu entries.  Should only be done after everything is added to the menu.
		 */
		public void Sort ()
		   {
		   rootFileMenu.Sort();
		   }



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: FindOrCreateFileSourceEntryOf
		 * Finds or creates the file source entry associated with the passed file.
		 */
		protected MenuEntries.File.FileSource FindOrCreateFileSourceEntryOf (Files.File file)
			{
			var fileSource = Engine.Instance.Files.FileSourceOf(file);
			var fileSourceEntry = FindFileSourceEntry(fileSource);

			if (fileSourceEntry == null)
				{  fileSourceEntry = CreateFileSourceEntry(fileSource);  }

			return fileSourceEntry;
			}


		/* Function: CreateFileSourceEntry
		 * Creates an entry for the file source, adds it to the menu, and returns it.  It will also create the <rootFileMenu> 
		 * container if necessary.
		 */
		protected MenuEntries.File.FileSource CreateFileSourceEntry (Files.FileSource fileSource)
		   {
			#if DEBUG
			if (FindFileSourceEntry(fileSource) != null)
				{  throw new Exception ("Tried to create a file source entry that already existed in the menu.");  }
			#endif

			if (rootFileMenu == null)
				{
				rootFileMenu = new MenuEntries.Base.Container();
				rootFileMenu.Title = Engine.Locale.Get("NaturalDocs.Engine", "Menu.Files");
				}

		   MenuEntries.File.FileSource fileSourceEntry = new MenuEntries.File.FileSource(fileSource);
		   fileSourceEntry.Parent = rootFileMenu;

		   rootFileMenu.Members.Add(fileSourceEntry);
			return fileSourceEntry;
		   }


		/* Function: FindFileSourceEntry
		 * Returns the menu entry that contains the passed file source, or null if there isn't one yet.
		 */
		protected MenuEntries.File.FileSource FindFileSourceEntry (Files.FileSource fileSource)
			{
			if (rootFileMenu == null)
				{  return null;  }

			// If the menu only had one file source and it was condensed, the root file entry may have been replaced
			// by that file source.
			else if (rootFileMenu is MenuEntries.File.FileSource)
				{
				MenuEntries.File.FileSource fileSourceEntry = (MenuEntries.File.FileSource)rootFileMenu;

				if (fileSourceEntry.WrappedFileSource == fileSource)
					{  return fileSourceEntry;  }
				else
					{  return null;  }
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
						
						if (fileSourceEntry.WrappedFileSource == fileSource)
							{  return fileSourceEntry;  }
						}
					}

				return null;
				}
			}



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