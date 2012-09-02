/* 
 * Class: GregValure.NaturalDocs.Engine.Output.MenuEntries.File.FileSource
 * ____________________________________________________________________________
 * 
 * A container that represents a <Files.FileSource> in <Menu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Output.MenuEntries.File
	{
	public class FileSource : Base.Container
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: FileSource
		 */
		public FileSource (Files.FileSource fileSource) : base ()
			{
			this.fileSource = fileSource;

			Title = fileSource.Name;  // May be null
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

				condensedPathFromFileSource = subFolder.PathFromFileSource;
				}
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

		/* Property: CondensedPathFromFileSource
		 * If this file source had a folder condensed into it, this will be the relative path from the file source to that 
		 * folder.
		 */
		public Path CondensedPathFromFileSource
			{
			get
				{  return condensedPathFromFileSource;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		/* var: fileSource
		 */
		protected Files.FileSource fileSource;

		/* var: condensedPathFromFileSource
		 */
		protected Path condensedPathFromFileSource;
		}
	}