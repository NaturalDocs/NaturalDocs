/* 
 * Class: CodeClear.NaturalDocs.Engine.Hierarchies.Manager
 * ____________________________________________________________________________
 * 
 * A module to handle the different hierarchies within Natural Docs.
 * 
 * 
 * Topic: Usage
 * 
 *		- Call <Engine.Instance.Start()> which will start this module.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Hierarchies
	{
	public class Manager : Module
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Manager
		 */
		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			}

		protected override void Dispose (bool strictRulesApply)
			{
			}
		
		/* Function: Start
		 * 
		 * Starts the module, returning whether it was successful.  If there were any  errors they will be added to errorList.
		 * 
		 * Dependencies:
		 * 
		 *		- Currently there are no dependencies.
		 */
		public bool Start (Errors.ErrorList errorList)
			{
			return true;
			}

		}
	}