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


		/* Function: WorkOnUpdatingOutput
		 * 
		 * Works on the task of updating the output files for any changes it has detected so far.  This is a parallelizable task, so
		 * multiple threads can call this function and the work will be divided up between them.  Note that the output may not be
		 * usable after this completes; you also need to call <WorkOnFinalizingOutput()>.
		 * 
		 * This function returns if it's cancelled or there is no more work to be done.  If there is only one thread working on this 
		 * then the task is complete, but if there are multiple threads the task isn't complete until they all have returned.  This one 
		 * may have returned because there was no more work for this thread to do, but other threads are still working.
		 */
		public void WorkOnUpdatingOutput (CancelDelegate cancelDelegate)
			{
			foreach (var target in targets)
				{
				if (cancelDelegate())
					{  return;  }
					
				target.WorkOnUpdatingOutput(cancelDelegate);
				}
			}


		/* Function: WorkOnFinalizingOutput
		 * 
		 * Works on the task of finalizing the output, which is any task that requires all files to be successfully processed by
		 * <WorkOnUpdatingOutput()> before it can run.  You must wait for all threads to return from <WorkOnUpdatingOutput()>
		 * before calling this function.  Examples of finalization include generating index and search data for HTML output and
		 * compiling the temporary files into the final one for PDF output.  This is a parallelizable task, so multiple threads can call 
		 * this function and the work will be divided up between them.
		 * 
		 * This function returns if it's cancelled or there is no more work to be done.  If there is only one thread working on this 
		 * then the task is complete, but if there are multiple threads the task isn't complete until they all have returned.  This one 
		 * may have returned because there was no more work for this thread to do, but other threads are still working.
		 */
		public void WorkOnFinalizingOutput (CancelDelegate cancelDelegate)
			{
			foreach (var target in targets)
				{
				if (cancelDelegate())
					{  return;  }
					
				target.WorkOnFinalizingOutput(cancelDelegate);
				}
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
			

		/* Function: UnitsOfWorkRemaining
		 * Returns a number representing how much work the targets have left to do.  What tasks the units represent can vary,
		 * so this is intended simply to allow a percentage to be calculated.
		 */
		public long UnitsOfWorkRemaining ()
			{
			long value = 0;

			foreach (var target in targets)
				{  value += target.UnitsOfWorkRemaining();  }

			return value;
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