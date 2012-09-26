/* 
 * Interface: GregValure.NaturalDocs.Engine.Collections.ILookupKey
 * ____________________________________________________________________________
 * 
 * An interface for any class that needs to be indexed with a specific key that differs from what's returned by ToString().
 * 
 * For example, <Symbols.ClassString>'s normal value is case sensitive but if its language is not it returns a lowercase 
 * string for the key instead.  It doesn't use the lowercase key as the normal value because it wants to preserve the
 * case as entered for display.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Collections
	{
	public interface ILookupKey
		{
		
		/* Property: LookupKey
		 * Returns the key that should be used when finding entries in the cache.
		 */
		string LookupKey { get; }
		
		}
	}