/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.Exceptions.FieldMustBeNull
 * ____________________________________________________________________________
 * 
 * Thrown when a field isn't correct for the operation you're trying to attempt.
 * It must be null.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.CodeDB.Exceptions
	{
	public class FieldMustBeNull : Exception
		{
		public FieldMustBeNull (string operation, string fieldName)
			: base ( "The " + fieldName + " field must be null to use " + operation + '.' )
			{
			}
		}
	}