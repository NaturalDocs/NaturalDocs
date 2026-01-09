/*
 * Class: CodeClear.NaturalDocs.CLI.StatusManagers.Building
 * ____________________________________________________________________________
 *
 * A class to monitor the output building stage.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
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

			percentagePositionLeft = 0;
			percentagePositionTop = 0;
			}

		protected override void ShowStartMessage ()
			{
			long workInProgress, workRemaining;
			builderProcess.GetStatus(out workInProgress, out workRemaining);

			totalWork = workInProgress + workRemaining;

			System.Console.Write(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.StartOutputBuilding")
				);

			if (Application.SimpleOutput)
				{  System.Console.WriteLine();  }
			else
				{
				System.Console.Write(' ');
				percentagePositionLeft = System.Console.CursorLeft;
				percentagePositionTop = System.Console.CursorTop;
				}
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

			// Never display "100%" in an update message.  Reserve that for the end message.
			if (newPercentage == 100)
				{  newPercentage = 99;  }

			// Another sanity check.  We use > instead of != because we don't want the percentage to ever go down.  It's better
			// for the percentage to just stall until it catches up again as that's less confusing to the user.
			if (newPercentage > lastPercentageDone)
				{
				if (Application.SimpleOutput)
					{
					System.Console.WriteLine(
						Engine.Locale.Get("NaturalDocs.CLI", "Status.SimpleOutput.Update(percent)", newPercentage)
						);
					}
				else
					{
					System.Console.CursorLeft = percentagePositionLeft;
					System.Console.CursorTop = percentagePositionTop;

					if (newPercentage < 10)
						{  System.Console.Write(' ');  }

					System.Console.Write(newPercentage);
					System.Console.Write('%');
					}

				lastPercentageDone = newPercentage;
				}
			}

		protected override void ShowEndMessage ()
			{
			if (!Application.SimpleOutput)
				{
				System.Console.CursorLeft = percentagePositionLeft;
				System.Console.CursorTop = percentagePositionTop;

				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.End")
					);
				}
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Engine.Output.Builder builderProcess;

		protected long totalWork;
		protected int lastPercentageDone;

		protected int percentagePositionLeft;
		protected int percentagePositionTop;

		}
	}
