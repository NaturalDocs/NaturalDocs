/* 
 * Struct: GregValure.NaturalDocs.Engine.Symbols.SymbolString
 * ____________________________________________________________________________
 * 
 * A struct encapsulating a symbol string, which is a normalized way of representing a hierarchal code element 
 * or topic, such as "PackageA.PackageB.FunctionC".
 * 
 * The encoding uses <SeparatorChars.Level1>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine.Tokenization;


namespace GregValure.NaturalDocs.Engine.Symbols
	{
	public struct SymbolString : IComparable
		{
		
		// Group: Constants
		// __________________________________________________________________________

		/* Constant: SeparatorChar
		 * The character used to separate symbol segments.
		 */
		public const char SeparatorChar = SeparatorChars.Level1;



		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: SymbolString
		 */
		private SymbolString (string newSymbolString)
			{
			symbolString = newSymbolString;
			}

			
		/* Function: FromPlainText
		 * 
		 * Creates a SymbolString from the passed string of plain text.
		 * 
		 * If the string ends in parenthesis they will be separated off from the string and returned in the parenthesis variable.  
		 * They will not be part of the resulting SymbolString.  The string is still in its raw form so to become a <ParameterString> it
		 * would need to be passed to <ParameterString.FromParenthesisString()>.  If there's no parenthesis the variable will be null.
		 * 
		 * The string will be normalized.  If you know the string is already in a normalized form because it originally came 
		 * from another SymbolString object, use <FromExportedString()>.
		 */
		static public SymbolString FromPlainText (string textSymbol, out string parenthesis)
			{
			if (textSymbol == null)
				{  throw new NullReferenceException();  }

			int parenthesisIndex = ParameterString.GetEndingParenthesisIndex(textSymbol);

			if (parenthesisIndex == -1)
				{  parenthesis = null;  }
			else
				{
				parenthesis = textSymbol.Substring(parenthesisIndex);
				textSymbol = textSymbol.Substring(0, parenthesisIndex);
				}

			SymbolString symbolString = new SymbolString(textSymbol);
			symbolString.Normalize();

			// If a symbol string is normalized to nothing yet it had parenthesis (think "::()") put them back together and redo.
			// This should be a rare edge case but we want to handle it.  We never want a null symbol string with a valid 
			// parenthesis string.
			if (symbolString.symbolString == null && parenthesis != null)
				{
				symbolString = new SymbolString(textSymbol + parenthesis);
				symbolString.Normalize();

				parenthesis = null;
				}
			
			return symbolString;
			}

			
		/* Function: FromPlainText_ParenthesisAlreadyRemoved
		 * 
		 * Creates a SymbolString from the passed string of plain text where the parenthesis have already been removed.
		 * 
		 * We use this awkward function name because 90% of the time you need to handle parenthesis, or at least strip them
		 * off.  If we just made an overload of <FromPlainText()> without the out parameter people would use this one by accident.
		 * By attaching _ParethesisAlreadyRemoved it forces you to only use this one if you know what you're doing.
		 * 
		 * The string will be normalized.  If you know the string is already in a normalized form because it originally came 
		 * from another SymbolString object, use <FromExportedString()>.
		 */
		static public SymbolString FromPlainText_ParenthesisAlreadyRemoved (string textSymbol)
			{
			if (textSymbol == null)
				{  throw new NullReferenceException();  }

			SymbolString symbolString = new SymbolString(textSymbol);
			symbolString.Normalize();

			return symbolString;
			}

			
		/* Function: FromExportedString
		 * 
		 * Creates a SymbolString from the passed string which originally came from another SymbolString object.  This skips 
		 * the normalization stage because it should already be in the proper format.  Only use this when retrieving SymbolStrings
		 * that were stored as plain text in a database or other data file.  All other uses should call <FromPlainText()> instead.
		 * 
		 * This throws an exception if <SeparatorChars.Escape> is the first character, as that signifies a special string that should
		 * not be interpreted as a SymbolString.  Null is acceptable however.
		 */
		static public SymbolString FromExportedString (string exportedSymbolString)
			{
			if (exportedSymbolString != null)
				{
				if (exportedSymbolString.Length == 0)
					{  exportedSymbolString = null;  }
				else if (exportedSymbolString[0] == SeparatorChars.Escape)
					{  throw new FormatException("You cannot convert an escaped string to a SymbolString.");  }
				}

			return new SymbolString(exportedSymbolString);
			}
			
		
		
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: EndingSymbol
		 * Returns the <EndingSymbol> of the symbol string.  So for "PackageA.PackageB.FunctionC" this will return
		 * an <EndingSymbol> for "functionc".  Remember that unlike SymbolStrings, <EndingSymbols> are case-insensitive.
		 */
		public EndingSymbol EndingSymbol
			{
			get
				{
				if (symbolString == null)
					{  return new EndingSymbol();  }

				int lastSeparator = symbolString.LastIndexOf(SeparatorChar);
				
				if (lastSeparator == -1)
					{  return EndingSymbol.FromSymbolStringSegment(symbolString);  }
				else
					{  return EndingSymbol.FromSymbolStringSegment(symbolString.Substring(lastSeparator + 1));  }
				}
			}
						
			
			
		// Group: Operators
		// __________________________________________________________________________
		
		
		/* operator: operator string
		 * A cast operator to covert the symbol to a string.
		 */
		public static implicit operator string (SymbolString symbol)
			{
			return symbol.symbolString;
			}
						
		/* Operator: operator ==
		 */
		public static bool operator== (SymbolString a, object b)
			{
			// We need to make the operator compare against object intead of another SymbolString in order to support
			// directly comparing against null.
			return a.Equals(b);
			}

		/* Operator: operator !=
		 */
		public static bool operator!= (SymbolString a, object b)
			{
			return !(a.Equals(b));
			}

		/* Operator: operator +
		 * Concatenates the two SymbolStrings.
		 */
		public static SymbolString operator+ (SymbolString a, SymbolString b)
			{
			if (a.symbolString == null)
				{  return b;  }
			else if (b.symbolString == null)
				{  return a;  }
			else
				{
				// Since they're both normalized already we can safely take this shortcut.
				return new SymbolString(a.symbolString + SeparatorChar + b.symbolString);
				}
			}

		/* Function: ToString
		 * Returns the SymbolString as a string.
		 */
		public override string ToString ()
			{
			return symbolString;
			}
			
		/* Function: GetHashCode
		 */
		public override int GetHashCode ()
			{
			if (symbolString == null)
				{  return 0;  }
			else
				{  return symbolString.GetHashCode();  }
			}

		/* Function: Equals
		 */
		public override bool Equals (object other)
			{
			if (other == null)
				{  return (symbolString == null);  }
			else if (other is SymbolString)
				{  return (symbolString == ((SymbolString)other).symbolString);  }
			else if (other is string)
				{  return (symbolString == (string)other);  }
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
		 * 
		 * Normalizes <symbolString>.
		 * 
		 *		- Applies canonical normalization to Unicode (FormC).
		 *		- Removes all existing instances of the <SeparatorChars>, including <SeparatorChar.Escape>.
		 *		- Whitespace is removed unless it is between two text characters as defined by <Tokenizer.FundamentalTypeOf()>.
		 *		- Whitespace not removed is condensed into a single space.
		 *		- Replaces the common package separator symbols (. :: ->) with <SeparatorChar>.
		 *		- Multiple consecutive separators are condensed into one.
		 *		- Separators on the edges are removed.
		 */
		private void Normalize ()
			{
			if (symbolString == null)
				{  return;  }

			symbolString = symbolString.Trim();

			if (symbolString == "")
				{
				symbolString = null;
				return;
				}
				
			symbolString = symbolString.Normalize(System.Text.NormalizationForm.FormC);  // Canonical decomposition and recombination

			int nextChar = symbolString.IndexOfAny(startingSeparatorCharsAndWhitespace);

			if (nextChar == -1)
				{  return;  }
			
			System.Text.StringBuilder normalizedString = new System.Text.StringBuilder(symbolString.Length);
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
						Tokenizer.FundamentalTypeOf( normalizedString[normalizedString.Length - 1] ) == FundamentalType.Text &&
						Tokenizer.FundamentalTypeOf( symbolString[index] ) == FundamentalType.Text)
						{
						normalizedString.Append(' ');
						}
					
					normalizedString.Append(symbolString, index, nextChar - index);
					ignoreSeparator = false;
					addWhitespace = false;
					}

				if (symbolString[nextChar] >= SeparatorChars.LowestValue && symbolString[nextChar] <= SeparatorChars.HighestValue)
					{
					// Ignore, doesn't affect anything.
					index = nextChar + 1;
					}				
				else if (symbolString[nextChar] == ' ' || symbolString[nextChar] == '\t')
					{  
					addWhitespace = true;
					// doesn't affect ignoreSeparator
					index = nextChar + 1;  
					}
				else if (symbolString[nextChar] == '.')
					{
					if (!ignoreSeparator)
						{
						normalizedString.Append(SeparatorChar);
						ignoreSeparator = true;
						addWhitespace = false;
						}
						
					index = nextChar + 1;
					}
				else if (nextChar + 1 < symbolString.Length && 
							( (symbolString[nextChar] == ':' && symbolString[nextChar + 1] == ':') ||
								(symbolString[nextChar] == '-' && symbolString[nextChar + 1] == '>') )
							)
					{
					if (!ignoreSeparator)
						{
						normalizedString.Append(SeparatorChar);
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
					Tokenizer.FundamentalTypeOf( normalizedString[normalizedString.Length - 1] ) == FundamentalType.Text &&
					Tokenizer.FundamentalTypeOf( symbolString[index] ) == FundamentalType.Text)
					{
					normalizedString.Append(' ');
					}
				
				normalizedString.Append(symbolString, index, symbolString.Length - index);
				}
				
			if (normalizedString.Length > 0 && normalizedString[ normalizedString.Length - 1 ] == SeparatorChar)
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
		 * An array containing the whitespace characters, separator characters, and the first characters of all the possible 
		 * text separators.
		 */
		static private char[] startingSeparatorCharsAndWhitespace = new char[] { ' ', '\t', ':', '-', '.', 
																																			  SeparatorChars.Level1, SeparatorChars.Level2,
																																			  SeparatorChars.Level3, SeparatorChars.Level4,
																																			  SeparatorChars.Escape };

		}
	}