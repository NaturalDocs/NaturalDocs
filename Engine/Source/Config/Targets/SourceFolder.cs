/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.SourceFolder
 * ____________________________________________________________________________
 * 
 * The configuration of a source folder input target.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config.Targets
	{
	public class SourceFolder : Targets.Input
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		public SourceFolder (PropertyLocation propertyLocation) : base (propertyLocation)
			{
			folder = null;
			folderPropertyLocation = PropertySource.NotDefined;

			this.name = null;
			namePropertyLocation = PropertySource.NotDefined;
			}

		public SourceFolder (SourceFolder toCopy) : base (toCopy)
			{
			folder = toCopy.folder;
			folderPropertyLocation = toCopy.folderPropertyLocation;

			name = toCopy.name;
			namePropertyLocation = toCopy.namePropertyLocation;
			}

		override public Input Duplicate ()
			{
			return new SourceFolder(this);
			}

		override public bool IsSameTarget (Input other)
			{
			if ((other is SourceFolder) == false)
				{  return false;  }

			return (folder == (other as SourceFolder).folder);
			}
			
		public override bool Validate (ErrorList errorList, int targetIndex)
			{
			if (System.IO.Directory.Exists(folder) == false)
				{
				errorList.Add( Locale.Get("NaturalDocs.Engine", "Project.txt.SourceFolderDoesNotExist(folder)", folder),
								     folderPropertyLocation,
								     "InputTargets[" + targetIndex + "].Folder" );
				return false;
				}

			return true;
			}

		public void GenerateDefaultName ()
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

			namePropertyLocation = PropertySource.SystemGenerated;
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


		/* Property: Name
		 * The name of the input target, or null if it isn't defined.  Names are used to distinguish multiple file sources in user-visible
		 * places such as menus.  They should ideally be unique.
		 */
		public string Name
			{
			get
				{  return name;  }
			set
				{  name = value;  }
			}


		/* Property: NamePropertyLocation
		 * Where <Name> is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation NamePropertyLocation
		    {
		    get
		        {  return namePropertyLocation;  }
		    set
		        {  namePropertyLocation = value;  }
		    }


		/* Property: Type
		 * The type of file source this input target provides.
		 */
		override public Files.InputType Type
			{  
			get
				{  return Files.InputType.Source;  }
			}


		/* Property: TypePropertyLocation
		 * Where <Type> is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		override public PropertyLocation TypePropertyLocation
		    {
		    get
		        {  
				// Same as where the entire property is defined, since it's specified by "Source Folder:".
				return this.PropertyLocation;  
				}
		    }


		
		// Group: Variables
		// __________________________________________________________________________
		

		protected string name;
		protected PropertyLocation namePropertyLocation;

		protected Path folder;
		protected PropertyLocation folderPropertyLocation;

		}
	}