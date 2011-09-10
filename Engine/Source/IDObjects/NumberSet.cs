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

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.IDObjects
	{
	public class NumberSet : System.Collections.Generic.IEnumerable<int>, IBinaryFileObject
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: NumberSet
		 * Creates an empty number set.
		 */
		public NumberSet ()
			{
			numberPairs = new int[2];
			numberPairsUsedLength = 0;
			}
			
		/* Constructor: NumberSet
		 * Creates a number set from the passed string.  It is safe to use with null or the empty string.
		 */
		public NumberSet (string input)
			{
			numberPairs = null;
			numberPairsUsedLength = 0;
			
			FromString(input);
			}
						
		/* Constructor: NumberSet
		 * Reads a number set from the passed <BinaryFile>.
		 */
		public NumberSet (BinaryFile input)
			{
			numberPairs = null;
			numberPairsUsedLength = 0;
			
			FromBinaryFile(input);
			}

		/* Constructor: NumberSet
		 * Creates a number set by duplicating the passed one.
		 */
		public NumberSet (NumberSet toCopy)
			{
			numberPairs = null;
			numberPairsUsedLength = 0;

			Duplicate(toCopy);
			}
			
			
		/* Function: Add
		 * Adds the specified number to the set.  Returns true if the number didn't already exist in the set and was added, false
		 * if it was already in the set.
		 */
		public bool Add (int number)
			{
			if (number < 1)
				{  throw new ArgumentException("Can't add zero or negative numbers to an ID number set.");  }
			
			int pairIndex = FindPairIndex(number);
			
			// If the pair index is in the existing array, meaning the number is in the indexed pair or should be
			// inserted right before it...
			if (pairIndex < numberPairsUsedLength)
				{
				
				// If the number is already in the indexed pair...
				if (number >= numberPairs[pairIndex] && number <= numberPairs[pairIndex + 1])
					{  return false;  }
					
				// If the number is one lower than the lower bounds of the indexed pair...
				else if (number == numberPairs[pairIndex] - 1)
					{
					numberPairs[pairIndex]--;
					
					// If it's not the first pair and now the lower bounds is only one higher than the prior pair's upper
					// bounds...
					if (pairIndex > 0 && numberPairs[pairIndex - 1] == numberPairs[pairIndex] - 1)
						{  
						numberPairs[pairIndex - 1] = numberPairs[pairIndex + 1];
						DeleteAtIndex(pairIndex);
						}

					return true;
					}
					
				// If it's not the first pair and the number is one higher than the upper bounds of the previous pair...
				else if (pairIndex > 0 && number == numberPairs[pairIndex - 1] + 1)
					{
					numberPairs[pairIndex - 1]++;
					// We don't have to check if we need to combine the prior pair with the indexed because we already
					// checked if it's one lower than the indexed's lower bounds.
					return true;
					}
					
				// If it's not one off from the indexed or prior pair...
				else
					{
					InsertAtIndex(pairIndex);
					numberPairs[pairIndex] = number;
					numberPairs[pairIndex+1] = number;
					return true;
					}

				}
			
			// If the pair is outside the existing array...
			else
				{
				
				// If the there is at least one pair in the array and the number is one higher than it's upper bounds...
				if (pairIndex > 0 && number == numberPairs[pairIndex - 1] + 1)
					{
					numberPairs[pairIndex - 1]++;
					return true;
					}
					
				// If the array is empty or it's more than one past the prior's upper bounds...
				else
					{
					InsertAtIndex(pairIndex);
					numberPairs[pairIndex] = number;
					numberPairs[pairIndex + 1] = number;
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
			int pairIndex = FindPairIndex(number);
			
			// If the number is inside the existing array...
			if (pairIndex < numberPairsUsedLength)
				{
				
				// If the number is the lower bounds of the pair, which also captures pairs for a single number...
				if (number == numberPairs[pairIndex])
					{
					// If the pair is for a single number...
					if (number == numberPairs[pairIndex + 1])
						{  DeleteAtIndex(pairIndex);  }
						
					else
						{  numberPairs[pairIndex]++;  }
						
					return true;
					}
					
				// If the number is the upper bounds of the pair and the pair isn't for a single number...
				else if (number == numberPairs[pairIndex + 1])
					{
					numberPairs[pairIndex + 1]--;
					return true;
					}
					
				// If the number is in the middle of the pair somewhere...
				else if (number > numberPairs[pairIndex])
					{
					InsertAtIndex(pairIndex + 2);
					numberPairs[pairIndex + 3] = numberPairs[pairIndex + 1];
					numberPairs[pairIndex + 1] = number - 1;
					numberPairs[pairIndex + 2] = number + 1;
					
					return true;
					}
					
				// Otherwise the number is lower than the lower bounds of the pair, meaning it's past the beginning of the
				// set or between pairs.  It's not present in the set so ignore it.
				else
					{  return false;  }

				}
				
			// The number is higher than the highest in the set.
			else
				{  return false;  }
			}
			
			
		/* Function: Remove
		 * Removes the contents an entire set from this one.
		 */
		public void Remove (NumberSet setToRemove)
			{
			int position = 0;
			int setToRemovePosition = 0;
			
			while (position < numberPairsUsedLength && setToRemovePosition < setToRemove.numberPairsUsedLength)
				{
				
				// If the lower bounds is less than the removal lower bounds...
				if (numberPairs[position] < setToRemove.numberPairs[setToRemovePosition])
					{
					
					// If the upper bounds is also less than the removal lower bounds, advance the position.
					if (numberPairs[position + 1] < setToRemove.numberPairs[setToRemovePosition])
						{  position += 2;  }
						
					// The upper bounds is somewhere in or past the removal pair.  If it is less than or equal to the removal
					// upper bounds, we can just truncate this pair.
					else if (numberPairs[position + 1] <= setToRemove.numberPairs[setToRemovePosition + 1])
						{
						numberPairs[position + 1] = setToRemove.numberPairs[setToRemovePosition] - 1;
						position += 2;
						}
						
					// The upper bounds is past the removal pair.  Split it.
					else
						{
						InsertAtIndex(position + 2);
						numberPairs[position + 3] = numberPairs[position + 1];
						numberPairs[position + 1] = setToRemove.numberPairs[setToRemovePosition] - 1;
						numberPairs[position + 2] = setToRemove.numberPairs[setToRemovePosition + 1] + 1;
						
						position += 2;
						setToRemovePosition += 2;
						}
					}
					
				// If the lower bounds is equal to the removal lower bounds...
				else if (numberPairs[position] == setToRemove.numberPairs[setToRemovePosition])
					{
					
					// If the upper bounds is less than or equal to the removal upper bounds, remove the pair entirely.
					if (numberPairs[position + 1] <= setToRemove.numberPairs[setToRemovePosition + 1])
						{  DeleteAtIndex(position);  }
						
					// The upper bounds is greater than the removal upper bounds, truncate the pair.
					else
						{
						numberPairs[position] = setToRemove.numberPairs[setToRemovePosition + 1] + 1;
						setToRemovePosition += 2;
						}
					
					}
					
				// If the lower bounds is greater than the removal lower bounds...
				else
					{
					
					// If the lower bounds is also greater than the removal upper bounds, advance the removal.
					if (numberPairs[position] > setToRemove.numberPairs[setToRemovePosition + 1])
						{  setToRemovePosition += 2;  }
						
					// The removal upper bounds is in or past the pair.  If it's greater than or equal to the upper bounds,
					// remove the pair.
					else if (numberPairs[position + 1] <= setToRemove.numberPairs[setToRemovePosition + 1])
						{  DeleteAtIndex(position);  }
						
					// Since it's less than the upper bounds, truncate the pair.
					else
						{
						numberPairs[position] = setToRemove.numberPairs[setToRemovePosition + 1] + 1;
						setToRemovePosition += 2;
						}
						
					}
					
				}
			}
			
			
		/* Function: Contains
		 * Returns whether the set contains the passed number.
		 */
		public bool Contains (int number)
			{
			int pairIndex = FindPairIndex(number);
			
			if (pairIndex >= numberPairsUsedLength)
				{  return false;  }
			else if (number >= numberPairs[pairIndex] && number <= numberPairs[pairIndex + 1])
				{  return true;  }
			else
				{  return false;  }
			}
			
			
		/* Function: Clear
		 * Removes all entries from the set, making it empty.
		 */
		public void Clear ()
			{
			numberPairsUsedLength = 0;
			
			if (numberPairs.Length >= NumberPairShrinkThreshold)
				{  numberPairs = new int[2];  }
			}


		/* Function: Duplicate
		 * Makes this number set have the same contents as the passed one.
		 */
		public void Duplicate (NumberSet other)
			{
			// DEPENDENCY: This has to be able to be called from the constructor when numberPairs is null.

			if (numberPairs == null || numberPairs.Length < other.numberPairsUsedLength || 
				 numberPairs.Length - other.numberPairsUsedLength >= NumberPairShrinkThreshold)
				{
				numberPairs = new int[other.numberPairsUsedLength];
				}

			numberPairsUsedLength = other.numberPairsUsedLength;
			Array.Copy(other.numberPairs, numberPairs, numberPairsUsedLength);
			}
			
			
		/* Operator: operator==
		 */
		static public bool operator== (NumberSet setA, NumberSet setB)
			{
			if ((object)setA == null && (object)setB == null)
				{  return true;  }
			else if ((object)setA == null || (object)setB == null)
				{  return false;  }
			else if (setA.numberPairsUsedLength != setB.numberPairsUsedLength)
				{  return false;  }
			else
				{
				for (int i = 0; i < setA.numberPairsUsedLength; i++)
					{
					if (setA.numberPairs[i] != setB.numberPairs[i])
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
			
			for (uint i = 0; i < numberPairsUsedLength; i += 2)
				{
				if (i > 0)
					{  output.Append(',');  }
					
				output.Append(numberPairs[i]);
				
				if (numberPairs[i + 1] != numberPairs[i])
					{
					output.Append('-');
					output.Append(numberPairs[i + 1]);
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
				numberPairs = new int[2];
				numberPairsUsedLength = 0;
				return;
				}
				
			if (input[0] != '{')
				{  throw new Exceptions.StringNotInValidFormat(input, this);  }


			// First parse the string to perform basic validation and determine the array size.

			int arraySize = 2;
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
					arraySize += 2;  
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
			
			if (numberPairs == null || numberPairs.Length < arraySize || numberPairs.Length - arraySize >= NumberPairShrinkThreshold)
				{  numberPairs = new int[arraySize];  }
				
			numberPairsUsedLength = arraySize;
			
			inputIndex = 1;
			int pairIndex = 0;
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
				
				numberPairs[pairIndex] = number;
				
				if (input[inputIndex] == '}')
					{  
					if (secondNumber == false)
						{  
						pairIndex++;
						numberPairs[pairIndex] = number;
						}
						
					break;
					}
				else if (input[inputIndex] == ',')
					{  
					if (secondNumber == false)
						{  
						pairIndex++;
						numberPairs[pairIndex] = number;
						}

					pairIndex++;  
					secondNumber = false;
					inputIndex++;
					}
				else // (input[inputIndex] == '-')
					{  
					pairIndex++;
					inputIndex++;
					secondNumber = true;
					}
				}

			// Catch any remaining errors like "3-5,6-9".
			if (!Validate())
				{  
				numberPairsUsedLength = 0;
				throw new Exceptions.StringNotInValidFormat(input, this);  
				}
			}


		/* Function: ToBinaryFile
		 * Writes the number set to the passed <BinaryFile>.
		 */
		public void ToBinaryFile (BinaryFile binaryFile)
			{
			// [int32: numbers] - Like numberPairsUsedLength, a count of integers, not of pairs.
			// [int32: low] [int32: high]
			// [int32: low] [int32: high]
			// ...

			binaryFile.WriteInt32(numberPairsUsedLength);

			for (int i = 0; i < numberPairsUsedLength; i++)
				{  binaryFile.WriteInt32(numberPairs[i]);  }
			}


		/* Function: FromBinaryFile
		 * Replaces the current number set with one from the passed <BinaryFile>.
		 */
		public void FromBinaryFile (BinaryFile binaryFile)
			{
			// DEPENDENCY: This has to be able to be called from the constructor when numberPairs is null.

			// [int32: numbers] - Like numberPairsUsedLength, a count of integers, not of pairs.
			// [int32: low] [int32: high]
			// [int32: low] [int32: high]
			// ...

			int length = binaryFile.ReadInt32();

			if (length < 0 || length % 2 != 0)
				{  throw new FormatException();  }

			if (numberPairs == null || numberPairs.Length < length || numberPairs.Length - length >= NumberPairShrinkThreshold)
				{  
				if (length == 0)
					{  numberPairs = new int[2];  }
				else
					{  numberPairs = new int[length];  }
				}

			numberPairsUsedLength = length;

			for (int i = 0; i < length; i++)
				{  numberPairs[i] = binaryFile.ReadInt32();  }

			if (!Validate())
				{  
				numberPairsUsedLength = 0;
				throw new FormatException();  
				}
			}


		/* Function: Validate
		 * Checks whether <numberPairs> is in the proper format to be used.
		 */
		protected bool Validate()
			{
			if (numberPairsUsedLength < 0 || numberPairsUsedLength % 2 != 0)
				{  return false;  }

			for (int i = 0; i < numberPairsUsedLength; i += 2)
				{
				if (numberPairs[i] <= 0 || numberPairs[i+1] < numberPairs[i])
					{  return false;  }
				if (i > 0 && numberPairs[i] <= numberPairs[i-1] + 1)
					{  return false;  }
				}

			return true;
			}
			
			
			
		// Group: Properties
		// __________________________________________________________________________
			
			
		/* Property: IsEmpty
		 * Whether the set is empty.
		 */
		public bool IsEmpty
			{
			get
				{  return (numberPairsUsedLength == 0);  }
			}
			
			
		/* Property: LowestAvailable
		 * The lowest unused number available, starting at one.
		 */
		public int LowestAvailable
			{
			get
				{
				if (numberPairsUsedLength == 0)
					{  return 1;  }
				else if (numberPairs[0] > 1)
					{  return 1;  }
				else
					{  return numberPairs[1] + 1;  }
				}
			}
			
			
		/* Property: Highest
		 * The highest number in the set or zero if the set is empty.
		 */
		public int Highest
			{
			get
				{
				if (numberPairsUsedLength == 0)
					{  return 0;  }
				else
					{  return numberPairs[numberPairsUsedLength - 1];  }
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
				
				while (index < numberPairsUsedLength)
					{
					count += (numberPairs[index + 1] - numberPairs[index] + 1);
					index += 2;
					}
					
				return count;
				}
			}
			
		
			
			
		// Group: Protected Functions
		// __________________________________________________________________________
		
		
		/* Function: FindPairIndex
		 * Finds the pair that would hold the passed number and returns its index into the array.  If the number is
		 * not in the array, it returns the index of the pair above it (the insertion point if a new pair were to be
		 * created.)  If it's lower than any pair, it returns zero.  If it's higher than any pair, it returns the index past
		 * the last pair so you must check the result against <numberPairsUsedLength>.
		 */
		protected int FindPairIndex (int number)
			{
			if (numberPairsUsedLength == 0)
				{  return 0;  }
			
			int firstPairIndex = 0;
			int lastPairIndex = numberPairsUsedLength - 2;  // lastPairIndex is inclusive.
			
			for (;;)
				{
				// Find the midpoint.
				int testPairIndex = (firstPairIndex + lastPairIndex) / 2;
				
				unchecked
					{
					// Use FFFE to enforce that the result is even.
					testPairIndex &= (int)0xFFFFFFFE;
					}
				
				if (number < numberPairs[testPairIndex])
					{
					if (testPairIndex == firstPairIndex)
						{  return testPairIndex;  }
					else
						{  lastPairIndex = testPairIndex - 2;  }
					}
					
				else if (number > numberPairs[testPairIndex + 1])
					{
					if (testPairIndex == lastPairIndex)
						{  return lastPairIndex + 2;  }
					else if (firstPairIndex == lastPairIndex - 2)
						{  firstPairIndex += 2;  }
					else
						{  firstPairIndex = testPairIndex + 2;  }
					}
					
				else // number is in the pair
					{
					return testPairIndex;
					}
				}
			}
			
			
		/* Function: InsertAtIndex
		 * Creates a space in <numberPairs> for a new pair at the specified index.  If necessary, will reallocate
		 * the array.  The values in the new space are undefined.
		 */
		protected void InsertAtIndex (int index)
			{
			if (numberPairsUsedLength == numberPairs.Length)
				{
				int[] newArray = new int[ numberPairs.Length + NumberPairGrowLength ];

				if (index > 0)
					{  Array.Copy( numberPairs, 0, newArray, 0, index );  }
				if (index < numberPairsUsedLength)
					{  Array.Copy( numberPairs, index, newArray, index + 2, numberPairsUsedLength - index);  }
					
				numberPairs = newArray;
				numberPairsUsedLength += 2;
				}
				
			else  // we don't have to reallocate the array
				{
				// This is safe to use with overlapping regions of the same array.
				if (index < numberPairsUsedLength)
					{  Array.Copy( numberPairs, index, numberPairs, index + 2, numberPairsUsedLength - index);  }
					
				numberPairsUsedLength += 2;
				}
			}
			
			
		/* Function: DeleteAtIndex
		 * Deletes a pair from <numberPairs> at the specified index and moves everything else down.
		 */
		protected void DeleteAtIndex (int index)
			{
			// Shrink the array if necessary.
			if (numberPairs.Length - numberPairsUsedLength - 2 >= NumberPairShrinkThreshold)
				{
				int[] newArray = new int[numberPairsUsedLength - 2];
				
				if (index > 0)
					{  Array.Copy( numberPairs, newArray, index);  }
				if (index + 2 < numberPairsUsedLength)
					{  Array.Copy( numberPairs, index + 2, newArray, index, numberPairsUsedLength - index - 2 );  }
					
				numberPairs = newArray;
				}
				
			// Otherwise just move everything down.  This is safe to use with overlapping regions of the same array.
			else if (index != numberPairsUsedLength - 2)
				{  Array.Copy( numberPairs, index + 2, numberPairs, index, numberPairsUsedLength - index - 2);  }
				
			numberPairsUsedLength -= 2;
			}
			
			
			
		// Group: Interface Functions
		// __________________________________________________________________________
		
		
		public NumberSetEnumerator GetEnumerator()
			{
			return new NumberSetEnumerator(this);
			}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
			return new NumberSetEnumerator(this);
			}
			
		System.Collections.Generic.IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()
			{
			return new NumberSetEnumerator(this);
			}

			
			
		// Group: Constants
		// __________________________________________________________________________
		
		
		/* Constant: EmptySetString
		 * The string that is generated by <ToString()> for an empty set.  Will not be null.
		 */
		public const string EmptySetString = "{}";
		
		
			
			
		// Group: Variables
		// __________________________________________________________________________
			
			
		/* array: numberPairs
		 * 
		 * An array of the used numbers stored as pairs, with the first being the lower bounds of a consecutive
		 * stretch and the second the upper bounds.  The bounds are inclusive.  So the numbers 1, 2, 3, 4, 8, 11,
		 * and 12 would be stored as 1,4,8,8,11,12, representing 1-4,8,11-12.
		 */
		protected internal int[] numberPairs;
		
		
		/* var: numberPairsUsedLength
		 * The length of the *used* array in <numberPairs> since the array may be larger than the content.  To get 
		 * the memory size use <numberPairs>.Length instead.
		 */
		protected internal int numberPairsUsedLength;
		
		
		
		// Group: Constants
		// __________________________________________________________________________
		
		
		/* Constant: NumberPairGrowLength
		 * The number of array entries to add when the content outstrips the array.  Must be even.
		 */
		protected const int NumberPairGrowLength = 8;
		
		/* Constant: NumberPairShrinkThreshold
		 * The number of unused entries that must be present before the class shrinks the array.  Must be even.
		 */
		protected const int NumberPairShrinkThreshold = 24;

		}




	/* ___________________________________________________________________________
	 * 
	 * Class: GregValure.NaturalDocs.Engine.IDObjects.NumberSetEnumerator
	 * ___________________________________________________________________________
	 * 
	 * An enumerator class that allows <IDObjects.NumberSet> to be used with foreach statements.
	 * 
	 */
	 
	 public class NumberSetEnumerator : System.Collections.Generic.IEnumerator<int>
		{
		
		public NumberSetEnumerator (NumberSet newNumberSet)
			{
			numberSet = newNumberSet;
			currentNumber = 0;
			numberPairIndex = -2;
			}
			
		public int Current
			{
			get
				{
				if (numberPairIndex == -2 || numberPairIndex >= numberSet.numberPairsUsedLength)
					{  throw new InvalidOperationException();  }
				else
					{  return currentNumber;  }
				}
			}
			
		int System.Collections.Generic.IEnumerator<int>.Current
			{
			get
				{  return Current;  }
			}
			
		object System.Collections.IEnumerator.Current
			{
			get
				{  return Current;  }
			}
			
		public bool MoveNext()
			{
			if (numberPairIndex >= numberSet.numberPairsUsedLength)
				{  return false;  }
				
			if (numberPairIndex == -2 || currentNumber == numberSet.numberPairs[ numberPairIndex + 1 ])
				{
				numberPairIndex += 2;
				
				if (numberPairIndex >= numberSet.numberPairsUsedLength)
					{  
					currentNumber = 0;
					return false;  
					}
				else
					{
					currentNumber = numberSet.numberPairs[ numberPairIndex ];
					return true;
					}
				}
			
			else
				{
				currentNumber++;
				return true;
				}
			}
			
		public void Reset()
			{
			currentNumber = 0;
			numberPairIndex = -2;
			}
			
		public void Dispose()
			{
			}
			
		protected NumberSet numberSet;
		protected int currentNumber;
		
		// var: numberPairIndex
		// The index into <NumberSet.numberPairs>, or -2 if we're before the first one.  Will always be set to the first item of a pair.
		protected int numberPairIndex;
		}
	}