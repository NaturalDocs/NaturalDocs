/* 
 * Class: GregValure.NaturalDocs.Engine.ParsedPrototype
 * ____________________________________________________________________________
 * 
 * A class representing a prototype that has been parsed into its component pieces.
 * 
 * Usage:
 * 
 *		- Create the object for the prototype.
 *		- If necessary, add markers for the start of each parameter from left to right with <AddStartOfParameter()>.  They 
 *		  *must* be added in order.
 *		- If necessary, add a marker for the end of the parameter list with <AddEndOfParameters()>.  If this is called at all,
 *		  it *must* be called after all parameters have been marked.
 *		  
 * Examples:
 * 
 *		> void Function (int x, int y)
 *		
 *		- "void Function ("
 *		- <AddStartOfParameter()>
 *		- "int x,"
 *		- <AddStartOfParameter()>
 *		- " int y"
 *		- <AddEndOfParameters()>.
 *		- ")"
 *		
 *		It is okay to do these:
 *		
 *		> void Function ()
 *		
 *		- "void Function ("
 *		- <AddStartOfParameter()>
 *		- <AddEndOfParameters()>
 *		- ")"
 *		
 *		> void Function ( )
 *		
 *		- "void Function ("
 *		- <AddStartOfParameter()>
 *		- " "
 *		- <AddEndOfParameters()>
 *		- ")"
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Tokenization;


namespace GregValure.NaturalDocs.Engine
	{
	public class ParsedPrototype
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: ParsedPrototype
		 */
		public ParsedPrototype (Tokenizer tokenizedPrototype)
			{
			tokenizer = tokenizedPrototype;
			parameterDividers = null;
			endOfParametersDivider = tokenizer.LastToken;
			needCleanup = false;
			}

		/* Constructor: ParsedPrototype
		 * If you already have the prototype in tokenized form it's more efficient to call the other constructor.
		 */
		public ParsedPrototype (string prototype)
			{
			tokenizer = new Tokenizer(prototype);
			parameterDividers = null;
			endOfParametersDivider = tokenizer.LastToken;
			needCleanup = false;
			}
			
		
		/* Function: AddStartOfParameter
		 * Adds a marker designating the start of a parameter.  You are required to add these left to right and only call
		 * <AddEndOfParameters()> after all parameters are done.  Parameters should be started after the opening parenthesis 
		 * of the parameter list and after the comma dividing one from another, or whatever the convention of the language is.
		 */
		public void AddStartOfParameter (TokenIterator iterator)
			{
			if (parameterDividers == null)
				{  parameterDividers = new List<TokenIterator>();  }

			parameterDividers.Add(iterator);
			needCleanup = true;
			}

		/* Function: AddEndOfParameters
		 * Adds a marker designating the end of the parameter list.  You can only call this after all parameters are marked
		 * with <AddStartOfParameter()>.  You are not required to call this if it's not relevant.  Also, you can call this if there's
		 * only whitespace between the start of the last parameter and this point and it will detect it automatically.
		 */
		public void AddEndOfParameters (TokenIterator iterator)
			{
			// Ignore it if there's no parameters.
			if (parameterDividers == null)
				{  return;  }

			endOfParametersDivider = iterator;
			needCleanup = true;
			}
			
		/* Function: Cleanup
		 * Goes through the dividers and removes any that would cause a section of only whitespace.
		 */
		protected void Cleanup ()
			{
			if (parameterDividers != null)
				{
				// Check if there's any content between the start of the last parameter and the end of the parameters so
				// we can handle cases like "Function ()" and "Function ( )".  If the end of the parameters was never set it
				// will be at the end of the tokenizer so we don't have to account for that.

				TokenIterator iterator = parameterDividers[ parameterDividers.Count -1 ];
				bool hasContent = false;

				while (iterator < endOfParametersDivider)
					{
					if (iterator.FundamentalType != FundamentalType.Whitespace)
						{  
						hasContent = true;  
						break;
						}
					else
						{  iterator.Next();  }
					}

				if (!hasContent)
					{  
					if (parameterDividers.Count == 1)
						{  parameterDividers = null;  }
					else
						{  parameterDividers.RemoveAt( parameterDividers.Count - 1 );  }
					}
				}

			// If there's no parameters the end of parameter divider becomes useless so we reset it.  This is a separate if
			// statement instead of an else because parameterDividers may have been changed to null above.
			if (parameterDividers == null)
				{  endOfParametersDivider = tokenizer.LastToken;  }

			needCleanup = false;
			}


		/* Function: GetBeforeParameters
		 * Returns the bounds of the section of the prototype prior to the parameters.  If there are no parameters, this will
		 * return the bound of the entire prototype.
		 */
		public void GetBeforeParameters (out TokenIterator start, out TokenIterator end)
			{
			if (needCleanup)
				{  Cleanup();  }

			start = tokenizer.FirstToken;

			if (parameterDividers == null)
				{  end = tokenizer.LastToken;  }
			else
				{  end = parameterDividers[0];  }
			}


		/* Function: GetParameter
		 * Returns the bounds of the numbered parameter and whether or not it exists.  Numbers start at zero.
		 */
		public bool GetParameter (int index, out TokenIterator start, out TokenIterator end, bool trimWhitespace = true)
			{
			if (needCleanup)
				{  Cleanup();  }

			if (parameterDividers == null || index >= parameterDividers.Count)
				{
				start = tokenizer.LastToken;
				end = tokenizer.LastToken;
				return false;
				}
			
			start = parameterDividers[index];

			if (index + 1 == parameterDividers.Count)
				{  
				// If it was never set it will be on the last token, which is still what we want.
				end = endOfParametersDivider;  
				}
			else
				{  end = parameterDividers[index + 1];  }

			if (trimWhitespace)
				{
				TokenIterator temp = end;
				temp.Previous();

				while (temp.FundamentalType == FundamentalType.Whitespace && temp >= start)
					{  
					end = temp;
					temp.Previous();
					}

				while (start.FundamentalType == FundamentalType.Whitespace && start < end)
					{  start.Next();  }
				}

			return true;
			}


		/* Function: GetAfterParameters
		 * Returns the bounds of the section of the prototype after the parameters and whether it exists.
		 */
		public bool GetAfterParameters (out TokenIterator start, out TokenIterator end)
			{
			if (needCleanup)
				{  Cleanup();  }

			start = endOfParametersDivider;
			end = tokenizer.LastToken;

			return (start != end);
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


		/* Property: NumberOfParameters
		 */
		public int NumberOfParameters
			{
			get
				{
				if (needCleanup)
					{  Cleanup();  }

				if (parameterDividers == null)
					{  return 0;  }
				else
					{  return parameterDividers.Count;  }
				}
			}
			

		
		// Group: Variables
		// __________________________________________________________________________
		
		/* var: tokenizer
		 * The <Tokenizer> containing the full prototype.
		 */
		protected Tokenizer tokenizer;

		/* var: parameterDividers
		 * A list of <TokenIterators> representing the dividers between each parameter, or null if there are no parameters.
		 * Each iterator should be on the first character of the parameter.
		 */
		protected List<TokenIterator> parameterDividers;
		
		/* var: endOfParametersDivider
		 * A <TokenIterator> serving as the divider for where the last parameter ends and where the remainder of the 
		 * prototype begins.  It should be on the first character past the end of the last parameter.  If there are no parameters
		 * or there is no content past the end of the last one, this will be set to one past the last token in <tokenizer>.
		 */
		protected TokenIterator endOfParametersDivider;

		/* var: needCleanup
		 * Whether any of the dividers need to be cleaned, meaning removing them if they're not dividing anything but 
		 * whitespace.
		 */
		protected bool needCleanup;

		}
	}