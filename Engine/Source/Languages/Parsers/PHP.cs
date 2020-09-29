/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.PHP
 * ____________________________________________________________________________
 * 
 * Additional language support for PHP.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
{
	public class PHP : Language
		{

		// Group: Functions
		// __________________________________________________________________________
		

		/* Constructor: PHP
		 */
		public PHP (Languages.Manager manager) : base (manager, "PHP")
			{
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */
		override public ParsedPrototype ParsePrototype (string stringPrototype, int commentTypeID)
			{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator iterator = tokenizedPrototype.FirstToken;

			ParsedPrototype parsedPrototype;


			// Search for the first opening parenthesis.

			while (iterator.IsInBounds)
				{
				if (iterator.Character == '(')
					{  break;  }
				else if (TryToSkipComment(ref iterator) ||
						   TryToSkipString(ref iterator))
					{  }
				else
					{  iterator.Next();  }
				}


			// If we found parentheses, it's a function prototype.  Mark the delimiters.

			if (iterator.Character == '(')
				{
				iterator.PrototypeParsingType = PrototypeParsingType.StartOfParams;
				iterator.Next();

				while (iterator.IsInBounds)
					{
					if (iterator.Character == ',')
						{
						iterator.PrototypeParsingType = PrototypeParsingType.ParamSeparator;
						iterator.Next();
						}

					else if (iterator.Character == ')')
						{
						iterator.PrototypeParsingType = PrototypeParsingType.EndOfParams;
						break;
						}

					// Unlike prototype detection, here we treat < as an opening bracket.  Since we're already in the parameter list
					// we shouldn't run into it as part of an operator overload, and we need it to not treat the comma in "template<a,b>"
					// as a parameter divider.
					else if (TryToSkipComment(ref iterator) || 
							   TryToSkipString(ref iterator) ||
							   TryToSkipBlock(ref iterator, true))
						{  }

					else
						{  iterator.Next();  }
					}


				// We have enough tokens marked to create the parsed prototype.  This will also let us iterate through the parameters
				// easily.

				parsedPrototype = new ParsedPrototype(tokenizedPrototype, supportsImpliedTypes: false);


				// Mark the part before the parameters, which includes the name and return value.

				TokenIterator start, end;
				parsedPrototype.GetBeforeParameters(out start, out end);

				// Exclude the opening bracket
				end.Previous();
				end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

				if (start < end)
					{  MarkParameter(start, end);  }


				// If there are any parameters, mark the tokens in them.

				if (parsedPrototype.NumberOfParameters > 0)
					{
					for (int i = 0; i < parsedPrototype.NumberOfParameters; i++)
						{
						parsedPrototype.GetParameter(i, out start, out end);
						MarkParameter(start, end);
						}
					}
				}

			
			// If there's no brackets, it's a variable, property, or class.

			else
				{
				parsedPrototype = new ParsedPrototype(tokenizedPrototype, supportsImpliedTypes: false);
				TokenIterator start = tokenizedPrototype.FirstToken;
				TokenIterator end = tokenizedPrototype.LastToken;

				MarkParameter(start, end);
				}

			return parsedPrototype;
			}


		/* Function: MarkParameter
		 * Marks the tokens in the parameter specified by the bounds with <PrototypeParsingTypes>.
		 */
		protected void MarkParameter (TokenIterator start, TokenIterator end)
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