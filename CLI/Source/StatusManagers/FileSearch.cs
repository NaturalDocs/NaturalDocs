/* 
 * Class: CodeClear.NaturalDocs.CLI.StatusManagers.FileSearch
 * ____________________________________________________________________________
 * 
 * A class to monitor the file searching stage.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.CLI.StatusManagers
	{
	public class FileSearch : StatusManager
		{

		// Group: Functions
		// __________________________________________________________________________

		
		public FileSearch () : base (Application.StatusInterval)
			{
			status = new Engine.Files.AddAllFilesStatus();
			
			lastSourceFilesFound = 0;
			lastSourceFoldersFound = 0;
			}

		protected override void ShowStartMessage ()
			{
			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.StartFileSearch")
				);
			}

		protected override void ShowUpdateMessage ()
			{
			Application.EngineInstance.Files.GetAddAllFilesStatus(ref status);
			
			if (lastSourceFilesFound != status.SourceFilesFound || lastSourceFoldersFound != status.SourceFoldersFound)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.FileSearchUpdate(files, folders)", status.SourceFilesFound, status.SourceFoldersFound)
					);
					
				lastSourceFilesFound = status.SourceFilesFound;
				lastSourceFoldersFound = status.SourceFoldersFound;
				}
			}

		protected override void ShowEndMessage ()
			{
			Application.EngineInstance.Files.GetAddAllFilesStatus(ref status);

			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.EndFileSearch(files, folders)", status.SourceFilesFound, status.SourceFoldersFound)
				);
			}
			
			
		// Group: Variables
		// __________________________________________________________________________
		
		Engine.Files.AddAllFilesStatus status;
		
		int lastSourceFoldersFound;
		int lastSourceFilesFound;
		
		}
	}