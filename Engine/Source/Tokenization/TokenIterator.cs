/* 
 * Struct: GregValure.NaturalDocs.Engine.Tokenization.TokenIterator
 * ____________________________________________________________________________
 * 
 * An iterator for efficiently walking through the tokens in <Tokenizer> while keeping track of the line number
 * and offset into the raw text.
 * 
 * It is designed to be tolerant to allow for easier parsing.  You can go past the bounds of the data without 
 * exceptions being thrown.
 * 
 * It is a struct rather than a class because it is expected that many of them are going to be created, copied, passed
 * around, and then disappear just as quickly.  It's not worth the memory churn to be a reference type, and having
 * them behave as a value type is more intuitive.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace GregValure.NaturalDocs.Engine.Tokenization
	{
	public struct TokenIterator
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Next (count)
		 * Moves forward the passed number of tokens, returning false if we're past the last token.
		 */
		public bool Next (int count = 1)
			{
			if (count < 0)
				{  throw new InvalidOperationException();  }
				
			if (tokenIndex < 0)
				{
				if (tokenIndex + count <= 0)
					{  
					tokenIndex += count;
					return false;
					}
				else
					{
					count += tokenIndex;
					tokenIndex = 0;
					}
				}

			while (count > 0 && tokenIndex < tokenizer.TokenCount)
				{
				if (FundamentalType == FundamentalType.LineBreak)
					{  lineNumber++;  }
					
				rawTextIndex += tokenizer.TokenLengths[tokenIndex];
				tokenIndex++;

				count--;
				}
				
			if (count > 0)
				{
				// Finish advancing regardless of whether we're past the end or not, so if we go past six we must go back six
				// to get into valid territory.
				tokenIndex += count;
				}
			
			return (tokenIndex < tokenizer.TokenCount);
			}
			
			
		/* Function: NextByCharacters
		 * 
		 * Moves forward by the passed number of characters, returning false if we're past the last token.
		 * 
		 * This throws an exception if advancing by the passed number of characters would cause the iterator to not fall
		 * evenly on a token boundary.  It is assumed that this function will primarily be used after a positive result from 
		 * <MatchesAcrossTokens()> or <TokensInCharacters()> which would cause this to not be an issue.
		 */
		public bool NextByCharacters (int characters)
			{
			int tokensInCharacters = TokensInCharacters(characters);
			
			if (tokensInCharacters == -1)
				{  throw new InvalidOperationException();  }
				
			return Next(tokensInCharacters);
			}
			
			
		/* Function: Previous (count)
		 * Moves backwards the passed number of tokens, returning false if we're past the first token.
		 */
		public bool Previous (int count = 1)
			{
			if (count < 0)
				{  throw new InvalidOperationException();  }
				
			if (tokenIndex > tokenizer.TokenCount)
				{
				if (tokenIndex - count >= tokenizer.TokenCount)
					{
					tokenIndex -= count;
					return false;
					}
				else
					{
					count -= tokenIndex - tokenizer.TokenCount;
					tokenIndex = tokenizer.TokenCount;
					}
				}

			while (count > 0 && tokenIndex > 0)
				{			
				tokenIndex--;
				rawTextIndex -= tokenizer.TokenLengths[tokenIndex];
			
				if (FundamentalType == FundamentalType.LineBreak)
					{  lineNumber--;  }
					
				count--;
				}

			if (count > 0)
				{
				// Finish advancing regardless of whether we're past the beginning or not, so if we go past six we must go 
				// forward six to get into valid territory.
				tokenIndex -= count;
				}
				
			return (tokenIndex >= 0);			
			}
			
			
		/* Function: PreviousByCharacters
		 * 
		 * Moves backwards by the passed number of characters, returning false if we're past the first token.
		 * 
		 * This throws an exception if backing up by the passed number of characters would cause the iterator to not 
		 * fall evenly on a token boundary.  It is assumed that this function will primarily be used after a positive result 
		 * from <MatchesAcrossTokens()> or <TokensInPreviousCharacters()> which would cause this to not be an issue.
		 */
		public bool PreviousByCharacters (int characters)
			{
			int tokensInCharacters = TokensInPreviousCharacters(characters);
			
			if (tokensInCharacters == -1)
				{  throw new InvalidOperationException();  }
				
			return Previous(tokensInCharacters);
			}
			
			
		/* Function: TokensInCharacters
		 * Returns the number of tokens between the current position and the passed number of characters.  If advancing
		 * by the character count would not land on a token boundary this returns -1.
		 */
		public int TokensInCharacters (int characterCount)
			{
			if (!IsInBounds)
				{  return -1;  }
				
			int i = tokenIndex;
			int tokenCount = 0;
			
			while (characterCount > 0 && i < tokenizer.TokenCount)
				{
				characterCount -= tokenizer.TokenLengths[i];
				i++;
				tokenCount++;
				}
				
			if (characterCount == 0)  // i landing one past the last token is okay
				{  return tokenCount;  }
			else
				{  return -1;  }
			}
			
			
		/* Function: TokensInPreviousCharacters
		 * Returns the number of tokens between the current position and the passed number of characters before it.  If 
		 * going backwards by the character count would not land on a token boundary this returns -1.
		 */
		public int TokensInPreviousCharacters (int characterCount)
			{
			// We want to accept if the iterator is one past the last token
			if (!IsInBounds && tokenIndex != tokenizer.TokenCount)
				{  return -1;  }
				
			int i = tokenIndex;
			int tokenCount = 0;
			
			while (characterCount > 0 && i > 0)
				{
				i--;
				characterCount -= tokenizer.TokenLengths[i];
				tokenCount++;
				}
				
			if (characterCount == 0)
				{  return tokenCount;  }
			else
				{  return -1;  }
			}
			
			
		/* Function: MatchesToken
		 * Returns whether the passed string matches the current token.  The string must match the entire
		 * token, so "some" won't match "something".  Returns false if the iterator is out of bounds.
		 */
		public bool MatchesToken (string text, bool ignoreCase = false)
			{
			if (!IsInBounds)
				{  return false;  }
				
			return ( text.Length == tokenizer.TokenLengths[tokenIndex] &&
							String.Compare(tokenizer.RawText, rawTextIndex, text, 0, text.Length, ignoreCase) == 0 );
			}
			
			
		/* Function: MatchesToken
		 * Applies a regular expression to the token and returns the Match object as if Regex.Match() was called.  If
		 * the iterator is out of bounds it will be applied to an empty string.
		 */
		public Match MatchesToken (System.Text.RegularExpressions.Regex regex)
			{
			if (!IsInBounds)
				{  return regex.Match("");  }
		
			return regex.Match(tokenizer.RawText, rawTextIndex, tokenizer.TokenLengths[tokenIndex]);
			}


		/* Function: MatchesAcrossTokens
		 * Returns whether the passed string matches the tokens at the current position.  The string comparison can 
		 * span multiple tokens, which allows you to test against things like "//" which would be two tokens.  However,
		 * the string must still match complete tokens so "// some" won't match "// something".  Returns false if the 
		 * iterator is out of bounds.
		 */
		public bool MatchesAcrossTokens (string text, bool ignoreCase = false)
			{
			if (!IsInBounds)
				{  return false;  }
				
			return ( TokensInCharacters(text.Length) != -1 &&
							String.Compare(tokenizer.RawText, rawTextIndex, text, 0, text.Length, ignoreCase) == 0 );
			}


		/* Function: MatchesAnyAcrossTokens
		 * Determines whether any of the passed strings match the tokens at the current position, returning the match's
		 * array index if true or -1 if not.  The string comparison can span multiple tokens, which allows you to test 
		 * against things like "//" which would be two tokens.  However, the string must still match complete tokens so 
		 * "// some" won't match "// something".
		 */
		public int MatchesAnyAcrossTokens (IList<string> text, bool ignoreCase = false)
			{
			if (!IsInBounds)
				{  return -1;  }
				
			for (int i = 0; i < text.Count; i++)
				{
				// We do this instead of just passing each string to MatchesAcrossTokens so we don't have to do a bounds
				// check for every iteration.
				if (TokensInCharacters(text[i].Length) != -1 &&
					 String.Compare(tokenizer.RawText, rawTextIndex, text[i], 0, text[i].Length, ignoreCase) == 0)
					{  return i;  }
				}

			return -1;
			}


		/* Function: MatchesAnyPairAcrossTokens
		 * Determines whether any of the passed string pairs match the tokens at the current position, returning the match's
		 * array index if true or -1 if not.  Only the first of each pair are tested against the current position.  The string 
		 * comparison can span multiple tokens, which allows you to test against things like "/*" which would be two tokens.  
		 * However, the string must still match complete tokens so "/* some" won't match "/* something".
		 */
		public int MatchesAnyPairAcrossTokens (IList<string> text, bool ignoreCase = false)
			{
			if (!IsInBounds)
				{  return -1;  }
			if (text.Count % 2 != 0)
				{  throw new Exceptions.ArrayDidntHaveEvenLength("symbol pairs");  }
				
			for (int i = 0; i < text.Count; i += 2)
				{
				if (TokensInCharacters(text[i].Length) != -1 &&
					 String.Compare(tokenizer.RawText, rawTextIndex, text[i], 0, text[i].Length, ignoreCase) == 0)
					{  return i;  }
				}

			return -1;
			}


		/* Function: AppendTokenTo
		 * Appends the token to the passed StringBuilder.  This is more efficient than appending the result of <String>
		 * because it copies directly from the source without creating an intermediate string.
		 */
		public void AppendTokenTo (System.Text.StringBuilder output)
			{
			int length = RawTextLength;  // Will be zero if out of bounds.

			if (length == 1)
				{  output.Append(tokenizer.RawText[rawTextIndex]);  }
			else if (length > 1)
				{  output.Append(tokenizer.RawText, rawTextIndex, length);  }
			}
			
			
		/* Function: SetCommentParsingTypeByCharacters
		 * 
		 * Changes the <CommentParsingType> of the tokens encompassed by the passed number of characters.
		 * 
		 * This throws an exception if the number of characters does not evenly fall on a token boundary.  It
		 * is assumed that this function will primarily be used after a positive result from <MatchesAcrossTokens()>
		 * or <TokensInCharacters()> which would cause this to not be an issue.
		 */
		public void SetCommentParsingTypeByCharacters (CommentParsingType newType, int characters)
			{
			int tokenCount = TokensInCharacters(characters);
			
			if (tokenCount == -1)
				{  throw new InvalidOperationException();  }
			else if (tokenCount == 1)
				{  CommentParsingType = newType;  }
			else
				{
				TokenIterator iterator = this;
				
				while (tokenCount > 0)
					{
					iterator.CommentParsingType = newType;
					iterator.Next();
					tokenCount--;
					}
				}
			}

			
		/* Function: SetSyntaxHighlightingTypeByCharacters
		 * 
		 * Changes the <SyntaxHighlightingType> of the tokens encompassed by the passed number of characters.
		 * 
		 * This throws an exception if the number of characters does not evenly fall on a token boundary.  It
		 * is assumed that this function will primarily be used after a positive result from <MatchesAcrossTokens()>
		 * or <TokensInCharacters()> which would cause this to not be an issue.
		 */
		public void SetSyntaxHighlightingTypeByCharacters (SyntaxHighlightingType newType, int characters)
			{
			int tokenCount = TokensInCharacters(characters);
			
			if (tokenCount == -1)
				{  throw new InvalidOperationException();  }
			else if (tokenCount == 1)
				{  SyntaxHighlightingType = newType;  }
			else
				{
				TokenIterator iterator = this;
				
				while (tokenCount > 0)
					{
					iterator.SyntaxHighlightingType = newType;
					iterator.Next();
					tokenCount--;
					}
				}
			}

			
			
			
		// Group: Protected/Internal Functions
		// __________________________________________________________________________
		
		
		/* Function: TokenIterator
		 * Creates a new iterator with the passed variables.
		 */
		internal TokenIterator (Tokenizer newTokenizer, int newTokenIndex, int newRawTextIndex, int newLineNumber)	
			{
			tokenizer = newTokenizer;
			tokenIndex = newTokenIndex;
			rawTextIndex = newRawTextIndex;
			lineNumber = newLineNumber;
			}

			
			
			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: String
		 * Returns the token as a string, or an empty string if it's out of bounds.  Note that this allocates memory and 
		 * creates a copy of the string.  Whenever possible use functions like <MatchesToken()> and <AppendTokenTo()> to 
		 * work directly on the original memory, or use <RawTextIndex> and <RawTextLength> with <Tokenizer.RawText> 
		 * to access it yourself.
		 */
		public string String
			{
			get
				{
				if (IsInBounds)
					{  return tokenizer.RawText.Substring( rawTextIndex, tokenizer.TokenLengths[tokenIndex] );  }
				else
					{  return "";  }
				}
			}
			
		/* Property: Character
		 * The first character of the token, or null if it's out of bounds.  This is useful for symbol tokens which will always be
		 * only one character long.
		 */
		public char Character
			{
			get
				{
				if (IsInBounds)
					{  return tokenizer.RawText[rawTextIndex];  }
				else
					{  return '\0';  }
				}
			}
			
		/* Property: FundamentalType
		 * The <FundamentalType> of the current token, or <FundamentalType.Null> if the iterator is out of bounds.  It 
		 * cannot be changed.
		 */
		public FundamentalType FundamentalType
			{
			get
				{  return tokenizer.FundamentalTypeAt(tokenIndex, rawTextIndex);  }
			}

		/* Property: CommentParsingType
		 * The <CommentParsingType> of the current token, or <CommentParsingType.Null> if it hasn't been set or the
		 * iterator is out of bounds.
		 */
		public CommentParsingType CommentParsingType
			{
			get
				{  return tokenizer.CommentParsingTypeAt(tokenIndex);  }
			set
				{  tokenizer.SetCommentParsingTypeAt(tokenIndex, value);  }
			}
			
		/* Property: SyntaxHighlightingType
		 * The <SyntaxHighlightingType> of the current token, or <SyntaxHighlightingType.Null> if it hasn't been set or the
		 * iterator is out of bounds.
		 */
		public SyntaxHighlightingType SyntaxHighlightingType
			{
			get
				{  return tokenizer.SyntaxHighlightingTypeAt(tokenIndex);  }
			set
				{  tokenizer.SetSyntaxHighlightingTypeAt(tokenIndex, value);  }
			}
			
		/* Property: LineNumber
		 * The line number of the current token, or the one it left off on if it went out of bounds.
		 */
		public int LineNumber
			{
			get
				{  return lineNumber;  }
			}
			
		/* Property: RawTextIndex
		 * The offset of the current token into <Tokenizer.RawText>.  Will be zero if it went past the beginning, or the index 
		 * one past the last character if it went past the end.
		 */
		public int RawTextIndex
			{
			get
				{  return rawTextIndex;  }
			}
			
		/* Property: RawTextLength
		 * The length of the current token in characters, or zero if the iterator is out of bounds.
		 */
		public int RawTextLength
			{
			get
				{
				if (IsInBounds)
					{  return tokenizer.TokenLengths[tokenIndex];  }
				else
					{  return 0;  }
				}
			}
			
		/* Property: IsInBounds
		 * Whether the iterator is not past the beginning or end of the list of tokens.
		 */
		public bool IsInBounds
			{
			get
				{  return (tokenIndex >= 0 && tokenIndex < tokenizer.TokenCount);  }
			}
			
		/* Property: Tokenizer
		 * The <Tokenizer> associated with this iterator.
		 */
		public Tokenizer Tokenizer
			{
			get
				{  return tokenizer;  }
			}

			
			
		// Group: Internal Properties
		// __________________________________________________________________________
		
			
		/* Property: TokenIndex
		 * The current index into <Tokenizer.Tokens>.
		 */
		internal int TokenIndex
			{
			get
				{  return tokenIndex;  }
			}
			
			
			
		// Group: Operators
		// __________________________________________________________________________


		public override bool Equals (object other)
			{
			// Since it's a struct, it will never equal an object.
			return false;
			}

		public override int GetHashCode ()
			{
			return tokenIndex.GetHashCode();
			}
			
		public static bool operator== (TokenIterator a, TokenIterator b)
			{
			return (a.tokenizer == b.tokenizer && a.tokenIndex == b.tokenIndex);
			}
			
		public static bool operator!= (TokenIterator a, TokenIterator b)
			{
			return !(a == b);
			}
			
		public static bool operator> (TokenIterator a, TokenIterator b)
			{
			if (a.tokenizer == null || b.tokenizer == null)
				{  throw new NullReferenceException();  }
			if (a.tokenizer != b.tokenizer)
				{  throw new Engine.Exceptions.RelativeCompareOfIteratorsNotOnSameBase();  }
				
			return (a.tokenIndex > b.tokenIndex);
			}
			
		public static bool operator>= (TokenIterator a, TokenIterator b)
			{
			if (a.tokenizer == null || b.tokenizer == null)
				{  throw new NullReferenceException();  }
			if (a.tokenizer != b.tokenizer)
				{  throw new Engine.Exceptions.RelativeCompareOfIteratorsNotOnSameBase();  }
				
			return (a.tokenIndex >= b.tokenIndex);
			}
			
		public static bool operator< (TokenIterator a, TokenIterator b)
			{
			return !(a >= b);
			}
			
		public static bool operator<= (TokenIterator a, TokenIterator b)
			{
			return !(a > b);
			}

			
			
			
		// Group: Variables
		// __________________________________________________________________________
		
		/* var: tokenizer
		 * The <Tokenizer> associated with this iterator.
		 */
		private Tokenizer tokenizer;
		
		/* var: tokenIndex
		 * The current index into the tokens.  Can be a negative number if we're before the first token.
		 */
		private int tokenIndex;
		
		/* var: rawTextIndex
		 * The current index into <Tokenizer.RawText>.
		 */
		private int rawTextIndex;
		
		/* var: lineNumber
		 * The current line number.  Lines start at one instead of zero.
		 */
		private int lineNumber;
	
		}
	}