/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.ContextIDCache
 * ____________________________________________________________________________
 * 
 * A class that provides a local cache of context ID mappings.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.CodeDB
	{
	public class ContextIDCache : IDObjects.Manager<ContextIDCacheEntry>
		{
		
		/* Constructor: ContextIDCache
		 */
		public ContextIDCache() : base (false, false, true)
			{
			}

		/* Function: Add
		 * Adds a context ID/string pair to the cache.  It is okay to call this repeatedly for the same entry.
		 */
		public void Add (int contextID, Symbols.ContextString contextString)
			{
			// Sanity check
			#if DEBUG
			if (Contains(contextID) && this[contextID].String != contextString)
				{  
				throw new Exception("Tried to add \"" + contextString + "\" to ContextIDCache under ID " + contextID + 
														" when the ID was already used for \"" + this[contextID].String + "\".");
				}
			#endif

			if (!Contains(contextID))
				{
				ContextIDCacheEntry cacheEntry = new ContextIDCacheEntry();
				cacheEntry.ID = contextID;
				cacheEntry.String = contextString;

				Add(cacheEntry);
				}
			}

		new public ContextIDCacheEntry this [string name]
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
	 *	 Class: Gregalure.NaturalDocs.Engine.CodeDB.ContextIDCacheEntry
	 *	 __________________________________________________________________________
	 */


	public class ContextIDCacheEntry : IDObjects.Base
		{
		override public string Name
			{
			get
				{  
				if (contextString == null)
					{  return ContextIDCache.NullStandIn;  }
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

		protected Symbols.ContextString contextString;
		}
	}
