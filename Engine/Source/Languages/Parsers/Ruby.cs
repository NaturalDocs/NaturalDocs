/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Ruby
 * ____________________________________________________________________________
 *
 * Additional language support for Ruby.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class Ruby : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Ruby
		 */
		public Ruby (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
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
			ParsedClassPrototype parsedPrototype = new ParsedClassPrototype(tokenizedPrototype);
			bool success = false;

			success = TryToSkipClassDeclarationLine(ref startOfPrototype, ParseMode.ParseClassPrototype);

			if (success)
				{  return parsedPrototype;  }
			else
			    {  return base.ParseClassPrototype(stringPrototype, commentTypeID);  }
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


			// Keyword

			if (lookahead.MatchesToken("class") == false)
				{  return false;  }

			if (mode == ParseMode.ParseClassPrototype)
				{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.Keyword;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Name

			TokenIterator startOfIdentifier = lookahead;

			if (TryToSkipUnqualifiedIdentifier(ref lookahead) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParseClassPrototype)
				{  startOfIdentifier.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.Name);  }

			TryToSkipWhitespace(ref lookahead);


			// Base class

			if (lookahead.Character == '<')
				{
				if (mode == ParseMode.ParseClassPrototype)
					{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfParents;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				TokenIterator startOfParent = lookahead;

				if (TryToSkipUnqualifiedIdentifier(ref lookahead) == false)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				if (mode == ParseMode.ParseClassPrototype)
					{  startOfParent.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.Name);  }
				}


			iterator = lookahead;
			return true;
			}

		}
	}
