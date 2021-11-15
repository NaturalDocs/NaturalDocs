/* 
 * Class: CodeClear.NaturalDocs.Engine.Styles.FileSourceAdder
 * ____________________________________________________________________________
 * 
 * A <Files.FileSourceAdder> that can be used with <Styles.FileSource>.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


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
		 * Goes through all the files for all the loaded styles in <Styles.Manager> and calls <Files.Manager.AddOrUpdateFile()>
		 * on each one.
		 */
		override public void AddAllFiles (CancelDelegate cancelDelegate)
			{
			status.Reset();

			IList<Style> styles = EngineInstance.Styles.LoadedStyles;
			bool forceReparse = EngineInstance.HasIssues( StartupIssues.NeedToStartFresh |
																				  StartupIssues.NeedToReparseAllFiles |
																				  StartupIssues.NeedToReparseStyleFiles );

			// String stack instead of Path stack because the IO functions will return strings and there's no need to normalize
			// them all or otherwise use Path functions on them.
			Stack<string> foldersToSearch = new Stack<string>();

			foreach (var style in styles)
				{
				if (style is Styles.CSSOnly)
					{
					AbsolutePath cssFile = (style as Styles.CSSOnly).CSSFile;

					status.AddFiles(Files.FileType.Style, 1);

					EngineInstance.Files.AddOrUpdateFile(cssFile, Files.FileType.Style, 
																			System.IO.File.GetLastWriteTimeUtc(cssFile), forceReparse);
					}

				else if (style is Styles.Advanced)
					{
					foldersToSearch.Clear();
					foldersToSearch.Push((style as Styles.Advanced).Folder);

					status.AddFolders(Files.InputType.Style, 1);

					while (foldersToSearch.Count > 0)
						{
						string folder = foldersToSearch.Pop();

						string[] subfolders = System.IO.Directory.GetDirectories(folder);
				   
						status.AddFolders(Files.InputType.Style, subfolders.Length);
				
						foreach (string subfolder in subfolders)
							{  foldersToSearch.Push(subfolder);  }

						if (cancelDelegate())
							{  return;  }

						string[] files = System.IO.Directory.GetFiles(folder);
				
						foreach (string file in files)
							{
							if (style.Contains(file))
								{  
								status.AddFiles(Files.FileType.Style, 1);

								EngineInstance.Files.AddOrUpdateFile(file, Files.FileType.Style, 
																						System.IO.File.GetLastWriteTimeUtc(file), forceReparse);

								if (cancelDelegate())
									{  return;  }
								}
							}
						}
					}

				else
					{  throw new NotImplementedException();  }
				}

			}
			
		}
	}