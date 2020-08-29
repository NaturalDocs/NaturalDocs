/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Builder
 * ____________________________________________________________________________
 * 
 * A process which handles building output files for all <Targets>.
 * 
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		Externally, this class is thread safe.
 *		
 *		Internally, all variable accesses must use a monitor on <accessLock>.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output
	{
	public class Builder : Process
		{
		
		// Group: Functions
		// __________________________________________________________________________
		

		/* Function: Builder
		 */
		public Builder (Engine.Instance engineInstance) : base (engineInstance)
			{
			targetBuilders = new TargetBuilder[ engineInstance.Output.Targets.Count ];
			accessLock = new object();
			}


		protected override void Dispose(bool strictRulesApply)
			{
			if (!strictRulesApply)
				{
				if (targetBuilders != null)
					{
					for (int i = 0; i < targetBuilders.Length; i++)
						{
						if (targetBuilders[i] != null)
							{  targetBuilders[i].Dispose();  }
						}
					}
				}
			}


		/* Function: WorkOnUpdatingOutput
		 * 
		 * Works on the task of going through all the detected changes and updating the generated output.  This is a parallelizable
		 * task, so multiple threads can call this function and they will divide up the work until it's done.  Pass a <CancelDelegate>
		 * if you'd like to be able to interrupt this task, or <Delegates.NeverCancel> if not.
		 * 
		 * Note that building the output is a two-stage process, so after this task is fully complete you must also call 
		 * <WorkOnFinalizingOutput()> to finish it.
		 * 
		 * This function returns if it's cancelled or there is no more work to be done.  If there is only one thread working on this 
		 * then the task is complete, but if there are multiple threads the task isn't complete until they all have returned.  This
		 * one may have returned because there was no more work for it to do but other threads could still be working.
		 */
		public void WorkOnUpdatingOutput (CancelDelegate cancelDelegate)
			{
			if (cancelDelegate())
				{  return;  }

			for (int i = 0; i < targetBuilders.Length; i++)
				{
				TargetBuilder targetBuilder;

				lock (accessLock)
					{
					targetBuilder = targetBuilders[i];

					if (targetBuilder == null)
						{
						targetBuilder = EngineInstance.Output.Targets[i].CreateBuilderProcess();
						targetBuilders[i] = targetBuilder;
						}
					}

				targetBuilder.WorkOnUpdatingOutput(cancelDelegate);

				if (cancelDelegate())
					{  return;  }
				}
			}
			

		/* Function: WorkOnFinalizingOutput
		 * 
		 * Works on the task of finalizing the output, which is any task that requires all files to be successfully processed by
		 * <WorkOnUpdatingOutput()> before it can run.  You must wait for all threads to return from <WorkOnUpdatingOutput()>
		 * before calling this function.  This is a parallelizable task, so multiple threads can call this function and the work will be
		 * divided up between them.  Pass a <CancelDelegate> if you'd like to be able to interrupt this task, or 
		 * <Delegates.NeverCancel> if not.
		 * 
		 * This function returns if it's cancelled or there is no more work to be done.  If there is only one thread working on this 
		 * then the task is complete, but if there are multiple threads the task isn't complete until they all have returned.  This
		 * one may have returned because there was no more work for it to do but other threads could still be working.
		 */
		public void WorkOnFinalizingOutput (CancelDelegate cancelDelegate)
			{
			if (cancelDelegate())
				{  return;  }

			for (int i = 0; i < targetBuilders.Length; i++)
				{
				TargetBuilder targetBuilder;
				
				lock (accessLock)
					{  
					// All TargetBuilders should have been created already by WorkOnUpdatingOutput().
					targetBuilder = targetBuilders[i];  
					}

				targetBuilder.WorkOnFinalizingOutput(cancelDelegate);

				if (cancelDelegate())
					{  return;  }
				}
			}
		 
		
		/* Function: GetStatus
		 * Returns numeric values representing the total changes being processed and those yet to be processed in the target's
		 * unprocessed changes.  The numbers are meaningless other than to track progress as they work their way towards zero.
		 */
		public void GetStatus (out long workInProgress, out long workRemaining)
			{
			workInProgress = 0;
			workRemaining = 0;

			lock (accessLock)
				{
				for (int i = 0; i < targetBuilders.Length; i++)
					{
					if (targetBuilders[i] != null)
						{
						long targetWorkInProgress, targetWorkRemaining;

						targetBuilders[i].GetStatus(out targetWorkInProgress, out targetWorkRemaining);

						workInProgress += targetWorkInProgress;
						workRemaining += targetWorkRemaining;
						}
					else
						{
						long targetWorkRemaining;

						EngineInstance.Output.Targets[i].GetStatus(out targetWorkRemaining);

						workRemaining += targetWorkRemaining;
						}
					}
				}
			}



		// Group: Variables
		// __________________________________________________________________________

			
		/* var: targetBuilders
		 * 
		 * An array of <TargetBuilders> corresponding to the entries in <Output.Manager.Targets>.  They're created on demand
		 * so entries may be null if building hasn't started on that target yet.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected TargetBuilder[] targetBuilders;
		
		
		/* var: accessLock
		 * An object used for a monitor that prevents more than one thread from accessing any of the variables at a time.
		 */
		protected object accessLock;

		}
	}