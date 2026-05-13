/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Go
 * ____________________________________________________________________________
 *
 * Additional language support for Go.
 *
 * Language Version:
 *
 *		The parser is based on Go 1.26, the latest release as of April 2026.
 *
 * Resources:
 *		- <Docs Home: https://go.dev/doc/>
 *		- <Language Specification: https://go.dev/ref/spec>
 *		- <Playground: https://go.dev/play/>
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class Go : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Go
		 */
		public Go (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		override protected bool TryToFindBasicPrototype (Topic topic, TokenIterator start, TokenIterator limit,
																				out TokenIterator prototypeStart, out TokenIterator prototypeEnd)
			{
			bool result = base.TryToFindBasicPrototype(topic, start, limit, out prototypeStart, out prototypeEnd);

			if (result && prototypeStart.MatchesAcrossTokens("type "))
				{  TruncateTypeDef(prototypeStart, ref prototypeEnd);  }

			return result;
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */
		override public ParsedPrototype ParsePrototype(string stringPrototype, int commentTypeID)
			{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator iterator = tokenizedPrototype.FirstToken;

			if (TryToSkipFunction(ref iterator, ParseMode.ParsePrototype) ||
				TryToSkipTypeDef(ref iterator, ParseMode.ParsePrototype) ||
				TryToSkipVariable(ref iterator, ParseMode.ParsePrototype))
				{
				return new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID,
														 parameterStyle: ParameterStyle.Pascal,
														 supportsImpliedTypes: true);
				}
			else
				{
				return base.ParsePrototype(stringPrototype, commentTypeID);
				}
			}


		/* Function: ParseClassPrototype
		 * Converts a raw text prototype into a <ParsedClassPrototype>.  Will return null if it is not an appropriate prototype.
		 */
		override public ParsedClassPrototype ParseClassPrototype (string stringPrototype, int commentTypeID)
			{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator iterator = tokenizedPrototype.FirstToken;

			if (TryToSkipTypeDef(ref iterator, ParseMode.ParseClassPrototype))
				{
				return new ParsedClassPrototype(tokenizedPrototype);
				}
			else
				{
				return base.ParseClassPrototype(stringPrototype, commentTypeID);
				}
			}


		/* Function: TruncateTypeDef
		 * If the iterators are on the bounds of a typedef, adjusts the ending to omit the definition.  This is necessary because typedefs
		 * can either have an equals sign for the assignment or just a space, the latter of which can't be handled by prototype enders.
		 * This will have no effect on typedefs assigned structs or interfaces because we want to keep those keywords.
		 */
		protected bool TruncateTypeDef (TokenIterator startOfPrototype, ref TokenIterator endOfPrototype)
			{
			if (!IsOnKeyword(startOfPrototype, "type"))
				{  return false;  }

			TokenIterator lookahead = startOfPrototype;
			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Name

			if (!TryToSkipUnqualifiedIdentifier(ref lookahead))
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);


			// Template definition (optional)

			if (TryToSkipTemplateDefinition(ref lookahead))
				{  TryToSkipWhitespace(ref lookahead);  }

			TokenIterator newEndOfPrototype = lookahead;


			// Equals sign (optional)
			// Alias typedefs have an equals sign, but regular typedefs don't, they just go straight to the type after whitespace.

			if (lookahead.Character == '=')
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Type (optional)

			// Next is the type, which isn't optional in the code, but which may be stripped out of prototypes so we don't fail if we don't
			// encounter one.

			// Don't strip the type for structs and interfaces though, we need to keep the keyword.
			if (IsOnAnyKeyword(lookahead, "struct", "interface"))
				{  return false;  }


			endOfPrototype = newEndOfPrototype;
			return true;
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: IsOnKeyword
		 *
		 * Returns whether the <TokenIterator> is on the passed keyword, making sure there are no other identifier tokens
		 * before or after it.  This allows us to be sure an iterator on "input" isn't actually on "_input" or similar.  This function
		 * assumes keywords are only one text token.
		 *
		 * If you have multiple keywords to test against, it is more efficient to use one of the <IsOnAnyKeyword()> functions.
		 */
		public bool IsOnKeyword (TokenIterator iterator, string keyword)
			{
			if (!iterator.MatchesToken(keyword))
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.FundamentalType == FundamentalType.Text ||
				lookahead.Character == '_')
				{  return false;  }

			// Just use iterator as a lookbehind instead of creating another one
			iterator.Previous();

			if (iterator.FundamentalType == FundamentalType.Text ||
				iterator.Character == '_')
				{  return false;  }

			return true;
			}


		/* Function: IsOnAnyKeyword
		 *
		 * Returns whether the <TokenIterator> is on the passed keyword, making sure there are no other identifier tokens
		 * before or after it.  This allows us to be sure an iterator on "input" isn't actually on "_input" or similar.  This function
		 * assumes keywords are only one text token.
		 */
		public bool IsOnAnyKeyword (TokenIterator iterator, params string[] keywords)
			{
			if (iterator.MatchesAnyAcrossTokens(keywords, true) == -1)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.FundamentalType == FundamentalType.Text ||
				lookahead.Character == '_')
				{  return false;  }

			// Just use iterator as a lookbehind instead of creating another one
			iterator.Previous();

			if (iterator.FundamentalType == FundamentalType.Text ||
				iterator.Character == '_')
				{  return false;  }

			return true;
			}


		/* Function: IsOnAnyKeyword
		 *
		 * Returns whether the <TokenIterator> is on the passed keyword, making sure there are no other identifier tokens
		 * before or after it.  This allows us to be sure an iterator on "input" isn't actually on "_input" or similar.  This function
		 * assumes keywords are only one text token.
		 */
		public bool IsOnAnyKeyword (TokenIterator iterator, StringSet keywords)
			{
			if (iterator.FundamentalType != FundamentalType.Text ||
				keywords.Contains(iterator.String) == false)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.FundamentalType == FundamentalType.Text ||
				lookahead.Character == '_')
				{  return false;  }

			// Just use iterator as a lookbehind instead of creating another one
			iterator.Previous();

			if (iterator.FundamentalType == FundamentalType.Text ||
				iterator.Character == '_')
				{  return false;  }

			return true;
			}


		/* Function: TryToSkipFunction
		 *
		 * If the iterator is on a function definition, an interface function declaration, or a method with a receiver, moves it past it and
		 * returns true.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipFunction (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;


			// Keyword (optional)
			// Will appear for normal function and method definitions, but not when defining functions in interfaces.

			bool hasKeyword = IsOnKeyword(iterator, "func");

			if (hasKeyword)
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Receiver (optional)
			// A receiver is the "this" for extension functions.  Only available for function definitions with the "func" keyword, not
			// functions in interfaces.

			if (hasKeyword && lookahead.Character == '(')
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				// "this" variable name
				if (!TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				TryToSkipWhitespace(ref lookahead);

				// Must declare a type, unlike variables and parameters where it's optional
				if (!TryToSkipType(ref lookahead, mode))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				TryToSkipWhitespace(ref lookahead);

				// Can't have multiple parameters, must end now
				if (lookahead.Character != ')')
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Name

			if (!TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			// Template definition (optional)

			if (TryToSkipTemplateDefinition(ref lookahead, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Parameters

			if (lookahead.Character != '(')
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfParams;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			for (;;)
				{
				if (!lookahead.IsInBounds)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				else if (lookahead.Character == ',')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}

				else if (lookahead.Character == ')')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.EndOfParams;  }

					lookahead.Next();
					iterator = lookahead;
					TryToSkipWhitespace(ref lookahead);
					break;
					}

				else if (TryToSkipParameter(ref lookahead, mode))
					{
					TryToSkipWhitespace(ref lookahead);
					}

				else
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}
				}


			// Return value (optional)

			// Can return multiple values in paretheses.  Mark them as a tuple.
			if (lookahead.Character == '(')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfTuple;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				for (;;)
					{
					if (!lookahead.IsInBounds)
						{
						ResetTokensBetween(iterator, lookahead, mode);
						return false;
						}

					else if (lookahead.Character == ',')
						{
						if (mode == ParseMode.ParsePrototype)
							{  lookahead.PrototypeParsingType = PrototypeParsingType.TupleMemberSeparator;  }

						lookahead.Next();
						TryToSkipWhitespace(ref lookahead);
						}

					else if (lookahead.Character == ')')
						{
						if (mode == ParseMode.ParsePrototype)
							{  lookahead.PrototypeParsingType = PrototypeParsingType.EndOfTuple;  }

						lookahead.Next();

						iterator = lookahead;
						TryToSkipWhitespace(ref lookahead);
						break;
						}

					else if (TryToSkipReturnParameter(ref lookahead, mode))
						{
						TryToSkipWhitespace(ref lookahead);
						}

					else
						{
						ResetTokensBetween(iterator, lookahead, mode);
						return false;
						}
					}
				}

			// Single type return value
			else if (TryToSkipType(ref lookahead, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			//  Body (optional)

			// Since this function is currently only being used to parse prototypes, and prototypes don't have the body included, we can omit
			// this step.  Documenting it here in case this changes later though.


			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipParameter
		 *
		 * If the iterator is on a parameter, moves it past it and returns true.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipParameter (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;


			// First try "Name" and "Name Type" since those are the most common cases.  Type is optional.

			bool success = true;

			if (!IsOnAnyKeyword(lookahead, goPrimitiveTypes) &&
				TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
				{  TryToSkipWhitespace(ref lookahead);  }
			else
				{  success = false;  }

			// "..." can appear before a variadic type, the equivalent of "params" in C#
			if (success &&
				lookahead.MatchesAcrossTokens("..."))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.ParamModifier, 3);  }

				lookahead.Next(3);
				TryToSkipWhitespace(ref lookahead);
				}

			if (success &&
				TryToSkipType(ref lookahead, mode))
				{  TryToSkipWhitespace(ref lookahead);  }

			if (lookahead.IsInBounds &&
				lookahead.Character != '=' &&
				lookahead.Character != ',' &&
				lookahead.Character != ')')
				{  success = false;  }


			// If that didn't work, only having the type is also an option.  We try that second since it should be uncommon and plenty
			// of identifiers can be interpreted as either a name or a type, so we default to the common case and use this as a fallback.

			if (!success)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				lookahead = iterator;
				success = true;

				if (lookahead.MatchesAcrossTokens("..."))
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.ParamModifier, 3);  }

					lookahead.Next(3);
					TryToSkipWhitespace(ref lookahead);
					}

				if (TryToSkipType(ref lookahead, mode))
					{  TryToSkipWhitespace(ref lookahead);  }
				else
					{  success = false;  }

				if (lookahead.IsInBounds &&
					lookahead.Character != '=' &&
					lookahead.Character != ',' &&
					lookahead.Character != ')')
					{  success = false;  }
				}


			// If that didn't work either we're done.

			if (!success)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}


			// Default value (optional)

			if (lookahead.Character == '=')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.DefaultValueSeparator;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				TokenIterator startOfDefaultValue = lookahead;

				while (lookahead.IsInBounds &&
						 lookahead.Character != ',' &&
						 lookahead.Character != ')')
					{
					GenericSkip(ref lookahead);
										}

				if (mode == ParseMode.ParsePrototype)
					{  startOfDefaultValue.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.DefaultValue);  }
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipReturnParameter
		 *
		 * If the iterator is on a parameter in a return value parenthetical, moves it past it and returns true.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Members will be marked as <PrototypeParsingType.Type> and <PrototypeParsingType.TupleMemberName>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipReturnParameter (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;


			// First try just the type.  Unlike a function parameter, return parameters are more likely to just be types so a single identifier
			// should default to being interpreted as one.

			bool success = true;

			if (TryToSkipType(ref lookahead, mode))
				{  TryToSkipWhitespace(ref lookahead);  }
			else
				{  success = false;  }

			if (lookahead.IsInBounds &&
				lookahead.Character != ',' &&
				lookahead.Character != ')')
				{  success = false;  }


			// If that didn't work, try interpreting it as a name and type.

			if (!success)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				lookahead = iterator;
				success = true;

				if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.TupleMemberName))
					{  TryToSkipWhitespace(ref lookahead);  }
				else
					{  success = false;  }

				if (success &&
					TryToSkipType(ref lookahead, mode))
					{  TryToSkipWhitespace(ref lookahead);  }
				else
					{  success = false;  }

				if (lookahead.IsInBounds &&
					lookahead.Character != ',' &&
					lookahead.Character != ')')
					{  success = false;  }
				}


			// If that didn't work either we're done.

			if (!success)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}


			// No default values allowed so we're done.

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipTemplateDefinition
		 *
		 * If the iterator is on a type's template definition, meaning the part in square brackets, moves the iterator past it and returns true.
		 * The iterator must be on the opening bracket.  This function handles a parameterized type's template definition and its constraints,
		 * such as in "TypeName[T1 ~int]", and not an instantiation of one with specific parameters, such as in "TypeName[int32]".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- <ParseMode.ParseClassPrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipTemplateDefinition (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '[')
				{  return false;  }

			TokenIterator lookahead = iterator;

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			for (;;)
				{
				if (!lookahead.IsInBounds)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				else if (lookahead.Character == ']')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;  }

					lookahead.Next();

					if (mode == ParseMode.ParseClassPrototype)
						{  iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.TemplateSuffix);  }

					iterator = lookahead;
					return true;
					}

				else if (lookahead.Character == ',')
					{
					// Don't mark it as a parameter separator since we're not marking this as a parameter section
					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}

				else if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
					{
					TryToSkipWhitespace(ref lookahead);

					for (;;)
						{
						if (!lookahead.IsInBounds)
							{
							ResetTokensBetween(iterator, lookahead, mode);
							return false;
							}

						else if (lookahead.Character == '~' ||
								   lookahead.Character == '|')
							{
							lookahead.Next();
							TryToSkipWhitespace(ref lookahead);
							}

						else if (IsOnAnyKeyword(lookahead, "struct", "interface"))
							{
							if (mode == ParseMode.ParsePrototype)
								{  lookahead.PrototypeParsingType = PrototypeParsingType.Type;  }

							lookahead.Next();
							TryToSkipWhitespace(ref lookahead);

							if (lookahead.Character != '{')
								{
								ResetTokensBetween(iterator, lookahead, mode);
								return false;
								}

							if (mode == ParseMode.ParsePrototype)
								{  lookahead.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;  }

							lookahead.Next();

							// We're not going to parse into their bodies, so just skip them
							if (!GenericSkipUntilOn(ref lookahead, '}', skipToEndIfNotFound: false))
								{
								ResetTokensBetween(iterator, lookahead, mode);
								return false;
								}

							if (mode == ParseMode.ParsePrototype)
								{  lookahead.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;  }

							lookahead.Next();
							TryToSkipWhitespace(ref lookahead);
							}

						else if (TryToSkipType(ref lookahead, mode))
							{
							TryToSkipWhitespace(ref lookahead);
							}

						else if (lookahead.Character == ',' ||
								   lookahead.Character == ']')
							{
							// Don't skip the token, we want to process it in the above loop
							break;
							}

						else
							{
							// Tolerate other things in here we haven't anticipated.
							GenericSkip(ref lookahead);
							}
						}
					}

				else
					{
					// Tolerate other things in here we haven't anticipated.
					GenericSkip(ref lookahead);
					}
				}
			}


		/* Function: TryToSkipVariable
		 *
		 * If the iterator is on a variable or constant, moves it past it and returns true.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipVariable (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.FundamentalType != FundamentalType.Text &&
				iterator.Character != '_')
				{  return false;  }

			if (IsOnAnyKeyword(iterator, "func", "type"))
				{  return false;  }


			// Keyword (optional)
			// "var" isn't needed for variables defined with :=.  "const" is always needed for constants.

			TokenIterator lookahead = iterator;

			if (IsOnAnyKeyword(lookahead, "var", "const"))
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Name

			if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
				{
				iterator = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}
			else
				{  return false;  }


			// Type (optional)

			if (TryToSkipType(ref lookahead, mode))
				{
				iterator = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}


			//  Default value (optional)

			// Since this function is currently only being used to parse prototypes, and prototypes don't have the default value included,
			// we can omit this step.  Documenting it here in case this changes later though.


			return true;
			}


		/* Function: TryToSkipTypeDef
		 *
		 * Tries to move the iterator past a type definition, such as "type MyType = int32" or "type MyType struct { ... }".  This covers simple
		 * typedefs, alias typedefs, structs, and interfaces since this is how named structs and interfaces are defined.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- <ParseMode.ParseClassPrototype>
		 *			- This will only succeed if the typedef is assigned a struct or interface.  It will return false otherwise.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipTypeDef (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (!IsOnKeyword(iterator, "type"))
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Name

			if (!TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			// Template definition (optional)

			if (TryToSkipTemplateDefinition(ref lookahead, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Equals sign (optional)
			// Alias typedefs have an equals sign, but regular typedefs don't, they just go straight to the type after whitespace.

			if (lookahead.Character == '=')
				{
				// Not treating it as a default value separator
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Type (optional)

			// Next is the type, which isn't optional in the code, but which may be stripped out of prototypes so we don't fail if we don't
			// encounter one.

			if (mode == ParseMode.ParseClassPrototype)
				{
				if (IsOnAnyKeyword(lookahead, "struct", "interface") == false)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.Keyword;
				}
			else
				{  TryToSkipType(ref lookahead, mode);  }


			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipType
		 *
		 * Tries to move the iterator past a type, such as "int", "[16]byte", "TypeName[string, string]", or "map[uint16]string".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipType (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.FundamentalType != FundamentalType.Text &&
				iterator.Character != '_' &&
				iterator.Character != '[' &&
				iterator.Character != '*' &&
				iterator.Character != '<')
				{  return false;  }


			// Arrays, slices, and pointers

			TokenIterator lookahead = iterator;

			while (lookahead.IsInBounds)
				{
				if (lookahead.Character == '*')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				else if (lookahead.Character == '[')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;  }

					TokenIterator endOfBlock = lookahead;
					endOfBlock.Next();

					if (!GenericSkipUntilOn(ref endOfBlock, ']', skipToEndIfNotFound: false))
						{
						ResetTokensBetween(iterator, endOfBlock, mode);
						return false;
						}

					if (mode == ParseMode.ParsePrototype)
						{  endOfBlock.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;  }

					lookahead = endOfBlock;
					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{  break;  }
				}


			// Maps

			if (IsOnKeyword(lookahead, "map"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.Type;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character != '[')
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (!TryToSkipType(ref lookahead, mode))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character != ']')
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (!TryToSkipType(ref lookahead, mode))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				iterator = lookahead;
				return true;
				}


			// Channels

			else if (IsOnKeyword(lookahead, "chan") ||
					   lookahead.MatchesAcrossTokens("<-"))
				{
				if (lookahead.MatchesAcrossTokens("<-"))
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.TypeModifier, 2);  }

					lookahead.Next(2);
					TryToSkipWhitespace(ref lookahead);
					}

				if (IsOnKeyword(lookahead, "chan"))
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.Type;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				if (lookahead.MatchesAcrossTokens("<-"))
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.TypeModifier, 2);  }

					lookahead.Next(2);
					TryToSkipWhitespace(ref lookahead);
					}

				if (!TryToSkipType(ref lookahead, mode))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				iterator = lookahead;
				return true;
				}


			// Inline structs and interfaces

			else if (IsOnAnyKeyword(lookahead, "struct", "interface"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.Type;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				// We may be parsing a prototype that has the body stripped off, such as "type TypeName struct { ... }", so we don't treat the
				// body being missing as a failure so long as there's no content left.
				if (lookahead.IsInBounds)
					{
					if (lookahead.Character != '{')
						{
						ResetTokensBetween(iterator, lookahead, mode);
						return false;
						}

					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;  }

					lookahead.Next();

					if (!GenericSkipUntilOn(ref lookahead, '}', skipToEndIfNotFound: false))
						{
						ResetTokensBetween(iterator, lookahead, mode);
						return false;
						}

					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;  }

					lookahead.Next();
					}

				iterator = lookahead;
				return true;
				}


			// All other types

			else
				{
				if (!TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Type))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				TokenIterator endOfType = lookahead;

				TryToSkipWhitespace(ref lookahead);


				// Template parameters (optional)

				if (lookahead.Character == '[')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);

					while (lookahead.IsInBounds)
						{
						if (lookahead.Character == ']')
							{
							if (mode == ParseMode.ParsePrototype)
								{  lookahead.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;  }

							lookahead.Next();
							endOfType = lookahead;
							break;
							}
						else if (lookahead.Character == ',')
							{
							lookahead.Next();
							TryToSkipWhitespace(ref lookahead);
							}
						else if (TryToSkipType(ref lookahead, mode))
							{
							TryToSkipWhitespace(ref lookahead);
							}
						else
							{  break;  }
						}
					}

				if (lookahead > endOfType)
					{  ResetTokensBetween(endOfType, lookahead, mode);  }

				iterator = endOfType;
				return true;
				}
			}


		/* Function: TryToSkipKeyword
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipKeyword (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			// No keywords have underscores, so they must be text and only one token long.

			if (iterator.FundamentalType != FundamentalType.Text)
				{  return false;  }

			if (goKeywords.Contains(iterator.ToString()) == false)
				{  return false;  }

			// Check if it's part of another identifier like "x_keyword".

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			if (lookbehind.Character == '_' || lookbehind.FundamentalType == FundamentalType.Text)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.Character == '_' || lookahead.FundamentalType == FundamentalType.Text)
				{  return false;  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

			iterator.Next();
			return true;
			}


		/* Function: TryToSkipString
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipString (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			char character = iterator.Character;

			if (character != '\'' && character != '\"' && character != '`')
				{  return false;  }

			char delimiter = character;

			TokenIterator lookahead = iterator;
			lookahead.Next();

			while (lookahead.IsInBounds)
				{
				if (lookahead.Character == delimiter)
					{
					lookahead.Next();
					break;
					}
				else if (lookahead.Character == '\\' && delimiter != '`')
					{  lookahead.Next(2);  }
				else
					{  lookahead.Next();  }
				}

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.String);  }

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipNumber
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipNumber (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			return TryToSkipNumber(ref iterator,
												ParseNumberFlags.AllowUnderscoreSeparators |
												ParseNumberFlags.AllowHexFloats,
												// Doesn't require digit after dot
												mode);
			}



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: goKeywords
		 */
		static protected StringSet goKeywords = new StringSet (KeySettings.Literal, new string[] {

			// Keywords
			"break", "case", "chan", "const", "continue", "default", "defer", "else", "fallthrough", "for", "func", "go", "goto",
			"if", "import", "interface", "map", "package", "range", "return", "select", "struct", "switch", "type", "var",

			// Types
			"int", "int8", "int16", "int32", "int64", "uint", "uint8", "uint16", "uint32", "uint64", "float32", "float64",
			"complex64", "complex128", "byte", "rune", "string", "bool", "uintptr", "any",

			// Values
			"true", "false", "nil", "iota",

			// Predeclared identifiers
			"comparable", "error"

			});

		/* var: goPrimitiveTypes
		 */
		static protected StringSet goPrimitiveTypes = new StringSet (KeySettings.Literal, new string[] {

			"int", "int8", "int16", "int32", "int64", "uint", "uint8", "uint16", "uint32", "uint64", "float32", "float64",
			"complex64", "complex128", "byte", "rune", "string", "bool", "uintptr", "any",

			// Also include these since we're using this to detect types that shouldn't be interpreted as identifiers
			"map", "chan", "struct", "interface"

			});

		}
	}
