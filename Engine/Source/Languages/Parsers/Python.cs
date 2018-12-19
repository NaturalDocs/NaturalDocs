/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Python
 * ____________________________________________________________________________
 * 
 * Additional language support for Python.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class Python : Language
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Python
		 */
		public Python (Languages.Manager manager) : base (manager, "Python")
			{
			}


		/* Function: ParseClassPrototype
		 * Converts a raw text prototype into a <ParsedClassPrototype>.  Will return null if it is not an appropriate prototype.
		 */
		override public ParsedClassPrototype ParseClassPrototype (string stringPrototype, int commentTypeID)
			{
			if (EngineInstance.CommentTypes.FromID(commentTypeID).Flags.ClassHierarchy == false)
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
				{  iterator.Tokenizer.SetClassPrototypeParsingTypeBetween(startOfIdentifier, lookahead, ClassPrototypeParsingType.Name);  }

			TryToSkipWhitespace(ref lookahead);


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


		/* Function: TryToSkipDecorators
		 * 
		 * Tries to move the iterator past a group of decorators.
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark each decorator with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and <ClassPrototypeParsingType.PrePrototypeLine>.
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
		 * Tries to move the iterator past a single decorator.  Note that there may be more than one decorator in a row, so use <TryToSkipDecorators()>
		 * if you need to move past all of them.
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark the first token with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and the rest with <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDecorator (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '@')
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (TryToSkipIdentifier(ref lookahead) == false)
				{  return false;  }

			TokenIterator endOfIdentifier = lookahead;

			TryToSkipWhitespace(ref lookahead);

			if (lookahead.Character == '(')
				{  
				if (TryToSkipBlock(ref lookahead, false) == false)
					{  return false;  }  
				}

			if (mode == ParseMode.ParseClassPrototype)
				{
				iterator.Tokenizer.SetClassPrototypeParsingTypeBetween(iterator, lookahead, ClassPrototypeParsingType.PrePrototypeLine);
				iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;
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
				{  lookahead.Tokenizer.SetClassPrototypeParsingTypeBetween(startOfIdentifier, lookahead, ClassPrototypeParsingType.Name);  }

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipIdentifier
		 * Tries to move the iterator past a qualified identifier, such as "X.Y.Z".  Use <TryToSkipUnqualifiedIdentifier()> if you only want
		 * to skip a single segment.
		 */
		protected bool TryToSkipIdentifier (ref TokenIterator iterator)
			{
			TokenIterator lookahead = iterator;

			for (;;)
				{
				if (TryToSkipUnqualifiedIdentifier(ref lookahead) == false)
					{  return false;  }

				if (lookahead.Character == '.')
					{  lookahead.Next();  }
				else
					{  break;  }
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipUnqualifiedIdentifier
		 * Tries to move the iterator past a single unqualified identifier, which means only "X" in "X.Y.Z".
		 */
		protected bool TryToSkipUnqualifiedIdentifier (ref TokenIterator iterator)
			{
			if (iterator.FundamentalType == FundamentalType.Text)
				{
				if (iterator.Character >= '0' && iterator.Character <= '9')
					{  return false;  }
				}
			else if (iterator.FundamentalType == FundamentalType.Symbol)
				{
				if (iterator.Character != '_')
					{  return false;  }
				}
			else
				{  return false;  }

			do
				{  iterator.Next();  }
			while (iterator.FundamentalType == FundamentalType.Text || iterator.Character == '_');

			return true;
			}

		}
	}