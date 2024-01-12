/*
 * Struct: CodeClear.NaturalDocs.Engine.Tokenization.SimpleTokenIterator
 * ____________________________________________________________________________
 *
 * An iterator that walks through an untokenized string in a similar manner to <TokenIterator>.  It figures out tokens on
 * the fly and is useful for comparing <TokenIterators> to simple strings where having a <Tokenizer> would be overkill.
 * However, it contains only a small subset of the functionality in <TokenIterator> and isn't efficient for serious parsing.
 *
 * Like <TokenIterator>, it is designed to be tolerant to allow for easier parsing.  You can go past the bounds of the data
 * without exceptions being thrown.
 *
 * It is a struct rather than a class because it is expected that many of them are going to be created, copied, passed
 * around, and then disappear just as quickly.  It's not worth the memory churn to be a reference type, and having
 * them behave as a value type is more intuitive.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Tokenization
	{
	public struct SimpleTokenIterator
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: SimpleTokenIterator
		 */
		public SimpleTokenIterator (string text, int offset = 0)
			{
			rawText = text;
			rawTextIndex = offset;
			tokenLength = 0;  // to shut the compiler up

			GetTokenLength();
			}

		/* Function: Next
		 * Moves forward the passed number of tokens, returning false if we're past the last token.
		 */
		public bool Next (int count = 1)
			{
			if (count < 0)
				{  throw new InvalidOperationException();  }

			while (count > 0 && rawTextIndex < rawText.Length)
				{
				rawTextIndex += tokenLength;
				GetTokenLength();

				count--;
				}

			return IsInBounds;
			}


		/* Function: NextPastWhitespace
		 * Moves forward until past all whitespace tokens.
		 */
		public void NextPastWhitespace ()
			{
			while (FundamentalType == FundamentalType.Whitespace)
				{  Next();  }
			}


		/* Function: NextPastWhitespace
		 * Moves forward until past all whitespace tokens or the limit is reached.
		 */
		public void NextPastWhitespace (SimpleTokenIterator limit)
			{
			while (FundamentalType == FundamentalType.Whitespace && this < limit)
				{  Next();  }
			}


		/* Function: MatchesToken
		 * Returns whether the passed string matches the current token.  The string must match the entire
		 * token, so "some" won't match "something".  Returns false if the iterator is out of bounds.
		 */
		public bool MatchesToken (string text, bool ignoreCase = false)
			{
			if (!IsInBounds)
				{  return false;  }

			return ( text.Length == tokenLength &&
							String.Compare(rawText, rawTextIndex, text, 0, text.Length, ignoreCase) == 0 );
			}


		/* Function: MatchesToken
		 * Returns whether the current token matches the one at the passed iterator.  Returns false if either iterator
		 * is out of bounds.
		 */
		public bool MatchesToken (TokenIterator other, bool ignoreCase = false)
			{
			return other.MatchesToken(this, ignoreCase);
			}


		/* Function: MatchesToken
		 * Returns whether the current token matches the one at the passed iterator.  Returns false if either iterator
		 * is out of bounds.
		 */
		public bool MatchesToken (SimpleTokenIterator other, bool ignoreCase = false)
			{
			if (!IsInBounds || !other.IsInBounds)
				{  return false;  }

			return ( tokenLength == other.tokenLength &&
						   String.Compare(rawText, rawTextIndex, other.rawText, other.RawTextIndex,
														tokenLength, ignoreCase) == 0 );
			}



		// Group: Private Functions
		// __________________________________________________________________________


		/* Function: GetTokenLength
		 * Generates <tokenLength> from <rawText> and <rawTextIndex>.
		 */
		private void GetTokenLength ()
			{
			if (!IsInBounds)
				{  tokenLength = 0;  }
			else
				{
				// DEPENDENCY: This must match the implementation of Tokenizer.FundamentalTypeOf().

				char character = rawText[rawTextIndex];


				// Text

				char maskedCharacter = (char)(character | 0x0020);  // Converts A-Z to a-z

				if ( (maskedCharacter >= 'a' && maskedCharacter <= 'z') ||
						(character >= '0' && character <= '9') ||
						character > 0x007F )  // Beyond ASCII
					{
					tokenLength = 1;
					int tempIndex = rawTextIndex + 1;

					while (tempIndex < rawText.Length && tokenLength < 255)
						{
						character = rawText[tempIndex];
						maskedCharacter = (char)(character | 0x0020);

						if ( (maskedCharacter >= 'a' && maskedCharacter <= 'z') ||
								(character >= '0' && character <= '9') ||
								character > 0x007F )
							{
							tokenLength++;
							tempIndex++;
							}
						else
							{  break;  }
						}
					}


				// Whitespace

				else if (character == ' ' || character == '\t')
					{
					tokenLength = 1;
					int tempIndex = rawTextIndex + 1;

					while (tempIndex < rawText.Length && tokenLength < 255)
						{
						character = rawText[tempIndex];

						if (character == ' ' || character == '\t')
							{
							tokenLength++;
							tempIndex++;
							}
						else
							{  break;  }
						}
					}


				// Line break

				else if (character == '\n' || character == '\r')
					{
					tokenLength = 1;

					if (rawTextIndex + 1 < rawText.Length && character == '\r' && rawText[rawTextIndex + 1] == '\n')
						{
						tokenLength++;
						}
					}


				// Symbols

				else
					{
					tokenLength = 1;
					}
				}
			}




		// Group: Properties
		// __________________________________________________________________________


		/* Property: String
		 * Returns the token as a string, or an empty string if it's out of bounds.  Note that this allocates memory and
		 * creates a copy of the string.  Whenever possible use functions like <MatchesToken()> to work directly on the
		 * original memory, or use <RawTextIndex> and <RawTextLength> with <RawText> to access it yourself.
		 */
		public string String
			{
			get
				{
				if (IsInBounds)
					{  return rawText.Substring( rawTextIndex, tokenLength);  }
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
					{  return rawText[rawTextIndex];  }
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
				{
				if (IsInBounds)
					{  return Tokenizer.FundamentalTypeOf(rawText[rawTextIndex]);  }
				else
					{  return FundamentalType.Null;  }
				}
			}

		/* Property: RawText
		 * The string that this iterator is navigating through.
		 */
		public string RawText
			{
			get
				{  return rawText;  }
			}

		/* Property: RawTextIndex
		 * The offset of the current token into <RawText>.  Will be zero if it went past the beginning, or the index
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
				{  return tokenLength;  }
			}

		/* Property: IsInBounds
		 * Whether the iterator is not past the beginning or end of the list of tokens.
		 */
		public bool IsInBounds
			{
			get
				{  return (rawTextIndex >= 0 && rawTextIndex < rawText.Length);  }
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
			return rawTextIndex.GetHashCode();
			}

		public static bool operator== (SimpleTokenIterator a, SimpleTokenIterator b)
			{
			return (a.rawText == b.rawText && a.rawTextIndex == b.rawTextIndex);
			}

		public static bool operator!= (SimpleTokenIterator a, SimpleTokenIterator b)
			{
			return !(a == b);
			}

		public static bool operator> (SimpleTokenIterator a, SimpleTokenIterator b)
			{
			if (a.rawText == null || b.rawText == null)
				{  throw new NullReferenceException();  }
			if ((object)a.rawText != (object)b.rawText)
				{  throw new Engine.Exceptions.RelativeCompareOfIteratorsNotOnSameBase();  }

			return (a.rawTextIndex > b.rawTextIndex);
			}

		public static bool operator>= (SimpleTokenIterator a, SimpleTokenIterator b)
			{
			if (a.rawText == null || b.rawText == null)
				{  throw new NullReferenceException();  }
			if ((object)a.rawText != (object)b.rawText)
				{  throw new Engine.Exceptions.RelativeCompareOfIteratorsNotOnSameBase();  }

			return (a.rawTextIndex >= b.rawTextIndex);
			}

		public static bool operator< (SimpleTokenIterator a, SimpleTokenIterator b)
			{
			return !(a >= b);
			}

		public static bool operator<= (SimpleTokenIterator a, SimpleTokenIterator b)
			{
			return !(a > b);
			}



		// Group: Variables
		// __________________________________________________________________________

		/* var: rawText
		 * The string associated with this iterator.
		 */
		private string rawText;

		/* var: rawTextIndex
		 * The current index into <rawText>.
		 */
		private int rawTextIndex;

		/* var: tokenLength
		 * The length of the current token.
		 */
		private int tokenLength;

		}
	}
