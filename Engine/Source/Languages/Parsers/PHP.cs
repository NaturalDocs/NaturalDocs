/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.PHP
 * ____________________________________________________________________________
 *
 * Additional language support for PHP.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
{
	public class PHP : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: PHP
		 */
		public PHP (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: MarkParameter
		 * Marks the tokens in the parameter specified by the bounds with <PrototypeParsingTypes>.
		 */
		override protected void MarkParameter (TokenIterator start, TokenIterator end, ParameterStyle parameterStyle = ParameterStyle.Unknown)
			{
			// Pass 1: Count the number of "words" in the parameter prior to the default value and mark the default value
			// separator.  We'll figure out how to interpret the words in the second pass.

			int words = 0;
			TokenIterator iterator = start;

			while (iterator < end)
				{

				// Default values

				if (iterator.Character == '=')
					{
					iterator.PrototypeParsingType = PrototypeParsingType.DefaultValueSeparator;
					iterator.Next();

					iterator.NextPastWhitespace(end);
					TokenIterator endOfDefaultValue = end;

					TokenIterator lookbehind = endOfDefaultValue;
					lookbehind.Previous();

					while (lookbehind >= iterator && lookbehind.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
						{
						endOfDefaultValue = lookbehind;
						lookbehind.Previous();
						}

					endOfDefaultValue.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, iterator);

					if (iterator < endOfDefaultValue)
						{  iterator.SetPrototypeParsingTypeBetween(endOfDefaultValue, PrototypeParsingType.DefaultValue);  }

					break;
					}


				// Param separator

				else if (iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
					{  break;  }


				// "Words" we're interested in

				else if (TryToSkipTypeOrVarName(ref iterator, end) ||
						   TryToSkipComment(ref iterator) ||
						   TryToSkipString(ref iterator) ||
						   TryToSkipBlock(ref iterator, true))
					{
					// If there was a comment in the prototype, that means it specifically wasn't filtered out because it was something
					// significant like a Splint comment or /*out*/.  Treat it like a modifier.

					// Strings don't really make sense in the prototype until the default value, but we need the parser to handle it anyway
					// just so it doesn't lose its mind if one occurs.

					// If we come across a block that doesn't immediately follow an identifier, it may be something like a C# property so
					// treat it as a modifier.

					words++;
					}


				// Whitespace and any unexpected random symbols

				else
					{  iterator.Next();  }
				}


			// Pass 2: Mark the "words" we counted from the first pass.  The order of words goes [modifier] [modifier] [type] [name],
			// starting from the right.  Typeless languages that only have one word will have it correctly interpreted as the name.

			iterator = start;
			TokenIterator wordStart, wordEnd;

			while (iterator < end)
				{
				wordStart = iterator;
				bool foundWord = false;
				bool foundBlock = false;

				if (iterator.PrototypeParsingType == PrototypeParsingType.DefaultValueSeparator ||
					iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
					{
					break;
					}
				else if (TryToSkipTypeOrVarName(ref iterator, end))
					{
					foundWord = true;
					}
				else if (TryToSkipComment(ref iterator) ||
						  TryToSkipString(ref iterator) ||
						  TryToSkipBlock(ref iterator, true))
					{
					foundWord = true;
					foundBlock = true;
					}
				else
					{
					iterator.Next();
					}

				// Process the word we found
				if (foundWord)
					{
					wordEnd = iterator;

					if (words >= 3)
						{
						if (foundBlock && wordEnd.TokenIndex - wordStart.TokenIndex >= 2)
							{
							wordStart.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;

							TokenIterator lookbehind = wordEnd;
							lookbehind.Previous();
							lookbehind.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
							}
						else
							{
							wordStart.SetPrototypeParsingTypeBetween(wordEnd, PrototypeParsingType.TypeModifier);
							}
						}
					else if (words == 2)
						{  MarkType(wordStart, wordEnd);  }
					else if (words == 1)
						{
						MarkName(wordStart, wordEnd);

						// Change the $ at the beginning of the name from a param modifier to part of the name
						if (wordStart.Character == '$')
							{  wordStart.PrototypeParsingType = PrototypeParsingType.Name;  }
						}

					words--;
					}
				}
			}

		}
	}
