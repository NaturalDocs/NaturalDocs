/* 
 * Class: CodeClear.NaturalDocs.Engine.Process
 * ____________________________________________________________________________
 * 
 * A base class for a major process to be run against a Natural Docs module.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine
	{
	abstract public class Process : IDisposable
		{
		
		// Group: Functions
		// ________________________________________________________________________
		
		public Process (Engine.Instance engineInstance)
			{
			this.engineInstance = engineInstance;
			}

		~Process ()
			{
			Dispose(true);
			}

		public void Dispose ()
			{
			Dispose(false);
			}

		protected abstract void Dispose (bool strictRulesApply);


		// Group: Properties
		// __________________________________________________________________________

		public Engine.Instance EngineInstance
			{
			get
				{  return engineInstance;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Engine.Instance engineInstance;

		}
	}