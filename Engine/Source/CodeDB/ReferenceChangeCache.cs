/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.ReferenceChangeCache
 * ____________________________________________________________________________
 * 
 * A class that stores changes to reference counts in memory so they don't have to hit the database on 
 * every operation.
 * 
 * This class ignores all operations on ID zero.
 * 
 * Usage:
 * 
 *		- Create the cache.
 *		
 *		- Every time a reference is added or deleted, call <AddReference()> or <RemoveReference()>.
 *		
 *		- If you know the reference count stored in the database for an object that's likely to change, add it to the 
 *		  cached information with <SetDatabaseReferenceCount()>.  This will lessen the database work when the cache 
 *		  is flushed, but don't do this for references that might not change or you'll just be filling up memory with 
 *		  unnecessary cache entries.
 *		  
 *		- When it's time to flush the cache...
 *		
 *			- Use foreach to iterate through the <ReferenceChangeEntries>.
 *		
 *			- Find the database reference count for any entries where it's unknown.
 *			
 *			- Update the database.  Delete any records where the total reference count is now zero, and update 
 *			  any other records where the count has changed.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.CodeDB
	{
	public class ReferenceChangeCache : List<ReferenceChangeEntry>
		{
		
		/* Constructor: ReferenceChangeCache
		 */
		public ReferenceChangeCache() : base ()
			{
			}


		/* Function: AddReference
		 * Increases the reference count of the passed object ID.  This is ignored if the ID is zero.
		 */
		public void AddReference (int objectID)
			{
			if (objectID == 0)
				{  return;  }

			int entryIndex = BinarySearch(objectID);

			if (entryIndex < 0)
				{
				ReferenceChangeEntry cacheEntry = new ReferenceChangeEntry(objectID);
				cacheEntry.ReferenceChange = 1;
				Insert(~entryIndex, cacheEntry);
				}
			else
				{  this[entryIndex].ReferenceChange++;  }
			}


		/* Function: RemoveReference
		 * Decreases the reference count of the passed object ID.  This is ignored if the ID is zero.
		 */
		public void RemoveReference (int objectID)
			{
			if (objectID == 0)
				{  return;  }

			int entryIndex = BinarySearch(objectID);

			if (entryIndex < 0)
				{
				ReferenceChangeEntry cacheEntry = new ReferenceChangeEntry(objectID);
				cacheEntry.ReferenceChange = -1;
				Insert(~entryIndex, cacheEntry);
				}
			else
				{  this[entryIndex].ReferenceChange--;  }
			}


		/* Function: SetDatabaseReferenceCount
		 * 
		 * Sets the number of references the passed object ID has according to the database.  This is before any changes
		 * stored in this cache are applied.
		 * 
		 * This is primarily used to fill in the existing entries before flushing the cache.  However, you can also use this to fill 
		 * in known data before that point to avoid a trip to the database later.  It's recommended that you only do this for
		 * entries that are likely to change so you don't balloon the cache with unnecessary data.
		 */
		public void SetDatabaseReferenceCount (int objectID, int databaseReferences)
			{
			if (objectID == 0)
				{  return;  }

			int entryIndex = BinarySearch(objectID);

			if (entryIndex < 0)
				{
				ReferenceChangeEntry cacheEntry = new ReferenceChangeEntry(objectID);
				cacheEntry.ReferenceChange = 0;
				cacheEntry.DatabaseReferenceCount = databaseReferences;
				Insert(~entryIndex, cacheEntry);
				}
			else
				{  this[entryIndex].DatabaseReferenceCount = databaseReferences;  }
			}


		/* Function: BinarySearch
		 * 
		 * Searches the list for the passed object ID.  If it finds it, the return value will be zero or positive representing the
		 * index of the item.  If it doesn't, the return value will be the bitwise complement of the index the item should be
		 * inserted at.  This is consistent with the system used by System.Collections.Generic.List.BinarySearch().
		 * 
		 * Why not just use the system function with a custom comparer?  Because it only looks up items by object, which
		 * means every time you would want to look up an item by its ID you would need to allocate a temporary object,
		 * which is ridiculously inefficient.
		 * 
		 * Why not just use System.Collections.Generic.SortedList?  Because then other code would have to iterate through
		 * using Pairs to get the ID and other data.  That's a little more awkward and it exposes too much implementation.
		 */
		protected int BinarySearch (int objectID)
			{
			if (Count == 0)
				{  return ~0;  }

			int firstIndex = 0;
			int lastIndex = Count - 1;  // lastIndex is inclusive.
			
			for (;;)
				{
				int testIndex = (firstIndex + lastIndex) / 2;
				
				if (objectID == this[testIndex].ID)
					{  
					return testIndex;  
					}

				else if (objectID < this[testIndex].ID)
					{
					if (testIndex == firstIndex)
						{  return ~testIndex;  }
					else
						{  lastIndex = testIndex - 1;  }
					}
					
				else // (objectID > this[testIndex].ID)
					{
					if (testIndex == lastIndex)
						{  return ~(lastIndex + 1);  }
					else
						{  firstIndex = testIndex + 1;  }
					}
				}
			}

		}



	/* ___________________________________________________________________________
	 * 
	 *	 Class: GregValure.NaturalDocs.Engine.CodeDB.ReferenceChangeEntry
	 *	 __________________________________________________________________________
	 */


	public class ReferenceChangeEntry
		{

		public ReferenceChangeEntry (int objectID)
			{
			#if DEBUG
			if (objectID == 0)
				{  throw new Exception("Cannot create a reference change entry for ID zero.");  }
			#endif 

			id = objectID;
			referenceChange = 0;
			databaseReferenceCount = -1;
			}

		/* Property: ID
		 * The ID of the element the cache entry is for.
		 */
		public int ID
			{
			get
				{  return id;  }
			}

		/* Property: ReferenceChange
		 * The change to the reference count of this <ID>.  If it's positive, that many references were added.  If it's
		 * negative, that many were removed.  If <DatabaseReferenceCount> is known, this value is added to it to 
		 * get the final reference count.
		 */
		public int ReferenceChange
			{
			get
				{  return referenceChange;  }
			set
				{  referenceChange = value;  }
			}

		/* Property: DatabaseReferenceCount
		 * The number of references this <ID> has according to what's stored in the database.  This will be -1 if
		 * it's not known yet.  Once it's known, <ReferenceChange> must be added to it to get the final value.
		 */
		public int DatabaseReferenceCount
			{
			get
				{  return databaseReferenceCount;  }
			set
				{  databaseReferenceCount = value;  }
			}

		/* Property: DatabaseReferenceCountKnown
		 * Whether <DatabaseReferenceCount> was retrieved from the database or is unknown.  Is the equivalent of testing
		 * <DatabaseReferenceCount> for -1.
		 */
		public bool DatabaseReferenceCountKnown
			{
			get
				{  return (databaseReferenceCount != -1);  }
			}

		protected int id;
		protected int referenceChange;
		protected int databaseReferenceCount;

		}
	}
