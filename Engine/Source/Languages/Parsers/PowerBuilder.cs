/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.PowerBuilder
 * ____________________________________________________________________________
 *
 * Additional language support for PowerBuilder.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
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
			if (iterator.FundamentalType == FundamentalType.Text &&
				powerbuilderKeywords.Contains(iterator.String))
				{
				if (mode == ParseMode.SyntaxHighlight)
					{  iterator.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

				iterator.Next();
				return true;
				}

			// "_debug" is the only keyword with an underscore anywhere in it
			else if (iterator.MatchesAcrossTokens("_debug"))
				{
				TokenIterator lookahead = iterator;
				lookahead.Next(2);

				if (mode == ParseMode.SyntaxHighlight)
					{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Keyword);  }

				iterator = lookahead;
				return true;
				}

			else
				{  return false;  }
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
