/* 
 * Class: GregValure.NaturalDocs.Engine.IDObjects.Manager
 * ____________________________________________________________________________
 * 
 * A class for managing objects that have to be referenced either by a string ID or a unique numeric ID.  This is a generic
 * class.  Set the type to be an object derived from <IDObjects.Base>.
 * 
 * 
 * Topic: Usage
 * 
 *		- All objects with known ID numbers *must* be added before those with unknown numbers.  Otherwise there will be
 *		  collisions which will cause exceptions to be thrown.
 *		  
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Collections;


namespace GregValure.NaturalDocs.Engine.IDObjects
	{
	public class Manager<IDObjectType> : IEnumerable<IDObjectType> where IDObjectType: IDObjects.Base 
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Manager
		 */
		public Manager (bool ignoreCase, bool normalize)
			{
			usedIDs = new IDObjects.NumberSet();
			objectsByID = new List<IDObjectType>();
			objectsByName = new StringTable<IDObjectType>(ignoreCase, normalize);
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
			if (objectsByName.ContainsKey(newObject.Name))
				{  throw new InvalidOperationException("Tried to add an IDObject with a name that was already used.");  }
			if (newObject.ID != 0 && newObject.ID < objectsByID.Count && objectsByID[newObject.ID] != null)
				{  throw new InvalidOperationException("Tried to add an IDObject with an ID that was already used.");  }
				
			if (newObject.ID == 0)
				{  newObject.ID = usedIDs.LowestAvailable;  }
				
			usedIDs.Add(newObject.ID);
				
			objectsByName.Add(newObject.Name, newObject);
			
			if (newObject.ID < objectsByID.Count)
				{
				objectsByID[newObject.ID] = newObject;
				}
			else
				{
				// If it's more than one past the end of the array we need to pad it with nulls.
				if (newObject.ID > objectsByID.Count)
					{
					// If it's higher than the capacity, manually update it because we don't want it to reallocate more than once if it's
					// far past the end of it.
					if (newObject.ID >= objectsByID.Capacity)
						{  objectsByID.Capacity = newObject.ID + 1;  }
						
					// Add null entries until we're right before the one we want to add.
					for (int i = objectsByID.Count; i < newObject.ID; i++)
						{  objectsByID.Add(null);  }
					}

				objectsByID.Add(newObject);
				}
			}
			
			
		/* Function: Remove (string)
		 * Removes the object with the associated textual name.  Returns whether it was present in the set.  It does not throw an
		 * exception if it was not.  After removal the associated ID will be available for assignment again.
		 */
		public bool Remove (string name)
			{
			IDObjects.Base obj = this[name];
			
			if (obj == null)
				{  return false;  }
			else
				{
				objectsByID[obj.ID] = null;
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
			IDObjects.Base obj = this[id];
			
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
				if (id < 0 || id >= objectsByID.Count)
					{  return null;  }
				else
					{  return objectsByID[id];  }
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
			if (id >= objectsByID.Count)
				{  return false;  }
			else
				{  return (objectsByID[id] != null);  }
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
		IEnumerator<IDObjectType> System.Collections.Generic.IEnumerable<IDObjectType>.GetEnumerator()
			{
			return new ManagerEnumerator<IDObjectType>(this);
			}
			
		// Function: GetEnumerator
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
			return new ManagerEnumerator<IDObjectType>(this);
			}
		
		
		
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: usedIDs
		 * The set of used identifiers.
		 */
		protected internal IDObjects.NumberSet usedIDs;
		
		
		/* var: objectsByID
		 * An array of objects where the index location corresponds to its numeric ID.
		 */
		protected List<IDObjectType> objectsByID;
	
		
		/* var: objectsByName
		 * A <StringTable> translating textual names to their objects.
		 */
		protected StringTable<IDObjectType> objectsByName;
		}
		
		
		
		
	/* ___________________________________________________________________________
	 * 
	 * Class: GregValure.NaturalDocs.Engine.IDObjects.ManagerEnumerator
	 * ___________________________________________________________________________
	 * 
	 * An enumerator class that allows <IDObjects.Manager> to be used with foreach statements.
	 * 
	 */
	 
	 public class ManagerEnumerator<IDObjectType> : IEnumerator<IDObjectType> where IDObjectType: IDObjects.Base
		{
		
		public ManagerEnumerator (Manager<IDObjectType> newManager)
			{
			manager = newManager;
			numberSetEnumerator = newManager.usedIDs.GetEnumerator();
			}
			
		IDObjectType System.Collections.Generic.IEnumerator<IDObjectType>.Current
			{
			get
				{
				return manager[ numberSetEnumerator.Current ];
				}
			}
			
		object System.Collections.IEnumerator.Current
			{
			get
				{
				return manager[ numberSetEnumerator.Current ];
				}
			}
			
		public bool MoveNext()
			{
			return numberSetEnumerator.MoveNext();
			}
			
		public void Reset()
			{
			numberSetEnumerator.Reset();
			}
			
		public void Dispose()
			{
			}
			
		protected Manager<IDObjectType> manager;
		protected NumberSetEnumerator numberSetEnumerator;
		}
	 
	 
	}