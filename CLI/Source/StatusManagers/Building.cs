/* 
 * Class: GregValure.NaturalDocs.CLI.StatusManagers.Building
 * ____________________________________________________________________________
 * 
 * A class to monitor the output building stage.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.CLI.StatusManagers
	{
	public class Building : StatusManager
		{
		
		// Group: Functions
		// __________________________________________________________________________


		public Building () : base (Application.StatusInterval)
			{
			lastPercentage = 0;
			totalUnitsOfWork = 0;
			}

		protected override void ShowStartMessage ()
			{
			totalUnitsOfWork = Engine.Instance.Output.UnitsOfWorkRemaining();

			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.StartOutputBuilding")
				);
			}

		protected override void ShowUpdateMessage ()
			{
			long unitsOfWorkRemaining = Engine.Instance.Output.UnitsOfWorkRemaining();

			// Sanity check since as it runs a builder can add tasks to the list of things it needs to do.
			if (unitsOfWorkRemaining > totalUnitsOfWork)
				{  unitsOfWorkRemaining = totalUnitsOfWork;  }

			long unitsOfWorkDone = totalUnitsOfWork - unitsOfWorkRemaining;
			int newPercentage = (int)((100 * unitsOfWorkDone) / totalUnitsOfWork);
			
			// Another sanity check.  We use > instead of != because we don't want the percentage to ever go down.  It's better
			// for the percentage to just stall until it catches up again as that's less confusing to the user.
			if (newPercentage > lastPercentage)
				{
				System.Console.WriteLine(
					Engine.Locale.Get("NaturalDocs.CLI", "Status.OutputBuildingUpdate(percent)", newPercentage)
					);
					
				lastPercentage = newPercentage;
				}
			}


		// Group: Variables
		// __________________________________________________________________________
		
		protected int lastPercentage;
		protected long totalUnitsOfWork;

		}
	}