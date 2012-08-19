/* 
 * Class: GregValure.NaturalDocs.Engine.Output.MenuEntries.File.File
 * ____________________________________________________________________________
 * 
 * Represents a file in <Menu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Output.MenuEntries.File
	{
	public class File : Base.Target
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: File
		 */
		public File (Files.File file) : base ()
			{
			this.file = file;
			this.Title = file.FileName.NameWithoutPath;
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: WrappedFile
		 * The <Files.File> associated with this entry.
		 */
		public Files.File WrappedFile
			{
			get
				{  return file;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Files.File file;

		}
	}