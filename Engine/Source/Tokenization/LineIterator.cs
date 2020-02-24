/* 
 * Struct: CodeClear.NaturalDocs.Engine.Tokenization.LineIterator
 * ____________________________________________________________________________
 * 
 * An iterator to go through a <Tokenizer> line by line instead of token by token.
 * 
 * It is designed to be tolerant to allow for easier parsing.  You can go past the bounds of the data without 
 * exceptions being thrown.
 * 
 * It is a struct rather than a class because it is expected that many of them are going to be created, copied, passed
 * around, and then disappear just as quickly.  It's not worth the memory churn to be a reference type, and having
 * them behave as a value type is more intuitive.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Tokenization
	{
	public struct LineIterator
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Next
		 * Moves forward the specified number of lines, returning false if we've moved past the end.  You can use this with
		 * negative numbers to move backwards.
		 */
		public bool Next (int count = 1)
			{
			if (count < 0)
				{  return Previous(-count);  }
				
			if (lineIndex < 0)
				{
				if (lineIndex + count <= 0)
					{  
					lineIndex += count;
					return false;
					}
				else
					{
					count += lineIndex;
					lineIndex = 0;
					}
				}
				
			while (count > 0 && lineIndex < tokenizer.Lines.Count)
				{
				tokenIndex += tokenizer.Lines[lineIndex].TokenLength;
				rawTextIndex += tokenizer.Lines[lineIndex].RawTextLength;
				lineIndex++;
				
				count--;
				}
				
			if (count > 0)
				{  lineIndex += count;  }
				
			return (lineIndex < tokenizer.Lines.Count);
			}
			
			
		/* Function: Previous
		 * Moves backwards the specified number of lines, returning false if we've move past the beginning.  You can use this with
		 * negative numbers to move forward.
		 */
		public bool Previous (int count = 1)
			{
			if (count < 0)
				{  return Next(-count);  }
				
			if (lineIndex > tokenizer.Lines.Count)
				{
				if (lineIndex - count >= tokenizer.Lines.Count)
					{
					lineIndex -= count;
					return false;
					}
				else
					{
					count -= lineIndex - tokenizer.Lines.Count;
					lineIndex = tokenizer.Lines.Count;
					}
				}
				
			while (count > 0 && lineIndex > 0)
				{
				lineIndex--;
				tokenIndex -= tokenizer.Lines[lineIndex].TokenLength;
				rawTextIndex -= tokenizer.Lines[lineIndex].RawTextLength;
				
				count--;
				}
				
			if (count > 0)
				{  lineIndex -= count;  }
			
			return (lineIndex >= 0);
			}
			
			
		/* Function: FirstToken
		 * Returns a <TokenIterator> at the beginning of the current line.  If the iterator is out of bounds it will be 
		 * set to one past the last token, regardless of which edge it has gone off.
		 */
		public TokenIterator FirstToken (LineBoundsMode boundsMode)
			{
			if (!IsInBounds)
				{  
				return new TokenIterator(tokenizer, tokenizer.TokenCount, tokenizer.RawText.Length, 
													tokenizer.StartingLineNumber + tokenizer.Lines.Count - 1);
				}
			else
				{
				int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
				CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);
				
				return new TokenIterator(tokenizer, tokenStart, rawTextStart, tokenizer.StartingLineNumber + lineIndex);
				}
			}
			
			
		/* Function: LastToken
		 * Returns a <TokenIterator> at the end of the current line.  If the iterator is out of bounds it will be 
		 * set to one past the last token, regardless of which edge it has gone off.
		 */
		public TokenIterator LastToken (LineBoundsMode boundsMode)
			{
			if (!IsInBounds)
				{  
				return new TokenIterator(tokenizer, tokenizer.TokenCount, tokenizer.RawText.Length, 
													tokenizer.StartingLineNumber + tokenizer.Lines.Count - 1);
				}
			else
				{
				int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
				CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);
				
				return new TokenIterator(tokenizer, tokenEnd, rawTextEnd, tokenizer.StartingLineNumber + lineIndex + 1);
				}
			}
			
			
		/* Function: GetBounds
		 * Sets two <TokenIterators> to the beginning and end of the current line.  If the iterator is out of bounds they
		 * will be equal.
		 */
		public void GetBounds (LineBoundsMode boundsMode, out TokenIterator lineStart, out TokenIterator lineEnd)
			{
			if (!IsInBounds)
				{  
				lineStart = FirstToken(boundsMode);
				lineEnd = lineStart;
				}
			else
				{
				int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
				CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);
				
				lineStart = new TokenIterator(tokenizer, tokenStart, rawTextStart, tokenizer.StartingLineNumber + lineIndex);
				lineEnd = new TokenIterator(tokenizer, tokenEnd, rawTextEnd, tokenizer.StartingLineNumber + lineIndex);
				}
			}
			
			
		/* Function: GetRawTextBounds
		 * Returns the location of the line in <Tokenizer.RawText>.
		 */
		public void GetRawTextBounds (LineBoundsMode boundsMode, out int lineStartIndex, out int lineEndIndex)
		    {
		    int tokenStart, tokenEnd;
		    CalculateBounds(boundsMode, out lineStartIndex, out lineEndIndex, out tokenStart, out tokenEnd);
		    }


		/* Function: String
		 * Returns the line as a string.  Note that this allocates a copy of the memory.  For efficiency, it's preferrable to work on 
		 * the original memory whenever possible with functions like <Match()> and <AppendTo()>.
		 */
		public string String (LineBoundsMode boundsMode)
			{
			int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
			CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);

			return tokenizer.RawText.Substring(rawTextStart, rawTextEnd - rawTextStart);			
			}
			
			
		/* Function: AppendTo
		 * Appends the line to the passed StringBuilder.  This works from the original memory so it's more efficent than appending
		 * the result from <String()>.
		 */
		public void AppendTo (System.Text.StringBuilder target, LineBoundsMode boundsMode)
			{
			int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
			CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);

			target.Append(tokenizer.RawText, rawTextStart, rawTextEnd - rawTextStart);			
			}
			
			
		/* Function: IsEmpty
		 * Returns whether the current line is empty according to the <LineBoundsMode>.
		 */
		public bool IsEmpty (LineBoundsMode boundsMode)
			{
			int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
			CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);
			
			return (rawTextStart == rawTextEnd);
			}


		/* Function: Indent
		 * Returns the indent of the current line content according to the <LineBoundsMode>, expanding tabs.
		 */
		public int Indent (LineBoundsMode boundsMode)
			{
			int rawTextBoundsStart, rawTextBoundsEnd, tokenBoundsStart, tokenBoundsEnd;
			CalculateBounds(boundsMode, out rawTextBoundsStart, out rawTextBoundsEnd, out tokenBoundsStart, out tokenBoundsEnd);
			
			int indent = 0;
			string rawText = tokenizer.RawText;
			
			for (int i = rawTextIndex; i < rawTextBoundsStart; i++)
				{
				if (rawText[i] == '\t')
					{
					indent += tokenizer.TabWidth;
					indent -= (indent % tokenizer.TabWidth);
					}
				else
					{  indent++;  }
				}
				
			return indent;
			}
		 
		 
		/* Function: Match
		 * Applies a regular expression to the line and returns the Match object as if Regex.Match() was called.  If
		 * the iterator is out of bounds it will be applied to an empty string.
		 */
		public System.Text.RegularExpressions.Match Match (System.Text.RegularExpressions.Regex regex, LineBoundsMode boundsMode)
			{
			if (!IsInBounds)
				{  return regex.Match("");  }
		
			int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
			CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);
			
			return regex.Match(tokenizer.RawText, rawTextStart, rawTextEnd - rawTextStart);
			}
			
			
		/* Function: FindToken
		 * Attempts to find the passed string as a token in the line, and set a <TokenIterator> at its position if successful.  
		 * The string must match the entire token, so "some" will not match "something".
		 */
		public bool FindToken (string text, bool ignoreCase, LineBoundsMode boundsMode, out TokenIterator result)
			{
			TokenIterator acrossTokensResult;
			
			if (FindAcrossTokens(text, ignoreCase, boundsMode, out acrossTokensResult) == false ||
				acrossTokensResult.RawTextLength != text.Length)
				{  
				result = new TokenIterator();
				return false;
				}
			else
				{  
				result = acrossTokensResult;
				return true;
				}
			}
			
			
		/* Function: FindAcrossTokens
		 * Attempts to find the passed string in the line, and sets a <TokenIterator> at its position if successful.  This function 
		 * can cross token boundaries, so you can search for "<<" even though that would normally be two tokens.  The result 
		 * must match complete tokens though, so "<< some" will not match "<< something".
		 */
		public bool FindAcrossTokens (string text, bool ignoreCase, LineBoundsMode boundsMode, out TokenIterator result)
			{
			if (!IsInBounds)
				{  
				result = new TokenIterator();  
				return false;
				}
				
			int rawTextStart, rawTextEnd, tokenStart, tokenEnd;
			CalculateBounds(boundsMode, out rawTextStart, out rawTextEnd, out tokenStart, out tokenEnd);

			int resultIndex = tokenizer.RawText.IndexOf( text, rawTextStart, rawTextEnd - rawTextStart,
																			  (ignoreCase ? StringComparison.CurrentCultureIgnoreCase : 
																								   StringComparison.CurrentCulture) );
																									   
			if (resultIndex == -1)
				{  
				result = new TokenIterator();  
				return false;
				}
				
			result = new TokenIterator(tokenizer, tokenStart, rawTextStart, LineNumber);

			// Do this instead of NextByCharacters() so we don't cause an exception if it's not on a token boundary.
			while (result.RawTextIndex < resultIndex)
				{  result.Next();  }

			if (result.RawTextIndex != resultIndex)
				{  
				result = new TokenIterator();
				return false;
				}
			
			return true;
			}



			
		// Group: Internal Functions
		// __________________________________________________________________________
			

		/* Function: LineIterator
		 * Creates a new iterator from the passed parameters.
		 */
		internal LineIterator (Tokenizer newTokenizer, int newLineIndex, int newTokenIndex, int newRawTextIndex)
			{
			tokenizer = newTokenizer;
			lineIndex = newLineIndex;
			tokenIndex = newTokenIndex;
			rawTextIndex = newRawTextIndex;
			}
			
			
		/* Function: CalculateBounds
		 * Determines and returns the bounds of the current line according to the <LineBoundsMode>.
		 */
		internal void CalculateBounds (LineBoundsMode boundsMode, out int rawTextStart, out int rawTextEnd,
																	out int tokenStart, out int tokenEnd)
			{
			if (!IsInBounds)
				{
				rawTextStart = 0;
				rawTextEnd = 0;
				tokenStart = 0;
				tokenEnd = 0;
				
				return;
				}
				
			rawTextStart = rawTextIndex;
			rawTextEnd = rawTextIndex + tokenizer.Lines[lineIndex].RawTextLength;
			tokenStart = tokenIndex;
			tokenEnd = tokenIndex + tokenizer.Lines[lineIndex].TokenLength;
			
			if (boundsMode == LineBoundsMode.Everything)
				{  return;  }
				
			while (tokenEnd > tokenStart && IsSkippable(tokenEnd - 1, rawTextEnd - tokenizer.TokenLengths[tokenEnd - 1], boundsMode))
				{
				tokenEnd--;
				rawTextEnd -= tokenizer.TokenLengths[tokenEnd];
				}
				
			while (tokenStart < tokenEnd && IsSkippable(tokenStart, rawTextStart, boundsMode))
				{
				rawTextStart += tokenizer.TokenLengths[tokenStart];
				tokenStart++;
				}
			}
			
			
		/* Function: IsSkippable
		 * Returns whether the token at the passed index should be skipped based on the <LineBoundsMode>.
		 */
		internal bool IsSkippable (int testTokenIndex, int testRawTextIndex, LineBoundsMode boundsMode)
			{
			if (boundsMode == LineBoundsMode.Everything)
				{  return false;  }

			FundamentalType fundamentalType = tokenizer.FundamentalTypeAt(testTokenIndex, testRawTextIndex);

			// Whitespace is skippable for both ExcludeWhitespace and CommentContent
			if (fundamentalType == FundamentalType.Whitespace || 
				 fundamentalType == FundamentalType.LineBreak)
				{  return true;  }

			if (boundsMode == LineBoundsMode.ExcludeWhitespace)
				{  return false;  }

			// The only other choice is CommentContent

			CommentParsingType commentParsingType = tokenizer.CommentParsingTypeAt(testTokenIndex);

			return (commentParsingType == CommentParsingType.CommentSymbol || 
						  commentParsingType == CommentParsingType.CommentDecoration);
			}
			

			
			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: LineNumber
		 */
		public int LineNumber
			{
			get
				{  return (tokenizer.StartingLineNumber + lineIndex);  }
			}
			
			
		/* Property: IsInBounds
		 * Whether the iterator is not past the beginning or end of the tokens.
		 */
		public bool IsInBounds
			{
			get
				{  return (lineIndex >= 0 && lineIndex < tokenizer.Lines.Count);  }
			}
			
			
		/* Property: Tokenizer
		 * The <Tokenizer> associated with this iterator.
		 */
		public Tokenizer Tokenizer
			{
			get
				{  return tokenizer;  }
			}
			

			
		// Group: Internal and Private Properties
		// __________________________________________________________________________
		
		
		/* Property: RawTextIndex
		 * The index into <Tokenizer.RawText> of the beginning of the current line.
		 */
		internal int RawTextIndex
			{
			get
				{  return rawTextIndex;  }
			}
			
			
		/* Property: TokenIndex
		 * The token index of the beginning of the current line.
		 */
		internal int TokenIndex
			{
			get
				{  return tokenIndex;  }
			}
			
			
		/* Property: LineIndex
		 * The current line's index into <Tokenizer.Lines>.
		 */
		internal int LineIndex
			{
			get
				{  return lineIndex;  }
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
			return lineIndex.GetHashCode();
			}

		public static bool operator== (LineIterator a, LineIterator b)
			{
			return (a.tokenizer == b.tokenizer && a.lineIndex == b.lineIndex);
			}
			
		public static bool operator!= (LineIterator a, LineIterator b)
			{
			return !(a == b);
			}
			
		public static bool operator> (LineIterator a, LineIterator b)
			{
			if (a.tokenizer == null || b.tokenizer == null)
				{  throw new NullReferenceException();  }
			if (a.tokenizer != b.tokenizer)
				{  throw new Engine.Exceptions.RelativeCompareOfIteratorsNotOnSameBase();  }
				
			return (a.lineIndex > b.lineIndex);
			}
			
		public static bool operator>= (LineIterator a, LineIterator b)
			{
			if (a.tokenizer == null || b.tokenizer == null)
				{  throw new NullReferenceException();  }
			if (a.tokenizer != b.tokenizer)
				{  throw new Engine.Exceptions.RelativeCompareOfIteratorsNotOnSameBase();  }
				
			return (a.lineIndex >= b.lineIndex);
			}
			
		public static bool operator< (LineIterator a, LineIterator b)
			{
			return !(a >= b);
			}
			
		public static bool operator<= (LineIterator a, LineIterator b)
			{
			return !(a > b);
			}
			
			
			
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: tokenizer
		 */
		private Tokenizer tokenizer;
		
		/* var: lineIndex
		 * Remember that this is an index, not a line number.
		 */
		private int lineIndex;
		
		/* var: tokenIndex
		 */
		private int tokenIndex;
		
		/* var: rawTextIndex
		 */
		private int rawTextIndex;
		
		}
	}