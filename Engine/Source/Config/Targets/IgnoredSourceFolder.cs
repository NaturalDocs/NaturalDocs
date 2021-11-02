/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.IgnoredSourceFolder
 * ____________________________________________________________________________
 * 
 * The configuration of an ignored source folder.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Config.Targets
	{
	public class IgnoredSourceFolder : Targets.Filter
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		public IgnoredSourceFolder (PropertyLocation propertyLocation) : base (propertyLocation)
			{
			folder = null;
			folderPropertyLocation = PropertySource.NotDefined;
			}

		public IgnoredSourceFolder (IgnoredSourceFolder toCopy) : base (toCopy)
			{
			folder = toCopy.folder;
			folderPropertyLocation = toCopy.folderPropertyLocation;
			}

		public override Filter Duplicate ()
			{
			return new IgnoredSourceFolder(this);
			}

		public override bool Validate (Errors.ErrorList errorList, int targetIndex)
			{
			if (System.IO.Directory.Exists(folder) == false)
				{  
				errorList.Add( 
					Locale.Get("NaturalDocs.Engine", "Project.txt.IgnoredSourceFolderDoesNotExist(folder)", folder),
					folderPropertyLocation,
					"FilterTargets[" + targetIndex + "].Folder" );

				return false;
				}

			return true;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Folder
		 * The <AbsolutePath> that should have its contents ignored.
		 */
		public AbsolutePath Folder
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
		

		protected AbsolutePath folder;
		protected PropertyLocation folderPropertyLocation;

		}
	}