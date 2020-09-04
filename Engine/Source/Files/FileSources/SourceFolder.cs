/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.FileSources.SourceFolder
 * ____________________________________________________________________________
 * 
 * A file source representing a specific source folder on disk.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files.FileSources
	{
	public class SourceFolder : FileSources.Folder
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: SourceFolder
		 */
		public SourceFolder (Files.Manager manager, Config.Targets.SourceFolder config) : base (manager)
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
			return new SourceFolderAdder(this, EngineInstance);
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
				{  return "Folder:" + config.Folder;  }
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
				{  return InputType.Source;  }
			}
			
		/* Property: Number
		 * The number assigned to this FileSource.
		 */
		override public int Number
			{
			get
				{  return config.Number;  }
			}
						
		/* Property: Name
		 * The name assigned to this FileSource, or null if one hasn't been set.
		 */
		override public string Name
			{
			get
				{  return config.Name;  }
			}



		// Group: Variables
		// __________________________________________________________________________
			
		protected Config.Targets.SourceFolder config;

		}
	}