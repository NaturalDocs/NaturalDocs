/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.PowerBuilder
 * ____________________________________________________________________________
 *
 * Additional language support for PowerBuilder.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class PowerBuilder : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: PowerBuilder
		 */
		public PowerBuilder (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
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
			// Handle "_debug" separately since it's the only one that starts with an underscore
			if (iterator.MatchesAcrossTokens("_debug", true))
				{
				TokenIterator lookahead = iterator;
				lookahead.Next(2);

				if (lookahead.Character == '_' ||
					lookahead.FundamentalType == FundamentalType.Text)
					{  return false;  }

				TokenIterator lookbehind = iterator;
				lookbehind.Previous();

				if (lookbehind.Character == '_' ||
					lookbehind.FundamentalType == FundamentalType.Text)
					{  return false;  }

				if (mode == ParseMode.SyntaxHighlight)
					{  iterator.SetSyntaxHighlightingTypeByCharacters(SyntaxHighlightingType.Keyword, 6);  }

				iterator.Next(2);
				return true;
				}

			// All other keywords are a single text token
			else
				{
				if (iterator.FundamentalType != FundamentalType.Text)
					{  return false;  }

				TokenIterator lookahead = iterator;
				lookahead.Next();

				if (lookahead.Character == '_' ||
					lookahead.FundamentalType == FundamentalType.Text)
					{  return false;  }

				TokenIterator lookbehind = iterator;
				lookbehind.Previous();

				if (lookbehind.Character == '_' ||
					lookbehind.FundamentalType == FundamentalType.Text)
					{  return false;  }

				if (!defaultKeywords.Contains(iterator.String))
					{  return false;  }

				if (mode == ParseMode.SyntaxHighlight)
					{  iterator.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

				iterator.Next();
				return true;
				}
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
			if (iterator.Character != '\'' && iterator.Character != '\"')
				{  return false;  }

			char openingCharacter = iterator.Character;

			TokenIterator lookahead = iterator;
			lookahead.Next();

			while (lookahead.IsInBounds)
				{
				if (lookahead.Character == '~')
					{  lookahead.Next(2);  }

				else if (lookahead.Character == openingCharacter)
					{
					lookahead.Next();
					break;
					}

				else
					{  lookahead.Next();  }
				}

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.String);  }

			iterator = lookahead;
			return true;
			}



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: powerbuliderKeywords
		 */
		static protected StringSet powerbuilderKeywords = new StringSet (KeySettings.IgnoreCase, new string[] {

			"alias", "and", "autoinstantiate", "call", "case", "catch", "choose", "close", "commit", "connect", "constant", "continue", "create",
			"cursor", "declare", "delete", "describe", "descriptor", "destroy", "disconnect", "do", "dynamic", "else", "elseif", "end", "enumerated",
			"event", "execute", "exit", "external", "false", "fetch", "finally", "first", "for", "forward", "from", "function", "global", "goto", "halt",
			"if", "immediate", "indirect", "insert", "into", "intrinsic", "is", "last", "library", "loop", "namespace", "native", "next", "not", "of", "on",
			"open", "or", "parent", "post", "prepare", "prior", "private", "privateread", "privatewrite", "procedure", "protected", "protectedread",
			"protectedwrite", "prototypes", "public", "readonly", "ref", "return", "rollback", "rpcfunc", "select", "selectblob", "shared", "static",
			"step", "subroutine", "super", "system", "systemread", "systemwrite", "then", "this", "throw", "throws", "to", "trigger", "true", "try",
			"type", "until", "update", "updateblob", "using", "variables", "while", "with", "within", "xor", "_debug",

			"any", "blob", "boolean", "byte", "char", "character", "date", "datetime", "dec", "decimal", "double", "int", "integer", "long", "longlong",
			"longptr", "real", "string", "time", "uint", "ulong", "unsignedint", "unsignedinteger", "unsignedlong"

			});

		}
	}
