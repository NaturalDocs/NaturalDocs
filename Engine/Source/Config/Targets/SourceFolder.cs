/* 
 * Class: GregValure.NaturalDocs.Engine.Config.Targets.SourceFolder
 * ____________________________________________________________________________
 * 
 * The configuration of a source folder input target.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Config;
using GregValure.NaturalDocs.Engine.Errors;


namespace GregValure.NaturalDocs.Engine.Config.Targets
	{
	public class SourceFolder : InputBase
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		public SourceFolder (PropertyLocation propertyLocation, Files.InputType type) : base (propertyLocation, type)
			{
			folder = null;
			folderPropertyLocation = Source.NotDefined;
			}

		public SourceFolder (SourceFolder toCopy) : base (toCopy)
			{
			folder = toCopy.folder;
			folderPropertyLocation = toCopy.folderPropertyLocation;
			}

		override public InputBase Duplicate ()
			{
			return new SourceFolder(this);
			}

		override public bool IsSameTarget (InputBase other)
			{
			if ((other is SourceFolder) == false)
				{  return false;  }

			return (folder == (other as SourceFolder).folder);
			}
			
		public override bool Validate (ErrorList errorList)
			{
			if (System.IO.Directory.Exists(folder) == false)
				{
				string key = "Project.txt.SourceFolderDoesNotExist(folder)";

				if (type == Files.InputType.Image)
					{  key = "Project.txt.ImageFolderDoesNotExist(folder)";  }

				errorList.Add( Locale.Get("NaturalDocs.Engine", key, folder) );
				return false;
				}

			return true;
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
				name = segments[nameIndex];

				Regex.Config.DefaultNameIgnoredSegment ignoredSegmentRegex = new Regex.Config.DefaultNameIgnoredSegment();
				
				while (nameIndex >= 0 && ignoredSegmentRegex.IsMatch(segments[nameIndex]))
					{  nameIndex--;  }
					
				if (nameIndex >= 0)
					{  name = segments[nameIndex];  }
				else
					{  name = segments[segments.Count - 1];  }
				}
			else
				{  name = folder;  }

			namePropertyLocation = Source.SystemGenerated;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Folder
		 * The <Path> that should be searched for source files.
		 */
		public Path Folder
		    {
		    get
		        {  return folder;  }
			set
				{  folder = value;  }
		    }


		
		// Group: Property Locations
		// __________________________________________________________________________
		
					
		/* Property: FolderPropertyLocation
		 * Where <Folder> is defined.
		 */
		public PropertyLocation FolderPropertyLocation
		    {
		    get
		        {  return folderPropertyLocation;  }
		    set
		        {  folderPropertyLocation = value;  }
		    }

	
		
		// Group: Variables
		// __________________________________________________________________________
		

		protected Path folder;

		protected PropertyLocation folderPropertyLocation;

		}
	}