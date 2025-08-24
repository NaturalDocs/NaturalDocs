/*
 * Class: CodeClear.NaturalDocs.Engine.Prototypes.TypeBuilder
 * ____________________________________________________________________________
 *
 * A class that helps build a new <Tokenizer> from individual tokens extracted from another one.  It is used to
 * build complete types from prototypes where the original tokens may not be continuous or are spread across
 * multiple parameters.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Prototypes
	{
	public class TypeBuilder
		{

		// Group: Types
		// __________________________________________________________________________

		public enum Spacing
			{  Auto, SpaceBefore, NoSpaceBefore  };


		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: TypeBuilder
		 */
		public TypeBuilder (int initialTextLength = -1, int initialTokenCount = -1)
			{
			if (initialTextLength == -1)
				{  rawText = new StringBuilder();  }
			else
				{  rawText = new StringBuilder(initialTextLength);  }

			if (initialTokenCount == -1)
				{
				prototypeParsingTypes = new List<PrototypeParsingType>();
				syntaxHighlightingTypes = new List<SyntaxHighlightingType>();
				}
			else
				{
				prototypeParsingTypes = new List<PrototypeParsingType>(initialTokenCount);
				syntaxHighlightingTypes = new List<SyntaxHighlightingType>(initialTokenCount);
				}

			lastTokenIterator = default(TokenIterator);
			lastCharacter = '\0';
			lastTokenType = FundamentalType.Null;
			}


		/* Function: AddToken
		 *
		 * Adds a single token to the type builder.
		 *
		 * You can optionally force it to include or not include a space before the token.  Normally you should leave this on
		 * <Spacing.Auto> so it will add spaces according to its own rules.
		 */
		public void AddToken (TokenIterator iterator, Spacing spacing = Spacing.Auto)
			{
			char thisCharacter = iterator.Character;
			FundamentalType thisTokenType = (thisCharacter == '_' ? FundamentalType.Text : iterator.FundamentalType);

			// Only add text and symbols.  We'll create the whitespace according to the rules below.
			if (thisTokenType == FundamentalType.Text ||
				thisTokenType == FundamentalType.Symbol)
				{
				bool addSpaceBeforeToken = false;

				// The conditionals are broken out for clarity and ease of documentation.  The compiler should be able to optimize them
				// for us.  This is better than having one big unwieldy mess of an if statement.

				if (spacing == Spacing.SpaceBefore)
					{  addSpaceBeforeToken = true;  }
				else if (spacing == Spacing.NoSpaceBefore)
					{  addSpaceBeforeToken = false;  }

				// Add a space between two non-consecutive text tokens.  It's possible the two iterators come from different tokenizers
				// so check for that.
				else if (lastTokenType == FundamentalType.Text &&
						  thisTokenType == FundamentalType.Text &&
						  (iterator.Tokenizer != lastTokenIterator.Tokenizer || iterator.TokenIndex != lastTokenIterator.TokenIndex + 1))
					{  addSpaceBeforeToken = true;  }

				// Add a space between a symbol followed by text, minus certain exceptions.
				else if (lastTokenType == FundamentalType.Symbol &&
						   thisTokenType == FundamentalType.Text)
					{
					// Space after a single colon like Name: Value, but not after double colon separators like Class::Member
					if (lastCharacter == ':')
						{
						TokenIterator lookbehind = lastTokenIterator;
						lookbehind.Previous();

						if (lookbehind.Character != ':')
							{
							addSpaceBeforeToken = true;
							}
						}

					else if (lastCharacter != '.' &&  // no space after dot separators like Class.Member
							   lastCharacter != '%' &&  // no space after keywords in Oracle's PL/SQL like MyVar%TYPE or MyTable%ROWTYPE
							   lastCharacter != '"' &&  // no space after strings in Java annotations like @copyright("me")
							   lastCharacter != '\'' &&
							   lastCharacter != '@' &&  // no space after tags in Java annotations like @copyright
							   lastCharacter != '$' &&  // no space after keywords in SystemVerilog like $unit
							   lastCharacter != '(' &&  // no space after opening symbols except braces
							   lastCharacter != '[' &&
							   lastCharacter != '<' &&
							   lastCharacter != '=' &&  // no space after assignments in SystemVerilog enum bodies like enum { x=2 }
							   lastCharacter != '`')  // no space after macro invocations in SystemVerilog like `MacroName
						{
						addSpaceBeforeToken = true;
						}
					}

				// Always add a space around braces.
				else if (thisCharacter == '{' ||
						   lastCharacter == '{' ||
						   thisCharacter == '}')
						   // allow symbols to follow } without a space, like SystemVerilog's "struct { ... }[7:0]"
					{
					addSpaceBeforeToken = true;
					}

				// Add a space between commas and other symbols, except when it's a closing symbol or another comma.  This lets us
				// condense things like "string[,,]".
				else if (lastCharacter == ',' &&
						   thisCharacter != ',' &&
						   thisCharacter != ')' &&
						   thisCharacter != ']' &&
						   thisCharacter != '}' &&
						   thisCharacter != '>')
					{
					addSpaceBeforeToken = true;
					}

				if (addSpaceBeforeToken && !IsEmpty)
					{
					rawText.Append(' ');
					prototypeParsingTypes.Add(PrototypeParsingType.Null);
					syntaxHighlightingTypes.Add(SyntaxHighlightingType.Null);
					}

				iterator.AppendTokenTo(rawText);
				prototypeParsingTypes.Add(iterator.PrototypeParsingType);
				syntaxHighlightingTypes.Add(iterator.SyntaxHighlightingType);

				lastTokenIterator = iterator;
				lastCharacter = thisCharacter;
				lastTokenType = thisTokenType;
				}
			}


		/* Function: AddTokens
		 *
		 * Adds a group of tokens to the type builder.
		 *
		 * You can optionally force it to include or not include a space before the first token.  Normally you should leave this on
		 * <Spacing.Auto> so it will add spaces according to its own rules.  This will only apply to the first token, the rest will be
		 * added with <Spacing.Auto>.
		 */
		public void AddTokens (TokenIterator iterator, TokenIterator end, Spacing firstTokenSpacing = Spacing.Auto)
			{
			if (iterator < end)
				{
				AddToken(iterator, firstTokenSpacing);
				iterator.Next();
				}

			while (iterator < end)
				{
				AddToken(iterator, Spacing.Auto);
				iterator.Next();
				}
			}


		/* Function: ToTokenizer
		 * Creates a <Tokenizer> from everything added to the type builder.
		 */
		public Tokenizer ToTokenizer ()
			{
			Tokenizer tokenizer = new Tokenizer(rawText.ToString());
			TokenIterator iterator = tokenizer.FirstToken;

			while (iterator.IsInBounds)
				{
				iterator.PrototypeParsingType = prototypeParsingTypes[iterator.TokenIndex];
				iterator.SyntaxHighlightingType = syntaxHighlightingTypes[iterator.TokenIndex];
				iterator.Next();
				}

			return tokenizer;
			}


		/* Function: HasSimilarSpacing
		 * Returns whether the spacing of the tokens between the two iterators matches what would have been built by this class
		 * if the tokens were passed through it.  If it returns true you can use the tokens themselves instead of a TypeBuilder.
		 */
		public static bool HasSimilarSpacing (TokenIterator start, TokenIterator end)
			{
			// We're going to keep this function simple instead of trying to recreate all the logic in AddToken() faithfully.  This should
			// cover 90% of the types people use and that should be good enough.

			TokenIterator iterator = start;

			while (iterator < end)
				{
				if (iterator.FundamentalType == FundamentalType.Text ||
					iterator.Character == '_')
					{
					iterator.Next();
					bool mustBeFollowedByText = false;

					// Allow a dot following text if it's also followed by text, such as ClassA.ClassB
					if (iterator.Character == '.')
						{
						iterator.Next();
						mustBeFollowedByText = true;
						}

					// Allow two colons following text if it's also followed by text, such as ClassA::ClassB
					else if (iterator.MatchesAcrossTokens("::"))
						{
						iterator.Next(2);
						mustBeFollowedByText = true;
						}

					// Allow a single space following text if it's also followed by text, such as "unsigned int"
					else if (iterator.Character == ' ' &&
							   iterator.TokenLength == 1)
						{
						iterator.Next();
						mustBeFollowedByText = true;
						}

					if (mustBeFollowedByText)
						{
						if (iterator.FundamentalType == FundamentalType.Text ||
							iterator.Character == '_')
							{  iterator.Next();  }
						else
							{  break;  }
						}
					}

				else // token isn't text
					{  break;  }
				}

			// Also allow these symbols immediately after the type name, since they're easy to include
			if (iterator < end &&
				(iterator.Character == '*' ||
				 iterator.Character == '&' ||
				 iterator.Character == '?'))
				{
				iterator.Next();
				}

			return (iterator == end);
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: IsEmpty
		 * Whether anything has been added to the type builder yet.
		 */
		public bool IsEmpty
			{
			get
				{  return (rawText.Length == 0);  }
			}



		// Group: Data Variables
		// __________________________________________________________________________


		/* var: rawText
		 * The raw text of the type being built.
		 */
		protected StringBuilder rawText;

		/* var: prototypeParsingTypes
		 * A list of the <PrototypeParsingTypes> that apply to the type being built.
		 */
		protected List<PrototypeParsingType> prototypeParsingTypes;

		/* var: syntaxHighlightingTypes
		 * A list of the <SyntaxHighlightingTypes> that apply to the type being built.
		 */
		 protected List<SyntaxHighlightingType> syntaxHighlightingTypes;



		// Group: State Variables
		// __________________________________________________________________________


		/* var: lastTokenIterator
		 * A copy of the <TokenIterator> at the position of the last added token.
		 */
		protected TokenIterator lastTokenIterator;

		/* var: lastCharacter
		 * The first character of the last token added.
		 */
		protected char lastCharacter;

		/* var: lastTokenType
		 * The <FundamentalType> of the last token added.  Will be set to <FundamentalType.Text> for underscores.
		 */
		protected FundamentalType lastTokenType;

		}
	}
