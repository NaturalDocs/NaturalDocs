/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.IDLookupCache
 * ____________________________________________________________________________
 * 
 * A class that provides a local cache of string to ID mappings.
 * 
 * This is preferable to a <Engine.Collections.StringTable> because it doesn't need the key normalization.
 * This is preferable to a System.Collections.Generic.Dictionary because we need to accept null strings
 * as valid keys.  However, like <Engine.Collections.StringTable>:
 * 
 * - Reading non-existent keys returns null instead of throwing an exception.
 * - Using <Add()> on a preexisting key overwrites the value instead of throwing an exception.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.CodeDB
	{
	public class IDLookupCache<LookupType> : System.Collections.Generic.Dictionary<string, int>
		{
		
		/* Function: Add
		 * Adds a new ID to the table, overwriting the previous value if it already existed.
		 */
		public void Add (int id, LookupType key)
			{
			// Sanity check
			#if DEBUG
			string stringKey = StringKey(key);

			if (ContainsKey(stringKey) && this[stringKey] != id)
				{  
				throw new Exception("Tried to add \"" + stringKey + "\" to IDLookupCache with ID " + id + 
														" when it was already added with ID " + this[stringKey] + ".");
				}
			#endif

			// We do this so it doesn't throw an exception if the value already exists.
			this[ StringKey(key) ] = id;
			}
			
			
		/* Function: Remove
		 * Removes a value from the table.  Returns whether the key was present in the table or not.  It does not throw an
		 * exception if it did not exist.
		 */
		public bool Remove (LookupType key)
			{
			return base.Remove( StringKey(key) );
			}
			
			
		/* Function: Contains
		 * Returns whether the table contains a specific key.
		 */
		public bool Contains (LookupType key)
			{
			return base.ContainsKey( StringKey(key) );
			}


		/* Function: Contains
		 * Returns whether the table contains a specific key.  This is only defined to allow null lookups.
		 */
		public bool Contains (object key)
			{
			#if DEBUG
			if (key != null)
				{  throw new Exception("Can only use IDLookupCache.Contains(object) with null.");  }
			#endif

			return base.ContainsKey(NullStandIn);
			}
			
			
		/* Operator: this
		 * An index operator.  When getting, returns zero if the key doesn't exist instead of throwing an exception.  When setting,
		 * creates an entry for the key or overwrites the existing one if it doesn't exist.
		 */
		public int this [LookupType key]
			{
			get
				{
				// We do this to make it so it doesn't throw an exception if the key doesn't exist.
				int id;
				bool success = TryGetValue( StringKey(key), out id);
				
				if (success)
					{  return id;  }
				else
					{  return 0;  }
				}
			set
				{  
				base[ StringKey(key) ] = value;  
				}
			}
			
			
		/* Function: StringKey
		 * Returns a string version of the key to use with the Dictionary, converting it to <NullStandIn> if necessary.
		 */
		protected string StringKey (LookupType key)
			{
			string stringKey = key.ToString();

			if (stringKey == null)
				{  stringKey = NullStandIn;  }

			return stringKey;
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: NullStandIn
		 * A string to use in place of null, since the lookup types can be null but the Dictionary class doesn't allow null keys.
		 * It uses <Symbols.SeparatorChars.Escape> to make sure it doesn't conflict with any actual keys.
		 */
		protected static string NullStandIn = Symbols.SeparatorChars.Escape + "null";
		
		}
	}