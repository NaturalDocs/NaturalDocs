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
		
		
		/* enum: ParseMode
		 * 
		 * What type of processing individual parsing functions will perform.  Not every mode is appropriate for every 
		 * function, but passing an unsupported mode would just be the equivalent of using <IterateOnly>.
		 * 
		 * IterateOnly - The function will simply move the iterator past the tokens.
		 * CreateElements - The function will create language <Elements> and add them to the list.
		 * SyntaxHighlight - The function will apply <SyntaxHighlightingTypes> to the tokens.
		 * ParsePrototype - The function will apply <PrototypeParsingTypes> to the tokens.
		 * ParseClassPrototype - The function will apply <ClassPrototypeParsingTypes> to the tokens.
		 */
		public enum ParseMode : byte
			{  IterateOnly, CreateElements, SyntaxHighlight, ParsePrototype, ParseClassPrototype  }


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


		/* enum: ValidateElementsMode
		 * CommentElements - Validates the results from <GetCommentElements()>.
		 * CodeElements - Validates the results from <GetCodeElements()>.
		 * MergedElements - Validates the results of merging the code and comment elements.
		 * Final - Validates the final list of <Elements> after all processing was performed.
		 */
		protected enum ValidateElementsMode : byte
			{  CommentElements, CodeElements, MergedElements, Final  }



		// Group: Functions
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
			List<Element> elements = null;
			topics = null;
			classParentLinks = null;


			// Find all the comments that could have documentation.

			List<PossibleDocumentationComment> possibleDocumentationComments = GetPossibleDocumentationComments(source);
			
			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }
			

			// Extract comment elements from them.  This could include Javadoc, XML, and headerless Natural Docs comments.
				
			List<Element> commentElements = GetCommentElements(possibleDocumentationComments);
			
			#if DEBUG
				ValidateElements(commentElements, ValidateElementsMode.CommentElements);
			#endif

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// Fill in the declared access levels.  We do this before merging with the code elements so the defaults that come from the 
			// comment settings only apply to topics that don't also appear in the code.  Anything that gets merged will have the comment
			// settings overwritten by the code settings.

			ApplyDeclaredAccessLevels(commentElements);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// If we have full language support, get the code elements as well.

			if (Type == LanguageType.FullSupport)
				{
				List<Element> codeElements = GetCodeElements(source);

				#if DEBUG
					ValidateElements(codeElements, ValidateElementsMode.CodeElements);
				#endif

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }


				// Fill in the declared access levels.  This is done before merging so code elements aren't affected by comment settings.

				ApplyDeclaredAccessLevels(codeElements);

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }


				// Combine the two.

				elements = MergeElements(commentElements, codeElements);
				codeElements = null;
				commentElements = null;

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }


				// Remove any headerless topics that weren't merged.

				RemoveHeaderlessTopics(elements);

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }


				#if DEBUG
					ValidateElements(elements, ValidateElementsMode.MergedElements);
				#endif
				}


			// If we have basic language support...

			else if (Type == LanguageType.BasicSupport)
				{
				
				// Fill in additional prototypes via our language-neutral algorithm.
	
				AddBasicPrototypes(source, commentElements, possibleDocumentationComments);

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }


				// Clear out headerless topics since there's no code topics to join them with.

				RemoveHeaderlessTopics(commentElements);

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }


				elements = commentElements;
				commentElements = null;
				}


			// If this is a text file just use the comment elements unaltered.

			else if (Type == LanguageType.TextFile)
				{
				// We don't have to remove headerless topics because there's no way to specify them in text files.

				elements = commentElements;
				commentElements = null;
				}


			// xxx Containers aren't supported yet so just return it as an empty file.

			else // Type == LanguageType.Container
				{
				topics = new List<Topic>();
				classParentLinks = new LinkSet();  
				return ParseResult.Success;  
				}


			// Calculate the effective access levels.  This is done after merging code and comment topics so members are consistent.  For
			// example, a public comment-only topic appearing in a private class needs to have an effective access level of private.

			GenerateEffectiveAccessLevels(elements);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// Apply file and language IDs.  This must be done before generating the remaining symbols because it needs to know the
			// language to be able to look up the enum settings.

			ApplyFileAndLanguageIDs(elements, fileID, this.ID);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// Generate remaining symbols.

			GenerateRemainingSymbols(elements);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// Apply comment prototypes, which will overwrite any code prototypes that exist.

			ApplyCommentPrototypes(elements);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// Apply remaining properties

			ApplyTags(elements);
			ApplyClassStrings(elements);
			ApplyUsingStatements(elements);
			ApplyContexts(elements);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// xxx apply prototype class parents
				// xxx make sure topic->element conversion searches class prototypes for them


			#if DEBUG
				ValidateElements(elements, ValidateElementsMode.Final);
			#endif


			// Extract and return the topics and parent links

			topics = new List<Topic>();
			classParentLinks = new LinkSet();

			foreach (var element in elements)
			   {
			   if (element.Topic != null)
			      {  topics.Add(element.Topic);  }

			   if (element.ClassParentLinks != null)
			      {
			      foreach (var link in element.ClassParentLinks)
			         {  classParentLinks.Add(link);  }
			      }
			   }

			return ParseResult.Success;
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


		/* Function: ParseClassPrototype
		 * Converts a raw text prototype into a <ParsedClassPrototype>.  Will return null if it is not an appropriate prototype.
		 */
		public virtual ParsedClassPrototype ParseClassPrototype (string stringPrototype, int topicTypeID)
			{
			if (Type == LanguageType.Container)
				{  throw new NotImplementedException();  }  //xxx

			if (Engine.Instance.TopicTypes.FromID(topicTypeID).Flags.ClassHierarchy == false)
				{  return null;  }

			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype);
			ParsedClassPrototype parsedPrototype = new ParsedClassPrototype(tokenizedPrototype);


			// First walk through trying to find a class keyword.  We're rather permissive when it comes to modifiers to allow for things
			// like splint comments and bracketed C# metadata.

			TokenIterator iterator = tokenizedPrototype.FirstToken;

			for (;;)
				{
				if (iterator.IsInBounds == false)
					{  return null;  }
				else if (iterator.MatchesToken("class") || 
						  iterator.MatchesToken("struct") || 
						  iterator.MatchesToken("interface"))
					{  
					// Only count it as a keyword if it's surrounded by whitespace.  We don't want to get tripped up on a macro called
					// external_class or something like that.

					TokenIterator lookahead = iterator;
					lookahead.Next();

					TokenIterator lookbehind = iterator;
					lookbehind.Previous();

					if (lookahead.FundamentalType == FundamentalType.Whitespace &&
						 (lookbehind.IsInBounds == false || lookbehind.FundamentalType == FundamentalType.Whitespace) )
						{  break;  }
					else
						{  iterator.Next();  }
					}
				else if (TryToSkipComment(ref iterator) ||
						  TryToSkipString(ref iterator) ||
						  TryToSkipBlock(ref iterator, true))
					{  }
				else
					{  iterator.Next();  }
				}

			TokenIterator startOfModifiers = tokenizedPrototype.FirstToken;
			TokenIterator endOfModifiers = iterator;

			endOfModifiers.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfModifiers);
			startOfModifiers.NextPastWhitespace(endOfModifiers);

			if (endOfModifiers > startOfModifiers)
				{  tokenizedPrototype.SetClassPrototypeParsingTypeBetween(startOfModifiers, endOfModifiers, ClassPrototypeParsingType.Modifier);  }


			// The iterator is on the keyword.  Get the name.

			iterator.Next();
			iterator.NextPastWhitespace();

			TokenIterator startOfName = iterator;

			for (;;)
				{
				if (iterator.FundamentalType == FundamentalType.Text ||
					iterator.Character == '_' || iterator.Character == '.')
					{  iterator.Next();  }
				else if (iterator.MatchesAcrossTokens("::"))
					{  iterator.Next(2);  }
				else
					{  break;  }
				}

			TokenIterator endOfName = iterator;

			endOfName.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfName);
			// We already moved startOfName past whitespace.

			if (endOfName == startOfName)
				{  return null;  }

			tokenizedPrototype.SetClassPrototypeParsingTypeBetween(startOfName, endOfName, ClassPrototypeParsingType.Name);


			// Iterator is past the name.  Get the template information if there is any.

			iterator.NextPastWhitespace();

			if (iterator.Character == '<')
				{
				TokenIterator startOfTemplate = iterator;

				if (TryToSkipBlock(ref iterator, true) == false)
					{  return null;  }

				tokenizedPrototype.SetClassPrototypeParsingTypeBetween(startOfTemplate, iterator, ClassPrototypeParsingType.TemplateSuffix);

				iterator.NextPastWhitespace();
				}

			
			// We now have a valid prototype.  See if we can find any parents.  We won't be parsing them yet, we're just finding the
			// separators.

			// These are the types of things we're looking for.
			// X : Y, Z
			// X : inherit Y, Z
			// X extends Y, Z
			// X extends Y implements Z

			bool getParents = false;

			if (iterator.Character == ':')
				{
				TokenIterator lookahead = iterator;
				bool doubleToken = false;

				lookahead.Next();
				lookahead.NextPastWhitespace();

				if (lookahead.MatchesAnyToken(inheritanceKeywords) != -1)
					{
					lookahead.Next();

					if (lookahead.FundamentalType == FundamentalType.Whitespace)
						{  doubleToken = true;  }
					}

				if (doubleToken)
					{
					tokenizedPrototype.SetClassPrototypeParsingTypeBetween(iterator, lookahead, ClassPrototypeParsingType.StartOfParents);
					iterator = lookahead;
					}
				else
					{
					iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfParents;
					iterator.Next();
					iterator.NextPastWhitespace();
					}

				getParents = true;
				}

			else if (iterator.MatchesAnyToken(inheritanceKeywords) != -1)
				{
				TokenIterator lookahead = iterator;
				lookahead.Next();

				if (lookahead.FundamentalType == FundamentalType.Whitespace)
					{
					iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfParents;
					iterator = lookahead;
					iterator.NextPastWhitespace();

					getParents = true;
					}
				}

			while (getParents && iterator.IsInBounds)
				{
				if (iterator.Character == ',' || iterator.Character == ';')
					{
					TokenIterator lookahead = iterator;
					bool doubleToken = false;

					lookahead.Next();
					lookahead.NextPastWhitespace();

					if (lookahead.MatchesAnyToken(inheritanceKeywords) != -1)
						{
						lookahead.Next();

						if (lookahead.FundamentalType == FundamentalType.Whitespace)
							{  doubleToken = true;  }
						}

					if (doubleToken)
						{
						tokenizedPrototype.SetClassPrototypeParsingTypeBetween(iterator, lookahead, ClassPrototypeParsingType.ParentSeparator);
						iterator = lookahead;
						iterator.NextPastWhitespace();
						}
					else
						{
						iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.ParentSeparator;
						iterator.Next();
						iterator.NextPastWhitespace();
						}
					}
				else if (iterator.MatchesAnyToken(inheritanceKeywords) != -1)
					{
					TokenIterator lookahead = iterator;
					lookahead.Next();

					TokenIterator lookbehind = iterator;
					lookbehind.Previous();

					if (lookahead.Character != '_' && lookbehind.Character != '_')
						{  iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.ParentSeparator;  }

					iterator.Next();
					}
				else if (iterator.Character == '{')
					{  
					iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfBody;  
					getParents = false;
					}
				else if (iterator.MatchesToken("where"))
					{
					TokenIterator lookahead = iterator;
					lookahead.Next();

					TokenIterator lookbehind = iterator;
					lookbehind.Previous();

					if (lookahead.Character != '_' && lookbehind.Character != '_')
						{
						while (lookahead.IsInBounds && lookahead.Character != '{')
							{
							if (TryToSkipComment(ref lookahead) ||
								 TryToSkipString(ref lookahead) ||
								 TryToSkipBlock(ref lookahead, true))
								{  }
							else
								{  lookahead.Next();  }
							}

						tokenizedPrototype.SetClassPrototypeParsingTypeBetween(iterator, lookahead, ClassPrototypeParsingType.Modifier);

						if (lookahead.Character == '{')
							{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfBody;  }

						getParents = false;
						}
					else
						{  iterator.Next();  }
					}
				else if (TryToSkipComment(ref iterator) ||
						  TryToSkipString(ref iterator) ||
						  TryToSkipBlock(ref iterator, true))
					{  }
				else
					{
					iterator.Next();
					}
				}


			// Now that we have our parents separated out, iterate through them to mark the names and modifiers.

			for (int i = 0; i < parsedPrototype.NumberOfParents; i++)
				{
				TokenIterator startOfParent, endOfParent;
				parsedPrototype.GetParent(i, out startOfParent, out endOfParent);


				// Find the name, which will be the last acceptable word or the first one followed by template information.

				startOfName = startOfParent;
				endOfName = startOfParent;

				iterator = startOfParent;

				while (iterator < endOfParent)
					{
					if (iterator.MatchesToken("where"))
						{
						TokenIterator lookahead = iterator;
						lookahead.Next();

						TokenIterator lookbehind = iterator;
						lookbehind.Previous();
						
						if ( (lookahead >= endOfParent || lookahead.Character != '_') && 
							 (lookbehind < startOfParent || lookbehind.Character != '_') )
							{
							// We've reached a "where" clause so we can stop looking for a name.
							break;
							}
						}

					if (iterator.FundamentalType == FundamentalType.Text || iterator.Character == '_')
						{
						startOfName = iterator;
						endOfName = iterator;
						endOfName.Next();

						while (endOfName < endOfParent)
							{
							if (endOfName.FundamentalType == FundamentalType.Text || 
								endOfName.Character == '.' || endOfName.Character == '_')
								{  endOfName.Next();  }
							else if (endOfName.MatchesAcrossTokens("::"))
								{  endOfName.Next(2);  }
							else
								{  break;  }
							}

						iterator = endOfName;
						iterator.NextPastWhitespace(endOfParent);

						if (iterator.Character == '<' && iterator < endOfParent)
							{
							TokenIterator lookahead = iterator;
							if (TryToSkipBlock(ref lookahead, true) == true && lookahead <= endOfParent)
								{
								// We've reached template information so we can stop looking for the name.
								break;
								}
							}
						}
					else if (TryToSkipComment(ref iterator) ||
							  TryToSkipString(ref iterator) ||
							  TryToSkipBlock(ref iterator, true))
						{  }
					else
						{  iterator.Next();  }
					}


				// Now mark the tokens

				if (endOfName > startOfName)
					{
					tokenizedPrototype.SetClassPrototypeParsingTypeBetween(startOfName, endOfName, ClassPrototypeParsingType.Name);

					startOfModifiers = startOfParent;
					endOfModifiers = startOfName;

					endOfModifiers.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfModifiers);
					startOfModifiers.NextPastWhitespace(endOfModifiers);

					if (endOfModifiers > startOfModifiers)
						{  tokenizedPrototype.SetClassPrototypeParsingTypeBetween(startOfModifiers, endOfModifiers, ClassPrototypeParsingType.Modifier);  }

					TokenIterator startOfTemplate = endOfName;
					startOfTemplate.NextPastWhitespace(endOfParent);

					TokenIterator endOfTemplate = startOfTemplate;
					if (startOfTemplate < endOfParent && startOfTemplate.Character == '<' && 
						TryToSkipBlock(ref endOfTemplate, true) == true && endOfTemplate <= endOfParent)
						{
						tokenizedPrototype.SetClassPrototypeParsingTypeBetween(startOfTemplate, endOfTemplate, ClassPrototypeParsingType.TemplateSuffix);
						}
					else
						{
						// Reset in case TryToSkipBlock() worked but it went past the parent.  
						endOfTemplate = startOfTemplate;  
						}

					// There may also be modifiers after the name.
					startOfModifiers = endOfTemplate;
					endOfModifiers = endOfParent;

					endOfModifiers.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfModifiers);
					startOfModifiers.NextPastWhitespace(endOfModifiers);

					if (endOfModifiers > startOfModifiers)
						{  tokenizedPrototype.SetClassPrototypeParsingTypeBetween(startOfModifiers, endOfModifiers, ClassPrototypeParsingType.Modifier);  }
					}
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


		// Function: GetPossibleDocumentationComments
		// 
		// Goes through the file looking for comments that could possibly contain documentation and returns them as a list.  These 
		// comments are not guaranteed to have documentation in them, just to be acceptable candidates for them.  If there are no 
		// comments it will return an empty list.
		//
		// All the comments in the returned list will have their comment symbols marked as <CommentParsingType.CommentSymbol>
		// in the tokenizer.  This allows further operations to be done on them in a language independent manner.  If you want to also
		// filter out text boxes and lines, use <Comments.Parsers.LineFinder>.
		//
		// If you already have the source code in tokenized form it would be more efficient to pass it as a <Tokenizer>.
		//
		public List<PossibleDocumentationComment> GetPossibleDocumentationComments (string source)
			{
			return GetPossibleDocumentationComments(new Tokenizer(source));
			}


		// Function: GetPossibleDocumentationComments
		// 
		// Goes through the file looking for comments that could possibly contain documentation and returns them as a list.  These 
		// comments are not guaranteed to have documentation in them, just to be acceptable candidates for them.  If there are no 
		// comments it will return an empty list.
		//
		// All the comments in the returned list will have their comment symbols marked as <CommentParsingType.CommentSymbol>
		// in the tokenizer.  This allows further operations to be done on them in a language independent manner.  If you want to also
		// filter out text boxes and lines, use <Comments.Parsers.LineFinder>.
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
		virtual public List<PossibleDocumentationComment> GetPossibleDocumentationComments (Tokenizer source)
			{
			List<PossibleDocumentationComment> possibleDocumentationComments = new List<PossibleDocumentationComment>();

			if (Type == LanguageType.Container)
				{  
				return new List<PossibleDocumentationComment>(); // xxx
				}

			else if (Type == LanguageType.TextFile)
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


		/* Function: IsBuiltInType
		 * Returns whether the type string is a built-in type such as "int" as opposed to a user-defined type.
		 */
		virtual public bool IsBuiltInType (string type)
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

			// This is a problem if one uses "constructor" and one uses "function" and they don't map to the same topic type.
			if (topicA.TopicTypeID != topicB.TopicTypeID)
				{  return false;  }

			bool ignoreCase = (Engine.Instance.Languages.FromID(topicA.LanguageID).CaseSensitive == false);

			if (string.Compare(topicA.Symbol, topicB.Symbol, ignoreCase) != 0)
				{  return false;  }

			// So now we have two topics of the same language, symbol, and type.  Now the assumption is they're the same
			// unless they're distinguished by parameters.
			return (string.Compare(topicA.PrototypeParameters, topicB.PrototypeParameters, ignoreCase) == 0);
			}


			
		// Group: Parsing Stages
		// __________________________________________________________________________
		
			
		/* Function: GetCommentElements
		 * 
		 * Goes through the <PossibleDocumentationComments> looking for comment <Elements> that should be included in the
		 * output.  If there are none, it will return an empty list.
		 * 
		 * The default implementation sends each <PossibleDocumentationComment> to <Comments.Manager.Parse()> and then
		 * converts the <Topics> to <Elements>.  There should be no need to change it.
		 */
		protected virtual List<Element> GetCommentElements (List<PossibleDocumentationComment> possibleDocumentationComments)
			{
			List<Topic> topics = new List<Topic>();
				
			foreach (var comment in possibleDocumentationComments)
				{  Engine.Instance.Comments.Parse(comment, topics);  }


			// Convert the topics to elements.  Generate ranges for scoped topics and groups.

			List<Element> elements = new List<Element>(topics.Count + 1);

			ParentElement rootElement = new ParentElement(0, 0, Element.Flags.InComments);
			rootElement.IsRootElement = true;
			rootElement.DefaultDeclaredChildAccessLevel = AccessLevel.Public;
			rootElement.DefaultChildLanguageID = this.ID;
			rootElement.EndingLineNumber = int.MaxValue;
			rootElement.EndingCharNumber = int.MaxValue;

			elements.Add(rootElement);

			ParentElement lastClass = null;
			ParentElement lastGroup = null;

			int groupTopicTypeID = Engine.Instance.TopicTypes.IDFromKeyword("group");

			int i = 0;
			while (i < topics.Count)
				{
				Topic topic = topics[i];
				TopicType topicType = null;

				// Look up the topic type and end the previous class or group if necessary.
				if (topic.TopicTypeID != 0)
					{
					topicType = Engine.Instance.TopicTypes.FromID(topic.TopicTypeID);

					if (topicType.Scope == TopicType.ScopeValue.Start ||
						 topicType.Scope == TopicType.ScopeValue.End)
						{
						if (lastClass != null)
							{
							lastClass.EndingLineNumber = topic.CommentLineNumber;
							lastClass.EndingCharNumber = 1;
							lastClass = null;
							}
						}

					if (topicType.Scope == TopicType.ScopeValue.Start ||
						 topicType.Scope == TopicType.ScopeValue.End ||
						 topicType.ID == groupTopicTypeID)
						{
						if (lastGroup != null)
							{
							lastGroup.EndingLineNumber = topic.CommentLineNumber;
							lastGroup.EndingCharNumber = 1;
							lastGroup = null;
							}
						}
					}

				// Embedded topics get a ParentElement and their children get regular Elements no matter what.
				if (i + 1 < topics.Count && topics[i + 1].IsEmbedded == true)
					{
					ParentElement parentElement = new ParentElement(topic.CommentLineNumber, 1, Element.Flags.InComments);
					parentElement.Topic = topic;
					parentElement.DefaultChildLanguageID = topic.LanguageID;
					parentElement.DefaultDeclaredChildAccessLevel = topic.DeclaredAccessLevel;
					elements.Add(parentElement);
					i++;

					do
						{
						Element embeddedElement = new Element(topics[i].CommentLineNumber, 1, Element.Flags.InComments);
						embeddedElement.Topic = topics[i];
						elements.Add(embeddedElement);
						i++;
						}
					while (i < topics.Count && topics[i].IsEmbedded == true);

					if (i < topics.Count)
						{
						parentElement.EndingLineNumber = topics[i].CommentLineNumber;
						parentElement.EndingCharNumber = 1;
						}
					else
						{
						parentElement.EndingLineNumber = topics[i - 1].CommentLineNumber + 1;
						parentElement.EndingCharNumber = 1;
						}
					}

				// Scoped and group topics get a ParentElement.  Sections get treated like classes because we want any modifiers they 
				// have to be inherited by the topics that follow.
				else if (topicType != null && topic.IsList == false &&
						  (topicType.Scope == TopicType.ScopeValue.Start || 
						   topicType.Scope == TopicType.ScopeValue.End || 
						   topicType.ID == groupTopicTypeID))
					{
					ParentElement parentElement = new ParentElement(topic.CommentLineNumber, 1, Element.Flags.InComments);
					parentElement.Topic = topic;
					parentElement.DefaultChildLanguageID = topic.LanguageID;

					if (topicType.ID == groupTopicTypeID)
						{  
						// Groups don't enfoce a maximum access level, they just set the default declared level.
						parentElement.DefaultDeclaredChildAccessLevel = topic.DeclaredAccessLevel;
						}
					else // scope start or end
						{  
						parentElement.MaximumEffectiveChildAccessLevel = topic.DeclaredAccessLevel;
						parentElement.DefaultDeclaredChildAccessLevel = AccessLevel.Public;  
						}

					elements.Add(parentElement);
					i++;

					if (topicType.ID == groupTopicTypeID)
						{  lastGroup = parentElement;  }
					else
						{  lastClass = parentElement;  }
					}

				// Other topics including headerless ones get regular Elements
				else
					{
					Element element = new Element(topic.CommentLineNumber, 1, Element.Flags.InComments);
					element.Topic = topic;
					elements.Add(element);
					i++;
					}
				}


			// Close up any open parents.

			if (lastClass != null)
				{
				lastClass.EndingLineNumber = possibleDocumentationComments[possibleDocumentationComments.Count - 1].End.LineNumber + 1;
				lastClass.EndingCharNumber = 1;
				}

			if (lastGroup != null)
				{
				lastGroup.EndingLineNumber = possibleDocumentationComments[possibleDocumentationComments.Count - 1].End.LineNumber + 1;
				lastGroup.EndingCharNumber = 1;
				}

			return elements;
			}


		/* Function: GetCodeElements
		 * 
		 * Goes through the file looking for code <Elements> that should be included in the output.  If there are none or the language 
		 * doesn't have full support, it will return an empty list.
		 * 
		 * Implementation Requirements:
		 * 
		 *		Subclasses override this function as part of providing full language support.  Any <Topics> returned within these <Elements>
		 *		must meet these requirements:
		 *		
		 *		- Every <Topic> must have a title set.
		 *		- Every <Topic> must have a topic type ID set.
		 *		- Every <Topic> must have a symbol set.
		 *		- Each symbol must be fully resolved.  For example, a function appearing in a class must have the symbol "Class.Function".
		 *		  Other code will not apply parent symbols to children.
		 *		- You cannot return list <Topics>.  If you have multiple variables declared in one statement or something similar, you must
		 *		  generate individual <Topics> for each one.
		 *		- You cannot return embedded <Topics>.  This may change in the future only to allow enum members, but for now it is not
		 *		  allowed at all.
		 */
		public virtual List<Element> GetCodeElements (Tokenizer source)
			{
			return new List<Element>();
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
		protected virtual void AddBasicPrototypes (Tokenizer source, List<Element> elements, 
																  List<PossibleDocumentationComment> possibleDocumentationComments)
			{
			int elementIndex = 0;

			for (int commentIndex = 0; commentIndex < possibleDocumentationComments.Count; commentIndex++)
				{
				PossibleDocumentationComment comment = possibleDocumentationComments[commentIndex];

				// Advance the element index to the last one before the end of this comment.  If there are multiple topics in a 
				// comment only the last one gets a prototype search.

				while (elementIndex + 1 < elements.Count && 
						 elements[elementIndex + 1].LineNumber < comment.End.LineNumber)
					{  elementIndex++;  }

				// Now back up past any embedded topics.  We don't want the last embedded topic to get the prototype
				// instead of the parent topic.

				while (elementIndex < elements.Count && 
						 elements[elementIndex].Topic != null &&
						 elements[elementIndex].Topic.IsEmbedded && 
						 elementIndex > 0 && 
						 elements[elementIndex - 1].LineNumber >= comment.Start.LineNumber)
					{  elementIndex--;  }

				if (elementIndex >= elements.Count ||
					 elements[elementIndex].Topic == null ||
					 elements[elementIndex].LineNumber < comment.Start.LineNumber ||
					 elements[elementIndex].LineNumber > comment.End.LineNumber)
					{  
					// We're out of topics or the one we're on isn't in this comment.
					continue;  
					}

				// If it already has a prototype, probably from one embedded in a comment, don't search for a new one.

				if (elements[elementIndex].Topic.Prototype != null)
					{  continue;  }

				// Build the bounds for the prototype search and perform it.

				Tokenization.LineIterator startCode = comment.End;
				Tokenization.LineIterator endCode;

				if (commentIndex + 1 < possibleDocumentationComments.Count)
					{  endCode = possibleDocumentationComments[commentIndex + 1].Start;  }
				else
					{  endCode = source.LastLine;  }

				AddBasicPrototype(elements[elementIndex].Topic, startCode, endCode);
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
		 * sets them as the topic prototype.  If the topic already had a prototype it will be overwritten.
		 */
		protected virtual void ApplyCommentPrototypes (List<Element> elements)
			{
			StringBuilder stringBuilder = null;

			foreach (var element in elements)
				{
				var topic = element.Topic;

				if (topic == null || topic.Body == null)
					{  continue;  }


				// Find the bounds of the prototype block and extract its content.

				int prototypeStartIndex = topic.Body.IndexOf("<pre type=\"prototype\"");

				if (prototypeStartIndex == -1)
					{  continue;  }

				NDMarkup.Iterator iterator = new NDMarkup.Iterator(topic.Body, prototypeStartIndex);

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


				// Apply it to the topic.  If it already had a prototype it will be overwritten.

				topic.Prototype = NormalizePrototype(stringBuilder.ToString());


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


		/* Function: MergeElements
		 * Combines code and comment <Elements> into one list.  The original <Elements> and/or <Topics> may be reused so don't use them 
		 * after calling this function.
		 */
		protected List<Element> MergeElements (List<Element> commentElements, List<Element> codeElements)
			{
			if (codeElements == null || codeElements.Count == 0 || 
				(codeElements.Count == 1 && codeElements[0] is ParentElement && (codeElements[0] as ParentElement).IsRootElement))
				{  return commentElements;  }

			if (commentElements == null || commentElements.Count == 0 || 
				(commentElements.Count == 1 && commentElements[0] is ParentElement && (commentElements[0] as ParentElement).IsRootElement))
				{  return codeElements;  }


			// So now both codeElements and commentElements have at least one element of actual content.

			List<Element> mergedElements = new List<Element>(codeElements.Count);
			int codeIndex = 0;
			int commentIndex = 0;


			// Ignore the comment root element.  If there's a code root element we'll keep it.

			if (commentElements[0] is ParentElement && (commentElements[0] as ParentElement).IsRootElement)
				{  commentIndex++;  }

			if (codeElements[0] is ParentElement && (codeElements[0] as ParentElement).IsRootElement)
				{
				mergedElements.Add(codeElements[0]);
				codeIndex++;
				}


			// Comments must appear above what they document, so any code elements that appear before the first comment element 
			// are added as is.

			while (codeIndex < codeElements.Count && codeElements[codeIndex].Position < commentElements[commentIndex].Position)
				{
				mergedElements.Add(codeElements[codeIndex]);
				codeIndex++;
				}


			// Now find and loop through block pairs.  The comment block is all the consecutive comments appearing until the next code 
			// element.  The code block is that element and all consecutive elements appearing until the next comment element or the end
			// of the file.

			while (commentIndex < commentElements.Count && codeIndex < codeElements.Count)
				{
				int commentCount = 1;

				while (commentIndex + commentCount < commentElements.Count &&
						 commentElements[commentIndex + commentCount].Position < codeElements[codeIndex].Position)
					{  commentCount++;  }

				int codeCount = 1;

				if (commentIndex + commentCount == commentElements.Count)
					{  codeCount = codeElements.Count - codeIndex;  }
				else
					{
					while (codeIndex + codeCount < codeElements.Count &&
							 codeElements[codeIndex + codeCount].Position < commentElements[commentIndex + commentCount].Position)
						{  codeCount++;  }
					}


				// First see if the last comment topic is headerless.  If so, it gets merged with the first code topic and everything else is
				// added as is.
				
				int lastCommentIndex = commentIndex + commentCount - 1;

				if (commentElements[lastCommentIndex].Topic != null &&
					commentElements[lastCommentIndex].Topic.Title == null &&
					CanMergeTopics(commentElements[lastCommentIndex].Topic, codeElements[codeIndex].Topic, true))
					{
					for (int i = commentIndex; i < lastCommentIndex; i++)
						{  mergedElements.Add(commentElements[i]);  }

					var mergedElement = codeElements[codeIndex];
					mergedElement.InComments = true;
					mergedElement.Topic = MergeTopics(commentElements[lastCommentIndex].Topic, codeElements[codeIndex].Topic);
					mergedElements.Add(mergedElement);

					for (int i = codeIndex + 1; i < codeIndex + codeCount; i++)
						{  mergedElements.Add(codeElements[i]);  }

					commentIndex += commentCount;
					codeIndex += codeCount;
					}


				// Otherwise go through each comment element one by one, looking for the first match in the code topics.

				else
					{
					while (commentCount > 0)
						{
						int matchingCodeIndex = -1;

						for (int i = codeIndex; i < codeIndex + codeCount; i++)
							{
							if (CanMergeTopics(commentElements[commentIndex].Topic, codeElements[i].Topic, false))
								{
								matchingCodeIndex = i;
								break;
								}
							}

						// If there's no matching code element, add the comment element and continue.  All code elements are still
						// candidates for the remaining comments.

						if (matchingCodeIndex == -1)
							{
							mergedElements.Add(commentElements[commentIndex]);
							commentIndex++;
							commentCount--;

							// Since we're using the code elements' positions, we may have to squish the comment element in beneath
							// the last one to make sure the list is still in order.

							if (mergedElements.Count > 1 &&
								mergedElements[mergedElements.Count - 1].Position < mergedElements[mergedElements.Count - 2].Position)
								{
								mergedElements[mergedElements.Count - 1].LineNumber = mergedElements[mergedElements.Count - 2].LineNumber;
								mergedElements[mergedElements.Count - 1].CharNumber = mergedElements[mergedElements.Count - 2].CharNumber + 1;
								}
							}

						// If there is a matching code element...
						else
							{
							// Comment and code elements should match in order to avoid weird side effects like members being pulled out of their parents' 
							// scope, so for non-list elements we enforce this.  We add all the code elements above the match as is; they are no longer 
							// candidates for matching.
							
							// Ideally we would do this for list topics as well, but we make an exception for them.  List topics are for documenting lots of
							// small elements in one place, which means they're much more likely to be far from their code elements, and forcing the user
							// to document them in the order in which they're defined is too restricting.  We'll make it the user's responsibility to not
							// document them in a way that would cause side effects.

							if (commentElements[commentIndex].Topic.IsEmbedded == false)
								{
								while (codeIndex < matchingCodeIndex)
									{
									mergedElements.Add(codeElements[codeIndex]);
									codeIndex++;
									codeCount--;
									}

								var mergedElement = codeElements[codeIndex];
								mergedElement.InComments = true;
								mergedElement.Topic = MergeTopics(commentElements[commentIndex].Topic, codeElements[codeIndex].Topic);
								mergedElements.Add(mergedElement);

								commentIndex++;
								commentCount--;
								codeIndex++;
								codeCount--;
								}

							else // comment topic is embedded
								{
								// Use the comment element instead of the code element.
								var mergedElement = commentElements[commentIndex];
								mergedElement.InCode = true;
								mergedElement.Topic = MergeTopics(commentElements[commentIndex].Topic, codeElements[matchingCodeIndex].Topic);
								mergedElements.Add(mergedElement);

								commentIndex++;
								commentCount--;

								// If the code element had a scope, we have to remove all its members.  This is so if you document a class as part of
								// a list topic (maybe documenting a lot of little structs?) you don't get the members appearing independently.

								if (codeElements[matchingCodeIndex] is ParentElement)
									{
									ParentElement parentElement = (ParentElement)codeElements[matchingCodeIndex];

									while (matchingCodeIndex + 1 < codeElements.Count && parentElement.Contains(codeElements[matchingCodeIndex + 1]))
										{
										codeElements.RemoveAt(matchingCodeIndex + 1);

										// The child elements may extend past the current block of code elements.
										if (matchingCodeIndex + 1 < codeIndex + codeCount)
											{  codeCount--;  }
										}
									}

								codeElements.RemoveAt(matchingCodeIndex);
								codeCount--;
								}
							}
						}


					// If there's no more comment elements, add the rest of the code elements as is.

					while (codeCount > 0)
						{
						mergedElements.Add(codeElements[codeIndex]);
						codeIndex++;
						codeCount--;
						}
					}

				}


			// Add any comment elements that appear after the last code element as is.

			while (commentIndex < commentElements.Count)
				{
				mergedElements.Add(commentElements[commentIndex]);
				commentIndex++;
				}


			// Now we may have to fix up the ranges of any comment only elements.

			for (int i = 0; i < mergedElements.Count; i++)
				{
				if (mergedElements[i].InCode == false && mergedElements[i] is ParentElement)
					{
					ParentElement element = (ParentElement)mergedElements[i];

					// First, it can't extend past the range of its parent.  This may happen if there's a group that appears in a class, which
					// would normally extend to the next class or group, but the actual code class ends before that.

					int parentIndex = FindElementParent(mergedElements, i);

					if (parentIndex != -1)
						{
						ParentElement parentElement = (ParentElement)mergedElements[parentIndex];

						if (element.EndingPosition > parentElement.EndingPosition)
							{
							element.EndingLineNumber = parentElement.EndingLineNumber;
							element.EndingCharNumber = parentElement.EndingCharNumber;
							}
						}

					// Next, if it has a topic that starts or ends scope, that scope only extends until the next code element.   Basically if
					// you declare a comment-only class, you can add comment-only members to it but once an actual code element is
					// reached the scope reverts back to the code.  This behavior doesn't apply to groups since we want to be able to use
					// them with code elements.

					if (element.Topic != null && element.Topic.TopicTypeID != 0)
						{
						var topicType = Engine.Instance.TopicTypes.FromID(element.Topic.TopicTypeID);

						if (topicType.Scope == TopicType.ScopeValue.Start ||
							topicType.Scope == TopicType.ScopeValue.End)
							{
							int nextCodeIndex = i + 1;

							while (nextCodeIndex < mergedElements.Count && mergedElements[nextCodeIndex].InCode == false)
								{  nextCodeIndex++;  }

							if (nextCodeIndex < mergedElements.Count)
								{
								var nextCodeElement = mergedElements[nextCodeIndex];

								if (element.EndingPosition > nextCodeElement.Position)
									{
									element.EndingLineNumber = nextCodeElement.LineNumber;
									element.EndingCharNumber = nextCodeElement.CharNumber;
									}
								}
							}
						}
					}
				}


			return mergedElements;
			}


		/* Function: CanMergeTopics
		 * Returns whether the <Topics> match and can be merged.  It is safe to pass null to this function.  If either topic is null it will 
		 * return false, even if both are.
		 */
		protected bool CanMergeTopics (Topic commentTopic, Topic codeTopic, bool allowHeaderlessTopics)
			{
			if (codeTopic == null || commentTopic == null)
				{  return false;  }

			// List topics should not be merged with code, only its members.
			if (commentTopic.IsList)
				{  return false;  }

			if (commentTopic.Title == null)
				{
				return allowHeaderlessTopics;
				}

			else
				{
				#if DEBUG
				if (commentTopic.TopicTypeID == 0)
					{  throw new Exception ("All comment topics with titles must have topic type IDs before calling CanMergeTopics().");  }
				#endif

				// Documentation and file topics should not be merged with code.  Headerless topics are assumed to be code.
				if (Engine.Instance.TopicTypes.FromID(commentTopic.TopicTypeID).Flags.Code == false)
					{  return false;  }

				#if DEBUG
				if (codeTopic.Symbol == null)
					{  throw new Exception ("All code topics must have symbols before calling CanMergeTopics().");  }
				#endif

				string ignore;
				SymbolString commentSymbol = SymbolString.FromPlainText(commentTopic.Title, out ignore);

				return (codeTopic.Symbol == commentSymbol ||
						  codeTopic.Symbol.EndsWith(commentSymbol));
				}
			}


		/* Function: MergeTopics
		 * Combines the properties of the two <Topics> and returns a new one.
		 */
		protected Topic MergeTopics (Topic commentTopic, Topic codeTopic)
			{
			#if DEBUG
			if (CanMergeTopics(commentTopic, codeTopic, true) == false)
				{  throw new Exception ("Tried to merge topics that did not pass CanMergeTopics().");  }
			#endif

			Topic mergedTopic = new Topic();

			// TopicID - Shouldn't be set on either.
			
			// Title - If the user specified one, we always want to use that.
			if (commentTopic.Title != null)
				{  mergedTopic.Title = commentTopic.Title;  }
			else
				{  mergedTopic.Title = codeTopic.Title;  }

			// Body - Use the comment.
			mergedTopic.Body = commentTopic.Body;

			// Summary - Use the comment.
			mergedTopic.Summary = commentTopic.Summary;

			// Prototype - Use the code.  If there's one manually specified in the comment, ApplyCommentPrototypes() will overwrite it later.
			mergedTopic.Prototype = codeTopic.Prototype;

			// Symbol - Use the code, even if we replaced the title.
			mergedTopic.Symbol = codeTopic.Symbol;

			// SymbolDefinitionNumber - Shouldn't be set on either.

			// ClassString - Use the code.
			mergedTopic.ClassString = codeTopic.ClassString;

			// ClassID - Shouldn't be set on either.

			// IsList - Shouldn't be set on either as they shouldn't be merged.
			#if DEBUG
			if (commentTopic.IsList || codeTopic.IsList)
				{  throw new Exception ("Tried to merge list topics.");  }
			#endif

			// IsEmbedded - Use the comment.  We want to be able to merge code topics into embedded comment topics.
			mergedTopic.IsEmbedded = commentTopic.IsEmbedded;

			// TopicTypeID - If the user specified one, we always want to use that.  We don't care if the topic type would normally switch the
			//					   containing element between an Element and a ParentElement, or if the new type has a different scope setting.  We'll
			//					   switch to the comment topic type but retain the code settings for those.
			if (commentTopic.TopicTypeID != 0)
				{  mergedTopic.TopicTypeID = commentTopic.TopicTypeID;  }
			else
				{  mergedTopic.TopicTypeID = codeTopic.TopicTypeID;  }

			// DeclaredAccessLevel - Use the code.  The topic settings only apply when the topic only appears in the comments.
			// xxx Copy when the language doesn't have native support for access levels, like JavaScript.
			mergedTopic.DeclaredAccessLevel = codeTopic.DeclaredAccessLevel;

			// EffectiveAccessLevel - Shouldn't be set on either.

			// TagIDs - Use the comment.
			mergedTopic.AddTagsFrom(commentTopic);

			// FileID - Use the code.
			mergedTopic.FileID = codeTopic.FileID;

			// CommentLineNumber - Use the comment.
			// CodeLineNumber - Use the code.
			mergedTopic.CommentLineNumber = commentTopic.CommentLineNumber;
			mergedTopic.CodeLineNumber = codeTopic.CodeLineNumber;

			// LanguageID - Use the code.
			mergedTopic.LanguageID = codeTopic.LanguageID;

			// PrototypeContext - Use the code.	
			mergedTopic.PrototypeContext = codeTopic.PrototypeContext;

			// BodyContext - Use the code.
			mergedTopic.BodyContext = codeTopic.BodyContext;

			return mergedTopic;
			}

			
		/* Function: RemoveHeaderlessTopics
		 * Deletes any <Topics> which do not have the Title field set, which means they were headerless and they were never merged
		 * with a code topic.  It will remove their <Elements> if they serve no other purpose.
		 */
		protected void RemoveHeaderlessTopics (List<Element> elements)
			{
			int i = 0;
			while (i < elements.Count)
				{
				Element element = elements[i];

				if (element.Topic != null && element.Topic.Title == null)
					{  
					if ((element is ParentElement) == false && element.ClassParentLinks == null)
						{  
						elements.RemoveAt(i);  
						}
					else
						{  
						element.Topic = null;
						i++;  
						}
					}
				else
					{  i++;  }
				}
			}


		/* Function: ApplyFileAndLanguageIDs
		 * Goes through all the <Elements> with <Topics> and applies the FileID and LanguageID properties.  All <Topics> will be set
		 * to the passed FileID, but the LanguageID will be inherited from the <ParentElements>, or set to the default if none of them 
		 * have one.
		 */
		protected void ApplyFileAndLanguageIDs (List<Element> elements, int defaultFileID, int defaultLanguageID)
			{
			for (int i = 0; i < elements.Count; i++)
				{
				Topic topic = elements[i].Topic;

				if (topic != null)
					{
					if (topic.FileID == 0)
						{  topic.FileID = defaultFileID;  }

					if (topic.LanguageID == 0)
						{
						int parentIndex = FindElementParent(elements, i);
						while (parentIndex != -1 && (elements[parentIndex] as ParentElement).DefaultChildLanguageID == 0)
							{  parentIndex = FindElementParent(elements, parentIndex);  }

						if (parentIndex == -1)
							{  topic.LanguageID = defaultLanguageID;  }
						else
							{  topic.LanguageID = (elements[parentIndex] as ParentElement).DefaultChildLanguageID;  }
						}
					}
				}
			}


		/* Function: ApplyDeclaredAccessLevels
		 * Makes sure all <Topics> have DeclaredAccessLevel set.  If one doesn't, it will be retrieved from the first <ParentElement> that
		 * has DefaultDeclaredChildAccessLevel set.  If none do, it will be set to Public.
		 */
		protected void ApplyDeclaredAccessLevels (List<Element> elements)
			{
			for (int i = 0; i < elements.Count; i++)
				{

				// Check for ParentElements that have DefaultDeclaredChildAccessLevel set (meaning it's a parent that affects access
				// levels like a class) but does not have MaximumEffectiveChildAccessLevel set (such as a class that does not have its
				// own declared access level.)  In these cases we set the maximum to its parent's default.

				if (elements[i] is ParentElement)
					{
					ParentElement elementAsParent = (ParentElement)elements[i];

					if (elementAsParent.MaximumEffectiveChildAccessLevel == AccessLevel.Unknown &&
						 elementAsParent.DefaultDeclaredChildAccessLevel != AccessLevel.Unknown)
						{
						int parentIndex = FindElementParent(elements, i);
						while (parentIndex != -1 && (elements[parentIndex] as ParentElement).DefaultDeclaredChildAccessLevel == AccessLevel.Unknown)
							{  parentIndex = FindElementParent(elements, parentIndex);  }

						if (parentIndex != -1)
							{
							elementAsParent.MaximumEffectiveChildAccessLevel = 
								(elements[parentIndex] as ParentElement).DefaultDeclaredChildAccessLevel;
							}
						}
					}


				// Now apply the parent's default to any Topics that do not have a declared access level already set.

				Topic topic = elements[i].Topic;

				if (topic != null && topic.DeclaredAccessLevel == AccessLevel.Unknown)
					{
					int parentIndex = FindElementParent(elements, i);
					while (parentIndex != -1 && (elements[parentIndex] as ParentElement).DefaultDeclaredChildAccessLevel == AccessLevel.Unknown)
						{  parentIndex = FindElementParent(elements, parentIndex);  }

					if (parentIndex == -1)
						{  topic.DeclaredAccessLevel = AccessLevel.Public;  }
					else
						{  topic.DeclaredAccessLevel = (elements[parentIndex] as ParentElement).DefaultDeclaredChildAccessLevel;  }
					}
				}
			}


		/* Function: GenerateEffectiveAccessLevels
		 * Calculates EffectiveAccessLevel for all <Topics> by combining its declared access level with the maximum effective access
		 * levels found it its <ParentElements>.  It assumes all <Topics> already have DeclaredAccessLevel set.
		 */
		protected void GenerateEffectiveAccessLevels (List<Element> elements)
			{
			for (int i = 0; i < elements.Count; i++)
				{
				Topic topic = elements[i].Topic;

				if (topic != null)
					{
					#if DEBUG
					if (topic.DeclaredAccessLevel == AccessLevel.Unknown)
						{  throw new Exception("GenerateEffectiveAccessLevels() requires DeclaredAccessLevel be set on all topics.");  }
					#endif

					topic.EffectiveAccessLevel = topic.DeclaredAccessLevel;

					for (int parentIndex = FindElementParent(elements, i); 
						  parentIndex != -1; 
						  parentIndex = FindElementParent(elements, parentIndex))
						{
						topic.EffectiveAccessLevel = 
							GenerateEffectiveAccessLevel(topic.EffectiveAccessLevel, (elements[parentIndex] as ParentElement).MaximumEffectiveChildAccessLevel);
						}
					}
				}
			}


		/* Function: GenerateEffectiveAccessLevel
		 * Combines the existing access level with the passed maximum and returns the new, limited level.  The current level must be set.  The
		 * maximum level may be unknown in which case it has no effect.
		 */
		protected AccessLevel GenerateEffectiveAccessLevel (AccessLevel current, AccessLevel maximum)
			{
			#if DEBUG
			if (current == AccessLevel.Unknown)
				{  throw new Exception("Can't call GenerateEffectiveAccessLevel() with current set to Unknown.");  }
			#endif


			// We'll use this chart to make sure the logic covers all the bases.
			// ---------------------------
			// ➤ Public + Unknown maximum = Public
			// ➤ Public + Public maximum = Public
			// ☐ Public + Protected maximum = Protected
			// ☐ Public + Internal maximum = Internal
			// ☐ Public + Protected Internal maximum = Protected Internal
			// ☐ Public + Private maximum = Private
			// ---------------------------
			// ➤ Protected + Unknown maximum = Protected
			// ➤ Protected + Public maximum = Protected
			// ☐ Protected + Protected maximum = Protected
			// ☐ Protected + Internal maximum = Internal*
			// ☐ Protected + Protected Internal maximum = Protected
			// ☐ Protected + Private maximum = Private
			// ---------------------------
			// ➤ Internal + Unknown maximum = Internal
			// ➤ Internal + Public maximum = Internal
			// ☐ Internal + Protected maximum = Internal*
			// ☐ Internal + Internal maximum = Internal
			// ☐ Internal + Protected Internal maximum = Internal
			// ☐ Internal + Private maximum = Private
			// ---------------------------
			// ➤ Protected Internal + Unknown maximum = Protected Internal
			// ➤ Protected Internal + Public maximum = Protected Internal
			// ☐ Protected Internal + Protected maximum = Protected
			// ☐ Protected Internal + Internal maximum = Internal
			// ☐ Protected Internal + Protected Internal maximum = Protected Internal
			// ☐ Protected Internal + Private maximum = Private
			// ---------------------------
			// ➤ Private + Unknown maximum = Private
			// ➤ Private + Public maximum = Private
			// ☐ Private + Protected maximum = Private
			// ☐ Private + Internal maximum = Private
			// ☐ Private + Protected Internal maximum = Private
			// ☐ Private + Private maximum = Private
			// ---------------------------
			// * This isn't entirely accurate from a code perspective.  It's really limited to the intersection of protected and internal (as opposed 
			//    to being accessible to the union of protected and internal, which is what the "protected internal" access level is) but we have
			//    to choose one, so internal it is.

			if (maximum == AccessLevel.Unknown || 
				maximum == AccessLevel.Public)
				{  return current;  }


			// ☒ Public + Unknown maximum = Public
			// ☒ Public + Public maximum = Public
			// ➤ Public + Protected maximum = Protected
			// ➤ Public + Internal maximum = Internal
			// ➤ Public + Protected Internal maximum = Protected Internal
			// ➤ Public + Private maximum = Private
			// ---------------------------
			// ☒ Protected + Unknown maximum = Protected
			// ☒ Protected + Public maximum = Protected
			// ☐ Protected + Protected maximum = Protected
			// ☐ Protected + Internal maximum = Internal*
			// ☐ Protected + Protected Internal maximum = Protected
			// ☐ Protected + Private maximum = Private
			// ---------------------------
			// ☒ Internal + Unknown maximum = Internal
			// ☒ Internal + Public maximum = Internal
			// ☐ Internal + Protected maximum = Internal*
			// ☐ Internal + Internal maximum = Internal
			// ☐ Internal + Protected Internal maximum = Internal
			// ☐ Internal + Private maximum = Private
			// ---------------------------
			// ☒ Protected Internal + Unknown maximum = Protected Internal
			// ☒ Protected Internal + Public maximum = Protected Internal
			// ☐ Protected Internal + Protected maximum = Protected
			// ☐ Protected Internal + Internal maximum = Internal
			// ☐ Protected Internal + Protected Internal maximum = Protected Internal
			// ☐ Protected Internal + Private maximum = Private
			// ---------------------------
			// ☒ Private + Unknown maximum = Private
			// ☒ Private + Public maximum = Private
			// ☐ Private + Protected maximum = Private
			// ☐ Private + Internal maximum = Private
			// ☐ Private + Protected Internal maximum = Private
			// ☐ Private + Private maximum = Private

			else if (current == AccessLevel.Public)
				{  return maximum;  }


			// ☒ Public + Unknown maximum = Public
			// ☒ Public + Public maximum = Public
			// ☒ Public + Protected maximum = Protected
			// ☒ Public + Internal maximum = Internal
			// ☒ Public + Protected Internal maximum = Protected Internal
			// ☒ Public + Private maximum = Private
			// ---------------------------
			// ☒ Protected + Unknown maximum = Protected
			// ☒ Protected + Public maximum = Protected
			// ☐ Protected + Protected maximum = Protected
			// ☐ Protected + Internal maximum = Internal*
			// ☐ Protected + Protected Internal maximum = Protected
			// ➤ Protected + Private maximum = Private
			// ---------------------------
			// ☒ Internal + Unknown maximum = Internal
			// ☒ Internal + Public maximum = Internal
			// ☐ Internal + Protected maximum = Internal*
			// ☐ Internal + Internal maximum = Internal
			// ☐ Internal + Protected Internal maximum = Internal
			// ➤ Internal + Private maximum = Private
			// ---------------------------
			// ☒ Protected Internal + Unknown maximum = Protected Internal
			// ☒ Protected Internal + Public maximum = Protected Internal
			// ☐ Protected Internal + Protected maximum = Protected
			// ☐ Protected Internal + Internal maximum = Internal
			// ☐ Protected Internal + Protected Internal maximum = Protected Internal
			// ➤ Protected Internal + Private maximum = Private
			// ---------------------------
			// ☒ Private + Unknown maximum = Private
			// ☒ Private + Public maximum = Private
			// ➤ Private + Protected maximum = Private
			// ➤ Private + Internal maximum = Private
			// ➤ Private + Protected Internal maximum = Private
			// ➤ Private + Private maximum = Private

			else if (current == AccessLevel.Private ||
					  maximum == AccessLevel.Private)
				{  return AccessLevel.Private;  }


			// ☒ Public + Unknown maximum = Public
			// ☒ Public + Public maximum = Public
			// ☒ Public + Protected maximum = Protected
			// ☒ Public + Internal maximum = Internal
			// ☒ Public + Protected Internal maximum = Protected Internal
			// ☒ Public + Private maximum = Private
			// ---------------------------
			// ☒ Protected + Unknown maximum = Protected
			// ☒ Protected + Public maximum = Protected
			// ☐ Protected + Protected maximum = Protected
			// ➤ Protected + Internal maximum = Internal*
			// ☐ Protected + Protected Internal maximum = Protected
			// ☒ Protected + Private maximum = Private
			// ---------------------------
			// ☒ Internal + Unknown maximum = Internal
			// ☒ Internal + Public maximum = Internal
			// ➤ Internal + Protected maximum = Internal*
			// ➤ Internal + Internal maximum = Internal
			// ➤ Internal + Protected Internal maximum = Internal
			// ☒ Internal + Private maximum = Private
			// ---------------------------
			// ☒ Protected Internal + Unknown maximum = Protected Internal
			// ☒ Protected Internal + Public maximum = Protected Internal
			// ☐ Protected Internal + Protected maximum = Protected
			// ➤ Protected Internal + Internal maximum = Internal
			// ☐ Protected Internal + Protected Internal maximum = Protected Internal
			// ☒ Protected Internal + Private maximum = Private
			// ---------------------------
			// ☒ Private + Unknown maximum = Private
			// ☒ Private + Public maximum = Private
			// ☒ Private + Protected maximum = Private
			// ☒ Private + Internal maximum = Private
			// ☒ Private + Protected Internal maximum = Private
			// ☒ Private + Private maximum = Private

			else if (current == AccessLevel.Internal ||
					  maximum == AccessLevel.Internal)
				{  return AccessLevel.Internal;  }


			// ☒ Public + Unknown maximum = Public
			// ☒ Public + Public maximum = Public
			// ☒ Public + Protected maximum = Protected
			// ☒ Public + Internal maximum = Internal
			// ☒ Public + Protected Internal maximum = Protected Internal
			// ☒ Public + Private maximum = Private
			// ---------------------------
			// ☒ Protected + Unknown maximum = Protected
			// ☒ Protected + Public maximum = Protected
			// ➤ Protected + Protected maximum = Protected
			// ☒ Protected + Internal maximum = Internal*
			// ➤ Protected + Protected Internal maximum = Protected
			// ☒ Protected + Private maximum = Private
			// ---------------------------
			// ☒ Internal + Unknown maximum = Internal
			// ☒ Internal + Public maximum = Internal
			// ☒ Internal + Protected maximum = Internal*
			// ☒ Internal + Internal maximum = Internal
			// ☒ Internal + Protected Internal maximum = Internal
			// ☒ Internal + Private maximum = Private
			// ---------------------------
			// ☒ Protected Internal + Unknown maximum = Protected Internal
			// ☒ Protected Internal + Public maximum = Protected Internal
			// ➤ Protected Internal + Protected maximum = Protected
			// ☒ Protected Internal + Internal maximum = Internal
			// ☐ Protected Internal + Protected Internal maximum = Protected Internal
			// ☒ Protected Internal + Private maximum = Private
			// ---------------------------
			// ☒ Private + Unknown maximum = Private
			// ☒ Private + Public maximum = Private
			// ☒ Private + Protected maximum = Private
			// ☒ Private + Internal maximum = Private
			// ☒ Private + Protected Internal maximum = Private
			// ☒ Private + Private maximum = Private

			else if (current == AccessLevel.Protected||
					  maximum == AccessLevel.Protected)
				{  return AccessLevel.Protected;  }


			// ☒ Public + Unknown maximum = Public
			// ☒ Public + Public maximum = Public
			// ☒ Public + Protected maximum = Protected
			// ☒ Public + Internal maximum = Internal
			// ☒ Public + Protected Internal maximum = Protected Internal
			// ☒ Public + Private maximum = Private
			// ---------------------------
			// ☒ Protected + Unknown maximum = Protected
			// ☒ Protected + Public maximum = Protected
			// ☒ Protected + Protected maximum = Protected
			// ☒ Protected + Internal maximum = Internal*
			// ☒ Protected + Protected Internal maximum = Protected
			// ☒ Protected + Private maximum = Private
			// ---------------------------
			// ☒ Internal + Unknown maximum = Internal
			// ☒ Internal + Public maximum = Internal
			// ☒ Internal + Protected maximum = Internal*
			// ☒ Internal + Internal maximum = Internal
			// ☒ Internal + Protected Internal maximum = Internal
			// ☒ Internal + Private maximum = Private
			// ---------------------------
			// ☒ Protected Internal + Unknown maximum = Protected Internal
			// ☒ Protected Internal + Public maximum = Protected Internal
			// ☒ Protected Internal + Protected maximum = Protected
			// ☒ Protected Internal + Internal maximum = Internal
			// ➤ Protected Internal + Protected Internal maximum = Protected Internal
			// ☒ Protected Internal + Private maximum = Private
			// ---------------------------
			// ☒ Private + Unknown maximum = Private
			// ☒ Private + Public maximum = Private
			// ☒ Private + Protected maximum = Private
			// ☒ Private + Internal maximum = Private
			// ☒ Private + Protected Internal maximum = Private
			// ☒ Private + Private maximum = Private

			else
				{  return AccessLevel.ProtectedInternal;  }
			}


		/* Function: ApplyTags
		 * Makes sure all <Topics> inherit the tags of their parents.
		 */
		protected void ApplyTags (List<Element> elements)
			{
			for (int i = 0; i < elements.Count; i++)
				{
				Topic topic = elements[i].Topic;

				if (topic != null)
					{
					for (int parentIndex = FindElementParent(elements, i);
						  parentIndex != -1;
						  parentIndex = FindElementParent(elements, parentIndex))
						{
						if (elements[parentIndex].Topic != null)
							{  topic.AddTagsFrom(elements[parentIndex].Topic);  }
						}
					}
				}
			}


		/* Function: GenerateRemainingSymbols
		 * 
		 * Finds any <Topics> that don't have their symbols set and generates them.  It will also generate <ClassStrings> and
		 * <ParentElement.ChildContextStrings> when appropriate.
		 * 
		 * Requirements:
		 * 
		 *		- This function assumes that all code element <Topics> have symbols and the only ones without them appear in the 
		 *		  comments only.
		 *		- This function assumes all headerless <Topics> have already been removed.
		 *		- This function assumes all <Topics> have LanguageID set.
		 */
		protected void GenerateRemainingSymbols (List<Element> elements)
			{
			#if DEBUG
			foreach (var element in elements)
				{
				if (element.Topic != null)
					{
					if (element.Topic.Symbol == null && (element.InCode == true || element.InComments == false))
						{  throw new Exception("Only comment topics may have undefined symbols in GenerateRemainingSymbols().");  }
					if (element.Topic.Title == null)
						{  throw new Exception("Headerless topics must be removed before calling GenerateRemainingSymbols().");  }
					if (element.Topic.LanguageID == 0)
						{  throw new Exception("All topics must have LanguageID set before calling GenerateRemainingSymbols().");  }
					}
				}
			#endif

			for (int i = 0; i < elements.Count; i++)
				{
				Element element = elements[i];
				Topic topic = element.Topic;

				if (topic != null && topic.Symbol == null)
					{

					// Gather information

					TopicType topicType = Engine.Instance.TopicTypes.FromID(topic.TopicTypeID);

					string ignore;
					SymbolString topicSymbol = SymbolString.FromPlainText(topic.Title, out ignore);

					int parentIndex = FindElementParent(elements, i);
					while (parentIndex != -1 && (elements[parentIndex] as ParentElement).ChildContextStringSet == false)
						{  parentIndex = FindElementParent(elements, parentIndex);  }

					ContextString parentContext;
					if (parentIndex == -1)
						{  parentContext = new ContextString();  }
					else
						{  parentContext = (elements[parentIndex] as ParentElement).ChildContextString;  }


					// Set Topic.Symbol

					if (topicType.Scope == TopicType.ScopeValue.Normal)
						{  topic.Symbol = parentContext.Scope + topicSymbol;  }
					else // Scope is Start, End, or AlwaysGlobal
						{  topic.Symbol = topicSymbol;  }


					// Set Topic.ClassString and ParentElement.DefaultChildClassString if appropriate

					if (topicType.Scope == TopicType.ScopeValue.Start && 
						  (topicType.Flags.ClassHierarchy == true || topicType.Flags.DatabaseHierarchy == true) )
						{
						ClassString.HierarchyType hierarchyType = (topicType.Flags.ClassHierarchy ? 
																												ClassString.HierarchyType.Class :
																												ClassString.HierarchyType.Database);
						Language language = Engine.Instance.Languages.FromID(topic.LanguageID);

						ClassString classString = ClassString.FromParameters(hierarchyType, language.ID, language.CaseSensitive, topicSymbol);

						topic.ClassString = classString;

						// Someone could have documented classes as a list, so it's not guaranteed to be a ParentElement as this may be a list
						// member.  Also, DefaultChildClassString wouldn't be relevant for the list topic itself.
						if (element is ParentElement && topic.IsList == false)
							{  (element as ParentElement).DefaultChildClassString = classString;  }
						}


					// Set ParentElement.ChildContextString if appropriate.  We don't want to copy using statements from parents
					// though, that will be handled by ApplyUsingStatements().

					if (element is ParentElement && topic.IsList == false)
						{
						EnumValues enumValue = 0;
						if (topicType.Flags.Enum == true)
							{  enumValue = Engine.Instance.Languages.FromID(topic.LanguageID).EnumValue;  }
							
						if (topicType.Scope == TopicType.ScopeValue.Start ||
						    (topicType.Flags.Enum == true && enumValue == EnumValues.UnderType))
							{
							ContextString newContext = new ContextString();
							newContext.Scope = topic.Symbol;
							(element as ParentElement).ChildContextString = newContext;
							}
						else if (topicType.Scope == TopicType.ScopeValue.End ||
								  (topicType.Flags.Enum == true && enumValue == EnumValues.Global))
							{
							(element as ParentElement).ChildContextString = new ContextString();
							}
						else if (topicType.Flags.Enum == true && enumValue == EnumValues.UnderParent)
							{
							ContextString newContext = new ContextString();
							newContext.Scope = parentContext.Scope;
							(element as ParentElement).ChildContextString = newContext;
							}
						// otherwise don't set ChildContextString
						}
					}
				}
			}


		/* Function: ApplyUsingStatements
		 * Fills in each <ParentElement.ChildContextStrings's> using statements by combining them with every parents' statements.
		 */
		protected void ApplyUsingStatements (List<Element> elements)
			{
			for (int i = 0; i < elements.Count; i++)
				{
				if ((elements[i] is ParentElement) == false)
					{  continue;  }

				ParentElement elementAsParent = (ParentElement)elements[i];

				if (elementAsParent.ChildContextStringSet == false)
					{  continue;  }

				int parentIndex = FindElementParent(elements, i);
				while (parentIndex != -1 && (elements[parentIndex] as ParentElement).ChildContextStringSet == false)
					{  parentIndex = FindElementParent(elements, parentIndex);  }

				if (parentIndex == -1)
					{  continue;  }

				ParentElement parentElement = (ParentElement)elements[parentIndex];
				
				ContextString temp = elementAsParent.ChildContextString;
				temp.InheritUsingStatementsFrom(parentElement.ChildContextString);
				elementAsParent.ChildContextString = temp;
				}
			}


		/* Function: ApplyContexts
		 * Fills in each <Topic's> PrototypeContext and BodyContext settings.  For <ParentElements> the prototype will use next
		 * higher <ContextString> and the body will use its own.  For other <Elements> they will both use their parents' contexts.
		 */
		protected void ApplyContexts (List<Element> elements)
			{
			for (int i = 0; i < elements.Count; i++)
				{
				Element element = elements[i];

				if (element.Topic == null)
					{  continue;  }

				ContextString parentContext;

				int parentIndex = FindElementParent(elements, i);
				while (parentIndex != -1 && (elements[parentIndex] as ParentElement).ChildContextStringSet == false)
					{  parentIndex = FindElementParent(elements, parentIndex);  }

				if (parentIndex == -1)
					{  parentContext = new ContextString();  }
				else
					{  parentContext = (elements[parentIndex] as ParentElement).ChildContextString;  }

				if (element is ParentElement && (element as ParentElement).ChildContextStringSet == true)
					{
					element.Topic.PrototypeContext = parentContext;
					element.Topic.BodyContext = (element as ParentElement).ChildContextString;
					}
				else
					{
					element.Topic.PrototypeContext = parentContext;
					element.Topic.BodyContext = parentContext;
					}
				}
			}


		/* Function: ApplyClassStrings
		 * Makes sure each <Topic's> <ClassString> is set.  If any <Topic> doesn't have one it will search its <ParentElements>
		 * for a default.
		 */
		protected void ApplyClassStrings (List<Element> elements)
			{
			for (int i = 0; i < elements.Count; i++)
				{
				Element element = elements[i];

				if (element.Topic == null || element.Topic.ClassString != null)
					{  continue;  }

				int parentIndex = FindElementParent(elements, i);
				while (parentIndex != -1 && (elements[parentIndex] as ParentElement).DefaultChildClassStringSet == false)
					{  parentIndex = FindElementParent(elements, parentIndex);  }

				if (parentIndex != -1)
					{  element.Topic.ClassString = (elements[parentIndex] as ParentElement).DefaultChildClassString;  }
				}
			}


		/* Function: FindElementParent
		 * Returns the index of the element's immediate parent, or -1 if there isn't one.
		 */
		protected int FindElementParent (List<Element> elements, int elementIndex)
			{
			Element element = elements[elementIndex];

			for (int i = elementIndex - 1; i >= 0; i--)
				{
				if (elements[i] is ParentElement && (elements[i] as ParentElement).Contains(element))
					{  return i;  }
				}

			return -1;
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
		 * 
		 * If the iterator is on whitespace or a comment, move past it and return true.
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight> (for comments)
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipWhitespace (ref TokenIterator iterator, bool includeLineBreaks = true, ParseMode mode = ParseMode.IterateOnly)
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
				else if (TryToSkipComment(ref iterator, mode))
					{  success = true;  }
				else
					{  break;  }
				}

			return success;
			}


		/* Function: TryToSkipComment
		 * 
		 * If the iterator is on a comment symbol, moves it past the entire comment and returns true.  If you need information 
		 * about  the specific type of comment it was, you need to call <TryToSkipLineComment()> and <TryToSkipBlockComment()> 
		 * individually.
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
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipComment (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			return ( TryToSkipLineComment(ref iterator, mode) || TryToSkipBlockComment(ref iterator, mode) );
			}


		/* Function: TryToSkipLineComment
		 * 
		 * If the iterator is on a line comment symbol, moves it past the entire comment, provides the symbol that was used, and 
		 * returns true.  It will not skip the line break after the comment since that may be relevant to the calling code.
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipLineComment (ref TokenIterator iterator, out string commentSymbol, ParseMode mode = ParseMode.IterateOnly)
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

			TokenIterator startOfComment = iterator;
			iterator.NextByCharacters(commentSymbol.Length);

			while (iterator.IsInBounds && iterator.FundamentalType != FundamentalType.LineBreak)
				{  iterator.Next();  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.Tokenizer.SetSyntaxHighlightingTypeBetween(startOfComment, iterator, SyntaxHighlightingType.Comment);  }

			return true;
			}


		/* Function: TryToSkipLineComment
		 * 
		 * If the iterator is on a line comment symbol, moves it past the entire comment and returns true.  It will not skip the line break 
		 * after the comment since that may be relevant to the calling code.
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipLineComment (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			string ignore;
			return TryToSkipLineComment(ref iterator, out ignore, mode);
			}


		/* Function: TryToSkipBlockComment
		 * 
		 * If the iterator is on an opening block comment symbol, moves it past the entire comment, provides the comment symbols that 
		 * were used, and returns true.
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipBlockComment (ref TokenIterator iterator, out string openingSymbol, out string closingSymbol, 
															  ParseMode mode = ParseMode.IterateOnly)
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

			TokenIterator startOfComment = iterator;
			iterator.NextByCharacters(openingSymbol.Length);

			while (iterator.IsInBounds && iterator.MatchesAcrossTokens(closingSymbol) == false)
				{  iterator.Next();  }

			if (iterator.IsInBounds)
				{  iterator.NextByCharacters(closingSymbol.Length);  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.Tokenizer.SetSyntaxHighlightingTypeBetween(startOfComment, iterator, SyntaxHighlightingType.Comment);  }

			// Return true even if the iterator reached the end of the content before finding a closing symbol.
			return true;
			}


		/* Function: TryToSkipBlockComment
		 * 
		 * If the iterator is on an opening block comment symbol, moves it past the entire comment and returns true.
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipBlockComment (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			string ignore1, ignore2;
			return TryToSkipBlockComment (ref iterator, out ignore1, out ignore2, mode);
			}


		/* Function: TryToSkipString
		 * 
		 * If the iterator is on a quote or apostrophe, moves it past the entire string and returns true.
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
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipString (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '"' && iterator.Character != '\'')
				{  return false;  }

			char quoteCharacter = iterator.Character;

			TokenIterator startOfString = iterator;
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

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.Tokenizer.SetSyntaxHighlightingTypeBetween(startOfString, iterator, SyntaxHighlightingType.String);  }

			// Return true even if the iterator reached the end of the content before finding a closing quote.
			return true;
			}


		/* Function: TryToSkipNumber
		 * 
		 * If the iterator is on a numeric literal, moves the iterator past it and returns true.
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipNumber (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if ( ((iterator.Character >= '0' && iterator.Character <= '9') || iterator.Character == '-' || iterator.Character == '.') == false)
				{  return false;  }

			TokenIterator lookahead = iterator;
			bool passedPeriod = false;
			bool lastCharWasE = false;
			bool isHex = false;

			if (lookahead.Character == '-')
				{  
				// Distinguish between -1 and x-1

				TokenIterator lookbehind = iterator;
				lookbehind.Previous();

				lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator);

				if (lookbehind.FundamentalType == FundamentalType.Text || lookbehind.Character == '_')
					{  return false;  }

				lookahead.Next();  
				}

			if (lookahead.Character == '.')
				{  
				lookahead.Next();  
				passedPeriod = true;
				}

			if (lookahead.Character >= '0' && lookahead.Character <= '9')
				{
				if (lookahead.Character == '0' && lookahead.RawTextLength > 1)
					{
					char secondChar = iterator.Tokenizer.RawText[ lookahead.RawTextIndex + 1 ];
					isHex = (secondChar == 'x' || secondChar == 'X');
					}

				lookahead.Next();

				char lastChar = iterator.Tokenizer.RawText[ lookahead.RawTextIndex - 1 ];
				lastCharWasE = (lastChar == 'e' || lastChar == 'E');
				}
			else
				{  return false;  }

			// We're definitely on a number, so apply the position in case the later lookaheads fail.
			TokenIterator startOfNumber = iterator;
			iterator = lookahead;

			if (lookahead.Character == '.' && !passedPeriod)
				{
				lookahead.Next();

				if (lookahead.Character >= '0' && lookahead.Character <= '9')
					{
					lookahead.Next();
					passedPeriod = true;

					char lastChar = iterator.Tokenizer.RawText[ lookahead.RawTextIndex - 1 ];
					lastCharWasE = (lastChar == 'e' || lastChar == 'E');
					}
				else
					{  lookahead = iterator;  }
				}

			if (lastCharWasE && !isHex && (lookahead.Character == '-' || lookahead.Character == '+'))
				{
				lookahead.Next();

				if (lookahead.Character >= '0' && lookahead.Character <= '9')
					{
					lookahead.Next();
					iterator = lookahead;
					}
				else
					{  lookahead = iterator;  }
				}

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.Tokenizer.SetSyntaxHighlightingTypeBetween(startOfNumber, iterator, SyntaxHighlightingType.Number);  }

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
				if (TryToSkipComment(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipString(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipNumber(ref iterator, ParseMode.SyntaxHighlight))
					{
					}
				else if (iterator.FundamentalType == FundamentalType.Text || iterator.Character == '_')
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
				else
					{  iterator.Next();  }
				}
			}


		/* Function: ValidateElements
		 * Validates a list of <Elements> to make sure all the properties are set correctly, throwing an exception if not.  This
		 * does nothing in non-debug builds.
		 */
		protected void ValidateElements (List<Element> elements, ValidateElementsMode mode)
			{
			#if DEBUG

			int lastLineNumber = 0;
			int lastCharNumber = 0;

			for (int i = 0; i < elements.Count; i++)
				{
				Element element = elements[i];


				// Generate a name for error messages

				string elementName = "(line " + element.LineNumber + ", char " + element.CharNumber + ")";

				if (element.Topic != null && element.Topic.Title != null)
					{  elementName = element.Topic.Title + " " + elementName;  }


				// Make sure they're in order

				if (element.LineNumber < lastLineNumber ||
					 (element.LineNumber == lastLineNumber && element.CharNumber < lastCharNumber))
					{  
					throw new Exception("Element " + elementName + " doesn't appear in order.  " +
															 "The previous element was at line " + lastLineNumber + ", char " + lastCharNumber + ".");  
					}

				lastLineNumber = element.LineNumber;
				lastCharNumber = element.CharNumber;


				if (element is ParentElement)
					{  
					ParentElement elementAsParent = (ParentElement)element;  


					// Make sure root only applies to the first element.

					if (elementAsParent.IsRootElement == true && i != 0)
						{  
						throw new Exception("IsRootElement was set on " + elementName + " which is at position " + i + " in the list.  " +
																 "IsRootElement can only be set on the first member of a list.");  
						}


					// Make sure parents have their ending values set.

					if (elementAsParent.EndingLineNumber == -1 || elementAsParent.EndingCharNumber == -1)
						{  throw new Exception(elementName + " did not have its ending position properties set.");  }

					if (elementAsParent.EndingLineNumber < elementAsParent.LineNumber ||
						 (elementAsParent.EndingLineNumber == elementAsParent.LineNumber &&
						  elementAsParent.EndingCharNumber < elementAsParent.CharNumber))
						{  throw new Exception(elementName + "'s ending position was before its starting position.");  }


					// Make sure ranges don't overlap badly.

					for (int j = 0; j < i; j++)
						{
						if (elements[j] is ParentElement)
							{
							ParentElement previousParent = (ParentElement)elements[j];

							// We only have to check if we're before the end of the previous parent to know we overlap.  We already made sure
							// the elements are in order so this range can't be before the previous one.
							if (elementAsParent.LineNumber < previousParent.EndingLineNumber ||
								 (elementAsParent.LineNumber == previousParent.EndingLineNumber &&
								  elementAsParent.CharNumber < previousParent.EndingCharNumber))
								{
								if (elementAsParent.EndingLineNumber > previousParent.EndingLineNumber ||
									 (elementAsParent.EndingLineNumber == previousParent.EndingLineNumber &&
									  elementAsParent.EndingCharNumber > previousParent.EndingCharNumber))
									{
									string previousParentName = "(line " + previousParent.LineNumber + ", char " + previousParent.CharNumber + ")";

									if (previousParent.Topic != null && previousParent.Topic.Title != null)
										{  previousParentName = previousParent.Topic.Title + " " + previousParentName;  }

									throw new Exception(elementName + " starts in " + previousParentName + "'s range but extends past it.");
									}
								}
							}
						}
					}


				if (mode == ValidateElementsMode.CodeElements && element.Topic != null)
					{
					if (element.Topic.Title == null)
						{  throw new Exception("All topics returned by GetCodeElements() must have titles.");  }
					if (element.Topic.Symbol == null)
						{  throw new Exception("All topics returned by GetCodeElements() must have symbols.");  }
					if (element.Topic.TopicTypeID == 0)
						{  throw new Exception("All topics returned by GetCodeElements() must have topic type IDs.");  }
					if (element.Topic.IsList)
						{  throw new Exception("GetCodeElements() cannot return list topics.");  }
					if (element.Topic.IsEmbedded)
						{  throw new Exception("GetCodeElements() cannot return embedded topics.");  }
					}

				if ((mode == ValidateElementsMode.CommentElements || 
					 mode == ValidateElementsMode.MergedElements ||
					 mode == ValidateElementsMode.Final) &&
					element.Topic != null)
					{
					if (element.Topic.IsList || element.Topic.IsEnum)
						{
						int bodyEmbeddedTopics = 0;

						if (element.Topic.Body != null)
							{
							for (int dsIndex = element.Topic.Body.IndexOf("<ds>");
								  dsIndex != -1;
								  dsIndex = element.Topic.Body.IndexOf("<ds>", dsIndex + 4))
								{  bodyEmbeddedTopics++;  }
							}

						int elementEmbeddedTopics = 0;

						for (int j = i + 1; j < elements.Count; j++)
							{
							if (elements[j].Topic != null && elements[j].Topic.IsEmbedded)
								{  elementEmbeddedTopics++;  }
							else
								{  break;  }
							}

						if (bodyEmbeddedTopics != elementEmbeddedTopics)
							{
							throw new Exception("Element " + elementName + " had " + bodyEmbeddedTopics + " embedded topics in its body but " +
														  elementEmbeddedTopics + " embedded topics following it in the elements.");
							}
						}

					if (element.Topic.IsEmbedded)
						{
						bool foundParent = false;

						for (int j = i - 1; j >= 0; j--)
							{
							if (elements[j].Topic == null)
								{  break;  }
							if (elements[j].Topic.IsEmbedded == false)
								{
								foundParent = (elements[j].Topic.IsList || elements[j].Topic.IsEnum);
								break;
								}
							}

						if (foundParent == false)
							{  throw new Exception("Embedded element " + elementName + " did not follow a list or enum topic.");  }
						}
					}

				if (mode == ValidateElementsMode.Final && element.Topic != null)
					{
					Topic topic = element.Topic;
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
				}

			#endif
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

			"class", "struct", "interface", "template", "package", "union", "namespace", "using",

			"base", "inherit", "inherits", "extend", "extends", "implement", "implements", 
			"import", "export", "extern", "native", "override", "overload", "explicit", "implicit",
			"super", "my", "our", "require", "this",

			"public", "private", "protected", "internal", "static", "virtual", "abstract", "friend", 
			"inline", "using", "final", "sealed", "register", "volatile",

			"ref", "in", "out", "inout", "const", "constant", "get", "set",

			"if", "else", "elif", "elseif", "then", "for", "foreach", "each", "do", "while", "switch", "case", "with", "in",
			"break", "continue", "next", "return", "goto",
			"try", "catch", "throw", "finally", "throws", "lock", "eval",

			"new", "delete", "sizeof", "typeof"
			});


		/* var: inheritanceKeywords
		 * A list of default keywords for searching for class parents across all languages.
		 */
		static protected string[] inheritanceKeywords = {
			"base",
			"inherit", "inherits",
			"extend", "extends",
			"implement", "implements"
			};


		static protected Regex.Comments.AcceptablePrototypeComments acceptablePrototypeCommentRegex
			= new Regex.Comments.AcceptablePrototypeComments();

		static protected Regex.Languages.ExtraOperatorWhitespace extraOperatorWhitespaceRegex
			= new Regex.Languages.ExtraOperatorWhitespace();

		}
	}