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
using GregValure.NaturalDocs.Engine.Comments;
using GregValure.NaturalDocs.Engine.Tokenization;
using GregValure.NaturalDocs.Engine.TopicTypes;


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


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>, optionally applying syntax highlighting to the contained
		 * <Tokenizer> as well.
		 */
		public virtual ParsedPrototype ParsePrototype (string rawPrototype, int topicTypeID, bool syntaxHighlight = false)
			{
			ParsedPrototype prototype = new ParsedPrototype(rawPrototype);
			SafeStack<char> brackets = null;


			// Search for the first opening bracket or brace.

			TokenIterator iterator = prototype.Tokenizer.FirstToken;

			while (iterator.IsInBounds)
				{
				if (iterator.Character == '(' || iterator.Character == '{')
					{
					brackets = new SafeStack<char>();
					brackets.Push(iterator.Character);
					iterator.Next();
					break;
					}
				else if (TryToSkipComment(ref iterator) ||
							  TryToSkipString(ref iterator))
					{  }
				else
					{  iterator.Next();  }
				}

			if (brackets != null)
				{
				prototype.AddStartOfParameter(iterator);

				while (iterator.IsInBounds)
					{
					if (brackets.Count == 1 && (iterator.Character == ',' || iterator.Character == ';'))
						{
						iterator.Next();
						prototype.AddStartOfParameter(iterator);
						}

					// Unlike prototype detection, here we treat < as an opening bracket.  Since we're already in the parameter list
					// we shouldn't run into it as part of an operator overload, and we need it to not treat the comma in "template<a,b>"
					// as a parameter divider.
					else if (iterator.Character == '(' || iterator.Character == '[' || 
								  iterator.Character == '{' || iterator.Character == '<')
						{
						brackets.Push(iterator.Character);
						iterator.Next();
						}

					else if ( (iterator.Character == ')' && brackets.Peek() == '(') ||
									(iterator.Character == ']' && brackets.Peek() == '[') ||
									(iterator.Character == '}' && brackets.Peek() == '{') ||
									(iterator.Character == '>' && brackets.Peek() == '<') )
						{
						brackets.Pop();
					
						if (brackets.Count == 0)
							{
							prototype.AddEndOfParameters(iterator);
							break;
							}
						else
							{  iterator.Next();  }
						}

					else if (TryToSkipComment(ref iterator) || TryToSkipString(ref iterator))
						{  }

					else
						{  iterator.Next();  }
					}
				}

			if (syntaxHighlight)
				{  SyntaxHighlight(prototype.Tokenizer, topicTypeID);  }

			return prototype;
			}


		/* Function: SyntaxHighlight
		 * Applies <SyntaxHighlightingTypes> to the passed tokenized content.  If it's for a prototype you can pass the
		 * topic type ID, or leave it zero if it's just for general code.
		 */
		public virtual void SyntaxHighlight (Tokenizer content, int topicTypeID = 0)
			{
			SimpleSyntaxHighlight(content, defaultKeywords);
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
				comment.Start = parser.TokenizedSourceCode.FirstLine;
				comment.End = parser.TokenizedSourceCode.LastLine;

				parser.PossibleDocumentationComments.Add(comment);
				}
			else
				{
				LineIterator lineIterator = parser.TokenizedSourceCode.FirstLine;

				while (lineIterator.IsInBounds)
					{
					if (parser.Cancelled)
						{  return;  }
				
					PossibleDocumentationComment comment = null;
				
				
					// Javadoc block comments

					// We test for these before regular block comments because they are usually extended versions of them, such
					// as /** and /*.

					// We also test block comments in general ahead of line comments because in Lua the line comments are a
					// substring of them: -- versus --[[ and ]]--.
				
					if (JavadocBlockCommentStringPairs != null)
						{
						for (int i = 0; comment == null && i < JavadocBlockCommentStringPairs.Length; i += 2)
							{
							comment = TryToGetPDBlockComment(parser, lineIterator, JavadocBlockCommentStringPairs[i],
																										 JavadocBlockCommentStringPairs[i+1], true);
							}

						if (comment != null)
							{  comment.Javadoc = true;  }
						}
					
					
					// Plain block comments
					
					if (comment == null && BlockCommentStringPairs != null)
						{
						for (int i = 0; comment == null && i < BlockCommentStringPairs.Length; i += 2)
							{
							comment = TryToGetPDBlockComment(parser, lineIterator, BlockCommentStringPairs[i], 
																										 BlockCommentStringPairs[i+1], false);
							}

						// Skip Splint comments so that they can appear in prototypes.
						if (comment != null && comment.Start.FirstToken(LineBoundsMode.CommentContent).Character == '@')
							{
							LineIterator lastLine = comment.End;
							lastLine.Previous();

							TokenIterator lastToken, ignore;
							lastLine.GetBounds(LineBoundsMode.CommentContent, out ignore, out lastToken);
							lastToken.Previous();

							if (lastToken.Character == '@')
								{  comment = null;  }
							}
						}
					
					
					// XML line comments

					if (comment == null && XMLLineCommentStrings != null)
						{
						for (int i = 0; comment == null && i < XMLLineCommentStrings.Length; i++)
							{
							comment = TryToGetPDLineComment(parser, lineIterator, XMLLineCommentStrings[i],
																									  XMLLineCommentStrings[i], true);
							}

						if (comment != null)
							{  comment.XML = true;  }
						}
						
						
					// Javadoc line comments

					// We check for these even if a XML comment is found because they may share an opening symbol, such as ///.
					// We change it to Javadoc if it's longer.  If it's equal it's just interpreting the XML as a Javadoc start with a
					// vertical line for the remainder, so leave it as XML.  Unless the comment is only one line long, in which case it's
					// genuinely ambiguous.
				
					if ( (comment == null || comment.XML == true) && JavadocLineCommentStringPairs != null)
						{
						PossibleDocumentationComment javadocComment = null;

						for (int i = 0; javadocComment == null && i < JavadocLineCommentStringPairs.Length; i += 2)
							{
							javadocComment = TryToGetPDLineComment(parser, lineIterator, JavadocLineCommentStringPairs[i],
																													  JavadocLineCommentStringPairs[i+1], true);
							}

						if (javadocComment != null)
							{
							javadocComment.Javadoc = true;

							if (comment == null)
								{  comment = javadocComment;  }
							else
								{
								int javadocLength = javadocComment.End.LineNumber - javadocComment.Start.LineNumber;
								int xmlLength = comment.End.LineNumber - comment.Start.LineNumber;

								if (javadocLength > xmlLength)
									{  comment = javadocComment;  }
								else if (javadocLength == 1 && xmlLength == 1)
									{  comment.Javadoc = true;  }
								// else stay with the XML comment
								}
							}
						}
						

					// Plain line comments
				
					if (comment == null && LineCommentStrings != null)
						{
						for (int i = 0; comment == null && i < LineCommentStrings.Length; i++)
							{
							comment = TryToGetPDLineComment(parser, lineIterator, LineCommentStrings[i], LineCommentStrings[i], false);
							}
						}
					
				
					// Nada.
				
					if (comment == null)
						{  lineIterator.Next();  }
					else
						{
						parser.PossibleDocumentationComments.Add(comment);
						lineIterator = comment.End;
						}
					
					}
				}
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
			string openingSymbol = null;
			string closingSymbol = null;

			bool goodPrototype = false;

			while (iterator < limit)
				{
				TokenIterator originalIterator = iterator;


				// Line Break

				if (iterator.FundamentalType == FundamentalType.LineBreak)
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
				else if (TryToSkipLineComment(ref iterator))
					{
					// Treat it as whitespace.  We're only dealing with Splint for block comments.

					if (lastWasWhitespace == false)
						{
						prototype.Append(' ');
						lastWasWhitespace = true;
						}
					}


				// Block Comment

				// We test this before looking for opening brackets in case the opening symbols are used for comments.
				else if (TryToSkipBlockComment(ref iterator, out openingSymbol, out closingSymbol))
					{
					TokenIterator commentContentStart = originalIterator;
					commentContentStart.NextByCharacters(openingSymbol.Length);

					TokenIterator commentContentEnd = iterator;
					commentContentEnd.PreviousByCharacters(closingSymbol.Length);

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


				// Strings

				else if (TryToSkipString(ref iterator))
					{
					// This also avoids whitespace condensation while in a string.

					tokenizer.AppendTextBetweenTo(originalIterator, iterator, prototype);
					lastWasWhitespace = false;
					}


				// Opening Bracket

				// We don't test for < because there might be an unbalanced pair as part of an operator overload.
				else if (iterator.Character == '(' || iterator.Character == '[' || iterator.Character == '{')
					{
					brackets.Push(iterator.Character);
					iterator.AppendTokenTo(prototype);
					iterator.Next();
					lastWasWhitespace = false;
					}


				// Closing Bracket, matching the last opening one

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

			// If the iterator ran past the limit, that means something like a string or a block comment was not closed before it
			// reached the limit.  Consider the prototype bad.
			if (iterator > limit)
				{  goodPrototype = false;  }

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



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: TryToGetPDBlockComment
		 * 
		 * If the line iterator is on the starting symbol of a block comment, return it as a <PossibleDocumentationComment>
		 * and mark the symbols as <CommentParsingType.CommentSymbol>.  If the iterator is not on the opening comment
		 * symbol or there is content after the closing comment symbol making it unsuitable as a documentation comment,
		 * returns null.
		 * 
		 * If openingMustBeAlone is set, that means no symbol can appear immediately after the opening symbol for this
		 * function to succeed.  This allows you to specifically detect something like /** without also matching /******.
		 */
		protected PossibleDocumentationComment TryToGetPDBlockComment (ParseState parser, LineIterator lineIterator, 
																																				  string openingSymbol, string closingSymbol,
																																				  bool openingMustBeAlone)
			{
			TokenIterator firstToken = lineIterator.FirstToken(LineBoundsMode.ExcludeWhitespace);

			if (firstToken.MatchesAcrossTokens(openingSymbol) == false)
				{  return null;  }

			if (openingMustBeAlone)
				{
				TokenIterator nextToken = firstToken;
				nextToken.NextByCharacters(openingSymbol.Length);
				if (nextToken.FundamentalType == FundamentalType.Symbol)
					{  return null;  }
				}

			PossibleDocumentationComment comment = new PossibleDocumentationComment();
			comment.Start = lineIterator;

			for (;;)
				{
				if (parser.Cancelled || !lineIterator.IsInBounds)
					{  return null;  }
					
				TokenIterator closingSymbolIterator;
				
				if (lineIterator.FindAcrossTokens(closingSymbol, false, LineBoundsMode.Everything, out closingSymbolIterator) == true)
					{
					closingSymbolIterator.NextByCharacters(closingSymbol.Length);

					while (closingSymbolIterator.FundamentalType == FundamentalType.Whitespace)
						{  closingSymbolIterator.Next();  }

					if (closingSymbolIterator.FundamentalType != FundamentalType.LineBreak &&
						 closingSymbolIterator.FundamentalType != FundamentalType.Null)
						{  return null;  }

					lineIterator.Next();
					comment.End = lineIterator;
					break;
					}

				lineIterator.Next();
				}
			
			// Success.  Mark the symbols before returning.
			firstToken.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, openingSymbol.Length);

			TokenIterator lastToken;
			lineIterator.Previous();
			lineIterator.GetBounds(LineBoundsMode.ExcludeWhitespace, out firstToken, out lastToken);
			lastToken.PreviousByCharacters(closingSymbol.Length);
			lastToken.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, closingSymbol.Length);

			return comment;
			}

			
		/* Function: TryToGetPDLineComment
		 * 
		 * If the line iterator is on a line comment, return it and all connected line comments as a 
		 * <PossibleDocumentationComment> and mark the symbols as <CommentParsingType.CommentSymbol>.  Returns null
		 * otherwise.
		 * 
		 * This function takes a separate comment symbol for the first line and all remaining lines, allowing you to detect
		 * Javadoc line comments that start with ## and the remaining lines use #.  Both symbols can be the same if this isn't
		 * required.  If openingMustBeAlone is set, no symbol can appear immediately after the first line symbol for this
		 * function to succeed.  This allows you to specifically detect something like ## without also matching #######.
		 */
		protected PossibleDocumentationComment TryToGetPDLineComment (ParseState parser, LineIterator lineIterator, 
																																				string firstSymbol, string remainderSymbol,
																																				bool openingMustBeAlone)
			{
			TokenIterator firstToken = lineIterator.FirstToken(LineBoundsMode.ExcludeWhitespace);

			if (firstToken.MatchesAcrossTokens(firstSymbol) == false)
				{  return null;  }

			if (openingMustBeAlone)
				{
				TokenIterator nextToken = firstToken;
				nextToken.NextByCharacters(firstSymbol.Length);
				if (nextToken.FundamentalType == FundamentalType.Symbol)
					{  return null;  }
				}

			PossibleDocumentationComment comment = new PossibleDocumentationComment();
			comment.Start = lineIterator;
			lineIterator.Next();

			// Since we're definitely returning a comment (barring the operation being cancelled) we can mark the comment
			// symbols as we go rather than waiting until the end.
			firstToken.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, firstSymbol.Length);

			while (lineIterator.IsInBounds)
				{
				if (parser.Cancelled)
					{  return null;  }

				firstToken = lineIterator.FirstToken(LineBoundsMode.ExcludeWhitespace);
					
				if (firstToken.MatchesAcrossTokens(remainderSymbol) == false)
					{  break;  }

				firstToken.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, remainderSymbol.Length);
				lineIterator.Next();
				}
			
			comment.End = lineIterator;
			return comment;
			}


		/* Function: SimpleSyntaxHighlight
		 */
		protected void SimpleSyntaxHighlight (Tokenizer content, StringSet keywords)
			{
			TokenIterator iterator = content.FirstToken;
			
			while (iterator.IsInBounds)
				{
				TokenIterator originalPosition = iterator;

				if (TryToSkipComment(ref iterator))
					{
					content.SetSyntaxHighlightingTypeBetween(originalPosition, iterator, SyntaxHighlightingType.Comment);
					}
				else if (TryToSkipString(ref iterator))
					{
					content.SetSyntaxHighlightingTypeBetween(originalPosition, iterator, SyntaxHighlightingType.String);
					}
				else if (iterator.FundamentalType == FundamentalType.Text)
					{
					if (iterator.Character >= '0' && iterator.Character <= '9')
						{
						iterator.SyntaxHighlightingType = SyntaxHighlightingType.Number;
						iterator.Next();

						if (iterator.Character == '.')
							{
							iterator.SyntaxHighlightingType = SyntaxHighlightingType.Number;
							iterator.Next();

							if (iterator.Character >= '0' && iterator.Character <= '9')
								{
								iterator.SyntaxHighlightingType = SyntaxHighlightingType.Number;
								iterator.Next();
								}
							}
						else
							{
							TokenIterator prev = originalPosition;
							prev.Previous();

							// For contants like .25 instead of 0.25.
							if (prev.Character == '.')
								{  prev.SyntaxHighlightingType = SyntaxHighlightingType.Number;  }
							}
						}

					else // not digits
						{
						// Note that this won't catch keywords with underscores in them, like wchar_t.

						if (keywords.Contains(iterator.String))
							{  iterator.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

						iterator.Next();
						}
					}
				else
					{  iterator.Next();  }
				}
			}


		/* Function: TryToSkipWhitespace
		 * If the iterator is on whitespace or a comment, move past it and return true.
		 */
		protected bool TryToSkipWhitespace (ref TokenIterator iterator, bool includeLineBreaks = true)
			{
			bool success = false;

			for (;;)
				{
				if (iterator.FundamentalType == FundamentalType.Whitespace ||
					 (includeLineBreaks == true && iterator.FundamentalType == FundamentalType.LineBreak) )
					{
					iterator.Next();
					success = true;
					}
				else if (TryToSkipComment(ref iterator))
					{  success = true;  }
				else
					{  break;  }
				}

			return success;
			}


		/* Function: TryToSkipComment
		 * If the iterator is on a comment symbol, moves past it and returns true.  If you need information about the specific type of
		 * comment it was, you need to call <TryToSkipLineComment()> and <TryToSkipBlockComment()> individually.
		 */
		protected bool TryToSkipComment (ref TokenIterator iterator)
			{
			return ( TryToSkipLineComment(ref iterator) || TryToSkipBlockComment(ref iterator) );
			}


		/* Function: TryToSkipLineComment
		 * If the iterator is on a line comment symbol, moves the iterator past it, provides information about the comment, and returns
		 * true.  It will not skip the line break after the comment since that may be relevant to the calling code.
		 */
		protected bool TryToSkipLineComment (ref TokenIterator iterator, out string commentSymbol)
			{
			if (LineCommentStrings == null)
				{
				commentSymbol = null;
				return false;
				}

			int commentSymbolIndex = iterator.MatchesAnyAcrossTokens(LineCommentStrings);

			if (commentSymbolIndex == -1)
				{
				commentSymbol = null;
				return false;
				}

			commentSymbol = LineCommentStrings[commentSymbolIndex];
			iterator.NextByCharacters(commentSymbol.Length);

			while (iterator.FundamentalType != FundamentalType.LineBreak)
				{  iterator.Next();  }

			return true;
			}


		/* Function: TryToSkipLineComment
		 * If the iterator is on a line comment symbol, moves the iterator past it and returns true.  It will not skip the line break 
		 * after the comment since that may be relevant to the calling code.
		 */
		protected bool TryToSkipLineComment (ref TokenIterator iterator)
			{
			string ignore;
			return TryToSkipLineComment(ref iterator, out ignore);
			}


		/* Function: TryToSkipBlockComment
		 * If the iterator is on an opening block comment symbol, moves the iterator past it, provides information about the comment,
		 * and returns true.
		 */
		protected bool TryToSkipBlockComment (ref TokenIterator iterator, out string openingSymbol, out string closingSymbol)
			{
			if (BlockCommentStringPairs == null)
				{
				openingSymbol = null;
				closingSymbol = null;
				return false;
				}

			int openingCommentSymbolIndex = iterator.MatchesAnyPairAcrossTokens(BlockCommentStringPairs);

			if (openingCommentSymbolIndex == -1)
				{
				openingSymbol = null;
				closingSymbol = null;
				return false;
				}

			openingSymbol = BlockCommentStringPairs[openingCommentSymbolIndex];
			closingSymbol = BlockCommentStringPairs[openingCommentSymbolIndex + 1];
			iterator.NextByCharacters(openingSymbol.Length);

			while (iterator.IsInBounds && iterator.MatchesAcrossTokens(closingSymbol) == false)
				{  iterator.Next();  }

			if (iterator.IsInBounds)
				{  iterator.NextByCharacters(closingSymbol.Length);  }

			// Return true even if the iterator reached the end of the content before finding a closing symbol.
			return true;
			}


		/* Function: TryToSkipBlockComment
		 * If the iterator is on an opening block comment symbol, moves the iterator past it and returns true.
		 */
		protected bool TryToSkipBlockComment (ref TokenIterator iterator)
			{
			string ignore1, ignore2;
			return TryToSkipBlockComment (ref iterator, out ignore1, out ignore2);
			}


		/* Function: TryToSkipString
		 * If the iterator is on a quote symbol, moves the iterator past the entire string and returns true.
		 */
		protected bool TryToSkipString (ref TokenIterator iterator)
			{
			if (iterator.Character != '"')
				{  return false;  }

			iterator.Next();

			while (iterator.IsInBounds)
				{
				if (iterator.Character == '"')
					{
					iterator.Next();
					break;
					}
				else if (iterator.Character == '\\')
					{  iterator.Next(2);  }
				else 
					{  iterator.Next();  }
				}

			// Return true even if the iterator reached the end of the content before finding a closing quote.
			return true;
			}



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: defaultKeywords
		 * A set of the default keywords for basic language support across all languages.
		 */
		static protected StringSet defaultKeywords = new StringSet(false, false, new string[] {

			// This isn't comprehensive but should cover most languages.

			"int", "int8", "int16", "int32", "int64", "uint", "uint8", "uint16", "uint32", "uint64",
			"signed", "unsigned", "integer", "long", "ulong", "short", "ushort", "real", "float", "double", "decimal",
			"float32", "float64", "float80", "void", "char", "string", "byte", "ubyte", "sbyte", "bool", "boolean",
			"var", "true", "false", "null", "undefined",

			"function", "operator", "delegate", "event", "enum", "typedef",

			"class", "struct", "interface", "template", "package", "union", "namespace",

			"extends", "implements", "import", "export", "extern", "native", "override", "overload", "explicit", "implicit",
			"super", "base", "my", "our", "require",

			"public", "private", "protected", "internal", "static", "virtual", "abstract", "friend", 
			"inline", "using", "final", "sealed", "register", "volatile",

			"ref", "in", "out", "inout", "const", "constant", "get", "set",

			"if", "else", "elif", "elseif", "for", "foreach", "each", "do", "while", "switch", "case", "with", "in",
			"break", "continue", "next", "return", "goto",
			"try", "catch", "throw", "finally", "throws", "lock", "eval",

			"new", "delete", "sizeof"
			});

		}
	}