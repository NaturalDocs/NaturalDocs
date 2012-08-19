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



		// Group: Variables
		// __________________________________________________________________________

		/* var: fileSource
		 */
		protected Files.FileSource fileSource;

		}
	}