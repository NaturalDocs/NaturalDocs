/* 
 * Class: CodeClear.NaturalDocs.Engine.Module
 * ____________________________________________________________________________
 * 
 * A base class for a core part of <Engine.Instance>.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine
	{
	public abstract class Module : IDisposable
		{
		
		// Group: Functions
		// ________________________________________________________________________
		
		public Module (Engine.Instance engineInstance)
			{
			this.engineInstance = engineInstance;
			}

		~Module ()
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