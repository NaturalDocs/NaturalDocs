/* 
 * Class: GregValure.NaturalDocs.Engine.Comments.Parsers.LineFinder
 * ____________________________________________________________________________
 * 
 * A general parser which finds vertical and horizontal lines in comments and marks them with
 * <Tokenization.CommentParsingType.CommentDecoration> so that they can be ignored in later stages of parsing.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine.Tokenization;


namespace GregValure.NaturalDocs.Engine.Comments.Parsers
	{
	public class LineFinder : Parser
		{
		
		/* Function: MarkTextBoxes
		 * 
		 * Finds all text boxes in a comment and marks their tokens as <Tokenization.CommentParsingType.CommentDecoration>.
		 * Vertical lines will only be detected if they are continuous throughout the comment and horizontal lines if they
		 * are connected to it.  Freestanding horizontal lines are *not* detected here.  This function tolerates differing
		 * symbols on corners and where embedded horizontal lines connect to the vertical.  It also tolerates tokens
		 * marked with <Tokenization.CommentParsingType.CommentSymbol> differing.
		 * 
		 * Examples:
		 * 
		 * The box below will be marked completely, including the middle horizontal line.
		 * 
		 * > // +----------+
		 * > // | Title    |
		 * > // +----------+
		 * > // | Text     |
		 * > // +----------+
		 * 
		 * The middle horizontal line below will not be marked, because it is not attached.
		 * 
		 * > // +----------+
		 * > // | Title    |
		 * > // | -------- |
		 * > // | Text     |
		 * > // +----------+
		 * 
		 * Nor will the horizontal line below since there is no vertical.
		 * 
		 * > // Title
		 * > // ----------
		 * > // Text
		 * 
		 * Freestanding horizontal lines are not detected because they may be intended literally, such as when part of
		 * a code section.  If you're not in such a section use <IsHorizontalLine()> before parsing a line to filter it out.
		 * 
		 * > // (start code)
		 * > // +-----+
		 * > // | box |
		 * > // +-----+
		 * > // (end code)
		 */
		public void MarkTextBoxes (PossibleDocumentationComment comment)
			{
			char symbolA, symbolB, symbolC;
			int symbolACount, symbolBCount, symbolCCount;
			
			char leftSymbol =	'\0';
			char rightSymbol = '\0';
			int leftSymbolCount = 0;
			int rightSymbolCount = 0;
			
			bool symbolIsAloneOnLine = false;
			bool testedForVerticalLines = false;
			
			LineIterator line = comment.Start;
			TokenIterator lineStart, lineEnd;
			
			// This should be okay to use since line numbers start at one.
			IDObjects.NumberSet horizontalLines = new IDObjects.NumberSet();

			// Skip leading blank lines since it's okay if they're not part of the text box.
			while (line < comment.End && line.IsEmpty(LineBoundsMode.CommentContent))
				{  line.Next();  }
			
			while (line < comment.End && line.IsEmpty(LineBoundsMode.CommentContent) == false)
				{
				line.GetBounds(LineBoundsMode.ExcludeWhitespace, out lineStart, out lineEnd);
				
				// Shrink the line to exclude its comment symbols, if any.  We didn't do this using the line bounds mode because
				// we need to know whether there was any whitespace between them and any horizontal lines.
				
				bool commentSymbolWithoutWhitespaceAtStart = false;
				bool commentSymbolWithoutWhitespaceAtEnd = false;
				
				if (lineStart.CommentParsingType == CommentParsingType.CommentSymbol)
					{
					commentSymbolWithoutWhitespaceAtStart = true;
					
					do
						{  lineStart.Next();  }
					while (lineStart.CommentParsingType == CommentParsingType.CommentSymbol);
					
					if (lineStart.FundamentalType == FundamentalType.Whitespace)
						{
						commentSymbolWithoutWhitespaceAtStart = false;
						
						do
							{  lineStart.Next();  }
						while (lineStart.FundamentalType == FundamentalType.Whitespace);
						}
					}
					
				lineEnd.Previous();
				if (lineEnd.CommentParsingType == CommentParsingType.CommentSymbol)
					{
					commentSymbolWithoutWhitespaceAtEnd = true;
					
					do
						{  lineEnd.Previous();  }
					while (lineEnd.CommentParsingType == CommentParsingType.CommentSymbol);
					
					if (lineEnd.FundamentalType == FundamentalType.Whitespace)
						{
						commentSymbolWithoutWhitespaceAtEnd = false;
						
						do
							{  lineEnd.Previous();  }
						while (lineEnd.FundamentalType == FundamentalType.Whitespace);
						}
					}					
				lineEnd.Next();
				
				
				// Horizontal line detection
				
				bool isHorizontalLine = false;
				
				CountSymbolLine(ref lineStart, lineEnd, out symbolA, out symbolB, out symbolC,
										out symbolACount, out symbolBCount, out symbolCCount);
										
				if (commentSymbolWithoutWhitespaceAtStart == true &&
					commentSymbolWithoutWhitespaceAtEnd == true &&
					symbolACount >= 4 && symbolBCount == 0)
					{  isHorizontalLine = true;  }
					
				else if (commentSymbolWithoutWhitespaceAtStart == true &&
						  symbolACount >= 4 &&
						  (symbolBCount == 0 || (symbolBCount <= 3 && symbolCCount == 0)) )
					{  isHorizontalLine = true;  }
					
				else if (commentSymbolWithoutWhitespaceAtEnd == true &&
						  ((symbolACount >= 1 && symbolACount <= 3 && symbolBCount >= 4 && symbolCCount == 0) ||
						   (symbolACount >= 4 && symbolBCount == 0)) )
					{  isHorizontalLine = true;  }
					
				else if ( (symbolACount >= 4 && symbolBCount == 0) ||
						   (symbolACount >= 1 && symbolACount <= 3 && symbolBCount >= 4 && symbolCCount <= 3) )
					{  isHorizontalLine = true;  }
					
				// The horizontal line has to be the only thing on the line to count.
				if (isHorizontalLine && lineStart == lineEnd)
					{
					horizontalLines.Add(line.LineNumber);
					}
					
					
				// Vertical line detection
				
				else if (testedForVerticalLines == false)
					{
					// We permit the very first line to be different to allow for this:
					// /** text
					//  * text
					//  */
					if (line != comment.Start)
						{
						if (CountEdgeSymbols(line, out leftSymbol, out rightSymbol, out leftSymbolCount, out rightSymbolCount, 
													  out symbolIsAloneOnLine) == false)
							{  return;  }
							
						testedForVerticalLines = true;
						}
					}
					
				else // testedForVerticalLines == true
					{
					char lineLeftSymbol, lineRightSymbol;
					int lineLeftSymbolCount, lineRightSymbolCount;
					bool lineSymbolIsAloneOnLine;
					
					CountEdgeSymbols(line, out lineLeftSymbol, out lineRightSymbol, out lineLeftSymbolCount, out lineRightSymbolCount,
											  out lineSymbolIsAloneOnLine);
											  
											  
					// Account for a lone symbol being the right symbol.
					
					if (lineSymbolIsAloneOnLine == true && symbolIsAloneOnLine == false && leftSymbolCount == 0 && rightSymbolCount > 0)
						{
						if (lineLeftSymbol != rightSymbol || lineLeftSymbolCount != rightSymbolCount)
							{  return;  }
						}
					else if (lineSymbolIsAloneOnLine == false && symbolIsAloneOnLine == true && lineLeftSymbolCount == 0 && lineRightSymbolCount > 0)
						{
						if (lineRightSymbol != leftSymbol || lineRightSymbolCount != leftSymbolCount)
							{  return;  }
							
						rightSymbol = leftSymbol;
						leftSymbol = '\0';
						rightSymbolCount = leftSymbolCount;
						leftSymbolCount = 0;
						}
						
					// Otherwise it's okay to do a straight compare.
					else
						{
						if (lineLeftSymbol != leftSymbol || lineLeftSymbolCount != leftSymbolCount)
							{
							leftSymbol = '\0';
							leftSymbolCount = 0;
							}
							
						if (lineRightSymbol != rightSymbol || lineRightSymbolCount != rightSymbolCount)
							{
							rightSymbol = '\0';
							rightSymbolCount = 0;
							}
							
						if (leftSymbolCount == 0 && rightSymbolCount == 0)
							{  return;  }
						}
						
					// Turn off the overall alone flag if this line didn't have it.
					if (lineSymbolIsAloneOnLine == false)
						{  symbolIsAloneOnLine = false;  }
					}
					
					
				line.Next();
				}
				
				
			// If we stopped because we hit a blank line, this comment is only acceptable for marking text boxes if all the lines
			// left are blank.
			
			while (line < comment.End && line.IsEmpty(LineBoundsMode.CommentContent))
				{  line.Next();  }
				
			if (line != comment.End)
				{  return;  }
				
				
			// If we made it this far without returning it means we have a valid text box which we have to mark as comment decoration.
			
			line = comment.Start;
			
			while (line < comment.End)
				{
				line.GetBounds(LineBoundsMode.CommentContent, out lineStart, out lineEnd);

				if (horizontalLines.Contains(line.LineNumber))
					{
					while (lineStart < lineEnd)
					    {
					    lineStart.CommentParsingType = CommentParsingType.CommentDecoration;
					    lineStart.Next();
					    }
					}
					
				else if (lineEnd > lineStart)
					{
					// We test the characters against the symbols to account for any exceptions we allowed to go through 
					// in previous code.
					
					for (int i = 0; i < leftSymbolCount; i++)
						{
						if (lineStart.Character == leftSymbol)
							{
							lineStart.CommentParsingType = CommentParsingType.CommentDecoration;
							lineStart.Next();
							}
						}
						
					lineEnd.Previous();
					for (int i = 0; i < rightSymbolCount; i++)
						{
						if (lineEnd.Character == rightSymbol)
							{
							lineEnd.CommentParsingType = CommentParsingType.CommentDecoration;
							lineEnd.Previous();
							}
						}
					}
					
				line.Next();
				}
			}
			
			
		/* Function: IsHorizontalLine
		 * Returns whether the passed <LineIterator> is at a horizontal line, not including any comment symbols or decoration.
		 */
		public bool IsHorizontalLine (LineIterator line)
			{
			TokenIterator start, end;
			line.GetBounds(LineBoundsMode.CommentContent, out start, out end);
			
			char symbol;
			int symbolCount;
			
			bool lineResult = CountSymbols(ref start, end, out symbol, out symbolCount);
			
			return (lineResult == true && symbolCount >= 4 && start == end);
			}
			
			
			
		// Group: Protected Functions
		// __________________________________________________________________________
			
			
		/* Function: CountSymbolLine
		 * 
		 * An internal function that detects whether the <TokenIterator> is on a symbol line, which is up to three
		 * stretches of symbol tokens in a row.  If it's on at least one symbol it returns true and sets the A, B, and C
		 * characters and counts.  If there are no B or C stretches they will be set to null and zero.  The start iterator
		 * will be left equal to end or after the symbol tokens.
		 * 
		 * If it wasn't on a symbol to begin with it will return false, A, B, and C will all be set to null and zero, and the
		 * start iterator will not be moved.
		 */
		protected bool CountSymbolLine (ref TokenIterator start, TokenIterator end,
														 out char symbolA, out char symbolB, out char symbolC,
														 out int symbolACount, out int symbolBCount, out int symbolCCount)
			{
			symbolA = '\0';
			symbolB = '\0';
			symbolC = '\0';
			symbolACount = 0;
			symbolBCount = 0;
			symbolCCount = 0;
			
			if (CountSymbols(ref start, end, out symbolA, out symbolACount) == false)
				{  return false;  }
				
			// Do these only if we've been successful so far, but we're returning true regardless.
			if (CountSymbols(ref start, end, out symbolB, out symbolBCount) == true)
				{  CountSymbols(ref start, end, out symbolC, out symbolCCount);  }
				
			return true;
			}
			
			
		/* Function: CountEdgeSymbols
		 * 
		 * An internal function that detects whether the <LineIterator> has symbols on its left and/or right sides.  The
		 * symbols must be no longer than three characters and be separated by whitespace from any other content on 
		 * the line.  If either edge has symbols it will return true along with what they are and how many.  If neither do it
		 * will return false.  The variables will be set to the null character and a zero count for any edge that doesn't have 
		 * symbols.
		 * 
		 * symbolIsAloneOnLine is set to true if there was only one symbol on the line and no other content.  The symbol
		 * and count will be returned as the left side, but it's possible it would be intended for the right.  For example,
		 * look at this comment box:
		 * 
		 * > ////////////
		 * > // text   //
		 * > //        //
		 * > // text   //
		 * > ////////////
		 * 
		 * Marking the comment symbols will leave the resulting content like this when using 
		 * <LineBoundsMode.CommentContent>:
		 * 
		 * > //////////
		 * > text   //
		 * > //
		 * > text   //
		 * > //////////
		 * 
		 * The slashes on the middle line will be returned as the left symbol but is meant to be the right.  Make sure you
		 * handle this situation correctly.
		 */
		protected bool CountEdgeSymbols (LineIterator line, out char leftSymbol, out char rightSymbol,
															out int leftSymbolCount, out int rightSymbolCount, out bool symbolIsAloneOnLine)
			{
			symbolIsAloneOnLine = false;
			
			TokenIterator lineStart, lineEnd;
			line.GetBounds(LineBoundsMode.CommentContent, out lineStart, out lineEnd);
			
			if (CountSymbols(ref lineStart, lineEnd, out leftSymbol, out leftSymbolCount))
				{
				if ( (lineStart.FundamentalType != FundamentalType.Whitespace && lineStart != lineEnd) || leftSymbolCount > 3)
					{
					leftSymbol = '\0';
					leftSymbolCount = 0;
					}
					
				while (lineStart.FundamentalType == FundamentalType.Whitespace && lineStart < lineEnd)
					{  lineStart.Next();  }
					
				if (lineStart == lineEnd)
					{  symbolIsAloneOnLine = true;  }
				}
				
			if (ReverseCountSymbols(lineStart, ref lineEnd, out rightSymbol, out rightSymbolCount))
				{
				lineEnd.Previous();
				
				if ( (lineEnd >= lineStart && lineEnd.FundamentalType != FundamentalType.Whitespace) || rightSymbolCount > 3)
					{
					rightSymbol = '\0';
					rightSymbolCount = 0;
					}
				}
				
			return (leftSymbolCount != 0 || rightSymbolCount != 0);
			}
			
			
		/* Function: CountSymbols
		 * 
		 * An internal function that detects whether the <TokenIterator> is on a stretch of symbols, and if so, returns
		 * true along with what the symbol is and how many there are.  It will leave the start iterator equal to end or at
		 * the first token after the stretch.
		 * 
		 * If the start iterator wasn't on a symbol it returns false, sets the symbol to null, the count to zero, and does
		 * not move the start iterator.
		 */
		protected bool CountSymbols (ref TokenIterator start, TokenIterator end, out char symbol, out int count)
			{
			if (start >= end || start.FundamentalType != FundamentalType.Symbol)
				{  
				symbol = '\0';
				count = 0;
				return false;  
				}
				
			symbol = start.Character;
			count = 1;
			start.Next();
			
			while (start < end && start.Character == symbol)
				{
				count++;
				start.Next();
				}
				
			return true;
			}
			
			
		/* Function: ReverseCountSymbols
		 * 
		 * An internal function that detects whether the <TokenIterator> is one past a stretch of symbols, and if so, returns
		 * true along with what the symbol is and how many there are.  It will leave the end iterator equal to start or at
		 * the first token of the stretch.
		 * 
		 * If the end iterator wasn't on a symbol it returns false, sets the symbol to null, the count to zero, and does
		 * not move the end iterator.
		 */
		protected bool ReverseCountSymbols (TokenIterator start, ref TokenIterator end, out char symbol, out int count)
			{
			end.Previous();
			
			if (end < start || end.FundamentalType != FundamentalType.Symbol)
				{
				symbol = '\0';
				count = 0;

				end.Next();

				return false;
				}
				
			symbol = end.Character;
			count = 1;
			
			while (end > start)
				{
				end.Previous();
				
				if (end.Character != symbol)	
					{
					end.Next();
					return true;
					}
					
				count++;
				}
				
			return true;
			}
		}
	}