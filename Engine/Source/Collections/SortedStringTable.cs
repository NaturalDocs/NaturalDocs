﻿/*
 * Class: CodeClear.NaturalDocs.Engine.Collections.SortedStringTable
 * ____________________________________________________________________________
 *
 * A generic sorted lookup table for mapping strings to other objects.  This is preferable to a
 * SortedDictionary<string, object> class because
 *
 * - It can apply the normalizations in <KeySettings>.
 * - Reading non-existent keys returns null instead of throwing an exception.
 * - Using <Add()> on a preexisting key overwrites the value instead of throwing an exception.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Collections
	{
	public class SortedStringTable<ObjectType> : SortedDictionary<string, ObjectType>
		{

		/* Function: SortedStringTable
		 * Creates an empty table using the default string comparer.
		 */
		public SortedStringTable (KeySettings keySettings = KeySettings.Literal) : base()
			{
			this.keySettings = keySettings;
			}


		/* Function: SortedStringTable
		 * Creates an empty table with a custom string comparer.
		 */
		public SortedStringTable (IComparer<string> comparer, KeySettings keySettings = KeySettings.Literal) : base(comparer)
			{
			this.keySettings = keySettings;
			}


		/* Function: Add
		 * Adds a new value to the table, overwriting the previous value if it already existed.
		 */
		new public void Add (string key, ObjectType value)
			{
			// We do this so it doesn't throw an exception if the value already exists.  The overloaded operator will handle
			// keySettings.
			this[key] = value;
			}


		/* Function: Remove
		 * Removes a value from the table.  Returns whether the key was present in the table or not.  It does not throw an
		 * exception if it did not exist.
		 */
		new public bool Remove (string key)
			{
			return base.Remove( key.NormalizeKey(keySettings) );
			}


		/* Function: ContainsKey
		 * Returns whether the table contains a specific key.  It will always return false for null.
		 */
		new public bool ContainsKey (string key)
			{
			if (key == null)
				{  return false;  }

			return base.ContainsKey( key.NormalizeKey(keySettings) );
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
				bool success = TryGetValue( key.NormalizeKey(keySettings), out value);

				if (success)
					{  return value;  }
				else
					{  return default(ObjectType);  }
				}
			set
				{
				base[ key.NormalizeKey(keySettings) ] = value;
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: keySettings
		 * Which normalizations to apply to the keys.
		 */
		protected KeySettings keySettings;

		}
	}
