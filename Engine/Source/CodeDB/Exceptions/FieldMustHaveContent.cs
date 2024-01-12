/*
 * Class: CodeClear.NaturalDocs.Engine.CodeDB.Exceptions.FieldMustHaveContent
 * ____________________________________________________________________________
 *
 * Thrown when a string field isn't correct for the operation you're trying to attempt.
 * It must have content, meaning it cannot be null or an empty string.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.CodeDB.Exceptions
	{
	public class FieldMustHaveContent : Exception
		{
		public FieldMustHaveContent (string operation, string fieldName)
			: base ( "The " + fieldName + " field must have content to use " + operation + '.' )
			{
			}
		}
	}
