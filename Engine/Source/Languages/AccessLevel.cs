/* 
 * Enum: GregValure.NaturalDocs.Engine.Languages.AccessLevel
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
 *							   protected or internal can access it.  It doesn't have to be both.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Languages
	{
	public enum AccessLevel : byte
		{
		Unknown = 0,
		Public,
		Private,
		Protected,
		Internal,
		ProtectedInternal
		}
	}