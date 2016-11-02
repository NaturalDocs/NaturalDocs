/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.Perl
 * ____________________________________________________________________________
 * 
 * Additional language support for Perl.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Comments;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class Perl : Language
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: PODLineType
		 * 
		 * StartPOD - The first line of a POD comment which isn't in the Natural Docs or Javadoc format, such as "=pod".
		 * StartNaturalDocs - The first line of a Natural Docs POD comment, such as "=begin natural docs".
		 * StartJavadoc - The first line of a Javadoc POD comment, such as "=begin javadoc"
		 * End - The end of the current POD comment.  Note that there can be multiple consecutive sections and there will be
		 *			only one end line after all of them.  For example, "=begin naturaldocs ... =begin text ... =end".
		 */
		public enum PODLineType : byte
			{  StartPOD, StartNaturalDocs, StartJavadoc, End  }



		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Perl
		 */
		public Perl (Languages.Manager manager) : base (manager, "Perl")
			{
			Type = LanguageType.BasicSupport;

			LineCommentStrings = new string[] { "#" };
			JavadocLineCommentStringPairs = new string[] { "##", "#" };
			XMLLineCommentStrings = new string[] { "###" };

			MemberOperator = "::";
			EnumValue = EnumValues.Global;
			CaseSensitive = true;
			}


		// Function: GetPossibleDocumentationComments
		// 
		// Goes through the file looking for comments that could possibly contain documentation and returns them as a list.  These 
		// comments are not guaranteed to have documentation in them, just to be acceptable candidates for them.  If there are no 
		// comments it will return an empty list.
		//
		// All the comments in the returned list will have their comment symbols marked as <CommentParsingType.CommentSymbol>
		// in the tokenizer.  This allows further operations to be done on them in a language independent manner.  If you want to also
		// filter out text boxes and lines, use <Comments.LineFinder>.
		//
		override public List<PossibleDocumentationComment> GetPossibleDocumentationComments (Tokenizer source)
			{
			List<PossibleDocumentationComment> comments = new List<PossibleDocumentationComment>();
			LineIterator lineIterator = source.FirstLine;

			PODLineType podLineType;

			while (lineIterator.IsInBounds)
				{
				TokenIterator tokenIterator = lineIterator.FirstToken(LineBoundsMode.ExcludeWhitespace);


				// Hash comments

				if (tokenIterator.Character == '#')
					{
					PossibleDocumentationComment comment = new PossibleDocumentationComment();
					comment.Start = lineIterator;


					// First line

					if (tokenIterator.MatchesAcrossTokens("###"))
						{
						comment.Javadoc = false;
						comment.XML = true;
						}
					else if (tokenIterator.MatchesAcrossTokens("##"))
						{
						comment.Javadoc = true;
						comment.XML = false;
						}
					else // just "#"
						{
						comment.Javadoc = false;
						comment.XML = false;
						}

					lineIterator.Next();


					// Subsequent lines

					while (lineIterator.IsInBounds)
						{
						tokenIterator = lineIterator.FirstToken(LineBoundsMode.ExcludeWhitespace);

						if (tokenIterator.Character != '#')
							{  break;  }

						if (tokenIterator.MatchesAcrossTokens("###"))
							{
							comment.Javadoc = false;
							// XML is still possible
							}
						else if (tokenIterator.MatchesAcrossTokens("##"))
							{
							comment.Javadoc = false;
							comment.XML = false;
							}
						else // just "#"
							{
							// Javadoc is still possible
							comment.XML = false;
							}

						lineIterator.Next();
						}

					comment.End = lineIterator;
					comments.Add(comment);

					
					// Go back and mark the tokens

					int firstLineCount, subsequentLineCount;

					if (comment.XML)
						{
						firstLineCount = 3;
						subsequentLineCount = 3;
						}
					else if (comment.Javadoc)
						{
						firstLineCount = 2;
						subsequentLineCount = 1;
						}
					else // plain
						{
						firstLineCount = 1;
						subsequentLineCount = 1;
						}

					LineIterator temp = comment.Start;
					tokenIterator = temp.FirstToken(LineBoundsMode.ExcludeWhitespace);

					tokenIterator.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, firstLineCount);
					temp.Next();

					while (temp < comment.End)
						{
						tokenIterator = temp.FirstToken(LineBoundsMode.ExcludeWhitespace);
						tokenIterator.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, subsequentLineCount);
						temp.Next();
						}

					
					// XML can actually appear in Javadoc comments

					if (comment.Javadoc)
						{  comment.XML = true;  }
					}


				// POD comments

				else if (TryToSkipPODLine(ref tokenIterator, out podLineType))
					{
					TokenIterator podLineStart, podLineEnd;
					lineIterator.GetBounds(LineBoundsMode.CommentContent, out podLineStart, out podLineEnd);

					lineIterator.Tokenizer.SetCommentParsingTypeBetween(podLineStart, podLineEnd, CommentParsingType.CommentSymbol);

					if (podLineType == PODLineType.StartNaturalDocs ||
						podLineType == PODLineType.StartJavadoc)
						{
						PossibleDocumentationComment comment = new PossibleDocumentationComment();
						comment.Start = lineIterator;

						if (podLineType == PODLineType.StartJavadoc)
							{  comment.Javadoc = true;  }

						for (;;)
							{
							lineIterator.Next();

							if (lineIterator.IsInBounds == false)
								{  break;  }

							tokenIterator = lineIterator.FirstToken(LineBoundsMode.CommentContent);

							if (TryToSkipPODLine(ref tokenIterator, out podLineType) == true)
								{
								if (podLineType == PODLineType.End)
									{
									lineIterator.GetBounds(LineBoundsMode.CommentContent, out podLineStart, out podLineEnd);
									lineIterator.Tokenizer.SetCommentParsingTypeBetween(podLineStart, podLineEnd, CommentParsingType.CommentSymbol);

									lineIterator.Next();
									}

								break;  
								}
							}

						comment.End = lineIterator;
						comments.Add(comment);
						}
					else
						{  lineIterator.Next();  }
					}

				else
					{  lineIterator.Next();  }
				}

			return comments;
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToSkipPODLine
		 * If the iterator is on a POD line such as "=pod", moves the iterator past it and returns true and the type.  If the iterator
		 * isn't on a POD line it will return false and leave the iterator alone.
		 */
		protected bool TryToSkipPODLine (ref TokenIterator iterator, out PODLineType type)
			{
			type = 0;

			if (iterator.Character != '=')
				{  return false;  }

			TokenIterator lookbehind = iterator;

			for (;;)
				{
				lookbehind.Previous();

				if (lookbehind.IsInBounds == false ||
					lookbehind.FundamentalType == FundamentalType.LineBreak)
					{  break;  }
				else if (lookbehind.FundamentalType != FundamentalType.Whitespace)
					{  return false;  }
				}

			TokenIterator endOfLine = iterator;
			endOfLine.Next();

			if (endOfLine.FundamentalType != FundamentalType.Text)
				{  return false;  }

			do
				{  endOfLine.Next();  }
			while (endOfLine.IsInBounds && 
					 endOfLine.FundamentalType != FundamentalType.LineBreak);

			TokenIterator endOfContent = endOfLine;
			endOfContent.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

			// ND and Javadoc lines may also match PODBeginLineRegex so test them first.
			if (iterator.Tokenizer.MatchTextBetween(PODBeginNDLineRegex, iterator, endOfContent).Success)
				{  type = PODLineType.StartNaturalDocs;  }
			else if (iterator.Tokenizer.MatchTextBetween(PODBeginJavadocLineRegex, iterator, endOfContent).Success)
				{  type = PODLineType.StartJavadoc;  }
			else if (iterator.Tokenizer.MatchTextBetween(PODBeginLineRegex, iterator, endOfContent).Success)
				{  type = PODLineType.StartPOD;  }
			else if (iterator.Tokenizer.MatchTextBetween(PODEndLineRegex, iterator, endOfContent).Success)
				{  type = PODLineType.End;  }
			else
				{  return false;  }

			iterator = endOfLine;

			if (iterator.FundamentalType == FundamentalType.LineBreak)
				{  iterator.Next();  }

			return true;
			}



		// Group: Static Variables
		// __________________________________________________________________________


		static protected Regex.Languages.Perl.PODBeginLine PODBeginLineRegex = new Regex.Languages.Perl.PODBeginLine();
		static protected Regex.Languages.Perl.PODBeginNDLine PODBeginNDLineRegex = new Regex.Languages.Perl.PODBeginNDLine();
		static protected Regex.Languages.Perl.PODBeginJavadocLine PODBeginJavadocLineRegex = new Regex.Languages.Perl.PODBeginJavadocLine();
		static protected Regex.Languages.Perl.PODEndLine PODEndLineRegex = new Regex.Languages.Perl.PODEndLine();

		}
	}