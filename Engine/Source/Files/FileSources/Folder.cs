/* 
 * Class: GregValure.NaturalDocs.Engine.Files.FileSources.Folder
 * ____________________________________________________________________________
 * 
 * A file source representing a specific folder on disk.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Files.FileSources
	{
	public class Folder : FileSource
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Folder
		 * Instance constructor.  If the path is relative it will be made absolute using the current working folder.
		 */
		public Folder (Config.Entries.InputFolder config) : base ()
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
		 * If the passed absolute <Path> is contained by this folder, returns a relative path to it.  Otherwise returns null.
		 */
		override public Path MakeRelative (Path file)
			{
			if (Path.Contains(file))
				{  return Path.MakeRelative(file);  }
			else
				{  return null;  }
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
					if (Engine.Instance.Files.SourceFolderIsIgnored(folder))
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
						if ( Engine.Instance.Languages.FromExtension(extension) != null)
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
						Engine.Instance.Files.AddOrUpdateFile(filePath, (FileType)type, System.IO.File.GetLastWriteTimeUtc(file));

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


		/* Property: Type
		 * The type of files this FileSource provides.
		 */
		override public InputType Type
			{
			get 
				{  return config.InputType;  }
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

		public Path Path
			{
			get 
				{  return config.Folder;  }
			}



		// Group: Variables
		// __________________________________________________________________________
			

		protected Config.Entries.InputFolder config;

		}
	}