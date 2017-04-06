
// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Collections
	{

	/* Enum: KeySettings
	 * 
	 * The way to handle keys in collections like <StringSet>.  These are flags and may be combined.
	 * 
	 * Literal - Don't apply any processing, just use the literal string.
	 * IgnoreCase - The keys should be case-insensitive.
	 * NormalizeUnicode - The key should have Unicode compatibility normalization applied (FormKC).
	 */
	[Flags]
	public enum KeySettings : byte
		{  Literal = 0x00, IgnoreCase = 0x01, NormalizeUnicode = 0x02  }
			
	}