/*
 * Enum: CodeClear.NaturalDocs.Engine.Files.FileType
 * ____________________________________________________________________________
 *
 * The type of an input file.
 *
 *		Source - The file contains source code to be parsed.
 *		Image - The file is an image that may be referenced in source code.  This is different than
 *					images which are part of a style.
 *		Style - The file is part of an output style.  This may include images.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public enum FileType : byte
		{
		Source = 1,
		Image = 2,
		Style = 3
		}
	}
