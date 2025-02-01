/*
 * Class: CodeClear.NaturalDocs.Engine.CodeDB.Exceptions.FieldMustBeValue
 * ____________________________________________________________________________
 *
 * Thrown when a field isn't correct for the operation you're trying to attempt.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.CodeDB.Exceptions
	{
	public class FieldMustBeValue : Exception
		{
		public FieldMustBeValue (string operation, string fieldName, int expectedValue)
			: base ( "The " + fieldName + " field must be " + expectedValue + " to use " + operation + '.' )
			{
			}

		public FieldMustBeValue (string operation, string fieldName, string expectedValue)
			: base ( "The " + fieldName + " field must be \"" + expectedValue + "\" to use " + operation + '.' )
			{
			}
		}
	}
