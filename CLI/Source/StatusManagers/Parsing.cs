/* 
 * Class: GregValure.NaturalDocs.CLI.StatusManagers.Parsing
 * ____________________________________________________________________________
 * 
 * A class to monitor the file parsing stage.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.CLI.StatusManagers
	{
	public class Parsing : StatusManager
		{
		
		public Parsing (int updateInterval) : base (updateInterval)
			{
			status = new Engine.Files.ProcessChangesStatus();
			totalFilesToProcess = 0;
			lastPercentage = 0;
			}

		public override void Start ()
			{
			NaturalDocs.Engine.Instance.Files.GetProcessChangesStatus(ref status);
			totalFilesToProcess = status.ChangedFilesRemaining + status.DeletedFilesRemaining;
			
			if (status.ChangedFilesRemaining == 0)
				{			
				if (status.DeletedFilesRemaining == 0)
					{
					System.Console.WriteLine(
						Engine.Locale.Get("NaturalDocs.CLI", "Status.NoChanges")
						);
					}
				else
					{
					System.Console.WriteLine(
						Engine.Locale.Get("NaturalDocs.CLI", "Status.StartFileParsing(deleted)", status.DeletedFilesRemaining)
						);
					}
				}
			else if (status.DeletedFilesRemaining == 0)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.StartFileParsing(changed)", status.ChangedFilesRemaining)
					);
				}
			else
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.StartFileParsing(changed, deleted)", 
											 status.ChangedFilesRemaining, status.DeletedFilesRemaining)
					);
				}
				
			base.Start();
			}

		protected override void Update (Object sender, System.Timers.ElapsedEventArgs args)
			{
			if (totalFilesToProcess == 0)
				{  return;  }
				
			Engine.Instance.Files.GetProcessChangesStatus(ref status);
			
			int completed = totalFilesToProcess - status.ChangedFilesRemaining - status.DeletedFilesRemaining - status.FilesBeingProcessed;
			int newPercentage = (completed * 100) / totalFilesToProcess;
			
			if (newPercentage != lastPercentage)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.FileParsingUpdate(percent)", newPercentage)
					);
					
				lastPercentage = newPercentage;
				}
			}

		public override void End ()
			{
			base.End();

			if (totalFilesToProcess > 0)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.EndFileParsing")
					);
				}
			}
			
			
		// Group: Variables
		// __________________________________________________________________________
		
		protected Engine.Files.ProcessChangesStatus status;
		protected int totalFilesToProcess;
		protected int lastPercentage;
		
		}
	}