/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.CommentFinder
 * ____________________________________________________________________________
 * 
 * A class that handles finding possible documenatation comments in any source file in a generic way.
 * 
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		Once the object is set up, meaning there will be no further changes to properties like <LineCommentStrings>,
 *		the object can be used by multiple threads to parse multiple files simultaneously.  The parsing functions store
 *		no state information inside the object.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Comments;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages
	{
	public class CommentFinder : IDObjects.Base
		{
				
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: CommentFinder
		 */
		public CommentFinder (string name) : base ()
			{
			this.name = name;

			lineCommentStrings = null;
			blockCommentStringPairs = null;
			javadocLineCommentStringPairs = null;
			javadocBlockCommentStringPairs = null;
			xmlLineCommentStrings = null;
			}


		/* Function: GenerateJavadocCommentStrings
		 * If they're not already defined, generate <JavadocLineCommentStringPairs> and <JavadocBlockCommentStringPairs>
		 * from <LineCommentStrings> and <BlockCommentStringPairs>.
		 */
		public void GenerateJavadocCommentStrings ()
			{
			if (javadocBlockCommentStringPairs == null && blockCommentStringPairs != null)
				{
				int count = 0;

				for (int i = 0; i < blockCommentStringPairs.Length; i += 2)
					{
					// We only accept strings like /* */ and (* *).  Anything else doesn't get it.
					if (blockCommentStringPairs[i].Length == 2 && 
						 blockCommentStringPairs[i+1].Length == 2 &&
						 blockCommentStringPairs[i][1] == '*' &&
						 blockCommentStringPairs[i+1][0] == '*')
						{  count++;  }
					}

				if (count > 0)
					{
					javadocBlockCommentStringPairs = new string[count * 2];
					int javadocIndex = 0;

					for (int i = 0; i < blockCommentStringPairs.Length; i += 2)
						{
						if (blockCommentStringPairs[i].Length == 2 && 
							 blockCommentStringPairs[i+1].Length == 2 &&
							 blockCommentStringPairs[i][1] == '*' &&
							 blockCommentStringPairs[i+1][0] == '*')
							{  
							javadocBlockCommentStringPairs[javadocIndex] = blockCommentStringPairs[i] + '*';
							javadocBlockCommentStringPairs[javadocIndex+1] = blockCommentStringPairs[i+1];
							javadocIndex += 2;
							}
						}
					}
				}

			if (javadocLineCommentStringPairs == null && lineCommentStrings != null)
				{
				javadocLineCommentStringPairs = new string[lineCommentStrings.Length * 2];

				for (int i = 0; i < lineCommentStrings.Length; i++)
					{
					javadocLineCommentStringPairs[i*2] = lineCommentStrings[i] + lineCommentStrings[i][ lineCommentStrings[i].Length - 1 ];
					javadocLineCommentStringPairs[(i*2)+1] = lineCommentStrings[i];
					}
				}
			}


		/* Function: GenerateXMLCommentStrings
		 * If they're not already defined, generate <XMLLineCommentStrings> from <LineCommentStrings>.
		 */
		public void GenerateXMLCommentStrings ()
			{
			if (xmlLineCommentStrings == null && lineCommentStrings != null)
				{
				xmlLineCommentStrings = new string[lineCommentStrings.Length];

				for (int i = 0; i < lineCommentStrings.Length; i++)
					{
					// If it's only one character, turn it to three like ''' in Visual Basic.
					if (lineCommentStrings[i].Length == 1)
						{  xmlLineCommentStrings[i] = lineCommentStrings[i] + lineCommentStrings[i][0] + lineCommentStrings[i];  }

					// Otherwise just duplicate the last charater like /// in C#.
					else
						{  xmlLineCommentStrings[i] = lineCommentStrings[i] + lineCommentStrings[i][ lineCommentStrings[i].Length - 1 ];  }
					}
				}
			}


		/* Function: GetPossibleDocumentationComments
		 * 
		 * 	Goes through the source looking for comments that could possibly contain documentation and returns them as a list.  These
		 * 	comments are not guaranteed to have documentation in them, just to be acceptable candidates for them.  If there are no
		 * 	comments it will return an empty list.
		 * 	
		 * 	All the comments in the returned list will have their comment symbols marked as <CommentParsingType.CommentSymbol>
		 * 	in the tokenizer.  This allows further operations to be done on them in a language independent manner.  If you want to also
		 * 	filter out text boxes and lines, use <Comments.LineFinder>.
		 * 	
		 * 	If you already have the source code in tokenized form it would be more efficient to pass it as a <Tokenizer>.
		 */
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
		// filter out text boxes and lines, use <Comments.LineFinder>.
		//
		// Default Implementation:
		//
		// The default implementation uses the comment symbols found in <Language> or passed to the constructor.  You can override
		// this function if you need to do something more sophisticated, such as interpret the POD directives in Perl.
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

			LineIterator lineIterator = source.FirstLine;

			while (lineIterator.IsInBounds)
				{
				bool foundComment = false;
				PossibleDocumentationComment possibleDocumentationComment = null;
				
				
				// Javadoc block comments

				// We test for these before regular block comments because they are usually extended versions of them, such
				// as /** and /*.

				if (javadocBlockCommentStringPairs != null)
					{
					for (int i = 0; foundComment == false && i < javadocBlockCommentStringPairs.Length; i += 2)
						{
						foundComment = TryToGetBlockComment(ref lineIterator, 
																					 javadocBlockCommentStringPairs[i], javadocBlockCommentStringPairs[i+1], true,
																					 out possibleDocumentationComment);
						}

					if (possibleDocumentationComment != null)
						{  possibleDocumentationComment.Javadoc = true;  }
					}
					
					
				// Plain block comments
					
				// We test block comments ahead of line comments because in Lua the line comments are a substring of them: --
				// versus --[[ and ]]--.

				if (foundComment == false && blockCommentStringPairs != null)
					{
					for (int i = 0; foundComment == false && i < blockCommentStringPairs.Length; i += 2)
						{
						foundComment = TryToGetBlockComment(ref lineIterator, 
																					 blockCommentStringPairs[i], blockCommentStringPairs[i+1], false,
																					 out possibleDocumentationComment);
						}

					// Skip Splint comments so that they can appear in prototypes.
					if (possibleDocumentationComment != null && 
						possibleDocumentationComment.Start.FirstToken(LineBoundsMode.CommentContent).Character == '@')
						{
						LineIterator lastLine = possibleDocumentationComment.End;
						lastLine.Previous();

						TokenIterator lastToken, ignore;
						lastLine.GetBounds(LineBoundsMode.CommentContent, out ignore, out lastToken);
						lastToken.Previous();

						if (lastToken.Character == '@')
							{  possibleDocumentationComment = null;  }
						}
					}
					
					
				// XML line comments

				if (foundComment == false && xmlLineCommentStrings != null)
					{
					for (int i = 0; foundComment == false && i < xmlLineCommentStrings.Length; i++)
						{
						foundComment = TryToGetLineComment(ref lineIterator, 
																				   xmlLineCommentStrings[i], xmlLineCommentStrings[i], true,
																				   out possibleDocumentationComment);
						}

					if (possibleDocumentationComment != null)
						{  possibleDocumentationComment.XML = true;  }
					}
						
						
				// Ambiguous XML/Javadoc line comments

				// If an XML comment is found we check the same position for Javadoc because they may share an opening 
				// symbol, such as ///.

				if (possibleDocumentationComment != null && possibleDocumentationComment.XML == true &&
					javadocLineCommentStringPairs != null)
					{
					LineIterator javadocLineIterator = possibleDocumentationComment.Start;
					PossibleDocumentationComment possibleJavadocDocumentationComment = null;
					bool foundJavadocComment = false;

					for (int i = 0; foundJavadocComment == false && i < javadocLineCommentStringPairs.Length; i += 2)
						{
						foundJavadocComment = TryToGetLineComment(ref javadocLineIterator, 
																							  javadocLineCommentStringPairs[i], javadocLineCommentStringPairs[i+1], true,
																							  out possibleJavadocDocumentationComment);
						}

					if (possibleJavadocDocumentationComment != null)
						{
						// If the Javadoc comment is longer we use that instead of the XML since it may have detected the first 
						// line as XML and ignored the rest for not having the same symbol.  For example:
						//
						// ## Comment
						// #
						// #
						//
						// This will be detected as a one line XML comment and a three line Javadoc comment.

						if (possibleJavadocDocumentationComment.End.LineNumber >
							possibleDocumentationComment.End.LineNumber)
							{
							possibleDocumentationComment = possibleJavadocDocumentationComment;
							possibleDocumentationComment.Javadoc = true;
							lineIterator = javadocLineIterator;
							}


						// If they're the same length...

						else if (possibleJavadocDocumentationComment.End.LineNumber ==
								  possibleDocumentationComment.End.LineNumber)
							{

							// If the comments are both one line long then it's genuinely ambiguous.  For example:
							//
							// ## Comment
							//
							// Is that a one line XML comment or a one line Javadoc comment?  We can't tell, so mark it as 
							// potentially either.

							if (possibleDocumentationComment.Start.LineNumber ==
								possibleDocumentationComment.End.LineNumber - 1)
								{
								possibleDocumentationComment.Javadoc = true;
								// XML should already be set to true
								}
							
							// If the comments are equal length but more than one line then it's just interpreting the XML as 
							// a Javadoc start with a vertical line for the remainder, so leave it as XML.  For example:
							//
							// ## Comment
							// ##
							// ##
							//
							// That's clearly a three line XML comment and not a Javadoc comment with a vertical line.

							}

						// If the XML comment is longer just leave it and ignore the Javadoc one.
					
						}
					}
						

				// Javadoc line comments

				if (foundComment == false && javadocLineCommentStringPairs != null)
					{
					for (int i = 0; foundComment == false && i < javadocLineCommentStringPairs.Length; i += 2)
						{
						foundComment = TryToGetLineComment(ref lineIterator, 
																				   javadocLineCommentStringPairs[i], javadocLineCommentStringPairs[i+1], true,
																				   out possibleDocumentationComment);
						}

					if (possibleDocumentationComment != null)
						{  possibleDocumentationComment.Javadoc = true;  }
					}


				// Plain line comments
				
				if (foundComment == false && lineCommentStrings != null)
					{
					for (int i = 0; foundComment == false && i < lineCommentStrings.Length; i++)
						{
						foundComment = TryToGetLineComment(ref lineIterator, 
																				   lineCommentStrings[i], lineCommentStrings[i], false,
																				   out possibleDocumentationComment);
						}
					}
					
				
				// Nada.
				
				if (foundComment == false)
					{  lineIterator.Next();  }
				else
					{
					if (possibleDocumentationComment != null)
						{
						// XML can actually use the Javadoc comment format in addition to its own.
						if (possibleDocumentationComment.Javadoc == true)
							{  possibleDocumentationComment.XML = true;  }

						possibleDocumentationComments.Add(possibleDocumentationComment);
						}

					// lineIterator would have been moved already if foundComment is true
					}
					
				}

			return possibleDocumentationComments;
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: TryToGetBlockComment
		 * 
		 * If the iterator is on a line that starts with the opening symbol of a block comment, this function moves the iterator
		 * past the entire comment and returns true.  If the comment is a candidate for documentation it will also return it as
		 * a <PossibleDocumentationComment> and mark the symbols as <CommentParsingType.CommentSymbol>.  If the
		 * line does not start with an opening comment symbol it will return false and leave the iterator where it is.
		 * 
		 * Not all the block comments it finds will be candidates for documentation, since some will have text after the closing
		 * symbol, so it's possible for this function to return true and have comment be null.  This is important because in Lua
		 * the block comment symbol is --[[ and the line comment symbol is --, so if we didn't move past the block comment 
		 * it could be interpreted as a line comment as well.
		 * 
		 * If openingMustBeAlone is set, that means no symbol can appear immediately after the opening symbol.  If it does
		 * the function will return false and not move past the comment.  This allows you to specifically detect something like 
		 * /** without also matching /******.
		 */
		protected bool TryToGetBlockComment (ref LineIterator lineIterator, 
																 string openingSymbol, string closingSymbol, bool openingMustBeAlone,
																 out PossibleDocumentationComment comment)
			{
			TokenIterator firstToken, endOfLine;
			lineIterator.GetBounds(LineBoundsMode.ExcludeWhitespace, out firstToken, out endOfLine);

			if (firstToken.MatchesAcrossTokens(openingSymbol) == false)
				{  
				comment = null;
				return false;
				}

			// Advance past the opening symbol because it's possible for it to be the same as the closing one, such as with 
			// Python's ''' and """ strings.
			firstToken.NextByCharacters(openingSymbol.Length);

			if (openingMustBeAlone && firstToken.FundamentalType == FundamentalType.Symbol)
				{  
				comment = null;  
				return false;
				}

			comment = new PossibleDocumentationComment();
			comment.Start = lineIterator;

			var tokenizer = lineIterator.Tokenizer;
			var lookahead = lineIterator;

			for (;;)
				{
				TokenIterator closingSymbolIterator;
				
				if (tokenizer.FindTokensBetween(closingSymbol, false, firstToken, endOfLine, out closingSymbolIterator) == true)
					{
					// Move past the end of the comment regardless of whether it's acceptable for documentation or not
					lookahead.Next();

					// Make sure nothing appears after the closing symbol on the line
					closingSymbolIterator.NextByCharacters(closingSymbol.Length);
					closingSymbolIterator.NextPastWhitespace();

					if (closingSymbolIterator.FundamentalType != FundamentalType.LineBreak &&
						closingSymbolIterator.FundamentalType != FundamentalType.Null)
						{  comment = null;  }
					else
						{  comment.End = lookahead;  }

					break;
					}

				lookahead.Next();

				// If we're not in bounds that means there was an unclosed comment at the end of the file.  Skip it but don't treat
				// it as a documentation candidate.
				if (!lookahead.IsInBounds)
					{  
					comment = null;
					break;  
					}

				lookahead.GetBounds(LineBoundsMode.ExcludeWhitespace, out firstToken, out endOfLine);
				}


			if (comment != null)
				{
				// Mark the symbols before returning

				firstToken = comment.Start.FirstToken(LineBoundsMode.ExcludeWhitespace);
				firstToken.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, openingSymbol.Length);

				LineIterator lastLine = lookahead;
				lastLine.Previous();
				lastLine.GetBounds(LineBoundsMode.ExcludeWhitespace, out firstToken, out endOfLine);
				endOfLine.PreviousByCharacters(closingSymbol.Length);
				endOfLine.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, closingSymbol.Length);
				}

			// If we made it this far that means we found a comment and can move the line iterator and return true.  Whether
			// that comment was suitable for documentation will be determined by the comment variable, but we are moving the
			// iterator and returning true either way.
			lineIterator = lookahead;
			return true;
			}


		/* Function: TryToGetLineComment
		 * 
		 * If the iterator is on a line that starts with a line comment symbol, this function moves the iterator past the entire
		 * comment and returns true.  If the comment is a candidate for documentation it will also return it as a
		 * <PossibleDocumentationComment> and mark the symbols as <CommentParsingType.CommentSymbol>.  If the
		 * line does not start with a line comment symbol it will return false and leave the iterator where it is.
		 * 
		 * This function takes a separate comment symbol for the first line and all remaining lines, allowing you to detect
		 * Javadoc line comments that start with ## and the remaining lines use #.  Both symbols can be the same if this isn't
		 * required.
		 * 
		 * If openingMustBeAlone is set, no symbol can appear immediately after the first line symbol.  If it does the function
		 * will return false and not move past the comment.  This allows you to specifically detect something like ## without 
		 * also matching #######.
		 */
		protected bool TryToGetLineComment (ref LineIterator lineIterator, 
																string firstSymbol, string remainderSymbol, bool openingMustBeAlone,
																out PossibleDocumentationComment comment)
			{
			TokenIterator firstToken = lineIterator.FirstToken(LineBoundsMode.ExcludeWhitespace);

			if (firstToken.MatchesAcrossTokens(firstSymbol) == false)
				{  
				comment = null;
				return false;
				}

			if (openingMustBeAlone)
				{
				TokenIterator nextToken = firstToken;
				nextToken.NextByCharacters(firstSymbol.Length);

				if (nextToken.FundamentalType == FundamentalType.Symbol)
					{  
					comment = null;
					return false;
					}
				}

			comment = new PossibleDocumentationComment();
			comment.Start = lineIterator;
			lineIterator.Next();

			// Since we're definitely returning a comment we can mark the comment symbols as we go rather than waiting until
			// the end.
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
			return true;
			}



		// Group: Properties
		// __________________________________________________________________________

		
		/* Property: Name
		 * The name of the language.
		 */
		override public string Name
			{
			get
				{  return name;  }
			}

		/* Property: LineCommentStrings
		 * An array of strings representing line comment symbols.  Will be null if none are defined.
		 */
		public string[] LineCommentStrings
			{
			get
				{  return lineCommentStrings;  }
			set
				{
				if (value != null && value.Length != 0)
					{  lineCommentStrings = value;  }
				else
					{  lineCommentStrings = null;  }
				}
			}
			
		/* Property: BlockCommentStringPairs
		 * An array of string pairs representing start and stop block comment symbols.  Will be null if none are defined.
		 */
		public string[] BlockCommentStringPairs
			{
			get
				{  return blockCommentStringPairs;  }
			set
				{
				if (value != null && value.Length != 0)
					{  
					if (value.Length % 2 == 1)
						{  throw new Engine.Exceptions.ArrayDidntHaveEvenLength("BlockCommentStringPairs");  }

					blockCommentStringPairs = value;  
					}
				else
					{  blockCommentStringPairs = null;  }
				}
			}
			
		/* Property: JavadocLineCommentStringPairs
		 * An array of string pairs representing Javadoc line comment symbols.  The first are are the symbols that must start the
		 * comment, and the second are the symbols that must be used on every following line.  Will be null if none are defined.
		 */
		public string[] JavadocLineCommentStringPairs
			{
			get
				{  return javadocLineCommentStringPairs;  }
			set
				{
				if (value != null && value.Length != 0)
					{  
					if (value.Length % 2 == 1)
						{  throw new Engine.Exceptions.ArrayDidntHaveEvenLength("JavadocLineCommentStringPairs");  }

					javadocLineCommentStringPairs = value;  
					}
				else
					{  javadocLineCommentStringPairs = null;  }
				}
			}
			
		/* Property: JavadocBlockCommentStringPairs
		 * An array of string pairs representing start and stop Javadoc block comment symbols.  Will be null if none are defined.
		 */
		public string[] JavadocBlockCommentStringPairs
			{
			get
				{  return javadocBlockCommentStringPairs;  }
			set
				{
				if (value != null && value.Length != 0)
					{  
					if (value.Length % 2 == 1)
						{  throw new Engine.Exceptions.ArrayDidntHaveEvenLength("JavadocBlockCommentStringPairs");  }

					javadocBlockCommentStringPairs = value;  
					}
				else
					{  javadocBlockCommentStringPairs = null;  }
				}
			}
			
		/* Property: XMLLineCommentStrings
		 * An array of strings representing XML line comment symbols.  Will be null if none are defined.
		 */
		public string[] XMLLineCommentStrings
			{
			get
				{  return xmlLineCommentStrings;  }
			set
				{
				if (value != null && value.Length != 0)
					{  xmlLineCommentStrings = value;  }
				else
					{  xmlLineCommentStrings = null;  }
				}
			}
			


		// Group: Variables
		// __________________________________________________________________________
		
		/* var: name
		 * The language name.
		 */
		protected string name;
		
		/* array: lineCommentStrings
		 * An array of strings that start line comments.
		 */
		protected string[] lineCommentStrings;
		
		/* array: blockCommentStringPairs
		 * An array of string pairs that start and end block comments.
		 */
		protected string[] blockCommentStringPairs;
		
		/* array: javadocLineCommentStringPairs
		 * An array of string pairs that start Javadoc line comments.  The first will be the symbol that must start it, and
		 * the second will be the symbol that must be used on every following line.
		 */
		protected string[] javadocLineCommentStringPairs;
		
		/* array: javadocBlockCommentStringPairs
		 * An array of string pairs that start and end Javadoc black comments.
		 */
		protected string[] javadocBlockCommentStringPairs;
		
		/* array: xmlLineCommentStrings
		 * An array of strings that start XML line comments.
		 */
		protected string[] xmlLineCommentStrings;
		
		}

	}