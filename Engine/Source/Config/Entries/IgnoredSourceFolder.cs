/* 
 * Class: GregValure.NaturalDocs.Engine.Config.Entries.IgnoredSourceFolder
 * ____________________________________________________________________________
 * 
 * An <Entry> for ignored folders.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Config.Entries
	{
	public class IgnoredSourceFolder : FilterEntry
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		public IgnoredSourceFolder (Path folder) : base ()
			{
			this.folder = folder;

			if (folder.IsRelative)
				{  throw new Exception("IgnoredSourceFolder entry must use absolute paths.");  }
			}

			
		// Group: Properties
		// __________________________________________________________________________

		
		/* Property: Folder
		 * The absolute <Path> to the ignored folder.
		 */
		public Path Folder
			{
			get
				{  return folder;  }
			}
			

		// Group: Variables
		// __________________________________________________________________________		
		
		protected Path folder;

		}
	}