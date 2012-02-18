/* 
 * Class: GregValure.NaturalDocs.Engine.IDObjects.NumberSet
 * ____________________________________________________________________________
 * 
 * A class for efficiently storing a large list of ID numbers and determining which ones are still available.  Also focuses 
 * on reusing deleted ID numbers rather than continuing on in autoincrement fashion.  
 * 
 * IDs start at one.  Zero and negative numbers are not allowed.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.IDObjects
	{
	public class NumberSet : IEnumerable<int>, IBinaryFileObject
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: NumberSet
		 * Creates an empty number set.
		 */
		public NumberSet ()
			{
			ranges = new NumberRange[1];
			usedRanges = 0;
			}
			
		/* Constructor: NumberSet
		 * Creates a number set from the passed string.  It is safe to use with null or the empty string.
		 */
		public NumberSet (string input)
			{
			ranges = null;
			usedRanges = 0;
			
			FromString(input);
			}
						
		/* Constructor: NumberSet
		 * Reads a number set from the passed <BinaryFile>.
		 */
		public NumberSet (BinaryFile input)
			{
			ranges = null;
			usedRanges = 0;
			
			FromBinaryFile(input);
			}

		/* Constructor: NumberSet
		 * Creates a number set by duplicating the passed one.
		 */
		public NumberSet (NumberSet toCopy)
			{
			ranges = null;
			usedRanges = 0;

			Duplicate(toCopy);
			}
			
			
		/* Function: Add
		 * Adds the specified number to the set.  Returns true if the number didn't already exist in the set and was 
		 * added, false if it was already in the set.
		 */
		public bool Add (int number)
			{
			if (number < 1)
				{  throw new ArgumentException("Can't add zero or negative numbers to an ID number set.");  }
			
			int index = FindRangeIndex(number);
			
			// If the index is in the existing array, meaning the number is in the indexed range or should be inserted right
			// before it...
			if (index < usedRanges)
				{
				
				// If the number is already in the indexed range...
				if (number >= ranges[index].Low && number <= ranges[index].High)
					{  return false;  }
					
				// If the number is one lower than the lower bounds of the indexed range...
				else if (number == ranges[index].Low - 1)
					{
					ranges[index].Low--;
					
					// If it's not the first range and now the lower bounds is only one higher than the prior range's upper
					// bounds...
					if (index > 0 && ranges[index - 1].High == ranges[index].Low - 1)
						{  
						ranges[index - 1].High = ranges[index].High;
						DeleteAtIndex(index);
						}

					return true;
					}
					
				// If it's not the first range and the number is one higher than the upper bounds of the previous range...
				else if (index > 0 && number == ranges[index - 1].High + 1)
					{
					ranges[index - 1].High++;
					// We don't have to check if we need to combine the prior range with the indexed because we already
					// checked if it's one lower than the indexed's lower bounds.
					return true;
					}
					
				// If it's not one off from the indexed or prior range...
				else
					{
					InsertAtIndex(index);
					ranges[index].Low = number;
					ranges[index].High = number;
					return true;
					}

				}
			
			// If the pair is outside the existing array...
			else
				{
				
				// If the there is at least one pair in the array and the number is one higher than it's upper bounds...
				if (index > 0 && number == ranges[index - 1].High + 1)
					{
					ranges[index - 1].High++;
					return true;
					}
					
				// If the array is empty or it's more than one past the prior's upper bounds...
				else
					{
					InsertAtIndex(index);
					ranges[index].Low = number;
					ranges[index].High = number;
					return true;
					}
					
				}
			}
			
			
		/* Function: Remove
		 * Removes the specified number from the set.  Returns true if the number existed in the set and was removed, false if
		 * it wasn't part of the set.
		 */
		public bool Remove (int number)
			{
			int index = FindRangeIndex(number);
			
			// If the number is inside the existing array...
			if (index < usedRanges)
				{
				
				// If the number is the lower bounds of the range, which also captures ranges for a single number...
				if (number == ranges[index].Low)
					{
					// If the range is for a single number...
					if (number == ranges[index].High)
						{  DeleteAtIndex(index);  }
						
					else
						{  ranges[index].Low++;  }
						
					return true;
					}
					
				// If the number is the upper bounds of the pair and the pair isn't for a single number...
				else if (number == ranges[index].High)
					{
					ranges[index].High--;
					return true;
					}
					
				// If the number is in the middle of the range somewhere...
				else if (number > ranges[index].Low)
					{
					InsertAtIndex(index + 1);
					ranges[index + 1].High = ranges[index].High;
					ranges[index + 1].Low = number + 1;
					ranges[index].High = number - 1;
					
					return true;
					}
					
				// Otherwise the number is lower than the lower bounds of the range, meaning it's past the beginning of the
				// set or between ranges.  It's not present in the set so ignore it.
				else
					{  return false;  }

				}
				
			// The number is higher than the highest in the set.
			else
				{  return false;  }
			}
			
			
		/* Function: Remove
		 * Removes the contents of an entire set from this one.
		 */
		public void Remove (NumberSet setToRemove)
			{
			int position = 0;
			int setToRemovePosition = 0;
			
			while (position < usedRanges && setToRemovePosition < setToRemove.usedRanges)
				{
				
				// If the lower bounds is less than the removal lower bounds...
				if (ranges[position].Low < setToRemove.ranges[setToRemovePosition].Low)
					{
					
					// If the upper bounds is also less than the removal lower bounds, advance the position.
					if (ranges[position].High < setToRemove.ranges[setToRemovePosition].Low)
						{  position++;  }
						
					// The upper bounds is somewhere in or past the removal range.  If it is less than or equal to the removal
					// upper bounds, we can just truncate this range.
					else if (ranges[position].High <= setToRemove.ranges[setToRemovePosition].High)
						{
						ranges[position].High = setToRemove.ranges[setToRemovePosition].Low - 1;
						position++;
						}
						
					// The upper bounds is past the removal range.  Split it.
					else
						{
						InsertAtIndex(position + 1);
						ranges[position + 1].High = ranges[position].High;
						ranges[position + 1].Low = setToRemove.ranges[setToRemovePosition].High + 1;
						ranges[position].High = setToRemove.ranges[setToRemovePosition].Low - 1;
						
						position++;
						setToRemovePosition++;
						}
					}
					
				// If the lower bounds is equal to the removal lower bounds...
				else if (ranges[position].Low == setToRemove.ranges[setToRemovePosition].Low)
					{
					
					// If the upper bounds is less than or equal to the removal upper bounds, remove the range entirely.
					if (ranges[position].High <= setToRemove.ranges[setToRemovePosition].High)
						{  DeleteAtIndex(position);  }
						
					// The upper bounds is greater than the removal upper bounds, truncate the range.
					else
						{
						ranges[position].Low = setToRemove.ranges[setToRemovePosition].High + 1;
						setToRemovePosition++;
						}
					
					}
					
				// If the lower bounds is greater than the removal lower bounds...
				else
					{
					
					// If the lower bounds is also greater than the removal upper bounds, advance the removal.
					if (ranges[position].Low > setToRemove.ranges[setToRemovePosition].High)
						{  setToRemovePosition++;  }
						
					// The removal upper bounds is in or past the range.  If it's greater than or equal to the upper bounds,
					// remove the range.
					else if (ranges[position].High <= setToRemove.ranges[setToRemovePosition].High)
						{  DeleteAtIndex(position);  }
						
					// Since it's less than the upper bounds, truncate the range.
					else
						{
						ranges[position].Low = setToRemove.ranges[setToRemovePosition].High + 1;
						setToRemovePosition++;
						}
						
					}
					
				}
			}
			
			
		/* Function: Contains
		 * Returns whether the set contains the passed number.
		 */
		public bool Contains (int number)
			{
			int index = FindRangeIndex(number);
			
			if (index >= usedRanges)
				{  return false;  }
			else if (number >= ranges[index].Low && number <= ranges[index].High)
				{  return true;  }
			else
				{  return false;  }
			}
			
			
		/* Function: Clear
		 * Removes all entries from the set, making it empty.
		 */
		public void Clear ()
			{
			usedRanges = 0;
			
			int shouldShrinkTo = ShouldShrinkTo(ranges.Length, 0);

			if (shouldShrinkTo < ranges.Length)
				{  ranges = new NumberRange[shouldShrinkTo];  }
			}


		/* Function: Duplicate
		 * Makes this number set have the same contents as the passed one.
		 */
		public void Duplicate (NumberSet other)
			{
			// DEPENDENCY: This has to be able to be called from the constructor when numberPairs is null.

			if (ranges == null || ranges.Length < other.usedRanges || 
				 ShouldShrinkTo(ranges.Length, other.usedRanges) < ranges.Length)
				{
				ranges = new NumberRange[other.usedRanges];
				}

			usedRanges = other.usedRanges;
			Array.Copy(other.ranges, ranges, usedRanges);
			}
			
			
		/* Operator: operator==
		 */
		static public bool operator== (NumberSet setA, NumberSet setB)
			{
			if ((object)setA == null && (object)setB == null)
				{  return true;  }
			else if ((object)setA == null || (object)setB == null)
				{  return false;  }
			else if (setA.usedRanges != setB.usedRanges)
				{  return false;  }
			else
				{
				for (int i = 0; i < setA.usedRanges; i++)
					{
					if (setA.ranges[i].Low != setB.ranges[i].Low ||
						 setA.ranges[i].High != setB.ranges[i].High)
						{  return false;  }
					}
					
				return true;
				}
			}
			
		/* Operator: operator!=
		 */
		static public bool operator!= (NumberSet setA, NumberSet setB)
			{
			return !(setA == setB);
			}
		
		override public bool Equals (object o)
			{
			if (o == null || (o is NumberSet) == false)
				{  return false;  }
			else
				{  return (this == (NumberSet)o);  }
			}

		public override int GetHashCode ()
			{
			return ToString().GetHashCode();
			}
			
			
			
		/* Function: ToString
		 * Returns the set as a string.
		 */
		public override string ToString ()
			{
			System.Text.StringBuilder output = new System.Text.StringBuilder();
			
			output.Append('{');
			
			for (int i = 0; i < usedRanges; i++)
				{
				if (i > 0)
					{  output.Append(',');  }
					
				output.Append(ranges[i].Low);
				
				if (ranges[i].High != ranges[i].Low)
					{
					output.Append('-');
					output.Append(ranges[i].High);
					}
				}
				
			output.Append('}');
			
			return output.ToString();
			}
			
			
		/* Function: FromString
		 * Fills the set using a string.  Throws an exception if it's not in the correct format.  The format isn't 
		 * documented because it should only be used with strings generated by <ToString()>.  This is safe to
		 * use with a null or empty string.
		 */
		public void FromString (string input)
			{
			// DEPENDENCY: This has to be able to be called from the constructor when numberPairs is null.
			
			if (string.IsNullOrEmpty(input) || input == EmptySetString)
				{
				ranges = new NumberRange[1];
				usedRanges = 0;
				return;
				}
				
			if (input[0] != '{')
				{  throw new Exceptions.StringNotInValidFormat(input, this);  }


			// First parse the string to perform basic validation and determine the array size.

			int arraySize = 1;
			int number;
			int inputIndex = 1;
			bool secondNumber = false;
						
			for (;;)
				{
				if (inputIndex >= input.Length || input[inputIndex] < '0' || input[inputIndex] > '9')
					{  throw new Exceptions.StringNotInValidFormat(input, this);  }
					
				number = (int)(input[inputIndex] - '0');
				inputIndex++;
				
				while (inputIndex < input.Length && input[inputIndex] >= '0' && input[inputIndex] <= '9')
					{
					number *= 10;
					number += (int)(input[inputIndex] - '0');
					inputIndex++;
					}
					
				if (inputIndex >= input.Length)
					{  throw new Exceptions.StringNotInValidFormat(input, this);  }
					
				if (input[inputIndex] == '}')
					{  break;  }
				else if (input[inputIndex] == ',')
					{  
					arraySize++;  
					secondNumber = false;
					inputIndex++;
					}
				else if (input[inputIndex] == '-')
					{  
					if (secondNumber == false)
						{  
						secondNumber = true;  
						inputIndex++;
						}
					else
						{  throw new Exceptions.StringNotInValidFormat(input, this);  }
					}
				else
					{  throw new Exceptions.StringNotInValidFormat(input, this);  }
				}
				
				
			// If we're here the string is valid enough to parse, though it still may contain errors like "3-5,6-9" which should be "3-9".
			
			if (ranges == null || ranges.Length < arraySize || ShouldShrinkTo(ranges.Length, arraySize) < ranges.Length)
				{  ranges = new NumberRange[arraySize];  }
				
			usedRanges = arraySize;
			
			inputIndex = 1;
			int rangeIndex = 0;
			secondNumber = false;
			
			for (;;)
				{
				number = (int)(input[inputIndex] - '0');
				inputIndex++;
				
				while (input[inputIndex] >= '0' && input[inputIndex] <= '9')
					{
					number *= 10;
					number += (int)(input[inputIndex] - '0');
					inputIndex++;
					}
				
				if (secondNumber)
					{  ranges[rangeIndex].High = number;  }
				else
					{  ranges[rangeIndex].Low = number;  }
				
				if (input[inputIndex] == '}')
					{  
					if (secondNumber == false)
						{  ranges[rangeIndex].High = number;  }
						
					break;
					}
				else if (input[inputIndex] == ',')
					{  
					if (secondNumber == false)
						{  ranges[rangeIndex].High = number;  }

					rangeIndex++;  
					secondNumber = false;
					inputIndex++;
					}
				else // (input[inputIndex] == '-')
					{  
					inputIndex++;
					secondNumber = true;
					}
				}

			// Catch any remaining errors like "3-5,6-9".
			if (!Validate())
				{  
				usedRanges = 0;
				throw new Exceptions.StringNotInValidFormat(input, this);  
				}
			}


		/* Function: ToBinaryFile
		 * Writes the number set to the passed <BinaryFile>.
		 */
		public void ToBinaryFile (BinaryFile binaryFile)
			{
			// [int32: ranges]
			// [int32: low] [int32: high]
			// [int32: low] [int32: high]
			// ...

			binaryFile.WriteInt32(usedRanges);

			for (int i = 0; i < usedRanges; i++)
				{  
				binaryFile.WriteInt32(ranges[i].Low);
				binaryFile.WriteInt32(ranges[i].High);
				}
			}


		/* Function: FromBinaryFile
		 * Replaces the current number set with one from the passed <BinaryFile>.
		 */
		public void FromBinaryFile (BinaryFile binaryFile)
			{
			// DEPENDENCY: This has to be able to be called from the constructor when numberPairs is null.

			// [int32: ranges]
			// [int32: low] [int32: high]
			// [int32: low] [int32: high]
			// ...

			int length = binaryFile.ReadInt32();

			if (length < 0)
				{  throw new FormatException();  }

			if (ranges == null || ranges.Length < length || ShouldShrinkTo(ranges.Length, length) < ranges.Length)
				{  
				if (length == 0)
					{  ranges = new NumberRange[1];  }
				else
					{  ranges = new NumberRange[length];  }
				}

			usedRanges = length;

			for (int i = 0; i < length; i++)
				{  
				ranges[i].Low = binaryFile.ReadInt32();  
				ranges[i].High = binaryFile.ReadInt32();
				}

			if (!Validate())
				{  
				usedRanges = 0;
				throw new FormatException();  
				}
			}


		/* Function: Validate
		 * Checks whether <ranges> is in the proper format to be used.
		 */
		protected bool Validate()
			{
			if (usedRanges < 0)
				{  return false;  }

			for (int i = 0; i < usedRanges; i++)
				{
				if (ranges[i].Low <= 0 || ranges[i].High < ranges[i].Low)
					{  return false;  }
				if (i > 0 && ranges[i].Low <= ranges[i-1].High + 1)
					{  return false;  }
				}

			return true;
			}
			

		/* Function: GetEnumerator
		 * Returns an enumerator that returns each value.  This allows the number set to be used with foreach.
		 */
		IEnumerator<int> IEnumerable<int>.GetEnumerator ()
			{
			for (int rangeIndex = 0; rangeIndex < usedRanges; rangeIndex++)
				{
				NumberRange range = ranges[rangeIndex];

				for (int number = range.Low; number <= range.High; number++)
					{  yield return number;  }
				}
			}


		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
			{
			return ((System.Collections.Generic.IEnumerable<int>)this).GetEnumerator();
			}
			

			
		// Group: Properties
		// __________________________________________________________________________
			
			
		/* Property: IsEmpty
		 * Whether the set is empty.
		 */
		public bool IsEmpty
			{
			get
				{  return (usedRanges == 0);  }
			}
			
			
		/* Property: LowestAvailable
		 * The lowest unused number available, starting at one.
		 */
		public int LowestAvailable
			{
			get
				{
				if (usedRanges == 0)
					{  return 1;  }
				else if (ranges[0].Low > 1)
					{  return 1;  }
				else
					{  return ranges[0].High + 1;  }
				}
			}
			
			
		/* Property: Highest
		 * The highest number in the set or zero if the set is empty.
		 */
		public int Highest
			{
			get
				{
				if (usedRanges == 0)
					{  return 0;  }
				else
					{  return ranges[usedRanges - 1].High;  }
				}
			}
			
			
		/* Property: Count
		 * How many discrete numbers are in the set.
		 */
		public int Count
			{
			get
				{
				int count = 0;
				int index = 0;
				
				while (index < usedRanges)
					{
					count += (ranges[index].High - ranges[index].Low + 1);
					index++;
					}
					
				return count;
				}
			}


		/* Property: Ranges
		 * 
		 * Returns an enumerator that returns each <NumberRange> in the set.  This property is usable with foreach.
		 * 
		 * > foreach (NumberRange range in numberSet.Ranges)
		 * >    { ... }
		 */
		public IEnumerable<NumberRange> Ranges
			{
			get
				{
				for (int i = 0; i < usedRanges; i++)
					{  yield return ranges[i];  }
				}
			}
			
		
			
			
		// Group: Protected Functions
		// __________________________________________________________________________
		
		
		/* Function: FindRangeIndex
		 * Finds the <NumberRange> that would hold the passed number and returns its index into the array.  If 
		 * the number is not in the array, it returns the index of the range above it (the insertion point if a new 
		 * range were to be created.)  If it's lower than any range, it returns zero.  If it's higher than any range, 
		 * it returns the index past the last range so you must check the result against <usedRanges>.
		 */
		protected int FindRangeIndex (int number)
			{
			if (usedRanges == 0)
				{  return 0;  }
			
			int firstIndex = 0;
			int lastIndex = usedRanges - 1;  // lastRangeIndex is inclusive.
			
			for (;;)
				{
				int testIndex = (firstIndex + lastIndex) / 2;
				
				if (number < ranges[testIndex].Low)
					{
					if (testIndex == firstIndex)
						{  return testIndex;  }
					else
						{  lastIndex = testIndex - 1;  }
					}
					
				else if (number > ranges[testIndex].High)
					{
					if (testIndex == lastIndex)
						{  return lastIndex + 1;  }
					else
						{  firstIndex = testIndex + 1;  }
					}
					
				else // number is in the range
					{
					return testIndex;
					}
				}
			}
			
			
		/* Function: InsertAtIndex
		 * Creates a space in <ranges> for a new <NumberRange> at the specified index.  If necessary, will reallocate
		 * the array.  The values in the new space are undefined.
		 */
		protected void InsertAtIndex (int index)
			{
			if (usedRanges == ranges.Length)
				{
				int newLength;

				if (usedRanges == 1)
					{  newLength = 4;  }
				else
					{  newLength = usedRanges * 2;  }

				NumberRange[] newArray = new NumberRange[newLength];

				if (index > 0)
					{  Array.Copy( ranges, 0, newArray, 0, index );  }
				if (index < usedRanges)
					{  Array.Copy( ranges, index, newArray, index + 1, usedRanges - index);  }
					
				ranges = newArray;
				usedRanges++;
				}
				
			else  // we don't have to reallocate the array
				{
				// This is safe to use with overlapping regions of the same array.
				if (index < usedRanges)
					{  Array.Copy( ranges, index, ranges, index + 1, usedRanges - index);  }
					
				usedRanges++;
				}
			}
			
			
		/* Function: DeleteAtIndex
		 * Deletes a <NumberRange> from <ranges> at the specified index and moves everything else down.
		 */
		protected void DeleteAtIndex (int index)
			{
			int shouldShrinkTo = ShouldShrinkTo(ranges.Length, usedRanges - 1);

			if (shouldShrinkTo < ranges.Length)
				{
				NumberRange[] newArray = new NumberRange[shouldShrinkTo];
				
				if (index > 0)
					{  Array.Copy( ranges, newArray, index);  }
				if (index + 1 < usedRanges)
					{  Array.Copy( ranges, index + 1, newArray, index, usedRanges - index - 1 );  }
					
				ranges = newArray;
				}
				
			// Otherwise just move everything down.  This is safe to use with overlapping regions of the same array.
			else if (index != usedRanges - 1)
				{  Array.Copy( ranges, index + 1, ranges, index, usedRanges - index - 1);  }
				
			usedRanges--;
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
			
			
			
		// Group: Constants
		// __________________________________________________________________________
		
		
		/* Constant: EmptySetString
		 * The string that is generated by <ToString()> for an empty set.  Will not be null.
		 */
		public const string EmptySetString = "{}";
		
			
			
		// Group: Variables
		// __________________________________________________________________________
			
			
		/* array: ranges
		 * 
		 * An array of <NumberRanges> representing used numbers.  The bounds are inclusive.  Single digits are stored as
		 * a range with the high and low bounds being the same.
		 * 
		 * For example, the numbers 1, 2, 3, 4, 8, 11, 12 would be stored as [1,4],[8,8],[11,12] representing 1-4,8,11-12.
		 */
		protected internal NumberRange[] ranges;
		
		
		/* var: usedRanges
		 * The length of the *used* array in <ranges> since the array may be larger than the content.
		 */
		protected internal int usedRanges;

		}
	}