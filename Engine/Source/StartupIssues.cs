/* 
 * Enum: CodeClear.NaturalDocs.Engine.StartupIssues
 * ____________________________________________________________________________
 * 
 * A set of flags that track certain issues that can occur during engine startup.
 * 
 *		None - None of the below sflags have been set.
 *		
 *		NeedToStartFresh - You should avoid loading any data from the previous run.
 *		NeedToReparseAllFiles - All source files need to be reparsed.
 *		NeedToRebuildAllOutput - All output files need to be rebuilt.
 * 
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
		NeedToRebuildAllOutput = 0x0004
		}

	}