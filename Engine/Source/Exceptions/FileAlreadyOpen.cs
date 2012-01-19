/* 
 * Class: GregValure.NaturalDocs.Engine.Exceptions.FileAlreadyOpen
 * ____________________________________________________________________________
 * 
 * Thrown when a you try to open a file with a class that already has one open.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Exceptions
	{
	public class FileAlreadyOpen : Exception
		{
		public FileAlreadyOpen (string newFileName, string alreadyOpenfileName)
			: base ("Tried to open " + newFileName + " when the class already had " + alreadyOpenfileName + " open.")
			{
			}
		}
	}