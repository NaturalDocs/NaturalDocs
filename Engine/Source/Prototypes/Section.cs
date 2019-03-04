/* 
 * Class: CodeClear.NaturalDocs.Engine.Prototypes.Section
 * ____________________________________________________________________________
 * 
 * A class that wraps a section of a <Tokenizer> which has been marked with <PrototypeParsingTypes>.  Provides basic
 * functionality that will be common to many sections.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Prototypes
	{
	public class Section
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Section
		 */
		public Section (TokenIterator start, TokenIterator end)
			{
			this.start = start;
			this.end = end;
			}


		/* Function: GetBounds
		 * Returns the bounds of the complete section, minus whitespace.
		 */
		public void GetBounds (out TokenIterator start, out TokenIterator end)
			{
			start = this.start;
			end = this.end;
			}


		/* Function: GetName
		 * Returns the bounds of the name if one is marked by <PrototypeParsingType.Name> tokens, or false if it couldn't find it.
		 */
		virtual public bool GetName (out TokenIterator nameStart, out TokenIterator nameEnd)
			{
			TokenIterator iterator = start;

			while (iterator < end && 
					  iterator.PrototypeParsingType != PrototypeParsingType.Name)
				{  iterator.Next();  }

			if (iterator.PrototypeParsingType == PrototypeParsingType.Name)
				{
				nameStart = iterator;

				do
					{  iterator.Next();  }
				while (iterator.PrototypeParsingType == PrototypeParsingType.Name);

				nameEnd = iterator;
				return true;
				}
			else // couldn't find it
				{
				nameStart = end;
				nameEnd = end;
				return false;
				}
			}


		/* Function: GetAccessLevel
		 * Returns the <Languages.AccessLevel> if it can be determined.  This should only be used with basic language support 
		 * as it's not as reliable as the results from the dedicated language parsers.
		 */
		virtual public Languages.AccessLevel GetAccessLevel ()
			{
			Languages.AccessLevel accessLevel = Languages.AccessLevel.Unknown;

			TokenIterator iterator = start;

			while (iterator < end)
				{
				if (iterator.FundamentalType == FundamentalType.Text &&
					iterator.PrototypeParsingType == PrototypeParsingType.TypeModifier &&
					IsStandaloneWord(iterator))
					{
					if (iterator.MatchesToken("public"))
						{  accessLevel = Languages.AccessLevel.Public;  }
					else if (iterator.MatchesToken("private"))
						{  accessLevel = Languages.AccessLevel.Private;  }
					else if (iterator.MatchesToken("protected"))
						{
						if (accessLevel == Languages.AccessLevel.Internal)
							{  accessLevel = Languages.AccessLevel.ProtectedInternal;  }
						else
							{  accessLevel = Languages.AccessLevel.Protected;  }
						}
					else if (iterator.MatchesToken("internal"))
						{
						if (accessLevel == Languages.AccessLevel.Protected)
							{  accessLevel = Languages.AccessLevel.ProtectedInternal;  }
						else
							{  accessLevel = Languages.AccessLevel.Internal;  }
						}
					}

				iterator.Next();
				}

			return accessLevel;
			}


		/* Function: GetBaseType
		 * Returns the bounds of the base type if one is marked by <PrototypeParsingType.Type> tokens, or false if it couldn't find it.
		 * It will also include type qualifiers ("Package.Class") but exclude modifiers (so "unsigned int*[]" would just be "int".)
		 */
		virtual public bool GetBaseType (out TokenIterator baseTypeStart, out TokenIterator baseTypeEnd)
			{
			TokenIterator iterator = start;

			while (iterator < end && 
					  iterator.PrototypeParsingType != PrototypeParsingType.Type &&
					  iterator.PrototypeParsingType != PrototypeParsingType.TypeQualifier)
				{  iterator.Next();  }

			baseTypeStart = iterator;

			while (iterator.PrototypeParsingType == PrototypeParsingType.Type ||
					  iterator.PrototypeParsingType == PrototypeParsingType.TypeQualifier)
				{  iterator.Next();  }

			baseTypeEnd = iterator;

			return (baseTypeStart != baseTypeEnd);
			}


		/* Function: BuildFullType
		 * 
		 * Returns the full type if one is marked by <PrototypeParsingType.Type> tokens, combining all its modifiers and qualifiers into
		 * one continuous string.
		 * 
		 * If the type and all its modifiers and qualifiers are continuous in the original <Tokenizer> it will return that <Tokenizer> and
		 * <TokenIterators> based on it.
		 * 
		 * If the type and all its modifiers and qualifiers are not continuous it will create a separate <Tokenizer> to hold a continuous
		 * version of it.  It will return that <Tokenizer> and the bounds will be <TokenIterators> based on it rather than on the original 
		 * <Tokenizer>.  The new <Tokenizer> will still contain the same <PrototypeParsingTypes> and <SyntaxHighlightingTypes> of 
		 * the original.
		 */
		virtual public bool BuildFullType (out TokenIterator fullTypeStart, out TokenIterator fullTypeEnd, out Tokenizer fullTypeTokenizer)
			{

			// Find the first type token

			TokenIterator iterator = start;
			bool foundType = false;

			while (iterator < end &&
					 iterator.PrototypeParsingType != PrototypeParsingType.Type &&
					 iterator.PrototypeParsingType != PrototypeParsingType.TypeModifier &&
					 iterator.PrototypeParsingType != PrototypeParsingType.TypeQualifier &&
					 iterator.PrototypeParsingType != PrototypeParsingType.OpeningTypeModifier &&
					 iterator.PrototypeParsingType != PrototypeParsingType.ParamModifier &&
					 iterator.PrototypeParsingType != PrototypeParsingType.OpeningParamModifier)
				{  iterator.Next();  }

			fullTypeStart = iterator;


			// Find the rest of the type tokens that follow continuously

			while (iterator < end)
				{
				if (iterator.PrototypeParsingType == PrototypeParsingType.Type)
					{  
					foundType = true;  
					iterator.Next();
					}
				else if (iterator.PrototypeParsingType == PrototypeParsingType.TypeModifier ||
						  iterator.PrototypeParsingType == PrototypeParsingType.TypeQualifier ||
						  iterator.PrototypeParsingType == PrototypeParsingType.ParamModifier)
					{
					iterator.Next();
					}
				else if (TryToSkipModifierBlock(ref iterator))
					{
					}
				else if (iterator.FundamentalType == FundamentalType.Whitespace)
					{
					// We'll allow whitespace if it's followed by another type token
					TokenIterator lookahead = iterator;
					lookahead.Next();

					if (lookahead.PrototypeParsingType == PrototypeParsingType.Type ||
						lookahead.PrototypeParsingType == PrototypeParsingType.TypeModifier ||
						lookahead.PrototypeParsingType == PrototypeParsingType.TypeQualifier ||
						lookahead.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
						lookahead.PrototypeParsingType == PrototypeParsingType.ParamModifier ||
						lookahead.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier)
						{  
						iterator = lookahead;
						}
					else
						{  break;  }
					}
				else
					{  break;  }
				}

			fullTypeEnd = iterator;


			// See if there are any more type tokens past the continuous part

			bool continuous = true;

			while (iterator < end)
				{
				if (iterator.PrototypeParsingType == PrototypeParsingType.Type)
					{  
					foundType = true;
					continuous = false;
					}
				else if (iterator.PrototypeParsingType == PrototypeParsingType.TypeModifier ||
						  iterator.PrototypeParsingType == PrototypeParsingType.TypeQualifier ||
						  iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
						  iterator.PrototypeParsingType == PrototypeParsingType.ParamModifier ||
						  iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier)
					{
					continuous = false;
					}

				// If we already found a type and know it's not continuous we can quit early
				if (foundType && !continuous)
					{  break;  }

				iterator.Next();
				}

			
			// If we didn't find a type we're done.

			if (!foundType)
				{
				fullTypeTokenizer = start.Tokenizer;
				fullTypeStart = end;
				fullTypeEnd = end;
				return false;
				}


			// If it's continuous, see if the spacing matches what we would have built on our own.  We want the result to
			// always be consistent.

			bool acceptableSpacing = true;

			if (continuous)
				{  acceptableSpacing = TypeBuilder.HasSimilarSpacing(fullTypeStart, fullTypeEnd);  }


			// Return the continuous one if it's acceptable or build a new one if it's not.

			if (continuous && acceptableSpacing)
				{  
				fullTypeTokenizer = start.Tokenizer;
				// fullTypeStart is already set
				// fullTypeEnd is already set

				#if DEBUG
				// Test that this returns the same thing BuildFullType() would have.
				Tokenizer tempTokenizer = BuildFullType();

				if (fullTypeEnd.RawTextIndex - fullTypeStart.RawTextIndex != tempTokenizer.RawText.Length ||
					!fullTypeStart.MatchesAcrossTokens(tempTokenizer.RawText))
					{
					throw new Exception("Continuous and built types don't match: \"" + 
													fullTypeTokenizer.RawText.Substring(fullTypeStart.RawTextIndex, fullTypeEnd.RawTextIndex - fullTypeStart.RawTextIndex) +
													"\", \"" + tempTokenizer.RawText + "\"");
					}
				#endif
		
				return true;  
				}
			else
				{
				fullTypeTokenizer = BuildFullType();

				#if DEBUG
				// Test that the call to BuildFullType() was necessary and not a quirk in our spacing detection logic.
				// fullTypeStart and fullTypeEnd are still set to the original tokenizer.

				if (continuous && !acceptableSpacing &&
					fullTypeEnd.RawTextIndex - fullTypeStart.RawTextIndex == fullTypeTokenizer.RawText.Length &&
					fullTypeStart.MatchesAcrossTokens(fullTypeTokenizer.RawText))
					{
					throw new Exception("Built type matches continuous, building was unnecessary: \"" + fullTypeTokenizer.RawText + "\"");
					}
				#endif

				fullTypeStart = fullTypeTokenizer.FirstToken;
				fullTypeEnd = fullTypeTokenizer.LastToken;

				return true;
				}
			}


		/* Function: GetDefaultValue
		 * Returns the bounds of the default value as marked by <PrototypeParsingType.DefaultValueSeparator> and 
		 * <PrototypeParsingType.DefaultValue, or false if it couldn't find it.
		 */
		virtual public bool GetDefaultValue (out TokenIterator defaultValueStart, out TokenIterator defaultValueEnd)
			{
			TokenIterator iterator = start;

			while (iterator < end && 
					 iterator.PrototypeParsingType != PrototypeParsingType.DefaultValue)
				{  iterator.Next();  }

			defaultValueStart = iterator;

			while (iterator.PrototypeParsingType == PrototypeParsingType.DefaultValue)
				{  iterator.Next();  }

			defaultValueEnd = iterator;

			return (defaultValueStart != defaultValueEnd);
			}



		// Group: Protected Functions
		// __________________________________________________________________________


		/* Function: IsStandaloneWord
		 * Returns whether the iterator is on a text token and the tokens immediately before and after it are not underscores.
		 */
		protected bool IsStandaloneWord (TokenIterator iterator)
			{
			if (iterator.FundamentalType != FundamentalType.Text)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.FundamentalType == FundamentalType.Text ||
				lookahead.Character == '_')
				{  return false;  }

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			if (lookbehind.FundamentalType == FundamentalType.Text ||
				lookbehind.Character == '_')
				{  return false;  }

			return true;
			}


		/* Function: GetClosingModifier
		 * If the iterator is on a <PrototypeParsingType.OpeningTypeModifier> or <PrototypeParsingType.OpeningParamModifier>
		 * token, returns a reference to the closing token.  It will handle any nested blocks.  If the iterator isn't on an appropriate
		 * token or it couldn't find the end of the block, returns false.
		 */
		protected bool GetClosingModifier (TokenIterator openingModifier, out TokenIterator closingModifier)
			{
			if (openingModifier.PrototypeParsingType != PrototypeParsingType.OpeningTypeModifier &&
				openingModifier.PrototypeParsingType != PrototypeParsingType.OpeningParamModifier)
				{
				closingModifier = openingModifier;
				return false;
				}

			closingModifier = openingModifier;
			closingModifier.Next();
			int level = 1;

			// We're going to cheat and assume all blocks are balanced and nested in a way that makes sense. This lets us handle 
			// both in a simple loop.
			for (;;)
				{
				if (closingModifier >= end)
					{
					closingModifier = openingModifier;
					return false;
					}
				else if (closingModifier.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
						  closingModifier.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier)
					{  
					level++;  
					}
				else if (closingModifier.PrototypeParsingType == PrototypeParsingType.ClosingTypeModifier ||
						  closingModifier.PrototypeParsingType == PrototypeParsingType.ClosingParamModifier)
					{  
					level--;

					if (level == 0)
						{  return true;  }
					}

				closingModifier.Next();
				}
			}

			
		/* Function: TryToSkipModifierBlock
		 * If the iterator is on a <PrototypeParsingType.OpeningTypeModifier> or <PrototypeParsingType.OpeningParamModifier>
		 * token, moves the iterator past the entire block including any nested blocks.
		 */
		protected bool TryToSkipModifierBlock (ref TokenIterator iterator)
			{
			TokenIterator closingModifier;

			if (GetClosingModifier(iterator, out closingModifier))
				{
				iterator = closingModifier;
				iterator.Next();
				return true;
				}
			else
				{  return false;  }
			}

			
		/* Function: BuildFullType
		 * Creates a new <Tokenizer> for the variable type, including all modifiers, even if they are not continuous.  This is a support 
		 * function for <BuildFullType(TokenIterator, TokenIterator, Tokenizer)> and it always builds a new <Tokenizer>.
		 */
		protected Tokenizer BuildFullType ()
			{
			TokenIterator iterator = start;
			TypeBuilder typeBuilder = new TypeBuilder(end.RawTextIndex - start.RawTextIndex, end.TokenIndex - start.TokenIndex);

			while (iterator < end)
				{
				if (iterator.PrototypeParsingType == PrototypeParsingType.Type ||
					iterator.PrototypeParsingType == PrototypeParsingType.TypeModifier ||
					iterator.PrototypeParsingType == PrototypeParsingType.TypeQualifier ||
					iterator.PrototypeParsingType == PrototypeParsingType.ParamModifier)
					{
					typeBuilder.AddToken(iterator);
					}
				else if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
						  iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier)
					{
					TokenIterator closingModifier;
					GetClosingModifier(iterator, out closingModifier);

					typeBuilder.AddModifierBlock(iterator, closingModifier);

					iterator = closingModifier;
					}

				iterator.Next();
				}

			return typeBuilder.ToTokenizer();
			}



		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Tokenizer
		 */
		public Tokenizer Tokenizer
			{
			get
				{  return start.Tokenizer;  }
			}


		/* Property: Start
		 */
		public TokenIterator Start
			{
			get
				{  return start;  }
			}


		/* Property: End
		 */
		public TokenIterator End
			{
			get
				{  return end;  }
			}


		/* Property: HasType
		 * Whether the section defines a type by containing a <PrototypeParsingType.Type> token.
		 */
		public bool HasType
			{
			get
				{
				TokenIterator iterator = start;

				while (iterator < end)
					{
					if (iterator.PrototypeParsingType == PrototypeParsingType.Type)
						{  return true;  }

					iterator.Next();
					}

				return false;
				}
			}


		
		// Group: Variables
		// __________________________________________________________________________
		

		/* var: start
		 * The first token of the section.
		 */
		protected TokenIterator start;

		/* var: end
		 * One past the last token of the section.
		 */
		protected TokenIterator end;
		}
	}