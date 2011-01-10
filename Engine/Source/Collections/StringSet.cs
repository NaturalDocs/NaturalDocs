/* 
 * Class: GregValure.NaturalDocs.Engine.Collections.StringSet
 * ____________________________________________________________________________
 * 
 * A general lookup table for tracking the existence of strings in a set, i.e. an existence hash.  This is preferable to 
 * a Dictionary class because
 * 
 * - It has a more straightforward interface for the intended purpose.
 * - It supports case sensitivity and Unicode normalization flags.
 * - It has a constructor that allows you to initialize it with an array of strings.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Collections
	{
	public class StringSet : System.Collections.Generic.Dictionary<string, bool>, System.Collections.IEnumerable
		{
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: StringSet
		 * Creates an empty set.
		 */
		public StringSet (bool ignoreCase, bool normalizeUnicode) : base()
			{
			this.ignoreCase = ignoreCase;
			this.normalizeUnicode = normalizeUnicode;
			}
			
		
		/* Function: StringSet
		 * Creates a set with the passed strings as members.
		 */
		public StringSet (bool ignoreCase, bool normalizeUnicode, params string[] members)
			{
			this.ignoreCase = ignoreCase;
			this.normalizeUnicode = normalizeUnicode;
			
			foreach (string member in members)
				{  Add(member);  }
			}
			
			
		/* Function: Add
		 * Adds a new string to the set.  Nothing happens if the string is already in it.
		 */
		public void Add (string key)
			{
			// We do this so it doesn't throw an exception if the value already exists.
			this[ key.NormalizeKey(ignoreCase, normalizeUnicode) ] = true;
			}
			
			
		/* Function: Remove
		 * Removes the string from the set.
		 */
		new public bool Remove (string key)
			{
			return base.Remove( key.NormalizeKey(ignoreCase, normalizeUnicode) );
			}
		
			
		/* Function: Contains
		 * Returns whether the string exists in the set.  Always returns false for null.
		 */
		public bool Contains (string key)
			{
			if (key == null)
				{  return false;  }
				
			return ContainsKey( key.NormalizeKey(ignoreCase, normalizeUnicode) );
			}


		/* Function: RemoveOne
		 * Removes and returns an arbitrary string from the set.  If the set is empty it will return null.
		 */
		public string RemoveOne ()
			{
			var enumerator = GetEnumerator();

			if (enumerator.MoveNext() == false)
				{  return null;  }
			else
				{
				string result = (string)enumerator.Current;
				Remove(result);
				return result;
				}
			}


		/* Function: operator ==
		 */
		public static bool operator== (StringSet set1, StringSet set2)
			{
			if ((object)set1 == null && (object)set2 == null)
				{  return true;  }
			else if ((object)set1 == null || (object)set2 == null)
				{  return false;  }
			else if (set1.Count != set2.Count)
				{  return false;  }
			else
				{
				foreach (string item in set1)
					{
					if (set2.Contains(item) == false)
						{  return false;  }
					}
				
				return true;
				}
			}
			
		
		/* Function: operator !=
		 */
		public static bool operator!= (StringSet set1, StringSet set2)
			{
			return !(set1 == set2);
			}
			
		
		/* Function: Equals
		 */
		public override bool Equals (object other)
			{
			if (other == null || !(other is StringSet))
				{  return false;  }
			else
				{  return (this == (StringSet)other);  }
			}
			
			
		/* Function: GetHashCode
		 */
		public override int GetHashCode ()
			{
			return base.GetHashCode();
			}
			
			
		/* Function: GetEnumerator
		 * Returns an enumerator.  This is just an interface function which allows the class to be used with foreach statements.
		 */
		new public System.Collections.IEnumerator GetEnumerator()
			{
			return new StringSetEnumerator(this);
			}



		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: IsEmpty
		 * Whether there are any strings in the set.
		 */
		public bool IsEmpty
			{
			get
				{  return (Count == 0);  }
			}

			

		// Group: Variables
		// __________________________________________________________________________

			
		/* var: ignoreCase
		 * Whether the set is case sensitive.
		 */
		protected bool ignoreCase;
		
		/* var: normalizeUnicode
		 * Whether the set uses Unicode normalization.
		 */
		protected bool normalizeUnicode;
			
		}
		
		
		
	/* ___________________________________________________________________________
	 * 
	 * Class: GregValure.NaturalDocs.Engine.Collections.StringSetEnumerator
	 * ___________________________________________________________________________
	 * 
	 * A class to allow <StringSets> to be used with the foreach statement.
	 */
	class StringSetEnumerator : System.Collections.IEnumerator
		{
		public StringSetEnumerator (StringSet set)
			{
			enumerator = ((Dictionary<string, bool>)set).GetEnumerator();
			}
			
		public object Current
			{
			get
				{  return enumerator.Current.Key;  }
			}
			
		public bool MoveNext()
			{  return enumerator.MoveNext();  }
			
		public void Reset()
			{  throw new System.NotSupportedException();  }
			
		protected Dictionary<string, bool>.Enumerator enumerator;
		}
	}