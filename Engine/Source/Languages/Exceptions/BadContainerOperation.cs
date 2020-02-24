/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.Exceptions.BadContainerOperation
 * ____________________________________________________________________________
 * 
 * Thrown when inappropriate <Language> functions are called on a container.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Languages.Exceptions
	{
	public class BadContainerOperation : Exception
		{
		public BadContainerOperation (string operation)
			: base ( "Attempted to use " + operation + " with a container language object." )
			{
			}
		}
	}