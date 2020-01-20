/* 
 * Enum: CodeClear.NaturalDocs.Engine.Files.FileFlags
 * ____________________________________________________________________________
 * 
 * Flags containing information about the file.
 * 
 * InBinaryFile - Whether this file appears in <Files.nd>.
 * InFileSource - Whether this file was found in a file source from <Engine.Files.Manager>.
 * Deleted - The file has been deleted.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	[Flags]
	public enum FileFlags : byte
		{
		InBinaryFile = 0x01,
		InFileSource = 0x02,
		Deleted = 0x04
		}
	}