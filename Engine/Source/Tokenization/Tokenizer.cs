/* 
 * Class: GregValure.NaturalDocs.Engine.Tokenization.Tokenizer
 * ____________________________________________________________________________
 * 
 * A class for dividing a block of text into easily navigable tokens.  See <FundamentalType> for a description of
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

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
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
		 * Creates a tokenizer from the passed string.  If the string doesn't come from the beginning of the file you can
		 * pass the line number it appears at.
		 */
		public Tokenizer (string input, int startingLineNumber = 1)
			{
			if (input == null)
				{
				rawText = null;
				tokenLengths = null;
				commentParsingTypes = null;
				syntaxHighlightingTypes = null;
				prototypeParsingTypes = null;
				classPrototypeParsingTypes = null;
				lines = null;
				this.startingLineNumber = startingLineNumber;
				}
			else
				{  
				// DEPENDENCY: Load() must set all internal variables.
				Load(input, startingLineNumber);  
				}
			}
			
			
		/* Function: CreateFromIterators
		 * Creates a new tokenizer from the range between two <TokenIterators>.  The token the ending iterator is
		 * on is not included in the range.  The new tokenizer has a copy of the memory and is thus independent.  This
		 * is faster than creating a new tokenizer around a substring of the raw text because it doesn't need to be
		 * tokenized all over again.  It also carries over any defined token information like <CommentParsingTypes>.
		 */
		public Tokenizer CreateFromIterators (TokenIterator start, TokenIterator end)
			{
			#if DEBUG
				if (start.Tokenizer != this || end.Tokenizer != this)
					{  throw new InvalidOperationException();  }
				if (!start.IsInBounds || start > end)
					{  throw new ArgumentOutOfRangeException();  }
			#endif

			Tokenizer result = new Tokenizer(null);
			
			// If end is out of bounds it's return values will be one past the end of the data.	
			result.rawText = rawText.Substring(start.RawTextIndex, end.RawTextIndex - start.RawTextIndex);
			
			result.tokenLengths = tokenLengths.GetRange(start.TokenIndex, end.TokenIndex - start.TokenIndex);
			result.startingLineNumber = start.LineNumber;
			
			// Leave lines null.  Even if they exist the iterators may not be cleanly on the beginning and end.  Let them be
			// recalculated.

			if (commentParsingTypes != null)
				{  
				result.commentParsingTypes = new CommentParsingType[result.tokenLengths.Count];
				Array.Copy(commentParsingTypes, start.TokenIndex, result.commentParsingTypes, 0, result.tokenLengths.Count);
				}
			if (syntaxHighlightingTypes != null)
				{  
				result.syntaxHighlightingTypes = new SyntaxHighlightingType[result.tokenLengths.Count];
				Array.Copy(syntaxHighlightingTypes, start.TokenIndex, result.syntaxHighlightingTypes, 0, result.tokenLengths.Count);
				}
			if (prototypeParsingTypes != null)
				{  
				result.prototypeParsingTypes = new PrototypeParsingType[result.tokenLengths.Count];
				Array.Copy(prototypeParsingTypes, start.TokenIndex, result.prototypeParsingTypes, 0, result.tokenLengths.Count);
				}
			if (classPrototypeParsingTypes != null)
				{  
				result.classPrototypeParsingTypes = new ClassPrototypeParsingType[result.tokenLengths.Count];
				Array.Copy(classPrototypeParsingTypes, start.TokenIndex, result.classPrototypeParsingTypes, 0, result.tokenLengths.Count);
				}

			return result;
			}


		/* Function: CreateFromIterators
		 * Creates a new tokenizer from the range between two <LineIterators>.  The line the ending iterator is
		 * on is not included in the range.  The new tokenizer has a copy of the memory and is thus independent.  This
		 * is faster than creating a new tokenizer around a substring of the raw text because it doesn't need to be
		 * tokenized all over again.  It also carries over any defined token information like <CommentParsingTypes>.
		 */
		public Tokenizer CreateFromIterators (LineIterator start, LineIterator end)
		    {
			Tokenizer result = CreateFromIterators( start.FirstToken(LineBoundsMode.Everything), 
																					end.FirstToken(LineBoundsMode.Everything) );

			result.lines = lines.GetRange(start.LineIndex, end.LineIndex - start.LineIndex);
			
			return result;
			}


		/* Function: TextBetween
		 * Returns the text between the two passed iterators.  If you plan to add it to a StringBuilder, it is more efficient to call
		 * <AppendTextBetweenTo()> instead because that won't require the creation of an intermediate string.
		 */
		public string TextBetween (TokenIterator start, TokenIterator end)
			{
			#if DEBUG
				if (start.Tokenizer != this || end.Tokenizer != this)
					{  throw new InvalidOperationException();  }
				if (!start.IsInBounds || start > end)
					{  throw new ArgumentOutOfRangeException();  }
			#endif

			return rawText.Substring(start.RawTextIndex, end.RawTextIndex - start.RawTextIndex);
			}


		/* Function: AppendTextBetweenTo
		 * Appends the text between the two passed iterators to the passed StringBuilder.  This is more effecient than appending
		 * the result from <TextBetween()> because it transfers directly from the raw text to the StringBuilder without creating 
		 * an intermediate string.
		 */
		public void AppendTextBetweenTo (TokenIterator start, TokenIterator end, System.Text.StringBuilder output)
			{
			#if DEBUG
				if (start.Tokenizer != this || end.Tokenizer != this)
					{  throw new InvalidOperationException();  }
				if (!start.IsInBounds || start > end)
					{  throw new ArgumentOutOfRangeException();  }
			#endif

			output.Append(rawText, start.RawTextIndex, end.RawTextIndex - start.RawTextIndex);
			}
			
			
		/* Function: MatchTextBetween
		 * Runs the passed regular expression on the text between the two iterators and returns the result.
		 */
		public Match MatchTextBetween (System.Text.RegularExpressions.Regex regex, TokenIterator start, TokenIterator end)
			{
			#if DEBUG
				if (start.Tokenizer != this || end.Tokenizer != this)
					{  throw new InvalidOperationException();  }
				if (!start.IsInBounds || start > end)
					{  throw new ArgumentOutOfRangeException();  }
			#endif

			return regex.Match(rawText, start.RawTextIndex, end.RawTextIndex - start.RawTextIndex);
			}
			
			
		/* Function: ContainsTextBetween
		 * Returns whether the text between the two iterators contains the passed string.
		 */
		public bool ContainsTextBetween (string searchText, bool ignoreCase, TokenIterator start, TokenIterator end)
			{
			#if DEBUG
				if (start.Tokenizer != this || end.Tokenizer != this)
					{  throw new InvalidOperationException();  }
				if (!start.IsInBounds || start > end)
					{  throw new ArgumentOutOfRangeException();  }
			#endif

			StringComparison compareMode = (ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
			return (rawText.IndexOf(searchText, start.RawTextIndex, end.RawTextIndex - start.RawTextIndex, compareMode) != -1);
			}


		/* Function: EqualsTextBetween
		 * Returns whether the text between the two iterators matches the passed string exactly.
		 */
		public bool EqualsTextBetween (string searchText, bool ignoreCase, TokenIterator start, TokenIterator end)
			{
			#if DEBUG
				if (start.Tokenizer != this || end.Tokenizer != this)
					{  throw new InvalidOperationException();  }
				if (!start.IsInBounds || start > end)
					{  throw new ArgumentOutOfRangeException();  }
			#endif

			if (end.RawTextIndex - start.RawTextIndex != searchText.Length)
				{  return false;  }

			StringComparison compareMode = (ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
			return (rawText.IndexOf(searchText, start.RawTextIndex, end.RawTextIndex - start.RawTextIndex, compareMode) == start.RawTextIndex);
			}


		/* Function: FindTokenBetween
		 * Attempts to find the passed string as a token between the two iterators, and set a <TokenIterator> at its position if successful.  
		 * The string must match the entire token, so "some" will not match "something".
		 */
		public bool FindTokenBetween (string text, bool ignoreCase, TokenIterator start, TokenIterator end, out TokenIterator result)
			{
			TokenIterator findTokensBetweenResult;
			
			if (FindTokensBetween(text, ignoreCase, start, end, out findTokensBetweenResult) == false ||
				findTokensBetweenResult.RawTextLength != text.Length)
				{  
				result = end;
				return false;
				}
			else
				{  
				result = findTokensBetweenResult;
				return true;
				}
			}
			
			
		/* Function: FindTokensBetween
		 * Attempts to find the passed string between the two iterators, and sets a <TokenIterator> at its position if successful.  This 
		 * function can cross token boundaries, so you can search for "<<" even though that would normally be two tokens.  The result 
		 * must match complete tokens though, so "<< some" will not match "<< something".
		 */
		public bool FindTokensBetween (string text, bool ignoreCase, TokenIterator start, TokenIterator end, out TokenIterator result)
			{
			if (!start.IsInBounds || start > end)
				{  
				result = end;
				return false;
				}
				
			int resultIndex = rawText.IndexOf( text, start.RawTextIndex, end.RawTextIndex - start.RawTextIndex,
																(ignoreCase ? StringComparison.CurrentCultureIgnoreCase : 
																					 StringComparison.CurrentCulture) );
																									   
			if (resultIndex == -1)
				{  
				result = end;
				return false;
				}
				
			result = start;

			// Do this instead of NextByCharacters() so we don't cause an exception if it's not on a token boundary.
			while (result.RawTextIndex < resultIndex)
				{  result.Next();  }

			if (result.RawTextIndex != resultIndex)
				{  
				result = end;
				return false;
				}
			
			return true;
			}



		// Group: Type Functions
		// __________________________________________________________________________


		/* Function: FundamentalTypeAt
		 * Returns the <FundamentalType> at the passed location.
		 */
		public FundamentalType FundamentalTypeAt (int tokenIndex, int rawTextIndex)
			{
			// We test the bounds of tokenIndex instead of rawTextIndex because iterators will keep the raw text index at zero if you go 
			// backwards past the beginning of the string.
			if (tokenIndex < 0 || tokenIndex >= tokenLengths.Count)
				{  return FundamentalType.Null;  }
			else
				{  return FundamentalTypeOf(rawText[rawTextIndex]);  }
			}

		/* Function: CommentParsingTypeAt
		 * Returns the <CommentParsingType> at the passed token index.
		 */
		public CommentParsingType CommentParsingTypeAt (int tokenIndex)
			{
			if (commentParsingTypes == null || tokenIndex < 0 || tokenIndex >= commentParsingTypes.Length)
				{  return CommentParsingType.Null;  }
			else
				{  return commentParsingTypes[tokenIndex];  }
			}

		/* Function: SetCommentParsingTypeAt
		 * Changes the <CommentParsingType> at the passed token index.
		 */
		public void SetCommentParsingTypeAt (int tokenIndex, CommentParsingType type)
			{
			if (commentParsingTypes == null)
				{  commentParsingTypes = new CommentParsingType[tokenLengths.Count];  }

			if (tokenIndex < 0 || tokenIndex >= commentParsingTypes.Length)
				{  throw new ArgumentOutOfRangeException();  }

			commentParsingTypes[tokenIndex] = type;
			}
			
		/* Function: SyntaxHighlightingTypeAt
		 * Returns the <SyntaxHighlightingType> at the passed token index.
		 */
		public SyntaxHighlightingType SyntaxHighlightingTypeAt (int tokenIndex)
			{
			if (syntaxHighlightingTypes == null || tokenIndex < 0 || tokenIndex >= syntaxHighlightingTypes.Length)
				{  return SyntaxHighlightingType.Null;  }
			else
				{  return syntaxHighlightingTypes[tokenIndex];  }
			}

		/* Function: SetSyntaxHighlightingTypeAt
		 * Changes the <SyntaxHighlightingType> at the passed token index.
		 */
		public void SetSyntaxHighlightingTypeAt (int tokenIndex, SyntaxHighlightingType type)
			{
			if (syntaxHighlightingTypes == null)
				{  syntaxHighlightingTypes = new SyntaxHighlightingType[tokenLengths.Count];  }

			if (tokenIndex < 0 || tokenIndex >= syntaxHighlightingTypes.Length)
				{  throw new ArgumentOutOfRangeException();  }

			syntaxHighlightingTypes[tokenIndex] = type;
			}
			
		/* Function: SetSyntaxHighlightingTypeBetween
		 * Changes the <SyntaxHighlightingType> of all the tokens between the two passed indexes.  The
		 * token at the ending index will not be changed.
		 */
		public void SetSyntaxHighlightingTypeBetween (int startingIndex, int endingIndex, SyntaxHighlightingType type)
			{
			if (syntaxHighlightingTypes == null)
				{  syntaxHighlightingTypes = new SyntaxHighlightingType[tokenLengths.Count];  }

			if (startingIndex < 0 || endingIndex > syntaxHighlightingTypes.Length)
				{  throw new ArgumentOutOfRangeException();  }
			if (startingIndex > endingIndex)
				{  throw new InvalidOperationException();  }

			for (int i = startingIndex; i < endingIndex; i++)
				{  syntaxHighlightingTypes[i] = type;  }
			}			
			
		/* Function: SetSyntaxHighlightingTypeBetween
		 * Changes the <SyntaxHighlightingType> of all the tokens between the two passed iterators.  The
		 * token at the ending iterator will not be changed.
		 */
		public void SetSyntaxHighlightingTypeBetween (TokenIterator startingIterator, TokenIterator endingIterator, 
																							 SyntaxHighlightingType type)
			{
			if (startingIterator.Tokenizer != this || endingIterator.Tokenizer != this)
				{  throw new InvalidOperationException();  }

			SetSyntaxHighlightingTypeBetween(startingIterator.TokenIndex, endingIterator.TokenIndex, type);
			}			
			
		/* Function: PrototypeParsingTypeAt
		 * Returns the <PrototypeParsingType> at the passed token index.
		 */
		public PrototypeParsingType PrototypeParsingTypeAt (int tokenIndex)
			{
			if (prototypeParsingTypes == null || tokenIndex < 0 || tokenIndex >= prototypeParsingTypes.Length)
				{  return PrototypeParsingType.Null;  }
			else
				{  return prototypeParsingTypes[tokenIndex];  }
			}

		/* Function: SetPrototypeParsingTypeAt
		 * Changes the <PrototypeParsingType> at the passed token index.
		 */
		public void SetPrototypeParsingTypeAt (int tokenIndex, PrototypeParsingType type)
			{
			if (prototypeParsingTypes == null)
				{  prototypeParsingTypes = new PrototypeParsingType[tokenLengths.Count];  }

			if (tokenIndex < 0 || tokenIndex >= prototypeParsingTypes.Length)
				{  throw new ArgumentOutOfRangeException();  }

			prototypeParsingTypes[tokenIndex] = type;
			}
			
		/* Function: SetPrototypeParsingTypeBetween
		 * Changes the <PrototypeParsingType> of all the tokens between the two passed indexes.  The
		 * token at the ending index will not be changed.
		 */
		public void SetPrototypeParsingTypeBetween (int startingIndex, int endingIndex, PrototypeParsingType type)
			{
			if (prototypeParsingTypes == null)
				{  prototypeParsingTypes = new PrototypeParsingType[tokenLengths.Count];  }

			if (startingIndex < 0 || endingIndex > prototypeParsingTypes.Length)
				{  throw new ArgumentOutOfRangeException();  }
			if (startingIndex > endingIndex)
				{  throw new InvalidOperationException();  }

			for (int i = startingIndex; i < endingIndex; i++)
				{  prototypeParsingTypes[i] = type;  }
			}			
			
		/* Function: SetPrototypeParsingTypeBetween
		 * Changes the <PrototypeParsingType> of all the tokens between the two passed iterators.  The
		 * token at the ending iterator will not be changed.
		 */
		public void SetPrototypeParsingTypeBetween (TokenIterator startingIterator, TokenIterator endingIterator, 
																							 PrototypeParsingType type)
			{
			if (startingIterator.Tokenizer != this || endingIterator.Tokenizer != this)
				{  throw new InvalidOperationException();  }

			SetPrototypeParsingTypeBetween(startingIterator.TokenIndex, endingIterator.TokenIndex, type);
			}
			
		/* Function: ClassPrototypeParsingTypeAt
		 * Returns the <ClassPrototypeParsingType> at the passed token index.
		 */
		public ClassPrototypeParsingType ClassPrototypeParsingTypeAt (int tokenIndex)
			{
			if (classPrototypeParsingTypes == null || tokenIndex < 0 || tokenIndex >= classPrototypeParsingTypes.Length)
				{  return ClassPrototypeParsingType.Null;  }
			else
				{  return classPrototypeParsingTypes[tokenIndex];  }
			}

		/* Function: SetClassPrototypeParsingTypeAt
		 * Changes the <ClassPrototypeParsingType> at the passed token index.
		 */
		public void SetClassPrototypeParsingTypeAt (int tokenIndex, ClassPrototypeParsingType type)
			{
			if (classPrototypeParsingTypes == null)
				{  classPrototypeParsingTypes = new ClassPrototypeParsingType[tokenLengths.Count];  }

			if (tokenIndex < 0 || tokenIndex >= classPrototypeParsingTypes.Length)
				{  throw new ArgumentOutOfRangeException();  }

			classPrototypeParsingTypes[tokenIndex] = type;
			}
			
		/* Function: SetClassPrototypeParsingTypeBetween
		 * Changes the <ClassPrototypeParsingType> of all the tokens between the two passed indexes.  The
		 * token at the ending index will not be changed.
		 */
		public void SetClassPrototypeParsingTypeBetween (int startingIndex, int endingIndex, ClassPrototypeParsingType type)
			{
			if (classPrototypeParsingTypes == null)
				{  classPrototypeParsingTypes = new ClassPrototypeParsingType[tokenLengths.Count];  }

			if (startingIndex < 0 || endingIndex > classPrototypeParsingTypes.Length)
				{  throw new ArgumentOutOfRangeException();  }
			if (startingIndex > endingIndex)
				{  throw new InvalidOperationException();  }

			for (int i = startingIndex; i < endingIndex; i++)
				{  classPrototypeParsingTypes[i] = type;  }
			}			
			
		/* Function: SetClassPrototypeParsingTypeBetween
		 * Changes the <ClassPrototypeParsingType> of all the tokens between the two passed iterators.  The
		 * token at the ending iterator will not be changed.
		 */
		public void SetClassPrototypeParsingTypeBetween (TokenIterator startingIterator, TokenIterator endingIterator, 
																											ClassPrototypeParsingType type)
			{
			if (startingIterator.Tokenizer != this || endingIterator.Tokenizer != this)
				{  throw new InvalidOperationException();  }

			SetClassPrototypeParsingTypeBetween(startingIterator.TokenIndex, endingIterator.TokenIndex, type);
			}
			
			
			
		// Group: Static Functions
		// __________________________________________________________________________
		
		
		/* Function: FundamentalTypeOf
		 * Returns the <FundamentalType> of the passed character.
		 */
		static public FundamentalType FundamentalTypeOf (char character)
			{
			// DEPENDENCY: If this implementation changes, these must change to match:
			//		- Tokenizer.Load()
			//		- SimpleTokenIterator.GetTokenLength()
			//		- lineBreakChars
			//		- Comments.Parsers.XMLIterator for line break chars
			
			char maskedCharacter = (char)(character | 0x0020);  // Converts A-Z to a-z
				
			if ( (maskedCharacter >= 'a' && maskedCharacter <= 'z') ||
				 (character >= '0' && character <= '9') ||
				 character > 0x007F )  // Beyond ASCII
				{
				return FundamentalType.Text;
				}
					
			else if (character == ' ' || character == '\t')
				{
				return FundamentalType.Whitespace;
				}
					
			else if (character == '\n' || character == '\r')
				{
				return FundamentalType.LineBreak;
				}
					
			else
				{
				return FundamentalType.Symbol;
				}
			}
			
			
			
		// Group: Protected/Internal Functions
		// __________________________________________________________________________


		/* Function: Load
		 * Loads and tokenizes the passed string.
		 */
		protected void Load (string input, int newStartingLineNumber)
			{
			// DEPENDENCY: The constructor assumes all internal variables will be set.

			rawText = input;
			// Random guess, almost definitely too low, but that's better than being too high.  Will still be closer than the default.
			tokenLengths = new List<byte>(8 + (input.Length / 20));
			commentParsingTypes = null;
			syntaxHighlightingTypes = null;
			lines = null;
			startingLineNumber = newStartingLineNumber;
			
			byte tokenLength;
			
			int index = 0;
			char character;
			char maskedCharacter;
			
			while (index < rawText.Length)
				{
				character = rawText[index];
				
				// DEPENDENCY: This must match the implementation of FundamentalTypeOf().
				
				// Text
				
				maskedCharacter = (char)(character | 0x0020);  // Converts A-Z to a-z
				
				if ( (maskedCharacter >= 'a' && maskedCharacter <= 'z') ||
					 (character >= '0' && character <= '9') ||
					 character > 0x007F )  // Beyond ASCII
					{
					tokenLength = 1;
					index++;
					
					while (index < rawText.Length && tokenLength < 255)
						{
						character = rawText[index];
						maskedCharacter = (char)(character | 0x0020);
						
						if ( (maskedCharacter >= 'a' && maskedCharacter <= 'z') ||
							 (character >= '0' && character <= '9') ||
							 character > 0x007F )
							{
							tokenLength++;
							index++;
							}
						else
							{  break;  }
						}
						
					tokenLengths.Add(tokenLength);
					}
					
					
				// Whitespace
				
				else if (character == ' ' || character == '\t')
					{
					tokenLength = 1;
					index++;
					
					while (index < rawText.Length && tokenLength < 255)
						{
						character = rawText[index];
						
						if (character == ' ' || character == '\t')
							{
							tokenLength++;
							index++;
							}
						else
							{  break;  }
						}
						
					tokenLengths.Add(tokenLength);
					}
					
					
				// Line break
				
				else if (character == '\n' || character == '\r')
					{
					tokenLength = 1;
					index++;
					
					if (index < rawText.Length && character == '\r' && rawText[index] == '\n')
						{
						tokenLength++;
						index++;
						}
						
					tokenLengths.Add(tokenLength);
					}
					
					
				// Symbols
				
				else
					{
					index++;
					tokenLengths.Add(1);
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
			int rawTextIndex = 0;
			FundamentalType previousType = FundamentalType.Null;
			
			while (tokenIndex < tokenLengths.Count)
				{
				line.TokenLength = 0;
				line.RawTextLength = 0;

				do
					{
					line.TokenLength++;
					line.RawTextLength += tokenLengths[tokenIndex];

					previousType = FundamentalTypeOf(rawText[rawTextIndex]);

					rawTextIndex += tokenLengths[tokenIndex];
					tokenIndex++;
					}
				while (tokenIndex < tokenLengths.Count && previousType != FundamentalType.LineBreak);
					
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
			
		/* Function: LastToken
		 * A <TokenIterator> set to one past the last token of this object.
		 */
		public TokenIterator LastToken
			{
			get
				{  return new TokenIterator(this, tokenLengths.Count, rawText.Length, StartingLineNumber + Lines.Count);  }
			}

		/* Function: TokenCount
		 * The number of tokens in this object.
		 */
		public int TokenCount
			{
			get
				{  return tokenLengths.Count;  }
			}
			
		/* Function: FirstLine
		 * A <LineIterator> set to the first line in this object.
		 */
		public LineIterator FirstLine
			{
			get
				{  return new LineIterator(this, 0, 0, 0);  }
			}

		/* Function: LastLine
		 * A <LineIterator> set to the one past the last line in this object.
		 */
		public LineIterator LastLine
			{
			get
				{  return new LineIterator(this, Lines.Count, tokenLengths.Count, rawText.Length);  }
			}

		/* Function: HasSyntaxHighlighting
		 * Whether syntax highlighting has been applied.
		 */
		public bool HasSyntaxHighlighting
			{
			get
				{  return (syntaxHighlightingTypes != null);  }
			}


			
		// Group: Protected/Internal Properties
		// __________________________________________________________________________
		
			
		/* Property: TokenLengths
		 * The list of token lengths.
		 */
		protected internal IList<byte> TokenLengths
			{
			get
				{  return tokenLengths;  }
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
		
		/* var: tokenLengths
		 * The list of token lengths generated for <rawText> based on <FundamentalTypes>.
		 */
		protected List<byte> tokenLengths;

		/* var: commentParsingTypes
		 * A list of <CommentParsingTypes> that are set for each token.  The array indexes correspond to those in
		 * <tokenLengths>.  This is created on demand, so if none have been assigned this will be null.
		 */
		protected CommentParsingType[] commentParsingTypes;

		/* var: syntaxHighlightingTypes
		 * A list of <SyntaxHighlightingTypes> that are set for each token.  The array indexes correspond to those in
		 * <tokenLengths>.  This is created on demand, so if none have been assigned this will be null.
		 */
		protected SyntaxHighlightingType[] syntaxHighlightingTypes;
		
		/* var: prototypeParsingTypes
		 * A list of <PrototypeParsingTypes> that are set for each token.  The array indexes correspond to those in
		 * <tokenLengths>.  This is created on demand, so if none have been assigned this will be null.
		 */
		protected PrototypeParsingType[] prototypeParsingTypes;

		/* var: classPrototypeParsingTypes
		 * A list of <ClassPrototypeParsingTypes> that are set for each token.  The array indexes correspond to those
		 * in <tokenLengths>.  This is created on demand, so if none have been assigned this will be null.
		 */
		protected ClassPrototypeParsingType[] classPrototypeParsingTypes;
		
		/* var: lines
		 * The list of <Lines> generated for <rawText>.  This is generated on demand so this variable will be null 
		 * if it hasn't been done yet.
		 */
		protected List<Line> lines;
		
		/* var: startingLineNumber
		 */
		protected int startingLineNumber;



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: lineBreakChars
		 * An array of all the characters that count as <FundamentalType.LineBreak>.
		 */
		internal static char[] lineBreakChars = { '\r', '\n' };
		// DEPENDENCY: This must match the implementation of FundamentalTypeOf().

		}
	}