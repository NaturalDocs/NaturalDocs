/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.ContextReferenceCache
 * ____________________________________________________________________________
 * 
 * A class that stores changes to context ID reference counts in memory so they don't have to hit the 
 * database on every operation.
 * 
 * Usage:
 * 
 *		- Create the cache.
 *		
 *		- Every time a reference is added or deleted, call <AddReference()> or <RemoveReference()>.
 *		
 *		- If you know the database's stored reference count for a context that's likely to change, such as for
 *		  a newly created context ID, send it to the cache with <SetDatabaseReferences()>.  This will lessen
 *		  the database work when the cache is flushed, but don't do this for references that might not change
 *		  or you'll just be filling up memory with unnecessary cache entries.
 *		  
 *		- When it's time to flush the cache...
 *		
 *			- Use foreach to iterate through the <ContextReferenceCacheEntries>.
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


namespace GregValure.NaturalDocs.Engine.CodeDB
	{
	public class ContextReferenceCache : IDObjects.Manager<ContextReferenceCacheEntry>
		{
		
		/* Constructor: ContextReferenceCache
		 */
		public ContextReferenceCache() : base (false, false, true)
			{
			}


		/* Function: AddReference
		 * Increases the reference count of the passed context ID and string.
		 */
		public void AddReference (int contextID, Symbols.ContextString contextString)
			{
			// Sanity checks
			#if DEBUG
			if (contextID == 0)
				{  throw new Exception("Context ID must not be zero");  }

			if (Contains(contextID) && this[contextID].String != contextString)
				{  
				throw new Exception("Passed \"" + contextString + "\" to ContextReferenceCache with ID " + contextID + 
														" when the ID was already used for \"" + this[contextID].String + "\".");
				}
			#endif

			ContextReferenceCacheEntry cacheEntry = this[contextID];

			if (cacheEntry == null)
				{
				cacheEntry = new ContextReferenceCacheEntry();
				cacheEntry.ID = contextID;
				cacheEntry.String = contextString;
				cacheEntry.Change = 1;

				this.Add(cacheEntry);
				}
			else
				{  cacheEntry.Change++;  }
			}


		/* Function: RemoveReference
		 * Decreases the reference count of the passed context ID and string.
		 */
		public void RemoveReference (int contextID, Symbols.ContextString contextString)
			{
			// Sanity checks
			#if DEBUG
			if (contextID == 0)
				{  throw new Exception("Context ID must not be zero");  }

			if (Contains(contextID) && this[contextID].String != contextString)
				{  
				throw new Exception("Passed \"" + contextString + "\" to ContextReferenceCache with ID " + contextID + 
														" when the ID was already used for \"" + this[contextID].String + "\".");
				}
			#endif

			ContextReferenceCacheEntry cacheEntry = this[contextID];

			if (cacheEntry == null)
				{
				cacheEntry = new ContextReferenceCacheEntry();
				cacheEntry.ID = contextID;
				cacheEntry.String = contextString;
				cacheEntry.Change = -1;

				this.Add(cacheEntry);
				}
			else
				{  cacheEntry.Change--;  }
			}


		/* Function: SetDatabaseReferences
		 * 
		 * Sets the number of references the passed context ID and string have according to the database.  This is before
		 * any changes stored in this cache are applied.
		 * 
		 * This is primarily used to fill in the existing entries before flushing the cache.  However, you can also use this to fill 
		 * in known data before that point to avoid a trip to the database later.  It's recommended that you only do this for
		 * entries that are guaranteed to change, such as newly created context IDs, so you don't balloon the cache with
		 * unnecessary data.
		 */
		public void SetDatabaseReferences (int contextID, Symbols.ContextString contextString, int databaseReferences)
			{
			// Sanity checks
			#if DEBUG
			if (contextID == 0)
				{  throw new Exception("Context ID must not be zero");  }

			if (Contains(contextID))
				{  
				if (this[contextID].String != contextString)
					{
					throw new Exception("Passed \"" + contextString + "\" to ContextReferenceCache with ID " + contextID + 
															" when the ID was already used for \"" + this[contextID].String + "\".");
					}
				if (this[contextID].DatabaseCountKnown && this[contextID].DatabaseCount != databaseReferences)
					{
					throw new Exception("Tried to set DatabaseReferences to " + databaseReferences + " on \"" + contextString + "\"" +
															" when the cache thought it was already " + this[contextID].DatabaseCount + ".");
					}
				}
			#endif

			ContextReferenceCacheEntry cacheEntry = this[contextID];

			if (cacheEntry == null)
				{
				cacheEntry = new ContextReferenceCacheEntry();
				cacheEntry.ID = contextID;
				cacheEntry.String = contextString;
				cacheEntry.Change = 0;

				this.Add(cacheEntry);
				}

			cacheEntry.DatabaseCount = databaseReferences;
			}


		new public ContextReferenceCacheEntry this [string name]
			{
			get
				{
				if (name == null)
					{  name = NullStandIn;  }

				return base[name];  
				}
			}

		new public bool Remove (string name)
			{
			if (name == null)
				{  name = NullStandIn;  }

			return base.Remove(name);
			}

		new public bool Contains (string name)
			{
			if (name == null)
				{  name = NullStandIn;  }

			return base.Contains(name);
			}

		/* var: NullStandIn
		 * A string to use in place of null, since <Symbols.ContextStrings> can be null but IDObjects must have Name
		 * set.  It uses <Symbols.SeparatorChars.Escape> to make sure it doesn't conflict with any actual context strings.
		 */
		internal static string NullStandIn = Symbols.SeparatorChars.Escape + "null";

		}



	/* ___________________________________________________________________________
	 * 
	 *	 Class: Gregalure.NaturalDocs.Engine.CodeDB.ContextReferenceCacheEntry
	 *	 __________________________________________________________________________
	 */


	public class ContextReferenceCacheEntry : IDObjects.Base
		{
		public ContextReferenceCacheEntry () : base ()
			{
			contextString = new Symbols.ContextString();
			change = 0;
			databaseCount = -1;
			}

		override public string Name
			{
			get
				{  
				if (contextString == null)
					{  return ContextReferenceCache.NullStandIn;  }
				else
					{  return contextString;  }
				}
			}

		/* Property: String
		 */
		public Symbols.ContextString String
			{
			get
				{  return contextString;  }
			set
				{  contextString = value;  }
			}

		/* Property: Change
		 * The change to the reference count on this context ID.  If it's positive, that many references were added.  If
		 * it's negative, that many were removed.  This value is added to <DatabaseCount> to get the final reference
		 * count, but <DatabaseCount> may be unknown until preparing to flush the cache.
		 */
		public int Change
			{
			get
				{  return change;  }
			set
				{  change = value;  }
			}

		/* Property: DatabaseCount
		 * The number of references this context ID has according to what's stored in the database.  This will be -1 if
		 * it's not known yet.  Once it's known, <Change> must be added to it to get the final value.
		 */
		public int DatabaseCount
			{
			get
				{  return databaseCount;  }
			set
				{  databaseCount = value;  }
			}

		/* Property: DatabaseCountKnown
		 * Whether <DatabaseCount> was retrieved from the database or is unknown.  Is the equivalent of testing
		 * <DatabaseCount> for -1.
		 */
		public bool DatabaseCountKnown
			{
			get
				{  return (databaseCount != -1);  }
			}

		protected Symbols.ContextString contextString;
		protected int change;
		protected int databaseCount;

		}
	}
