﻿/*
 * Class: CodeClear.NaturalDocs.CLI.StatusManagers.FileSearch
 * ____________________________________________________________________________
 *
 * A class to monitor the file searching stage.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
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
			}

		protected override void ShowStartMessage ()
			{
			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.StartFileSearch")
				);
			}

		protected override void ShowUpdateMessage ()
			{
			process.GetStatus(ref status);

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
			process.GetStatus(ref status);

			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.EndFileSearch(files, folders)", status.SourceFilesFound, status.SourceFoldersFound)
				);
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Engine.Files.Adder process;
		protected Engine.Files.AdderStatus status;

		protected int lastSourceFoldersFound;
		protected int lastSourceFilesFound;

		}
	}
