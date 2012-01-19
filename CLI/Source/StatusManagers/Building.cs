/* 
 * Class: GregValure.NaturalDocs.CLI.StatusManagers.Building
 * ____________________________________________________________________________
 * 
 * A class to monitor the output building stage.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.CLI.StatusManagers
	{
	public class Building : StatusManager
		{
		
		public Building (int updateInterval) : base (updateInterval)
			{
			lastPercentage = 0;
			totalUnitsOfWork = 0;
			}

		public override void Start ()
			{
			totalUnitsOfWork = Engine.Instance.Output.UnitsOfWorkRemaining();

			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.StartOutputBuilding")
				);
				
			base.Start();
			}

		protected override void Update (Object sender, System.Timers.ElapsedEventArgs args)
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

		public override void End ()
			{
			base.End();

			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.EndOutputBuilding")
				);
			}
		
		
		protected int lastPercentage;
		protected long totalUnitsOfWork;

		}
	}