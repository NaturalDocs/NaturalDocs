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
		 * If the string ends in parentheses they will be separated off from the string and returned in the parentheses variable.  
		 * They will not be part of the resulting SymbolString.  The string is still in its raw form so to become a <ParameterString> it
		 * would need to be passed to <ParameterString.FromParenthesesString()>.  If there's no parentheses the variable will be null.
		 * 
		 * The string will be normalized.  If you know the string is already in a normalized form because it originally came 
		 * from another SymbolString object, use <FromExportedString()>.
		 */
		static public SymbolString FromPlainText (string textSymbol, out string parentheses)
			{
			if (textSymbol == null)
				{  throw new NullReferenceException();  }

			string undecoratedTextSymbol;
			ParameterString.SplitFromEndingParentheses(textSymbol, out undecoratedTextSymbol, out parentheses);

			SymbolString symbolString = new SymbolString(undecoratedTextSymbol);
			symbolString.Normalize();

			// If a symbol string is normalized to nothing yet it had parentheses (think "::()") put them back together and redo.
			// This should be a rare edge case but we want to handle it.  We never want a null symbol string with a valid 
			// parentheses string.
			if (symbolString.symbolString == null && parentheses != null)
				{
				symbolString = new SymbolString(textSymbol);
				symbolString.Normalize();

				parentheses = null;
				}
			
			return symbolString;
			}

			
		/* Function: FromPlainText_ParenthesesAlreadyRemoved
		 * 
		 * Creates a SymbolString from the passed string of plain text where the parentheses have already been removed.
		 * 
		 * We use this awkward function name because 90% of the time you need to handle parentheses, or at least strip them
		 * off.  If we just made an overload of <FromPlainText()> without the out parameter people would use this one by accident.
		 * By attaching _ParenthesesAlreadyRemoved it forces you to only use this one if you know what you're doing.
		 * 
		 * The string will be normalized.  If you know the string is already in a normalized form because it originally came 
		 * from another SymbolString object, use <FromExportedString()>.
		 */
		static public SymbolString FromPlainText_ParenthesesAlreadyRemoved (string textSymbol)
			{
			if (textSymbol == null)
				{  throw new NullReferenceException();  }

			SymbolString symbolString = new SymbolString(textSymbol);
			symbolString.Normalize();

			return symbolString;
			}

			
		/* Function: FromExportedString
		 * Creates a SymbolString from the passed string which originally came from another SymbolString object.  This skips 
		 * the normalization stage because it should already be in the proper format.  Only use this when retrieving SymbolStrings
		 * that were stored as plain text in a database or other data file.  All other uses should call <FromPlainText()> instead.
		 */
		static public SymbolString FromExportedString (string exportedSymbolString)
			{
			if (exportedSymbolString != null && exportedSymbolString.Length == 0)
				{  exportedSymbolString = null;  }

			return new SymbolString(exportedSymbolString);
			}


		/* Function: SplitSegments
		 * Returns the symbol as an array of individual segments.
		 */
		public string[] SplitSegments ()
			{
			if (symbolString == null)
				{  return new string[0];  }
			else
				{  return symbolString.Split(SeparatorChar);  }
			}
			
		
		/* Function: FormatWithSeparator
		 * Returns the symbol as a string using the passed separator character.
		 */
		public string FormatWithSeparator (char newSeparator)
			{
			if (symbolString == null)
				{  return null;  }
			else
				{  return symbolString.Replace(SeparatorChar, newSeparator);  }
			}
			
		
		/* Function: FormatWithSeparator
		 * Returns the symbol as a string using the passed separator string.
		 */
		public string FormatWithSeparator (string newSeparator)
			{
			if (symbolString == null)
				{  return null;  }
			else
				{  return symbolString.Replace(SeparatorChar.ToString(), newSeparator);  }
			}


		/* Function: StartsWith
		 * Returns whether the start of the symbol matches the passed symbol, such as "PackageA.PackageB.Function" and PackageA.PackageB".
		 * It must match a complete segment, so "PackageA.PackageB.Function" will not match "PackageA.Package".
		 */
		public bool StartsWith (SymbolString other, bool ignoreCase = false)
			{
			return (symbolString.Length > other.symbolString.Length &&
					  symbolString[other.symbolString.Length] == SeparatorChar &&
					  symbolString.StartsWith(other.symbolString, ignoreCase, System.Globalization.CultureInfo.CurrentCulture));
			}
			
		
		/* Function: EndsWith
		 * Returns whether the end of the symbol matches the passed symbol, such as "PackageA.PackageB.Function" and "PackageB.Function".
		 * It must match a complete segment, so "PackageA.PackageB.Function" will not match "B.Function".
		 */
		public bool EndsWith (SymbolString other, bool ignoreCase = false)
			{
			return (symbolString.Length > other.symbolString.Length &&
					  symbolString[symbolString.Length - other.symbolString.Length - 1] == SeparatorChar &&
					  symbolString.EndsWith(other.symbolString, ignoreCase, System.Globalization.CultureInfo.CurrentCulture));
			}
			
		
		
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: EndingSymbol
		 * Returns the <EndingSymbol> of the symbol string.  Unlike <LastSegment> and SymbolStrings in general,
		 * <EndingSymbols> are case-insensitive.  So for "PackageA.PackageB.FunctionC" this will return an 
		 * <EndingSymbol> for "functionc".  
		 */
		public EndingSymbol EndingSymbol
			{
			get
				{  return EndingSymbol.FromSymbolStringSegment(LastSegment);  }
			}
						
			
		/* Property: LastSegment
		 * Returns the last segment of the symbol string.  Unlike <EndingSymbol> this is case sensitive, so for 
		 * "PackageA.PackageB.FunctionC" this will return "FunctionC".
		 */
		public string LastSegment
			{
			get
				{
				if (symbolString == null)
					{  return null;  }

				int lastSeparator = symbolString.LastIndexOf(SeparatorChar);
				
				if (lastSeparator == -1)
					{  return symbolString.ToString();  }
				else
					{  return symbolString.Substring(lastSeparator + 1);  }
				}
			}
						
			
		/* Property: WithoutLastSegment
		 * Returns the symbol without its last segment, which is its parent scope, or null if there is only one segment.
		 * For "PackageA.PackageB.FunctionC" this will return "PackageA.PackageB".
		 */
		public SymbolString WithoutLastSegment
			{
			get
				{
				if (symbolString == null)
					{  return new SymbolString();  }

				int lastSeparator = symbolString.LastIndexOf(SeparatorChar);
				
				if (lastSeparator == -1)
					{  return new SymbolString();  }
				else
					{  return new SymbolString(symbolString.Substring(0, lastSeparator));  }
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
		public int CompareTo (SymbolString other, bool ignoreCase = false)
			{
			return string.Compare(symbolString, other.symbolString, ignoreCase);
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
		 *		- Removes all existing instances of the <SeparatorChars>.
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
																																			  SeparatorChars.Level3, SeparatorChars.Level4 };
		}
	}