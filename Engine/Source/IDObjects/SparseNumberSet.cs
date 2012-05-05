/* 
 * Class: GregValure.NaturalDocs.Engine.IDObjects.SparseNumberSet
 * ____________________________________________________________________________
 * 
 * A class similar to <NumberSet> but for storing ID numbers that are very unlikely to be consecutive.  As such they're 
 * stored individually instead of as ranges, which lessens the memory requirements.
 * 
 * Only a small subset of the functionality of <NumberSet> is reproduced here as this is only needed for specialized 
 * situations.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.IDObjects
	{
	public class SparseNumberSet : IEnumerable<int>
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: SparseNumberSet
		 * Creates an empty number set.
		 */
		public SparseNumberSet ()
			{
			ids = new int[1];
			usedIDs = 0;
			}
			

		/* Function: Add
		 * Adds the specified number to the set.  Returns true if the number didn't already exist in the set and was 
		 * added, false if it was already in the set.
		 */
		public bool Add (int number)
			{
			if (number < 1)
				{  throw new ArgumentException("Can't add zero or negative numbers to an ID number set.");  }

			int index = BinarySearch(number);

			if (index >= 0)
				{  return false;  }
			else
				{
				InsertAt(~index, number);
				return true;
				}
			}
			
			
		/* Function: Remove
		 * Removes the specified number from the set.  Returns true if the number existed in the set and was removed, false if
		 * it wasn't part of the set.
		 */
		public bool Remove (int number)
			{
			int index = BinarySearch(number);

			if (index >= 0)
				{
				RemoveAt(index);
				return true;
				}
			else
				{  return false;  }
			}
			
			
		/* Function: Contains
		 * Returns whether the set contains the passed number.
		 */
		public bool Contains (int number)
			{
			return (BinarySearch(number) >= 0);
			}
			
			
		/* Function: Clear
		 * Removes all entries from the set, making it empty.
		 */
		public void Clear ()
			{
			usedIDs = 0;
			
			int shouldShrinkTo = ShouldShrinkTo(ids.Length, 0);

			if (shouldShrinkTo < ids.Length)
				{  ids = new int[shouldShrinkTo];  }
			}


		/* Function: ToString
		 * Returns the set as a string.
		 */
		public override string ToString ()
			{
			System.Text.StringBuilder output = new System.Text.StringBuilder();
			
			output.Append('{');
			
			for (int i = 0; i < usedIDs; i++)
				{
				if (i > 0)
					{  output.Append(',');  }
					
				output.Append(ids[i]);
				}
				
			output.Append('}');
			
			return output.ToString();
			}
			
			
		/* Function: GetEnumerator
		 * Returns an enumerator that returns each value.  This allows the number set to be used with foreach.
		 */
		IEnumerator<int> IEnumerable<int>.GetEnumerator ()
			{
			foreach (int id in ids)
				{  yield return id;  }
			}


		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
			{
			return ids.GetEnumerator();
			}
			

			
		// Group: Properties
		// __________________________________________________________________________
			
			
		/* Property: IsEmpty
		 * Whether the set is empty.
		 */
		public bool IsEmpty
			{
			get
				{  return (usedIDs == 0);  }
			}
			
			
		/* Property: Count
		 * How many discrete numbers are in the set.
		 */
		public int Count
			{
			get
				{  return usedIDs;  }
			}


			
		// Group: Protected Functions
		// __________________________________________________________________________
		
		
		/* Function: BinarySearch
		 * Returns the index of the number in <ids> if it exists, which will always be zero or positive.  If the number doesn't
		 * exist, returns the bitwise complement of the position it should be inserted into, which will always be negative.
		 */
		protected int BinarySearch (int number)
			{
			if (usedIDs == 0)
				{  return ~0;  }
			
			int firstIndex = 0;
			int lastIndex = usedIDs - 1;  // lastIndex is inclusive.
			
			for (;;)
				{
				int testIndex = (firstIndex + lastIndex) / 2;
				
				if (number < ids[testIndex])
					{
					if (testIndex == firstIndex)
						{  return ~testIndex;  }
					else
						{  lastIndex = testIndex - 1;  }
					}
					
				else if (number > ids[testIndex])
					{
					if (testIndex == lastIndex)
						{  return ~(lastIndex + 1);  }
					else
						{  firstIndex = testIndex + 1;  }
					}
					
				else // equal
					{
					return testIndex;
					}
				}
			}
			
			
		/* Function: InsertAt
		 * Inserts a number in <ids> at the specified index.  If necessary, it will reallocate the array.
		 */
		protected void InsertAt (int index, int newID)
			{
			if (usedIDs == ids.Length)
				{
				int newLength;

				if (usedIDs == 1)
					{  newLength = 4;  }
				else
					{  newLength = usedIDs * 2;  }

				int[] newArray = new int[newLength];

				if (index > 0)
					{  Array.Copy( ids, 0, newArray, 0, index );  }
				if (index < ids.Length)
					{  Array.Copy( ids, index, newArray, index + 1, usedIDs - index);  }
					
				ids = newArray;
				usedIDs++;
				}
				
			else  // we don't have to reallocate the array
				{
				// This is safe to use with overlapping regions of the same array.
				if (index < usedIDs)
					{  Array.Copy( ids, index, ids, index + 1, usedIDs - index);  }
					
				usedIDs++;
				}

			ids[index] = newID;
			}
			
			
		/* Function: RemoveAt
		 * Removes an integer from <ids> at the specified index and moves everything else down.
		 */
		protected void RemoveAt (int index)
			{
			int shouldShrinkTo = ShouldShrinkTo(ids.Length, usedIDs - 1);

			if (shouldShrinkTo < ids.Length)
				{
				int[] newArray = new int[shouldShrinkTo];
				
				if (index > 0)
					{  Array.Copy( ids, newArray, index);  }
				if (index + 1 < usedIDs)
					{  Array.Copy( ids, index + 1, newArray, index, usedIDs - index - 1 );  }
					
				ids = newArray;
				}
				
			// Otherwise just move everything down.  This is safe to use with overlapping regions of the same array.
			else if (index != usedIDs - 1)
				{  Array.Copy( ids, index + 1, ids, index, usedIDs - index - 1);  }
				
			usedIDs--;
			}


		/* Function: ShouldShrinkTo
		 * If an array should be replaced with a smaller one given the passed data and array sizes, returns the new array size that 
		 * should be used.  If the array shouldn't be reallocated this will return the existing length.
		 */
		protected static int ShouldShrinkTo (int arrayLength, int dataLength)
			{
			// We're much more conservative about shrinking than growing because we'll actually end up using more memory until the 
			// next garbage collection, so the savings have to be significant.
			if (arrayLength > 8 && dataLength <= arrayLength / 8)
				{
				if (dataLength < 4)
					{  return 4;  }
				else
					{  return dataLength;  }
				}
			else
				{  return arrayLength;  }
			}



		// Group: Variables
		// __________________________________________________________________________
			
			
		/* array: ids
		 * A list of integers representing used numbers.
		 */
		protected int[] ids;

		/* var: usedIDs
		 * The length of the *used* array in <ids> since the array may be larger than the content.
		 */
		protected int usedIDs;

		}
	}