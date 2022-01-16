/*
 * Class: CodeClear.NaturalDocs.Engine.IDObjects.Manager
 * ____________________________________________________________________________
 *
 * A class for managing objects that have to be referenced either by a string ID or a unique numeric ID.  This is a generic
 * class.  Set the type to be an object derived from <IDObject>.
 *
 *
 * Topic: Usage
 *
 *		- All objects with known ID numbers *must* be added before those with unknown numbers.  Otherwise there will be
 *		  collisions which will cause exceptions to be thrown.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.IDObjects
	{
	public class Manager<IDObjectType> : IEnumerable<IDObjectType> where IDObjectType: IDObjects.IDObject
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: Manager
		 *
		 * Creates a new IDObject manager.
		 *
		 * Parameters:
		 *
		 *		keySettings - The <KeySettings> that should apply when referencing objects by name.
		 *
		 *		sparse - If false, it assumes the manager will be handling objects with low and mostly consecutive ID numbers.
		 *					This allows it to store them in an array where the index maps directly to the ID number, which is very
		 *					fast.  However, if there are going to be large gaps in the IDs stored this will waste a lot of memory.
		 *
		 *					If true, it assumes the manager will be handling objects with high and/or non-consecutive ID numbers
		 *					with large gaps between the values.  This means it will store them in a sorted array and use a binary
		 *					search for lookups and insertions.  This is slower but more memory efficient.
		 */
		public Manager (KeySettings keySettings, bool sparse)
			{
			usedIDs = new IDObjects.NumberSet();
			objectsByID = new List<IDObjectType>();
			objectsByName = new StringTable<IDObjectType>(keySettings);
			this.sparse = sparse;
			}


		/* Function: Add
		 * Adds a new object to the manager.  The objects Name must be set.  If the object's ID is set, it will attempt to add
		 * it using that ID number and throw an exception if it's already taken.  If it's not set, it will assign it the lowest
		 * available ID.  For that reason you should add all objects with known IDs before adding any that need them assigned.
		 */
		public void Add (IDObjectType newObject)
			{
			if (string.IsNullOrEmpty(newObject.Name))
				{  throw new ArgumentException("Tried to add an IDObject that didn't have a name set.");  }
			else if (Contains(newObject.Name))
				{  throw new InvalidOperationException("Tried to add an IDObject with a name that was already used.");  }

			if (newObject.ID == 0)
				{  newObject.ID = usedIDs.LowestAvailable;  }
			else if (Contains(newObject.ID))
				{  throw new InvalidOperationException("Tried to add an IDObject with an ID that was already used.");  }

			usedIDs.Add(newObject.ID);

			objectsByName.Add(newObject.Name, newObject);

			if (!sparse)
				{
				if (newObject.ID < objectsByID.Count)
					{
					objectsByID[newObject.ID] = newObject;
					}
				else
					{
					// If it's more than one past the end of the array we need to pad it with nulls.
					if (newObject.ID > objectsByID.Count)
						{
						// If it's significantly higher than the capacity, manually update it because we don't want it to reallocate more than
						// once.
						if (newObject.ID >= objectsByID.Capacity * 2)
							{  objectsByID.Capacity = newObject.ID + 1;  }

						// Add null entries until we're right before the one we want to add.
						for (int i = objectsByID.Count; i < newObject.ID; i++)
							{  objectsByID.Add(null);  }
						}

					objectsByID.Add(newObject);
					}
				}

			else // sparse
				{
				objectsByID.Insert(~BinarySearch(newObject.ID), newObject);
				}
			}


		/* Function: Remove (string)
		 * Removes the object with the associated textual name.  Returns whether it was present in the set.  It does not throw an
		 * exception if it was not.  After removal the associated ID will be available for assignment again.
		 */
		public bool Remove (string name)
			{
			IDObjects.IDObject obj = objectsByName[name];

			if (obj == null)
				{  return false;  }
			else
				{
				if (!sparse)
					{  objectsByID[obj.ID] = null;  }
				else
					{  objectsByID.RemoveAt(BinarySearch(obj.ID));  }

				objectsByName.Remove(name);
				usedIDs.Remove(obj.ID);

				return true;
				}
			}


		/* Function: Remove (id)
		 * Removes the object with the associated numeric ID.  Returns whether it was present in the set.  It does not throw an
		 * exception if it was not.  After removal the associated ID will be available for assignment again.
		 */
		public bool Remove (int id)
			{
			if (!sparse)
				{
				IDObjects.IDObject obj = objectsByID[id];

				if (obj == null)
					{  return false;  }
				else
					{
					objectsByID[id] = null;
					objectsByName.Remove(obj.Name);
					usedIDs.Remove(id);

					return true;
					}
				}

			else // sparse
				{
				int position = BinarySearch(id);

				if (position < 0)
					{  return false;  }
				else
					{
					IDObjects.IDObject obj = objectsByID[position];

					objectsByID.RemoveAt(position);
					objectsByName.Remove(obj.Name);
					usedIDs.Remove(id);

					return true;
					}
				}
			}


		/* Function: this (string)
		 * An index operator to retrieve the object with the associated textual name, or null if there isn't one.
		 */
		public IDObjectType this [string name]
			{
			get
				{  return objectsByName[name];  }
			}


		/* Function: this (int)
		 * An index operator to retrieve the object with the associated numeric ID, or null if there isn't one.
		 */
		public IDObjectType this [int id]
			{
			get
				{
				if (!sparse)
					{
					if (id < 0 || id >= objectsByID.Count)
						{  return null;  }
					else
						{  return objectsByID[id];  }
					}
				else // sparse
					{
					int position = BinarySearch(id);

					if (position >= 0)
						{  return objectsByID[position];  }
					else
						{  return null;  }
					}
				}
			}


		/* Function: Contains (string)
		 * Returns whether an object exists with the passed textual name.
		 */
		public bool Contains (string name)
			{
			return objectsByName.ContainsKey(name);
			}


		/* Function: Contains (int)
		 * Returns whether an object exists with the passed numeric ID.
		 */
		public bool Contains (int id)
			{
			return usedIDs.Contains(id);
			}


		/* Function: GetUsedIDs
		 * Returns a <NumberSet> of all the used IDs.  The returned set is an independent copy, which means you can
		 * change it without affecting this object, and it's a snapshot that will not reflect future changes to this object.
		 */
		public NumberSet GetUsedIDs ()
			{
			return new NumberSet(usedIDs);
			}


		/* Function: Clear
		 * Removes all objects, making the manager empty.
		 */
		public void Clear ()
			{
			usedIDs.Clear();
			objectsByID.Clear();
			objectsByName.Clear();
			}


		/* Function: BinarySearch
		 *
		 * Searches <objectsByID> for the passed ID.  If it finds it, the return value will be zero or positive representing the
		 * index of the item.  If it doesn't, the return value will be the bitwise complement of the index the item should be
		 * inserted at.  This is consistent with the system used by System.Collections.Generic.List.BinarySearch().
		 *
		 * Why not just use the system function with a custom comparer?  Because it only looks up items by object, which
		 * means every time you would want to look up an item by its ID you would need to allocate a temporary object,
		 * which is ridiculously inefficient.
		 */
		protected int BinarySearch (int id)
			{
			if (objectsByID.Count == 0)
				{  return ~0;  }

			int firstIndex = 0;
			int lastIndex = objectsByID.Count - 1;  // lastIndex is inclusive.

			for (;;)
				{
				int testIndex = (firstIndex + lastIndex) / 2;

				if (id == objectsByID[testIndex].ID)
					{
					return testIndex;
					}

				else if (id < objectsByID[testIndex].ID)
					{
					if (testIndex == firstIndex)
						{  return ~testIndex;  }
					else
						{
						// Not testIndex - 1 because even though ID is lower, this may be the position we would insert it at.
						lastIndex = testIndex;
						}
					}

				else // (id > objectsByID[testIndex].ID)
					{
					if (testIndex == lastIndex)
						{  return ~(lastIndex + 1);  }
					else
						{  firstIndex = testIndex + 1;  }
					}
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Count
		 * The number of objects being managed.
		 */
		public int Count
			{
			get
				{
				return usedIDs.Count;
				}
			}



		// Group: Interface Functions
		// __________________________________________________________________________


		// Function: GetEnumerator
		IEnumerator<IDObjectType> IEnumerable<IDObjectType>.GetEnumerator()
			{
			foreach (IDObjectType obj in objectsByID)
				{
				if (obj != null)
					{  yield return obj;  }
				}
			}

		// Function: GetEnumerator
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
			return ((IEnumerable<IDObjectType>)this).GetEnumerator();
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: usedIDs
		 * The set of used identifiers.
		 */
		protected internal IDObjects.NumberSet usedIDs;

		/* var: objectsByID
		 * An array of objects.  If <sparse> is false, the index location corresponds to its numeric ID.  If <sparse> is true, the
		 * objects will be in ID order but you have to use a binary search to find the ID you want.
		 */
		protected List<IDObjectType> objectsByID;

		/* var: objectsByName
		 * A <StringTable> translating textual names to their objects.
		 */
		protected StringTable<IDObjectType> objectsByName;

		/* var: sparse
		 * Whether <objectsByID> is sparse or not.
		 */
		protected bool sparse;

		}
	}
