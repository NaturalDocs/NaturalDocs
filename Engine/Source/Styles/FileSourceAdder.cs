/* 
 * Class: CodeClear.NaturalDocs.Engine.Styles.FileSourceAdder
 * ____________________________________________________________________________
 * 
 * A <Files.FileSourceAdder> that can be used with <Output.Styles.FileSource>.
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

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Styles
	{
	public class FileSourceAdder : Files.FileSourceAdder
		{
		
		/* Function: FileSourceAdder
		 */
		public FileSourceAdder (Styles.FileSource fileSource, Engine.Instance engineInstance) : base (fileSource, engineInstance)
			{
			}
			
		/* Function: AddAllFiles
		 * Goes through all the files for all the used styles and calls <Files.Manager.AddOrUpdateFile()> on each one.
		 */
		override public void AddAllFiles (CancelDelegate cancelDelegate)
			{
			status.Reset();

			Styles.FileSource stylesFileSource = (Styles.FileSource)FileSource;
			IList<Style> styles = stylesFileSource.Styles;

			// String stack instead of Path stack because the IO functions will return strings and there's no need to normalize
			// them all or otherwise use Path functions on them.
			Stack<string> foldersToSearch = new Stack<string>();
			foldersToSearch.Push(EngineInstance.Config.SystemStyleFolder);
			foldersToSearch.Push(EngineInstance.Config.ProjectConfigFolder);
			status.AddFolders(Files.InputType.Style, 2);
			
			while (foldersToSearch.Count > 0)
			   {
			   string folder = foldersToSearch.Pop();
			   string[] subfolders = System.IO.Directory.GetDirectories(folder);
			   status.AddFolders(Files.InputType.Style, subfolders.Length);
				
			   if (cancelDelegate())
			      {  return;  }
			
			   foreach (string subfolder in subfolders)
			      {  foldersToSearch.Push(subfolder);  }

			   string[] files = System.IO.Directory.GetFiles(folder);
				
			   if (cancelDelegate())
			      {  return;  }

			   foreach (string file in files)
			      {
			      if (cancelDelegate())
			         {  return;  }

					Path filePath = file;

					foreach (Style style in styles)
						{
						if (style.Contains(filePath))
							{  
							status.AddFiles(Files.FileType.Style, 1);
							Manager.AddOrUpdateFile(filePath, Files.FileType.Style, System.IO.File.GetLastWriteTimeUtc(file), 
																  stylesFileSource.ForceReparse);
							break;
							}
						}
					}
				}
			}
			
		}
	}