/* 
 * Class: GregValure.NaturalDocs.CLI.StatusManagers.Building
 * ____________________________________________________________________________
 * 
 * A class to monitor the output building stage.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
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
			}

		public override void Start ()
			{
			System.Console.WriteLine(
				Engine.Locale.Get("NaturalDocs.CLI", "Status.StartOutputBuilding")
				);
				
			base.Start();
			}

		protected override void Update (Object sender, System.Timers.ElapsedEventArgs args)
			{
			int newPercentage = 0;
			
			if (newPercentage != lastPercentage)
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
		
		}
	}