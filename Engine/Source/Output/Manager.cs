/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Manager
 * ____________________________________________________________________________
 * 
 * A class to manage all the output targets.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Output
	{
	public class Manager : Module
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Manager
		 */
		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			targets = new List<Target>();
			}
			
			
		/* Function: Dispose
		 */
		override protected void Dispose (bool strictRulesApply)
			{
			if (!strictRulesApply)
				{
				foreach (var target in targets)
					{  target.Dispose();  }
				}
			}


		/* Function: AddTarget
		 * Adds an <Output.Target> to the list.  This can only be called before the class is started.
		 */
		public void AddTarget (Target target)
			{
			targets.Add(target);
			}
			
			
		/* Function: Start
		 * Initializes the manager and returns whether all the settings are correct and that execution is ready to begin.  
		 * If there are problems they are added as <Errors> to the errorList parameter.  This class is *not* designed to allow 
		 * multiple attempts.  If this function fails scrap the entire <Engine.Instance> and start again.
		 */
		public bool Start (ErrorList errorList)
			{
			bool success = true;
			
			foreach (var target in targets)
				{
				if (target.Start(errorList) == false)
					{  success = false;  }
				}
				
			return success;
			}


		/* Function: CreateBuilderProcess
		 * Creates a <Builder> for updating all the output.
		 */
		public Builder CreateBuilderProcess ()
			{
			return new Builder(engineInstance);
			}


		/* Function: Cleanup
		 * Cleans up the module's internal data when everything is up to date.  This will do things like remove empty output
		 * folders.  You can pass a <CancelDelegate> to interrupt the process if necessary.
		 */
		public void Cleanup (CancelDelegate cancelDelegate)
			{
			foreach (var target in targets)
				{  target.Cleanup(cancelDelegate);  }
			}



		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Targets
		 * A read-only list of <Targets> managed by this module.  If there are none, the list will be empty instead of null.
		 */
		public IList<Target> Targets
			{
			get
				{  return targets.AsReadOnly();  }
			}


			
		// Group: Variables
		// __________________________________________________________________________
		
		protected List<Target> targets;

		}
	}