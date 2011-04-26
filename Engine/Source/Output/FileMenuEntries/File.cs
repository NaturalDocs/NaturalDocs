/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileMenuEntries.File
 * ____________________________________________________________________________
 * 
 * Represents a file in a <FileMenu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Output.FileMenuEntries
	{
	public class File : Entry
		{

		// Group: Functions
		// __________________________________________________________________________

		public File (Path newFileName) : base ()
			{
			fileName = newFileName;
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: FileName
		 * The name of the file.  Does not include the full path, only the file name.
		 */
		public string FileName
			{
			get
				{  return fileName;  }
			}

		/* Property: SortString
		 * Returns the string that should be used to sort this entry in a list.
		 */
		override public string SortString
			{  
			get
				{  return fileName;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Path fileName;
		}
	}