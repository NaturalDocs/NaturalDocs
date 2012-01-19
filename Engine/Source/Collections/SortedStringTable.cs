/* 
 * Class: GregValure.NaturalDocs.Engine.Collections.SortedStringTable
 * ____________________________________________________________________________
 * 
 * A generic sorted lookup table for mapping strings to other objects.  This is preferable to a 
 * SortedDictionary<string, object> class because
 * 
 * - It has case sensitivity and Unicode normalization flags.
 * - Reading non-existant keys returns null instead of throwing an exception.
 * - Using <Add()> on a preexisting key overwrites the value instead of throwing an exception.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Collections
	{
	public class SortedStringTable<ObjectType> : SortedDictionary<string, ObjectType>
		{
		
		/* Function: SortedStringTable
		 * Creates an empty table using the default string comparer.
		 */
		public SortedStringTable (bool ignoreCase, bool normalizeUnicode) : base()
			{
			this.ignoreCase = ignoreCase;
			this.normalizeUnicode = normalizeUnicode;
			}
			
			
		/* Function: SortedStringTable
		 * Creates an empty table with a custom string comparer.
		 */
		public SortedStringTable (bool ignoreCase, bool normalizeUnicode, IComparer<string> comparer) : base(comparer)
			{
			this.ignoreCase = ignoreCase;
			this.normalizeUnicode = normalizeUnicode;
			}
			
			
		/* Function: Add
		 * Adds a new value to the table, overwriting the previous value if it already existed.
		 */
		new public void Add (string key, ObjectType value)
			{
			// We do this so it doesn't throw an exception if the value already exists.  The overloaded operator will handle
			// the flags.
			this[key] = value;
			}
			
			
		/* Function: Remove
		 * Removes a value from the table.  Returns whether the key was present in the table or not.  It does not throw an
		 * exception if it did not exist.
		 */
		new public bool Remove (string key)
			{
			return base.Remove( key.NormalizeKey(ignoreCase, normalizeUnicode) );
			}
			
			
		/* Function: ContainsKey
		 * Returns whether the table contains a specific key.  It will always return false for null.
		 */
		new public bool ContainsKey (string key)
			{
			if (key == null)
				{  return false;  }
			
			return base.ContainsKey( key.NormalizeKey(ignoreCase, normalizeUnicode) );
			}
			
			
		/* Operator: this
		 * An index operator.  When getting, returns null if the key doesn't exist instead of throwing an exception.  When setting,
		 * creates an entry for the key or overwrites the existing one if it doesn't exist.
		 */
		new public ObjectType this [string key]
			{
			get
				{
				if (key == null)
					{  return default(ObjectType);  }
					
				// We do this to make it so it doesn't throw an exception if the key doesn't exist.
				ObjectType value;
				bool success = TryGetValue( key.NormalizeKey(ignoreCase, normalizeUnicode), out value);
				
				if (success)
					{  return value;  }
				else
					{  return default(ObjectType);  }
				}
			set
				{  
				base[ key.NormalizeKey(ignoreCase, normalizeUnicode) ] = value;  
				}
			}
						
			
			
		// Group: Variables
		// __________________________________________________________________________
		
			
		/* var: ignoreCase
		 * Whether the table has case sensitive keys.
		 */
		protected bool ignoreCase;
		
		/* var: normalizeUnicode
		 * Whether the table uses Unicode normalization for its keys.
		 */
		protected bool normalizeUnicode;
		
		}
	}