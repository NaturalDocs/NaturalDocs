/* 
 * Class: CodeClear.NaturalDocs.Engine.Exceptions.NameChangeDifferedInMoreThanCapitalization
 * ____________________________________________________________________________
 * 
 * Thrown when something is renamed and the new name differs in more than just capitalization when it is not allowed to.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Exceptions
	{
	public class NameChangeDifferedInMoreThanCapitalization : Exception
		{
		public NameChangeDifferedInMoreThanCapitalization (string oldName, string newName, string typeName)
			: base ("Tried to rename " + typeName + " from \"" + oldName + "\" to \"" + newName + "\" when they " +
						"are only allowed to differ in capitalization.")
			{
			}
		}
	}