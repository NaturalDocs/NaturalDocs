/* 
 * Class: GregValure.NaturalDocs.Engine.Languages.Language
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Tokenization;
using GregValure.NaturalDocs.Engine.Comments;


namespace GregValure.NaturalDocs.Engine.Languages
	{
	public partial class Language
		{
		
		// Group: Types
		// __________________________________________________________________________
		
		
		/* enum: ParseResult
		 * 
		 * The result of a <Parse()> operation.
		 * 
		 * Success - The parsing completed successfully.
		 * Cancelled - The parsing was cancelled before completion.
		 * SyntaxErrors - The parsing has completed but there were syntax errors.
		 */
		public enum ParseResult : byte
			{  Success, Cancelled, SyntaxErrors  }



		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Parse
		 * Parses the source code as a string and returns it as a list of <Topics>.  The list will be null if there are no topics or parsing was
		 * interrupted.  Set cancelDelegate for the ability to interrupt parsing, or use <Delegates.NeverCancel>.
		 * 
		 * If you already have the source code in tokenized form it would be more efficient to pass it as a <Tokenizer>.
		 */
		public ParseResult Parse (string sourceCodeString, int fileID, CancelDelegate cancelDelegate, out List<Topic> topics)
			{
			return Parse( new Tokenizer(sourceCodeString), fileID, cancelDelegate, out topics );
			}
			
			
		/* Function: Parse
		 * Parses the tokenized source code and returns it as a list of <Topics>.  The list will be null if there are no topics or parsing was
		 * interrupted.  Set cancelDelegate for the ability to interrupt parsing, or use <Delegates.NeverCancel>.
		 */
		virtual public ParseResult Parse (Tokenizer tokenizedSourceCode, int fileID, CancelDelegate cancelDelegate, 
																	  out List<Topic> topics)
			{ 
			topics = null;
			ParseState parser = new ParseState();
			
			parser.TokenizedSourceCode = tokenizedSourceCode;
			parser.CancelDelegate = cancelDelegate;
			
			GetPossibleDocumentationComments(parser);
			
			if (parser.Cancelled)
				{  return ParseResult.Cancelled;  }
				
			ParsePossibleDocumentationComments(parser);
			
			if (parser.Cancelled)
				{  return ParseResult.Cancelled;  }
				
			GetCodeTopics(parser);
			
			if (parser.Cancelled)
				{  return ParseResult.Cancelled; }
				
			MergeTopics(parser);
			
			if (parser.Cancelled)
				{  return ParseResult.Cancelled;  }

			foreach (Topic topic in parser.MergedTopics)
				{
				topic.FileID = fileID;
				topic.LanguageID = this.ID;
				}
								
			GenerateRemainingSymbols(parser);
				
			// Need to do one last check anyway, because the previous function could have quit early because of a cancellation.
			if (parser.Cancelled)
				{  return ParseResult.Cancelled;  }
		
		
			if (parser.MergedTopics.Count > 0)
				{  topics = parser.MergedTopics;  }
				
			return ParseResult.Success;
			}


		/* Function: GetComments
		 * 
		 * Goes through the file looking for comments that could possibly contain documentation and returns them as a list.  These 
		 * comments are not guaranteed to have documentation in them, just to be acceptable candidates for them.  If there are no 
		 * comments it will return an empty list.
		 * 
		 * This function is NOT required for the normal parsing of files.  Just calling <Parse()> is enough.  This function is only 
		 * available to provide alternate uses of the parser, such as in <Output.Shrinker>.
		 * 
		 * All the comments in the returned list will have their comment symbols marked as <CommentParsingType.CommentSymbol>
		 * in the tokenizer.  This allows further operations to be done on them in a language independent manner.  Text boxes and lines
		 * will also be marked as <CommentParsingType.CommentDecoration>.
		 * 
		 * If you already have the source code in tokenized form it would be more efficient to pass it as a <Tokenizer>.
		 */
		public List<PossibleDocumentationComment> GetComments (string sourceCode)
			{
			return GetComments( new Tokenizer(sourceCode) );
			}


		/* Function: GetComments
		 * 
		 * Goes through the file looking for comments that could possibly contain documentation and returns them as a list.  These 
		 * comments are not guaranteed to have documentation in them, just to be acceptable candidates for them.  If there are no 
		 * comments it will return an empty list.
		 * 
		 * This function is NOT required for the normal parsing of files.  Just calling <Parse()> is enough.  This function is only 
		 * available to provide alternate uses of the parser, such as in <Output.Shrinker>.
		 * 
		 * All the comments in the returned list will have their comment symbols marked as <CommentParsingType.CommentSymbol>
		 * in the tokenizer.  This allows further operations to be done on them in a language independent manner.  Text boxes and lines
		 * will also be marked as <CommentParsingType.CommentDecoration>.
		 */
		public List<PossibleDocumentationComment> GetComments (Tokenizer tokenizedSourceCode)
			{
			ParseState parser = new ParseState();
			
			parser.TokenizedSourceCode = tokenizedSourceCode;
			parser.CancelDelegate = Delegates.NeverCancel;
			
			GetPossibleDocumentationComments(parser);

			foreach (PossibleDocumentationComment comment in parser.PossibleDocumentationComments)
				{
				Instance.Comments.LineFinder.MarkTextBoxes(comment);
				}

			return parser.PossibleDocumentationComments;
			}


			
		// Group: Overridable Parsing Stages
		// Override these stages in subclasses as necessary.
		// __________________________________________________________________________
		
			
		// Function: GetPossibleDocumentationComments
		// 
		// Goes through the file looking for comments that could possibly contain documentation and retrieves them as a list in
		// <ParseState.PossibleDocumentationComments>.  These comments are not guaranteed to have documentation in them, 
		// just to be acceptable candidates for them.  If there are no comments, PossibleDocumentationComments will be set to
		// an empty list.
		//
		// All the comments in the returned list will have their comment symbols marked as <CommentParsingType.CommentSymbol>
		// in the tokenizer.  This allows further operations to be done on them in a language independent manner.
		//
		// Default Implementation:
		//
		// The default implementation uses the comment symbols found in <Language>.  You can override this function if you need
		// to do something more sophisticated, such as interpret the POD directives in Perl.
		//
		// Comments must be alone on a line to be a candidate for documentation, meaning that the comment symbol must be the 
		// first non-whitespace character on a line, and in the case of block comments, nothing but whitespace may trail the closing
		// symbol.  The latter rule is important because a comment may start correctly but not end so, as in this prototype with Splint 
		// annotation:
		// 
		// > int get_array(integer_t id,
		// >               /*@out@*/ array_t array);
		//
		// Speaking of which, block comments surrounded by @ symbols are not included because they're Splint comments.  Not
		// including them in the possible documentation comments list means the Splint comment below won't end prototype detection.
		//
		// > void initialize ()
		// >    /*@globals undef globnum,
		// >               undef globname @*/
		// >    { ... }
		//
		// It also goes through the code line by line in a simple manner, not accounting for things like strings, so if a language contains
		// a multiline string whose content looks like a language comment it will be interpreted as one.  This isn't ideal but is accepted
		// as a conscious tradeoff because there are actually many different string formats (literal quotes denoted with \", literal quotes 
		// denoted with "", Perl's q{} forms and here doc) so you can't account for them all in a generalized way.  Also, putting this in 
		// an independent stage even when using full language support means comments don't disappear the way prototypes do if the 
		// parser gets tripped up on something like an unmatched brace.
		//
		virtual protected void GetPossibleDocumentationComments (ParseState parser)
			{
			parser.PossibleDocumentationComments = new List<PossibleDocumentationComment>();

			if (Type == LanguageType.TextFile)
				{
				PossibleDocumentationComment comment = new PossibleDocumentationComment();
				comment.Type = Comments.Type.Plain;
				comment.Start = parser.TokenizedSourceCode.FirstLine;
				comment.End = parser.TokenizedSourceCode.LastLine;

				parser.PossibleDocumentationComments.Add(comment);
				}
			else
				{
				LineIterator lineIterator = parser.TokenizedSourceCode.FirstLine;
				bool foundComment = false;
			
				while (lineIterator.IsInBounds)
					{
					if (parser.Cancelled)
						{  return;  }
				
					TokenIterator startOfLine = lineIterator.FirstToken(LineBoundsMode.ExcludeWhitespace);
					foundComment = false;
				
				
					// Javadoc block comments.  We test for these first because they are usually extended versions of the regular block 
					// comment symbols, such as /** and /*.
				
					if (JavadocBlockCommentStringPairs != null)
						{
						for (int i = 0; foundComment == false && i < JavadocBlockCommentStringPairs.Length; i += 2)
							{
							if (startOfLine.MatchesAcrossTokens(JavadocBlockCommentStringPairs[i], false))
								{
								// Check that it's not another symbol afterwards, so we don't mistake a /****** line for Javadoc.
							
								TokenIterator afterOpeningBlock = startOfLine;
								afterOpeningBlock.NextByCharacters(JavadocBlockCommentStringPairs[i].Length);
							
								if (afterOpeningBlock.FundamentalType != FundamentalType.Symbol)
									{
									startOfLine.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, 
																							JavadocBlockCommentStringPairs[i].Length);

									PossibleDocumentationComment comment = new PossibleDocumentationComment();
									comment.Type = Comments.Type.Javadoc;
									comment.Start = lineIterator;

									if (GetPossibleDocumentationComments_GetUntil (parser, JavadocBlockCommentStringPairs[i+1],
																												  ref lineIterator, comment) )
										{  parser.PossibleDocumentationComments.Add(comment);  }
									
									foundComment = true;
									}
								}
							}
						}
					
					
					// Plain block comment strings.
					
					if (foundComment == false && BlockCommentStringPairs != null)
						{
						for (int i = 0; foundComment == false && i < BlockCommentStringPairs.Length; i += 2)
							{
							if (startOfLine.MatchesAcrossTokens(BlockCommentStringPairs[i], false))
								{
								startOfLine.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, 
																						BlockCommentStringPairs[i].Length);

								PossibleDocumentationComment comment = new PossibleDocumentationComment();
								comment.Type = Comments.Type.Plain;
								comment.Start = lineIterator;
							
								if (GetPossibleDocumentationComments_GetUntil (parser, BlockCommentStringPairs[i+1],
																											  ref lineIterator, comment) )
									{  
									bool isSplint = false;

									if (comment.Start.FirstToken(LineBoundsMode.CommentContent).Character == '@')
										 {
										 LineIterator lastLine = comment.End;
										 lastLine.Previous();

										 TokenIterator lastToken, ignore;
										 lastLine.GetBounds(LineBoundsMode.CommentContent, out ignore, out lastToken);
										 lastToken.Previous();

										 if (lastToken.Character == '@')
											{  isSplint = true;  }
										 }

									if (!isSplint)
										{  parser.PossibleDocumentationComments.Add(comment);  }
									}
							
								foundComment = true;
								}
							}
						}
					
					
					// Javadoc line comments.
				
					if (foundComment == false && JavadocLineCommentStringPairs != null)
						{
						for (int i = 0; foundComment == false && i < JavadocLineCommentStringPairs.Length; i += 2)
							{
							if (startOfLine.MatchesAcrossTokens(JavadocLineCommentStringPairs[i], false))
								{
								// Check that it's not another symbol afterwards, so we don't mistake a ##### line for Javadoc.
							
								TokenIterator afterOpeningBlock = startOfLine;
								afterOpeningBlock.NextByCharacters(JavadocLineCommentStringPairs[i].Length);
							
								if (afterOpeningBlock.FundamentalType != FundamentalType.Symbol)
									{
									startOfLine.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, 
																							JavadocLineCommentStringPairs[i].Length);

									PossibleDocumentationComment comment = new PossibleDocumentationComment();
									comment.Type = Comments.Type.Javadoc;
									comment.Start = lineIterator;
								
									lineIterator.Next();

									GetPossibleDocumentationComments_GetWhile (parser, JavadocLineCommentStringPairs[i+1],
																												ref lineIterator, comment);
									parser.PossibleDocumentationComments.Add(comment);
									
									foundComment = true;
									}
								}
							}
						}
						
						
					// XML line comments.
				
					if (foundComment == false && XMLLineCommentStrings != null)
						{
						for (int i = 0; foundComment == false && i < XMLLineCommentStrings.Length; i++)
							{
							if (startOfLine.MatchesAcrossTokens(XMLLineCommentStrings[i], false))
								{
								// Check that it's not another symbol afterwards, so we don't mistake a ///// line for XML.
							
								TokenIterator afterOpeningBlock = startOfLine;
								afterOpeningBlock.NextByCharacters(XMLLineCommentStrings[i].Length);
							
								if (afterOpeningBlock.FundamentalType != FundamentalType.Symbol)
									{
									startOfLine.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, 
																							XMLLineCommentStrings[i].Length);

									PossibleDocumentationComment comment = new PossibleDocumentationComment();
									comment.Type = Comments.Type.XML;
									comment.Start = lineIterator;
								
									lineIterator.Next();

									GetPossibleDocumentationComments_GetWhile (parser, XMLLineCommentStrings[i],
																												ref lineIterator, comment);
									parser.PossibleDocumentationComments.Add(comment);
									
									foundComment = true;
									}
								}
							}
						}
						
						
					// Plain line comments.
				
					if (foundComment == false && LineCommentStrings != null)
						{
						for (int i = 0; foundComment == false && i < LineCommentStrings.Length; i++)
							{
							if (startOfLine.MatchesAcrossTokens(LineCommentStrings[i], false))
								{
								startOfLine.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, 
																						LineCommentStrings[i].Length);

								PossibleDocumentationComment comment = new PossibleDocumentationComment();
								comment.Type = Comments.Type.Plain;
								comment.Start = lineIterator;
							
								lineIterator.Next();

								GetPossibleDocumentationComments_GetWhile (parser, LineCommentStrings[i],
																											ref lineIterator, comment);
								parser.PossibleDocumentationComments.Add(comment);
								
								foundComment = true;
								}
							}
						}
					
				
					// Nada.
				
					if (foundComment == false)
						{  lineIterator.Next();  }
					
					}
				}
			}
			
		/* Function: GetPossibleDocumentationComments_GetUntil
		 * 
		 * A helper function used only by <GetPossibleDocumentationComments()> that advances the iterator until it
		 * reaches a line containing the passed closing comment symbol.  If that was the last thing on the line, it sets the ending
		 * iterator field on the comment object and returns true.  If not, or if it reaches the end of the file, it returns false.
		 * The closing comment symbol will be marked as <CommentParsingType.CommentSymbol>.
		 * 
		 * The passed iterator should be on the first line of the comment so that it can capture a single line block comment.  The 
		 * iterator will be left on the line following the one with the ending comment symbol.
		 */
		 protected bool GetPossibleDocumentationComments_GetUntil (ParseState parser, string closingCommentSymbol, 
																										ref LineIterator lineIterator,
																										PossibleDocumentationComment comment)
			{
			do
				{
				if (parser.Cancelled)
					{  return false;  }
					
				TokenIterator symbolPosition;
				
				if (lineIterator.FindAcrossTokens(closingCommentSymbol, false, LineBoundsMode.Everything, out symbolPosition) == true)
					{
					int rawTextStart, rawTextEnd;
					lineIterator.GetRawTextBounds(LineBoundsMode.ExcludeWhitespace, out rawTextStart, out rawTextEnd);
					
					bool lastThingOnLine = ( symbolPosition.RawTextIndex + closingCommentSymbol.Length == rawTextEnd );
						
					symbolPosition.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, 
																															 closingCommentSymbol.Length);
					lineIterator.Next();
					
					if (lastThingOnLine == true)
						{
						comment.End = lineIterator;
						return true;
						}
					else
						{  return false;  }
					}
					
				lineIterator.Next();
				}
			while (lineIterator.IsInBounds);
			
			return false;
			}
			
			
		/* Function: GetPossibleDocumentationComments_GetWhile
		 * 
		 * A helper function used only by <GetPossibleDocumentationComments()> that advances the iterator until it
		 * reaches a line that doesn't start with the passed comment symbol.  It then sets the ending iterator field in
		 * the comment.  All the comment symbols will be marked as <CommentParsingType.CommentSymbol>.
		 * 
		 * The passed iterator should start on the second line of the comment since you should already know the first one is
		 * a part of it.  The iterator will be left on the line following the last one which started with the comment symbol.
		 */
		 protected void GetPossibleDocumentationComments_GetWhile (ParseState parser, string commentSymbol, 
																										  ref LineIterator lineIterator,
																										  PossibleDocumentationComment comment)
			{
			do
				{
				if (parser.Cancelled)
					{  return;  }
					
				TokenIterator symbolPosition = lineIterator.FirstToken(LineBoundsMode.ExcludeWhitespace);
				
				if (symbolPosition.MatchesAcrossTokens(commentSymbol, false))
					{
					symbolPosition.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, commentSymbol.Length);
					lineIterator.Next();
					}
				else
					{  break;  }
				}
			while (lineIterator.IsInBounds);
			
			comment.End = lineIterator;
			}
			
			
		/* Function: ParsePossibleDocumentationComments
		 * 
		 * Converts the raw comments in <ParseState.PossibleDocumentationComments> to <ParseState.CommentTopics>.
		 * If there are none, CommentTopics will be set to an empty list.
		 * 
		 * The default implementation sends each <PossibleDocumentationComment> to <Comments.Manager.Parse()>.  There
		 * should be no need to change it.
		 */
		protected virtual void ParsePossibleDocumentationComments (ParseState parser)
			{
			parser.CommentTopics = new List<Topic>();
				
			foreach (PossibleDocumentationComment comment in parser.PossibleDocumentationComments)
				{
				Engine.Instance.Comments.Parse(comment, parser.CommentTopics);
				
				if (parser.Cancelled)
					{  return;  }
				}
			}


		/* Function: GetCodeTopics
		 * 
		 * Goes through the file looking for code elements that should be included in the output and creates a list in
		 * <ParseState.CodeTopics>.  If there are none, CodeTopics will be set to an empty list.
		 *
		 * Default Implementation:
		 *
		 * The default implementation uses <ParseState.CommentTopics> and the language's prototype enders to gather 
		 * prototypes for languages with basic support.  It basically takes the code between the end of the comment topic and the 
		 * next one (or the next entry in <ParseState.PossibleDocumentationComments>) and if it finds the topic title before
		 * it finds an ender, the prototype will be the code between the topic and the ender.  You can override this function to do
		 * real language processing for full support.
		 *
		 * This function does the basic looping of the search but throws the actual prototype detection to <GetPrototype()>.
		 * This makes it easier for tweaks to be implemented for certain languages that have unique prototype formats but don't 
		 * have full language support.
		 */
		protected virtual void GetCodeTopics (ParseState parser)
			{
			parser.CodeTopics = new List<Topic>();

			int topicIndex = 0;

			for (int commentIndex = 0; commentIndex < parser.PossibleDocumentationComments.Count; commentIndex++)
				{
				if (parser.Cancelled)
					{  return;  }

				PossibleDocumentationComment comment = parser.PossibleDocumentationComments[commentIndex];

				// Advance the topic index to the last one before the end of this comment.  If there are multiple topics in a 
				// comment only the last one gets a prototype search.

				while (topicIndex + 1 < parser.CommentTopics.Count && 
							parser.CommentTopics[topicIndex + 1].CommentLineNumber < comment.End.LineNumber)
					{  topicIndex++;  }

				if (topicIndex >= parser.CommentTopics.Count ||
					 parser.CommentTopics[topicIndex].CommentLineNumber < comment.Start.LineNumber ||
					 parser.CommentTopics[topicIndex].CommentLineNumber > comment.End.LineNumber)
					{  
					// We're out of topics or the one we're on isn't in this comment.
					continue;  
					}

				// Build the bounds for the prototype search and perform it.

				Tokenization.LineIterator startCode = comment.End;
				Tokenization.LineIterator endCode;

				if (commentIndex + 1 < parser.PossibleDocumentationComments.Count)
					{  endCode = parser.PossibleDocumentationComments[commentIndex + 1].Start;  }
				else
					{  endCode = parser.TokenizedSourceCode.LastLine;  }

				GetPrototype(parser.CommentTopics[topicIndex], startCode, endCode);
				}
			}


		/* Function: GetPrototype
		 * Attempts to find a prototype for the passed <Topic> between the iterators.  If one is found, it will set <Topic.Prototoype>.
		 */
		protected virtual void GetPrototype (Topic topic, LineIterator startCode, LineIterator endCode)
			{
			PrototypeEnders prototypeEnders = GetPrototypeEnders(topic.TopicTypeID);

			if (prototypeEnders == null)
				{  return;  }

			StringBuilder prototype = new StringBuilder();
			Tokenizer tokenizer = startCode.Tokenizer;

			TokenIterator start = startCode.FirstToken(LineBoundsMode.ExcludeWhitespace);
			TokenIterator iterator = start;
			TokenIterator limit = endCode.FirstToken(LineBoundsMode.ExcludeWhitespace);

			SafeStack<char> brackets = new SafeStack<char>();
			bool lastWasWhitespace = true;
			bool lineHasExtender = false;
			int blockCommentIndex = -1;

			bool goodPrototype = false;

			while (iterator < limit)
				{

				// Inside a String

				if (brackets.Peek() == '"')
					{
					// No whitespace condensation while in a string.  We also don't have to worry about maintaining
					// lastWasWhitespace until we're leaving it.

					if (iterator.Character == '\\')
						{  
						prototype.Append('\\');
						iterator.Next();  
						iterator.AppendTokenTo(prototype);
						iterator.Next();
						}
					else 
						{
						if (iterator.Character == '"')
							{  
							brackets.Pop();  
							lastWasWhitespace = false;
							}
						
						iterator.AppendTokenTo(prototype);
						iterator.Next();
						}
					}


				// Line Break

				else if (iterator.FundamentalType == FundamentalType.LineBreak)
					{
					if (prototypeEnders.IncludeLineBreaks && !lineHasExtender)
						{  
						goodPrototype = true;
						break;  
						}

					else
						{
						if (lastWasWhitespace == false)
							{
							prototype.Append(' ');
							lastWasWhitespace = true;
							}

						iterator.Next();
						lineHasExtender = false;
						}
					}


				// Line Extender

				else if (LineExtender != null && iterator.MatchesAcrossTokens(LineExtender))
					{
					// If the line extender is an underscore we don't want to include it if it's adjacent to any text because
					// it's probably part of an identifier.

					bool partOfIdentifier = false;

					if (LineExtender == "_")
						{
						TokenIterator temp = iterator;

						temp.Previous();
						if (temp.FundamentalType == FundamentalType.Text || temp.Character == '_')
							{  partOfIdentifier = true;  }

						temp.Next(2);
						if (temp.FundamentalType == FundamentalType.Text || temp.Character == '_')
							{  partOfIdentifier = true;  }
						}

					if (partOfIdentifier)
						{
						iterator.AppendTokenTo(prototype);
						iterator.Next();
						lastWasWhitespace = false;
						}
					else
						{
						lineHasExtender = true;

						// We don't want it in the output so treat it like whitespace
						if (lastWasWhitespace == false)
							{
							prototype.Append(' ');
							lastWasWhitespace = true;
							}

						iterator.Next();
						}
					}


				// Ender Symbol, not in a bracket

				// We test this before looking for opening brackets so the opening symbols can be used as enders.
				else if (prototypeEnders.Symbols != null && brackets.Count == 0 && 
							 iterator.MatchesAnyAcrossTokens(prototypeEnders.Symbols) != -1)
					{
					goodPrototype = true;
					break;
					}

				
				// Line Comment

				// We test this before looking for opening brackets in case the opening symbols are used for comments.
				else if (LineCommentStrings != null && iterator.MatchesAnyAcrossTokens(LineCommentStrings) != -1)
					{
					// Treat it as whitespace and skip to the next line break.  We're only dealing with Splint for block comments.
					do
						{  iterator.Next();  }
					while (iterator.FundamentalType != FundamentalType.LineBreak && iterator < limit);

					if (lastWasWhitespace == false)
						{
						prototype.Append(' ');
						lastWasWhitespace = true;
						}
					}


				// Block Comment

				// We test this before looking for opening brackets in case the opening symbols are used for comments.
				else if (BlockCommentStringPairs != null && 
							 (blockCommentIndex = iterator.MatchesAnyPairAcrossTokens(BlockCommentStringPairs)) != -1)
					{
					string openingSymbol = BlockCommentStringPairs[blockCommentIndex];
					string closingSymbol = BlockCommentStringPairs[blockCommentIndex+1];

					iterator.NextByCharacters(openingSymbol.Length);
					TokenIterator commentContentStart = iterator;

					while (iterator.MatchesAcrossTokens(closingSymbol) == false && iterator < limit)
						{  iterator.Next();  }

					TokenIterator commentContentEnd = iterator;

					if (iterator < limit)
						{  iterator.NextByCharacters(closingSymbol.Length);  }

					// Allow certain comments to appear in the output, such as those for Splint.  See splint.org.
					if (tokenizer.MatchTextBetween(acceptablePrototypeCommentRegex, 
																			 commentContentStart, commentContentEnd).Success)
						{
						prototype.Append(openingSymbol);

						string commentContent = tokenizer.TextBetween(commentContentStart, commentContentEnd);
						commentContent = commentContent.Replace('\r', ' ');
						commentContent = commentContent.Replace('\n', ' ');
						commentContent = commentContent.CondenseWhitespace();

						prototype.Append(commentContent);
						prototype.Append(closingSymbol);
						lastWasWhitespace = false;
						}
					else
						{
						if (lastWasWhitespace == false)
							{
							prototype.Append(' ');
							lastWasWhitespace = true;
							}
						}
					}


				// Opening Bracket or Quote

				// We don't test for < because there might be an unbalanced pair as part of an operator overload.
				else if (iterator.Character == '(' || iterator.Character == '[' || iterator.Character == '{' || iterator.Character == '"')
					{
					brackets.Push(iterator.Character);
					iterator.AppendTokenTo(prototype);
					iterator.Next();
					lastWasWhitespace = false;
					}


				// Closing Bracket, matching the last opening one

				// We already handled quotes at the beginning of the loop.
				else if ( (iterator.Character == ')' && brackets.Peek() == '(') ||
							  (iterator.Character == ']' && brackets.Peek() == '[') ||
							  (iterator.Character == '}' && brackets.Peek() == '{') )
					{
					brackets.Pop();
					iterator.AppendTokenTo(prototype);
					iterator.Next();
					lastWasWhitespace = false;
					}


				// Whitespace

				else if (iterator.FundamentalType == FundamentalType.Whitespace)
					{
					if (lastWasWhitespace == false)
						{
						prototype.Append(' ');
						lastWasWhitespace = true;
						}

					iterator.Next();
					}


				// Everything Else

				else
					{
					iterator.AppendTokenTo(prototype);
					lastWasWhitespace = false;
					iterator.Next();  
					}
				}

			if (goodPrototype)
				{
				// Strip trailing space
				if (lastWasWhitespace && prototype.Length > 0)
					{  prototype.Remove(prototype.Length - 1, 1);  }

				string prototypeString = prototype.ToString();

				if (prototypeString.IndexOf(topic.UndecoratedTitle, StringComparison.CurrentCultureIgnoreCase) != -1)
					{  topic.Prototype = prototypeString;  }
				}
			}
			
		
		/* Function: MergeTopics
		 * 
		 * Combines the topics in <ParseState.CodeTopics> and <ParseState.CommentTopics> into a single list and places the result
		 * in <ParseState.MergedTopics>.   CodeTopics and CommentTopics will be set to null afterwards.  Headerless topics that don't
		 * match a code topic will be removed.
		 * 
		 * Note that this is a destructive act.  The <Topics> that were in CodeTopics and CommentTopics may have been modified 
		 * and placed in MergedTopics rather than new ones being created.  As such, you should not rely on or have reference to any 
		 * <Topic> on those two lists after calling this function.
		 */
		protected virtual void MergeTopics (ParseState parser)
			{
			// The code topics should already be in perfect form so if there's no comment topics to deal with, move the list as is.
			// This also covers if both topic lists are empty as it will just make the empty code list become the empty merged list.	
			if (parser.CommentTopics.Count == 0)
				{  
				parser.MergedTopics = parser.CodeTopics;  
				parser.CodeTopics = null;
				parser.CommentTopics = null;
				}
				
			// If there's comment topics but no code topics, all we need to do is punt any headerless topics.
			else if (parser.CodeTopics.Count == 0)
				{
				parser.MergedTopics = parser.CommentTopics;
				parser.CommentTopics = null;
				
				int i = 0;
				while (i < parser.MergedTopics.Count)
					{
					if (parser.MergedTopics[i].Title == null)
						{  parser.MergedTopics.RemoveAt(i);  }
					else
						{  i++;  }
					}
				}
			
			else
				{
				// XXX: topic merging
				}
			}
			
			
		/* Function: GenerateRemainingSymbols
		 * 
		 * Generates <Symbols> for any <Topics> which do not already have one.  As code topics always have symbols and any comment
		 * that was merged with one would inherit it, this only applies to the Natural Docs topics which were not merged with a code element.
		 * 
		 * The default implementation will follow a combination of the code topics' scope and the Natural Docs scoping rules.  Basically, the
		 * Natural Docs scoping rules only apply between code topics, so entire classes can be defined between real elements but members
		 * would otherwise pick up their scope from the code.  Also, if this comes from a basic language support parser, there will be no
		 * code scope so Natural Docs' rules will apply to the whole thing.
		 */
		protected virtual void GenerateRemainingSymbols (ParseState parser)
			{
			// XXX - This currently doesn't follow any scoping rules at all, it just gets it from the title.  We'll do it properly later.
			// XXX - When doing code scope, remember there has to be a scope record in parser.  Just going by the code topics won't tell
			// you when the scope ends.
			
			foreach (Topic topic in parser.MergedTopics)
				{
				if (topic.Symbol == null)
					{  topic.Symbol = Symbol.FromPlainText (topic.UndecoratedTitle);  }
				}
			}

		}
	}