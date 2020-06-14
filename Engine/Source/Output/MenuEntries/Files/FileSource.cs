/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.MenuEntries.Files.FileSource
 * ____________________________________________________________________________
 * 
 * A container that represents a <Files.FileSource> in <Menu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Output.MenuEntries.Files
	{
	public class FileSource : Base.Container
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: FileSource
		 */
		public FileSource (Engine.Files.FileSource fileSource) : base ()
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

				foreach (var member in Members)
					{  member.Parent = this;  }

				// We ignore any titles it had, condensed or otherwise, to get rid of unnecessary subfolders.
				// We want to go straight into the topmost folder that had content.

				condensedPathFromFileSource = subFolder.PathFromFileSource;
				}
			}


		// Group: Properties
		// __________________________________________________________________________


		/* Property: WrappedFileSource
		 * The <Engine.Files.FileSource> associated with this entry.
		 */
		public Engine.Files.FileSource WrappedFileSource
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
		protected Engine.Files.FileSource fileSource;

		/* var: condensedPathFromFileSource
		 */
		protected Path condensedPathFromFileSource;
		}
	}