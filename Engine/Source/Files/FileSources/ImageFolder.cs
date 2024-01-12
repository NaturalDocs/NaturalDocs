/*
 * Class: CodeClear.NaturalDocs.Engine.Files.FileSources.ImageFolder
 * ____________________________________________________________________________
 *
 * A file source representing a specific image folder on disk.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Globalization;


namespace CodeClear.NaturalDocs.Engine.Files.FileSources
	{
	public class ImageFolder : FileSources.Folder
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: ImageFolder
		 */
		public ImageFolder (Files.Manager manager, Config.Targets.ImageFolder config) : base (manager)
			{
			this.config = config;
			}



		// Group: Processes
		// __________________________________________________________________________


		/* Function: CreateAdderProcess
		 * Returns a <FileSourceAdder> that can be used with this FileSource.
		 */
		override public FileSourceAdder CreateAdderProcess()
			{
			return new ImageFolderAdder(this, EngineInstance);
			}



		// Group: Properties
		// __________________________________________________________________________

		/* Property: UniqueIDString
		 * A string that uniquely identifies this FileSource among all others of its <Type>, including FileSources based on other
		 * classes.
		 */
		override public string UniqueIDString
			{
			get
				{
				if (SystemInfo.IgnoreCaseInPaths)
					{  return "ImageFolder:" + config.Folder.ToString().ToLower(CultureInfo.InvariantCulture);  }
				else
					{  return "ImageFolder:" + config.Folder;  }
				}
			}

		/* Property: Path
		 * The path to the FileSource's folder.
		 */
		override public Path Path
			{
			get
				{  return config.Folder;  }
			}

		/* Property: Type
		 * The type of files this FileSource provides.
		 */
		override public InputType Type
			{
			get
				{  return InputType.Image;  }
			}

		/* Property: Number
		 * The number assigned to this FileSource.
		 */
		override public int Number
			{
			get
				{  return config.Number;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		protected Config.Targets.ImageFolder config;

		}
	}
