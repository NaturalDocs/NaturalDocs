/* 
 * Class: GregValure.NaturalDocs.Engine.Languages.Language
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
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
		virtual public ParseResult Parse (Tokenizer newTokenizedSourceCode, int fileID, CancelDelegate newCancelDelegate, 
																  out List<Topic> topics)
			{
			topics = null;
			ParseState parser = new ParseState();
			
			parser.TokenizedSourceCode = newTokenizedSourceCode;
			parser.CancelDelegate = newCancelDelegate;
			
			ExtractPossibleDocumentationComments(parser);
			
			if (parser.Cancelled)
				{  return ParseResult.Cancelled;  }
				
			ParsePossibleDocumentationComments(parser);
			
			if (parser.Cancelled)
				{  return ParseResult.Cancelled;  }
				
			// XXX: Parse for code topics
			parser.CodeTopics = new List<Topic>();
			
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


			
		// Group: Overridable Parsing Stages
		// Override these stages in subclasses as necessary.
		// __________________________________________________________________________
		
			
		// Function: ExtractPossibleDocumentationComments
		// 
		// Goes through the file looking for comments that could possibly contain documentation and retrieves them as a list in
		// <ParseState.PossibleDocumentationComments>.  These comments are not guaranteed to have documentation in them, 
		// just to be acceptable candidates for them.  If there are no comments, PossibleDocumentationComments will be set to
		// an empty list.
		//
		// All the comments in the returned list will have their comment symbols marked as <TokenType.CommentSymbol> in the
		// tokenizer.  This allows further operations to be done on them in a language independent manner.
		//
		// Default Implementation:
		//
		// The default implementation uses the comment symbols found in <Language>.  You can override this function if you need
		// to do something more sophisticated, such as interpret the POD directives in Perl.
		//
		// Comments must be alone on a line to be a candidate for documentation, meaning that the comment symbol must be the 
		// first non-whitespace character on a line, and in the case of block comments, nothing but whitespace may trail the closing
		// symbol.  The latter rule is important because a comment may start correctly but not end so, as in this prototype with splint 
		// annotation:
		// 
		// > int get_array(integer_t id,
		// >               /*@out@*/ array_t array);
		//
		// It also goes through the code line by line in a simple manner, not accounting for things like strings, so if a language contains
		// a multiline string whose content looks like a language comment it will be interpreted as one.  This isn't ideal but is accepted
		// as a conscious tradeoff because there are actually many different string formats (literal quotes denoted with \", literal quotes 
		// denoted with "", Perl's q{} forms and here doc) so you can't account for them all in a generalized way.  Also, putting this in 
		// an independent stage even when using full language support means comments don't disappear the way prototypes do if the 
		// parser gets tripped up on something like an unmatched brace.
		//
		virtual protected void ExtractPossibleDocumentationComments (ParseState parser)
			{
			parser.PossibleDocumentationComments = new List<PossibleDocumentationComment>();
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
							
							if (afterOpeningBlock.FundamentalType != TokenType.Symbol)
								{
								startOfLine.ChangeTypeByCharacters(TokenType.CommentSymbol, 
																						JavadocBlockCommentStringPairs[i].Length);

								PossibleDocumentationComment comment = new PossibleDocumentationComment();
								comment.Type = Comments.Type.Javadoc;
								comment.Start = lineIterator;

								if (ExtractPossibleDocumentationComments_GetUntil (parser, JavadocBlockCommentStringPairs[i+1],
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
							startOfLine.ChangeTypeByCharacters(TokenType.CommentSymbol, 
																					BlockCommentStringPairs[i].Length);

							PossibleDocumentationComment comment = new PossibleDocumentationComment();
							comment.Type = Comments.Type.Plain;
							comment.Start = lineIterator;
							
							if (ExtractPossibleDocumentationComments_GetUntil (parser, BlockCommentStringPairs[i+1],
																										  ref lineIterator, comment) )
								{  parser.PossibleDocumentationComments.Add(comment);  }
							
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
							
							if (afterOpeningBlock.FundamentalType != TokenType.Symbol)
								{
								startOfLine.ChangeTypeByCharacters(TokenType.CommentSymbol, 
																						JavadocLineCommentStringPairs[i].Length);

								PossibleDocumentationComment comment = new PossibleDocumentationComment();
								comment.Type = Comments.Type.Javadoc;
								comment.Start = lineIterator;
								
								lineIterator.Next();

								ExtractPossibleDocumentationComments_GetWhile (parser, JavadocLineCommentStringPairs[i+1],
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
							
							if (afterOpeningBlock.FundamentalType != TokenType.Symbol)
								{
								startOfLine.ChangeTypeByCharacters(TokenType.CommentSymbol, 
																						XMLLineCommentStrings[i].Length);

								PossibleDocumentationComment comment = new PossibleDocumentationComment();
								comment.Type = Comments.Type.XML;
								comment.Start = lineIterator;
								
								lineIterator.Next();

								ExtractPossibleDocumentationComments_GetWhile (parser, XMLLineCommentStrings[i],
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
							startOfLine.ChangeTypeByCharacters(TokenType.CommentSymbol, 
																					LineCommentStrings[i].Length);

							PossibleDocumentationComment comment = new PossibleDocumentationComment();
							comment.Type = Comments.Type.Plain;
							comment.Start = lineIterator;
							
							lineIterator.Next();

							ExtractPossibleDocumentationComments_GetWhile (parser, LineCommentStrings[i],
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

			
		/* Function: ExtractPossibleDocumentationComments_GetUntil
		 * 
		 * A helper function used only by <ExtractPossibleDocumentationComments()> that advances the iterator until it
		 * reaches a line containing the passed closing comment symbol.  If that was the last thing on the line, it sets the ending
		 * iterator field on the comment object and returns true.  If not, or if it reaches the end of the file, it returns false.
		 * The closing comment symbol will be marked as <TokenType.CommentSymbol>.
		 * 
		 * The passed iterator should be on the first line of the comment so that it can capture a single line block comment.  The 
		 * iterator will be left on the line following the one with the ending comment symbol.
		 */
		 protected bool ExtractPossibleDocumentationComments_GetUntil (ParseState parser, string closingCommentSymbol, 
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
						
					symbolPosition.ChangeTypeByCharacters(TokenType.CommentSymbol, closingCommentSymbol.Length);
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
			
			
		/* Function: ExtractPossibleDocumentationComments_GetWhile
		 * 
		 * A helper function used only by <ExtractPossibleDocumentationComments()> that advances the iterator until it
		 * reaches a line that doesn't start with the passed comment symbol.  It then sets the ending iterator field in
		 * the comment.  All the comment symbols will be marked as <TokenType.CommentSymbol>.
		 * 
		 * The passed iterator should start on the second line of the comment since you should already know the first one is
		 * a part of it.  The iterator will be left on the line following the last one which started with the comment symbol.
		 */
		 protected void ExtractPossibleDocumentationComments_GetWhile (ParseState parser, string commentSymbol, 
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
					symbolPosition.ChangeTypeByCharacters(TokenType.CommentSymbol, commentSymbol.Length);
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
		 * If there are none, CommentTopics will be set to an empty list.  PossibleDocumentationComments will be set to null
		 * afterwards.
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
				
			parser.PossibleDocumentationComments = null;
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