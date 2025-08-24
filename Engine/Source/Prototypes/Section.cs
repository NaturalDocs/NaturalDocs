/*
 * Class: CodeClear.NaturalDocs.Engine.Prototypes.Section
 * ____________________________________________________________________________
 *
 * A class that wraps a section of a <Tokenizer> which has been marked with <PrototypeParsingTypes>.  Provides basic
 * functionality that will be common to many sections.
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
					  iterator.PrototypeParsingType != PrototypeParsingType.Name &&
					  iterator.PrototypeParsingType != PrototypeParsingType.KeywordName)
				{  iterator.Next();  }

			if (iterator.PrototypeParsingType == PrototypeParsingType.Name ||
				iterator.PrototypeParsingType == PrototypeParsingType.KeywordName)
				{
				nameStart = iterator;

				do
					{  iterator.Next();  }
				while (iterator.PrototypeParsingType == PrototypeParsingType.Name ||
						  iterator.PrototypeParsingType == PrototypeParsingType.KeywordName);

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
						{
						if (accessLevel == Languages.AccessLevel.Protected)
							{  accessLevel = Languages.AccessLevel.PrivateProtected;  }
						else
							{  accessLevel = Languages.AccessLevel.Private;  }
						}
					else if (iterator.MatchesToken("protected"))
						{
						if (accessLevel == Languages.AccessLevel.Internal)
							{  accessLevel = Languages.AccessLevel.ProtectedInternal;  }
						else if (accessLevel == Languages.AccessLevel.Private)
							{  accessLevel = Languages.AccessLevel.PrivateProtected;  }
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
					  iterator.PrototypeParsingType != PrototypeParsingType.TypeQualifier &&
					  iterator.PrototypeParsingType != PrototypeParsingType.StartOfTuple)
				{  iterator.Next();  }

			baseTypeStart = iterator;

			// Only advance if it's not on a tuple.  We return false for tuples.
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
		 * If the type and all its modifiers and qualifiers are continuous in the original <Tokenizer> it will return <TokenIterators> based
		 * on it.  However, if the type and all its modifiers and qualifiers are NOT continuous it will create a separate <Tokenizer> to hold
		 * a continuous version of it.  The returned bounds will be <TokenIterators> based on that rather than on the original <Tokenizer>.
		 * The new <Tokenizer> will still contain the same <PrototypeParsingTypes> and <SyntaxHighlightingTypes> of the original.
		 */
		virtual public bool BuildFullType (out TokenIterator fullTypeStart, out TokenIterator fullTypeEnd)
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
					 iterator.PrototypeParsingType != PrototypeParsingType.OpeningParamModifier &&
					 iterator.PrototypeParsingType != PrototypeParsingType.StartOfTuple)
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
				else if (iterator.PrototypeParsingType == PrototypeParsingType.StartOfTuple &&
						   ParsedPrototype.TryToSkipBlock(ref iterator, end))
					{
					foundType = true;
					}
				else if (ParsedPrototype.TryToSkipBlock(ref iterator, end))
					{
					// Other non-tuple blocks like OpeningTypeModifier and OpeningParamModifier
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
						lookahead.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier ||
						lookahead.PrototypeParsingType == PrototypeParsingType.StartOfTuple)
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
				if (iterator.PrototypeParsingType == PrototypeParsingType.Type ||
					iterator.PrototypeParsingType == PrototypeParsingType.StartOfTuple)
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
				// fullTypeStart is already set
				// fullTypeEnd is already set

				#if DEBUG
				// Test that this returns the same thing BuildFullType() would have.
				Tokenizer tempTokenizer = BuildFullType();

				if (fullTypeStart.TextBetween(fullTypeEnd) != tempTokenizer.RawText)
					{
					throw new Exception("Continuous and built types don't match.  Continuous: \"" + fullTypeStart.TextBetween(fullTypeEnd) +
													"\", TypeBuilder: \"" + tempTokenizer.RawText + "\"");
					}
				#endif

				return true;
				}
			else
				{
				Tokenizer fullTypeTokenizer = BuildFullType();

				fullTypeStart = fullTypeTokenizer.FirstToken;
				fullTypeEnd = fullTypeTokenizer.EndOfTokens;

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
					iterator.Next();
					}
				else if (iterator.PrototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
						   iterator.PrototypeParsingType == PrototypeParsingType.OpeningParamModifier ||
						   iterator.PrototypeParsingType == PrototypeParsingType.StartOfTuple)
					{
					TokenIterator closingToken, endOfBlock;
					ParsedPrototype.GetEndOfBlock(iterator, end, out closingToken, out endOfBlock);

					typeBuilder.AddTokens(iterator, endOfBlock);

					iterator = endOfBlock;
					}
				else
					{
					iterator.Next();
					}
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
		 * Whether the section defines a type.
		 */
		public bool HasType
			{
			get
				{
				TokenIterator iterator = start;

				while (iterator < end)
					{
					if (iterator.PrototypeParsingType == PrototypeParsingType.Type ||
						iterator.PrototypeParsingType == PrototypeParsingType.StartOfTuple)
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
