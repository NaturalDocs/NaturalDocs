/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Tcl
 * ____________________________________________________________________________
 *
 * Additional language support for Tcl.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Topics;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class Tcl : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Tcl
		 */
		public Tcl (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */
		override public ParsedPrototype ParsePrototype(string stringPrototype, int commentTypeID)
			{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator iterator = tokenizedPrototype.FirstToken;

			if (TryToSkipProcedure(ref iterator, ParseMode.ParsePrototype))
				{
				return new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID,
														 parameterStyle: ParameterStyle.C);
				}
			else
				{
				return base.ParsePrototype(stringPrototype, commentTypeID);
				}
			}


		override protected bool TryToFindBasicPrototype (Topic topic, TokenIterator start, TokenIterator limit,
																				out TokenIterator prototypeStart, out TokenIterator prototypeEnd)
			{
			TryToSkipWhitespace(ref start);
			TokenIterator lookahead = start;

			if (TryToSkipProcedure(ref lookahead, ParseMode.ParsePrototype))
				{
				prototypeStart = start;
				prototypeEnd = lookahead;
				return true;
				}
			else
				{
				return base.TryToFindBasicPrototype(topic, start, limit, out prototypeStart, out prototypeEnd);
				}
			}


		/* Function: TryToSkipProcedure
		 *
		 * If the passed prototype is a procedure declaration, moves the iterator past it, sets the relevant token types, and returns
		 * true.  Otherwise it leaves the iterator alone, does not set any token types, and returns false.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipProcedure (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{

			// Proc keyword

			if (iterator.MatchesToken("proc") == false)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();
			lookahead.NextPastWhitespace();


			// Name

			if (!TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			lookahead.NextPastWhitespace();


			// Parameters.  Can be a single identifier if there's one or a number of them in braces

			// Parameter list in braces
			if (lookahead.Character == '{')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfParams;  }

				lookahead.Next();
				lookahead.NextPastWhitespace();

				while (lookahead.IsInBounds &&
						  lookahead.Character != '}')
					{
					// Parameter and default value in braces like "{a 12}"
					if (lookahead.Character == '{')
						{
						if (mode == ParseMode.ParsePrototype)
							{  lookahead.PrototypeParsingType = PrototypeParsingType.OpeningParamDecorator;  }

						lookahead.Next();
						lookahead.NextPastWhitespace();

						bool foundName = TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name);

						lookahead.NextPastWhitespace();

						TokenIterator startOfDefaultValue = lookahead;
						GenericSkipUntilAfter(ref lookahead, '}');

						if (mode == ParseMode.ParsePrototype)
							{
							TokenIterator lookbehind = lookahead;
							lookbehind.Previous();

							lookbehind.PrototypeParsingType = PrototypeParsingType.ClosingParamDecorator;

							if (foundName)
								{  startOfDefaultValue.SetPrototypeParsingTypeBetween(lookbehind, PrototypeParsingType.DefaultValue);  }
							}
						}

					// Parameter alone like "a"
					else
						{
						if (!TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
							{
							ResetTokensBetween(iterator, lookahead, mode);
							return false;
							}
						}

					// Whitespace following parameter, which may be a separator for the next param
					if (mode == ParseMode.ParsePrototype)
						{
						if (lookahead.FundamentalType == FundamentalType.Whitespace)
							{
							TokenIterator whitespace = lookahead;
							lookahead.NextPastWhitespace();

							if (lookahead.IsInBounds &&
								lookahead.Character != '}')
								{
								whitespace.PrototypeParsingType = PrototypeParsingType.ParamSeparator;
								}
							}
						}
					else
						{  lookahead.NextPastWhitespace();  }
					}

				// Closing brace
				if (lookahead.Character == '}')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.EndOfParams;  }

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


			// Single parameter without braces

			else
				{
				TokenIterator lookbehind = lookahead;
				lookbehind.Previous();

				if (lookbehind.FundamentalType != FundamentalType.Whitespace)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				if (mode == ParseMode.ParsePrototype)
					{  lookbehind.PrototypeParsingType = PrototypeParsingType.StartOfParams;  }

				if (!TryToSkipUnqualifiedIdentifier(ref lookahead, mode, PrototypeParsingType.Name))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				iterator = lookahead;
				return true;
				}
			}


		/* Function: TryToSkipIdentifier
		 *
		 * If the passed iterator is on an identifier, moves the iterator past it and returns true.  If the iterator isn't on an
		 * identifier, nothing is changed and it returns false.  This function accepts qualified identifiers but will still return
		 * true if it's on an unqualified one.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.Name> or
		 *			  <PrototypeParsingType.Type>.  If set to Type, it will use <PrototypeParsingType.Type> and
		 *			  <PrototypeParsingType.TypeQualifier>.
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
														   PrototypeParsingType prototypeParsingType = PrototypeParsingType.Name)
			{
			bool success = false;

			TokenIterator lookahead = iterator;
			TokenIterator endOfIdentifier = iterator;
			TokenIterator endOfQualifier = iterator;

			for (;;)
				{
				if (lookahead.MatchesAcrossTokens("::"))
					{
					lookahead.NextByCharacters(2);
					endOfQualifier = lookahead;
					}
				else if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode, prototypeParsingType))
					{
					endOfIdentifier = lookahead;
					success = true;
					}
				else
					{  break;  }
				}

			if (!success)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParsePrototype &&
				prototypeParsingType == PrototypeParsingType.Type &&
				endOfQualifier > iterator)
				{
				iterator.SetPrototypeParsingTypeBetween(endOfQualifier, PrototypeParsingType.TypeQualifier);
				}

			iterator = endOfIdentifier;
			return true;
			}


		/* Function: TryToSkipUnqualifiedIdentifier
		 *
		 * If the passed iterator is on an identifier, moves the iterator past it and returns true.  If the iterator isn't on an identifier,
		 * nothing is changed and it returns false.  This only handles a single unqualified identifier.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.Name> or
		 *			  <PrototypeParsingType.Type>.
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipUnqualifiedIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
																		   PrototypeParsingType prototypeParsingType = PrototypeParsingType.Name)
			{
			if (iterator.FundamentalType == FundamentalType.Text)
				{
				if (iterator.Character >= '0' && iterator.Character <= '9')
					{  return false;  }
				}
			else if (iterator.FundamentalType == FundamentalType.Symbol)
				{
				if (iterator.Character != '_' && iterator.Character != '$')
					{  return false;  }
				}
			else
				{  return false;  }

			TokenIterator lookahead = iterator;

			do
				{
				lookahead.Next();
				}
			while (lookahead.FundamentalType == FundamentalType.Text ||
					  lookahead.Character == '_' || lookahead.Character == '$');

			if (mode == ParseMode.ParsePrototype)
				{  iterator.SetPrototypeParsingTypeBetween(lookahead, prototypeParsingType);  }

			iterator = lookahead;
			return true;
			}


		/* Function: GenericSkip
		 *
		 * Advances the iterator one place through general code.
		 *
		 * - If the position is on a string, it will skip it completely.
		 * - If the position is on an opening brace, parenthesis, or bracket it will skip until the past the closing symbol.
		 * - If the position is on whitespace or comments it will skip it completely.
		 * - Otherwise it skips one token.
		 */
		protected void GenericSkip (ref TokenIterator iterator)
			{
			if (iterator.Character == '(')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, ')');
				}
			else if (iterator.Character == '[')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, ']');
				}
			else if (iterator.Character == '{')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, '}');
				}

			else if (TryToSkipString(ref iterator) ||
					  TryToSkipWhitespace(ref iterator))
				{  }

			else
				{  iterator.Next();  }
			}


		/* Function: GenericSkipUntilAfter
		 * Advances the iterator via <GenericSkip()> until a specific symbol is reached and passed.
		 */
		protected void GenericSkipUntilAfter (ref TokenIterator iterator, char symbol)
			{
			while (iterator.IsInBounds)
				{
				if (iterator.Character == symbol)
					{
					iterator.Next();
					break;
					}
				else
					{  GenericSkip(ref iterator);  }
				}
			}

		}
	}
