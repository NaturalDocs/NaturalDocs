/* 
 * Class: GregValure.NaturalDocs.Engine.Config.Entries.InputFolder
 * ____________________________________________________________________________
 * 
 * An <Entry> for a source or image input folder.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Config.Entries
	{
	public class InputFolder : InputEntry
		{
		
		// Group: Functions
		// __________________________________________________________________________

		
		public InputFolder (Path folder, Files.InputType inputType) : base (inputType)
			{
			this.folder = folder;

			if (folder.IsRelative)
				{  throw new Exception("InputFolder entry must use absolute paths.");  }
			}

		public override bool IsSameFundamentalEntry (Entry other)
		    {
		    return ( other is InputFolder && 
		               ((InputFolder)other).InputType == InputType &&
		               ((InputFolder)other).Folder == Folder );
		    }

		public override void CopyUnsetPropertiesFrom (Entry other)
		    {
		    InputFolder inputFolderEntry = (InputFolder)other;
			
		    if (InputType == Files.InputType.Source && Name == null)
		        {  Name = inputFolderEntry.Name;  }
		    if (Number == 0)
		        {  Number = inputFolderEntry.Number;  }
		    }

		public override void GenerateDefaultName ()
			{
			string prefix;
			List<string> segments;
			
			folder.Split(out prefix, out segments);
			
			if (segments.Count > 0)
				{  
				// The name gets set to the last segment by default.  However, we also walk down the list to see if there are any
				// that don't match the ignored segment regex.  If there is, we use that instead.
				int nameIndex = segments.Count - 1;
				Name = segments[nameIndex];

				Regex.Config.DefaultNameIgnoredSegment ignoredSegmentRegex = new Regex.Config.DefaultNameIgnoredSegment();
				
				while (nameIndex >= 0 && ignoredSegmentRegex.IsMatch(segments[nameIndex]))
					{  nameIndex--;  }
					
				if (nameIndex >= 0)
					{  Name = segments[nameIndex];  }
				}
			else
				{  Name = folder;  }
			}
		

		
		// Group: Properties
		// __________________________________________________________________________
		
		/* Property: Folder
		 * The absolute <Path> to the input folder.
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