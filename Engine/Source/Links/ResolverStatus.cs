/*
 * Class: CodeClear.NaturalDocs.Engine.Links.ResolverStatus
 * ____________________________________________________________________________
 *
 * Statistics on the progress of <Links.Resolver.WorkOnResolvingLinks()>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Links
	{
	public class ResolverStatus
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: ResolverStatus
		 */
		public ResolverStatus ()
			{
			ChangesBeingProcessed = 0;
			ChangesRemaining = 0;
			}



		// Group: Public Variables
		// __________________________________________________________________________


		/* Variable: ChangesBeingProcessed
		 */
		public int ChangesBeingProcessed;

		/* Variable: ChangesRemaining
		 */
		public int ChangesRemaining;

		}
	}
