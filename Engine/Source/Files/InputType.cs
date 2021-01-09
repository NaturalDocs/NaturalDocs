/* 
 * Enum: CodeClear.NaturalDocs.Engine.Files.InputType
 * ____________________________________________________________________________
 * 
 * The type of <FileSource> being represented.
 * 
 *		Source - The <FileSource> provides source files, although it can provide image files as well.
 *		Image - The <FileSource> provides images only.
 *		Style - The <FileSource> provides style files only.  Any images included here will be marked as
 *				  <FileType.Style>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public enum InputType : byte
		{  
		Source = FileType.Source, 
		Image = FileType.Image, 
		Style = FileType.Style
		};
	}