/* 
 * Class: CodeClear.NaturalDocs.CLI.StatusManagers.PossiblyLongStartupOperation
 * ____________________________________________________________________________
 * 
 * A class to monitor engine initialization, posting messages only if an event that might take a long time actually does.
 * One object can be used for multiple operations, just not at the same time obviously.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.CLI.StatusManagers
	{
	public class PossiblyLongStartupOperation : StatusManager
		{
		
		// Group: Functions
		// __________________________________________________________________________


		public PossiblyLongStartupOperation () : base (0, Application.DelayedMessageThreshold)
			{
			operationName = null;
			}

		public void Start (string operationName)
			{
			this.operationName = operationName;
			base.Start();
			}

		protected override void ShowStartMessage ()
			{
			// Operation names are subject to change arbitrarily, so we can't guarantee there's an entry for it in the translation
			// file.  Substitute null if it's missing and just don't display a message.
			string message = Engine.Locale.SafeGet("NaturalDocs.CLI", "Status.LongStartupOperation.Start" + operationName, null);

			if (message != null)
				{  System.Console.WriteLine(message);  }
			}

		
		// Group: Variables
		// __________________________________________________________________________

		protected string operationName;
		
		}
	}