/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.Exceptions.TopicValueMustBeNonZero
 * ____________________________________________________________________________
 * 
 * Thrown when a <Topic's> field isn't correct for the operation you're trying to attempt.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.CodeDB.Exceptions
	{
	public class TopicValueMustBeNonZero : Exception
		{
		public TopicValueMustBeNonZero (string operation, string fieldName)
			: base ( "Topic's " + fieldName + " must be non-zero to use " + operation + '.' )
			{
			}
		}
	}