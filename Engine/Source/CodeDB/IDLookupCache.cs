/*
 * Class: CodeClear.NaturalDocs.Engine.CodeDB.IDLookupCache
 * ____________________________________________________________________________
 *
 * A class that provides a local cache of string to ID mappings.  The string class must implement
 * <Collections.ILookupKey>.
 *
 * This is preferable to a <Engine.Collections.StringTable> because it doesn't need the key normalization
 * and it has different behavior when dealing with preexisting, missing, or null keys.  It also uses
 * <Collections.ILookupKey> to allow for per-item case insensitivity like in <Symbols.ClassStrings>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.CodeDB
	{
	public class IDLookupCache<LookupType> : System.Collections.Generic.Dictionary<string, int> where LookupType: Collections.ILookupKey
		{

		/* Function: Add
		 * Adds a new ID to the table.  If the key already existed, the ID must match the existing value or it will throw
		 * an exception.  You cannot add the null key.
		 */
		public void Add (LookupType key, int id)
			{
			// Let the index operator handle the logic.
			this[key] = id;
			}


		/* Function: Remove
		 * Removes a value from the table.  Will not throw an exception if it did not exist.
		 */
		public void Remove (LookupType key)
			{
			if (key != null)
				{  base.Remove(key.LookupKey);  }
			}


		/* Function: Contains
		 * Returns whether the table contains a specific key.  Will always return true for null.
		 */
		public bool Contains (LookupType key)
			{
			if (key == null)
				{  return true;  }
			else
				{  return base.ContainsKey(key.LookupKey);  }
			}


		/* Operator: this
		 * An index operator.  When getting, this will throw an exception if the key doesn't exist.  Getting a null
		 * key will always return zero.  When setting, if there's a preexisting value for the key it must match the
		 * new value.  You cannot set a null key to anything other than zero.
		 */
		public int this [LookupType key]
			{
			get
				{
				string stringKey = key.LookupKey;

				if (stringKey == null)
					{  return 0;  }
				else
					{
					// Will throw an exception if the key doesn't exist.
					return base[stringKey];
					}
				}
			set
				{
				string stringKey = key.LookupKey;

				if (stringKey == null)
					{
					#if DEBUG
					if (value != 0)
						{  throw new Exception("Tried to assign a non-zero value to the null key.");  }
					#endif
					}
				else
					{
					#if DEBUG
					if (ContainsKey(stringKey) && base[stringKey] != value)
						{  throw new Exception("Tried to assign " + value + " to \"" + stringKey + "\" when it was already set to " + base[stringKey] + ".");  }
					#endif

					base[stringKey] = value;
					}
				}
			}

		}
	}
