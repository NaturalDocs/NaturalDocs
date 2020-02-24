/* 
 * Class: CodeClear.NaturalDocs.Engine.Exceptions.ArrayDidntHaveEvenLength
 * ____________________________________________________________________________
 * 
 * Thrown when an array must have an even number of elements but does not.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Exceptions
	{
	public class ArrayDidntHaveEvenLength : Exception
		{
		public ArrayDidntHaveEvenLength (string arrayName) 
			: base ("The array " + arrayName + " didn't have an even number of elements.")
			{
			}
		}
	}