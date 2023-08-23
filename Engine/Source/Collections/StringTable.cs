/*
 * Class: CodeClear.NaturalDocs.Engine.Collections.StringTable
 * ____________________________________________________________________________
 *
 * A generic lookup table for mapping strings to other objects.  This is preferable to a Dictionary<string, object> class
 * because:
 *
 * - It can apply the normalizations in <KeySettings>.
 * - Reading non-existent keys returns null instead of throwing an exception.
 * - Using <Add()> on a preexisting key overwrites the value instead of throwing an exception.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Collections
	{
	public class StringTable<ObjectType> : System.Collections.Generic.Dictionary<string, ObjectType>
		{

		/* Function: StringTable
		 * Creates an empty table.
		 */
		public StringTable (KeySettings keySettings = KeySettings.Literal) : base()
			{
			this.keySettings = keySettings;
			}


		/* Function: Add
		 * Adds a new value to the table, overwriting the previous value if it already existed.
		 */
		new public void Add (string key, ObjectType value)
			{
			// We do this so it doesn't throw an exception if the value already exists.  The overloaded operator will handle
			// case sensitivity and normalization.
			this[ key.NormalizeKey(keySettings) ] = value;
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
		 * Returns whether the table contains a specific key.  Always returns false for null.
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


		/* Function: operator ==
		 */
		public static bool operator== (StringTable<ObjectType> table1, StringTable<ObjectType> table2)
			{
			if ((object)table1 == null && (object)table2 == null)
				{  return true;  }
			else if ((object)table1 == null || (object)table2 == null)
				{  return false;  }
			else if (table1.Count != table2.Count)
				{  return false;  }
			else
				{
				foreach (System.Collections.Generic.KeyValuePair<string, ObjectType> pair in table1)
					{
					if (table2.ContainsKey(pair.Key) == false)
						{  return false;  }

					ObjectType otherValue = table2[pair.Key];

					if ( ((object)pair.Value == null && (object)otherValue != null) ||
					     ((object)pair.Value != null && pair.Value.Equals(otherValue) == false) )
						{  return false;  }
					}

				return true;
				}
			}


		/* Function: operator !=
		 */
		public static bool operator!= (StringTable<ObjectType> table1, StringTable<ObjectType> table2)
			{
			return !(table1 == table2);
			}


		/* Function: Equals
		 */
		public override bool Equals (object other)
			{
			if (other == null || !(other is StringTable<ObjectType>))
				{  return false;  }
			else
				{  return (this == (StringTable<ObjectType>)other);  }
			}


		/* Function: GetHashCode
		 */
		public override int GetHashCode ()
			{
			return base.GetHashCode();
			}


		/* var: keySettings
		 * What normalizations to apply to the keys.
		 */
		protected KeySettings keySettings;

		}
	}
