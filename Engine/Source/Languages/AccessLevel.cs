/*
 * Enum: CodeClear.NaturalDocs.Engine.Languages.AccessLevel
 * ____________________________________________________________________________
 *
 * An enum representing a member access level.
 *
 * Unknown - The access level is unknown.  This should only be used with comments or code elements
 *					under basic language support.  It should never be used with code elements under full
 *					language support.
 *
 * Public - Public access level.
 * Protected - Protected access level.
 * Private - Private access level.
 * Internal - Internal access level.
 * ProtectedInternal - C#'s "protected internal" access level, which means things that are *either*
 *							   derived classes or in the same assembly can access it.
 * PrivateProtected - C#'- "private protected" access level, which means things that are *both*
 *							   derived classes and in the same assembly can access it.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Languages
	{
	public enum AccessLevel : byte
		{
		Unknown = 0,
		Public,
		Private,
		Protected,
		Internal,
		ProtectedInternal,
		PrivateProtected
		}
	}
