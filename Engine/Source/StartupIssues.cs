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
 *		FileIDsInvalidated - The file IDs may have changed since the last run.
 *		CodeIDsInvalidated - Any of the ID types related to the contents of source files may have changed since the last run.  This
 *										covers class IDs, language IDs, link IDs, and context IDs.  Link IDs are covered both here and in
 *										<CommentIDsInvalidated> because they can be either class parent links or Natural Docs links.
 *		CommentIDsInvalidated - Any of the ID types related to the comments and documentation may have changed since the last
 *											  run.  This covers topic IDs, comment type IDs, link IDs, and image link IDs.  Link IDs are covered
 *											  both here and in <CodeIDsInvalidated> because they can be either Natural Docs links or class
 *											  parent links.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine
	{

	[Flags]
	public enum StartupIssues: byte
		{
		None = 0,

		NeedToStartFresh = 0x01,
		NeedToReparseAllFiles = 0x02,
		NeedToReparseStyleFiles = 0x04,
		NeedToRebuildAllOutput = 0x08,

		FileIDsInvalidated = 0x10,
		CodeIDsInvalidated = 0x20,
		CommentIDsInvalidated = 0x40
		}

	}
