/* 
 * Enum: GregValure.NaturalDocs.Engine.Files.FileFlags
 * ____________________________________________________________________________
 * 
 * Flags containing information about the file.
 * 
 * InBinaryFile - Whether this file appears in <Files.nd>.
 * InFileSource - Whether this file was found in a file source from <Engine.Files.Manager>.
 * 
 * Unchanged - The file is unchanged since the last run.
 * NewOrChanged - The file is new or has been changed since the last run.
 * Deleted - The file has been deleted since the last run.
 * StatusMask - The bitmask to use with <Unchanged>, <NewOrChanged>, and <Deleted>.
 * 
 * Claimed - The file is currently claimed.
 * 
 * UnchangedSinceClaimed - The file's status hasn't been changed since it was claimed.
 * NewOrChangedSinceClaimed - The file has been created or changed since it was claimed.
 * DeletedSinceClaimed - The file has been deleted since it was claimed.
 * StatusSinceClaimedMask - The bitmask to use with <UnchangedSinceClaimed>, <NewOrChangedSinceClaimed>,
 *										and <DeletedSinceClaimed>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Files
	{
	[Flags]
	public enum FileFlags : byte
		{
		InBinaryFile = 0x01,
		InFileSource = 0x02,
		
		Unchanged = 0x00,
		NewOrChanged = 0x04,
		Deleted = 0x08,
		StatusMask = 0x0C,
		
		Claimed = 0x10,
		
		UnchangedSinceClaimed = 0x00,
		NewOrChangedSinceClaimed = 0x20,
		DeletedSinceClaimed = 0x40,
		StatusSinceClaimedMask = 0x60
		}
	}