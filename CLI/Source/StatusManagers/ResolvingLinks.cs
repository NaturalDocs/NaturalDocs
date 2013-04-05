/* 
 * Class: GregValure.NaturalDocs.CLI.StatusManagers.ResolvingLinks
 * ____________________________________________________________________________
 * 
 * A class to monitor the link resolving stage.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.CLI.StatusManagers
	{
	public class ResolvingLinks : StatusManager
		{

		// Group: Functions
		// __________________________________________________________________________

		
		public ResolvingLinks () : base (Application.StatusInterval)
			{
			lastPercentage = 0;
			totalUnitsOfWork = 0;
			}

		public override void Start ()
			{
			totalUnitsOfWork = Engine.Instance.CodeDB.ResolvingUnitsOfWorkRemaining();

			base.Start();
			}

		protected override void ShowStartMessage ()
			{
			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.StartLinkResolving")
				);
			}

		protected override void ShowUpdateMessage ()
			{
			long unitsOfWorkRemaining = Engine.Instance.CodeDB.ResolvingUnitsOfWorkRemaining();

			// Sanity check in case it increases while running.
			if (unitsOfWorkRemaining > totalUnitsOfWork)
				{  unitsOfWorkRemaining = totalUnitsOfWork;  }

			long unitsOfWorkDone = totalUnitsOfWork - unitsOfWorkRemaining;
			int newPercentage = (int)((100 * unitsOfWorkDone) / totalUnitsOfWork);
			
			// Another sanity check.  We use > instead of != because we don't want the percentage to ever go down.  It's better
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

		protected int lastPercentage;
		protected long totalUnitsOfWork;

		}
	}