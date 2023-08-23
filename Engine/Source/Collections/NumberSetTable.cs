/*
 * Class: CodeClear.NaturalDocs.Engine.Collections.NumberSetTable
 * ____________________________________________________________________________
 *
 * A generic lookup table for mapping something to NumberSets.  This is preferable to a Dictionary<object, NumberSet>
 * class because:
 *
 * - Adding and removing individual numbers automatically creates and deletes NumberSets.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Collections
	{
	public class NumberSetTable<ObjectType> : SafeDictionary<ObjectType, IDObjects.NumberSet>
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: NumberSetTable
		 * Creates an empty table.
		 */
		public NumberSetTable () : base()
			{
			}


		/* Function: Add
		 * Adds a new value to the table.
		 */
		public void Add (ObjectType key, int value)
			{
			IDObjects.NumberSet numberSet = this[key];

			if (numberSet == null)
				{
				numberSet = new IDObjects.NumberSet();
				this[key] = numberSet;
				}

			numberSet.Add(value);
			}


		/* Function: Remove
		 * Removes a value from the table.  Returns whether it was present in the table or not.  It does not throw an
		 * exception if it did not exist.
		 */
		public bool Remove (ObjectType key, int value)
			{
			IDObjects.NumberSet numberSet = this[key];

			if (numberSet == null)
				{  return false;  }

			bool result = numberSet.Remove(value);

			if (numberSet.IsEmpty)
				{  this.Remove(key);  }

			return result;
			}


		/* Function: Pop
		 * Removes and returns one key and numberset from the table.  Returns false if it was empty.
		 */
		public bool Pop (out ObjectType key, out IDObjects.NumberSet numberSet)
			{
			if (IsEmpty)
				{
				key = default(ObjectType);
				numberSet = null;
				return false;
				}
			else
				{
				var enumerator = base.GetEnumerator();
				enumerator.MoveNext();  // It's not positioned on the first element by default.

				key = enumerator.Current.Key;
				numberSet = enumerator.Current.Value;

				base.Remove(key);

				return true;
				}
			}


		/* Function: LowestAvailable
		 * The lowest unused number available for the passed key, starting at one.
		 */
		public int LowestAvailable (ObjectType key)
			{
			IDObjects.NumberSet numberSet = this[key];

			if (numberSet == null)
				{  return 1;  }
			else
				{  return numberSet.LowestAvailable;  }
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: IsEmpty
		 * Returns whether the table is empty, which is faster than testing <Count> if that's all you need.
		 */
		public bool IsEmpty
			{
			get
				{
				return (base.Count == 0);
				}
			}


		/*	Property: Count
		 *	Returns the total number of values across all keys.
		 */
		new public int Count
			{
			get
				{
				int count = 0;

				foreach (var kvPair in this)
					{  count += kvPair.Value.Count;  }

				return count;
				}
			}

		}
	}
