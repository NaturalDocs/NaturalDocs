/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Lua
 * ____________________________________________________________________________
 *
 * Additional language support for Lua.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Comments;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class Lua : Parser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Lua
		 */
		public Lua (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: GetPossibleDocumentationComments
		 *
		 * Goes through the file looking for comments that could possibly contain documentation and returns them as a list.  These
		 * comments are not guaranteed to have documentation in them, just to be acceptable candidates for them.  If there are no
		 * comments it will return an empty list.
		 *
		 * All the comments in the returned list will have their comment symbols marked as <CommentParsingType.CommentSymbol>
		 * in the tokenizer.  This allows further operations to be done on them in a language independent manner.  If you want to also
		 * filter out text boxes and lines, use <Comments.LineFinder>.
		*/
		override public List<PossibleDocumentationComment> GetPossibleDocumentationComments (Tokenizer source)
			{
			List<PossibleDocumentationComment> possibleDocumentationComments = new List<PossibleDocumentationComment>();

			LineIterator lineIterator = source.FirstLine;

			while (lineIterator.IsInBounds)
				{
				PossibleDocumentationComment possibleDocumentationComment = null;

				if (TryToGetBlockComment(ref lineIterator, out possibleDocumentationComment) ||
					TryToGetLineComment(ref lineIterator, out possibleDocumentationComment))
					{
					if (possibleDocumentationComment != null)
						{
						// XML can actually use the Javadoc comment format in addition to its own.
						if (possibleDocumentationComment.Javadoc == true)
							{  possibleDocumentationComment.XML = true;  }

						possibleDocumentationComments.Add(possibleDocumentationComment);
						}

					// lineIterator should already be moved
					}
				else
					{  lineIterator.Next();  }
				}

			return possibleDocumentationComments;
			}


		/* Function: TryToGetBlockComment
		 *
		 * If the iterator is on a line that starts with the opening symbol of a block comment, this function moves the iterator
		 * past the entire comment and returns true.  If the comment is a candidate for documentation it will also return it as
		 * a <PossibleDocumentationComment> and mark the symbols as <CommentParsingType.CommentSymbol>.  If the
		 * line does not start with an opening comment symbol it will return false and leave the iterator where it is.
		 */
		protected bool TryToGetBlockComment (ref LineIterator lineIterator, out PossibleDocumentationComment comment)
			{
			TokenIterator firstToken, endOfLine;
			lineIterator.GetBounds(LineBoundsMode.ExcludeWhitespace, out firstToken, out endOfLine);


			// Are we on a block comment?

			TokenIterator lookahead = firstToken;
			string closingSymbol;

			if (TryToSkipOpeningBlockCommentSymbol(ref lookahead, out closingSymbol) == false)
				{
				comment = null;
				return false;
				}


			// We are.  Create a possible documentation comment.

			comment = new PossibleDocumentationComment();
			comment.Start = lineIterator;


			// Check if we're on a Javadoc comment, which will be an extra [.

			if (lookahead.Character == '[')
				{
				lookahead.Next();

				if (lookahead.FundamentalType != FundamentalType.Symbol)
					{  comment.Javadoc = true;  }
				}


			// Find the end of the comment, which could be on the same line as the start.

			var tokenizer = lineIterator.Tokenizer;
			var lineLookahead = lineIterator;
			bool hadTrailingDashes = false;

			for (;;)
				{
				TokenIterator closingSymbolIterator;

				if (tokenizer.FindTokensBetween(closingSymbol, false, firstToken, endOfLine, out closingSymbolIterator) == true)
					{
					// Move past the end of the comment regardless of whether it's acceptable for documentation or not
					lineLookahead.Next();

					// Make sure nothing appears after the closing symbol on the line
					closingSymbolIterator.NextByCharacters(closingSymbol.Length);

					// We'll allow -- though since some people use --[[ and ]]-- for balance even though the latter is actually the
					// closing comment symbol followed by a line comment.
					if (closingSymbolIterator.MatchesAcrossTokens("--"))
						{
						hadTrailingDashes = true;
						closingSymbolIterator.Next(2);
						}

					closingSymbolIterator.NextPastWhitespace();

					if (closingSymbolIterator.FundamentalType != FundamentalType.LineBreak &&
						closingSymbolIterator.FundamentalType != FundamentalType.Null)
						{  comment = null;  }
					else
						{  comment.End = lineLookahead;  }

					break;
					}

				lineLookahead.Next();

				// If we're not in bounds that means there was an unclosed comment at the end of the file.  Skip it but don't treat
				// it as a documentation candidate.
				if (!lookahead.IsInBounds)
					{
					comment = null;
					break;
					}

				lineLookahead.GetBounds(LineBoundsMode.ExcludeWhitespace, out firstToken, out endOfLine);
				}


			if (comment != null)
				{
				// Mark the symbols before returning

				firstToken = comment.Start.FirstToken(LineBoundsMode.ExcludeWhitespace);
				lookahead = firstToken;
				TryToSkipOpeningBlockCommentSymbol(ref lookahead, out closingSymbol);

				if (comment.Javadoc)
					{  lookahead.Next();  }

				firstToken.SetCommentParsingTypeBetween(lookahead, CommentParsingType.CommentSymbol);

				LineIterator lastLine = comment.End;
				lastLine.Previous();
				lastLine.GetBounds(LineBoundsMode.ExcludeWhitespace, out firstToken, out endOfLine);

				lookahead = endOfLine;

				if (hadTrailingDashes)
					{  lookahead.Previous(2);  }

				lookahead.PreviousByCharacters(closingSymbol.Length);

				lookahead.SetCommentParsingTypeBetween(endOfLine, CommentParsingType.CommentSymbol);
				}

			// If we made it this far that means we found a comment and can move the line iterator and return true.  Whether
			// that comment was suitable for documentation will be determined by the comment variable, but we are moving the
			// iterator and returning true either way.
			lineIterator = lineLookahead;
			return true;
			}


		/* Function: TryToGetLineComment
		 *
		 * If the iterator is on a line that starts with a line comment symbol, this function moves the iterator past the entire
		 * comment and returns true.  If the comment is a candidate for documentation it will also return it as a
		 * <PossibleDocumentationComment> and mark the symbols as <CommentParsingType.CommentSymbol>.  If the
		 * line does not start with a line comment symbol it will return false and leave the iterator where it is.
		 */
		protected bool TryToGetLineComment (ref LineIterator lineIterator, out PossibleDocumentationComment comment)
			{
			TokenIterator firstToken = lineIterator.FirstToken(LineBoundsMode.ExcludeWhitespace);
			TokenIterator lookahead = firstToken;


			// Are we on a line comment?

			if (TryToSkipLineCommentSymbol(ref lookahead) == false)
				{
				comment = null;
				return false;
				}


			// We are.  Create a possible documentation comment.

			comment = new PossibleDocumentationComment();
			comment.Start = lineIterator;


			// Check if we're a Javadoc/XML comment.  We can't tell the difference from just the first line.

			TokenIterator endOfCommentSymbol = lookahead;

			if (lookahead.Character == '-')
				{
				lookahead.Next();

				if (lookahead.FundamentalType != FundamentalType.Symbol)
					{
					endOfCommentSymbol = lookahead;
					comment.Javadoc = true;
					comment.XML = true;
					}
				}


			// Mark it.

			firstToken.SetCommentParsingTypeBetween(endOfCommentSymbol, CommentParsingType.CommentSymbol);


			// Continue to find the rest of the comment

			lineIterator.Next();
			bool hasXMLishLines = false;
			bool hasNonXMLishLines = false;
			bool hasMultipleLines = false;

			while (lineIterator.IsInBounds)
				{
				firstToken = lineIterator.FirstToken(LineBoundsMode.ExcludeWhitespace);
				lookahead = firstToken;

				if (TryToSkipLineCommentSymbol(ref lookahead) == false)
					{  break;  }

				hasMultipleLines = true;
				endOfCommentSymbol = lookahead;

				if (lookahead.Character == '-')
					{
					lookahead.Next();

					if (lookahead.FundamentalType != FundamentalType.Symbol)
						{
						hasXMLishLines = true;
						endOfCommentSymbol = lookahead;
						}
					else
						{  hasNonXMLishLines = true;  }
					}
				else
					{  hasNonXMLishLines = true;  }

				firstToken.SetCommentParsingTypeBetween(endOfCommentSymbol, CommentParsingType.CommentSymbol);
				lineIterator.Next();
				}

			comment.End = lineIterator;

			if (hasMultipleLines && comment.Javadoc)
				{
				if (hasXMLishLines && !hasNonXMLishLines)
					{  comment.Javadoc = false;  }
				else if (hasNonXMLishLines)
					{  comment.XML = false;  }
				}

			return true;
			}


		/* Function: TryToSkipOpeningBlockCommentSymbol
		 *
		 * If the iterator is on an opening block comment symbol, moves it past it and returns true and the closing symbol.  Otherwise
		 * leaves the iterator alone and returns false and null.  This handles --[[ comments as well as forms with equals signs such as
		 * --[==[.
		 */
		protected bool TryToSkipOpeningBlockCommentSymbol (ref TokenIterator iterator, out string closingSymbol)
			{
			if (iterator.MatchesAcrossTokens("--[") == false)
				{
				closingSymbol = null;
				return false;
				}

			TokenIterator lookahead = iterator;
			lookahead.Next(3);

			int equals = 0;
			while (lookahead.Character == '=')
				{
				lookahead.Next();
				equals++;
				}

			if (lookahead.Character != '[')
				{
				closingSymbol = null;
				return false;
				}

			lookahead.Next();
			iterator = lookahead;

			if (equals == 0)
				{  closingSymbol = "]]";  }
			else
				{
				StringBuilder closingSymbolBuilder = new StringBuilder(2 + equals);
				closingSymbolBuilder.Append(']');
				closingSymbolBuilder.Append('=', equals);
				closingSymbolBuilder.Append(']');
				closingSymbol = closingSymbolBuilder.ToString();
				}

			return true;
			}


		/* Function: TryToSkipLineCommentSymbol
		 *
		 * If the iterator is on a line comment, moves it past it and returns true.  Otherwise leaves the iterator alone and
		 * returns false. This handles the fact that block comments also start with -- and will not move past them.
		 */
		protected bool TryToSkipLineCommentSymbol (ref TokenIterator iterator)
			{
			if (iterator.MatchesAcrossTokens("--") &&
				!IsOnOpeningBlockCommentSymbol(iterator))
				{
				iterator.Next(2);
				return true;
				}
			else
				{
				return false;
				}
			}


		/* Function: IsOnOpeningBlockCommentSymbol
		 *
		 * Returns whether  the iterator is on an opening block comment symbol.  This handles --[[ comments as well as forms with
		 * equals signs such as --[==[.
		 */
		protected bool IsOnOpeningBlockCommentSymbol (TokenIterator iterator)
			{
			if (iterator.MatchesAcrossTokens("--[") == false)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next(3);

			while (lookahead.Character == '=')
				{  lookahead.Next();  }

			if (lookahead.Character == '[')
				{  return true;  }
			else
				{  return false;  }
			}

		}
	}
