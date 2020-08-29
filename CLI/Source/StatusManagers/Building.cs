/* 
 * Class: CodeClear.NaturalDocs.CLI.StatusManagers.Building
 * ____________________________________________________________________________
 * 
 * A class to monitor the output building stage.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.CLI.StatusManagers
	{
	public class Building : StatusManager
		{
		
		// Group: Functions
		// __________________________________________________________________________


		public Building (Engine.Output.Builder builderProcess) : base (Application.StatusInterval)
			{
			this.builderProcess = builderProcess;

			totalWork = 0;
			lastPercentageDone = 0;
			}

		protected override void ShowStartMessage ()
			{
			long workInProgress, workRemaining;
			builderProcess.GetStatus(out workInProgress, out workRemaining);

			totalWork = workInProgress + workRemaining;

			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.StartOutputBuilding")
				);
			}

		protected override void ShowUpdateMessage ()
			{
			// Don't want to divide by zero
			if (totalWork == 0)
				{  return;  }

			long workInProgress, workRemaining;
			builderProcess.GetStatus(out workInProgress, out workRemaining);

			// We don't want to count work in progress until it's done.
			workRemaining += workInProgress;

			// Sanity check since as it runs a builder can add tasks to the list of things it needs to do.
			if (workRemaining > totalWork)
				{  workRemaining = totalWork;  }

			long workDone = totalWork - workRemaining;
			int newPercentage = (int)((100 * workDone) / totalWork);
			
			// Another sanity check.  We use > instead of != because we don't want the percentage to ever go down.  It's better
			// for the percentage to just stall until it catches up again as that's less confusing to the user.
			if (newPercentage > lastPercentageDone)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.OutputBuildingUpdate(percent)", newPercentage)
					);
					
				lastPercentageDone = newPercentage;
				}
			}


		// Group: Variables
		// __________________________________________________________________________
		
		protected Engine.Output.Builder builderProcess;

		protected long totalWork;
		protected int lastPercentageDone;

		}
	}