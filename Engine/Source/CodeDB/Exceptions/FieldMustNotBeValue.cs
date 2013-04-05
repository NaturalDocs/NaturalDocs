/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.Exceptions.FieldMustNotBeValue
 * ____________________________________________________________________________
 * 
 * Thrown when a field isn't correct for the operation you're trying to attempt.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.CodeDB.Exceptions
	{
	public class FieldMustNotBeValue : Exception
		{
		public FieldMustNotBeValue (string operation, string fieldName, int forbiddenValue)
			: base ( "The " + fieldName + " field must not be " + forbiddenValue + " to use " + operation + '.' )
			{
			}

		public FieldMustNotBeValue (string operation, string fieldName, string forbiddenValue)
			: base ( "The " + fieldName + " field must not be \"" + forbiddenValue + "\" to use " + operation + '.' )
			{
			}
		}
	}