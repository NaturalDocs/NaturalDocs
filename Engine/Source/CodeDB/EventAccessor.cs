/*
 * Class: CodeClear.NaturalDocs.Engine.CodeDB.EventAccessor
 * ____________________________________________________________________________
 *
 * A variation of <Accessor> that is passed along with change events.  This is necessary because the code
 * handling the events may need to query the database for more information, and this exposes only the small
 * subset of functions that should be allowed then.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.CodeDB
	{
	public partial class EventAccessor
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: EventAccessor
		 */
		internal EventAccessor (Accessor newAccessor)
			{
			accessor = newAccessor;
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: accessor
		 * A reference to the main <Accessor> this object uses.
		 */
		protected Accessor accessor;

		}
	}
