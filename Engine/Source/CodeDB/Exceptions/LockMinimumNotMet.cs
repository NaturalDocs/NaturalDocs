/*
 * Class: CodeClear.NaturalDocs.Engine.CodeDB.Exceptions.LockMinimumNotMet
 * ____________________________________________________________________________
 *
 * Thrown when an <Accessor> was attempting to perform an operation that required a stronger lock than was held.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.CodeDB.Exceptions
	{
	public class LockMinimumNotMet : Exception
		{
		public LockMinimumNotMet (Accessor.LockType held, Accessor.LockType minimum)
			: base ( MakeMessage(held, minimum) )
			{
			}

		private static string MakeMessage (Accessor.LockType held, Accessor.LockType minimum)
			{
			if (held == Accessor.LockType.None)
				{
				throw new Exception ("Attempted a database operation which requires at least a " + minimum +
												" lock when no lock was held.");
				}
			else
				{
				throw new Exception ("Attempted a database operation which requires at least a " + minimum +
												" lock when only a " + held + "lock was held.");
				}
			}
		}
	}
