/*
 * Class: CodeClear.NaturalDocs.Engine.Files.FileSources.SourceFolderAdder
 * ____________________________________________________________________________
 *
 * A <FileSourceAdder> that can be used with <FileSources.SourceFolder>.
 *
 *
 * Topic: Usage
 *
 *		- Call <AddAllFiles()>.  This is not a WorkOn function so only a single thread can call it.
 *
 *		- Other threads may check the status with GetStatus().
 *
 *
 * Multithreading: Thread Safety Notes
 *
 *		Externally, this class is thread safe.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Files.FileSources
	{
	public class SourceFolderAdder : FileSourceAdder
		{

		/* Function: SourceFolderAdder
		 */
		public SourceFolderAdder (FileSources.SourceFolder fileSource, Engine.Instance engineInstance) : base (fileSource, engineInstance)
			{
			}

		/* Function: AddAllFiles
		 * Goes through all the files in the <FileSource> and calls <Files.Manager.AddOrUpdateFile()> on each one.
		 */
		override public void AddAllFiles (CancelDelegate cancelDelegate)
			{
			status.Reset();
			bool forceReparse = EngineInstance.HasIssues( StartupIssues.NeedToStartFresh |
																				  StartupIssues.NeedToReparseAllFiles );

			Path path = (FileSource as FileSources.SourceFolder).Path;

			// Using a string stack instead of Path stack because the I/O functions will return strings and there's no need to normalize
			// them all or otherwise use Path functions on them.
			Stack<string> foldersToSearch = new Stack<string>();
			foldersToSearch.Push(path);

			while (foldersToSearch.Count > 0)
				{
				string folder = foldersToSearch.Pop();

				if (Manager.SourceFolderIsIgnored(folder))
						{  continue;  }

				status.AddFolders(InputType.Source, 1);

				string[] subfolders = System.IO.Directory.GetDirectories(folder);

				if (cancelDelegate())
					{  return;  }

				foreach (string subfolder in subfolders)
					{  foldersToSearch.Push(subfolder);  }

				string[] files = System.IO.Directory.GetFiles(folder);

				if (cancelDelegate())
					{  return;  }

				foreach (string file in files)
					{
					AbsolutePath filePath = file;
					string extension = filePath.Extension;
					FileType? fileType = null;

					if ( EngineInstance.Languages.FromFileExtension(extension) != null)
						{  fileType = FileType.Source;  }
					// We also look for images in the source folders because "(see image.jpg)" may be relative to the source
					// file instead of an image folder.
					else if (Files.Manager.ImageExtensions.Contains(extension) )
						{  fileType = FileType.Image;  }

					if (fileType != null)
						{
						status.AddFiles((FileType)fileType, 1);
						Manager.AddOrUpdateFile(filePath, (FileType)fileType, System.IO.File.GetLastWriteTimeUtc(file), forceReparse,
															  (fileType == FileType.Source ? fileSource.CharacterEncodingID(filePath) : 0) );
						}

					if (cancelDelegate())
						{  return;  }
					}
				}
			}

		}
	}
