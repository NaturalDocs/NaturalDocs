/*
 * Class: CodeClear.NaturalDocs.CLI.StatusManagers.Parsing
 * ____________________________________________________________________________
 *
 * A class to monitor the file parsing stage.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.CLI.StatusManagers
	{
	public class Parsing : StatusManager
		{

		// Group: Functions
		// __________________________________________________________________________


		public Parsing (Engine.Files.ChangeProcessor process, string alternativeStartMessage = null)
			: base (Application.StatusInterval, acceptsInput: true)
			{
			this.process = process;
			status = new Engine.Files.ChangeProcessorStatus();

			totalFilesToProcess = 0;
			lastPercentage = 0;
			this.alternativeStartMessage = alternativeStartMessage;
			}

		protected override void ShowStartMessage ()
			{
			process.GetStatus(ref status);
			totalFilesToProcess = status.NewOrChangedFilesRemaining + status.DeletedFilesRemaining;

			if (alternativeStartMessage != null)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", alternativeStartMessage)
					);
				}
			else if (status.NewOrChangedFilesRemaining == 0 && status.DeletedFilesRemaining == 0)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.NoChanges")
					);
				}
			else if (status.NewOrChangedFilesRemaining == 0)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.StartFileParsing(deleted)", status.DeletedFilesRemaining)
					);
				}
			else if (status.DeletedFilesRemaining == 0)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.StartFileParsing(changed)", status.NewOrChangedFilesRemaining)
					);
				}
			else
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.StartFileParsing(changed, deleted)",
											 status.NewOrChangedFilesRemaining, status.DeletedFilesRemaining)
					);
				}
			}

		protected override void ShowUpdateMessage ()
			{
			if (totalFilesToProcess == 0)
				{  return;  }

			process.GetStatus(ref status);

			int completed = totalFilesToProcess - status.NewOrChangedFilesRemaining - status.DeletedFilesRemaining - status.FilesBeingProcessed;
			int newPercentage = (completed * 100) / totalFilesToProcess;

			if (newPercentage != lastPercentage)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.FileParsingUpdate(percent)", newPercentage)
					);

				lastPercentage = newPercentage;
				}
			}

		protected override void ShowDetailedStatus()
			{
			Engine.Files.ChangeProcessorDetailedStatus status = new Engine.Files.ChangeProcessorDetailedStatus();
			process.GetStatus(ref status);

			System.Console.WriteLine();


			//
			// Currently processing
			//

			if (status.FileIDsBeingProcessed.Count == 0)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "DetailedStatus.NotProcessingAnyFiles")
					);
				}
			else
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "DetailedStatus.ProcessingFiles(count)", status.FileIDsBeingProcessed.Count)
					);

				foreach (var fileIDBeingProcessed in status.FileIDsBeingProcessed)
					{
					string fileName = null;

					try
						{  fileName = process.EngineInstance.Files.FromID(fileIDBeingProcessed).FileName;  }
					catch
						{  fileName = "File ID " + fileIDBeingProcessed;  }

					System.Console.WriteLine("- " + fileName);
					}

				System.Console.WriteLine();
				}


			//
			// New or changed files waiting
			//

			if (status.NewOrChangedFileIDsRemaining.Count == 0)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "DetailedStatus.NoNewOrChangedFiles")
					);
				}
			else
				{
				int maxCount = 8;

				string messageID = (status.NewOrChangedFileIDsRemaining.Count <= maxCount ?
											  "DetailedStatus.NewOrChangedFilesWaiting.ShowingAll(count)" :
											  "DetailedStatus.NewOrChangedFilesWaiting.ShowingSome(count)");

				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", messageID, status.NewOrChangedFileIDsRemaining.Count)
					);

				int count = maxCount;
				foreach (var newOrChangedFileID in status.NewOrChangedFileIDsRemaining)
					{
					string fileName = null;

					try
						{  fileName = process.EngineInstance.Files.FromID(newOrChangedFileID).FileName;  }
					catch
						{  fileName = "File ID " + newOrChangedFileID;  }

					System.Console.WriteLine("- " + fileName);

					count--;
					if (count == 0)
						{  break;  }
					}

				System.Console.WriteLine();
				}


			// Deleted files waiting

			if (status.DeletedFileIDsRemaining.Count == 0)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "DetailedStatus.NoDeletedFiles")
					);
				}
			else
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "DetailedStatus.DeletedFilesWaiting(count)", status.FileIDsBeingProcessed.Count)
					);
				}

			System.Console.WriteLine();
			}


		// Group: Properties
		// __________________________________________________________________________

		public int TotalFilesToProcess
			{
			get
				{  return totalFilesToProcess;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Engine.Files.ChangeProcessor process;
		protected Engine.Files.ChangeProcessorStatus status;

		protected int totalFilesToProcess;
		protected int lastPercentage;
		protected string alternativeStartMessage;

		}
	}
