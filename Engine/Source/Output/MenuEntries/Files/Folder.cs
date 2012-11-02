/* 
 * Class: GregValure.NaturalDocs.Engine.Output.MenuEntries.Files.Folder
 * ____________________________________________________________________________
 * 
 * Represents a folder or group of folders in a <Menu>.  It will only represent a group of folders
 * ("FolderA/FolderB") if the parent folder contains nothing other than the child folder.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Output.MenuEntries.Files
	{
	public class Folder : Base.Container
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: Folder
		 */
		public Folder (Path pathFromFileSource) : base ()
			{
			this.pathFromFileSource = pathFromFileSource;
			this.Title = pathFromFileSource.NameWithoutPath;
			}

		override public void Condense ()
			{
			CondenseContainersInMembers();

			if (Members.Count == 1 && Members[0] is Folder)
				{
				Folder subFolder = (Members[0] as Folder);

				Members = subFolder.Members;

				if (CondensedTitles == null)
					{  CondensedTitles = new List<string>();  }

				CondensedTitles.Add(subFolder.Title);

				if (subFolder.CondensedTitles != null)
					{  CondensedTitles.AddRange(subFolder.CondensedTitles);  }

				pathFromFileSource = subFolder.pathFromFileSource;
				}
			}



		// Group: Properties
		// __________________________________________________________________________

		/* Property: PathFromFileSource
		 * The relative path from the file source to the folder associated with this entry.  The path will otherwise be
		 * complete so you don't have to combine it with parent paths, only with the file source if you want to get an
		 * absolute path.
		 */
		public Path PathFromFileSource
			{
			get
				{  return pathFromFileSource;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		/* var: pathFromFileSource
		 */
		protected Path pathFromFileSource;

		}
	}