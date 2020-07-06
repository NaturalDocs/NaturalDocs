/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.MenuEntries.File
 * ____________________________________________________________________________
 * 
 * Represents a file in <Menu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.MenuEntries
	{
	public class File : Entry
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: File
		 */
		public File (Engine.Files.File file) : base ()
			{
			this.file = file;
			this.Title = file.FileName.NameWithoutPath;
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: WrappedFile
		 * The <Engine.Files.File> associated with this entry.
		 */
		public Engine.Files.File WrappedFile
			{
			get
				{  return file;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Engine.Files.File file;

		}
	}