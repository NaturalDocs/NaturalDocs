/* 
 * Class: GregValure.NaturalDocs.Engine.Languages.Parser
 * ____________________________________________________________________________
 * 
 * A reusable helper class to handle <Language> parsing tasks.
 * 
 * Why a Separate Class?:
 * 
 *		Parsing is broken up into a lot of  functions and there's context information that needs to be passed back and forth
 *		between them.  These can't be instance variables in <Language> because it needs to support multiple concurrent 
 *		parsing threads.  Passing these structures around individually quickly becomes unwieldy.  If you bundle the context 
 *		up into a single object to pass around, then you've already paid for the extra allocation and you might as well put 
 *		the functions in it as well.
 * 
 * 
 * Topic: Usage
 *		
 *		- Create a Parser object.
 *		- Call any functions you need, such as <Parse()> or <GetComments()>.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.  It has an internal state that is used during parsing
 *		functions and no other functions should be called until it's completed.  Instead each thread should create its own
 *		object.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Comments;
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Tokenization;
using GregValure.NaturalDocs.Engine.TopicTypes;


namespace GregValure.NaturalDocs.Engine.Languages
	{
	public class Parser
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
		
		
		/* Function: Parser
		 */
		public Parser (Language language)
			{
			this.language = language;
			source = null;
			cancelDelegate = Delegates.NeverCancel;
			
			possibleDocumentationComments = null;
			commentTopics = null;
			codeTopics = null;
			mergedTopics = null;
			}
	

		/* Function: Reset
		 * Resets all the parse state variables so a new parsing function can start fresh.
		 */
		protected void Reset ()
			{
			source = null;
			cancelDelegate = Delegates.NeverCancel;
			
			if (possibleDocumentationComments != null)
				{  possibleDocumentationComments.Clear();  }
			if (commentTopics != null)
				{  commentTopics.Clear();  }
			if (codeTopics != null)
				{  codeTopics.Clear();  }
			if (mergedTopics != null)
				{  mergedTopics.Clear();  }
			}
	

		/* Function: Parse
		 * Parses the source code as a string and returns it as a list of <Topics>.  The list will be null if there are no topics or parsing was
		 * interrupted.  Set cancelDelegate for the ability to interrupt parsing, or use <Delegates.NeverCancel>.
		 * 
		 * If you already have the source code in tokenized form it would be more efficient to pass it as a <Tokenizer>.
		 */
		public ParseResult Parse (string sourceCodeString, int fileID, CancelDelegate cancelDelegate, out IList<Topic> topics)
			{
			return Parse( new Tokenizer(sourceCodeString), fileID, cancelDelegate, out topics );
			}
			
			
		/* Function: Parse
		 * Parses the tokenized source code and returns it as a list of <Topics>.  The list will be null if there are no topics or parsing was
		 * interrupted.  Set cancelDelegate for the ability to interrupt parsing, or use <Delegates.NeverCancel>.
		 */
		virtual public ParseResult Parse (Tokenizer tokenizedSourceCode, int fileID, CancelDelegate cancelDelegate, 
																	  out IList<Topic> topics)
			{ 
			Reset();
			topics = null;
			
			source = tokenizedSourceCode;
			this.cancelDelegate = cancelDelegate;
			
			GetPossibleDocumentationComments();
			
			if (Cancelled)
				{  return ParseResult.Cancelled;  }
				
			ParsePossibleDocumentationComments();
			
			if (Cancelled)
				{  return ParseResult.Cancelled;  }
				
			GetCodeTopics();
			
			if (Cancelled)
				{  return ParseResult.Cancelled; }
				
			MergeTopics();
			
			if (Cancelled)
				{  return ParseResult.Cancelled;  }

			foreach (Topic topic in mergedTopics)
				{
				topic.FileID = fileID;
				topic.LanguageID = language.ID;
				}
								
			GenerateRemainingSymbols();
				
			// Need to do one last check anyway, because the previous function could have quit early because of a cancellation.
			if (Cancelled)
				{  return ParseResult.Cancelled;  }
		
		
			if (mergedTopics.Count > 0)
				{  
				topics = mergedTopics;  

				// Set it to null so the parser doesn't reuse the list that was returned.
				mergedTopics = null;
				}
				
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
		public IList<PossibleDocumentationComment> GetComments (string sourceCode)
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
		public IList<PossibleDocumentationComment> GetComments (Tokenizer tokenizedSourceCode)
			{
			Reset();
			source = tokenizedSourceCode;
			
			GetPossibleDocumentationComments();

			foreach (PossibleDocumentationComment comment in possibleDocumentationComments)
				{
				Engine.Instance.Comments.LineFinder.MarkTextBoxes(comment);
				}

			// Set it to null so the parser doesn't reuse the list that was returned.
			var result = possibleDocumentationComments;
			possibleDocumentationComments = null;

			return result;
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>, optionally applying syntax highlighting to the contained
		 * <Tokenizer> as well.
		 */
		public virtual ParsedPrototype ParsePrototype (string rawPrototype, int topicTypeID, bool syntaxHighlight = false)
			{
			Reset();
			source = new Tokenizer(rawPrototype);
			ParsedPrototype parsedPrototype = new ParsedPrototype(source);


			// Search for the first opening bracket or brace.  Also be on the lookout for anything that would indicate this is a
			// class prototype.

			TokenIterator iterator = source.FirstToken;
			char closingBracket = '\0';

			while (iterator.IsInBounds)
				{
				if (iterator.Character == '(')
					{
					closingBracket = ')';
					break;
					}
				else if (iterator.Character == '{')
					{
					closingBracket = '}';
					break;
					}
				else if (TryToSkipComment(ref iterator) ||
							  TryToSkipString(ref iterator))
					{  }
				else
					{  iterator.Next();  }
				}


			// If we found brackets, it's either a function prototype or a class prototype that includes members.  
			// Separate out the parameters/members.

			if (closingBracket != '\0')
				{
				iterator.PrototypeParsingType = PrototypeParsingType.StartOfParams;
				iterator.Next();

				while (iterator.IsInBounds)
					{
					if (iterator.Character == ',' || iterator.Character == ';')
						{
						iterator.PrototypeParsingType = PrototypeParsingType.ParamSeparator;
						iterator.Next();
						}

					else if (iterator.Character == closingBracket)
						{
						iterator.PrototypeParsingType = PrototypeParsingType.EndOfParams;
						break;
						}

					// Unlike prototype detection, here we treat < as an opening bracket.  Since we're already in the parameter list
					// we shouldn't run into it as part of an operator overload, and we need it to not treat the comma in "template<a,b>"
					// as a parameter divider.
					else if (TryToSkipBlock(ref iterator, true) || 
								 TryToSkipComment(ref iterator) || 
								 TryToSkipString(ref iterator))
						{  }

					else
						{  iterator.Next();  }
					}
				

				// If we have any, parse the parameters.

				// We use ParsedPrototype.GetParameter() instead of trying to build it into the loop above because ParsedPrototype 
				// does things like trimming whitespace and ignoring empty parenthesis.

				TokenIterator start, end;

				if (parsedPrototype.NumberOfParameters > 0)
					{
					for (int i = 0; i < parsedPrototype.NumberOfParameters; i++)
						{
						parsedPrototype.GetParameter(i, out start, out end);
						ParsePrototypeParameter(start, end, topicTypeID);
						}
					}


				// Mark the return value of functions.

				parsedPrototype.GetAfterParameters(out start, out end);

				// Exclude the closing bracket
				start.Next();
				start.NextPastWhitespace(end);

				// If there's a colon immediately after the parameters, it's a Pascal-style function.  Mark the return value after it 
				// the same as the part of a parameter after the colon.
				if (start < end && start.Character == ':')
					{  
					start.Next();
					start.NextPastWhitespace();

					if (start < end)
						{  MarkPascalParameterAfterColon(start, end, topicTypeID);  }
					}

				// Otherwise it's a C-style function.  Mark the part before the parameters as if it was a parameter to get the return
				// value.
				else
					{  
					parsedPrototype.GetBeforeParameters(out start, out end);

					// Exclude the opening bracket
					end.Previous();
					end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

					if (start < end)
						{  MarkCParameter(start, end, topicTypeID);  }
					}
				}

			
			// If there's no brackets, it's a variable or property.  Mark it like a parameter.

			else
				{
				TokenIterator start, end;
				parsedPrototype.GetCompletePrototype(out start, out end);
				ParsePrototypeParameter(start, end, topicTypeID);
				}

			if (syntaxHighlight)
				{  
				// Using the public-facing SyntaxHighlight function resets the parse state, but we don't need it anymore.
				SyntaxHighlight(source, topicTypeID);  
				}

			return parsedPrototype;
			}


		/* Function: SyntaxHighlight
		 * Applies <SyntaxHighlightingTypes> to the passed tokenized content.  If it's for a prototype you can pass the
		 * topic type ID, or leave it zero if it's just for general code.
		 */
		public virtual void SyntaxHighlight (Tokenizer source, int topicTypeID = 0)
			{
			Reset();
			this.source = source;

			SimpleSyntaxHighlight();
			}


		/* Function: IsBuiltInType
		 * Returns whether the type string is a built-in type such as "int" as opposed to a user-defined type.
		 */
		public bool IsBuiltInType (string type)
			{
			return defaultKeywords.Contains(type);
			}


			
		// Group: Overridable Parsing Stages
		// Override these stages in subclasses as necessary.
		// __________________________________________________________________________
		
			
		// Function: GetPossibleDocumentationComments
		// 
		// Goes through the file looking for comments that could possibly contain documentation and retrieves them as a list in
		// <PossibleDocumentationComments>.  These comments are not guaranteed to have documentation in them, just to be
		// acceptable candidates for them.  If there are no comments, PossibleDocumentationComments will be set to an empty list.
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
		virtual protected void GetPossibleDocumentationComments ()
			{
			possibleDocumentationComments = new List<PossibleDocumentationComment>();

			if (language.Type == Language.LanguageType.TextFile)
				{
				PossibleDocumentationComment comment = new PossibleDocumentationComment();
				comment.Start = source.FirstLine;
				comment.End = source.LastLine;

				possibleDocumentationComments.Add(comment);
				}
			else
				{
				LineIterator lineIterator = source.FirstLine;

				while (lineIterator.IsInBounds)
					{
					if (Cancelled)
						{  return;  }
				
					PossibleDocumentationComment comment = null;
				
				
					// Javadoc block comments

					// We test for these before regular block comments because they are usually extended versions of them, such
					// as /** and /*.

					// We also test block comments in general ahead of line comments because in Lua the line comments are a
					// substring of them: -- versus --[[ and ]]--.
				
					if (language.JavadocBlockCommentStringPairs != null)
						{
						for (int i = 0; comment == null && i < language.JavadocBlockCommentStringPairs.Length; i += 2)
							{
							comment = TryToGetPDBlockComment(lineIterator, language.JavadocBlockCommentStringPairs[i],
																										 language.JavadocBlockCommentStringPairs[i+1], true);
							}

						if (comment != null)
							{  comment.Javadoc = true;  }
						}
					
					
					// Plain block comments
					
					if (comment == null && language.BlockCommentStringPairs != null)
						{
						for (int i = 0; comment == null && i < language.BlockCommentStringPairs.Length; i += 2)
							{
							comment = TryToGetPDBlockComment(lineIterator, language.BlockCommentStringPairs[i], 
																										 language.BlockCommentStringPairs[i+1], false);
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

					if (comment == null && language.XMLLineCommentStrings != null)
						{
						for (int i = 0; comment == null && i < language.XMLLineCommentStrings.Length; i++)
							{
							comment = TryToGetPDLineComment(lineIterator, language.XMLLineCommentStrings[i],
																									  language.XMLLineCommentStrings[i], true);
							}

						if (comment != null)
							{  comment.XML = true;  }
						}
						
						
					// Javadoc line comments

					// We check for these even if a XML comment is found because they may share an opening symbol, such as ///.
					// We change it to Javadoc if it's longer.  If it's equal it's just interpreting the XML as a Javadoc start with a
					// vertical line for the remainder, so leave it as XML.  Unless the comment is only one line long, in which case it's
					// genuinely ambiguous.
				
					if ( (comment == null || comment.XML == true) && language.JavadocLineCommentStringPairs != null)
						{
						PossibleDocumentationComment javadocComment = null;

						for (int i = 0; javadocComment == null && i < language.JavadocLineCommentStringPairs.Length; i += 2)
							{
							javadocComment = TryToGetPDLineComment(lineIterator, language.JavadocLineCommentStringPairs[i],
																													  language.JavadocLineCommentStringPairs[i+1], true);
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
				
					if (comment == null && language.LineCommentStrings != null)
						{
						for (int i = 0; comment == null && i < language.LineCommentStrings.Length; i++)
							{
							comment = TryToGetPDLineComment(lineIterator, language.LineCommentStrings[i], language.LineCommentStrings[i], false);
							}
						}
					
				
					// Nada.
				
					if (comment == null)
						{  lineIterator.Next();  }
					else
						{
						possibleDocumentationComments.Add(comment);
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
		protected virtual void ParsePossibleDocumentationComments ()
			{
			commentTopics = new List<Topic>();
				
			foreach (PossibleDocumentationComment comment in possibleDocumentationComments)
				{
				Engine.Instance.Comments.Parse(comment, commentTopics);
				
				if (Cancelled)
					{  return;  }
				}
			}


		/* Function: GetCodeTopics
		 * 
		 * Goes through the file looking for code elements that should be included in the output and creates a list in <CodeTopics>.  
		 * If there are none, CodeTopics will be set to an empty list.
		 *
		 * Default Implementation:
		 *
		 * The default implementation uses <CommentTopics> and the language's prototype enders to gather  prototypes for languages 
		 * with basic support.  It basically takes the code between the end of the comment topic and the next one (or the next entry in 
		 * <PossibleDocumentationComments>) and if it finds the topic title before it finds an ender, the prototype will be the code 
		 * between the topic and the ender.  You can override this function to do real language processing for full support.
		 *
		 * This function does the basic looping of the search but throws the actual prototype detection to <GetPrototype()>.
		 * This makes it easier for tweaks to be implemented for certain languages that have unique prototype formats but don't 
		 * have full language support.
		 */
		protected virtual void GetCodeTopics ()
			{
			codeTopics = new List<Topic>();

			int topicIndex = 0;

			for (int commentIndex = 0; commentIndex < possibleDocumentationComments.Count; commentIndex++)
				{
				if (Cancelled)
					{  return;  }

				PossibleDocumentationComment comment = possibleDocumentationComments[commentIndex];

				// Advance the topic index to the last one before the end of this comment.  If there are multiple topics in a 
				// comment only the last one gets a prototype search.

				while (topicIndex + 1 < commentTopics.Count && 
							commentTopics[topicIndex + 1].CommentLineNumber < comment.End.LineNumber)
					{  topicIndex++;  }

				if (topicIndex >= commentTopics.Count ||
					 commentTopics[topicIndex].CommentLineNumber < comment.Start.LineNumber ||
					 commentTopics[topicIndex].CommentLineNumber > comment.End.LineNumber)
					{  
					// We're out of topics or the one we're on isn't in this comment.
					continue;  
					}

				// Build the bounds for the prototype search and perform it.

				Tokenization.LineIterator startCode = comment.End;
				Tokenization.LineIterator endCode;

				if (commentIndex + 1 < possibleDocumentationComments.Count)
					{  endCode = possibleDocumentationComments[commentIndex + 1].Start;  }
				else
					{  endCode = source.LastLine;  }

				GetPrototype(commentTopics[topicIndex], startCode, endCode);
				}
			}


		/* Function: GetPrototype
		 * Attempts to find a prototype for the passed <Topic> between the iterators.  If one is found, it will set <Topic.Prototoype>.
		 */
		protected virtual void GetPrototype (Topic topic, LineIterator startCode, LineIterator endCode)
			{
			PrototypeEnders prototypeEnders = language.GetPrototypeEnders(topic.TopicTypeID);

			if (prototypeEnders == null)
				{  return;  }

			StringBuilder prototype = new StringBuilder();

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

				else if (language.LineExtender != null && iterator.MatchesAcrossTokens(language.LineExtender))
					{
					// If the line extender is an underscore we don't want to include it if it's adjacent to any text because
					// it's probably part of an identifier.

					bool partOfIdentifier = false;

					if (language.LineExtender == "_")
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
					if (source.MatchTextBetween(acceptablePrototypeCommentRegex, 
																			 commentContentStart, commentContentEnd).Success)
						{
						prototype.Append(openingSymbol);

						string commentContent = source.TextBetween(commentContentStart, commentContentEnd);
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

					source.AppendTextBetweenTo(originalIterator, iterator, prototype);
					lastWasWhitespace = false;
					}


				// Opening Bracket

				// We don't test for < because there might be an unbalanced pair as part of an operator overload.
				// We don't use TryToSkipBlock() because it wouldn't condense whitespace and remove comments.
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
		 * Combines the topics in <CodeTopics> and <CommentTopics> into a single list and places the result in <MergedTopics>.   
		 * CodeTopics and CommentTopics will be set to null afterwards.  Headerless topics that don't match a code topic will be
		 * removed.
		 * 
		 * Note that this is a destructive act.  The <Topics> that were in CodeTopics and CommentTopics may have been modified 
		 * and placed in MergedTopics rather than new ones being created.  As such, you should not rely on or have reference to any 
		 * <Topic> on those two lists after calling this function.
		 */
		protected virtual void MergeTopics ()
			{
			// The code topics should already be in perfect form so if there's no comment topics to deal with, move the list as is.
			// This also covers if both topic lists are empty as it will just make the empty code list become the empty merged list.	
			if (commentTopics.Count == 0)
				{  
				mergedTopics = codeTopics;  
				codeTopics = null;
				commentTopics = null;
				}
				
			// If there's comment topics but no code topics, all we need to do is punt any headerless topics.
			else if (codeTopics.Count == 0)
				{
				mergedTopics = commentTopics;
				commentTopics = null;
				
				int i = 0;
				while (i < mergedTopics.Count)
					{
					if (mergedTopics[i].Title == null)
						{  mergedTopics.RemoveAt(i);  }
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
		 * Generates <Topic.Symbol> and <Topic.Parameters> for any <Topics> which don't already have one.  As code topics always 
		 * have symbols and any comment that was merged with one would inherit it, this only applies to the Natural Docs topics which 
		 * were not merged with a code element.
		 * 
		 * The default implementation will follow a combination of the code topics' scope and the Natural Docs scoping rules.  Basically, the
		 * Natural Docs scoping rules only apply between code topics, so entire classes can be defined between real elements but members
		 * would otherwise pick up their scope from the code.  Also, if this comes from a basic language support parser, there will be no
		 * code scope so Natural Docs' rules will apply to the whole thing.
		 */
		protected virtual void GenerateRemainingSymbols ()
			{
			// XXX - When doing code scope, remember there has to be a scope record in parser.  Just going by the code topics won't tell
			// you when the scope ends.  Also, you don't want to carry ND topic scoping across code topics.

			SymbolString scope = new SymbolString();

			// Generating parsed prototypes resets the parser state, so we'll create a new one on demand if we need it.
			Parser prototypeParser = null;
			
			foreach (Topic topic in mergedTopics)
				{
				TopicType topicType = Instance.TopicTypes.FromID(topic.TopicTypeID);

				if (topic.Symbol == null)
					{  
					string parenthesis = null;
					topic.Symbol = SymbolString.FromPlainText(topic.Title, out parenthesis);

					if (scope != null &&
						topicType.Scope != TopicType.ScopeValue.AlwaysGlobal &&
						topicType.Scope != TopicType.ScopeValue.End)
						{  
						topic.Symbol = scope + topic.Symbol;  
						}

					// Parenthesis in the title takes precedence over the prototype.
					if (parenthesis != null)
						{  
						topic.Parameters = ParameterString.FromParenthesisString(parenthesis);  
						}

					else if (topic.Prototype != null)
						{
						if (prototypeParser == null)
							{  prototypeParser = language.GetParser();  }

						ParsedPrototype parsedPrototype = prototypeParser.ParsePrototype(topic.Prototype, topic.TopicTypeID, false);
						
						if (parsedPrototype.NumberOfParameters > 0)
							{
							string[] parameterTypes = new string[parsedPrototype.NumberOfParameters];
							TokenIterator start, end;

							for (int i = 0; i < parsedPrototype.NumberOfParameters; i++)
								{
								parsedPrototype.GetBaseParameterType(i, out start, out end);
								parameterTypes[i] = parsedPrototype.Tokenizer.TextBetween(start, end);
								}

							topic.Parameters = ParameterString.FromParameterTypes(parameterTypes);
							}
						}
					}

				if (topicType.Scope == TopicType.ScopeValue.Start)
					{  scope = topic.Symbol;  }
				else if (topicType.Scope == TopicType.ScopeValue.End)
					{  scope = new SymbolString();  }
				}
			}



		// Group: Prototype Parsing Support Functions
		// __________________________________________________________________________


		/* Function: ParsePrototypeParameter
		 * Marks the tokens in the parameter specified by the bounds with <CommentParsingTypes>.
		 */
		protected void ParsePrototypeParameter (TokenIterator start, TokenIterator end, int topicTypeID)
			{
			// Pass 1: Count the number of "words" in the parameter and determine whether it has a colon, and is thus a Pascal-style 
			// parameter.  We'll figure out how to interpret the words in the second pass.  Pascal can define more than one parameter 
			// per type ("x, y: int") but as long as there's only one word in the first one it will still be interpreted as we want it.
			//
			// If they exist, also mark the colon as a name/type separator and mark the default value.

			int words = 0;
			int wordsBeforeColon = 0;
			bool hasColon = false;

			TokenIterator iterator = start;

			while (iterator < end)
				{
				if (iterator.Character == '=' || iterator.MatchesAcrossTokens(":="))
					{
					if (iterator.Character == '=')
						{
						iterator.PrototypeParsingType = PrototypeParsingType.DefaultValueSeparator;
						iterator.Next();
						}
					else
						{
						iterator.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.DefaultValueSeparator, 2);
						iterator.Next(2);
						}

					iterator.NextPastWhitespace(end);

					TokenIterator endOfDefaultValue = end;
					TokenIterator temp = end;
					temp.Previous();

					while (temp >= iterator && temp.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
						{
						endOfDefaultValue = temp;
						temp.Previous();
						}

					endOfDefaultValue.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, iterator);

					if (iterator < endOfDefaultValue)
						{  source.SetPrototypeParsingTypeBetween(iterator, endOfDefaultValue, PrototypeParsingType.DefaultValue);  }

					break;
					}

				// Can only check for this after checking for :=
				else if (iterator.Character == ':')
					{
					hasColon = true;
					wordsBeforeColon = words;
					iterator.PrototypeParsingType = PrototypeParsingType.NameTypeSeparator;
					iterator.Next();
					}
				else if (iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
					{
					break;
					}
				else if (TryToSkipTypeOrVarName(ref iterator, end) ||
							 TryToSkipComment(ref iterator) ||
							 TryToSkipString(ref iterator) ||
							 TryToSkipBlock(ref iterator, true))
					{
					// If there was a comment in the prototype, that means it specifically wasn't filtered out because it was something
					// significant like a Splint comment or /*out*/.  Treat it like a modifier.

					// Strings don't really make sense in the prototype until the default value, but we need the parser to handle it anyway
					// just so it doesn't lose its mind if one occurs.

					// If we come across a block that doesn't immediately follow an identifier, it may be something like a C# property so
					// treat it as a modifier.  

					words++;
					}

				// Skip over whitespace plus any unexpected random symbols that appear.
				else
					{
					iterator.Next();
					}
				}


			// Pass 2: Mark the "words" we counted from the first pass.  If we don't have a colon and thus have C-style parameters, 
			// the order of words goes [modifier] [modifier] [type] [name], starting from the right.  Typeless languages that only have
			// one word will have it correctly interpreted as the name.  Pascal-style languages that don't have a colon on this line because
			// they're sharing a type declaration will also have it correctly interpreted as the name.

			if (hasColon == false)
				{
				MarkCParameter(start, end, topicTypeID, words);
				}

			// If we do have a colon, the order of words goes [name]: [modifier] [modifier] [type], the type portion starting
			// from the right.
			else
				{
				iterator = start;

				while (iterator < end && iterator.PrototypeParsingType != PrototypeParsingType.NameTypeSeparator)
					{  iterator.Next();  }

				MarkPascalParameterBeforeColon(start, iterator, topicTypeID, wordsBeforeColon);

				while (iterator < end && iterator.PrototypeParsingType == PrototypeParsingType.NameTypeSeparator)
					{  iterator.Next();  }

				MarkPascalParameterAfterColon(iterator, end, topicTypeID, words - wordsBeforeColon);
				}
			}


		/* Function: CountParameterWords
		 * Returns the number of "words" between the bounds.
		 */
		protected int CountParameterWords (TokenIterator start, TokenIterator end, int topicTypeID)
			{
			TokenIterator iterator = start;
			int words = 0;

			while (iterator < end)
				{
				if (TryToSkipTypeOrVarName(ref iterator, end) ||
					 TryToSkipComment(ref iterator) ||
					 TryToSkipString(ref iterator) ||
					 TryToSkipBlock(ref iterator, true))
					{
					words++;
					}

				// Skip over whitespace plus any unexpected random symbols that appear.
				else
					{
					iterator.Next();
					}
				}

			return words;
			}


		/* Function: MarkCParameter
		 * Marks the tokens in the C-style parameter specified by the bounds with <CommentParsingTypes>.  This function will also
		 * work correctly for typeless parameters and Pascal-style parameters that don't have a type.  If you leave the word count
		 * -1 it will use <CountParameterWords()> to determine it itself.
		 */
		protected void MarkCParameter (TokenIterator start, TokenIterator end, int topicTypeID, int words = -1)
			{
			if (words == -1)
				{  words = CountParameterWords(start, end, topicTypeID);  }

			// The order of words goes [modifier] [modifier] [type] [name], starting from the right.  Typeless languages that only have
			// one word will have it correctly interpreted as the name.  Pascal-style languages that don't have a colon on this line because
			// they're sharing a type declaration will also have it correctly interpreted as the name.

			TokenIterator iterator = start;

			TokenIterator startWord = iterator;
			TokenIterator endWord = iterator;
			bool markWord = false;

			while (iterator < end)
				{
				startWord = iterator;
				markWord = false;

				if (iterator.PrototypeParsingType == PrototypeParsingType.DefaultValueSeparator ||
						iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
					{
					break;
					}
				else if (TryToSkipTypeOrVarName(ref iterator, end) ||
							 TryToSkipComment(ref iterator) ||
							 TryToSkipString(ref iterator) ||
							 TryToSkipBlock(ref iterator, true))
					{
					markWord = true;
					endWord = iterator;
					}
				else
					{
					iterator.Next();
					}

				if (markWord)
					{
					if (words >= 3)
						{  source.SetPrototypeParsingTypeBetween(startWord, endWord, PrototypeParsingType.TypeModifier);  }
					else if (words == 2)
						{  
						MarkType(startWord, endWord);  

						// Go back and change any trailing * or & to name prefixes because even if they're textually attached to the type
						// (int* x) they're actually part of the name in C++ (int *x).

						TokenIterator namePrefix = endWord;
						namePrefix.Previous();

						if (namePrefix >= startWord && (namePrefix.Character == '*' || namePrefix.Character == '&' || namePrefix.Character == '^'))
							{
							for (;;)
								{
								TokenIterator temp = namePrefix;
								temp.Previous();
								temp.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator, startWord);

								if (temp >= startWord && (temp.Character == '*' || temp.Character == '&' || temp.Character == '^'))
									{  namePrefix = temp;  }
								else
									{  break;  }
								}

							source.SetPrototypeParsingTypeBetween(namePrefix, endWord, PrototypeParsingType.NamePrefix_PartOfType);
							}
						}
					else if (words == 1)
						{  MarkName(startWord, endWord);  }

					words--;
					}
				}
			}


		/* Function: MarkPascalParameterBeforeColon
		 * Marks the tokens in the Pascal-style parameter specified by the bounds with <CommentParsingTypes>.  The bounds
		 * contain the part of the prototype prior to the colon.  If the word count is -1 it will determine it itself with 
		 * <CountParameterWords()>.
		 */
		protected void MarkPascalParameterBeforeColon (TokenIterator start, TokenIterator end, int topicTypeID, int words = -1)
			{
			if (words == -1)
				{  words = CountParameterWords(start, end, topicTypeID);  }

			TokenIterator iterator = start;
			TokenIterator startWord = iterator;

			// First word is the name no matter what.

			if (TryToSkipTypeOrVarName(ref iterator, end) ||
					TryToSkipComment(ref iterator) ||
					TryToSkipString(ref iterator) ||
					TryToSkipBlock(ref iterator, true))
				{  }
			else
				{  iterator.Next();  }

			TokenIterator endWord = iterator;
			MarkName(startWord, endWord);

			// Ignore everything else before the colon.
			}


		/* Function: MarkPascalParameterAfterColon
		 * Marks the tokens in the Pascal-style parameter specified by the bounds with <CommentParsingTypes>.  The bounds
		 * contain the part of the prototype after the colon.  If the word count is -1 it will determine it itself with
		 * <CountParameterWords()>.
		 */
		protected void MarkPascalParameterAfterColon (TokenIterator start, TokenIterator end, int topicTypeID, int words = -1)
			{
			if (words == -1)
				{  words = CountParameterWords(start, end, topicTypeID);  }

			TokenIterator iterator = start;
			TokenIterator startWord = iterator;
			TokenIterator endWord = iterator;

			// Mark words in the type section as [modifier] [modifier] [type].

			bool markWord = false;

			while (iterator < end)
				{
				startWord = iterator;
				markWord = false;

				if (iterator.PrototypeParsingType == PrototypeParsingType.DefaultValueSeparator ||
					 iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
					{
					break;
					}
				else if (TryToSkipTypeOrVarName(ref iterator, end) ||
							 TryToSkipComment(ref iterator) ||
							 TryToSkipString(ref iterator) ||
							 TryToSkipBlock(ref iterator, true))
					{
					markWord = true;
					endWord = iterator;
					}
				else
					{
					iterator.Next();
					}

				if (markWord)
					{
					if (words >= 2)
						{  source.SetPrototypeParsingTypeBetween(startWord, endWord, PrototypeParsingType.TypeModifier);  }
					else if (words == 1)
						{  MarkType(startWord, endWord);  }

					words--;
					}
				}
			}


		/* Function: MarkType
		 * Marks the passed stretch of tokens with <PrototypeParsingTypes> for variable types.
		 */
		protected void MarkType (TokenIterator start, TokenIterator end)
			{
			TokenIterator iterator = start;
			TokenIterator qualifierEnd = start;

			while (iterator < end && iterator.FundamentalType != FundamentalType.Text && iterator.Character != '_')
				{  iterator.Next();  }

			while (iterator < end)
				{
				if (iterator.Character == '.')
					{
					iterator.Next();
					qualifierEnd = iterator;
					}
				else if (iterator.MatchesAcrossTokens("::"))
					{
					iterator.Next(2);
					qualifierEnd = iterator;
					}
				else if (iterator.FundamentalType == FundamentalType.Text || iterator.Character == '_')
					{
					iterator.Next();
					}
				else
					{  break;  }
				}

			if (qualifierEnd > start)
				{  source.SetPrototypeParsingTypeBetween(start, qualifierEnd, PrototypeParsingType.TypeQualifier);  }
			if (iterator > qualifierEnd)
				{  source.SetPrototypeParsingTypeBetween(qualifierEnd, iterator, PrototypeParsingType.Type);  }
			if (iterator < end)
				{  MarkTypeSuffix(iterator, end);  }
			}


		/* Function: MarkTypeSuffix
		 * Marks the passed stretch of tokens with <PrototypeParsingTypes> for variable type suffixes.  Opening and closing
		 * brackets will be searched for nested types.
		 */
		protected void MarkTypeSuffix (TokenIterator start, TokenIterator end)
			{
			TokenIterator iterator = start;
			TokenIterator prevIterator;

			while (iterator < end)
				{
				prevIterator = iterator;

				if (TryToSkipBlock(ref iterator, true))
					{
					prevIterator.PrototypeParsingType = PrototypeParsingType.OpeningTypeSuffix;
					prevIterator.Next();

					iterator.Previous();
					iterator.PrototypeParsingType = PrototypeParsingType.ClosingTypeSuffix;

					MarkTypeSuffixParamList(prevIterator, iterator);

					iterator.Next();
					}
				else
					{
					iterator.PrototypeParsingType = PrototypeParsingType.TypeSuffix;
					iterator.Next();
					}
				}
			}


		/* Function: MarkTypeSuffixParamList
		 * Marks the passed stretch of tokens with <PrototypeParsingTypes> for parameter lists appearing in a variable type suffix.
		 * This is used for things like finding the classes in "List<ClassA, ClassB>".
		 */
		protected void MarkTypeSuffixParamList (TokenIterator start, TokenIterator end)
			{
			TokenIterator iterator = start;

			while (iterator < end)
				{
				TokenIterator startOfType = iterator;

				while (iterator < end && iterator.Character != ',' && iterator.Character != ';')
					{  
					if (TryToSkipTypeOrVarName(ref iterator, end) ||
						 TryToSkipComment(ref iterator) ||
						 TryToSkipString(ref iterator) ||
						 TryToSkipBlock(ref iterator, true))
						{  }
					else
						{  iterator.Next();  }
					}

				TokenIterator endOfType = iterator;

				endOfType.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfType);
				startOfType.NextPastWhitespace(endOfType);

				if (endOfType > startOfType)
					{  MarkTypeSuffixParam(startOfType, endOfType);  }

				iterator.Next();
				}
			}


		/* Function: MarkTypeSuffixParam
		 * Marks the passed stretch of tokens with <PrototypeParsingTypes> for a variable type suffix parameter.
		 */
		protected void MarkTypeSuffixParam (TokenIterator start, TokenIterator end)
			{
			// Pass 1: Count the number of "words" in the parameter.

			int words = 0;
			TokenIterator iterator = start;

			while (iterator < end)
				{
				if (TryToSkipTypeOrVarName(ref iterator, end) ||
					 TryToSkipComment(ref iterator) ||
					 TryToSkipString(ref iterator) ||
					 TryToSkipBlock(ref iterator, true))
					{
					// If there was a comment in the prototype, that means it specifically wasn't filtered out because it was something
					// significant like a Splint comment or /*out*/.  Treat it like a modifier.

					// Strings don't really make sense in the prototype until the default value, but we need the parser to handle it anyway
					// just so it doesn't lose its mind if one occurs.

					// If we come across a block that doesn't immediately follow an identifier, it may be something like a C# property so
					// treat it as a modifier.  

					words++;
					}

				// Skip over whitespace plus any unexpected random symbols that appear.
				else
					{
					iterator.Next();
					}
				}


			// Pass 2: Mark tokens.

			iterator = start;

			TokenIterator startWord = iterator;
			TokenIterator endWord = iterator;
			bool markWord = false;

			while (iterator < end)
				{
				startWord = iterator;
				markWord = false;

				if (TryToSkipTypeOrVarName(ref iterator, end) ||
					 TryToSkipComment(ref iterator) ||
					 TryToSkipString(ref iterator) ||
					 TryToSkipBlock(ref iterator, true))
					{
					markWord = true;
					endWord = iterator;
					}
				else
					{
					iterator.Next();
					}

				if (markWord)
					{
					if (words >= 2)
						{  source.SetPrototypeParsingTypeBetween(startWord, endWord, PrototypeParsingType.TypeModifier);  }
					else if (words == 1)
						{  MarkType(startWord, endWord);  }

					words--;
					}
				}
			}


		/* Function: MarkName
		 * Marks the passed stretch of tokens with <PrototypeParsingTypes> for variable names.
		 */
		protected void MarkName (TokenIterator start, TokenIterator end)
			{
			while (start < end && start.FundamentalType != FundamentalType.Text && start.Character != '_')
				{
				start.PrototypeParsingType = PrototypeParsingType.NamePrefix_PartOfType;
				start.Next();
				}

			while (start < end)
				{
				if (start.FundamentalType == FundamentalType.Text || start.Character == '_' || start.Character == '.')
					{
					start.PrototypeParsingType = PrototypeParsingType.Name;
					start.Next();
					}
				else if (start.MatchesAcrossTokens("::"))
					{
					start.SetPrototypeParsingTypeByCharacters(PrototypeParsingType.Name, 2);
					start.Next(2);
					}
				else
					{  break;  }
				}

			if (start < end)
				{
				source.SetPrototypeParsingTypeBetween(start, end, PrototypeParsingType.NameSuffix_PartOfType);
				}
			}



		// Group: General Parsing Support Functions
		// __________________________________________________________________________


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
		 * If the iterator is on a line comment symbol, moves the iterator past it, provides information about the comment, and 
		 * returns true.  It will not skip the line break after the comment since that may be relevant to the calling code.
		 */
		protected bool TryToSkipLineComment (ref TokenIterator iterator, out string commentSymbol)
			{
			if (language.LineCommentStrings == null)
				{
				commentSymbol = null;
				return false;
				}

			int commentSymbolIndex = iterator.MatchesAnyAcrossTokens(language.LineCommentStrings);

			if (commentSymbolIndex == -1)
				{
				commentSymbol = null;
				return false;
				}

			commentSymbol = language.LineCommentStrings[commentSymbolIndex];
			iterator.NextByCharacters(commentSymbol.Length);

			while (iterator.IsInBounds && iterator.FundamentalType != FundamentalType.LineBreak)
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
		 * If the iterator is on an opening block comment symbol, moves the iterator past it, provides information about the 
		 * comment, and returns true.
		 */
		protected bool TryToSkipBlockComment (ref TokenIterator iterator, out string openingSymbol, out string closingSymbol)
			{
			if (language.BlockCommentStringPairs == null)
				{
				openingSymbol = null;
				closingSymbol = null;
				return false;
				}

			int openingCommentSymbolIndex = iterator.MatchesAnyPairAcrossTokens(language.BlockCommentStringPairs);

			if (openingCommentSymbolIndex == -1)
				{
				openingSymbol = null;
				closingSymbol = null;
				return false;
				}

			openingSymbol = language.BlockCommentStringPairs[openingCommentSymbolIndex];
			closingSymbol = language.BlockCommentStringPairs[openingCommentSymbolIndex + 1];
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
		 * If the iterator is on a quote or apostrophe, moves the iterator past the entire string and returns true.
		 */
		protected bool TryToSkipString (ref TokenIterator iterator)
			{
			if (iterator.Character != '"' && iterator.Character != '\'')
				{  return false;  }

			char quoteCharacter = iterator.Character;
			iterator.Next();

			while (iterator.IsInBounds)
				{
				if (iterator.Character == quoteCharacter)
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


		/* Function: TryToSkipBlock
		 * If the iterator is on an opening symbol, moves it past the entire block and returns true.  This takes care of
		 * nested blocks, strings, and comments, but otherwise doesn't parse the underlying code.  You must specify
		 * whether to include < as an opening symbol because it may be relevant in some places (template definitions)
		 * but detrimental in others (general code where < could mean less than and not have a closing >.)
		 */
		 protected bool TryToSkipBlock (ref TokenIterator iterator, bool includeAngleBrackets)
			{
			if (iterator.Character != '(' && iterator.Character != '[' && iterator.Character != '{' &&
				 (iterator.Character != '<' || includeAngleBrackets == false) )
				{  return false;  }

			SafeStack<char> symbols = new SafeStack<char>();
			symbols.Push(iterator.Character);
			iterator.Next();

			while (iterator.IsInBounds)
				{
				if (iterator.Character == '(' || iterator.Character == '[' || iterator.Character == '{' ||
					 (iterator.Character == '<' && includeAngleBrackets) )
					{
					symbols.Push(iterator.Character);
					iterator.Next();
					}
				else if ( (iterator.Character == ')' && symbols.Peek() == '(') ||
							  (iterator.Character == ']' && symbols.Peek() == '[') ||
							  (iterator.Character == '}' && symbols.Peek() == '{') ||
							  (iterator.Character == '>' && symbols.Peek() == '<') )
					{
					symbols.Pop();
					iterator.Next();

					if (symbols.Count == 0)
						{  break;  }
					}
				else if (TryToSkipString(ref iterator) ||
							 TryToSkipComment(ref iterator))
					{  }
				else
					{  iterator.Next();  }
				}

			return true;
			}


		/* Function: TryToSkipTypeOrVarName
		 * 
		 * If the iterator is on what could be a complex type or variable name, moves the iterator past it and returns true.
		 * This supports things like name, $name, PkgA::Class*, int[], and List<List<void*, float>>.  It does not include anything
		 * separated by a space, so modifiers like unsigned and const have to be handled separately.
		 * 
		 * A limit is required since this will swallow a block following an identifier and that may not be desired or expected.  If you
		 * genuinely don't need a limit, set it to <Tokenizer.LastToken>.
		 */
		protected bool TryToSkipTypeOrVarName (ref TokenIterator iterator, TokenIterator limit)
			{
			if (iterator < limit &&
				 (iterator.FundamentalType == FundamentalType.Text ||
				  iterator.Character == '_' || iterator.Character == '*' || iterator.Character == '&' ||
				  iterator.Character == '$' || iterator.Character == '@' || iterator.Character == '%') )
				{
				iterator.Next();

				while (iterator < limit)
					{
					// Add dot to our previous list.  Also ^ for Pascal pointers and ? for C# nullable types.
					if (iterator.FundamentalType == FundamentalType.Text || iterator.Character == '.' ||
						 iterator.Character == '_' || iterator.Character == '*' || iterator.Character == '&' || iterator.Character == '^' ||
						 iterator.Character == '$' || iterator.Character == '@' || iterator.Character == '%' ||
						 iterator.Character == '^' || iterator.Character == '?')
						{  iterator.Next();  }

					else if (iterator.MatchesAcrossTokens("::"))
						{  iterator.Next(2);  }

					else if (iterator.Character == '<')
						{
						TokenIterator temp = iterator;
						temp.Previous();
						temp.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator);

						if (temp.MatchesToken("operator") || TryToSkipBlock(ref iterator, true) == false)
							{  
							do
								{  iterator.Next();  }
							while (iterator.Character == '<');
							}
						}

					// Handle array or template brackets
					else if (TryToSkipBlock(ref iterator, false))
						{  }

					// Catch freestanding symbols and consts like "int * x" and "int* const x".  However, cut off after the symbol so we don't 
					// include the x in "int *x".
					else if (iterator.FundamentalType == FundamentalType.Whitespace)
						{
						TokenIterator lookahead = iterator;
						lookahead.NextPastWhitespace();
						bool acceptableSuffix;

						while (lookahead < limit)
							{
							acceptableSuffix = false;

							if (lookahead.Character == '*' || lookahead.Character == '&' || lookahead.Character == '^')
								{
								lookahead.Next();
								acceptableSuffix = true;
								}
							else if (lookahead.MatchesToken("const"))
								{
								lookahead.Next();
								if (lookahead.Character != '_')
									{  acceptableSuffix = true;  }
								}

							if (acceptableSuffix)
								{
								iterator = lookahead;
								lookahead.NextPastWhitespace();
								}
							else
								{  break;  }
							}

						break;
						}

					else
						{  break;  }
					}

				return true;
				}
			else
				{  return false;  }
			}


		// Group: Other Support Functions
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
		protected PossibleDocumentationComment TryToGetPDBlockComment (LineIterator lineIterator, 
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
				if (Cancelled || !lineIterator.IsInBounds)
					{  return null;  }
					
				TokenIterator closingSymbolIterator;
				
				if (lineIterator.FindAcrossTokens(closingSymbol, false, LineBoundsMode.Everything, out closingSymbolIterator) == true)
					{
					closingSymbolIterator.NextByCharacters(closingSymbol.Length);

					closingSymbolIterator.NextPastWhitespace();

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
		 * <PossibleDocumentationComment> and mark the symbols as <CommentParsingType.CommentSymbol>.  Returns 
		 * null otherwise.
		 * 
		 * This function takes a separate comment symbol for the first line and all remaining lines, allowing you to detect
		 * Javadoc line comments that start with ## and the remaining lines use #.  Both symbols can be the same if this isn't
		 * required.  If openingMustBeAlone is set, no symbol can appear immediately after the first line symbol for this
		 * function to succeed.  This allows you to specifically detect something like ## without also matching #######.
		 */
		protected PossibleDocumentationComment TryToGetPDLineComment (LineIterator lineIterator, 
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
				if (Cancelled)
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
		 * Applies syntax highlighting based on the passed keywords with the assumption that there's no unusual rules for 
		 * comments or strings, and there's nothing like unquoted regular expressions to confuse a simple parser.  If no
		 * keywords are passed it uses <defaultKeywords>.
		 */
		protected void SimpleSyntaxHighlight (StringSet keywords = null)
			{
			if (keywords == null)
				{  keywords = defaultKeywords;  }

			TokenIterator iterator = source.FirstToken;
			
			while (iterator.IsInBounds)
				{
				TokenIterator originalPosition = iterator;

				if (TryToSkipComment(ref iterator))
					{
					source.SetSyntaxHighlightingTypeBetween(originalPosition, iterator, SyntaxHighlightingType.Comment);
					}
				else if (TryToSkipString(ref iterator))
					{
					source.SetSyntaxHighlightingTypeBetween(originalPosition, iterator, SyntaxHighlightingType.String);
					}
				else if (iterator.FundamentalType == FundamentalType.Text || iterator.Character == '_')
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
						TokenIterator endOfIdentifier = iterator;
						
						do
							{  endOfIdentifier.Next();  }
						while (endOfIdentifier.FundamentalType == FundamentalType.Text ||
									endOfIdentifier.Character == '_');

						string identifier = source.TextBetween(iterator, endOfIdentifier);

						if (keywords.Contains(identifier))
							{  iterator.SetSyntaxHighlightingTypeByCharacters(SyntaxHighlightingType.Keyword, identifier.Length);  }

						iterator = endOfIdentifier;
						}
					}
				else
					{  iterator.Next();  }
				}
			}


			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Cancelled
		 * Whether parsing should be interrupted midway.
		 */
		public bool Cancelled
			{
			get
				{  return cancelDelegate();  }
			}


		/* Property: Language
		 * The <Language> associated with this parser.
		 */
		public Language Language
			{
			get
				{  return language;  }
			}
			


		// Group: Variables
		// __________________________________________________________________________
		

		/* var: language
		 * The <Language> object associated with this parser.
		 */
		protected Language language;

		/* var: source
		 * The source code in a <Tokenizer>.
		 */
		protected Tokenizer source;
			
		/* var: cancelDelegate
		 * The delegate that controls whether parsing should be interrupted midway.  Use <Delegates.NeverCancel>l if it's not 
		 * necessary.
		 */
		protected CancelDelegate cancelDelegate;
			
		/* var: possibleDocumentationComments
		 * A list of <PossibleDocumentationComment>s retrieved from <source>.
		 */
		protected List<PossibleDocumentationComment> possibleDocumentationComments;
		
		/* var: commentTopics
		 * A list of <Topics> generated by the source file's comments.
		 */
		protected List<Topic> commentTopics;
		
		/* var: codeTopics
		 * A list of <Topics> generated by the source file's code.
		 */
		protected List<Topic> codeTopics;
		
		/* var: mergedTopics
		 * A list of <Topics> created by merging <CommentTopics> and <CodeTopics>.
		 */
		protected List<Topic> mergedTopics;
		


		// Group: Static Variables
		// __________________________________________________________________________

		/* var: defaultKeywords
		 * A set of the default keywords for basic language support across all languages.
		 */
		static protected StringSet defaultKeywords = new StringSet(false, false, new string[] {

			// This isn't comprehensive but should cover most languages.

			"int", "int8", "int16", "int32", "int64", "uint", "uint8", "uint16", "uint32", "uint64",
			"signed", "unsigned", "integer", "long", "ulong", "short", "ushort", "real", "float", "double", "decimal",
			"float32", "float64", "float80", "void", "char", "string", "wchar", "wchar_t", "byte", "ubyte", "sbyte", 
			"bool", "boolean", "true", "false", "null", "undefined", "var",

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


		static protected Regex.Comments.AcceptablePrototypeComments acceptablePrototypeCommentRegex
			= new Regex.Comments.AcceptablePrototypeComments();

		}
	}