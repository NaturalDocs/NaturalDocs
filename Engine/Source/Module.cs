/*
 * Class: CodeClear.NaturalDocs.Engine.Module
 * ____________________________________________________________________________
 *
 * A base class for a core part of <Engine.Instance>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
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
			started = false;
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

		/* Property: Started
		 * Whether the module was successfully started.
		 */
		public bool Started
			{
			get
				{  return started;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Engine.Instance engineInstance;

		/* var: started
		 * Whether the module was successfully started.
		 */
		protected bool started;

		}
	}
