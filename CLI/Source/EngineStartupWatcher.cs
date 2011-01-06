/* 
 * Class: GregValure.NaturalDocs.CLI.EngineStartupWatcher
 * ____________________________________________________________________________
 * 
 * A class that handles events that may occur during engine initialization.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.CLI
	{
	public class EngineStartupWatcher : Engine.IStartupWatcher
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		public EngineStartupWatcher ()
			{
			possiblyLongStartupOperationWatcher = 
				new StatusManagers.PossiblyLongStartupOperation(Application.LongOperationMessageDelay);
			}

		public void OnStartPossiblyLongOperation (string operationName)
			{
			possiblyLongStartupOperationWatcher.Start(operationName);
			}
		
		public void OnEndPossiblyLongOperation ()
			{
			possiblyLongStartupOperationWatcher.End();
			}
		

		// Group: Variables
		// __________________________________________________________________________
		
		protected StatusManagers.PossiblyLongStartupOperation possiblyLongStartupOperationWatcher;
		
		}
	}