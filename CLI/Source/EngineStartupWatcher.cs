/*
 * Class: CodeClear.NaturalDocs.CLI.EngineStartupWatcher
 * ____________________________________________________________________________
 *
 * A class that handles events that may occur during engine initialization.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.CLI
	{
	public class EngineStartupWatcher : Engine.IStartupWatcher
		{

		// Group: Functions
		// __________________________________________________________________________

		public EngineStartupWatcher ()
			{
			status = new StatusManagers.PossiblyLongStartupOperation();
			}

		public void OnStartPossiblyLongOperation (string operationName)
			{
			status.Start(operationName);
			}

		public void OnEndPossiblyLongOperation ()
			{
			status.End();
			}

		public void OnStartupIssues (Engine.StartupIssues newIssues, Engine.StartupIssues allIssues)
			{
			}


		// Group: Variables
		// __________________________________________________________________________

		protected StatusManagers.PossiblyLongStartupOperation status;

		}
	}
