/* 
 * Class: GregValure.NaturalDocs.Engine.ParsedClassPrototype
 * ____________________________________________________________________________
 * 
 * A class that wraps a <Tokenizer> for a prototype that's been marked with <ClassPrototypeParsingTypes>, providing easier 
 * access to things like parent lines.
 * 
 * Usage:
 * 
 *		The functions and properties obviously rely on the relevant tokens being set.  You cannot expect a proper result from
 *		<GetParent()> or <NumberOfParents> unless the tokens are marked with <ClassPrototypeParsingType.StartOfParents>
 *		and <ClassPrototypeParsingType.ParentSeparator>.  Likewise, you can't get anything from <GetParentName()> unless
 *		you also have tokens marked with <ClassPrototypeParsingType.Name>.  However, you can set the parent divider tokens,
 *		call <GetParent()>, and then use those bounds to further parse the parent and set tokens like 
 *		<ClassPrototypeParsingType.Name>.
 *		
 *		You can set multiple consecutive tokens to <ClassPrototypeParsingType.StartOfParents> and 
 *		<ClassPrototypeParsingType.ParentSeparator> and they will be counted as one separator.  However, all whitespace in
 *		between them must be marked as well.
 * 
 *		An important thing to remember though is that the parent divisions are calculated once and saved.  Only call functions like
 *		<GetParent()> after *ALL* the separator tokens (<ClassPrototypeParsingType.StartOfParents>,
 *		<ClassPrototypeParsingType.ParentSeparator>, and optionally <ClassPrototypeParsingType.StartOfBody>) are set and will 
 *		not change going forward.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Tokenization;


namespace GregValure.NaturalDocs.Engine
	{
	public class ParsedClassPrototype
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: ParsedClassPrototype
		 * Creates a new parsed prototype.
		 */
		public ParsedClassPrototype (Tokenizer prototype)
			{
			tokenizer = prototype;
			dividers = null;
			}


		/* Function: GetName
		 * Gets the bounds of the class name, or returns false if it couldn't find it.
		 */
		public bool GetName (out TokenIterator start, out TokenIterator end)
			{
			start = tokenizer.FirstToken;

			while (start.IsInBounds &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.Name &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfParents &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfBody)
				{  start.Next();  }

			end = start;

			while (end.ClassPrototypeParsingType == ClassPrototypeParsingType.Name)
				{  end.Next();  }

			return (end > start);
			}


		/* Function: GetModifiers
		 * Gets the bounds of any modifiers to the class, such as "static" or "public", or returns false if there aren't any.
		 */
		public bool GetModifiers (out TokenIterator start, out TokenIterator end)
			{
			start = tokenizer.FirstToken;

			while (start.IsInBounds &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.Modifier &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.Name &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfParents &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfBody)
				{  start.Next();  }

			end = start;

			while (end.ClassPrototypeParsingType == ClassPrototypeParsingType.Modifier ||
					 end.FundamentalType == FundamentalType.Whitespace)
				{  end.Next();  }

			end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

			return (end > start);
			}


		/* Function: GetTemplateSuffix
		 * Gets the bounds of the template suffix attached to the class name, such as "<T>" in "List<T>", or returns false if there isn't one.
		 */
		public bool GetTemplateSuffix (out TokenIterator start, out TokenIterator end)
			{
			start = tokenizer.FirstToken;

			while (start.IsInBounds &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.TemplateSuffix &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfParents &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfBody)
				{  start.Next();  }

			end = start;

			while (end.ClassPrototypeParsingType == ClassPrototypeParsingType.TemplateSuffix)
				{  end.Next();  }

			return (end > start);
			}


		/* Function: GetPostModifiers
		 * Gets the bounds of any modifiers that appear after the class name, or returns false if there aren't any.
		 */
		public bool GetPostModifiers (out TokenIterator start, out TokenIterator end)
			{
			start = tokenizer.FirstToken;

			while (start.IsInBounds && 
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.Name &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfParents &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfBody)
				{  start.Next();  }

			while (start.IsInBounds && 
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.Modifier &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfParents &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfBody)
				{  start.Next();  }

			end = start;

			while (end.IsInBounds && 
					 (end.ClassPrototypeParsingType == ClassPrototypeParsingType.Modifier || 
					  end.FundamentalType == FundamentalType.Whitespace) )
				{  end.Next();  }

			end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

			return (end > start);
			}


		/* Function: GetParent
		 * Gets the bounds of the numbered parent, or returns false if it doesn't exist.  Numbers start at zero.
		 */
		public bool GetParent (int index, out TokenIterator start, out TokenIterator end)
			{
			if (dividers == null)
				{  CalculateDividers();  }

			if (index >= NumberOfParents)
				{
				start = tokenizer.LastToken;
				end = start;
				return false;
				}
			else
				{
				int startIndex = dividers[index] + 1;  // to skip the divider
				int endIndex = dividers[index + 1];

				start = tokenizer.FirstToken;
				start.Next(startIndex);

				end = start;
				end.Next(endIndex - startIndex);

				while (start < end && 
						 (start.ClassPrototypeParsingType == ClassPrototypeParsingType.StartOfParents ||
						  start.ClassPrototypeParsingType == ClassPrototypeParsingType.ParentSeparator) )
					{  start.Next();  }

				TrimWhitespace(ref start, ref end);

				return true;
				}
			}


		/* Function: GetParentName
		 * Gets the bounds of the parent's name, or returns false if it couldn't find it.
		 */
		public bool GetParentName (int index, out TokenIterator start, out TokenIterator end)
			{
			TokenIterator parentStart, parentEnd;

			if (!GetParent(index, out parentStart, out parentEnd))
				{  
				start = parentEnd;
				end = parentEnd;
				return false;  
				}

			start = parentStart;

			while (start < parentEnd && start.ClassPrototypeParsingType != ClassPrototypeParsingType.Name)
				{  start.Next();  }

			end = start;

			while (end < parentEnd && end.ClassPrototypeParsingType == ClassPrototypeParsingType.Name)
				{  end.Next();  }

			return (end > start);
			}


		/* Function: GetParentModifiers
		 * Gets the bounds of the parent's modifiers, such as "public", or returns false if it couldn't find any.
		 */
		public bool GetParentModifiers (int index, out TokenIterator start, out TokenIterator end)
			{
			TokenIterator parentStart, parentEnd;

			if (!GetParent(index, out parentStart, out parentEnd))
				{  
				start = parentEnd;
				end = parentEnd;
				return false;  
				}

			start = parentStart;

			while (start < parentEnd && 
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.Modifier &&
					 start.ClassPrototypeParsingType != ClassPrototypeParsingType.Name)
				{  start.Next();  }

			end = start;

			while (end < parentEnd && 
					(end.ClassPrototypeParsingType == ClassPrototypeParsingType.Modifier || 
					 end.FundamentalType == FundamentalType.Whitespace) )
				{  end.Next();  }

			end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

			return (end > start);
			}


		/* Function: GetParentTemplateSuffix
		 * Gets the bounds of the parent's template suffix, or returns false if it couldn't find one.
		 */
		public bool GetParentTemplateSuffix (int index, out TokenIterator start, out TokenIterator end)
			{
			TokenIterator parentStart, parentEnd;

			if (!GetParent(index, out parentStart, out parentEnd))
				{  
				start = parentEnd;
				end = parentEnd;
				return false;  
				}

			start = parentStart;

			while (start < parentEnd && start.ClassPrototypeParsingType != ClassPrototypeParsingType.TemplateSuffix)
				{  start.Next();  }

			end = start;

			while (end < parentEnd && end.ClassPrototypeParsingType == ClassPrototypeParsingType.TemplateSuffix)
				{  end.Next();  }

			return (end > start);
			}


		/* Function: GetParentPostModifiers
		 * Gets the bounds of modifiers appearing *after* the parent, or returns false if it couldn't find any.
		 */
		public bool GetParentPostModifiers (int index, out TokenIterator start, out TokenIterator end)
			{
			TokenIterator parentStart, parentEnd;

			if (!GetParent(index, out parentStart, out parentEnd))
				{  
				start = parentEnd;
				end = parentEnd;
				return false;  
				}

			start = parentStart;

			while (start < parentEnd && start.ClassPrototypeParsingType != ClassPrototypeParsingType.Name)
				{  start.Next();  }

			while (start < parentEnd && start.ClassPrototypeParsingType != ClassPrototypeParsingType.Modifier)
				{  start.Next();  }

			end = start;

			while (end < parentEnd && 
						(end.ClassPrototypeParsingType == ClassPrototypeParsingType.Modifier || 
						 end.FundamentalType == FundamentalType.Whitespace) )
				{  end.Next();  }

			end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

			return (end > start);
			}


		/* Function: CalculateDividers
		 */
		protected void CalculateDividers ()
			{
			TokenIterator iterator = tokenizer.FirstToken;

			while (iterator.IsInBounds && iterator.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfParents)
				{  iterator.Next();  }

			if (iterator.IsInBounds == false)
				{
				dividers = new int[1];
				dividers[0] = iterator.TokenIndex;
				}
			else // we have StartOfParents
				{
				TokenIterator startOfParents = iterator;

				do
					{  iterator.Next();  }
				while (iterator.ClassPrototypeParsingType == ClassPrototypeParsingType.StartOfParents);

				int numberOfParents = 1;

				while (iterator.IsInBounds && iterator.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfBody)
					{
					if (iterator.ClassPrototypeParsingType == ClassPrototypeParsingType.ParentSeparator)
						{  
						numberOfParents++;  

						do
							{  iterator.Next();  }
						while (iterator.ClassPrototypeParsingType == ClassPrototypeParsingType.ParentSeparator);
						}
					else
						{
						iterator.Next();
						}
					}

				dividers = new int[numberOfParents + 1];
				dividers[0] = startOfParents.TokenIndex;

				int i = 1;
				iterator = startOfParents;

				do
					{  iterator.Next();  }
				while (iterator.ClassPrototypeParsingType == ClassPrototypeParsingType.StartOfParents);

				while (iterator.IsInBounds && iterator.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfBody)
					{
					if (iterator.ClassPrototypeParsingType == ClassPrototypeParsingType.ParentSeparator)
						{
						dividers[i] = iterator.TokenIndex;
						i++;

						do
							{  iterator.Next();  }
						while (iterator.ClassPrototypeParsingType == ClassPrototypeParsingType.ParentSeparator);
						}
					else
						{
						iterator.Next();
						}
					}

				// End of prototype or start of body
				dividers[i] = iterator.TokenIndex;
				}
			}


		/* Function: TrimWhitespace
		 * Shrinks the passed bounds to exclude whitespace on the edges.
		 */
		protected void TrimWhitespace (ref TokenIterator start, ref TokenIterator end)
			{
			end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);
			start.NextPastWhitespace(end);
			}
			


		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Tokenizer
		 * The tokenized prototype.
		 */
		public Tokenizer Tokenizer
			{
			get
				{  return tokenizer;  }
			}


		/* Property: NumberOfParents
		 */
		public int NumberOfParents
			{
			get
				{  
				if (dividers == null)
					{  CalculateDividers();  }

				return dividers.Length - 1;
				}
			}


		/* Property: HasBody
		 */
		public bool HasBody
			{
			get
				{
				if (dividers == null)
					{  CalculateDividers();  }

				int lastDivider = dividers[ dividers.Length - 1 ];
				return (tokenizer.ClassPrototypeParsingTypeAt(lastDivider) == ClassPrototypeParsingType.StartOfBody);
				}
			}


		
		// Group: Variables
		// __________________________________________________________________________
		
		/* var: tokenizer
		 * The <Tokenizer> containing the full prototype.
		 */
		protected Tokenizer tokenizer;

		/* var: dividers
		 * 
		 * An array of token indexes representing parent dividers in the prototype, or null if it hasn't been generated yet.  The 
		 * first one represents the beginning of the parent list, the last one represents the beginning of the body or the end of 
		 * the prototype, and each one in between a divider between parent entries.  If there are no parent entries it will only 
		 * have one divider for the beginning of the body/end of the prototype.
		 * 
		 * There may be multiple consecutive separator tokens and this only stores the token index of the first one.  Therefore
		 * you must make sure to skip StartOfParents and ParentDivider tokens at the beginning of each segment.
		 */
		protected int[] dividers;

		}
	}