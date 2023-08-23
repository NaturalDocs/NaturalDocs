/*
 * Class: CodeClear.NaturalDocs.Engine.Collections.SafeDictionary
 * ____________________________________________________________________________
 *
 * A variation of .NET's Dictionary class that uses null returns instead of exceptions.
 *
 * - Reading non-existent keys returns null (or the default for value types) instead of throwing an exception.
 * - Using <Add()> on a preexisting key overwrites the value instead of throwing an exception.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Collections
	{
	public class SafeDictionary<KeyType, ValueType> : System.Collections.Generic.Dictionary<KeyType, ValueType>
		{

		/* Function: Add
		 * Adds a new value to the table, overwriting the previous value if it already existed.
		 */
		new public void Add (KeyType key, ValueType value)
			{
			this[key] = value;
			}


		/* Operator: this
		 * An index operator.  When getting, returns null (or the default for value types) if the key doesn't exist instead of
		 * throwing an exception.  When setting, creates an entry for the key or overwrites the existing one if it doesn't exist.
		 */
		new public ValueType this [KeyType key]
			{
			get
				{
				ValueType value = default(ValueType);
				TryGetValue(key, out value);
				return value;
				}
			set
				{
				base[key] = value;
				}
			}

		}
	}
