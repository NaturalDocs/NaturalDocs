/*
 * Class: CodeClear.NaturalDocs.Engine.CodeDB.Exceptions.FieldMustBeNonZero
 * ____________________________________________________________________________
 *
 * Thrown when a field isn't correct for the operation you're trying to attempt.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.CodeDB.Exceptions
	{
	public class FieldMustBeNonZero : Exception
		{
		public FieldMustBeNonZero (string operation, string fieldName)
			: base ( "The " + fieldName + " field must be non-zero to use " + operation + '.' )
			{
			}
		}
	}
