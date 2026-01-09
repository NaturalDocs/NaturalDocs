/*
 * Class: CodeClear.NaturalDocs.CLI.StatusManagers.FileSearch
 * ____________________________________________________________________________
 *
 * A class to monitor the file searching stage.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.CLI.StatusManagers
	{
	public class FileSearch : StatusManager
		{

		// Group: Functions
		// __________________________________________________________________________


		public FileSearch (Engine.Files.Adder process) : base (Application.StatusInterval)
			{
			this.process = process;
			status = new Engine.Files.AdderStatus();

			lastSourceFilesFound = 0;
			lastSourceFoldersFound = 0;

			firstLinePositionLeft = 0;
			firstLinePositionTop = 0;

			secondLinePositionLeft = 0;
			secondLinePositionTop = 0;
			}

		protected override void ShowStartMessage ()
			{
			System.Console.Write(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.StartFileSearch")
				);

			if (!Application.SimpleOutput)
				{
				System.Console.Write(' ');
				firstLinePositionLeft = System.Console.CursorLeft;
				firstLinePositionTop = System.Console.CursorTop;
				}

			System.Console.WriteLine();

			if (!Application.SimpleOutput)
				{
				secondLinePositionLeft = System.Console.CursorLeft;
				secondLinePositionTop = System.Console.CursorTop;

				// Check whether the second line caused the terminal window to scroll, in which case we have to adjust the
				// coordinates of the first
				if (secondLinePositionTop == firstLinePositionTop)
					{  firstLinePositionTop--;  }
				}
			}

		protected override void ShowUpdateMessage ()
			{
			process.GetStatus(ref status);

			if (lastSourceFilesFound != status.SourceFilesFound || lastSourceFoldersFound != status.SourceFoldersFound)
				{
				if (Application.SimpleOutput)
					{
					System.Console.WriteLine(
						Engine.Locale.Get("NaturalDocs.CLI", "Status.SimpleOutput.UpdateNumberFound(files, folders)", status.SourceFilesFound, status.SourceFoldersFound)
						);
					}
				else
					{
					System.Console.CursorLeft = secondLinePositionLeft;
					System.Console.CursorTop = secondLinePositionTop;

					System.Console.Write(Application.SecondaryStatusIndent);

					System.Console.Write(
						Engine.Locale.Get("NaturalDocs.CLI", "Status.NumberFound(files, folders)", status.SourceFilesFound, status.SourceFoldersFound)
						);
					}

				lastSourceFilesFound = status.SourceFilesFound;
				lastSourceFoldersFound = status.SourceFoldersFound;
				}
			}

		protected override void ShowEndMessage ()
			{
			process.GetStatus(ref status);

			if (!Application.SimpleOutput)
				{
				System.Console.CursorLeft = firstLinePositionLeft;
				System.Console.CursorTop = firstLinePositionTop;

				System.Console.Write(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.End")
					);

				System.Console.CursorLeft = secondLinePositionLeft;
				System.Console.CursorTop = secondLinePositionTop;

				System.Console.Write(Application.SecondaryStatusIndent);
				}

			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.NumberFound(files, folders)", status.SourceFilesFound, status.SourceFoldersFound)
				);
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Engine.Files.Adder process;
		protected Engine.Files.AdderStatus status;

		protected int lastSourceFoldersFound;
		protected int lastSourceFilesFound;

		protected int firstLinePositionLeft;
		protected int firstLinePositionTop;

		protected int secondLinePositionLeft;
		protected int secondLinePositionTop;

		}
	}
