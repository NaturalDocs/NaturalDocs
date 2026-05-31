/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.TypeScript
 * ____________________________________________________________________________
 *
 * Additional language support for TypeScript.
 *
 * Language Version:
 *
 *		The parser is based on TypeScript 6.0, the latest release as of May 2026.
 *
 * Resources:
 *		- <Docs Home: https://www.typescriptlang.org/docs/>
 *			- No formal BNF-like language specification
 *		- <Playground: https://www.typescriptlang.org/play/>
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
	public class TypeScript : Parsers.JavaScript
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: TemplateSignatureType
		 * Definition - The signature of a template definition, such as "class MyTemplate<X, Y> { ... }".
		 * Instantiation - The signature of a template instantiation, such as "MyTemplate<number, string> x = null;".
		 */
		public enum TemplateSignatureType: byte
			{  Definition, Instantiation  }



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: TypeScript
		 */
		public TypeScript (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: TryToFindBasicPrototype
		 */
		override protected bool TryToFindBasicPrototype (Topic topic, TokenIterator start, TokenIterator limit,
																			   out TokenIterator prototypeStart, out TokenIterator prototypeEnd)
			{
			// Skip the decorators before each item.  This allows us to ignore their line breaks where line breaks are otherwise
			// prototype enders.  As a bonus it also prevents the decorators from being matched against the topic title.
			TokenIterator lookahead = start;

			TryToSkipWhitespace(ref lookahead, mode: ParseMode.ParsePrototype);
			if (TryToSkipDecorators(ref lookahead, mode: ParseMode.ParsePrototype, breakPrototypeSections: false))
				{  TryToSkipWhitespace(ref lookahead, mode: ParseMode.ParsePrototype);  }

			// Use the base implementation but set treatAngleBracketsAsBlocks to true so inline object types in template signatures
			// (like "<T extends { x: number }>") don't prematurely end the prototype because of the opening brace.  TypeScript
			// doesn't have operator overloading so it's safe to always treat angle brackets as blocks.
			if (base.TryToFindBasicPrototype(topic, lookahead, limit,
															treatAngleBracketsAsBlocks: true,
															out prototypeStart, out prototypeEnd))
				{
				prototypeStart = start;
				return true;
				}
			else
				{  return false;  }
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */
		override public ParsedPrototype ParsePrototype(string stringPrototype, int commentTypeID)
			{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator iterator = tokenizedPrototype.FirstToken;

			if (TryToSkipFunction(ref iterator, ParseMode.ParsePrototype))
				{
				return new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID,
														 parameterStyle: ParameterStyle.Pascal,
														 supportsImpliedTypes: false);
				}
			else
				{
				return base.ParsePrototype(stringPrototype, commentTypeID);
				}
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


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


			// Decorators

			if (TryToSkipDecorators(ref lookahead, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers before keyword
			// Things like "public" and "static", but we're going to allow any text-based keywords as long as they're followed by "function"

			while (lookahead.FundamentalType == FundamentalType.Text &&
					  !IsOnKeyword(lookahead, "function"))
				{
				do
					{
					// Mark them as type modifiers so public/private/protected can be picked up
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

					lookahead.Next();
					}
				while (lookahead.FundamentalType == FundamentalType.Text ||
						  lookahead.Character == '_' ||
						  lookahead.Character == '$');

				TryToSkipWhitespace(ref lookahead);
				}


			// Keyword

			if (!IsOnKeyword(lookahead, "function"))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Name

			if (!TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			// Template Signature

			if (TryToSkipTemplateSignature(ref lookahead, TemplateSignatureType.Definition, mode))
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

			if (lookahead.Character == ':')
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				TokenIterator startOfReturnValue = lookahead;

				// First try a type predicate like "a is string"
				if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
					{
					TryToSkipWhitespace(ref lookahead);

					if (IsOnKeyword(lookahead, "is"))
						{
						lookahead.Next();
						TryToSkipWhitespace(ref lookahead);
						// continue to type parsing
						}
					else
						{
						// Reset to try regular type parsing
						ResetTokensBetween(startOfReturnValue, lookahead, mode);
						lookahead = startOfReturnValue;
						}
					}

				if (!TryToSkipType(ref lookahead, mode))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				TryToSkipWhitespace(ref lookahead);
				}


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
			TryToSkipWhitespace(ref lookahead);


			// Decorators

			if (TryToSkipDecorators(ref lookahead, mode, breakPrototypeSections: false))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Rest Modifier

			if (lookahead.MatchesAcrossTokens("..."))
				{
				// Leave it unmarked so it doesn't appear as part of the type, such as "...x: number[]" having a type of "...number[]"
				lookahead.Next(3);
				TryToSkipWhitespace(ref lookahead);
				}


			// Name

			if (!TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			// Optional Modifier

			if (lookahead.Character == '?')
				{
				// Leave it unmarked so it doesn't appear as part of the type, such as "x?: string" having a type of "? string"
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Type

			if (lookahead.Character == ':')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.NameTypeSeparator;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (!TryToSkipType(ref lookahead, ParseMode.ParsePrototype))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				TryToSkipWhitespace(ref lookahead);
				}


			// Default Value

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
					{  GenericSkip(ref lookahead, false);  }

				if (mode == ParseMode.ParsePrototype)
					{  startOfDefaultValue.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.DefaultValue);  }
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipType
		 *
		 * Tries to move the iterator past a type, such as "string", "number[]", or more unusual things like tuples, allowable value
		 * lists, or inline objects.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipType (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;

			for (;;)
				{

				// Simple identifier

				if (TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Type))
					{
					iterator = lookahead;
					TryToSkipWhitespace(ref lookahead);

					// Template signature
					if (TryToSkipTemplateSignature(ref lookahead, TemplateSignatureType.Instantiation, mode))
						{
						iterator = lookahead;
						TryToSkipWhitespace(ref lookahead);
						}

					// Array
					if (lookahead.Character == '[')
						{
						TokenIterator endOfArray = lookahead;
						endOfArray.Next();

						if (GenericSkipUntilOn(ref endOfArray, ']', skipToEndIfNotFound: false))
							{
							if (mode == ParseMode.ParsePrototype)
								{
								lookahead.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;
								endOfArray.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
								}

							endOfArray.Next();
							iterator = endOfArray;

							lookahead = iterator;
							TryToSkipWhitespace(ref lookahead);
							}
						}
					}


				// Tuples and inline objects

				else if (TryToSkipTupleDefinition(ref lookahead, mode) ||
						   TryToSkipInlineObject(ref lookahead, mode))
					{
					iterator = lookahead;
					TryToSkipWhitespace(ref lookahead);
					}


				// Strings and numbers

				else if (TryToSkipString(ref lookahead) ||
						   TryToSkipNumber(ref lookahead))
					{
					// These are used for allowable value lists, such as "-1 | 0 | 1".

					if (mode == ParseMode.ParsePrototype)
						{
						// Find the beginning again from the last accepted end of the type.  This is a bit awkward and a little bit of duplicated
						// work but allowed value lists should be rare and it's better to do it on the exception than do extra work on the norm.
						TokenIterator startOfValue = iterator;
						TryToSkipWhitespace(ref startOfValue);

						// Skip | or & too since those didn't update the iterator
						if (startOfValue.Character == '|' ||
							startOfValue.Character == '&')
							{
							startOfValue.Next();
							TryToSkipWhitespace(ref startOfValue);
							}

						startOfValue.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.Type);
						}

					iterator = lookahead;
					TryToSkipWhitespace(ref lookahead);
					}


				// None of the above

				else
					{  return false;  }


				// Continue another round on | or &

				if (lookahead.Character == '|' ||
					lookahead.Character == '&')
					{
					// We want them included as part of the type such as in "number | string"
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{  break;  }
				}

			return true;
			}


		/* Function: TryToSkipTemplateSignature
		 *
		 * Tries to move the iterator past a template signature, such as "<number>" in "List<number>".  It can handle nested templates.
		 * Set the <TemplateSignatureType> to set whether it's handling a definition, such as "class MyTemplate<X, Y> { ... }", or an
		 * instantiation, such as "MyTemplate<number, string> x = null;".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- When using <TemplateSignatureType.Instantiation>, it will mark tokens with these types, including in nested templates:
		 *				- <PrototypeParsingType.OpeningTypeModifier>
		 *				- <PrototypeParsingType.ClosingTypeModifier>
		 *				- <PrototypeParsingType.Type>
		 *				- <PrototypeParsingType.TypeQualifier>
		 *				- <PrototypeParsingType.TypeModifier>
		 *			- When using <TemplateSignatureType.Definition>, it will mark everything with these types:
		 *				- <PrototypeParsingType.OpeningParamModifier>
		 *				- <PrototypeParsingType.ClosingParamModifier>
		 *				- <PrototypeParsingType.Name>
		 *		- <ParseMode.ParseClassPrototype>
		 *			- All tokens will be marked with <ClassPrototypeParsingType.TemplateSuffix>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipTemplateSignature (ref TokenIterator iterator, TemplateSignatureType signatureType,
																		ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '<')
				{  return false;  }

			TokenIterator lookahead = iterator;

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			while (lookahead.IsInBounds && lookahead.Character != '>')
				{
				for (;;)
					{
					if (signatureType == TemplateSignatureType.Instantiation)
						{
						if (TryToSkipType(ref lookahead, mode) == false)
							{
							ResetTokensBetween(iterator, lookahead, mode);
							return false;
							}

						TryToSkipWhitespace(ref lookahead);
						}
					else if (signatureType == TemplateSignatureType.Definition)
						{
						while (IsOnAnyKeyword(lookahead, "in", "out"))
							{
							if (mode == ParseMode.ParsePrototype)
								{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

							lookahead.Next();
							TryToSkipWhitespace(ref lookahead);
							}

						if (!TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
							{
							ResetTokensBetween(iterator, lookahead, mode);
							return false;
							}

						TryToSkipWhitespace(ref lookahead);

						if (IsOnKeyword(lookahead, "extends"))
							{
							lookahead.Next();
							TryToSkipWhitespace(ref lookahead);

							if (IsOnAnyKeyword(lookahead, "keyof", "typeof"))
								{
								lookahead.Next();
								TryToSkipWhitespace(ref lookahead);
								}

							if (!TryToSkipType(ref lookahead, mode))
								{
								ResetTokensBetween(iterator, lookahead, mode);
								return false;
								}

							TryToSkipWhitespace(ref lookahead);
							}

						if (lookahead.Character == '=')
							{
							lookahead.Next();
							TryToSkipWhitespace(ref lookahead);

							if (!TryToSkipType(ref lookahead, mode))
								{
								ResetTokensBetween(iterator, lookahead, mode);
								return false;
								}

							TryToSkipWhitespace(ref lookahead);
							}
						}
					else
						{  throw new NotImplementedException();  }

					if (lookahead.Character == ',')
						{
						lookahead.Next();
						TryToSkipWhitespace(ref lookahead);
						}
					else
						{  break;  }
					}
				}

			if (lookahead.Character == '>')
				{
				if (mode == ParseMode.ParsePrototype)
					{
					if (signatureType == TemplateSignatureType.Instantiation)
						{
						iterator.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;
						lookahead.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
						}
					else if (signatureType == TemplateSignatureType.Definition)
						{
						iterator.PrototypeParsingType = PrototypeParsingType.OpeningParamModifier;
						lookahead.PrototypeParsingType = PrototypeParsingType.ClosingParamModifier;
						}
					else
						{  throw new NotImplementedException();  }

					lookahead.Next();
					}
				else if (mode == ParseMode.ParseClassPrototype)
					{
					lookahead.Next();
					iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.TemplateSuffix);
					}
				else
					{  lookahead.Next();  }

				iterator = lookahead;
				return true;
				}
			else
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}
			}


		/* Function: TryToSkipTupleDefinition
		 *
		 * Tries to move the iterator past a tuple definition, such as "[number, string]".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipTupleDefinition (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '[')
				{  return false;  }

			TokenIterator lookahead = iterator;

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			while (lookahead.IsInBounds && lookahead.Character != ']')
				{
				if (!TryToSkipType(ref lookahead, mode))
					{  break;  }

				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character == ',')
					{
					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{  break;  }
				}

			if (lookahead.Character == ']')
				{
				if (mode == ParseMode.ParsePrototype)
					{
					iterator.PrototypeParsingType = PrototypeParsingType.StartOfTuple;
					lookahead.PrototypeParsingType = PrototypeParsingType.EndOfTuple;
					}

				lookahead.Next();
				iterator = lookahead;
				return true;
				}
			else
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}
			}


		/* Function: TryToSkipInlineObject
		 *
		 * Tries to move the iterator past an inline object definition, such as "{ x: number; y: number }".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- It will be marked like a tuple definition.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipInlineObject (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '{')
				{  return false;  }

			TokenIterator lookahead = iterator;

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			while (lookahead.IsInBounds && lookahead.Character != '}')
				{
				if (!TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.TupleMemberName))
					{  break;  }

				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character != ':')
					{  break;  }

				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TupleMemberSeparator;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (!TryToSkipType(ref lookahead, mode))
					{  break;  }

				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character == ';')
					{
					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{  break;  }
				}

			if (lookahead.Character == '}')
				{
				if (mode == ParseMode.ParsePrototype)
					{
					iterator.PrototypeParsingType = PrototypeParsingType.StartOfTuple;
					lookahead.PrototypeParsingType = PrototypeParsingType.EndOfTuple;
					}

				lookahead.Next();
				iterator = lookahead;
				return true;
				}
			else
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}
			}


		/* Function: TryToSkipMetadata
		 *
		 * Override to support detecting decorators as metadata.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Each annotation will create a new prototype section.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipMetadata (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			return TryToSkipDecorators(ref iterator, mode, breakPrototypeSections: true);
			}


		/* Function: TryToSkipDecorators
		 *
		 * Tries to move the iterator past one or more decorators, like "@Preliminary" or "@Copyright("String")".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Each annotation will create a new prototype section.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDecorators (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
															 bool breakPrototypeSections = true)
			{
			if (TryToSkipDecorator(ref iterator, mode, breakPrototypeSections))
				{
				TokenIterator lookahead = iterator;

				for (;;)
					{
					TryToSkipWhitespace(ref lookahead, true, mode);

					if (TryToSkipDecorator(ref lookahead, mode, breakPrototypeSections))
						{  iterator = lookahead;  }
					else
						{  break;  }
					}

				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToSkipDecorator
		 *
		 * Tries to move the iterator past a single decorator, like "@Preliminary" or "@Copyright("String")".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Each annotation will create a new prototype section.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDecorator (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
														   bool breakPrototypeSections = true)
			{
			if (iterator.Character != '@')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (!TryToSkipIdentifier(ref lookahead, mode))
				{  return false;  }

			TokenIterator endOfIdentifier = lookahead;

			TryToSkipWhitespace(ref lookahead);

			if (lookahead.Character == '(')
				{
				lookahead.Next();
				GenericSkipUntilAfter(ref lookahead, ')', false);
				}
			else
				{  lookahead = endOfIdentifier;  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Metadata);  }

			else if (mode == ParseMode.ParsePrototype)
				{
				iterator.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.ParamModifier);

				if (breakPrototypeSections)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;  }
				}

			iterator = lookahead;
			return true;
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
			// No keywords contain symbols so they must be text and only one token long.

			if (iterator.FundamentalType != FundamentalType.Text)
				{  return false;  }

			string token = iterator.ToString();

			if (javascriptKeywords.Contains(token) == false &&
				typescriptKeywords.Contains(token) == false)
				{  return false;  }

			if (IsPartOfLongerIdentifier(iterator))
				{  return false;  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

			iterator.Next();
			return true;
			}



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: typescriptKeywords
		 * You should also check against the inherited JavaScript keywords.  These are only the additions from TypeScript.
		 */
		static protected StringSet typescriptKeywords = new StringSet (KeySettings.Literal, new string[] {

			// Keywords
			"type", "interface", "as", "implements", "keyof", "readonly", "abstract", "declare", "satisfies", "infer", "namespace",
			"unknown", "never", "out",

			// Types
			"string", "number", "bigint", "boolean", "any", "void"

			});

		}
	}
