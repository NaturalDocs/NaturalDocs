/*
 * Class: CodeClear.NaturalDocs.Engine.Output.TargetBuilder
 * ____________________________________________________________________________
 *
 * The base class for processes which handles building output files for a single <Target>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output
	{
	abstract public class TargetBuilder : Process
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: TargetBuilder
		 */
		public TargetBuilder (Target target) : base (target.EngineInstance)
			{
			this.target = target;
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
		abstract public void WorkOnUpdatingOutput (CancelDelegate cancelDelegate);


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
		 *
		 * The default implementation of this function is to do nothing, so if your output target doesn't need a finalization stage
		 * you can just implement <WorkOnUpdatingOutput()>.
		 */
		virtual public void WorkOnFinalizingOutput (CancelDelegate cancelDelegate)
			{
			}


		/* Function: GetStatus
		 * Returns numeric values representing the total changes being processed and those yet to be processed in the target's
		 * unprocessed changes.  The numbers are meaningless other than to track progress as they work their way towards zero.
		 */
		abstract public void GetStatus (out long workInProgress, out long workRemaining);



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Target
		 * The <Target> associated with this builder.
		 */
		public Target Target
			{
			get
				{  return target;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: target
		 * The <Target> associated with this builder.
		 */
		protected readonly Target target;

		}
	}
