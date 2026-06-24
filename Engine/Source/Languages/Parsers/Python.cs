/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Python
 * ____________________________________________________________________________
 *
 * Additional language support for Python.
 *
 * Language Version:
 *
 *		The parser is based on Python 3.14.5, the latest release as of May 2026.
 *
 * Resources:
 *		- <Docs Home: https://www.python.org/doc/>
 *		- <Language Reference: https://docs.python.org/3/reference/>
 *			- Built-in types like "int" are documented in the <Standard Library: https://docs.python.org/3/library/stdtypes.html>.
 *			- Built-in constants like "None" are documented in the <Standard Library: https://docs.python.org/3/library/constants.html>.
 *			- <Type Hints Cheat Sheet: https://mypy.readthedocs.io/en/stable/cheat_sheet_py3.html>
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class Python : Parser
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: TemplateSignatureType
		 * Definition - The signature of a template definition, such as "class MyTemplate[X, Y]".
		 * Instantiation - The signature of a template instantiation, such as "x: MyTemplate[int, str]".
		 */
		public enum TemplateSignatureType: byte
			{  Definition, Instantiation  }



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Python
		 */
		public Python (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
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
				return new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID, engineInstance);
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
			if (EngineInstance.CommentTypes.InClassHierarchy(commentTypeID) == false)
				{  return null;  }

			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator startOfPrototype = tokenizedPrototype.FirstToken;

			if (TryToSkipClassDeclarationLine(ref startOfPrototype, ParseMode.ParseClassPrototype))
				{
				return new ParsedClassPrototype(tokenizedPrototype);
				}
			else
			    {
				return base.ParseClassPrototype(stringPrototype, commentTypeID);
				}
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToSkipClassDeclarationLine
		 *
		 * If the iterator is on a class's declaration line, moves it past it and returns true.  It does not handle the class body.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParseClassPrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipClassDeclarationLine (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;


			// Decorators

			if (TryToSkipDecorators(ref lookahead, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword

			if (lookahead.MatchesToken("class") == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParseClassPrototype)
				{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.Keyword;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Name

			TokenIterator startOfIdentifier = lookahead;

			if (TryToSkipIdentifier(ref lookahead) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParseClassPrototype)
				{  startOfIdentifier.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.Name);  }

			TryToSkipWhitespace(ref lookahead);


			// Template Signature

			if (TryToSkipTemplateSignature(ref lookahead, TemplateSignatureType.Definition, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Base classes

			if (lookahead.Character == '(')
				{
				if (mode == ParseMode.ParseClassPrototype)
					{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfParents;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				for (;;)
					{
					if (lookahead.Character == ')')
						{
						if (mode == ParseMode.ParseClassPrototype)
							{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.EndOfParents;  }

						break;
						}

					if (TryToSkipClassParent(ref lookahead, mode) == false)
						{
						ResetTokensBetween(iterator, lookahead, mode);
						return false;
						}

					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == ',')
						{
						if (mode == ParseMode.ParseClassPrototype)
							{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.ParentSeparator;  }

						lookahead.Next();
						TryToSkipWhitespace(ref lookahead);
						}
					}
				}


			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipClassParent
		 *
		 * Tries to move the iterator past a single class parent declaration.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParseClassPrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipClassParent (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;

			if (lookahead.MatchesToken("metaclass"))
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character == '=')
					{
					if (mode == ParseMode.ParseClassPrototype)
						{  iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.Modifier;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{
					// Nevermind, reset
					lookahead = iterator;
					}
				}


			TokenIterator startOfIdentifier = lookahead;

			if (TryToSkipIdentifier(ref lookahead) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParseClassPrototype)
				{  startOfIdentifier.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.Name);  }

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipFunction
		 *
		 * If the iterator is on a function definition,moves it past it and returns true.
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


			// Modifiers

			if (IsOnKeyword(lookahead, "async"))
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Keyword

			if (!IsOnKeyword(lookahead, "def"))
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

			if (lookahead.MatchesAcrossTokens("->"))
				{
				lookahead.Next(2);
				TryToSkipWhitespace(ref lookahead);

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


			// Star or slash

			if (lookahead.Character == '/' || lookahead.Character == '*')
				{
				// We only accept this here if this is the only content of the parameter.  Stars can also precede the parameter name so
				// we'll reset lookahead and continue if this fails.

				TokenIterator starOrSlash = lookahead;

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character == ',' || lookahead.Character == ')')
					{
					if (mode == ParseMode.ParsePrototype)
						{
						// Not the best option, but we want it to format in the name column, so that's what it gets.
						starOrSlash.PrototypeParsingType = PrototypeParsingType.Name;
						}

					iterator = lookahead;
					return true;
					}
				else
					{
					lookahead = starOrSlash;
					// continue below
					}
				}


			// Leading asterisks

			// Only one or two are allowed, but we'll just loop it.
			while (lookahead.Character == '*')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamModifier;  }

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
		 * Tries to move the iterator past a type.  It can handle simple types ("str"), parameterized types ("list[int]"), tuples ("(int, str)"), and
		 * unions ("str | bytearray").
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

				// Leading asterisk for tuples

				if (lookahead.Character == '*')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);

					// Continue below.  Deliberately not using "else if" for the next statement.
					}


				// Simple identifier

				if (TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Type))
					{
					iterator = lookahead;

					// Check if the identifier ends with "Callable" since that has special handling.
					TokenIterator lookbehind = lookahead;
					lookbehind.Previous();
					bool isCallable = IsOnKeyword(lookbehind, "Callable");

					TryToSkipWhitespace(ref lookahead);

					// Template signature
					if (TryToSkipTemplateSignature(ref lookahead, TemplateSignatureType.Instantiation, mode, isCallable))
						{
						iterator = lookahead;
						TryToSkipWhitespace(ref lookahead);
						}

					// Python doesn't have array suffixes like "int[12]".  All collections use named parameterized types like "array[int]".
					}


				// Tuples

				else if (TryToSkipTupleDefinition(ref lookahead, mode))
					{
					iterator = lookahead;
					TryToSkipWhitespace(ref lookahead);
					}


				// Ellipsis

				else if (lookahead.MatchesAcrossTokens("..."))
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.Type, 3);  }

					lookahead.Next(3);
					iterator = lookahead;
					TryToSkipWhitespace(ref lookahead);
					}


				// Strings and numbers

				else if (TryToSkipString(ref lookahead) ||
						   TryToSkipNumber(ref lookahead))
					{
					// These may be use with allowable value lists like Literal["GET" | "POST"].

					if (mode == ParseMode.ParsePrototype)
						{
						// Find the beginning again from the last accepted end of the type.  This is a bit awkward and a little bit of duplicated
						// work but allowed value lists should be rare and it's better to do it on the exception than do extra work on the norm.
						TokenIterator startOfValue = iterator;
						TryToSkipWhitespace(ref startOfValue);

						// Skip | too since it didn't update the iterator
						if (startOfValue.Character == '|')
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


				// Continue another round on unions

				if (lookahead.Character == '|')
					{
					// We want them included as part of the type such as in "int | str"
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
		 * Tries to move the iterator past a template signature, such as "[int]" in "list[int]".  It can handle nested templates.
		 * Set the <TemplateSignatureType> to set whether it's handling a definition, such as "def MyFunction[X, Y]:", or an
		 * instantiation, such as "myVar: MyTemplate[int, str]".
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
																		ParseMode mode = ParseMode.IterateOnly, bool isCallable = false)
			{
			if (iterator.Character != '[')
				{  return false;  }

			TokenIterator lookahead = iterator;

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			bool isFirstParameter = true;

			while (lookahead.IsInBounds && lookahead.Character != ']')
				{
				for (;;)
					{
					if (signatureType == TemplateSignatureType.Instantiation)
						{
						// Callable is the only thing that can have a nested pair of brackets immediately inside an existing one without
						// an intervening keyword, like "Callable[[int, int], int]".
						if (isCallable && isFirstParameter && lookahead.Character == '[')
							{
							if (!TryToSkipTemplateSignature(ref lookahead, signatureType, mode, isCallable = false))
								{
								ResetTokensBetween(iterator, lookahead, mode);
								return false;
								}
							}
						else
							{
							if (TryToSkipType(ref lookahead, mode) == false)
								{
								ResetTokensBetween(iterator, lookahead, mode);
								return false;
								}
							}

						TryToSkipWhitespace(ref lookahead);
						}
					else if (signatureType == TemplateSignatureType.Definition)
						{
						// Leading stars.  Only one or two are allowed, but we'll just loop it.
						while (lookahead.Character == '*')
							{
							if (mode == ParseMode.ParsePrototype)
								{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamModifier;  }

							lookahead.Next();
							TryToSkipWhitespace(ref lookahead);
							}

						if (!TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
							{
							ResetTokensBetween(iterator, lookahead, mode);
							return false;
							}

						TryToSkipWhitespace(ref lookahead);

						if (lookahead.Character == ':')
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

						isFirstParameter = false;
						}
					else
						{  break;  }
					}
				}

			if (lookahead.Character == ']')
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
		 * Tries to move the iterator past a tuple definition, such as "(int, str)".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipTupleDefinition (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '(')
				{  return false;  }

			TokenIterator lookahead = iterator;

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			while (lookahead.IsInBounds && lookahead.Character != ')')
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

			if (lookahead.Character == ')')
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
			return TryToSkipDecorator(ref iterator, mode);
			}


		/* Function: TryToSkipDecorators
		 *
		 * Tries to move the iterator past a group of decorators.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark each decorator with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and
		 *			  <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDecorators (ref TokenIterator iterator, ParseMode mode = ParseMode.ParseClassPrototype)
			{
			if (TryToSkipDecorator(ref iterator, mode) == false)
				{  return false;  }

			for (;;)
				{
				TokenIterator lookahead = iterator;
				TryToSkipWhitespace(ref lookahead);

				if (TryToSkipDecorator(ref lookahead, mode) == true)
					{  iterator = lookahead;  }
				else
					{  break;  }
				}

			return true;
			}


		/* Function: TryToSkipDecorator
		 *
		 * Tries to move the iterator past a single decorator.  Note that there may be more than one decorator in a row, so use
		 * <TryToSkipDecorators()> if you need to move past all of them.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Each decorator will create a new prototype section.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark the first token with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and the rest with
		 *			  <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDecorator (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '@')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();
			 TryToSkipWhitespace(ref lookahead);

			// Decorators can technically be any expression now.  Ideally we'd handle from the @ to the next line break, but
			// line breaks are normalized out of prototypes so we'll just handle the most common forms and things we can
			// say with high certainty are part of the decorator.

			// First try the @() form
			if (lookahead.Character == '(')
				{
				// Try to interpret it as parameters first so we can format the walrus operator that way ("@(Name := value)")
				// but if that fails retry as just a block.
				if (!TryToSkipDecoratorParameters(ref lookahead, mode))
					{
					lookahead.Next();

					if (!GenericSkipUntilAfter(ref lookahead, ')'))
						{  return false;  }
					}
				}

			// Next try the @identifier form, which may or may not have parameters.
			else if (TryToSkipIdentifier(ref lookahead, mode))
				{
				// We want to accept spaces between the identifier and parameters, so check if that's what comes next
				TokenIterator pastWhitespace = lookahead;
				pastWhitespace.NextPastWhitespace();

				if (TryToSkipDecoratorParameters(ref pastWhitespace, mode))
					{
					lookahead = pastWhitespace;
					pastWhitespace.NextPastWhitespace();
					}

				// Now continue for anything that we have high confidence is part of the decorator
				for (;;)
					{
					// Allow blocks for parameters past the first one (but we don't format them as such) and brackets for arrays
					if (TryToSkipBlock(ref pastWhitespace, includeAngleBrackets: false))
						{
						lookahead = pastWhitespace;
						pastWhitespace.NextPastWhitespace();
						}

					// Allow a dot for chained function calls, but only if there's no whitespace around it (hence checking lookahead
					// and not pastWhitespace)
					else if (lookahead.Character == '.')
						{
						lookahead.Next();

						if (TryToSkipIdentifier(ref lookahead))
							{
							pastWhitespace = lookahead;
							pastWhitespace.NextPastWhitespace();
							}
						else
							{
							// Return lookahead to the dot
							lookahead.Previous();
							break;
							}
						}

					else
						{  break;  }
					}
				}

			// Those are the only forms we'll accept.
			else
				{  return false;  }


			if (mode == ParseMode.SyntaxHighlight)
				{
				iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Metadata);
				}
			else if (mode == ParseMode.ParsePrototype)
				{
				iterator.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;
				lookahead.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;

				NormalizeMetadataProperties(iterator, lookahead);
				}
			else if (mode == ParseMode.ParseClassPrototype)
				{
				iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.PrePrototypeLine);
				iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipDecoratorParameters
		 *
		 * Tries to move the iterator past a decorator parameter section, such as "("String")" in "@Copyright("String")".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- The contents will be marked with parameter tokens.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDecoratorParameters (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '(')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (!GenericSkipUntilOn(ref lookahead, ')'))
				{  return false;  }

			if (mode == ParseMode.SyntaxHighlight)
				{
				// Move past the closing parenthesis.
				lookahead.Next();

				iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Metadata);
				}

			else if (mode == ParseMode.ParsePrototype)
				{
				// Mark the parentheses

				TokenIterator openingParen = iterator;
				TokenIterator closingParen = lookahead;

				openingParen.PrototypeParsingType = PrototypeParsingType.StartOfMetadataParams;
				closingParen.PrototypeParsingType = PrototypeParsingType.EndOfMetadataParams;


				// Mark the parameters

				lookahead = openingParen;
				lookahead.Next();

				TokenIterator startOfParam = lookahead;

				while (lookahead < closingParen)
					{
					if (lookahead.Character == ',')
						{
						MarkDecoratorParameter(startOfParam, lookahead, mode);

						lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;
						lookahead.Next();

						startOfParam = lookahead;
						}

					else
						{  GenericSkip(ref lookahead, true);  }
					}

				MarkDecoratorParameter(startOfParam, lookahead, mode);

				// Move past the closing parenthesis.
				lookahead.Next();
				}

			else
				{
				// Move past the closing parenthesis.
				lookahead.Next();
				}

			iterator = lookahead;
			return true;
			}


		/* Function: MarkDecoratorParameter
		 *
		 * Applies types to an decorator parameter, such as ""String"" in "@Copyright("String")" or "id = 12" in
		 * "@RequestForEnhancement(id = 12, engineer = "String")".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.ParsePrototype>
		 *			- The contents will be marked with parameter tokens.
		 *		- Everything else has no effect.
		 */
		protected void MarkDecoratorParameter (TokenIterator start, TokenIterator end, ParseMode mode = ParseMode.IterateOnly)
			{
			if (mode != ParseMode.ParsePrototype)
				{  return;  }

			start.NextPastWhitespace(end);
			end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

			if (start >= end)
				{  return;  }


			// Find and mark the equals sign, if there is one

			TokenIterator equals = start;

			while (equals < end)
				{
				if (equals.Character == '=')
					{
					equals.PrototypeParsingType = PrototypeParsingType.PropertyValueSeparator;
					break;
					}
				else if (equals.MatchesAcrossTokens(":="))
					{
					equals.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.PropertyValueSeparator, 2);
					break;
					}
				else
					{  GenericSkip(ref equals, true);  }
				}


			// The equals sign will be at or past the end if it doesn't exist.

			if (equals >= end)
				{
				start.SetPrototypeParsingTypeBetween(end, PrototypeParsingType.PropertyValue);
				}
			else
				{
				TokenIterator iterator = equals;
				iterator.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

				if (start < iterator)
					{  start.SetPrototypeParsingTypeBetween(iterator, PrototypeParsingType.Name);  }

				iterator = equals;

				do
					{  iterator.Next();  }
				while (iterator.PrototypeParsingType == PrototypeParsingType.PropertyValueSeparator);

				iterator.NextPastWhitespace(end);

				if (iterator < end)
					{  iterator.SetPrototypeParsingTypeBetween(end, PrototypeParsingType.PropertyValue);  }
				}
			}


		/* Function: IsPartOfLongerIdentifier
		 * Returns whether the <TokenIterator> is on token that's part of a longer identifier, such as by being next to an underscore.
		 * This is primarily used to validate keywords after checking the contents of the token against a keyword list, so that "input"
		 * by itself will be distinguished from "_input" or similar.
		 */
		protected bool IsPartOfLongerIdentifier (TokenIterator iterator)
			{
			// All python keywords are a single text token

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.FundamentalType == FundamentalType.Text ||
				lookahead.Character == '_')
				{  return true;  }

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			if (lookbehind.FundamentalType == FundamentalType.Text ||
				lookbehind.Character == '_')
				{  return true;  }

			return false;
			}


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
			return (iterator.MatchesToken(keyword) &&
					   !IsPartOfLongerIdentifier(iterator));
			}


		/* Function: IsOnAnyKeyword
		 *
		 * Returns whether the <TokenIterator> is on the passed keyword, making sure there are no other identifier tokens
		 * before or after it.  This allows us to be sure an iterator on "input" isn't actually on "_input" or similar.  This function
		 * assumes keywords are only one text token.
		 */
		public bool IsOnAnyKeyword (TokenIterator iterator, params string[] keywords)
			{
			return (iterator.MatchesAnyAcrossTokens(keywords, true) != -1 &&
					   !IsPartOfLongerIdentifier(iterator));
			}


		/* Function: IsOnAnyKeyword
		 *
		 * Returns whether the <TokenIterator> is on the passed keyword, making sure there are no other identifier tokens
		 * before or after it.  This allows us to be sure an iterator on "input" isn't actually on "_input" or similar.  This function
		 * assumes keywords are only one text token.
		 */
		public bool IsOnAnyKeyword (TokenIterator iterator, StringSet keywords)
			{
			return (iterator.FundamentalType == FundamentalType.Text &&
					   keywords.Contains(iterator.String) &&
					   !IsPartOfLongerIdentifier(iterator));
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
			// All python keywords are a single text token

			if (!IsOnAnyKeyword(iterator, pythonKeywords))
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
			if (iterator.Character != '\'' && iterator.Character != '\"' &&
				(iterator.FundamentalType == FundamentalType.Text && iterator.TokenLength <= 2) == false)
				{  return false;  }

			TokenIterator lookahead = iterator;
			TokenIterator startOfLastStringSegment = iterator;


			// Text prefix

			bool interpolated = false;

			// We've already established that it's only one or two characters long
			if (lookahead.FundamentalType == FundamentalType.Text)
				{
				char character = lookahead.Character;

				if (character == 'f' || character == 'F' ||
					character == 't' || character == 'T')
					{  interpolated = true;  }
				else if (character != 'r' && character != 'R' &&
						   character != 'b' && character != 'B' &&
						   character != 'u' && character != 'U')
					{  return false;  }

				if (lookahead.TokenLength == 2)
					{
					character = lookahead.Tokenizer.RawText[ lookahead.RawTextIndex + 1 ];

					if (character == 'f' || character == 'F' ||
						character == 't' || character == 'T')
						{  interpolated = true;  }
					else if (character != 'r' && character != 'R' &&
							   character != 'b' && character != 'B')
						{  return false;  }
					}

				lookahead.Next();
				}


			// Opening delimiter

			char delimiter;
			int delimiterCount;

			if (lookahead.MatchesAcrossTokens("'''") ||
				lookahead.MatchesAcrossTokens("\"\"\""))
				{
				delimiter = lookahead.Character;
				delimiterCount = 3;
				lookahead.Next(3);
				}
			else if (lookahead.Character == '\'' ||
					   lookahead.Character == '"')
				{
				delimiter = lookahead.Character;
				delimiterCount = 1;
				lookahead.Next();
				}
			else
				{  return false;  }


			// Contents

			while (lookahead.IsInBounds)
				{
				if (lookahead.Character == delimiter &&
					ConsecutiveCharacterCount(lookahead) >= delimiterCount)
					{
					lookahead.Next(delimiterCount);
					break;
					}

				else if (lookahead.Character == '\\')
					{
					lookahead.Next(2);
					}

				// Interpolated strings
				else if (interpolated && lookahead.Character == '{')
					{
					TokenIterator startOfInterpolatedCode = lookahead;
					lookahead.Next();

					// Double braces are escaped, so ignore
					if (lookahead.Character == '{')
						{  lookahead.Next();  }
					else
						{
						if (mode == ParseMode.SyntaxHighlight)
							{  startOfLastStringSegment.SetSyntaxHighlightingTypeBetween(startOfInterpolatedCode, SyntaxHighlightingType.String);  }

						GenericSkipUntilAfter(ref lookahead, '}', skipToEndIfNotFound: true);

						if (mode == ParseMode.SyntaxHighlight)
							{  SyntaxHighlight(startOfInterpolatedCode, lookahead);  }

						startOfLastStringSegment = lookahead;
						}
					}

				else
					{  lookahead.Next();  }
				}


			// Done

			if (mode == ParseMode.SyntaxHighlight)
				{  startOfLastStringSegment.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.String);  }

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipNumber
		 *
		 * If the iterator is on a numeric literal, moves the iterator past it and returns true.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipNumber (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (TryToSkipNumber(ref iterator,
										  ParseNumberFlags.AllowUnderscoreSeparators,
										  mode))
				{
				// Still need to catch the case of "123.j".  All other cases are handled by the above function.
				if (iterator.Character == 'j' && iterator.TokenLength == 1)
					{
					// We know the character before exists because we just skipped a value, so do this to avoid creating a lookbehind
					// iterator.
					if (iterator.Tokenizer.RawText[ iterator.RawTextIndex - 1 ] == '.')
						{
						TokenIterator lookahead = iterator;
						lookahead.Next();

						if (lookahead.FundamentalType != FundamentalType.Text &&
							lookahead.Character != '_')
							{
							// Now we can add the j to the number
							if (mode == ParseMode.SyntaxHighlight)
								{  iterator.SyntaxHighlightingType = SyntaxHighlightingType.Number;  }

							iterator.Next();
							}
						}
					}

				return true;
				}
			else
				{  return false;  }
			}



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: pythonKeywords
		 */
		static protected StringSet pythonKeywords = new StringSet (KeySettings.Literal, new string[] {

			// Keywords
			"False", "await", "else", "import", "pass",
			"None", "break", "except", "in", "raise",
			"True", "class", "finally", "is", "return",
			"and", "continue", "for", "lambda", "try",
			"as", "def", "from", "nonlocal", "while",
			"assert", "del", "global", "not" ,"with",
			"async", "elif", "if", "or", "yield",

			// Soft Keywords
			"match", "case", "type",

			// Primitive Types
			"int", "float", "complex",
			"bool",
			"list", "tuple", "range",
			"str",
			"bytes", "bytearray", "memoryview",
			"set", "frozenset", "dict",

			// Misc
			"Any", "metaclass", "NotImplemented"

			});

		}
	}
