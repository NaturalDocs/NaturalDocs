/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.Exceptions.TopicStringMustHaveContent
 * ____________________________________________________________________________
 * 
 * Thrown when a <Topic's> string field isn't correct for the operation you're trying to attempt.
 * It must have content, meaning not be null or an empty string.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.CodeDB.Exceptions
	{
	public class TopicStringMustHaveContent : Exception
		{
		public TopicStringMustHaveContent (string operation, string fieldName)
			: base ( "Topic's " + fieldName + " must be set to use " + operation + '.' )
			{
			}
		}
	}