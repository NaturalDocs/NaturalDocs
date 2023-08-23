/*
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.ImageFolder
 * ____________________________________________________________________________
 *
 * The configuration of an image folder input target.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
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


		public ImageFolder (PropertyLocation propertyLocation) : base (Files.InputType.Image, propertyLocation)
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
