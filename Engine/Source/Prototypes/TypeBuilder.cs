/* 
 * Class: CodeClear.NaturalDocs.Engine.Prototypes.TypeBuilder
 * ____________________________________________________________________________
 * 
 * A class that helps build a new <Tokenizer> from individual tokens extracted from another one.  It is used to
 * build complete types from prototypes where the original tokens may not be continuous or are spread across
 * multiple parameters.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
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
			lastTokenType = FundamentalType.Null;
			pastFirstText = false;
			dontAddSpaceAfterSymbol = false;
			lastSymbolWasBlock = false;
			}


		/* Function: AddToken
		 * Adds a single token to the type builder.  You must use <AddBlock()> for blocks.
		 */
		public void AddToken (TokenIterator iterator)
			{
			if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
				iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier)
				{  throw new InvalidOperationException();  }

			if (iterator.PrototypeParsingType == PrototypeParsingType.Type ||
				iterator.PrototypeParsingType == PrototypeParsingType.TypeModifier ||
				iterator.PrototypeParsingType == PrototypeParsingType.TypeQualifier ||
				iterator.PrototypeParsingType == PrototypeParsingType.ParamModifier ||
				iterator.PrototypeParsingType == PrototypeParsingType.StartOfTuple ||
				iterator.PrototypeParsingType == PrototypeParsingType.EndOfTuple ||
				iterator.PrototypeParsingType == PrototypeParsingType.TupleMemberSeparator ||
				iterator.PrototypeParsingType == PrototypeParsingType.TupleMemberName)
				{
				FundamentalType thisTokenType = (iterator.Character == '_' ? FundamentalType.Text : iterator.FundamentalType);

				// ignore whitespace, we'll create it as necessary
				if (thisTokenType == FundamentalType.Text ||
					thisTokenType == FundamentalType.Symbol)
					{
					// Do we need to add a space?
					if ( (thisTokenType == FundamentalType.Text && lastTokenType == FundamentalType.Text &&
						  (iterator.Tokenizer != lastTokenIterator.Tokenizer || iterator.TokenIndex != lastTokenIterator.TokenIndex + 1)) 
						 ||
						 (thisTokenType == FundamentalType.Text && lastTokenType == FundamentalType.Symbol && 
						  !dontAddSpaceAfterSymbol && (pastFirstText || lastSymbolWasBlock))
						 ||
						 (!lastTokenIterator.IsNull && lastTokenIterator.PrototypeParsingType == PrototypeParsingType.TupleMemberSeparator))
						{
						rawText.Append(' ');
						prototypeParsingTypes.Add(PrototypeParsingType.Null);
						syntaxHighlightingTypes.Add(SyntaxHighlightingType.Null);
						}

					// Special handling for package separators and a few other things
					if (iterator.FundamentalType == FundamentalType.Symbol)
						{
						if (iterator.Character == '.'  || iterator.MatchesAcrossTokens("::") || 
							(iterator.Character == ':' && dontAddSpaceAfterSymbol) ||  // second colon of ::
							iterator.Character == '%' ||  // used for MyVar%TYPE or MyTable%ROWTYPE in Oracle's PL/SQL
							iterator.Character == '"' || iterator.Character == '\'' ||  // strings in Java annotations like @copyright("me")
							iterator.Character == '@' ||  // tags in Java annotations like @copyright
							iterator.Character == '(')  // parens around tuples
							{  dontAddSpaceAfterSymbol = true;  }
						else
							{  dontAddSpaceAfterSymbol = false;  }
						}

					iterator.AppendTokenTo(rawText);
					prototypeParsingTypes.Add(iterator.PrototypeParsingType);
					syntaxHighlightingTypes.Add(iterator.SyntaxHighlightingType);

					lastTokenIterator = iterator;
					lastTokenType = thisTokenType;
					lastSymbolWasBlock = false;

					if (thisTokenType == FundamentalType.Text)
						{  pastFirstText = true;  }
					}
				}
			}


		/* Function: AddModifierBlock
		 * Adds a full modifier block marked with <PrototypeParsingType.OpeningTypeModifier> or <PrototypeParsingType.OpeningParamModifier>
		 * to the type builder.
		 */
		public void AddModifierBlock (TokenIterator openingToken, TokenIterator closingToken)
			{
			if ( (openingToken.PrototypeParsingType != PrototypeParsingType.OpeningTypeModifier &&
				  openingToken.PrototypeParsingType != PrototypeParsingType.OpeningParamModifier) ||
				 (closingToken.PrototypeParsingType != PrototypeParsingType.ClosingTypeModifier &&
				  closingToken.PrototypeParsingType != PrototypeParsingType.ClosingParamModifier) )
				{  throw new InvalidOperationException();  }

			TokenIterator iterator = openingToken;
			TokenIterator end = closingToken;
			end.Next();

			while (iterator < end)
				{
				FundamentalType thisTokenType = (iterator.Character == '_' ? FundamentalType.Text : iterator.FundamentalType);

				// Do we need to add a space?
				if ( (thisTokenType == FundamentalType.Text && lastTokenType == FundamentalType.Text &&
					  (iterator.Tokenizer != lastTokenIterator.Tokenizer || iterator.TokenIndex != lastTokenIterator.TokenIndex + 1)) 
					||
					(thisTokenType == FundamentalType.Text && lastTokenType == FundamentalType.Symbol && 
					 !dontAddSpaceAfterSymbol && (pastFirstText || lastSymbolWasBlock)) )
					{
					rawText.Append(' ');
					prototypeParsingTypes.Add(PrototypeParsingType.Null);
					syntaxHighlightingTypes.Add(SyntaxHighlightingType.Null);
					}

				while (iterator < end)
					{
					iterator.AppendTokenTo(rawText);
					prototypeParsingTypes.Add(iterator.PrototypeParsingType);
					syntaxHighlightingTypes.Add(iterator.SyntaxHighlightingType);

					iterator.Next();
					}

				lastTokenIterator = closingToken;
				lastTokenType = thisTokenType;
				lastSymbolWasBlock = true;
				dontAddSpaceAfterSymbol = false;

				if (thisTokenType == FundamentalType.Text)
					{  pastFirstText = true;  }
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
		 * Returns whether the spacing of the tokens between the two iterators matches what would have been built by this
		 * class if the tokens were passed through it.
		 */
		public static bool HasSimilarSpacing (TokenIterator start, TokenIterator end)
			{
			// ::Package::Name* array of const*[]
			// Single spaces only between words, and between words and prior symbols except for leading symbols and package separators

			TokenIterator iterator = start;

			bool pastFirstText = false;
			FundamentalType lastTokenType = FundamentalType.Null;
			bool dontAddSpaceAfterSymbol = false;
			bool lastSymbolWasBlock = false;

			while (iterator < end)
				{
				if (iterator.FundamentalType == FundamentalType.Text ||
					iterator.Character == '_')
					{
					if (lastTokenType == FundamentalType.Null ||
						lastTokenType == FundamentalType.Text ||
						(lastTokenType == FundamentalType.Symbol && (dontAddSpaceAfterSymbol || (!pastFirstText && !lastSymbolWasBlock))) ||
						lastTokenType == FundamentalType.Whitespace)
						{
						pastFirstText = true;
						lastTokenType = FundamentalType.Text;

						if (!TryToSkipModifierBlock(ref iterator))
							{  iterator.Next();  }
						}
					else
						{  return false;  }
					}
				else if (iterator.FundamentalType == FundamentalType.Symbol)
					{
					if (iterator.Character == ',')
						{
						// Quit early on commas, since it could be x[,,] or (x, y), in which case it's not clear whether there should be
						// a space without making this logic even more complicated.  Just fail out and build a new one.
						return false;
						}

					if (lastTokenType == FundamentalType.Null ||
						lastTokenType == FundamentalType.Text ||
						lastTokenType == FundamentalType.Symbol)
						{
						lastTokenType = FundamentalType.Symbol;

						if (iterator.MatchesAcrossTokens("::"))
							{
							lastSymbolWasBlock = false;
							dontAddSpaceAfterSymbol = true;
							iterator.NextByCharacters(2);
							}
						else if (iterator.Character == '.' || iterator.Character == '%' || 
								   iterator.Character == '"' || iterator.Character == '\'' || iterator.Character == '@' ||
								   iterator.Character == '(')
							{
							lastSymbolWasBlock = false;
							dontAddSpaceAfterSymbol = true;
							iterator.Next();
							}
						else if (TryToSkipModifierBlock(ref iterator))
							{
							lastSymbolWasBlock = true;
							dontAddSpaceAfterSymbol = false;
							// already moved iterator
							}
						else
							{
							lastSymbolWasBlock = false;
							dontAddSpaceAfterSymbol = false;
							iterator.Next();  
							}
						}
					else
						{  return false;  }
					}
				else if (iterator.FundamentalType == FundamentalType.Whitespace &&
						  iterator.Character == ' ' && iterator.RawTextLength == 1)
					{
					if ((lastTokenType == FundamentalType.Symbol && !dontAddSpaceAfterSymbol && (pastFirstText || lastSymbolWasBlock)) ||
						lastTokenType == FundamentalType.Text)
						{
						lastTokenType = FundamentalType.Whitespace;
						iterator.Next();
						}
					else
						{  return false;  }
					}
				else
					{  return false;  }
				}

			return true;
			}



		// Group: Protected Functions
		// __________________________________________________________________________


		/* Function: TryToSkipModifierBlock
		 * If the iterator is on a <PrototypeParsingType.OpeningTypeModifier> or <PrototypeParsingType.OpeningParamModifier>
		 * token, moves it past the entire block including any nested blocks.
		 */
		protected static bool TryToSkipModifierBlock(ref TokenIterator iterator)
			{
			if (iterator.PrototypeParsingType != PrototypeParsingType.OpeningTypeModifier &&
				iterator.PrototypeParsingType != PrototypeParsingType.OpeningParamModifier)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();
			int level = 1;

			// We're going to cheat and assume all blocks are balanced and nested in a way that makes sense. This lets us handle 
			// both in a simple loop.
			while (lookahead.IsInBounds)
				{
				if (lookahead.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
					lookahead.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier)
					{
					level++;  
					}
				else if (lookahead.PrototypeParsingType == PrototypeParsingType.ClosingTypeModifier ||
						  lookahead.PrototypeParsingType == PrototypeParsingType.ClosingParamModifier)
					{  
					level--;

					if (level == 0)
						{
						lookahead.Next();
						iterator = lookahead;
						return true; 
						}
					}

				lookahead.Next();
				}

			return false;
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

		/* var: lastTokenType
		 * The <FundamentalType> of the last token added.  Will be set to <FundamentalType.Text> for underscores.
		 */
		protected FundamentalType lastTokenType;

		/* var: pastFirstText
		 * Whether any text tokens or underscores have been added.
		 */
		protected bool pastFirstText;

		/* var: dontAddSpaceAfterSymbol
		 * Whether to skip adding a space after the last symbol, such as if it was a package separator.
		 */
		protected bool dontAddSpaceAfterSymbol;

		/* var: lastSymbolWasBlock
		 * Whether the last symbol added was a block.
		 */
		protected bool lastSymbolWasBlock;

		}
	}