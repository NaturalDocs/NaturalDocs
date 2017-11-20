/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.FileSources.Folder
 * ____________________________________________________________________________
 * 
 * A file source representing a specific folder on disk.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Files.FileSources
	{
	public class Folder : FileSource
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Folder
		 * Instance constructor.  If the path is relative it will be made absolute using the current working folder.
		 */
		public Folder (Files.Manager manager, Config.Targets.SourceFolder config) : base (manager)
			{
			this.config = config;
			}
			
			
		/* Function: Validate
		 * Makes sure the folder exists and adds an error if not.
		 */
		override public bool Validate (Errors.ErrorList errors)
			{
			if (System.IO.Directory.Exists(Path))
				{  return true;  }
			else
				{
				errors.Add(
					Locale.Get("NaturalDocs.Engine", "Error.FolderDoesntExist(type, name)", Type.ToString().ToLower(), Path)
					);
				
				return false;
				}
			}

			
		/* Function: Contains
		 * Returns whether this folder contains the passed file.
		 */
		override public bool Contains (Path file)
			{
			return Path.Contains(file);
			}
			
			
		/* Function: MakeRelative
		 * Converts the passed absolute path to one relative to this source.  If this source doesn't contain the path, it will
		 * return null.
		 */
		override public Path MakeRelative (Path path)
			{
			if (this.Path.Contains(path))
				{  return path.MakeRelativeTo(this.Path);  }
			else
				{  return null;  }
			}


		/* Function: MakeAbsolute
		 * Converts the passed relative path to an absolute one based on this source.  This may or may not result in a path
		 * that actually maps to an existing file.
		 */
		override public Path MakeAbsolute (Path path)
			{
			return (this.Path + "/" + path);
			}


		/* Function: AddAllFiles
		 * Calls <Files.Manager.AddOrUpdateFile()> for every file in the folder and its subfolders.
		 */
		override public void AddAllFiles (CancelDelegate cancelDelegate)
			{
			addAllFilesStatus.Reset();
			
			// String stack instead of Path stack because the IO functions will return strings and there's no need to normalize
			// them all or otherwise use Path functions on them.
			Stack<string> foldersToSearch = new Stack<string>();
			foldersToSearch.Push(Path);				
			
			while (foldersToSearch.Count > 0)
				{
				string folder = foldersToSearch.Pop();

				if (Type == InputType.Source)
					{
					if (Manager.SourceFolderIsIgnored(folder))
 						{  continue;  }	
					else
						{  addAllFilesStatus.SourceFoldersFound++;  }
					}
				
				string[] subfolders = System.IO.Directory.GetDirectories(folder);
				
				if (cancelDelegate())
					{  return;  }
			
				foreach (string subfolder in subfolders)
					{  foldersToSearch.Push(subfolder);  }

				string[] files = System.IO.Directory.GetFiles(folder);
				
				if (cancelDelegate())
					{  return;  }
					
				// This is deliberately not batched to increase parallelism.  Reading all the file modification times could potentially be
				// a long, IO intensive operation if there are a lot of files in a folder.  It would be more efficient in a single threaded
				// application to put off triggering the change notifications for each one, but in a multithreaded application it's 
				// preventing other file sources from searching and/or parsers from working on the files already found.
			
				foreach (string file in files)
					{
					Path filePath = file;
					string extension = filePath.Extension;
					FileType? type = null;
						
					if (Type == InputType.Source)
						{
						if ( EngineInstance.Languages.FromExtension(extension) != null)
							{  type = FileType.Source;  }
						// We also look for images in the source folders because "(see image.jpg)" may be relative to the source
						// file instead of an image folder.
						else if (Files.Manager.ImageExtensions.Contains(extension) )
							{  type = FileType.Image;  }
						}
					else if (Type == InputType.Image && Files.Manager.ImageExtensions.Contains(extension))
						{  type = FileType.Image;  }

					if (type != null)
						{  
						Manager.AddOrUpdateFile(filePath, (FileType)type, System.IO.File.GetLastWriteTimeUtc(file));

						if (type == FileType.Source)
							{  addAllFilesStatus.SourceFilesFound++;  }
						}

					if (cancelDelegate())
						{  return;  }
					}
				}

			addAllFilesStatus.Completed = true;
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
		public Path Path
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
				{  return config.Type;  }
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