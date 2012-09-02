/* 
 * Class: GregValure.NaturalDocs.Engine.Collections.StringSet
 * ____________________________________________________________________________
 * 
 * A general lookup table for tracking the existence of strings in a set.  This is preferable to a HashSet class 
 * because
 * 
 * - It supports case sensitivity and Unicode normalization flags.
 * - It has a constructor that allows you to initialize it with an array of strings.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Collections
	{
	public class StringSet : System.Collections.Generic.HashSet<string>
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
		new public void Add (string key)
			{
			base.Add( key.NormalizeKey(ignoreCase, normalizeUnicode) );
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
		new public bool Contains (string key)
			{
			if (key == null)
				{  return false;  }
				
			return base.Contains( key.NormalizeKey(ignoreCase, normalizeUnicode) );
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
			   string result = enumerator.Current;
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
			else
				{  return set1.SetEquals(set2);  }
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
			
			
		/* Function: ToBinaryFile
		 * Writes the contents of the string set to the passed binary file.
		 */
		public void ToBinaryFile (BinaryFile binaryFile)
			{
			// [String: member]
			// [String: member]
			// ...
			// [String: null]

			foreach (string member in this)
			   {  binaryFile.WriteString(member);  }

			binaryFile.WriteString(null);
			}


		/* Function: FromBinaryFile
		 * Reads the contents of the string set from the passed binary file.
		 */
		static public StringSet FromBinaryFile (BinaryFile binaryFile, bool ignoreCase, bool normalizeUnicode)
			{
			StringSet stringSet = new StringSet(ignoreCase, normalizeUnicode);

			// [String: member]
			// [String: member]
			// ...
			// [String: null]

			for (string member = binaryFile.ReadString(); member != null; member = binaryFile.ReadString())
			   {  stringSet.Add(member);  }

			return stringSet;
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
	 * Class: GregValure.NaturalDocs.Engine.Collections.StringSet_BinaryFileExtensions
	 * ___________________________________________________________________________
	 * 
	 */
	public static class StringSet_BinaryFileExtensions
		{
		/* Function: ReadStringSet
		 * An extension method to <BinaryFile> which reads a string set from it.  Call with 
		 * "stringSet = binaryFile.ReadStringSet(ignoreCase, normalizeUnicode);"
		 */
		static public StringSet ReadStringSet (this BinaryFile binaryFile, bool ignoreCase, bool normalizeUnicode)
			{
			return StringSet.FromBinaryFile(binaryFile, ignoreCase, normalizeUnicode);
			}

		/* Function: WriteStringSet
		 * An extension method to <BinaryFile> which writes the string set to it.  Call with "binaryFile.WriteStringSet(stringSet);"
		 */
		static public void WriteStringSet (this BinaryFile binaryFile, StringSet stringSet)
			{
			stringSet.ToBinaryFile(binaryFile);
			}
		}

	}