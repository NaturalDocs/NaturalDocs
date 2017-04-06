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

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
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
				PossibleDocumentationComment comment = null;
				
				
				// Javadoc block comments

				// We test for these before regular block comments because they are usually extended versions of them, such
				// as /** and /*.

				// We also test block comments in general ahead of line comments because in Lua the line comments are a
				// substring of them: -- versus --[[ and ]]--.
				
				if (javadocBlockCommentStringPairs != null)
					{
					for (int i = 0; comment == null && i < javadocBlockCommentStringPairs.Length; i += 2)
						{
						comment = TryToGetPDBlockComment(lineIterator, javadocBlockCommentStringPairs[i], 
																								 javadocBlockCommentStringPairs[i+1], true);
						}

					if (comment != null)
						{  comment.Javadoc = true;  }
					}
					
					
				// Plain block comments
					
				if (comment == null && blockCommentStringPairs != null)
					{
					for (int i = 0; comment == null && i < blockCommentStringPairs.Length; i += 2)
						{
						comment = TryToGetPDBlockComment(lineIterator, blockCommentStringPairs[i], 
																								 blockCommentStringPairs[i+1], false);
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

				if (comment == null && xmlLineCommentStrings != null)
					{
					for (int i = 0; comment == null && i < xmlLineCommentStrings.Length; i++)
						{
						comment = TryToGetPDLineComment(lineIterator, xmlLineCommentStrings[i],
																							   xmlLineCommentStrings[i], true);
						}

					if (comment != null)
						{  comment.XML = true;  }
					}
						
						
				// Javadoc line comments

				// We check for these even if a XML comment is found because they may share an opening symbol, such as ///.
				// We change it to Javadoc if it's longer.  If it's equal it's just interpreting the XML as a Javadoc start with a
				// vertical line for the remainder, so leave it as XML.  Unless the comment is only one line long, in which case it's
				// genuinely ambiguous.
				
				if ( (comment == null || comment.XML == true) && javadocLineCommentStringPairs != null)
					{
					PossibleDocumentationComment javadocComment = null;

					for (int i = 0; javadocComment == null && i < javadocLineCommentStringPairs.Length; i += 2)
						{
						javadocComment = TryToGetPDLineComment(lineIterator, javadocLineCommentStringPairs[i],
																										  javadocLineCommentStringPairs[i+1], true);
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
				
				if (comment == null && lineCommentStrings != null)
					{
					for (int i = 0; comment == null && i < lineCommentStrings.Length; i++)
						{
						comment = TryToGetPDLineComment(lineIterator, lineCommentStrings[i], lineCommentStrings[i], false);
						}
					}
					
				
				// Nada.
				
				if (comment == null)
					{  lineIterator.Next();  }
				else
					{
					// XML can actually use the Javadoc comment format in addition to its own.
					if (comment.Javadoc)
						{  comment.XML = true;  }

					possibleDocumentationComments.Add(comment);
					lineIterator = comment.End;
					}
					
				}

			return possibleDocumentationComments;
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