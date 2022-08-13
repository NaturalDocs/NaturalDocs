/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.SQL
 * ____________________________________________________________________________
 *
 * Additional language support for PL/SQL and T-SQL.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
{
	public class SQL : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: SQL
		 * Static constructor.
		 */
		static SQL ()
			{
			generalKeyphrases = new StringTable<string[]>(KeySettings.IgnoreCase);

			generalKeyphrases["BFILE"] = new string[] { "BFILE_BASE" };
			generalKeyphrases["BINARY"] = new string[] { "BINARY_DOUBLE", "BINARY_FLOAT", "BINARY_INTEGER" };
			generalKeyphrases["BLOB"] = new string[] { "BLOB_BASE" };
			generalKeyphrases["CHAR"] = new string[] { "CHAR_BASE" };
			generalKeyphrases["CLOB"] = new string[] { "CLOB_BASE" };
			generalKeyphrases["CURRENT"] = new string[] { "CURRENT_DATE", "CURRENT_TIME", "CURRENT_TIMESTAMP", "CURRENT_USER" };
			generalKeyphrases["DATE"] = new string[] { "DATE_BASE" };
			generalKeyphrases["DOUBLE"] = new string[] { "DOUBLE PRECISION" };
			generalKeyphrases["IDENTITY"] = new string[] { "IDENTITY_INSERT" };
			generalKeyphrases["NUMBER"] = new string[] { "NUMBER_BASE" };
			generalKeyphrases["PARALLEL"] = new string[] { "PARALLEL_ENABLE" };
			generalKeyphrases["PLS"] = new string[] { "PLS_INTEGER" };
			generalKeyphrases["RELIES"] = new string[] { "RELIES_ON" };
			generalKeyphrases["RESULT"] = new string[] { "RESULT_CACHE" };
			generalKeyphrases["SESSION"] = new string[] {  "SESSION_USER" };
			generalKeyphrases["SIMPLE"] = new string[] { "SIMPLE_INTEGER" };
			generalKeyphrases["SIZE"] = new string[] { "SIZE_T" };
			generalKeyphrases["SYSTEM"] = new string[] { "SYSTEM_USER" };
			generalKeyphrases["TIMEZONE"] = new string[] { "TIMEZONE_ABBR", "TIMEZONE_HOUR", "TIMEZONE_MINUTE", "TIMEZONE_REGION" };
			generalKeyphrases["TRY"] = new string[] { "TRY_CONVERT" };
			generalKeyphrases["WITHIN"] = new string[] { "WITHIN GROUP" };
			}


		/* Constructor: SQL
		 */
		public SQL (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}

		/* Function: TryToFindBasicPrototype
		 */
		override protected bool TryToFindBasicPrototype (Topic topic, LineIterator startCode, LineIterator endCode,
																			   out TokenIterator prototypeStart, out TokenIterator prototypeEnd)
			{
			if (topic.CommentTypeID != 0 &&
				( topic.CommentTypeID == EngineInstance.CommentTypes.IDFromKeyword("function", language.ID) ||
				  topic.CommentTypeID == EngineInstance.CommentTypes.IDFromKeyword("procedure", language.ID) ||
				  topic.CommentTypeID == EngineInstance.CommentTypes.IDFromKeyword("database function", language.ID) ||
				  topic.CommentTypeID == EngineInstance.CommentTypes.IDFromKeyword("database procedure", language.ID) ))
				{
				TokenIterator startToken = startCode.FirstToken(LineBoundsMode.ExcludeWhitespace);
				TokenIterator endToken = endCode.FirstToken(LineBoundsMode.Everything);

				TokenIterator iterator = startToken;

				if (TryToSkipFunction(ref iterator) &&
					iterator <= endToken &&
					iterator.Tokenizer.ContainsTextBetween(topic.Title, true, startToken, iterator))
					{
					prototypeStart = startToken;
					prototypeEnd = iterator;
					return true;
					}
				else
					{
					prototypeStart = iterator;
					prototypeEnd = iterator;
					return false;
					}
				}

			// Let the default implementation handle variables, since ParseVariable doesn't set an endpoint

			// If they didn't work, fall back to the default implementation
			else
				{
				return base.TryToFindBasicPrototype(topic, startCode, endCode, out prototypeStart, out prototypeEnd);
				}
			}


		/* Function: OnIdentifierToken
		 * Returns whether the iterator is on a token that can be part of an identifier, which means text, underscores, and the
		 * @ $ # symbols.
		 */
		protected bool OnIdentifierToken (TokenIterator iterator)
			{
			if (iterator.FundamentalType == FundamentalType.Text)
				{  return true;  }
			else if (iterator.FundamentalType == FundamentalType.Symbol)
				{
				return (iterator.Character == '@' ||
						   iterator.Character == '$' ||
						   iterator.Character == '#' ||
						   iterator.Character == '_');
				}
			else
				{  return false;  }
			}


		/* Function: OnKeyword
		 * Returns whether the iterator is on a keyword, meaning a text token like "FUNCTION" surrounded by whitespace
		 * and not something like "@FUNCTION" or "FUNCTION_NAME".  It does not matter what the keyword is, just that it's
		 * the right format to be one.
		 */
		protected bool OnKeyword (TokenIterator iterator)
			{
			if (iterator.FundamentalType != FundamentalType.Text)
				{  return false;  }

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			if (lookbehind.Character == '.' ||
				OnIdentifierToken(lookbehind))
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.Character == '.' ||
				OnIdentifierToken(lookahead))
				{  return false;  }

			return true;
			}


		/* Function: OnKeyword
		 * Returns whether the iterator is on the passed keyword, meaning something like "FUNCTION" surrounded by whitespace
		 * and not something like "@FUNCTION" or "FUNCTION_NAME".
		 */
		protected bool OnKeyword (TokenIterator iterator, string word)
			{
			if (!iterator.MatchesToken(word, true))
				{  return false;  }

			return OnKeyword(iterator);
			}


		/* Function: OnKeyphrase
		 * Returns whether the iterator is on the passed keyphrase, meaning something like "EXECUTE AS" surrounded by
		 * whitespace and not something like "@EXECUTE AS".
		 */
		protected bool OnKeyphrase (TokenIterator iterator, string phrase)
			{
			if (!iterator.MatchesAcrossTokens(phrase, true))
				{  return false;  }

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			if (OnIdentifierToken(lookbehind))
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.NextByCharacters(phrase.Length);

			if (OnIdentifierToken(lookahead))
				{  return false;  }

			return true;
			}


		/* Function: OnAnyKeyword
		 * Returns whether the iterator is on one of the passed keywords, meaning something like "FUNCTION" surrounded
		 * by whitespace and not something like "@FUNCTION" or "FUNCTION_NAME".  Use the other version of the function
		 * if you need to know what the matching keyword was.
		 */
		protected bool OnAnyKeyword (TokenIterator iterator, params string[] words)
			{
			string ignore;
			return OnAnyKeyword(iterator, out ignore, words);
			}


		/* Function: OnAnyKeyword
		 * Returns whether the iterator is on one of the passed keywords, meaning something like "FUNCTION" surrounded
		 * by whitespace and not something like "@FUNCTION" or "FUNCTION_NAME".  This version of the function will also
		 * return what the matching keyword was.
		 */
		protected bool OnAnyKeyword (TokenIterator iterator, out string matchingWord, params string[] words)
			{
			int matchIndex = iterator.MatchesAnyToken(words, true);

			if (matchIndex == -1 ||
				!OnKeyword(iterator))
				{
				matchingWord = null;
				return false;
				}

			matchingWord = words[matchIndex];
			return true;
			}


		/* Function: OnAnyKeyphrase
		 * Returns whether the iterator is on one of the passed keyphrase, meaning something like "EXECUTE AS" surrounded
		 * by whitespace and not something like "@EXECUTE AS".  Use the other version of this function if you need to know
		 * what the matching phrase was.
		 */
		protected bool OnAnyKeyphrase (TokenIterator iterator, params string[] phrases)
			{
			string ignore;
			return OnAnyKeyphrase(iterator, out ignore, phrases);
			}


		/* Function: OnAnyKeyphrase
		 * Returns whether the iterator is on one of the passed keyphrase, meaning something like "EXECUTE AS" surrounded
		 * by whitespace and not something like "@EXECUTE AS".  This version of the function will also return what the matching
		 * phrase was.
		 */
		protected bool OnAnyKeyphrase (TokenIterator iterator, out string matchingPhrase, params string[] phrases)
			{
			int matchIndex = iterator.MatchesAnyAcrossTokens(phrases, true);

			if (matchIndex == -1)
				{
				matchingPhrase = null;
				return false;
				}

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			if (OnIdentifierToken(lookbehind))
				{
				matchingPhrase = null;
				return false;
				}

			TokenIterator lookahead = iterator;
			lookahead.NextByCharacters(phrases[matchIndex].Length);

			if (OnIdentifierToken(lookahead))
				{
				matchingPhrase = null;
				return false;
				}

			matchingPhrase = phrases[matchIndex];
			return true;
			}


		/* Funuction: SyntaxHighlight
		 */
		override public void SyntaxHighlight (Tokenizer source)
			{
			TokenIterator iterator = source.FirstToken;

			if (!TryToSkipFunction(ref iterator, ParseMode.SyntaxHighlight))
				{  SimpleSyntaxHighlightBetween(source.FirstToken, source.LastToken);  }
			}


		/* Function: SimpleSyntaxHighlightBetween
		 * Provides simple syntax highlighting between the two TokenIterators.  This will mark things like comments, strings,
		 * and certain keywords but not keywords specific to certain statements.
		 */
		public void SimpleSyntaxHighlightBetween (TokenIterator start, TokenIterator end)
			{
			TokenIterator iterator = start;

			while (iterator < end)
				{  GenericSkip(ref iterator, ParseMode.SyntaxHighlight);  }
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */
		override public ParsedPrototype ParsePrototype (string stringPrototype, int commentTypeID)
			{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);

			if (commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("function", language.ID) ||
				commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("procedure", language.ID) ||
				commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("database function", language.ID) ||
				commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("database procedure", language.ID))
				{
				TokenIterator iterator = tokenizedPrototype.FirstToken;
				TryToSkipFunction(ref iterator, ParseMode.ParsePrototype);
				}
			else if (commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("variable", language.ID) ||
					   commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("column", language.ID) ||
					   commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("cursor", language.ID) ||
					   commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("database variable", language.ID) ||
					   commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("database column", language.ID) ||
					   commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("database cursor", language.ID))
				{
				ParseVariable(tokenizedPrototype.FirstToken, tokenizedPrototype.LastToken, ParseMode.ParsePrototype);
				}

			return new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID);
			}


		/* Function: ParseVariable
		 *
		 * Parses a variable declaration, function parameter, or column definition between the two iterators and sets the relevant
		 * token types.  This isn't a TryToSkip function, it's assuming the code definitely is a variable declaration and will treat
		 * unrecognized keywords accordingly.  Returns whether it was successful.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool ParseVariable (TokenIterator start, TokenIterator end, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = start;


			// DECLARE, if present

			if (OnKeyword(lookahead, "DECLARE"))
				{
				if (mode == ParseMode.SyntaxHighlight)
					{  lookahead.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Identifier

			if (!TryToSkipIdentifier(ref lookahead, mode))
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);


			// AS, if present

			if (OnKeyword(lookahead, "AS"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.NameTypeSeparator;  }
				else if (mode == ParseMode.SyntaxHighlight)
					{  lookahead.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Pre-type modifiers

			while (OnAnyKeyword(lookahead, "IN", "OUT", "NOCOPY", "VARYING", "CONSTANT"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamModifier;  }
				else if (mode == ParseMode.SyntaxHighlight)
					{  lookahead.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Type and modifiers

			if (OnKeyword(lookahead, "TABLE"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.Type;  }
				else if (mode == ParseMode.SyntaxHighlight)
					{  lookahead.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead, true, mode);

				if (lookahead.Character == '(')
					{  TryToSkipParenthesizedParameters(ref lookahead, mode);  }
				}
			else if (TryToSkipBuiltInType(ref lookahead, end, mode))
				{  }
			else if (TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Type))
				{
				if (lookahead.Character == '(')
					{  TryToSkipTypeParentheses(ref lookahead, mode);  }
				}

			if (lookahead > end)
				{
				ResetTokensBetween(start, lookahead, mode);
				return false;
				}


			// Any words between the type and the end of the line or the default value are modifiers

			while (lookahead < end &&
					  lookahead.Character != '=' &&
					  !lookahead.MatchesAcrossTokens(":=") &&
					  !OnKeyword(lookahead, "DEFAULT"))
				{
				if (OnKeyword(lookahead))
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }
					else if (mode == ParseMode.SyntaxHighlight)
						{  lookahead.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

					lookahead.Next();

					TryToSkipWhitespace(ref lookahead, true, mode);

					if (lookahead < end)
						{  TryToSkipTypeParentheses(ref lookahead, mode);  }
					}
				else if (TryToSkipWhitespace(ref lookahead, true, mode) ||
						   TryToSkipString(ref lookahead, mode))
					{  }
				else
					{  GenericSkip(ref lookahead, mode);  }
				}


			// Default value

			if (lookahead < end &&
				 (lookahead.Character == '=' ||
				  lookahead.MatchesAcrossTokens(":=") ||
				  OnKeyword(lookahead, "DEFAULT")) )
				{
				if (lookahead.Character == ':')  // :=
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.DefaultValueSeparator, 2);  }

					lookahead.NextByCharacters(2);
					}
				else if (lookahead.Character == '=')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.DefaultValueSeparator;  }

					lookahead.Next();
					}
				else // DEFAULT
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.DefaultValueSeparator;  }
					else if (mode == ParseMode.SyntaxHighlight)
						{  lookahead.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

					lookahead.Next();
					}

				TryToSkipWhitespace(ref lookahead, true, mode);

				if (lookahead < end)
					{
					TokenIterator startOfDefaultValue = lookahead;

					// It's possible for "READONLY" or "OUTPUT" to appear AFTER the default value (sigh) so detect it as the end.
					while (lookahead < end &&
							  !OnAnyKeyword(lookahead, "READONLY", "OUTPUT"))
						{  GenericSkip(ref lookahead, mode);  }

					if (lookahead > end)
						{  lookahead = end;  }

					if (mode == ParseMode.ParsePrototype)
						{  startOfDefaultValue.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.DefaultValue);  }
					}
				}


			// Yet more modifiers that could appear AFTER the default value

			while (lookahead < end)
				{
				if (OnKeyword(lookahead))
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }
					else if (mode == ParseMode.SyntaxHighlight)
						{  lookahead.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

					lookahead.Next();

					TryToSkipWhitespace(ref lookahead, true, mode);

					if (lookahead < end)
						{  TryToSkipTypeParentheses(ref lookahead, mode);  }
					}
				else if (TryToSkipWhitespace(ref lookahead, true, mode) ||
						   TryToSkipString(ref lookahead, mode))
					{  }
				else
					{  GenericSkip(ref lookahead, mode);  }
				}


			return true;
			}


		/* Function: TryToSkipFunction
		 *
		 * If the passed prototype is a function or procedure declaration, moves the iterator past it, sets the relevant token
		 * types, and returns true.  Otherwise it leaves the iterator alone, does not set any token types, and returns false.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipFunction (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;


			// Keywords

			// Skip things like CREATE OR REPLACE and random modifiers like EDITIONABLE to get to the main keyword
			// Microsoft allows PROC instead of PROCEDURE, though not FUNC instead of FUNCTION
			while ( (lookahead.FundamentalType == FundamentalType.Text ||
						lookahead.FundamentalType == FundamentalType.Whitespace ||
						lookahead.FundamentalType == FundamentalType.LineBreak ||
						lookahead.Character == '_') &&
						!OnAnyKeyword(lookahead, "FUNCTION", "PROCEDURE", "PROC"))
				{  lookahead.Next();  }

			if (!OnAnyKeyword(lookahead, "FUNCTION", "PROCEDURE", "PROC"))
				{  return false;  }

			lookahead.Next();

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Keyword);  }

			TryToSkipWhitespace(ref lookahead);


			// Name

			if (!TryToSkipIdentifier(ref lookahead, mode))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			// Parameters, if any

			// Microsoft functions might not use parentheses to list the parameters.  The parameter names
			// always start with @ though.
			if (lookahead.Character == '@')
				{
				if (mode == ParseMode.ParsePrototype)
					{
					TokenIterator lookbehind = lookahead;
					lookbehind.Previous();
					lookbehind.PrototypeParsingType = PrototypeParsingType.StartOfParams;
					}

				TokenIterator startOfParameter = lookahead;

				while (lookahead.IsInBounds)
					{

					// If we're starting a new parameter, handle the possibility of "@name AS type" as we don't want the "AS" to
					// throw off parsing later.
					if (startOfParameter == lookahead)
						{
						TryToSkipWhitespace(ref lookahead, true);

						// We'll let ParseParameter mark the tokens later.  We only need to iterate for now.
						if (TryToSkipIdentifier(ref lookahead, ParseMode.IterateOnly))
							{
							TryToSkipWhitespace(ref lookahead, true);

							if (OnKeyword(lookahead, "AS"))
								{  lookahead.Next();  }
							}
						}

					// Deliberately starting another "if" instead of using "else if"
					if (lookahead.Character == ',')
						{
						if (mode != ParseMode.IterateOnly)
							{
							if (mode == ParseMode.ParsePrototype)
								{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;  }

							TryToSkipWhitespace(ref startOfParameter, true, mode);

							if (startOfParameter < lookahead)
								{  ParseVariable(startOfParameter, lookahead, mode);  }
							}

						lookahead.Next();
						startOfParameter = lookahead;
						}

					else if (lookahead.Character == ';')
						{
						if (mode != ParseMode.IterateOnly)
							{
							TryToSkipWhitespace(ref startOfParameter, true, mode);

							if (startOfParameter < lookahead)
								{  ParseVariable(startOfParameter, lookahead, mode);  }
							}

						break;
						}

					else if (OnAnyKeyword(lookahead, "RETURNS", "WITH", "AS", "BEGIN"))
						{
						if (mode != ParseMode.IterateOnly)
							{
							TokenIterator lookbehind = lookahead;
							lookbehind.Previous();

							if (mode == ParseMode.ParsePrototype)
								{  lookbehind.PrototypeParsingType = PrototypeParsingType.EndOfParams;  }

							TryToSkipWhitespace(ref startOfParameter, true, mode);

							if (startOfParameter < lookbehind)
								{  ParseVariable(startOfParameter, lookbehind, mode);  }
							}

						break;
						}

					else
						{
						// This will skip entire parentheses in one step so it won't trip on the comma in "NUMBER(5,6)"
						GenericSkip(ref lookahead, mode);

						// If we're leaving this loop because we hit the end, make sure to finish up the last parameter
						if (!lookahead.IsInBounds &&
							mode != ParseMode.IterateOnly)
							{
							TryToSkipWhitespace(ref startOfParameter, true, mode);

							if (startOfParameter < lookahead)
								{  ParseVariable(startOfParameter, lookahead, mode);  }
							}
						}
					}
				}

			// Oracle functions always use parentheses.  Microsoft functions can.
			else if (lookahead.Character == '(')
				{
				if (TryToSkipParenthesizedParameters(ref lookahead, mode) == false)
					{  return false;  }
				}


			// If we made it this far we are successful.  We're going to drop the iterator after each following successful section because
			// TryToSkipWhitespace() can potentially suck up the next documentation comment, which we don't want.

			iterator = lookahead;


			// Return value

			TryToSkipWhitespace(ref lookahead);


			// Microsoft uses RETURNS

			if (OnKeyword(lookahead, "RETURNS"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;  }
				else if (mode == ParseMode.SyntaxHighlight)
					{  lookahead.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);


				// Identifier, if present

				if (lookahead.Character == '@')
					{
					TryToSkipIdentifier(ref lookahead, mode);
					TryToSkipWhitespace(ref lookahead);
					}


				// Table

				if (OnKeyword(lookahead, "TABLE"))
					{
					if (mode == ParseMode.SyntaxHighlight)
						{  lookahead.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

					lookahead.Next();
					iterator = lookahead;

					TryToSkipWhitespace(ref lookahead);
					if (lookahead.Character == '(')
						{
						if (TryToSkipParenthesizedParameters(ref lookahead, mode) == false)
							{
							// Return what we have anyway, just don't include the parentheses section
							return true;
							}
						}
					}


				// Type

				else
					{
					TokenIterator beginningOfType = lookahead;

					while (lookahead.IsInBounds)
						{
						// Microsoft functions can have a RETURNS followed by a RETURN
						if (OnAnyKeyword(lookahead, "RETURN", "WITH", "AS", "BEGIN") ||
							lookahead.Character == ';')
							{  break;  }
						else
							{  GenericSkip(ref lookahead, mode);  }
						}

					if (mode != ParseMode.IterateOnly)
						{
						if (TryToSkipBuiltInType(ref beginningOfType, lookahead, mode))
							{  }
						else if (TryToSkipIdentifier(ref beginningOfType, mode, PrototypeParsingType.Type))
							{
							if (beginningOfType < lookahead &&
								beginningOfType.Character == '(')
								{  TryToSkipTypeParentheses(ref beginningOfType, mode);  }
							}
						else
							{  beginningOfType.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.Type);  }
						}
					}

				iterator = lookahead;
				}


			// Oracle uses RETURN

			else if (OnKeyword(lookahead, "RETURN"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;  }
				else if (mode == ParseMode.SyntaxHighlight)
					{  lookahead.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);


				// Type

				if (TryToSkipBuiltInType(ref lookahead, mode))
					{  }
				else if (TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Type))
					{
					if (lookahead.Character == '(')
						{  TryToSkipTypeParentheses(ref lookahead, mode);  }
					}
				else
					{
					TokenIterator startOfType = lookahead;

					while (lookahead.IsInBounds &&
							  !OnAnyKeyword(lookahead, "IS", "AS", "BEGIN",
																	  "SHARING", "AUTHID", "ACCESSIBLE", "DETERMINISTIC", "AGGREGATE", "PIPELINED") &&
							  !OnAnyKeyphrase(lookahead, "DEFAULT COLLATION", "PARALLEL_ENABLE", "RESULT_CACHE") &&
							  lookahead.Character != ';')
						{
						GenericSkip(ref lookahead, mode);
						}

					if (mode == ParseMode.ParsePrototype)
						{  startOfType.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.Type);  }

					if (lookahead.IsInBounds)
						{  lookahead.Next();  }
					}

				iterator = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}


			// WITH section for Microsoft

			TryToSkipWhitespace(ref lookahead);

			if (OnKeyword(lookahead, "WITH"))
				{
				TokenIterator startOfWith = lookahead;

				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;  }
				// We don't have to syntax highlight here, they'll all be turned into metadata at the end

				lookahead.Next();

				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfParams;  }

				while (lookahead.IsInBounds &&
						  !OnAnyKeyword(lookahead, "RETURN", "AS", "BEGIN") &&
						  !OnKeyphrase(lookahead, "FOR REPLICATION"))
					{
					string matchingPhrase;

					// Prevent EXECUTE AS from being interpreted as the function body's AS
					if (OnAnyKeyphrase(lookahead, out matchingPhrase, "EXECUTE AS", "EXEC AS"))
						{  lookahead.NextByCharacters(matchingPhrase.Length);  }
					else if (lookahead.Character == ',')
						{
						if (mode == ParseMode.ParsePrototype)
							{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;  }

						lookahead.Next();
						}
					else
						{  GenericSkip(ref lookahead, ParseMode.IterateOnly);  }
					}

				if (mode == ParseMode.SyntaxHighlight)
					{  startOfWith.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Metadata);  }

				iterator = lookahead;
				}


			// FOR REPLICATION for Microsoft

			TryToSkipWhitespace(ref lookahead);

			if (OnKeyphrase(lookahead, "FOR REPLICATION"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;  }
				else if (mode == ParseMode.SyntaxHighlight)
					{  lookahead.SetSyntaxHighlightingTypeByCharacters(SyntaxHighlightingType.Metadata, 15);  }

				lookahead.NextByCharacters(15);
				iterator = lookahead;
				}


			// Various modifiers for Oracle

			if (OnAnyKeyword(lookahead, "SHARING", "AUTHID", "ACCESSIBLE", "DETERMINISTIC", "AGGREGATE", "PIPELINED") ||
				OnAnyKeyphrase(lookahead, "DEFAULT COLLATION", "PARALLEL_ENABLE", "RESULT_CACHE"))
				{
				TokenIterator startOfModifiers = lookahead;

				// Oracle modifiers can be dramatically longer than Microsoft's so give each one its own section instead of grouping
				// them all into one like we do for Microsoft's WITH.  They're also not separated by commas like Microsoft's so this
				// also helps visual separation.
				do
					{
					string matchingPhrase;

					if (OnAnyKeyword(lookahead, "SHARING", "AUTHID", "ACCESSIBLE", "DETERMINISTIC", "AGGREGATE", "PIPELINED"))
						{
						if (mode == ParseMode.ParsePrototype)
							{ lookahead.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection; }
						// We don't have to syntax highlight here, they'll all be turned into metadata at the end

						lookahead.Next();
						}
					else if (OnAnyKeyphrase(lookahead, out matchingPhrase, "DEFAULT COLLATION", "PARALLEL_ENABLE", "RESULT_CACHE"))
						{
						if (mode == ParseMode.ParsePrototype)
							{ lookahead.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection; }

						lookahead.NextByCharacters(matchingPhrase.Length);
						}
					else
						{  GenericSkip(ref lookahead, ParseMode.IterateOnly);  }
					}
				while (lookahead.IsInBounds &&
						  !OnAnyKeyword(lookahead, "IS", "AS", "BEGIN") &&
						  lookahead.Character != ';');

				// End the prototype section so the IS/AS gets its own one instead of being tacked onto the last one of these
				if (mode == ParseMode.ParsePrototype)
					{
					TokenIterator lookbehind = lookahead;
					lookbehind.Previous();
					lookbehind.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;
					}
				else if (mode == ParseMode.SyntaxHighlight)
					{
					startOfModifiers.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Metadata);
					}

				iterator = lookahead;
				}

			return true;
			}


		/* Function: TryToSkipParenthesizedParameters
		 *
		 * If the passed prototype is on the opening parenthesis of a list of parameters, such as function parameters or an inline
		 * table declaration, moves the iterator past it, sets the relevant token types, and returns true.  Otherwise no tokens are
		 * set, the iterator is left alone, and it returns false.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipParenthesizedParameters (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '(')
				{  return false;  }

			TokenIterator lookahead = iterator;

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfParams;  }

			lookahead.Next();
			TokenIterator startOfParameter = lookahead;

			for (;;)
				{
				if (lookahead.IsInBounds == false)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				else if (lookahead.Character == ',')
					{
					if (mode != ParseMode.IterateOnly)
						{
						if (mode == ParseMode.ParsePrototype)
							{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;  }

						TryToSkipWhitespace(ref startOfParameter, true, mode);

						if (startOfParameter < lookahead)
							{  ParseVariable(startOfParameter, lookahead, mode);  }
						}

					lookahead.Next();
					startOfParameter = lookahead;
					}

				else if (lookahead.Character == ')')
					{
					if (mode != ParseMode.IterateOnly)
						{
						if (mode == ParseMode.ParsePrototype)
							{  lookahead.PrototypeParsingType = PrototypeParsingType.EndOfParams;  }

						TryToSkipWhitespace(ref startOfParameter, true, mode);

						if (startOfParameter < lookahead)
							{  ParseVariable(startOfParameter, lookahead, mode);  }
						}

					lookahead.Next();
					iterator = lookahead;
					return true;
					}

				else
					{
					// This will skip entire parentheses in one step so it won't trip on the comma in "NUMBER(5,6)"
					GenericSkip(ref lookahead, mode);
					}
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
		 *			  <PrototypeParsingType.Type>.  If set to Type, it will use <PrototypeParsingType.Type>, <PrototypeParsingType.TypeModifier>,
		 *			  and <PrototypeParsingType.TypeQualifier>.
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
														   PrototypeParsingType prototypeParsingType = PrototypeParsingType.Name)
			{
			TokenIterator lookahead = iterator;
			TokenIterator endOfIdentifier;
			TokenIterator endOfQualifier = iterator;

			for (;;)
				{
				if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode, prototypeParsingType) == false)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				endOfIdentifier = lookahead;

				if (lookahead.Character != '.')
					{  break;  }

				lookahead.Next();
				endOfQualifier = lookahead;
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
		 * nothing is changed and it returns false.  This only handles a single unqualified identifier, with the exception of bracketed
		 * Microsoft identifiers like "[A.B]".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.Name> or
		 *			  <PrototypeParsingType.Type>.  If set to Type, it will use <PrototypeParsingType.Type> and possibly
		 *			  <PrototypeParsingType.TypeModifier>.
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipUnqualifiedIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
																		   PrototypeParsingType prototypeParsingType = PrototypeParsingType.Name)
			{
			TokenIterator lookahead = iterator;

			// Bracketed identifiers.  Microsoft allows identifiers to be in brackets and contain things that are not otherwise allowed,
			// like spaces.
			if (lookahead.Character == '[')
				{
				TokenIterator openingBracket = lookahead;

				do
					{  lookahead.Next();  }
				while (lookahead.IsInBounds && lookahead.Character != ']');

				if (lookahead.Character != ']')
					{  return false;  }

				lookahead.Next();

				if (mode == ParseMode.ParsePrototype)
					{  iterator.SetPrototypeParsingTypeBetween(lookahead, prototypeParsingType);  }

				iterator = lookahead;
				return true;
				}

			// Unbracketed identifiers.  Microsoft's can have @, $, and #.  Oracle's can have $ and #, plus be extended by %TYPE
			// or %ROWTYPE.
			else if (OnIdentifierToken(lookahead) &&
					  (lookahead.Character >= '0' && lookahead.Character <= '9') == false)
				{
				lookahead.Next();

				while (OnIdentifierToken(lookahead))
					{  lookahead.Next();  }

				if (mode == ParseMode.ParsePrototype)
					{  iterator.SetPrototypeParsingTypeBetween(lookahead, prototypeParsingType);  }

				iterator = lookahead;

				if (lookahead.MatchesAcrossTokens("%TYPE", true))
					{
					if (mode == ParseMode.ParsePrototype &&
						prototypeParsingType == PrototypeParsingType.Type)
						{  lookahead.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.TypeModifier, 5);  }
					else if (mode == ParseMode.SyntaxHighlight)
						{  lookahead.SetSyntaxHighlightingTypeByCharacters(SyntaxHighlightingType.Keyword, 5);  }

					lookahead.NextByCharacters(5);
					iterator = lookahead;
					}
				else if (lookahead.MatchesAcrossTokens("%ROWTYPE", true))
					{
					if (mode == ParseMode.ParsePrototype &&
						prototypeParsingType == PrototypeParsingType.Type)
						{  lookahead.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.TypeModifier, 8);  }
					else if (mode == ParseMode.SyntaxHighlight)
						{  lookahead.SetSyntaxHighlightingTypeByCharacters(SyntaxHighlightingType.Keyword, 8);  }

					lookahead.NextByCharacters(8);
					iterator = lookahead;
					}

				return true;
				}

			else
				{  return false;  }
			}


		/* Function: TryToSkipBuiltInType
		 *
		 * If the passed iterator is on a built-in data type such as "INTEGER", moves the iterator past it and all its modifiers, marks
		 * the tokens if required, and returns true.  If the iterator isn't on a built-in type nothing is changed and it returns false.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Marks the tokens with <PrototypeParsingType.Type>, <PrototypeParsingType.TypeModifier>, etc.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipBuiltInType (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			return TryToSkipBuiltInType(ref iterator, iterator.Tokenizer.LastToken, mode);
			}


		/* Function: TryToSkipBuiltInType
		 *
		 * If the passed iterator is on a built-in data type such as "INTEGER", moves the iterator past it and all its modifiers, marks
		 * the tokens if required, and returns true.  If the iterator isn't on a built-in type nothing is changed and it returns false.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Marks the tokens with <PrototypeParsingType.Type>, <PrototypeParsingType.TypeModifier>, etc.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipBuiltInType (ref TokenIterator iterator, TokenIterator limit, ParseMode mode = ParseMode.IterateOnly)
			{
			string matchingPhrase;

			if (iterator >= limit)
				{  return false;  }


			// Simple types, some of which can be followed by parenthesis, like "CHAR(12)"

			if (OnAnyKeyword(iterator, "CHAR", "VARCHAR", "VARCHAR2", "TEXT", "NCHAR", "NVARCHAR", "NVARCHAR2", "NTEXT",
													"NUMBER", "NUMERIC", "DECIMAL", "DEC", "INTEGER", "INT", "BIGINT", "SMALLINT", "TINYINT", "LONG",
													"FLOAT", "REAL",
													"DATE", "TIME", "DATETIME", "DATETIME2", "DATETIMEOFFSET", "SMALLDATETIME",
													"BOOLEAN", "BIT",
													"ROWID", "UROWID",
													"MONEY", "SMALLMONEY",
													"RAW", "BINARY", "VARBINARY", "IMAGE", "BLOB", "CLOB", "NCLOB", "BFILE"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  iterator.PrototypeParsingType = PrototypeParsingType.Type;  }
				else if (mode == ParseMode.SyntaxHighlight)
					{  iterator.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

				iterator.Next();

				TokenIterator lookahead = iterator;
				TryToSkipWhitespace(ref lookahead, true, mode);

				// Also accept VARYING for character types
				if (OnKeyword(lookahead, "VARYING") && lookahead < limit)
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }
					else if (mode == ParseMode.SyntaxHighlight)
						{  lookahead.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

					lookahead.Next();
					iterator = lookahead;

					TryToSkipWhitespace(ref lookahead, true, mode);
					}

				if (TryToSkipTypeParentheses(ref lookahead, mode) && lookahead < limit)
					{  iterator = lookahead;  }

				return true;
				}


			// Phrases like "LONG RAW" and "BINARY_FLOAT".  Since keywords with underscores occupy multiple tokens we
			// need to treat them as phrases.

			else if (OnAnyKeyphrase(iterator, out matchingPhrase, "BINARY_FLOAT", "BINARY_DOUBLE", "BINARY_INTEGER",
																							"PLS_INTEGER", "SIMPLE_INTEGER",
																							"LONG RAW", "DOUBLE PRECISION"))
				{
				TokenIterator lookahead = iterator;
				lookahead.NextByCharacters(matchingPhrase.Length);

				if (lookahead < limit)
					{
					if (mode == ParseMode.ParsePrototype)
						{  iterator.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.Type);  }
					else if (mode == ParseMode.SyntaxHighlight)
						{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Keyword);  }

					iterator = lookahead;
					return true;
					}
				else
					{  return false;  }
				}


			// Funky stuff that could have a lot of following words in various permutations, like "INTERVAL DAY (2) TO SECOND (6)"
			// or "TIMESTAMP (6) WITH LOCAL TIME ZONE".  Rather than build parsers for every possible one, we'll accept keywords
			// and parentheses up until the next symbol or reserved word like "BEGIN".

			else if (OnAnyKeyword(iterator, "TIMESTAMP", "INTERVAL", "NATIONAL"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  iterator.PrototypeParsingType = PrototypeParsingType.Type;  }
				else if (mode == ParseMode.SyntaxHighlight)
					{  iterator.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

				iterator.Next();
				TokenIterator lookahead = iterator;

				while (lookahead < limit)
					{
					TryToSkipWhitespace(ref lookahead, true, mode);

					if (TryToSkipTypeParentheses(ref lookahead, mode) && lookahead < limit)
						{  iterator = lookahead;  }
					else if (OnAnyKeyword(lookahead, "BEGIN", "IS", "RETURN", "RETURNS", "DEFAULT"))
						{  break;  }
					else if (OnKeyword(lookahead) && lookahead < limit)
						{
						if (mode == ParseMode.ParsePrototype)
							{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }
						else if (mode == ParseMode.SyntaxHighlight)
							{  lookahead.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

						lookahead.Next();
						iterator = lookahead;
						}
					else // symbols, etc.
						{  break;  }
					}

				return true;
				}


			else
				{  return false;  }
			}


		/* Function: TryToSkipTypeParentheses
		 *
		 * If the passed iterator is on the opening parenthesis of a type parenthetical, such as "(12)" in "CHAR(12)", moves the
		 * iterator past it, marks the tokens if required, and returns true.  If the iterator isn't on type parentheses nothing is
		 * changed and it returns false.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Marks the parentheses with <PrototypeParsingType.OpeningTypeModifier> and <PrototypeParsingType.ClosingTypeModifier>.
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipTypeParentheses (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '(')
				{  return false;  }

			TokenIterator lookahead = iterator;

			lookahead.Next();
			GenericSkipUntilAfter(ref lookahead, ')', mode);

			TokenIterator closingParen = lookahead;
			closingParen.Previous();

			if (closingParen.Character != ')')
				{  return false;  }

			if (mode == ParseMode.ParsePrototype)
				{
				iterator.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;
				closingParen.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
				}
			else if (mode == ParseMode.SyntaxHighlight)
				{
				SimpleSyntaxHighlightBetween(iterator, lookahead);
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipString
		 *
		 * If the passed iterator is on a string literal, moves the iterator past it and returns true.  If not, the iterator isn't changed and
		 * it returns false.  This is different from <Language.TryToSkipString()> because SQL handles embedded quotes with double
		 * characters instead of backslashes.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipString (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			char delimiter = iterator.Character;

			if (delimiter != '"' && delimiter != '\'')
				{  return false;  }

			TokenIterator startOfString = iterator;
			iterator.Next();

			// We can work directly on iterator instead of creating a lookahead because if it's unclosed it's going to the end of the file.
			while (iterator.IsInBounds)
				{
				if (iterator.Character == delimiter)
					{
					iterator.Next();

					// Check for doubled delimiters
					if (iterator.Character == delimiter)
						{  iterator.Next();  }
					else
						{  break;  }
					}
				else
					{  iterator.Next();  }
				}

			if (mode == ParseMode.SyntaxHighlight)
				{  startOfString.SetSyntaxHighlightingTypeBetween(iterator, SyntaxHighlightingType.String);  }

			return true;
			}


		/* Function: GenericSkip
		 *
		 * Advances the iterator one place through general code.
		 *
		 * - If the position is on a string, it will skip it completely.
		 * - If the position is on an opening brace, parenthesis, or bracket it will skip until the past the closing symbol.
		 * - If the position is on whitespace, comments, or identifiers it will skip it completely.  Skipping identifiers in one step
		 *   prevents things like @WITH from being mistaken as a keyword.
		 * - Otherwise it skips one token.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected void GenericSkip (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character == '(')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, ')', mode);
				}
			else if (iterator.Character == '[')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, ']', mode);
				}
			else if (iterator.Character == '{')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, '}', mode);
				}

			else if (TryToSkipString(ref iterator, mode) ||
					  TryToSkipNumber(ref iterator, mode) ||
					  TryToSkipWhitespace(ref iterator, true, mode))
				{  }

			else if (mode == ParseMode.SyntaxHighlight)
				{
				if (iterator.FundamentalType == FundamentalType.Text)
					{
					string token = iterator.String;

					string matchingPhrase;
					string[] matchingPhrases = generalKeyphrases[token];

					if (matchingPhrases != null &&
						OnAnyKeyphrase(iterator, out matchingPhrase, matchingPhrases))
						{
						iterator.SetSyntaxHighlightingTypeByCharacters(SyntaxHighlightingType.Keyword, matchingPhrase.Length);
						iterator.NextByCharacters(matchingPhrase.Length);
						}
					else if (generalKeywords.Contains(token) &&
							   OnKeyword(iterator))
						{
						iterator.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;
						iterator.Next();
						}
					else
						{  iterator.Next();  }
					}

				else if (iterator.MatchesAcrossTokens("%TYPE", true))
					{
					iterator.SetSyntaxHighlightingTypeByCharacters(SyntaxHighlightingType.Keyword, 5);
					iterator.NextByCharacters(5);
					}
				else if (iterator.MatchesAcrossTokens("%ROWTYPE", true))
					{
					iterator.SetSyntaxHighlightingTypeByCharacters(SyntaxHighlightingType.Keyword, 8);
					iterator.NextByCharacters(8);
					}
				else
					{  iterator.Next();  }
				}

			else
				{
				iterator.Next();
				}
			}


		/* Function: GenericSkipUntilAfter
		 *
		 * Advances the iterator via <GenericSkip()> until a specific symbol is reached and passed.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected void GenericSkipUntilAfter (ref TokenIterator iterator, char symbol, ParseMode mode = ParseMode.IterateOnly)
			{
			while (iterator.IsInBounds)
				{
				if (iterator.Character == symbol)
					{
					iterator.Next();
					break;
					}
				else
					{  GenericSkip(ref iterator, mode);  }
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: generalKeywords
		 * A set of general keywords for syntax highlighting.
		 */
		static protected StringSet generalKeywords = new StringSet (KeySettings.IgnoreCase, new string[] {

			"ACCESSIBLE", "ADD", "AGENT", "AGGREGATE", "ALL", "ALTER", "AND", "ANY", "ARRAY", "AS", "ASC", "AT", "ATTRIBUTE",
			"AUTHID", "AUTHORIZATION", "AVG", "BACKUP", "BEGIN", "BETWEEN", "BFILE", "BIGINT", "BINARY", "BIT", "BLOB", "BLOCK",
			"BODY", "BOOLEAN", "BOTH", "BOUND", "BREAK", "BROWSE", "BULK", "BY", "BYTE", "CALL", "CALLING", "CASCADE", "CASE",
			"CHAR", "CHARACTER", "CHARSET", "CHARSETFORM", "CHARSETID", "CHECK", "CHECKPOINT", "CLOB", "CLONE", "CLOSE",
			"CLUSTER", "CLUSTERED", "CLUSTERS", "COALESCE", "COLAUTH", "COLLATE", "COLLECT", "COLUMN", "COLUMNS", "COMMENT",
			"COMMIT", "COMMITTED", "COMPILED", "COMPRESS", "COMPUTE", "CONNECT", "CONSTANT", "CONSTRAINT", "CONSTRUCTOR",
			"CONTAINS", "CONTAINSTABLE", "CONTEXT", "CONTINUE", "CONVERT", "COUNT", "CRASH", "CREATE", "CREDENTIAL", "CROSS",
			"CURRENT", "CURSOR", "CUSTOMDATUM", "DANGLING", "DATA", "DATABASE", "DATE", "DATETIME", "DATETIME2",
			"DATETIMEOFFSET", "DAY", "DBCC", "DEALLOCATE", "DEC", "DECIMAL", "DECLARE", "DEFAULT", "DEFINE", "DELETE", "DENY",
			"DESC", "DETERMINISTIC", "DIRECTORY", "DISK", "DISTINCT", "DISTRIBUTED", "DOUBLE", "DROP", "DUMP", "DURATION",
			"ELEMENT", "ELSE", "ELSIF", "EMPTY", "END", "ERRLVL", "ESCAPE", "EXCEPT", "EXCEPTION", "EXCEPTIONS", "EXCLUSIVE",
			"EXEC", "EXECUTE", "EXISTS", "EXIT", "EXTERNAL", "FETCH", "FILE", "FILLFACTOR", "FINAL", "FIRST", "FIXED", "FLOAT", "FOR",
			"FORALL", "FORCE", "FOREIGN", "FREETEXT", "FREETEXTTABLE", "FROM", "FULL", "FUNCTION", "GENERAL", "GOTO", "GRANT",
			"GROUP", "HASH", "HAVING", "HEAP", "HIDDEN", "HOLDLOCK", "HOUR", "IDENTIFIED", "IDENTITY", "IDENTITYCOL", "IF",
			"IMAGE", "IMMEDIATE", "IN", "INCLUDING", "INDEX", "INDEXES", "INDICATOR", "INDICES", "INFINITE", "INNER", "INSERT",
			"INSTANTIABLE", "INT", "INTEGER", "INTERFACE", "INTERSECT", "INTERVAL", "INTO", "INVALIDATE", "IS", "ISOLATION", "JAVA",
			"JOIN", "KEY", "KILL", "LANGUAGE", "LARGE", "LEADING", "LEFT", "LENGTH", "LEVEL", "LIBRARY", "LIKE", "LIKE2", "LIKE4",
			"LIKEC", "LIMIT", "LIMITED", "LINENO", "LOAD", "LOCAL", "LOCK", "LONG", "LOOP", "MAP", "MAX", "MAXLEN", "MEMBER",
			"MERGE", "MIN", "MINUS", "MINUTE", "MOD", "MODE", "MODIFY", "MONEY", "MONTH", "MULTISET", "NAME", "NAN", "NATIONAL",
			"NATIVE", "NCHAR", "NCLOB", "NEW", "NOCHECK", "NOCOMPRESS", "NOCOPY", "NONCLUSTERED", "NOT", "NOWAIT", "NTEXT",
			"NULL", "NULLIF", "NUMBER", "NUMERIC", "NVARCHAR", "NVARCHAR2", "OBJECT", "OCICOLL", "OCIDATE", "OCIDATETIME",
			"OCIDURATION", "OCIINTERVAL", "OCILOBLOCATOR", "OCINUMBER", "OCIRAW", "OCIREF", "OCIREFCURSOR", "OCIROWID",
			"OCISTRING", "OCITYPE", "OF", "OFF", "OFFSETS", "OLD", "ON", "ONLY", "OPAQUE", "OPEN", "OPENDATASOURCE", "OPENQUERY",
			"OPENROWSET", "OPENXML", "OPERATOR", "OPTION", "OR", "ORACLE", "ORADATA", "ORDER", "ORGANIZATION", "ORLANY",
			"ORLVARY", "OTHERS", "OUT", "OUTER", "OVER", "OVERLAPS", "OVERRIDING", "PACKAGE", "PARAMETER", "PARAMETERS",
			"PARENT", "PARTITION", "PASCAL", "PERCENT", "PERSISTABLE", "PIPE", "PIPELINED", "PIVOT", "PLAN", "PLUGGABLE",
			"POLYMORPHIC", "PRAGMA", "PRECISION", "PRIMARY", "PRINT", "PRIOR", "PRIVATE", "PROC", "PROCEDURE", "PUBLIC", "RAISE",
			"RAISERROR", "RANGE", "RAW", "READ", "READTEXT", "REAL", "RECONFIGURE", "RECORD", "REF", "REFERENCE", "REFERENCES",
			"REM", "REMAINDER", "RENAME", "REPLICATION", "RESOURCE", "RESTORE", "RESTRICT", "RESULT", "RETURN", "RETURNING",
			"RETURNS", "REVERSE", "REVERT", "REVOKE", "RIGHT", "ROLLBACK", "ROW", "ROWCOUNT", "ROWGUIDCOL", "ROWID", "RULE",
			"SAMPLE", "SAVE", "SAVEPOINT", "SB1", "SB2", "SB4", "SCHEMA", "SECOND", "SECURITYAUDIT", "SEGMENT", "SELECT", "SELECT",
			"SELF", "SEMANTICKEYPHRASETABLE", "SEMANTICSIMILARITYDETAILSTABLE", "SEMANTICSIMILARITYTABLE", "SEPARATE",
			"SEQUENCE", "SERIALIZABLE", "SET", "SETUSER", "SHARE", "SHORT", "SHUTDOWN", "SIZE", "SMALLDATETIME", "SMALLINT",
			"SMALLMONEY", "SOME", "SPARSE", "SQL", "SQLCODE", "SQLDATA", "SQLNAME", "SQLSTATE", "STANDARD", "START", "STATIC",
			"STATISTICS", "STDDEV", "STORED", "STRING", "STRUCT", "STYLE", "SUBMULTISET", "SUBPARTITION", "SUBSTITUTABLE",
			"SUBTYPE", "SUM", "SYNONYM", "TABAUTH", "TABLE", "TABLESAMPLE", "TDO", "TEXT", "TEXTSIZE", "THE", "THEN", "TIME",
			"TIMESTAMP", "TINYINT", "TO", "TOP", "TRAILING", "TRAN", "TRANSACTION", "TRANSACTIONAL", "TRIGGER", "TRUNCATE",
			"TRUSTED", "TSEQUAL", "TYPE", "UB1", "UB2", "UB4", "UNDER", "UNION", "UNIQUE", "UNPIVOT", "UNPLUG", "UNSIGNED",
			"UNTRUSTED", "UPDATE", "UPDATETEXT", "UROWID", "USE", "USER", "USING", "VALIST", "VALUE", "VALUES", "VARBINARY",
			"VARCHAR", "VARCHAR2", "VARIABLE", "VARIANCE", "VARRAY", "VARYING", "VIEW", "VIEWS", "VOID", "WAITFOR", "WHEN",
			"WHERE", "WHILE", "WITH", "WORK", "WRAPPED", "WRITE", "WRITETEXT", "YEAR", "ZONE"

			});


		/* var: generalKeyphrases
		 * General keyphrases for syntax highlighting.  It maps the first token of the keyphrase to an array of the full tokens.
		 */
		static protected StringTable<string[]> generalKeyphrases;

		}
	}
