/* 
 * Class: GregValure.NaturalDocs.Engine.Languages.Language
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Comments;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Tokenization;
using GregValure.NaturalDocs.Engine.Topics;
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



		// Group: Public Parsing Functions
		// __________________________________________________________________________
	

		/* Function: Parse
		 * Parses the tokenized source code and returns it as a list of <Topics> and class parent <Links>.  Both of these will be empty 
		 * but not null if there weren't any.  Set cancelDelegate for the ability to interrupt parsing, or use <Delegates.NeverCancel>.
		 * 
		 * If you already have the source code in tokenized form it would be more efficient to pass it as a <Tokenizer>.
		 */
		public ParseResult Parse (string source, int fileID, CancelDelegate cancelDelegate, 
													 out IList<Topic> topics, out LinkSet classParentLinks)
			{
			return Parse(new Tokenizer(source), fileID, cancelDelegate, out topics, out classParentLinks);
			}
			
			
		/* Function: Parse
		 * Parses the tokenized source code and returns it as a list of <Topics> and class parent <Links>.  Both of these will be empty 
		 * but not null if there weren't any.  Set cancelDelegate for the ability to interrupt parsing, or use <Delegates.NeverCancel>.
		 */
		virtual public ParseResult Parse (Tokenizer source, int fileID, CancelDelegate cancelDelegate, 
																  out IList<Topic> topics, out LinkSet classParentLinks)
			{
			if (Type == LanguageType.Container)
				{
				// xxx not handled yet
				topics = new List<Topic>();
				classParentLinks = new LinkSet();  
				return ParseResult.Success;  
				}

			topics = null;
			classParentLinks = null;


			// Find all the comments that could have documentation.

			IList<PossibleDocumentationComment> possibleDocumentationComments = GetPossibleDocumentationComments(source);
			
			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }
			

			// Extract Topics from them.  This could include Javadoc, XML, and headerless Natural Docs comments.
				
			IList<Topic> commentTopics = GetCommentTopics(possibleDocumentationComments);
			
			foreach (Topic commentTopic in commentTopics)
			   {
			   commentTopic.FileID = fileID;
			   commentTopic.LanguageID = this.ID;
			   }

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// Scan the comments for "Prototype:" headings, applying them as the prototypes and removing them from the body.

			ApplyCommentPrototypes(commentTopics);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// For basic language support, fill in additional prototypes via our language-neutral algorithm.

			if (Type == LanguageType.BasicSupport)
				{
				AddBasicPrototypes(source, commentTopics, possibleDocumentationComments);

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }
				}


			// Convert our Topic list to CodePoints.  Topics only store line numbers, so we'll move a LineIterator to each one to
			// get the character position CodePoint needs.

			List<CodePoint> commentCodePoints = new List<CodePoint>(commentTopics.Count);
			LineIterator lineIterator = source.FirstLine;

			foreach (Topic commentTopic in commentTopics)
				{
				lineIterator.Next(commentTopic.CommentLineNumber - lineIterator.LineNumber);

				CodePoint commentCodePoint = new CodePoint(lineIterator, CodePoint.Flags.InComments);
				commentCodePoint.Topic = commentTopic;

				commentCodePoints.Add(commentCodePoint);
				}

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// Generate topic symbols and apply context settings according to topic scoping rules.  This only applies to Natural Docs
			// topics with headers as Javadoc, MS XML, and headerless Natural Docs comments don't have titles to generate symbols
			// from nor topic types to get scoping information from.  We will remove them later if they aren't combined with any code
			// topics which do have these things.

			GenerateCommentContextAndSymbols(commentCodePoints);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// xxx apply comment class parents


			// For full language support, get code points from the source.

			IList<CodePoint> sourceCodePoints;

			if (Type == LanguageType.FullSupport)
				{
				sourceCodePoints = GetSourceCodePoints(source);

				foreach (CodePoint sourceCodePoint in sourceCodePoints)
					{
					if (sourceCodePoint.Topic != null)
						{  sourceCodePoint.Topic.FileID = fileID;  }  
					}

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }
				}
			else
				{  sourceCodePoints = new List<CodePoint>();  }


			// Merge the source and comment code points into one list.  We're not combining topics yet.

			List<CodePoint> codePoints = new List<CodePoint>(commentCodePoints.Count + sourceCodePoints.Count);

			int sourceCPIndex = 0;
			int commentCPIndex = 0;

			for (;;)
				{
				if (commentCPIndex < commentCodePoints.Count)
					{
					if (sourceCPIndex < sourceCodePoints.Count &&
						 sourceCodePoints[sourceCPIndex].CharOffset < commentCodePoints[commentCPIndex].CharOffset)
						{
						codePoints.Add(sourceCodePoints[sourceCPIndex]);
						sourceCPIndex++;
						}
					else
						{
						codePoints.Add(commentCodePoints[commentCPIndex]);
						commentCPIndex++;
						}
					}
				else if (sourceCPIndex < sourceCodePoints.Count)
					{
					codePoints.Add(sourceCodePoints[sourceCPIndex]);
					sourceCPIndex++;
					}
				else
					{  break;  }
				}


			// xxx apply class/group tags/access levels to children


			// xxx combine topics


			// Extract the topics and parent links from the code points, and filter out any headerless topics.  This removes Javadoc, 
			// MS XML, and headerless Natural Docs comments that weren't combined with a source topic.

			List<Topic> finalTopics = new List<Topic>();
			LinkSet finalClassParentLinks = new LinkSet();

			foreach (CodePoint codePoint in codePoints)
				{
				if (codePoint.Topic != null && codePoint.Topic.Title != null)
					{  finalTopics.Add(codePoint.Topic);  }

				if (codePoint.ClassParentLinks != null)
					{
					foreach (Link link in codePoint.ClassParentLinks)
						{  finalClassParentLinks.Add(link);  }
					}
				}


			// Sanity check the generated topics.

			#if DEBUG
			foreach (Topic topic in finalTopics)
				{
				string missingProperties = "";

				if (topic.FileID == 0)
					{  missingProperties += " FileID";  }
				if (topic.LanguageID == 0)
					{  missingProperties += " LanguageID";  }
				if (topic.Symbol == null)
					{  missingProperties += " Symbol";  }
				if (topic.Title == null)
					{  missingProperties += " Title";  }
				if (topic.TopicTypeID == 0)
					{  missingProperties += " TopicTypeID";  }

				if (missingProperties != "")
					{  throw new Exception("Generated Topic is missing properties:" + missingProperties);  }
				}
			#endif


			topics = finalTopics;
			classParentLinks = finalClassParentLinks;

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
		public IList<PossibleDocumentationComment> GetComments (string source)
			{
			return GetComments(new Tokenizer(source));
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
		public IList<PossibleDocumentationComment> GetComments (Tokenizer source)
			{
			if (Type == LanguageType.Container)
				{  return new List<PossibleDocumentationComment>();  }  //xxx

			IList<PossibleDocumentationComment> possibleDocumentationComments = GetPossibleDocumentationComments(source);
			var lineFinder = Engine.Instance.Comments.LineFinder;

			foreach (PossibleDocumentationComment comment in possibleDocumentationComments)
				{  lineFinder.MarkTextBoxes(comment);  }

			return possibleDocumentationComments;
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */
		public virtual ParsedPrototype ParsePrototype (string stringPrototype, int topicTypeID)
			{
			if (Type == LanguageType.Container)
				{  throw new NotImplementedException();  }  //xxx

			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype);
			ParsedPrototype parsedPrototype = new ParsedPrototype(tokenizedPrototype);


			// Search for the first opening bracket or brace.  Also be on the lookout for anything that would indicate this is a
			// class prototype.

			TokenIterator iterator = tokenizedPrototype.FirstToken;
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
					else if (TryToSkipComment(ref iterator) || 
								 TryToSkipString(ref iterator) ||
								 TryToSkipBlock(ref iterator, true))
						{  }

					else
						{  iterator.Next();  }
					}
				

				// If we have any, parse the parameters.

				// We use ParsedPrototype.GetParameter() instead of trying to build it into the loop above because ParsedPrototype 
				// does things like trimming whitespace and ignoring empty parentheses.

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

			return parsedPrototype;
			}


		/* Function: SyntaxHighlight
		 * Applies <SyntaxHighlightingTypes> to the tokenized content.
		 */
		public virtual void SyntaxHighlight (Tokenizer source)
			{
			if (Type == LanguageType.Container)
				{  return;  }  //xxx

			SimpleSyntaxHighlight(source);
			}


		/* Function: SyntaxHighlight
		 * Applies <SyntaxHighlightingTypes> to the <ParsedPrototype>.
		 */
		public void SyntaxHighlight (ParsedPrototype prototype)
			{
			SyntaxHighlight(prototype.Tokenizer);
			}


		/* Function: IsBuiltInType
		 * Returns whether the type string is a built-in type such as "int" as opposed to a user-defined type.
		 */
		public bool IsBuiltInType (string type)
			{
			return defaultKeywords.Contains(type);
			}


		/* Function: IsBuiltInType
		 * Returns whether the text between the iterators is a built-in type such as "int" as opposed to a user-defined 
		 * type.
		 */
		public bool IsBuiltInType (TokenIterator start, TokenIterator end)
			{
			return IsBuiltInType( start.Tokenizer.TextBetween(start, end) );
			}


		/* Function: IsSameCodeElement
		 * Returns whether the two topics represent the same code element.  For example, the same function appearing
		 * in C++ header and source files, or the same C# class defined across multiple files with the "partial" keyword.
		 */
		public bool IsSameCodeElement (Topic topicA, Topic topicB)
			{
			if (topicA.LanguageID != topicB.LanguageID)
				{  return false;  }

			#if DEBUG
			if (topicA.LanguageID != this.ID)
				{  throw new Exception("Tried to call IsSameCodeElement() using a language object that neither topic uses.");  }
			#endif

			if (topicA.Symbol != topicB.Symbol)
				{  return false;  }

			// This is a problem if one uses "constructor" and one uses "function" and they don't map to the same topic type.
			if (topicA.TopicTypeID != topicB.TopicTypeID)
				{  return false;  }

			// So now we have two topics of the same language, symbol, and type.  Now the assumption is they're the same
			// unless they're distinguished by parameters.
			return (topicA.PrototypeParameters == topicB.PrototypeParameters);
			}


			
		// Group: Overridable Parsing Stages
		// Override these stages in subclasses as necessary.
		// __________________________________________________________________________
		
			
		// Function: GetPossibleDocumentationComments
		// 
		// Goes through the file looking for comments that could possibly contain documentation and returns them as a list of
		// <PossibleDocumentationComments>.  These comments are not guaranteed to have documentation in them, just to be
		// acceptable candidates for them.  If there are no comments, it will return an empty list.
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
		virtual protected IList<PossibleDocumentationComment> GetPossibleDocumentationComments (Tokenizer source)
			{
			List<PossibleDocumentationComment> possibleDocumentationComments = new List<PossibleDocumentationComment>();

			if (Type == LanguageType.TextFile)
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
							comment = TryToGetPDBlockComment(lineIterator, JavadocBlockCommentStringPairs[i], 
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
							comment = TryToGetPDBlockComment(lineIterator, BlockCommentStringPairs[i], 
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
							comment = TryToGetPDLineComment(lineIterator, XMLLineCommentStrings[i],
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
							javadocComment = TryToGetPDLineComment(lineIterator, JavadocLineCommentStringPairs[i],
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
							comment = TryToGetPDLineComment(lineIterator, LineCommentStrings[i], LineCommentStrings[i], false);
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

			return possibleDocumentationComments;
			}

			
		/* Function: GetCommentTopics
		 * 
		 * Finds and parses all <Topics> in the <PossibleDocumentationComments>.  If there are none, it will return an empty list.
		 * 
		 * The default implementation sends each <PossibleDocumentationComment> to <Comments.Manager.Parse()>.  There
		 * should be no need to change it.
		 */
		protected virtual IList<Topic> GetCommentTopics (IList<PossibleDocumentationComment> possibleDocumentationComments)
			{
			List<Topic> commentTopics = new List<Topic>();
				
			foreach (PossibleDocumentationComment comment in possibleDocumentationComments)
				{
				Engine.Instance.Comments.Parse(comment, commentTopics);
				}

			return commentTopics;
			}


		/* Function: GetSourceCodePoints
		 * 
		 * Goes through the file looking for code elements that should be included in the output and returns a list of <CodePoints>.
		 * If there are none, it will return an empty list.
		 *
		 * This will only be called for languages with full support.  The default implementation throws an exception since all classes
		 * implementing full support must override this function.
		 */
		protected virtual IList<CodePoint> GetSourceCodePoints (Tokenizer source)
			{
			throw new NotImplementedException();
			}


		/* Function: AddBasicPrototypes
		 * 
		 * Adds prototypes to the <Topics> for languages with basic support.  It examines the code between the end of each
		 * comment topic and the next one (or the next <PossibleDocumentationComment>) and if it finds the topic title before it 
		 * finds one of the language's prototype enders the prototype will be set to the code between the topic and the ender.
		 *
		 * This function does the basic looping of the search but throws the individual prototype detection to <AddBasicPrototype()>,
		 * so languages with basic support can just override that to implement tweaks instead.
		 * 
		 * This function will not apply a prototype to a <Topic> that already has one.
		 */
		protected virtual void AddBasicPrototypes (Tokenizer source, IList<Topic> commentTopics, 
																					 IList<PossibleDocumentationComment> possibleDocumentationComments)
			{
			int topicIndex = 0;

			for (int commentIndex = 0; commentIndex < possibleDocumentationComments.Count; commentIndex++)
				{
				PossibleDocumentationComment comment = possibleDocumentationComments[commentIndex];

				// Advance the topic index to the last one before the end of this comment.  If there are multiple topics in a 
				// comment only the last one gets a prototype search.

				while (topicIndex + 1 < commentTopics.Count && 
							commentTopics[topicIndex + 1].CommentLineNumber < comment.End.LineNumber)
					{  topicIndex++;  }

				// Now back up past any embedded topics.  We don't want the last embedded topic to get the prototype
				// instead of the parent topic.

				while (topicIndex < commentTopics.Count && commentTopics[topicIndex].IsEmbedded && 
							topicIndex > 0 && commentTopics[topicIndex - 1].CommentLineNumber >= comment.Start.LineNumber)
					{  topicIndex--;  }

				if (topicIndex >= commentTopics.Count ||
					 commentTopics[topicIndex].CommentLineNumber < comment.Start.LineNumber ||
					 commentTopics[topicIndex].CommentLineNumber > comment.End.LineNumber)
					{  
					// We're out of topics or the one we're on isn't in this comment.
					continue;  
					}

				// If it already has a prototype, probably from one embedded in a comment, don't search for a new one.

				if (commentTopics[topicIndex].Prototype != null)
					{  continue;  }

				// Build the bounds for the prototype search and perform it.

				Tokenization.LineIterator startCode = comment.End;
				Tokenization.LineIterator endCode;

				if (commentIndex + 1 < possibleDocumentationComments.Count)
					{  endCode = possibleDocumentationComments[commentIndex + 1].Start;  }
				else
					{  endCode = source.LastLine;  }

				AddBasicPrototype(commentTopics[topicIndex], startCode, endCode);
				}
			}


		/* Function: AddBasicPrototype
		 * Attempts to find a prototype for the passed <Topic> between the iterators.  If one is found, it will be normalized and put in
		 * <Topic.Prototoype>.
		 */
		protected virtual void AddBasicPrototype (Topic topic, LineIterator startCode, LineIterator endCode)
			{
			PrototypeEnders prototypeEnders = GetPrototypeEnders(topic.TopicTypeID);

			if (prototypeEnders == null)
				{  return;  }

			TokenIterator start = startCode.FirstToken(LineBoundsMode.ExcludeWhitespace);
			TokenIterator iterator = start;
			TokenIterator limit = endCode.FirstToken(LineBoundsMode.ExcludeWhitespace);

			bool lineHasExtender = false;
			bool goodPrototype = false;

			while (iterator < limit)
				{

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
						iterator.Next();
						lineHasExtender = false;
						}
					}


				// Line Extender

				else if (LineExtender != null && iterator.MatchesAcrossTokens(LineExtender))
					{
					// If the line extender is an underscore we don't want to treat it as one if it's adjacent to any text because
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

					if (!partOfIdentifier)
						{  lineHasExtender = true;  }

					iterator.Next();
					}


				// Ender Symbol, not in a bracket

				// We test this before looking for opening brackets so the opening symbols can be used as enders.
				else if (prototypeEnders.Symbols != null && iterator.MatchesAnyAcrossTokens(prototypeEnders.Symbols) != -1)
					{
					goodPrototype = true;
					break;
					}

				
				// Comments, Strings, and Brackets

				// We test comments before brackets in case the opening symbols are used for comments.
				// We don't include < when skipping brackets because there might be an unbalanced pair as part of an operator overload.
				else if (TryToSkipComment(ref iterator) ||
							 TryToSkipString(ref iterator) ||
							 TryToSkipBlock(ref iterator, false))
					{
					}


				// Everything Else

				else
					{  iterator.Next();  }
				}

			// If the iterator ran past the limit, that means something like a string or a block comment was not closed before it
			// reached the limit.  Consider the prototype bad.
			if (iterator > limit)
				{  goodPrototype = false;  }

			if (goodPrototype)
				{
				iterator.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

				string undecoratedTitle, parentheses;
				Symbols.ParameterString.SplitFromEndingParentheses(topic.Title, out undecoratedTitle, out parentheses);

				if (start.Tokenizer.ContainsTextBetween(undecoratedTitle, true, start, iterator))
					{  
					// goodPrototype remains true.
					}

				// If "operator" is in the string, make another attempt to see if we can match "operator +" with "operator+".
				else if (undecoratedTitle.IndexOf("operator", StringComparison.CurrentCultureIgnoreCase) != -1)
					{
					string undecoratedTitleOp = extraOperatorWhitespaceRegex.Replace(undecoratedTitle, "");
					string prototypeString = start.Tokenizer.TextBetween(start, iterator);
					string prototypeStringOp = extraOperatorWhitespaceRegex.Replace(prototypeString, "");

					if (prototypeStringOp.IndexOf(undecoratedTitleOp, StringComparison.CurrentCultureIgnoreCase) == -1)
						{  goodPrototype = false;  }
					}

				// If ".prototype." is in the string, make another attempt to see if we can match with it removed.
				else if (start.Tokenizer.ContainsTextBetween(".prototype.", true, start, iterator))
					{
					string prototypeString = start.Tokenizer.TextBetween(start, iterator);
					int prototypeIndex = prototypeString.IndexOf(".prototype.", StringComparison.CurrentCultureIgnoreCase);

					// We want to keep the trailing period so String.prototype.Function becomes String.Function.
					prototypeString = prototypeString.Substring(0, prototypeIndex) + prototypeString.Substring(prototypeIndex + 10);

					if (prototypeString.IndexOf(undecoratedTitle, StringComparison.CurrentCultureIgnoreCase) == -1)
						{  goodPrototype = false;  }
					}

				else
					{  goodPrototype = false;  }
				}

			if (goodPrototype)
				{
				Tokenizer prototype = start.Tokenizer.CreateFromIterators(start, iterator);
				topic.Prototype = NormalizePrototype(prototype);
				}
			}
			
		
		/* Function: NormalizePrototype
		 * 
		 * Puts the passed prototype in a form that's appropriate for the rest of the program.  It assumes the syntax is valid.  If
		 * you already have the input in a <Tokenizer>, it is more efficient to call <NormalizePrototype(tokenizer)>.
		 * 
		 * - Whitespace will be condensed.
		 * - Most comments will be removed, excluding things like Splint comments.
		 * - Line breaks will be removed, including extension characters if the language has them.
		 */
		protected virtual string NormalizePrototype (string input)
			{
			return NormalizePrototype(new Tokenizer(input));
			}


		/* Function: NormalizePrototype
		 * 
		 * Puts the passed prototype in a form that's appropriate for the rest of the program.  It assumes the syntax is valid.
		 * 
		 * - Whitespace will be condensed.
		 * - Most comments will be removed, excluding things like Splint comments.
		 * - Line breaks will be removed, including extension characters if the language has them.
		 */
		protected virtual string NormalizePrototype (Tokenizer input)
			{
			StringBuilder output = new StringBuilder(input.RawText.Length);

			TokenIterator start = input.FirstToken;
			TokenIterator iterator = start;
			TokenIterator end = input.LastToken;

			bool lastWasWhitespace = true;
			string openingSymbol, closingSymbol;

			while (iterator < end)
				{
				TokenIterator originalIterator = iterator;


				// Line Break

				if (iterator.FundamentalType == FundamentalType.LineBreak)
					{
					if (lastWasWhitespace == false)
						{
						output.Append(' ');
						lastWasWhitespace = true;
						}

					iterator.Next();
					}


				// Line Extender

				else if (LineExtender != null && iterator.MatchesAcrossTokens(LineExtender))
					{
					// If the line extender is an underscore we don't want to treat it as one if it's adjacent to any text because
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
						iterator.AppendTokenTo(output);
						iterator.Next();
						lastWasWhitespace = false;
						}
					else
						{
						// We don't want it in the output so treat it like whitespace
						if (lastWasWhitespace == false)
							{
							output.Append(' ');
							lastWasWhitespace = true;
							}

						iterator.Next();
						}
					}


				// Line Comment

				// We test this before looking for opening brackets in case the opening symbols are used for comments.
				else if (TryToSkipLineComment(ref iterator))
					{
					// Treat it as whitespace.  We're only dealing with Splint for block comments.

					if (lastWasWhitespace == false)
						{
						output.Append(' ');
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
					if (input.MatchTextBetween(acceptablePrototypeCommentRegex, 
																	  commentContentStart, commentContentEnd).Success)
						{
						output.Append(openingSymbol);

						string commentContent = input.TextBetween(commentContentStart, commentContentEnd);
						commentContent = commentContent.Replace('\r', ' ');
						commentContent = commentContent.Replace('\n', ' ');
						commentContent = commentContent.CondenseWhitespace();

						output.Append(commentContent);
						output.Append(closingSymbol);
						lastWasWhitespace = false;
						}
					else
						{
						if (lastWasWhitespace == false)
							{
							output.Append(' ');
							lastWasWhitespace = true;
							}
						}
					}


				// Strings

				else if (TryToSkipString(ref iterator))
					{
					// This also avoids whitespace condensation while in a string.

					input.AppendTextBetweenTo(originalIterator, iterator, output);
					lastWasWhitespace = false;
					}


				// Whitespace

				else if (iterator.FundamentalType == FundamentalType.Whitespace)
					{
					if (lastWasWhitespace == false)
						{
						output.Append(' ');
						lastWasWhitespace = true;
						}

					iterator.Next();
					}


				// Everything Else

				else
					{
					iterator.AppendTokenTo(output);
					lastWasWhitespace = false;
					iterator.Next();  
					}
				}

			// Strip trailing space
			if (lastWasWhitespace && output.Length > 0)
				{  output.Remove(output.Length - 1, 1);  }

			return output.ToString();
			}


		/* Function: ApplyCommentPrototypes
		 * Goes through the <Topics> and looks for prototype code blocks.  If it finds any, it removes them from the body and
		 * sets them as the topic prototype.
		 */
		protected virtual void ApplyCommentPrototypes (IList<Topic> topics)
			{
			StringBuilder stringBuilder = null;
			int replaceEmbeddedLanguageID = 0;

			foreach (Topic topic in topics)
				{
				// If a topic changed its language ID by having a comment prototype, make sure it applies to any embedded topics
				// that immediately follow it as well.
				if (topic.IsEmbedded)
					{
					if (replaceEmbeddedLanguageID != 0)
						{  topic.LanguageID = replaceEmbeddedLanguageID;  }
					}
				else
					{  replaceEmbeddedLanguageID = 0;  }

				if (topic.Body == null)
					{  continue;  }

				int prototypeStartIndex = topic.Body.IndexOf("<pre type=\"prototype\"");

				if (prototypeStartIndex == -1)
					{  continue;  }

				NDMarkup.Iterator iterator = new NDMarkup.Iterator(topic.Body, prototypeStartIndex);
				string prototypeLanguage = iterator.Property("language");

				if (stringBuilder == null)
					{  stringBuilder = new StringBuilder();  }
				else
					{  stringBuilder.Remove(0, stringBuilder.Length);  }

				for (;;)
					{
					iterator.Next();

					// Since we can assume NDMarkup is valid, we can assume we'll hit another pre tag before the end of the body
					// and that it will be the closing one.
					if (iterator.Type == NDMarkup.Iterator.ElementType.PreTag)
						{  break;  }
					else if (iterator.Type == NDMarkup.Iterator.ElementType.PreLineBreakTag)
						{  stringBuilder.Append('\n');  }
					else
						{  iterator.EntityDecodeAndAppendTo(stringBuilder);  }
					}

				iterator.Next();  // Past closing pre tag
				int prototypeEndIndex = iterator.RawTextIndex;


				// If the prototype specified a language, use it to replace the topic one.  This allows you to do things like document
				// SQL in text files.

				if (prototypeLanguage != null)
					{  
					Language languageObject = Instance.Languages.FromName(prototypeLanguage);

					if (languageObject != null)
						{  
						topic.LanguageID = languageObject.ID;
						replaceEmbeddedLanguageID = languageObject.ID;
						}
					}


				// Normalize and apply.

				string prototypeString = stringBuilder.ToString();

				if (topic.LanguageID == this.ID)
					{  prototypeString = NormalizePrototype(prototypeString);  }
				else
					{  prototypeString = Instance.Languages.FromID(topic.LanguageID).NormalizePrototype(prototypeString);  }

				topic.Prototype = prototypeString;


				// Check if there's a header immediately before it.

				int headingStartIndex = -1;
				int headingEndIndex = -1;

				if (prototypeStartIndex >= 5 && string.Compare(topic.Body, prototypeStartIndex - 4, "</h>", 0, 4) == 0)
					{
					headingEndIndex = prototypeStartIndex;
					headingStartIndex = topic.Body.LastIndexOf("<h", headingEndIndex - 5);  // We can assume it exists.
					}


				// Build the body without the prototype and possibly the heading.  The iterator was left after the closing pre tag of the
				// prototype.

				stringBuilder.Remove(0, stringBuilder.Length);  // Reuse

				// Remove the heading tag if there was nothing after the prototype or it's immediately followed by another heading.  In
				// other words, this heading contained nothing but the prototype.  Remember that headingStartIndex is only set if there
				// was nothing between it and the prototype.
				if (headingStartIndex != -1 && (iterator.IsInBounds == false || iterator.Type == NDMarkup.Iterator.ElementType.HeadingTag))
					{
					if (headingStartIndex > 0)
						{  stringBuilder.Append(topic.Body, 0, headingStartIndex);  }
					}
				else
					{
					if (prototypeStartIndex > 0)
						{  stringBuilder.Append(topic.Body, 0, prototypeStartIndex);  }
					}

				if (prototypeEndIndex < topic.Body.Length)
					{  stringBuilder.Append(topic.Body, prototypeEndIndex, topic.Body.Length - prototypeEndIndex);  }

				if (stringBuilder.Length == 0)
					{  topic.Body = null;  }
				else
					{  topic.Body = stringBuilder.ToString();  }
				}
			}

			
		/* Function: GenerateCommentContextAndSymbols
		 * Fills in context and symbol fields for a list of <CodePoints> and <Topics> generated from comments.  This will only apply 
		 * to Natural Docs topics with headers and will follow topic scoping rules.  Javadoc, MS XML, and headerless Natural Docs 
		 * topics will be skipped as they don't have titles to generate symbols from nor topic types to get scoping information from.
		 * 
		 * Specifically, these fields are filled in:
		 * 
		 *		- <CodePoint.Context>
		 *		- <Topic.PrototypeContext>
		 *		- <Topic.BodyContext>
		 *		- <Topic.Symbol>
		 *		- <Topic.ClassString>
		 */
		protected virtual void GenerateCommentContextAndSymbols (IList<CodePoint> commentCodePoints)
			{
			ClassString currentClass = new ClassString();
			ContextString currentContext = new ContextString();
			Topic lastNonEmbeddedTopic = null;

			foreach (CodePoint codePoint in commentCodePoints)
				{
				if (codePoint.Topic != null && codePoint.Topic.TopicTypeID != 0 && codePoint.Topic.Title != null)
					{
					TopicType topicType = Instance.TopicTypes.FromID(codePoint.Topic.TopicTypeID);

					string parentheses = null;
					SymbolString symbol = SymbolString.FromPlainText(codePoint.Topic.Title, out parentheses);

					bool partOfEnum;

					if (codePoint.Topic.IsEmbedded)
						{
						partOfEnum = lastNonEmbeddedTopic.IsEnum;
						}
					else
						{  
						lastNonEmbeddedTopic = codePoint.Topic;  
						partOfEnum = false;
						}

					if ( (partOfEnum && enumValue == EnumValues.UnderParent) ||
						  (!partOfEnum && topicType.Scope == TopicType.ScopeValue.Normal) )
						{
						codePoint.Topic.Symbol = currentContext.Scope + symbol;

						codePoint.Topic.ClassString = currentClass;
						codePoint.Topic.PrototypeContext = currentContext;
						codePoint.Topic.BodyContext = currentContext;
						}

					else if ( (partOfEnum && enumValue == EnumValues.Global) ||
								  (!partOfEnum && topicType.Scope == TopicType.ScopeValue.AlwaysGlobal) )
						{
						// The topic is global without affecting the current context
						codePoint.Topic.Symbol = symbol;

						// However, it's possible for an enum to be part of a class but have its values set to global.
						// In this case we leave the symbol global but still give it the same class ID as the enum 
						// because we want them to appear together in the output's class view.  The same goes for
						// Always Global topics that were documented as part of a class.
						codePoint.Topic.ClassString = currentClass;

						// Blank out the scope for the prototype context but leave any using statements
						ContextString globalContext = currentContext;
						globalContext.Scope = new SymbolString();

						codePoint.Topic.PrototypeContext = globalContext;

						// However, allow the body to retain the scope for linking
						codePoint.Topic.BodyContext = currentContext;
						}

					else if (partOfEnum && enumValue == EnumValues.UnderType)
						{
						codePoint.Topic.Symbol = lastNonEmbeddedTopic.Symbol + symbol;
						codePoint.Topic.ClassString = currentClass;

						ContextString enumContext = currentContext;
						enumContext.Scope = lastNonEmbeddedTopic.Symbol;

						codePoint.Topic.PrototypeContext = currentContext;
						codePoint.Topic.BodyContext = enumContext;
						}

					else if (topicType.Scope == TopicType.ScopeValue.Start)
						{
						codePoint.Topic.Symbol = symbol;

						if (topicType.Flags.ClassHierarchy)
							{
							currentClass = ClassString.FromParameters(ClassString.HierarchyType.Class, this.ID, this.CaseSensitive, symbol);
							codePoint.Topic.ClassString = currentClass;
							}
						else if (topicType.Flags.DatabaseHierarchy)
							{
							currentClass = ClassString.FromParameters(ClassString.HierarchyType.Database, 0, false, symbol);
							codePoint.Topic.ClassString = currentClass;
							}
						else
							{
							currentClass = new ClassString();
							}

						// Classes are treated as global
						currentContext.Scope = new SymbolString();
						codePoint.Topic.PrototypeContext = currentContext;

						// but the body is under the new scope so it can link to members easily.
						currentContext.Scope = symbol;
						codePoint.Topic.BodyContext = currentContext;

						codePoint.ContextChanged = true;
						codePoint.Context = currentContext;
						}

					else // (topicType.Scope == TopicType.ScopeValue.End)
						{
						codePoint.Topic.Symbol = symbol;

						// Everything is global
						currentContext.Scope = new SymbolString();
						currentClass = new ClassString();

						codePoint.Topic.ClassString = currentClass;
						codePoint.Topic.PrototypeContext = currentContext;
						codePoint.Topic.BodyContext = currentContext;

						codePoint.ContextChanged = true;
						codePoint.Context = currentContext;
						}

					// If it's an enum topic where the values are under the type, replace the body context with one that
					// includes the symbol name.
					if (codePoint.Topic.IsEmbedded == false && 
						 codePoint.Topic.IsEnum &&
						 enumValue == EnumValues.UnderType)
						{
						ContextString bodyContext = currentContext;
						bodyContext.Scope = codePoint.Topic.Symbol;

						codePoint.Topic.BodyContext = bodyContext;
						}
					}
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
						{  iterator.Tokenizer.SetPrototypeParsingTypeBetween(iterator, endOfDefaultValue, PrototypeParsingType.DefaultValue);  }

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
						{  startWord.Tokenizer.SetPrototypeParsingTypeBetween(startWord, endWord, PrototypeParsingType.TypeModifier);  }
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

							namePrefix.Tokenizer.SetPrototypeParsingTypeBetween(namePrefix, endWord, PrototypeParsingType.NamePrefix_PartOfType);
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
						{  startWord.Tokenizer.SetPrototypeParsingTypeBetween(startWord, endWord, PrototypeParsingType.TypeModifier);  }
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
				{  start.Tokenizer.SetPrototypeParsingTypeBetween(start, qualifierEnd, PrototypeParsingType.TypeQualifier);  }
			if (iterator > qualifierEnd)
				{  qualifierEnd.Tokenizer.SetPrototypeParsingTypeBetween(qualifierEnd, iterator, PrototypeParsingType.Type);  }
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
						{  startWord.Tokenizer.SetPrototypeParsingTypeBetween(startWord, endWord, PrototypeParsingType.TypeModifier);  }
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
				start.Tokenizer.SetPrototypeParsingTypeBetween(start, end, PrototypeParsingType.NameSuffix_PartOfType);
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
		 * 
		 * If the iterator is on a comment symbol, moves past it and returns true.  If you need information about the specific type of
		 * comment it was, you need to call <TryToSkipLineComment()> and <TryToSkipBlockComment()> individually.
		 * 
		 * Important:
		 * 
		 *		When you're skipping over generic code without interpreting it, these functions should always be called in this order:
		 * 
		 *		- <TryToSkipComment()>
		 *		- <TryToSkipString()>
		 *		- <TryToSkipBlock()>
		 * 
		 *		You want to check for comments before strings because Visual Basic uses the ' character for comments and you don't 
		 *		want the parser to interpret it as a string and search for a closing quote.
		 *		
		 *		You want to check for comments before blocks because Pascal uses braces for comments and you don't want to 
		 *		interpret the comment content as code.
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
		 * 
		 * If the iterator is on a quote or apostrophe, moves the iterator past the entire string and returns true.
		 * 
		 * Important:
		 * 
		 *		When you're skipping over generic code without interpreting it, these functions should always be called in this order:
		 * 
		 *		- <TryToSkipComment()>
		 *		- <TryToSkipString()>
		 *		- <TryToSkipBlock()>
		 * 
		 *		You want to check for comments before strings because Visual Basic uses the ' character for comments and you don't 
		 *		want the parser to interpret it as a string and search for a closing quote.
		 *		
		 *		You want to check for comments before blocks because Pascal uses braces for comments and you don't want to 
		 *		interpret the comment content as code.
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
		 * 
		 * If the iterator is on an opening symbol, moves it past the entire block and returns true.  This takes care of
		 * nested blocks, strings, and comments, but otherwise doesn't parse the underlying code.  You must specify
		 * whether to include < as an opening symbol because it may be relevant in some places (template definitions)
		 * but detrimental in others (general code where < could mean less than and not have a closing >.)
		 * 
		 * Important:
		 * 
		 *		When you're skipping over generic code without interpreting it, these functions should always be called in this order:
		 * 
		 *		- <TryToSkipComment()>
		 *		- <TryToSkipString()>
		 *		- <TryToSkipBlock()>
		 * 
		 *		You want to check for comments before strings because Visual Basic uses the ' character for comments and you don't 
		 *		want the parser to interpret it as a string and search for a closing quote.
		 *		
		 *		You want to check for comments before blocks because Pascal uses braces for comments and you don't want to 
		 *		interpret the comment content as code.
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
				// TryToSkipComment has to come first so ' comments in VB don't get interpreted as strings.
				else if (TryToSkipComment(ref iterator) ||
							 TryToSkipString(ref iterator))
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
				if (!lineIterator.IsInBounds)
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
		protected void SimpleSyntaxHighlight (Tokenizer source, StringSet keywords = null)
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
			"bool", "boolean", "true", "false", "null", "undef", "undefined", "var",

			"function", "operator", "delegate", "event", "enum", "typedef",

			"class", "struct", "interface", "template", "package", "union", "namespace",

			"extends", "implements", "import", "export", "extern", "native", "override", "overload", "explicit", "implicit",
			"super", "base", "my", "our", "require",

			"public", "private", "protected", "internal", "static", "virtual", "abstract", "friend", 
			"inline", "using", "final", "sealed", "register", "volatile",

			"ref", "in", "out", "inout", "const", "constant", "get", "set",

			"if", "else", "elif", "elseif", "then", "for", "foreach", "each", "do", "while", "switch", "case", "with", "in",
			"break", "continue", "next", "return", "goto",
			"try", "catch", "throw", "finally", "throws", "lock", "eval",

			"new", "delete", "sizeof"
			});


		static protected Regex.Comments.AcceptablePrototypeComments acceptablePrototypeCommentRegex
			= new Regex.Comments.AcceptablePrototypeComments();

		static protected Regex.Languages.ExtraOperatorWhitespace extraOperatorWhitespaceRegex
			= new Regex.Languages.ExtraOperatorWhitespace();

		}
	}