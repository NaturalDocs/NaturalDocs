/* 
 * Struct: GregValure.NaturalDocs.Engine.Symbol
 * ____________________________________________________________________________
 * 
 * A struct encapsulating a symbol, which is a normalized way of representing a hierarchal code element or topic,
 * such as "PackageA.PackageB.FunctionC".  Symbols are normalized to maintain case-insensitivity, to make the
 * separators interchangeable (. :: ->) and to otherwise be tolerant.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine.Tokenization;


namespace GregValure.NaturalDocs.Engine
	{
	public struct Symbol : IComparable
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Symbol
		 */
		private Symbol (string newSymbolString)
			{
			symbolString = newSymbolString;
			}
			
		/* Function: FromPlainText
		 * Creates a symbol from the passed string of plain text.  It will be normalized.  If you know the string is already
		 * in a normalized form because it originally came from another Symbol object, use <FromSymbolString()>.
		 */
		static public Symbol FromPlainText (string plainText)
			{
			Symbol symbol = new Symbol(plainText);
			symbol.Normalize();
			
			return symbol;
			}
			
		/* Function: FromSymbolString
		 * Creates a symbol from the passed string which originally came from another Symbol object.  This skips the
		 * normalization stage because it should already be in the proper format.  If there's any doubt about this, use
		 * <FromPlainText()> instead.
		 */
		static public Symbol FromSymbolString (string symbolString)
			{
			Symbol symbol = new Symbol(symbolString);
			return symbol;
			}
			
		
		
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: LastSegment
		 * Returns the last segment of the symbol as its own symbol.  So for "PackageA.PackageB.FunctionC" this will
		 * return a symbol for "FunctionC".  If there are no separators it will return a copy of this symbol.
		 */
		public Symbol LastSegment
			{
			get
				{
				Symbol result = new Symbol();
				
				int lastIndex = symbolString.LastIndexOf('\x001F');
				
				if (lastIndex == -1)
					{  result.symbolString = symbolString;  }
				else
					{  result.symbolString = symbolString.Substring(lastIndex + 1);  }
					
				return result;
				}
			}
						
			
			
		// Group: Operators
		// __________________________________________________________________________
		
		
		/* Operator: operator string
		 * A cast operator to covert the Symbol to a string.
		 */
		public static implicit operator string (Symbol symbol)
			{
			return symbol.symbolString;
			}
						
		/* Operator: operator Symbol
		 * 
		 * A cast operator to covert a string to a Symbol.  It will be normalized.
		 * 
		 * Although I would prefer not to have it in order to force <FromPlainText()>/<FromSymbolString()>, it is necessary
		 * for the == operator to be able to compare against null correctly.  If this function isn't defined the custom ==
		 * operators won't be called and it will always be false.  Why?  I'm not exactly sure.
		 */
		public static implicit operator Symbol (string text)
			{
			return FromPlainText(text);
			}
						
		/* Operator: operator ==
		 */
		public static bool operator== (Symbol a, Symbol b)
			{
			// Since they're structs we don't have to worry about null objects.
			return (a.symbolString == b.symbolString);
			}

		/* Operator: operator !=
		 */
		public static bool operator!= (Symbol a, Symbol b)
			{
			// Since they're structs we don't have to worry about null objects.
			return (a.symbolString != b.symbolString);
			}

		/* Function: ToString
		 * Returns the Symbol as a string.
		 */
		public override string ToString ()
			{
			return symbolString;
			}
			
		/* Function: GetHashCode
		 */
		public override int GetHashCode ()
			{
			return symbolString.GetHashCode();
			}

		/* Function: Equals
		 */
		public override bool Equals (object obj)
			{
			if (obj == null)
				{  return (symbolString == null);  }
			else if (obj is Symbol)
				{  return (this == (Symbol)obj);  }
			else
				{  return false;  }
			}
			
		/* Function: CompareTo
		 */
		public int CompareTo (object other)
			{
			return symbolString.CompareTo(other);
			}
		
			
			
		// Group: Private Functions
		// __________________________________________________________________________
		
		
		/* Function: Normalize
		 * Normalizes <symbolString>.
		 * 
		 *		- Converts all characters to lowercase.
		 *		- Whitespace is removed unless it is between two text characters as defined by <Tokenizer.FundamentalTypeOf()>.
		 *		- Whitespace not removed is condensed into a single space.
		 *		- Replaces the common package separator symbols (. :: ->) with 0x1F.
		 *		- Multiple consecutive separators are condensed into one.
		 *		- Separators on the edges are removed.
		 */
		private void Normalize ()
			{
			if (symbolString == null)
			    {  return;  }
			if (symbolString == "")
			    {
			    symbolString = null;
			    return;
			    }
				
			symbolString = symbolString.ToLower();
			int nextChar = symbolString.IndexOfAny(startingSeparatorCharsAndWhitespace);

			if (nextChar == -1)
			    {  return;  }
			
			System.Text.StringBuilder normalizedString = new System.Text.StringBuilder();
			int index = 0;
			
			// Set to true if we just added a separator, so we don't want to add another one right after it.  Starts at true since
			// we don't want one to lead off the symbol.
			bool ignoreSeparator = true;
			
			// Set to true if we just passed whitespace, since we only want to add it to the normalized string if it's between two 
			// text characters.  We also want to condense multiple characters to a single space.
			bool addWhitespace = false;
			
			do
			    {
			    if (nextChar > index)
			        {
			        if (addWhitespace && normalizedString.Length > 0 &&
						Tokenizer.FundamentalTypeOf( normalizedString[normalizedString.Length - 1] ) == TokenType.Text &&
						Tokenizer.FundamentalTypeOf( symbolString[index] ) == TokenType.Text)
						{
						normalizedString.Append(' ');
						}
					
			        normalizedString.Append(symbolString, index, nextChar - index);
			        ignoreSeparator = false;
			        addWhitespace = false;
			        }
				
			    if (symbolString[nextChar] == ' ' || symbolString[nextChar] == '\t')
			        {  
			        addWhitespace = true;
			        // doesn't affect ignoreSeparator
			        index = nextChar + 1;  
			        }
			    else if (symbolString[nextChar] == '.')
			        {
			        if (!ignoreSeparator)
			            {
			            normalizedString.Append('\x001F');
			            ignoreSeparator = true;
			            addWhitespace = false;
			            }
						
			        index = nextChar + 1;
			        }
			    else if (index + 1 < symbolString.Length && 
			              ( (symbolString[nextChar] == ':' && symbolString[nextChar + 1] == ':') ||
			                (symbolString[nextChar] == '-' && symbolString[nextChar + 1] == '>') )
			             )
			        {
			        if (!ignoreSeparator)
			            {
			            normalizedString.Append('\x001F');
			            ignoreSeparator = true;
			            addWhitespace = false;
			            }
						
			        index = nextChar + 2;
			        }
			    else
			        {
			        normalizedString.Append(symbolString[nextChar]);
			        ignoreSeparator = false;
			        addWhitespace = false;
			        index = nextChar + 1;
			        }
				
			    nextChar = symbolString.IndexOfAny(startingSeparatorCharsAndWhitespace, index);
			    }
			while (nextChar != -1);
			
			if (index < symbolString.Length)
			    {
		        if (addWhitespace && normalizedString.Length > 0 &&
					Tokenizer.FundamentalTypeOf( normalizedString[normalizedString.Length - 1] ) == TokenType.Text &&
					Tokenizer.FundamentalTypeOf( symbolString[index] ) == TokenType.Text)
					{
					normalizedString.Append(' ');
					}
					
			    normalizedString.Append(symbolString, index, symbolString.Length - index);
			    }
				
			if (normalizedString.Length > 0 && normalizedString[ normalizedString.Length - 1 ] == '\x001F')
			    {
			    normalizedString.Remove( normalizedString.Length - 1, 1 );
			    }
				
			if (normalizedString.Length == 0)
			    {  symbolString = null;  }
			else
			    {  symbolString = normalizedString.ToString();  }
			}


			
		
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* string: symbolString
		 * The symbol, _always_ in normalized form.
		 */
		private string symbolString;
	
		/* var: startingSeparatorCharsAndWhitespace
		 * An array containing the whitespace characters and the first characters of all the possible separators.
		 */
		static private char[] startingSeparatorCharsAndWhitespace = new char[] { ' ', '\t', ':', '-', '.' };
		}
	}