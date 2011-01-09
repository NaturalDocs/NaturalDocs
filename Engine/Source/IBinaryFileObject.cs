/* 
 * Class: GregValure.NaturalDocs.Engine.IBinaryFileObject
 * ____________________________________________________________________________
 * 
 * An interface for an object that can be read from and written to a <BinaryFile>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine
	{
	public interface IBinaryFileObject
		{

		/* Function: FromBinaryFile
		 * Reads the contents of this object from the passed binary file.  Whatever the object prevously
		 * contained will be replaced.
		 */
		void FromBinaryFile (BinaryFile binaryFile);
			
		/* Function: ToBinaryFile
		 * Writes the contents of this object to the passed binary file.
		 */
		void ToBinaryFile (BinaryFile binaryFile);

		}
	}