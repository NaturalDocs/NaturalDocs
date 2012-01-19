/* 
 * Class: GregValure.NaturalDocs.Engine.Config.Entries.HTMLOutputFolder
 * ____________________________________________________________________________
 * 
 * An <Entry> for a HTML output folder.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Config.Entries
	{
	public class HTMLOutputFolder : OutputEntry
		{
		
		public HTMLOutputFolder (Path folder, Path configFile = default(Path), int lineNumber = -1) : base (folder, configFile, lineNumber)
			{
			}

		public override bool Validate(Errors.ErrorList errorList)
			{
			if (System.IO.Directory.Exists(folder) == false)
				{  
				errorList.Add( Locale.Get("NaturalDocs.Engine", "Project.txt.OutputFolderDoesNotExist(folder)", folder), configFile, lineNumber );  
				return false;
				}

			return true;
			}

		public override bool IsSameFundamentalEntry (Entry other)
		    {
		    return ( other is HTMLOutputFolder && 
		               (other as HTMLOutputFolder).Folder == Folder );
		    }

		public override void CopyUnsetPropertiesFrom (Entry other)
			{
			if (Number == 0)
				{  Number = (other as HTMLOutputFolder).Number;  }
			}

		}
	}