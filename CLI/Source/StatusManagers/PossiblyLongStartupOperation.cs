/* 
 * Class: GregValure.NaturalDocs.CLI.StatusManagers.PossiblyLongStartupOperation
 * ____________________________________________________________________________
 * 
 * A class to monitor engine initialization, posting messages only if an event that might take a long time actually does.
 * One object can be used for multiple operations, just not at the same time obviously.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.CLI.StatusManagers
	{
	public class PossiblyLongStartupOperation : StatusManager
		{
		
		public PossiblyLongStartupOperation (int updateInterval) : base (updateInterval)
			{
			operationName = null;
			displayedMessage = false;
			}

		public void Start (string operationName)
			{
			this.operationName = operationName;
			displayedMessage = false;
				
			base.Start();
			}

		protected override void Update (Object sender, System.Timers.ElapsedEventArgs args)
			{
			if (operationName != null && displayedMessage == false)
				{
				displayedMessage = true;
			
				// Operation names are subject to change arbitrarily, so we can't guarantee there's an entry for it in the translation
				// file.  Substitute null if it's missing and just don't display a message.
				string message = Engine.Locale.SafeGet("NaturalDocs.CLI", "Status.LongStartupOperation.Start" + operationName, null);

				if (message != null)
					{  System.Console.WriteLine(message);  }
				}
			}

		public override void End()
			{
			base.End();

			if (operationName != null && displayedMessage == true)
				{
				string message = Engine.Locale.SafeGet("NaturalDocs.CLI", "Status.LongStartupOperation.End" + operationName, null);

				if (message != null)
					{  System.Console.WriteLine(message);  }

				operationName = null;
				}
			}
		
		
		protected string operationName;
		protected bool displayedMessage;
		
		}
	}