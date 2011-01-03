/* 
 * Class: GregValure.NaturalDocs.Engine.Tokenization.Tokenizer
 * ____________________________________________________________________________
 * 
 * A class for dividing a block of text into easily navigable tokens.  See <TokenType> for a description of
 * how they are divided up by default.
 * 
 * 
 * Usage:
 * 
 *		- <Config.Manager> must be started before use because it depends on it for tab expansion.
 *		
 * 
 * Multithreading: Not Thread Safe, Doesn't Support Reader/Writer
 * 
 *		This class is NOT thread safe, not even with an external reader/writer lock because line information
 *		is generated on demand during reads.  Multiple iterators can be used on the same tokenizer but
 *		they must all be in the same thread.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace GregValure.NaturalDocs.Engine.Tokenization
	{
	public class Tokenizer
		{
				
		// Group: Functions
		// __________________________________________________________________________

		
		/* Function: Tokenizer
		 * Creates a tokenizer from the passed string.
		 */
		public Tokenizer (string input)
			{
			rawText = null;
			tokens = null;
			lines = null;
			startingLineNumber = 1;
			
			Load(input, 1);
			}
			
			
		/* Function: CreateFromIterators
		 * Creates a new tokenizer from the range between two <LineIterators>.  The line the ending iterator is
		 * on is not included in the range.  The new tokenizer has a copy of the memory and is thus independent.  This
		 * is faster than creating a new tokenizer around a substring of the raw text because it doesn't need to be
		 * tokenized all over again.
		 */
		public Tokenizer CreateFromIterators (LineIterator start, LineIterator end)
		    {
		    // We don't just pass it off to CreateFromIterators(TokenIterator) because we don't want to regenerate the
		    // lines array.
		    
			if (!start.IsInBounds || start > end)
				{  throw new ArgumentOutOfRangeException();  }
			
			// If end is out of bounds it's return values will be one past the end of the data.	
			string newRawText = rawText.Substring(start.RawTextIndex, end.RawTextIndex - start.RawTextIndex);
			List<Token> newTokens = tokens.GetRange(start.TokenIndex, end.TokenIndex - start.TokenIndex);
			List<Line> newLines = lines.GetRange(start.LineIndex, end.LineIndex - start.LineIndex);
			
			return new Tokenizer(newRawText, newTokens, newLines, start.LineNumber);
		    }


		/* Function: CreateFromIterators
		 * Creates a new tokenizer from the range between two <TokenIterators>.  The token the ending iterator is
		 * on is not included in the range.  The new tokenizer has a copy of the memory and is thus independent.  This
		 * is faster than creating a new tokenizer around a substring of the raw text because it doesn't need to be
		 * tokenized all over again.
		 */
		public Tokenizer CreateFromIterators (TokenIterator start, TokenIterator end)
			{
			if (!start.IsInBounds || start > end)
				{  throw new ArgumentOutOfRangeException();  }
			
			// If end is out of bounds it's return values will be one past the end of the data.	
			string newRawText = rawText.Substring(start.RawTextIndex, end.RawTextIndex - start.RawTextIndex);
			List<Token> newTokens = tokens.GetRange(start.TokenIndex, end.TokenIndex - start.TokenIndex);
			
			return new Tokenizer(newRawText, newTokens, null, start.LineNumber);
			}


		/* Function: TextBetween
		 * Returns the text between the two passed iterators.  If you plan to add it to a StringBuilder, it is more efficient to call
		 * <AppendTextBetweenTo()> instead because that won't require the creation of an intermediate string.
		 */
		public string TextBetween (TokenIterator start, TokenIterator end)
			{
			if (start.Tokenizer != this || end.Tokenizer != this || start.RawTextIndex >= end.RawTextIndex)
				{  throw new InvalidOperationException();  }

			return rawText.Substring(start.RawTextIndex, end.RawTextIndex - start.RawTextIndex);
			}


		/* Function: AppendTextBetweenTo
		 * Appends the text between the two passed iterators to the passed StringBuilder.  This is more effecient than appending
		 * the result from <TextBetween()> because it transfers directly from the raw text to the StringBuilder without creating 
		 * an intermediate string.
		 */
		public void AppendTextBetweenTo (TokenIterator start, TokenIterator end, System.Text.StringBuilder output)
			{
			if (start.Tokenizer != this || end.Tokenizer != this || start.RawTextIndex >= end.RawTextIndex)
				{  throw new InvalidOperationException();  }

			output.Append(rawText, start.RawTextIndex, end.RawTextIndex - start.RawTextIndex);
			}
			
			
		/* Function: MatchTextBetween
		 * Runs the passed regular expression on the text between the two iterators and returns the result.
		 */
		public Match MatchTextBetween (System.Text.RegularExpressions.Regex regex, TokenIterator start, TokenIterator end)
			{
			if (start.Tokenizer != this || end.Tokenizer != this || start.RawTextIndex >= end.RawTextIndex)
				{  throw new InvalidOperationException();  }

			return regex.Match(rawText, start.RawTextIndex, end.RawTextIndex - start.RawTextIndex);
			}
			
			
		/* Function: ContainsTextBetween
		 * Returns whether the text between the two iterators contains the passed string.
		 */
		public bool ContainsTextBetween (string searchText, bool ignoreCase, TokenIterator start, TokenIterator end)
			{
			if (start.Tokenizer != this || end.Tokenizer != this || start.RawTextIndex >= end.RawTextIndex)
				{  throw new InvalidOperationException();  }

			StringComparison compareMode = (ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
			return (rawText.IndexOf(searchText, start.RawTextIndex, end.RawTextIndex - start.RawTextIndex, compareMode) != -1);
			}
			
			
			
		// Group: Static Functions
		// __________________________________________________________________________
		
		
		/* Function: FundamentalTypeOf
		 * Returns the fundamental <TokenType> of the passed character.
		 */
		static public TokenType FundamentalTypeOf (char character)
			{
			// DEPENDENCY: If this changes, Load() must also change.
			
			char maskedCharacter = (char)(character & 0xFFDF);  // Converts a-z to A-Z
				
			if ( (maskedCharacter >= 'A' && maskedCharacter <= 'Z') ||
				 (character >= '0' && character <= '9') ||
				 character > 0x007F )  // Beyond ASCII
				{
				return TokenType.Text;
				}
					
			else if (character == ' ' || character == '\t')
				{
				return TokenType.Whitespace;
				}
					
			else if (character == '\n' || character == '\r')
				{
				return TokenType.LineBreak;
				}
					
			else
				{
				return TokenType.Symbol;
				}
			}
			
			
			
		// Group: Protected/Internal Functions
		// __________________________________________________________________________


		/* Function: Tokenizer
		 */
		protected Tokenizer (string newRawText, List<Token> newTokens, List<Line> newLines,
									   int newStartingLineNumber)
			{
			rawText = newRawText;
			tokens = newTokens;
			lines = newLines;
			startingLineNumber = newStartingLineNumber;
			}
			
			
		/* Function: Load
		 * Loads and tokenizes the passed string.
		 */
		protected void Load (string input, int newStartingLineNumber)
			{
			rawText = input;
			// Random guess, almost definitely too low, but that's better than being too high.  Will still be closer than the default.
			tokens = new List<Token>(8 + (input.Length / 20));
			lines = null;
			startingLineNumber = newStartingLineNumber;
			
			Token token;
			
			int index = 0;
			char character;
			char maskedCharacter;
			
			while (index < rawText.Length)
				{
				character = rawText[index];
				
				// DEPENDENCY: If this changes, FundamentalTypeOf() must also change.
				
				// Text
				
				maskedCharacter = (char)(character & 0xFFDF);  // Converts a-z to A-Z
				
				if ( (maskedCharacter >= 'A' && maskedCharacter <= 'Z') ||
					 (character >= '0' && character <= '9') ||
					 character > 0x007F )  // Beyond ASCII
					{
					token.Type = TokenType.Text;
					token.Length = 1;
					index++;
					
					while (index < rawText.Length && token.Length < 255)
						{
						character = rawText[index];
						maskedCharacter = (char)(character & 0xFFDF);
						
						if ( (maskedCharacter >= 'A' && maskedCharacter <= 'Z') ||
							 (character >= '0' && character <= '9') ||
							 character > 0x007F )
							{
							token.Length++;
							index++;
							}
						else
							{  break;  }
						}
						
					tokens.Add(token);
					}
					
					
				// Whitespace
				
				else if (character == ' ' || character == '\t')
					{
					token.Type = TokenType.Whitespace;
					token.Length = 1;
					index++;
					
					while (index < rawText.Length && token.Length < 255)
						{
						character = rawText[index];
						
						if (character == ' ' || character == '\t')
							{
							token.Length++;
							index++;
							}
						else
							{  break;  }
						}
						
					tokens.Add(token);
					}
					
					
				// Line break
				
				else if (character == '\n' || character == '\r')
					{
					token.Type = TokenType.LineBreak;
					token.Length = 1;
					index++;
					
					if (index < rawText.Length && character == '\r' && rawText[index] == '\n')
						{
						token.Length++;
						index++;
						}
						
					tokens.Add(token);
					}
					
					
				// Symbols
				
				else
					{
					token.Type = TokenType.Symbol;
					token.Length = 1;
					index++;
					
					tokens.Add(token);
					}
				}
			}


		/* Function: CalculateLines
		 * Calculates the <Lines> list for the current tokens.
		 */
		protected void CalculateLines ()
			{
			// Random guess, almost definitely too low, but that's better than being too high.  Will still be closer than the default.
  			lines = new List<Line>(4 + (rawText.Length / 60));
				
			Line line;
			int tokenIndex = 0;
			TokenType previousType = TokenType.Null;
			
			while (tokenIndex < tokens.Count)
				{
				line.TokenLength = 0;
				line.RawTextLength = 0;

				do
					{
					line.TokenLength++;
					line.RawTextLength += tokens[tokenIndex].Length;
					previousType = tokens[tokenIndex].Type;

					tokenIndex++;
					}
				while (tokenIndex < tokens.Count && previousType != TokenType.LineBreak);
					
				lines.Add(line);
				}
			}
			
			
			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: RawText
		 * The raw, unadulterated input string.
		 */
		public string RawText
			{
			get
				{  return rawText;  }
			}
			
			
		/* Property: StartingLineNumber
		 * The starting line number for the tokenized text.
		 */
		public int StartingLineNumber
			{
			get
				{  return startingLineNumber;  }
			}
			
			
		/* Function: FirstToken
		 * A <TokenIterator> set to the first token of this object.
		 */
		public TokenIterator FirstToken
			{
			get
				{  return new TokenIterator(this, 0, 0, StartingLineNumber);  }
			}
			
		/* Function: FirstLine
		 * A <LineIterator> set to the first line in this object.
		 */
		public LineIterator FirstLine
			{
			get
				{  return new LineIterator(this, 0, 0, 0);  }
			}


			
		// Group: Protected/Internal Properties
		// __________________________________________________________________________
		
			
		/* Property: Tokens
		 * The list of tokens.
		 */
		protected internal IList<Token> Tokens
			{
			get
				{  return tokens;  }
			}
			
		/* Property: Lines
		 * The list of <Lines>.
		 */
		protected internal IList<Line> Lines
			{
			get
				{
				if (lines == null)
					{  CalculateLines();  }
					
				return lines;
				}
			}
			
			
			
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: rawText
		 * The raw, unadulterated input text.
		 */
		protected string rawText;
		
		/* var: tokens
		 * The list of <Tokens> generated for <rawText>.
		 */
		protected List<Token> tokens;
		
		/* var: lines
		 * The list of <Lines> generated for <rawText>.  Unlike <tokens>, this is generated on demand
		 * so this variable will be null if it hasn't been done yet.
		 */
		protected List<Line> lines;
		
		/* var: startingLineNumber
		 */
		protected int startingLineNumber;
		}
	}