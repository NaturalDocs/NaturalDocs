/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.EventAccessor
 * ____________________________________________________________________________
 * 
 * A variation of <Accessor> that is passed along with change events.  This is necessary because the code
 * handling the events may need to query the database for more information, and this exposes only the small
 * subset of functions that should be allowed then.  Also, since there may be multiple build targets querying
 * for the same information, this caches certain results so they don't have to be retrieved from the database
 * every time.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.CodeDB
	{
	public class EventAccessor
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: EventAccessor
		 * Creates a new EventAccessor.  The <Accessor> passed must have a read/write or read/possible write 
		 * lock or else this will throw an exception.
		 */
		internal EventAccessor (Accessor newAccessor)
			{
			if (newAccessor.LockHeld != Accessor.LockType.ReadWrite && 
				newAccessor.LockHeld != Accessor.LockType.ReadPossibleWrite)
				{
				throw new InvalidOperationException();
				}
				
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