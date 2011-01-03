/* 
 * Class: GregValure.NaturalDocs.CLI.StatusManagers.FileSearch
 * ____________________________________________________________________________
 * 
 * A class to monitor the file searching stage.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.CLI.StatusManagers
	{
	public class FileSearch : StatusManager
		{
		
		public FileSearch (int updateInterval) : base (updateInterval)
			{
			status = new Engine.Files.AddAllFilesStatus();
			
			lastSourceFilesFound = 0;
			lastSourceFoldersFound = 0;
			}

		public override void Start ()
			{
			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.StartFileSearch")
				);
				
			base.Start();
			}

		protected override void Update (Object sender, System.Timers.ElapsedEventArgs args)
			{
			Engine.Instance.Files.GetAddAllFilesStatus(ref status);
			
			if (lastSourceFilesFound != status.SourceFilesFound || lastSourceFoldersFound != status.SourceFoldersFound)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.FileSearchUpdate(files, folders)", status.SourceFilesFound, status.SourceFoldersFound)
					);
					
				lastSourceFilesFound = status.SourceFilesFound;
				lastSourceFoldersFound = status.SourceFoldersFound;
				}
			}

		public override void End ()
			{
			base.End();
			
			Engine.Instance.Files.GetAddAllFilesStatus(ref status);

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