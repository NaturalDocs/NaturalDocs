/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.ImageFolder
 * ____________________________________________________________________________
 * 
 * The configuration of an image folder input target.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config.Targets
	{
	public class ImageFolder : Targets.Input
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		public ImageFolder (PropertyLocation propertyLocation) : base (propertyLocation)
			{
			folder = null;
			folderPropertyLocation = PropertySource.NotDefined;
			}

		public ImageFolder (ImageFolder toCopy) : base (toCopy)
			{
			folder = toCopy.folder;
			folderPropertyLocation = toCopy.folderPropertyLocation;
			}

		override public Input Duplicate ()
			{
			return new ImageFolder(this);
			}

		override public bool IsSameTarget (Input other)
			{
			if ((other is ImageFolder) == false)
				{  return false;  }

			return (folder == (other as ImageFolder).folder);
			}
			
		public override bool Validate (ErrorList errorList, int targetIndex)
			{
			if (System.IO.Directory.Exists(folder) == false)
				{
				errorList.Add( Locale.Get("NaturalDocs.Engine", "Project.txt.ImageFolderDoesNotExist(folder)", folder),
								     folderPropertyLocation,
								     "InputTargets[" + targetIndex + "].Folder" );
				return false;
				}

			return true;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Folder
		 * The <AbsolutePath> that should be searched for image files.
		 */
		public AbsolutePath Folder
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


		/* Property: Type
		 * The type of file source this input target provides.
		 */
		override public Files.InputType Type
			{  
			get
				{  return Files.InputType.Image;  }
			}


		/* Property: TypePropertyLocation
		 * Where <Type> is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		override public PropertyLocation TypePropertyLocation
		    {
		    get
		        {  
				// Same as where the entire property is defined, since it's specified by "Image Folder:".
				return this.PropertyLocation;  
				}
		    }

	
		
		// Group: Variables
		// __________________________________________________________________________
		

		protected AbsolutePath folder;
		protected PropertyLocation folderPropertyLocation;

		}
	}