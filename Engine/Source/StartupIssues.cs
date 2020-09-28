/* 
 * Enum: CodeClear.NaturalDocs.Engine.StartupIssues
 * ____________________________________________________________________________
 * 
 * A set of flags that track certain issues that can occur during engine startup.
 * 
 *		None - None of the below flags have been set.
 *		
 *		NeedToStartFresh - You should avoid loading any data from the previous run.
 *		NeedToReparseAllFiles - All source files need to be reparsed.
 *		NeedToReparseStyleFiles - All style files need to be reparsed.
 *		NeedToRebuildAllOutput - All output files need to be rebuilt.
 *		
 *		LanguageIDsInvalidated - The language IDs may have changed since the last run.
 *		LanguageSettingsChanged - The language settings may have changed since the last run.
 *		
 *		CommentTypeIDsInvalidated - The comment type IDs may have changed since the last run.
 *		CommentTypeSettingsChanged - The comment type settings may have changed since the last run.
 *		
 *		FileIDsInvalidated - The file IDs may have changed since the last run.
 *		CodeIDsInvalidated - Any of the ID types related to the contents of source files may have changed since the last run.  This 
 *										covers class IDs, link IDs, and context IDs.  Link IDs are covered both here and in <CommentIDsInvalidated>
 *										because they can be either class parent links or Natural Docs links.
 *		CommentIDsInvalidated - Any of the ID types related to the comments and documentation may have changed since the last
 *											  run.  This covers topic IDs, link IDs, and image link IDs.  Link IDs are covered both here and in
 *											  <CodeIDsInvalidated> because they can be either Natural Docs links or class parent links.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine
	{

	[Flags]
	public enum StartupIssues: ushort
		{
		None = 0,

		NeedToStartFresh = 0x0001,
		NeedToReparseAllFiles = 0x0002,
		NeedToReparseStyleFiles = 0x0004,
		NeedToRebuildAllOutput = 0x0008,

		LanguageIDsInvalidated = 0x0010,
		LanguageSettingsChanged = 0x0020,

		CommentTypeIDsInvalidated = 0x0040,
		CommentTypeSettingsChanged = 0x0080,

		FileIDsInvalidated = 0x0100,
		CodeIDsInvalidated = 0x0200,
		CommentIDsInvalidated = 0x0400
		}

	}