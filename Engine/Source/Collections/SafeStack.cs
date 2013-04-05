/* 
 * Class: GregValure.NaturalDocs.Engine.Collections.SafeStack
 * ____________________________________________________________________________
 * 
 * A variation of .NET's Stack class that uses null returns instead of exceptions.
 * 
 * - Calling <Pop()> or <Peek()> on an empty stack returns null (or the default for value types) instead of throwing an exception.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Collections
	{
	public class SafeStack<ObjectType> : System.Collections.Generic.Stack<ObjectType>
		{
		
		/* Function: Peek
		 * Returns the top value on the stack without removing it, or null (or the default for value types) if the stack is empty.
		 */
		new public ObjectType Peek ()
			{
			if (Count == 0)
				{  return default(ObjectType);  }
			else
				{  return base.Peek();  }
			}
			
		/* Function: Pop
		 * Removes and returns the top value on the stack, or null (or the default for value types) if the stack is empty.
		 */
		new public ObjectType Pop ()
			{
			if (Count == 0)
				{  return default(ObjectType);  }
			else
				{  return base.Pop();  }
			}
			
		}
	}