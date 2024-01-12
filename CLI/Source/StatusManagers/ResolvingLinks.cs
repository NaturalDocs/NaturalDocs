/*
 * Class: CodeClear.NaturalDocs.CLI.StatusManagers.ResolvingLinks
 * ____________________________________________________________________________
 *
 * A class to monitor the link resolving stage.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.CLI.StatusManagers
	{
	public class ResolvingLinks : StatusManager
		{

		// Group: Functions
		// __________________________________________________________________________


		public ResolvingLinks (Engine.Links.Resolver process) : base (Application.StatusInterval)
			{
			this.process = process;
			status = new Engine.Links.ResolverStatus();

			lastPercentage = 0;
			totalChanges = 0;
			}

		protected override void ShowStartMessage ()
			{
			process.GetStatus(ref status);
			totalChanges = status.ChangesBeingProcessed + status.ChangesRemaining;

			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.StartLinkResolving")
				);
			}

		protected override void ShowUpdateMessage ()
			{
			// Prevent divide by zero when calculating a percentage
			if (totalChanges == 0)
				{  return;  }

			process.GetStatus(ref status);

			int changesComplete = totalChanges - status.ChangesRemaining - status.ChangesBeingProcessed;
			int newPercentage = (int)((100 * changesComplete) / totalChanges);

			// Sanity check.  We use > instead of != because we don't want the percentage to ever go down.  It's better
			// for the percentage to just stall until it catches up again as that's less confusing to the user.
			if (newPercentage > lastPercentage)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.LinkResolvingUpdate(percent)", newPercentage)
					);

				lastPercentage = newPercentage;
				}
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Engine.Links.Resolver process;
		protected Engine.Links.ResolverStatus status;

		protected int lastPercentage;
		protected int totalChanges;

		}
	}
