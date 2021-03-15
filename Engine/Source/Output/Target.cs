/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Target
 * ____________________________________________________________________________
 * 
 * The base class for an output target.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Styles;


namespace CodeClear.NaturalDocs.Engine.Output
	{
	abstract public class Target : Module
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Target
		 */
		public Target (Output.Manager manager) : base (manager.EngineInstance)
			{
			this.manager = manager;
			}
			
		
		/* Function: Start
		 * Initializes the target and returns whether all the settings are correct and that execution is ready to begin.  
		 * If there are problems they are added as <Errors> to the errorList parameter.
		 */
		virtual public bool Start (Errors.ErrorList errorList)
			{  
			started = true;
			return true;
			}
			
			
		/* Function: Cleanup
		 * Cleans up the target's internal data when everything is up to date.  The default implementation does nothing.  You
		 * can pass a <CancelDelegate> to interrupt the process if necessary.
		 */
		virtual public void Cleanup (CancelDelegate cancelDelegate)
			{
			}


		/* Function: CreateBuilderProcess
		 * Creates a <TargetBuilder> capable of building the output for this target.
		 */
		abstract public TargetBuilder CreateBuilderProcess ();


		/* Function: GetStatus
		 * Returns a numeric value representing the total changes yet to be processed.  The value is meaningless other than 
		 * to track the progress of the <TargetBuilder> as it works its way towards zero.
		 */
		abstract public void GetStatus (out long workRemaining);



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Manager
		 * The <Output.Manager> associated with this target.
		 */
		public Output.Manager Manager
			{
			get
				{  return manager;  }
			}


		/* Property: Style
		 * The <Style> that applies to this target, or null if none.
		 */
		virtual public Style Style
			{
			get
				{  return null;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		protected Output.Manager manager;

		}
	}