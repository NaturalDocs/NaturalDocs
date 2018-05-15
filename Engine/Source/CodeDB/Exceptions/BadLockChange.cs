/* 
 * Class: CodeClear.NaturalDocs.Engine.CodeDB.Exceptions.BadLockChange
 * ____________________________________________________________________________
 * 
 * Thrown when an <Accessor> was attempting to move from one lock type to another in an incorrect way.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.CodeDB.Exceptions
	{
	public class BadLockChange : Exception
		{
		public BadLockChange (Accessor.LockType held, Accessor.LockType desired, Accessor.LockType required)
			: base ( MakeMessage(held, desired, required) )
			{
			}
			
		private static string MakeMessage (Accessor.LockType held, Accessor.LockType desired, Accessor.LockType required)
			{
			if (required == Accessor.LockType.None)
				{
				return "Attempted to acquire a " + desired + " database lock when a " + held + " lock was held, but no lock must be held.";
				}
			else
				{
				return "Attempted to acquire a " + desired + " database lock when a " + held + " lock was held, but a " + required + " lock must be held instead.";
				}
			}
		}
	}