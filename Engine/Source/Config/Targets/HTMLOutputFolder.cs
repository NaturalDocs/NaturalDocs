/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.HTMLOutputFolder
 * ____________________________________________________________________________
 * 
 * The configuration of a HTML build target.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config.Targets
	{
	public class HTMLOutputFolder : Targets.Output
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		public HTMLOutputFolder (PropertyLocation propertyLocation) : base (propertyLocation)
			{
			folder = null;
			folderPropertyLocation = PropertySource.NotDefined;
			}

		public HTMLOutputFolder (HTMLOutputFolder toCopy) : base (toCopy)
			{
			folder = toCopy.folder;
			folderPropertyLocation = toCopy.folderPropertyLocation;
			}

		override public Output Duplicate ()
			{
			return new HTMLOutputFolder(this);
			}

		override public bool IsSameTarget (Output other)
			{
			if ((other is HTMLOutputFolder) == false)
				{  return false;  }

			return (folder == (other as HTMLOutputFolder).folder);
			}

		override public bool Validate (ErrorList errorList, int targetIndex)
			{
			if (System.IO.Directory.Exists(folder) == false)
				{  
				errorList.Add(
					Locale.Get("NaturalDocs.Engine", "Project.txt.OutputFolderDoesNotExist(folder)", folder),
					folderPropertyLocation,
					"OutputTargets[" + targetIndex + "].Folder" );

				return false;
				}

			return true;
			}
			
	

		// Group: Properties
		// __________________________________________________________________________


		/* Property: Folder
		 * The <AbsolutePath> where the generated HTML output will go.
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