/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parser
 * ____________________________________________________________________________
 *
 * A generalized language parser, and also a base class for language-specific parsers.
 *
 *
 * Multithreading: Thread Safe
 *
 *		The parser object may be used by multiple threads at the same time.  It stores no internal state related to parsing,
 *		and other instance variables are read-only once the parser is created.
 *
 *
 * Topic: Implementing Language-Specific Parsers
 *
 *		Since there will only be one shared parser per <Language> object, all derived classes must also be thread safe and
 *		allow multiple threads to use them on multiple files concurrently.  There should be no parsing state data stored in
 *		instance variables.
 *
 *		In order to have Natural Docs use the parser automatically for a specific language you must create a predefined
 *		language entry in <Languages.Manager>'s constructor.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Comments;
using CodeClear.NaturalDocs.Engine.CommentTypes;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Languages
	{
	public partial class Parser
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
		 * FileDoesntExist - The file couldn't be opened because it doesn't exist.
		 * CantAccessFile - The file exists but couldn't be opened, such as if the program doesn't have permission to access
		 *							the file.
		 */
		public enum ParseResult : byte
			{  Success, Cancelled, FileDoesntExist, CantAccessFile  }


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


		/* Function: Parser
		 */
		public Parser (Engine.Instance engineInstance, Language language)
			{
			this.engineInstance = engineInstance;
			this.language = language;
			}


		/* Function: Parse
		 * Parses the passed file and returns it as a list of <Topics> and class parent <Links>.  Set cancelDelegate for the ability
		 * to interrupt parsing, or use <Delegates.NeverCancel> if that's not needed.
		 */
		virtual public ParseResult Parse (AbsolutePath filePath, int fileID, CancelDelegate cancelDelegate,
														out IList<Topic> topics, out LinkSet classParentLinks)
			{
			topics = null;
			classParentLinks = null;

			string content = null;

			try
				{
				// This function supports determining the character encoding from just the path, which is important because unit test
				// files may not have been added to Files.Manager.
				int characterEncodingID = EngineInstance.Files.CharacterEncodingID(filePath);

				if (characterEncodingID == 0)  // Unicode auto-detect
					{
					content = System.IO.File.ReadAllText(filePath);
					}
				else
					{
					var encoding = System.Text.Encoding.GetEncoding(characterEncodingID);
					content = System.IO.File.ReadAllText(filePath, encoding);
					}
				}

			catch (System.IO.FileNotFoundException)
				{  return ParseResult.FileDoesntExist;  }
			catch (System.IO.DirectoryNotFoundException)
				{  return ParseResult.FileDoesntExist;  }
			catch
				{  return ParseResult.CantAccessFile;  }

			return Parse(new Tokenizer(content, tabWidth: EngineInstance.Config.TabWidth),
							   fileID, cancelDelegate, out topics, out classParentLinks);
			}


		/* Function: Parse
		 * Parses the tokenized source code and returns it as a list of <Topics> and class parent <Links>.  Set cancelDelegate for
		 * the ability to interrupt parsing, or use <Delegates.NeverCancel>.
		 */
		virtual public ParseResult Parse (Tokenizer source, int fileID, CancelDelegate cancelDelegate,
													out IList<Topic> topics, out LinkSet classParentLinks)
			{
			if (language.Type == Language.LanguageType.Container)
				{  throw new Exceptions.BadContainerOperation("Parse(tokenizer)");  }

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


			// Apply file and language IDs since we're going to need the language ID to deal with prototypes.

			ApplyFileAndLanguageIDs(commentElements, fileID, language.ID);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// Apply prototypes manually defined inside comments.

			ApplyCommentPrototypes(commentElements);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// If we have full language support, get the code elements as well.

			if (language.Type == Language.LanguageType.FullSupport)
				{

				// Fill in any access levels we can find in comment prototypes.  Since this requires prototype parsing, we have to make sure
				// the language IDs are set beforehand.

				ApplyPrototypeAccessLevels(commentElements);

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }


				// Fill in the declared access levels for comment elements.  We do this before merging with the code elements so the defaults
				// that come from the comment settings only apply to topics that don't also appear in the code.  Anything that gets merged will
				// have the comment settings overwritten by the code settings.

				ApplyDeclaredAccessLevels(commentElements);

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }


				// Now retrieve the code elements.

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


				// Preprocess enums to prevent issues that may otherwise occur in the merging process.

				PreprocessEnumComments(commentElements, codeElements);

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


				// Make sure all enum topics have their values in their bodies.

				AddEnumValuesToBodies(elements);


				#if DEBUG
					ValidateElements(elements, ValidateElementsMode.MergedElements);
				#endif


				// Remove code-only topics if --documented-only is on.  We do this after merging and keep all the original elements so that
				// the code's effects still apply.

				if (EngineInstance.Config.DocumentedOnly)
					{
					int i = 0;
					while (i < elements.Count)
						{
						if (elements[i].Topic != null &&
							elements[i].InComments == false)
							{
							elements[i].Topic = null;
							i++;

							// Remove all embedded topics for consistency, even if they're documented
							while (i < elements.Count &&
									 elements[i].Topic != null &&
									 elements[i].Topic.IsEmbedded)
								{
								elements[i].Topic = null;
								i++;
								}
							}

						else
							{
							i++;

							// Skip embedded topics as well
							while (i < elements.Count &&
									 elements[i].Topic != null &&
									 elements[i].Topic.IsEmbedded)
								{  i++;  }
							}
						}
					}
				}


			// If we have basic language support...

			else if (language.Type == Language.LanguageType.BasicSupport)
				{

				// Fill in additional prototypes via our language-neutral algorithm.  These will not overwrite the comment prototypes.

				AddBasicPrototypes(source, commentElements, possibleDocumentationComments);

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }


				// Fill in any access levels we can find in the prototypes.

				ApplyPrototypeAccessLevels(commentElements);

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }


				// Fill in the declared access levels.

				ApplyDeclaredAccessLevels(commentElements);

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

			else if (language.Type == Language.LanguageType.TextFile)
				{

				// Fill in any access levels we can find in the prototypes.  Prototypes may have been defined in the comments.

				ApplyPrototypeAccessLevels(commentElements);

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }


				// Fill in the declared access levels.

				ApplyDeclaredAccessLevels(commentElements);

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }


				// We don't have to remove headerless topics because there's no way to specify them in text files.

				elements = commentElements;
				commentElements = null;
				}

			#if DEBUG
			else
				{
				// Container was already handled at the beginning of the function.
				throw new Exception ("Unrecognized language type " + language.Type);
				}
			#endif


			// Add automatic grouping.

			if (EngineInstance.Config.AutoGroup)
				{
				if (AddAutomaticGrouping(elements))
					{
					// Need to add these to any newly created groups.
					ApplyDeclaredAccessLevels(elements);
					}

				if (cancelDelegate())
					{  return ParseResult.Cancelled;  }
				}


			// Reapply file and language IDs so that they also apply to code elements and auto-groups.

			ApplyFileAndLanguageIDs(elements, fileID, language.ID);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// Calculate the effective access levels.  This is done after merging code and comment topics so members are consistent.  For
			// example, a public comment-only topic appearing in a private class needs to have an effective access level of private.

			GenerateEffectiveAccessLevels(elements);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// Generate remaining symbols.

			GenerateRemainingSymbols(elements);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// Apply remaining properties. Class strings and using statements affect contexts so they must be applied
			// first.

			ApplyTags(elements);
			ApplyClassStrings(elements);
			ApplyUsingStatements(elements);
			ApplyContexts(elements);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


			// Extract class parent links from class prototypes.  This can't be done earlier because they're affected by class strings,
			// using statements, and contexts.

			ExtractClassParentLinks(elements);

			if (cancelDelegate())
				{  return ParseResult.Cancelled;  }


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
		public virtual ParsedPrototype ParsePrototype (string stringPrototype, int commentTypeID)
			{
			if (language.Type == Language.LanguageType.Container)
				{  throw new Exceptions.BadContainerOperation("ParsePrototype");  }

			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			ParsedPrototype parsedPrototype;


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
				else if (iterator.Character == '[')
					{
					// Only treat brackets as parameters if it's following "this", meaning it's an iterator.  Ignore all others so we
					// don't get tripped up on metadata or array brackets on return values.

					TokenIterator lookbehind = iterator;
					lookbehind.Previous();
					lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator);

					if (lookbehind.MatchesToken("this"))
						{
						closingBracket = ']';
						break;
						}
					else
						{  iterator.Next();  }
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
			// Mark the delimiters.

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


				// We have enough tokens marked to create the parsed prototype.  This will also let us iterate through the parameters
				// easily.

				parsedPrototype = new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID);


				// If there are any parameters, mark the tokens in them.

				TokenIterator start, end;

				if (parsedPrototype.NumberOfParameters > 0)
					{
					parsedPrototype.ParameterStyle = DetectParameterStyle(parsedPrototype);

					for (int i = 0; i < parsedPrototype.NumberOfParameters; i++)
						{
						parsedPrototype.GetParameter(i, out start, out end);

						if (parsedPrototype.ParameterStyle == ParameterStyle.C)
							{  MarkCParameter(start, end);  }
						else if (parsedPrototype.ParameterStyle == ParameterStyle.Pascal)
							{  MarkPascalParameter(start, end);  }
						else
							{  throw new NotImplementedException();  }
						}
					}


				// Mark the return value of functions.  First we'll check if there's a colon after the parameters to see if it's a
				// Pascal-style function.

				parsedPrototype.GetAfterParameters(out start, out end);

				// Exclude the closing bracket
				if (start.PrototypeParsingType == PrototypeParsingType.EndOfParams)
					{
					start.Next();

					while (start.PrototypeParsingType == PrototypeParsingType.ClosingExtensionSymbol)
						{  start.Next();  }

					start.NextPastWhitespace(end);
					}

				// If there's a colon immediately after the parameters, assume it's a Pascal-style function and mark the return
				// value after it.  We can't rely on parameterStyle since the prototype may not have parameters.
				if (start < end && start.Character == ':')
					{
					start.Next();
					start.NextPastWhitespace(end);

					if (start < end)
						{  MarkTypeAndModifiers(start, end);  }
					}

				// Otherwise assume it's a C-style function.  Mark the part before the parameters, which includes the name.
				else
					{
					parsedPrototype.GetBeforeParameters(out start, out end);

					// Exclude the opening bracket
					end.Previous();
					end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

					if (start < end)
						{  MarkCParameter(start, end);  }
					}
				}


			// If there's no brackets, it's a variable, property, or class.

			else
				{
				parsedPrototype = new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID);
				TokenIterator start = tokenizedPrototype.FirstToken;
				TokenIterator end = tokenizedPrototype.EndOfTokens;

				parsedPrototype.ParameterStyle = DetectParameterStyle(start, end);

				if (parsedPrototype.ParameterStyle == ParameterStyle.C)
					{  MarkCParameter(start, end);  }
				else if (parsedPrototype.ParameterStyle == ParameterStyle.Pascal)
					{  MarkPascalParameter(start, end);  }
				else
					{  throw new NotImplementedException();  }
				}

			return parsedPrototype;
			}


		/* Function: ParseClassPrototype
		 * Converts a raw text prototype into a <ParsedClassPrototype>.  Will return null if it is not an appropriate prototype.
		 */
		public virtual ParsedClassPrototype ParseClassPrototype (string stringPrototype, int commentTypeID)
			{
			if (language.Type == Language.LanguageType.Container)
				{  throw new Exceptions.BadContainerOperation("ParseClassPrototype");  }

			if (EngineInstance.CommentTypes.InClassHierarchy(commentTypeID) == false)
				{  return null;  }

			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			ParsedClassPrototype parsedPrototype = new ParsedClassPrototype(tokenizedPrototype);


			// First walk through trying to find a class keyword.  We're rather permissive when it comes to modifiers to allow for things
			// like splint comments and bracketed C# metadata.

			TokenIterator iterator = tokenizedPrototype.FirstToken;
			bool foundKeyword = false;

			for (;;)
				{
				if (iterator.IsInBounds == false)
					{  break;  }
				else if (iterator.MatchesToken("class") ||
						  iterator.MatchesToken("struct") ||
						  iterator.MatchesToken("interface"))
					{
					// Only count it as a keyword if it's a standalone word.  We don't want to get tripped up on a macro called external_class or
					// something like that.
					if (iterator.IsStandaloneWord())
						{
						iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.Keyword;
						foundKeyword = true;
						break;
						}
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


			// If we found a recognized keyword like class or struct, we can assume everything before it is a modifier and what's immediately after
			// it is the name.

			if (foundKeyword)
				{
				TokenIterator startOfModifiers = tokenizedPrototype.FirstToken;
				TokenIterator endOfModifiers = iterator;

				endOfModifiers.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfModifiers);
				startOfModifiers.NextPastWhitespace(endOfModifiers);

				if (endOfModifiers > startOfModifiers)
					{  startOfModifiers.SetClassPrototypeParsingTypeBetween(endOfModifiers, ClassPrototypeParsingType.Modifier);  }

				// The iterator is on the keyword.  Move past it to the name.
				iterator.Next();
				iterator.NextPastWhitespace();
				}


			// If we didn't find a recognized keyword, treat it as a space separated list of words, the last one being the name and the rest being modifiers.

			else
				{
				iterator = tokenizedPrototype.FirstToken;
				iterator.NextPastWhitespace();

				if (iterator.FundamentalType != FundamentalType.Text &&
					iterator.Character != '_')
					{  return null;  }

				TokenIterator startOfModifiers = iterator;
				TokenIterator endOfModifiers = iterator;

				for (;;)
					{
					TokenIterator startOfLastWord = iterator;

					while (iterator.IsInBounds)
						{
						if (iterator.FundamentalType == FundamentalType.Text ||
							iterator.Character == '.' ||
							iterator.Character == '_')
							{  iterator.Next();  }
						else if (iterator.MatchesAcrossTokens("::"))
							{  iterator.Next(2);  }
						else
							{  break;  }
						}

					if (iterator.FundamentalType != FundamentalType.Whitespace)
						{
						iterator = startOfLastWord;
						break;
						}

					TokenIterator lookahead = iterator;
					lookahead.NextPastWhitespace();

					if (lookahead.FundamentalType != FundamentalType.Text &&
						lookahead.Character != '_')
						{
						iterator = startOfLastWord;
						break;
						}

					endOfModifiers = iterator;
					iterator = lookahead;
					}

				if (endOfModifiers > startOfModifiers)
					{  startOfModifiers.SetClassPrototypeParsingTypeBetween(endOfModifiers, ClassPrototypeParsingType.Modifier);  }
				}


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

			startOfName.SetClassPrototypeParsingTypeBetween(endOfName, ClassPrototypeParsingType.Name);


			// Iterator is past the name.  Get the template information if there is any.

			iterator.NextPastWhitespace();

			if (iterator.Character == '<' || iterator.Character == '[' || iterator.Character == '(' ||
				iterator.MatchesAcrossTokens("#("))  // SystemVerilog
				{
				TokenIterator startOfTemplate = iterator;

				if (iterator.Character == '#')
					{  iterator.Next();  }

				if (TryToSkipBlock(ref iterator, true) == false)
					{  return null;  }

				startOfTemplate.SetClassPrototypeParsingTypeBetween(iterator, ClassPrototypeParsingType.TemplateSuffix);

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
					iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.StartOfParents);
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
						iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.ParentSeparator);
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
					if (iterator.IsStandaloneWord())
						{  iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.ParentSeparator;  }

					iterator.Next();
					}
				else if (iterator.Character == '{')
					{
					iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfBody;
					getParents = false;
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

						if (iterator < endOfParent)
							{
							if (iterator.Character == '<' || iterator.Character == '[' || iterator.Character == '(' ||
								iterator.MatchesAcrossTokens("#("))  // SystemVerilog
								{
								TokenIterator lookahead = iterator;

								if (lookahead.Character == '#')
									{  lookahead.Next();  }

								if (TryToSkipBlock(ref lookahead, true) == true && lookahead <= endOfParent)
									{
									// We've reached template information so we can stop looking for the name.
									break;
									}
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
					startOfName.SetClassPrototypeParsingTypeBetween(endOfName, ClassPrototypeParsingType.Name);

					TokenIterator startOfModifiers = startOfParent;
					TokenIterator endOfModifiers = startOfName;

					endOfModifiers.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfModifiers);
					startOfModifiers.NextPastWhitespace(endOfModifiers);

					if (endOfModifiers > startOfModifiers)
						{  startOfModifiers.SetClassPrototypeParsingTypeBetween(endOfModifiers, ClassPrototypeParsingType.Modifier);  }

					TokenIterator startOfTemplate = endOfName;
					startOfTemplate.NextPastWhitespace(endOfParent);

					TokenIterator endOfTemplate = startOfTemplate;
					if (endOfTemplate < endOfParent &&
						(endOfTemplate.Character == '<' || endOfTemplate.Character == '[' || endOfTemplate.Character == '(' ||
						 endOfTemplate.MatchesAcrossTokens("#(")) )  // SystemVerilog
						{
						if (endOfTemplate.Character == '#')
							{  endOfTemplate.Next();  }

						if (TryToSkipBlock(ref endOfTemplate, true) == true && endOfTemplate <= endOfParent)
							{
							startOfTemplate.SetClassPrototypeParsingTypeBetween(endOfTemplate, ClassPrototypeParsingType.TemplateSuffix);
							}
						else
							{
							// Reset in case TryToSkipBlock() worked but it went past the parent.
							endOfTemplate = startOfTemplate;
							}
						}

					// There may also be modifiers after the name.
					startOfModifiers = endOfTemplate;
					endOfModifiers = endOfParent;

					endOfModifiers.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfModifiers);
					startOfModifiers.NextPastWhitespace(endOfModifiers);

					if (endOfModifiers > startOfModifiers)
						{  startOfModifiers.SetClassPrototypeParsingTypeBetween(endOfModifiers, ClassPrototypeParsingType.Modifier);  }
					}
				}

			return parsedPrototype;
			}


		/* Function: SyntaxHighlight
		 * Applies <SyntaxHighlightingTypes> to the tokenized content.
		 */
		public virtual void SyntaxHighlight (Tokenizer source)
			{
			if (language.Type == Language.LanguageType.Container)
				{  throw new Exceptions.BadContainerOperation("SyntaxHighlight");  }

			TokenIterator iterator = source.FirstToken;

			while (iterator.IsInBounds)
				{
				if (TryToSkipComment(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipString(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipNumber(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipKeyword(ref iterator, ParseMode.SyntaxHighlight))
					{
					}
				else
					{  iterator.Next();  }
				}
			}


		/* Function: SyntaxHighlight
		 * Applies <SyntaxHighlightingTypes> to the <ParsedPrototype>.
		 */
		public void SyntaxHighlight (ParsedPrototype prototype)
			{
			SyntaxHighlight(prototype.Tokenizer);
			}


		/* Function: SyntaxHighlight
		 * Applies <SyntaxHighlightingTypes> to the <ParsedClassPrototype>.
		 */
		public void SyntaxHighlight (ParsedClassPrototype prototype)
			{
			SyntaxHighlight(prototype.Tokenizer);
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
		//	The default implementation uses the comment symbols found in <Language>.  You can override this function if you need to
		// do something more sophisticated, such as interpret the POD directives in Perl.
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
			if (language.Type == Language.LanguageType.Container)
				{  throw new Exceptions.BadContainerOperation("GetPossibleDocumentationComments");  }

			else if (language.Type == Language.LanguageType.TextFile)
				{
				List<PossibleDocumentationComment> possibleDocumentationComments = new List<PossibleDocumentationComment>(1);

				PossibleDocumentationComment comment = new PossibleDocumentationComment();
				comment.Start = source.FirstLine;
				comment.End = source.EndOfLines;

				possibleDocumentationComments.Add(comment);
				return possibleDocumentationComments;
				}

			else
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

					if (language.HasJavadocBlockCommentSymbols)
						{
						foreach (var javadocBlockCommentSymbols in language.JavadocBlockCommentSymbols)
							{
							foundComment = TryToGetBlockComment(ref lineIterator,
																						 javadocBlockCommentSymbols.OpeningSymbol,
																						 javadocBlockCommentSymbols.ClosingSymbol,
																						 true, out possibleDocumentationComment);

							if (foundComment)
								{  break;  }
							}

						if (possibleDocumentationComment != null)
							{  possibleDocumentationComment.Javadoc = true;  }
						}


					// Plain block comments

					// We test block comments ahead of line comments because in Lua the line comments are a substring of them: --
					// versus --[[ and ]].

					if (foundComment == false && language.HasBlockCommentSymbols)
						{
						foreach (var blockCommentSymbols in language.BlockCommentSymbols)
							{
							foundComment = TryToGetBlockComment(ref lineIterator,
																						 blockCommentSymbols.OpeningSymbol,
																						 blockCommentSymbols.ClosingSymbol,
																						 false, out possibleDocumentationComment);

							if (foundComment)
								{  break;  }
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

					if (foundComment == false && language.HasXMLLineCommentSymbols)
						{
						foreach (var xmlLineCommentSymbol in language.XMLLineCommentSymbols)
							{
							foundComment = TryToGetLineComment(ref lineIterator, xmlLineCommentSymbol, xmlLineCommentSymbol,
																					   true, out possibleDocumentationComment);

							if (foundComment)
								{  break;  }
							}

						if (possibleDocumentationComment != null)
							{  possibleDocumentationComment.XML = true;  }
						}


					// Ambiguous XML/Javadoc line comments

					// If an XML comment is found we check the same position for Javadoc because they may share an opening
					// symbol, such as ///.

					if (possibleDocumentationComment != null && possibleDocumentationComment.XML == true &&
						language.HasJavadocLineCommentSymbols)
						{
						LineIterator javadocLineIterator = possibleDocumentationComment.Start;
						PossibleDocumentationComment possibleJavadocDocumentationComment = null;
						bool foundJavadocComment = false;

						foreach (var javadocLineCommentSymbols in language.JavadocLineCommentSymbols)
							{
							foundJavadocComment = TryToGetLineComment(ref javadocLineIterator,
																								  javadocLineCommentSymbols.FirstLineSymbol,
																								  javadocLineCommentSymbols.FollowingLinesSymbol,
																								  true, out possibleJavadocDocumentationComment);

							if (foundJavadocComment)
								{  break;  }
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

					if (foundComment == false && language.HasJavadocLineCommentSymbols)
						{
						foreach (var javadocLineCommentSymbols in language.JavadocLineCommentSymbols)
							{
							foundComment = TryToGetLineComment(ref lineIterator,
																					   javadocLineCommentSymbols.FirstLineSymbol,
																					   javadocLineCommentSymbols.FollowingLinesSymbol,
																					   true, out possibleDocumentationComment);

							if (foundComment)
								{  break;  }
							}

						if (possibleDocumentationComment != null)
							{  possibleDocumentationComment.Javadoc = true;  }
						}


					// Plain line comments

					if (foundComment == false && language.HasLineCommentSymbols)
						{
						foreach (var lineCommentSymbol in language.LineCommentSymbols)
							{
							foundComment = TryToGetLineComment(ref lineIterator, lineCommentSymbol, lineCommentSymbol,
																					   false, out possibleDocumentationComment);

							if (foundComment)
								{  break;  }
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
			}


		/* Function: GetInlineDocumentationComment
		 *
		 * If the iterator is on an inline comment suitable for documentation, returns it as a <InlineDocumentationComment>, or
		 * null if not.  The iterator will not be moved.
		 *
		 * The comment will have its comment symbols marked as <CommentParsingType.CommentSymbol> in the tokenizer.  This
		 * allows further operations to be done on it in a language independent manner.
		 *
		 * Default Implementation:
		 *
		 *		The default implementation uses the Javadoc and XML comment symbols found in <Language>.  It will not return a
		 *		<InlineDocumentationComment> for standard comment symbols.
		 *
		 *		The comment must be at the end of a line to be a candidate for documentation, so the comment symbol must be the
		 *		next non-whitespace character on the line, and in the case of block comments, nothing but whitespace may trail the
		 *		closing symbol.
		 *
		 *		Multiple consecutive line comments are allowed but they must be continuous and the later line comments must be
		 *		alone on their lines.
		 */
		virtual public InlineDocumentationComment GetInlineDocumentationComment (TokenIterator iterator)
			{
			if (language.Type == Language.LanguageType.Container ||
				language.Type == Language.LanguageType.TextFile)
				{  throw new Exceptions.BadContainerOperation("GetInlineDocumentationComment");  }

			while (iterator.FundamentalType == FundamentalType.Whitespace)
				{  iterator.Next();  }


			// Javadoc block comments

			if (language.HasJavadocBlockCommentSymbols)
				{
				foreach (var javadocBlockCommentSymbols in language.JavadocBlockCommentSymbols)
					{
					if (TryToGetBlockComment(ref iterator,
															javadocBlockCommentSymbols.OpeningSymbol,
															javadocBlockCommentSymbols.ClosingSymbol,
															true, out InlineDocumentationComment comment))
						{  return comment;  }
					}
				}


			// XML line comments

			// XML and Javadoc may share an openingsymbol, such as ///.  In this case check for both and use whichever is
			// longest.
			InlineDocumentationComment longestLineComment = null;

			if (language.HasXMLLineCommentSymbols)
				{
				foreach (var xmlLineCommentSymbol in language.XMLLineCommentSymbols)
					{
					TokenIterator temp = iterator;

					if (TryToGetLineComment(ref temp, xmlLineCommentSymbol, xmlLineCommentSymbol,
														  true, out InlineDocumentationComment comment))
						{
						if (longestLineComment == null ||
							comment.End > longestLineComment.End)
							{  longestLineComment = comment;  }
						}
					}
				}


			// Javadoc line comments

			if (language.HasJavadocLineCommentSymbols)
				{
				foreach (var javadocLineCommentSymbol in language.JavadocLineCommentSymbols)
					{
					TokenIterator temp = iterator;

					if (TryToGetLineComment(ref temp,
														  javadocLineCommentSymbol.FirstLineSymbol,
														  javadocLineCommentSymbol.FollowingLinesSymbol,
														  true, out InlineDocumentationComment comment))
						{
						if (longestLineComment == null ||
							comment.End > longestLineComment.End)
							{  longestLineComment = comment;  }
						}
					}
				}

			if (longestLineComment != null)
				{  return longestLineComment;  }


			// Nada

			return null;
			}


		/* Function: IsBuiltInType
		 * Returns whether the type string is a built-in type such as "int" as opposed to a user-defined type.
		 */
		virtual public bool IsBuiltInType (string type)
			{
			return typeKeywords.Contains(type);
			}


		/* Function: IsBuiltInType
		 * Returns whether the text between the iterators is a built-in type such as "int" as opposed to a user-defined
		 * type.
		 */
		public bool IsBuiltInType (TokenIterator start, TokenIterator end)
			{
			return IsBuiltInType( start.TextBetween(end) );
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
			if (topicA.LanguageID != language.ID)
				{  throw new Exception("Tried to call IsSameCodeElement() using a language object that neither topic uses.");  }
			#endif

			// This is a problem if one uses "constructor" and one uses "function" and they don't map to the same comment type.
			if (topicA.CommentTypeID != topicB.CommentTypeID)
				{  return false;  }

			bool ignoreCase = (Manager.FromID(topicA.LanguageID).CaseSensitive == false);

			if (string.Compare(topicA.Symbol, topicB.Symbol, ignoreCase) != 0)
				{  return false;  }

			// So now we have two topics of the same language, symbol, and type.  Now the assumption is they're the same
			// unless they're distinguished by parameters.

			if (!topicA.DefinesClass)
				{
				ParameterString topicAPrototypeParameters = topicA.PrototypeParameters;
				ParameterString topicBPrototypeParameters = topicB.PrototypeParameters;

				if (topicAPrototypeParameters == null && topicBPrototypeParameters == null)
					{  return true;  }
				else if (topicAPrototypeParameters != null && topicBPrototypeParameters != null)
					{  return (string.Compare(topicA.PrototypeParameters, topicB.PrototypeParameters, ignoreCase) == 0);  }
				else
					{  return false;  }
				}

			return true;
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
				{  EngineInstance.Comments.Parse(comment, language.ID, topics);  }


			// Convert the topics to elements.  Generate ranges for scoped topics and groups.

			List<Element> elements = new List<Element>(topics.Count + 1);

			ParentElement rootElement = new ParentElement(0, 0, Element.Flags.InComments);
			rootElement.IsRootElement = true;
			rootElement.DefaultDeclaredChildAccessLevel = AccessLevel.Public;
			rootElement.DefaultChildLanguageID = language.ID;
			rootElement.EndingLineNumber = int.MaxValue;
			rootElement.EndingCharNumber = int.MaxValue;

			elements.Add(rootElement);

			ParentElement lastClass = null;
			ParentElement lastGroup = null;

			int i = 0;
			while (i < topics.Count)
				{
				Topic topic = topics[i];
				CommentType commentType = null;

				// Look up the comment type and end the previous class or group if necessary.
				if (topic.CommentTypeID != 0)
					{
					commentType = EngineInstance.CommentTypes.FromID(topic.CommentTypeID);

					if (commentType.Scope == CommentType.ScopeValue.Start ||
						 commentType.Scope == CommentType.ScopeValue.End)
						{
						if (lastClass != null)
							{
							lastClass.EndingLineNumber = topic.CommentLineNumber;
							lastClass.EndingCharNumber = 1;
							lastClass = null;
							}
						}

					if (commentType.Scope == CommentType.ScopeValue.Start ||
						 commentType.Scope == CommentType.ScopeValue.End ||
						 topic.IsGroup)
						{
						if (lastGroup != null)
							{
							lastGroup.EndingLineNumber = topic.CommentLineNumber;
							lastGroup.EndingCharNumber = 1;
							lastGroup = null;
							}
						}
					}

				// Embedded topics get a ParentElement and their children get regular Elements no matter what.  This covers enums
				// with values defined in the comment.
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

				// Scoped, group, and enum topics always get a ParentElement.  Sections get treated like classes because we want
				// any modifiers they have to be inherited by the topics that follow.
				else if (commentType != null && topic.IsList == false &&
						  (commentType.Scope == CommentType.ScopeValue.Start ||
						   commentType.Scope == CommentType.ScopeValue.End ||
						   topic.IsGroup || topic.IsEnum))
					{
					ParentElement parentElement = new ParentElement(topic.CommentLineNumber, 1, Element.Flags.InComments);
					parentElement.Topic = topic;
					parentElement.DefaultChildLanguageID = topic.LanguageID;

					if (topic.IsGroup)
						{
						// Groups don't enfoce a maximum access level, they just set the default declared level.
						parentElement.DefaultDeclaredChildAccessLevel = topic.DeclaredAccessLevel;
						}
					else if (topic.IsEnum)
						{
						parentElement.DefaultDeclaredChildAccessLevel = topic.DeclaredAccessLevel;

						// This is for enums without values defined in the comments.  We want it to be a parent element for merging
						// later, but it doesn't currently have children, so set the end to the same as the start.
						parentElement.EndingFilePosition = parentElement.FilePosition;
						}
					else // scope start or end
						{
						parentElement.MaximumEffectiveChildAccessLevel = topic.DeclaredAccessLevel;
						parentElement.DefaultDeclaredChildAccessLevel = AccessLevel.Public;
						}

					elements.Add(parentElement);
					i++;

					if (topic.IsGroup)
						{  lastGroup = parentElement;  }
					else if (topic.IsEnum)
						{  }
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
				// It's okay if this extends past the end of the file, MergeElements will take care of limiting them to their parents.
				// We can't use one line past the last comment because if it's an enum it would start in the class/group but its own
				// scope would extend past it.
				lastClass.EndingLineNumber = int.MaxValue;
				lastClass.EndingCharNumber = int.MaxValue;
				}

			if (lastGroup != null)
				{
				lastGroup.EndingLineNumber = int.MaxValue;
				lastGroup.EndingCharNumber = int.MaxValue;
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
		 *		- Every <Topic> must have a comment type ID set.
		 *		- Every <Topic> must have a symbol set.
		 *		- Each symbol must be fully resolved.  For example, a function appearing in a class must have the symbol "Class.Function".
		 *		  Other code will not apply parent symbols to children.
		 *		- You cannot return list <Topics>.  If you have multiple variables declared in one statement or something similar, you must
		 *		  generate individual <Topics> for each one.
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
					{  endCode = source.EndOfLines;  }

				TokenIterator prototypeStart, prototypeEnd;
				if (TryToFindBasicPrototype(elements[elementIndex].Topic, startCode, endCode, out prototypeStart, out prototypeEnd))
					{
					Tokenizer prototype = Tokenizer.CreateFromIterators(prototypeStart, prototypeEnd);
					elements[elementIndex].Topic.Prototype = NormalizePrototype(prototype);
					}
				}
			}


		/* Function: TryToFindBasicPrototype
		 * Attempts to find a prototype for the passed <Topic> between the iterators.  If one is found, it will be normalized and put in
		 * <Topic.Prototoype>.
		 */
		protected virtual bool TryToFindBasicPrototype (Topic topic, LineIterator startCode, LineIterator endCode,
																			out TokenIterator prototypeStart, out TokenIterator prototypeEnd)
			{
			PrototypeEnders prototypeEnders = language.GetPrototypeEndersFor(topic.CommentTypeID);

			if (prototypeEnders == null)
				{
				prototypeStart = startCode.FirstToken(LineBoundsMode.Everything);
				prototypeEnd = prototypeStart;
				return false;
				}

			// Skip leading blank lines even in languages where line breaks matter.
			while (startCode < endCode && startCode.IsEmpty(LineBoundsMode.ExcludeWhitespace))
				{  startCode.Next();  }

			TokenIterator start = startCode.FirstToken(LineBoundsMode.ExcludeWhitespace);
			TokenIterator limit = endCode.FirstToken(LineBoundsMode.ExcludeWhitespace);

			return TryToFindBasicPrototype(topic, start, limit, out prototypeStart, out prototypeEnd);
			}


		/* Function: TryToFindBasicPrototype
		 * Attempts to find a prototype for the passed <Topic> between the iterators.  If one is found, it will be normalized and put in
		 * <Topic.Prototoype>.
		 */
		protected virtual bool TryToFindBasicPrototype (Topic topic, TokenIterator start, TokenIterator limit,
																			out TokenIterator prototypeStart, out TokenIterator prototypeEnd)
			{
			PrototypeEnders prototypeEnders = language.GetPrototypeEndersFor(topic.CommentTypeID);

			if (prototypeEnders == null)
				{
				prototypeStart = limit;
				prototypeEnd = limit;
				return false;
				}

			TokenIterator iterator = start;

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

				else if (language.LineExtender != null && iterator.MatchesAcrossTokens(language.LineExtender))
					{
					// If the line extender is an underscore we don't want to treat it as one if it's adjacent to any text because
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

					if (!partOfIdentifier)
						{  lineHasExtender = true;  }

					iterator.Next();
					}


				// Ender Symbol, not in a bracket

				// We test this before looking for opening brackets so the opening symbols can be used as enders.
				else if (prototypeEnders.Symbols != null &&
						   iterator.MatchesAnyAcrossTokens(prototypeEnders.Symbols, !language.CaseSensitive) != -1)
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
				goodPrototype = BasicPrototypeMatchesTitle(topic, start, iterator);
				}

			if (goodPrototype)
				{
				prototypeStart = start;
				prototypeEnd = iterator;
				return true;
				}
			else
				{
				prototypeStart = limit;
				prototypeEnd = limit;
				return false;
				}
			}


		/* Function: BasicPrototypeMatchesTitle
		 * Returns whether the prototype between the iterators matches the title in the passed <Topic>.
		 */
		protected virtual bool BasicPrototypeMatchesTitle (Topic topic, TokenIterator prototypeStart, TokenIterator prototypeEnd)
			{
			string undecoratedTitle, parameters;
			Symbols.ParameterString.SplitFromParameters(topic.Title, out undecoratedTitle, out parameters);

			// If the topic is scoped, only check the last segment.  This lets "// Class: MyNamespace.MyClass" match "class MyClass {}".
			string[] separators = { ".", "::" };
			int separatorCutPoint = -1;

			// For each separator, find the last instance of it that still has content afterwards (i.e. not "Class: MyClass::").  Use the
			// rightmost one.
			foreach (var separator in separators)
				{
				int lastIndex = undecoratedTitle.LastIndexOf(separator);

				while (lastIndex != -1)
					{
					if (lastIndex + separator.Length < undecoratedTitle.Length)
						{
						if (lastIndex + separator.Length > separatorCutPoint)
							{  separatorCutPoint = lastIndex + separator.Length;  }

						break;
						}

					if (lastIndex == 0)
						{  break;  }

					lastIndex = undecoratedTitle.LastIndexOf(separator, lastIndex - 1);
					}
				}

			if (separatorCutPoint != -1)
				{  undecoratedTitle = undecoratedTitle.Substring(separatorCutPoint);  }

			// If the title starts with a common variable prefix like $ strip it off.  This lets "x" and "$x" match.  This also lets both of them
			// match "$scope::x" since the $ would be stripped off with the scope.
			if (undecoratedTitle.Length > 1 &&
				(undecoratedTitle[0] == '$' || undecoratedTitle[0] == '@' || undecoratedTitle[0] == '%'))
				{  undecoratedTitle = undecoratedTitle.Substring(1);  }

			Tokenizer tokenizer = prototypeStart.Tokenizer;

			if (tokenizer.ContainsTextBetween(undecoratedTitle, true, prototypeStart, prototypeEnd))
				{
				return true;
				}

			// If "operator" is in the string, make another attempt to see if we can match "operator +" with "operator+".
			else if (undecoratedTitle.IndexOf("operator", StringComparison.OrdinalIgnoreCase) != -1)
				{
				string undecoratedTitleOp = FindExtraOperatorWhitespaceRegex().Replace(undecoratedTitle, "");
				string prototypeString = prototypeStart.TextBetween(prototypeEnd);
				string prototypeStringOp = FindExtraOperatorWhitespaceRegex().Replace(prototypeString, "");

				if (prototypeStringOp.IndexOf(undecoratedTitleOp, StringComparison.OrdinalIgnoreCase) != -1)
					{  return true;  }
				}

			// If ".prototype." is in the string, make another attempt to see if we can match with it removed.
			else if (tokenizer.ContainsTextBetween(".prototype.", true, prototypeStart, prototypeEnd))
				{
				string prototypeString = prototypeStart.TextBetween(prototypeEnd);
				int prototypeIndex = prototypeString.IndexOf(".prototype.", StringComparison.OrdinalIgnoreCase);

				// We want to keep the trailing period so String.prototype.Function becomes String.Function.
				prototypeString = prototypeString.Substring(0, prototypeIndex) + prototypeString.Substring(prototypeIndex + 10);

				if (prototypeString.IndexOf(undecoratedTitle, StringComparison.OrdinalIgnoreCase) != -1)
					{  return true;  }
				}

			return false;
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
			return NormalizePrototype(new Tokenizer(input, tabWidth: EngineInstance.Config.TabWidth));
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
			TokenIterator end = input.EndOfTokens;

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

				else if (language.LineExtender != null && iterator.MatchesAcrossTokens(language.LineExtender))
					{
					// If the line extender is an underscore we don't want to treat it as one if it's adjacent to any text because
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
					if (input.MatchTextBetween(IsPreservedPrototypeCommentRegex(),
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


		/* Function: ApplyPrototypeAccessLevels
		 * Goes through the <Topics> and applies any <AccessLevels> it finds in the prototypes.  This will not override any existing
		 * <AccessLevels>.
		 */
		protected void ApplyPrototypeAccessLevels (List<Element> elements)
			{
			foreach (var element in elements)
				{
				if (element.Topic != null &&
					 element.Topic.Prototype != null &&
					 element.Topic.DeclaredAccessLevel == AccessLevel.Unknown)
					{
					element.Topic.DeclaredAccessLevel = element.Topic.ParsedPrototype.GetAccessLevel();

					if (element is ParentElement)
						{  (element as ParentElement).MaximumEffectiveChildAccessLevel = element.Topic.DeclaredAccessLevel;  }
					}
				}
			}


		/* Function: PreprocessEnumElements
		 *
		 * Finds any enums in the code <Elements> and adjusts any comment <Elements> appearing inside their bodies.  This is
		 * done because standard comments and/or comments of the wrong type appearing with the values can throw off merging.
		 *
		 * For now all comments inside an enum's body will be removed.  In the future appropriate comments will be merged into
		 * the code elements like inline comments are, with adjustments if necessary, and others will either be deleted or moved
		 * to outside the enum's scope.
		 */
		protected void PreprocessEnumComments (List<Element> commentElements, List<Element> codeElements)
			{
			int commentIndex = 0;
			int codeIndex =0;

			// Group comments create ParentElements that have a scope of what should be included in the group.  They end at the
			// next group comment or any other comment that affects scope.  We want group comments appearing inside enum bodies
			// to be handled differently and not affect other groups' range, so we may have to fix them.  Thus we keep track of the last
			// non-enum group comment we found in case we need to extend the end of its range.
			ParentElement lastNonEnumGroup = null;

			while (codeIndex < codeElements.Count)
				{
				if (codeElements[codeIndex] is ParentElement &&
					codeElements[codeIndex].Topic != null &&
					codeElements[codeIndex].Topic.IsEnum)
					{
					ParentElement enumElement = (ParentElement)codeElements[codeIndex];

					// Find the value elements in the enum body
					int firstValueIndex = codeIndex + 1;
					int endOfValuesIndex = firstValueIndex;

					while (endOfValuesIndex < codeElements.Count &&
							  enumElement.Contains(codeElements[endOfValuesIndex]))
						{  endOfValuesIndex++;  }

					// Walk past the comments that appear before the enum's body
					while (commentIndex < commentElements.Count &&
							  commentElements[commentIndex].FilePosition <= enumElement.FilePosition)
						{
						var commentElement = commentElements[commentIndex];

						if (commentElement.Topic != null &&
							commentElement.Topic.IsGroup)
							{  lastNonEnumGroup = (ParentElement)commentElement;  }

						commentIndex++;
						}

					// Walk through comments that appear inside the enum's body
					while (commentIndex < commentElements.Count &&
							  commentElements[commentIndex].FilePosition <= enumElement.EndingFilePosition)
						{
						var commentElement = commentElements[commentIndex];

						if (commentElement.Topic == null)
							{  commentIndex++;  continue;  }

						var commentTopic = commentElement.Topic;


						// If the topic is a group, remove it (for now)

						if (commentTopic.IsGroup)
							{
							// The group may have ended a previous group.  If so, extend the previous group.
							if (lastNonEnumGroup != null &&
								lastNonEnumGroup.EndingFilePosition == commentElement.FilePosition)
								{  lastNonEnumGroup.EndingFilePosition = (commentElement as ParentElement).EndingFilePosition;  }

							commentElements.RemoveAt(commentIndex);
							}


						// If the topic is any other type, remove it (for now)

						else
							{  commentElements.RemoveAt(commentIndex);  }
						}

					codeIndex = endOfValuesIndex;
					}

				else // not an enum topic
					{  codeIndex++;  }
				}
			}


		/* Function: MergeElements
		 * Combines code and comment <Elements> into one list.  The original <Elements> and/or <Topics> may be reused so don't use them
		 * after calling this function.
		 */
		protected List<Element> MergeElements (List<Element> commentElements, List<Element> codeElements)
			{

			// If only one of the lists has usable content we can return it as is.

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

			while (codeIndex < codeElements.Count && codeElements[codeIndex].FilePosition < commentElements[commentIndex].FilePosition)
				{
				mergedElements.Add(codeElements[codeIndex]);
				codeIndex++;
				}


			// Now find and loop through block pairs.  The comment block is all the consecutive comments appearing until the next code
			// element.  The code block is that element and all consecutive elements appearing until the next comment element or the end
			// of the file.

			while (commentIndex < commentElements.Count && codeIndex < codeElements.Count)
				{

				// Determine the lengths of the blocks

				int commentCount = 1;

				while (commentIndex + commentCount < commentElements.Count &&
						 commentElements[commentIndex + commentCount].FilePosition < codeElements[codeIndex].FilePosition)
					{  commentCount++;  }

				int codeCount = 1;

				if (commentIndex + commentCount == commentElements.Count)
					{  codeCount = codeElements.Count - codeIndex;  }
				else
					{
					while (codeIndex + codeCount < codeElements.Count &&
							 codeElements[codeIndex + codeCount].FilePosition < commentElements[commentIndex + commentCount].FilePosition)
						{  codeCount++;  }
					}


				// First see if the last comment topic is headerless.  If so, it gets merged with the first code topic and everything else is
				// added as is.

				int lastCommentIndex = commentIndex + commentCount - 1;

				// If it's embedded walk backwards to find the last non-embedded one.
				while (commentElements[lastCommentIndex].Topic != null &&
						 commentElements[lastCommentIndex].Topic.IsEmbedded == true &&
						 lastCommentIndex > commentIndex)
					{  lastCommentIndex--;  }

				if (commentElements[lastCommentIndex].Topic != null &&
					commentElements[lastCommentIndex].Topic.Title == null &&
					CanMergeTopics(commentElements[lastCommentIndex].Topic, codeElements[codeIndex].Topic, true))
					{
					// Add the comment elements prior to the last one as is.
					while (commentIndex < lastCommentIndex)
						{
						mergedElements.Add(commentElements[commentIndex]);
						commentIndex++;
						commentCount--;
						}

					var mergedElement = codeElements[codeIndex];
					mergedElement.InComments = true;
					mergedElement.Topic = MergeTopics(commentElements[lastCommentIndex].Topic, codeElements[codeIndex].Topic);
					mergedElements.Add(mergedElement);

					commentIndex++;
					commentCount--;
					codeIndex++;
					codeCount--;

					// If the merged element is an enum, that means any definition list entries in its body should be symbols to represent
					// the enum's values.  However, since it was orginally a headerless topic, it wasn't known that it was an enum when it
					// was first parsed and they were treated as regular definition list entries.  Convert them to symbol entries.
					if (mergedElement.Topic.IsEnum)
						{
						var commentParser = EngineInstance.Comments.NaturalDocsParser;

						int definitionListEntryCount = commentParser.ConvertDefinitionEntriesToSymbols(mergedElement.Topic);

						if (definitionListEntryCount > 0)
							{
							// We also have to make embedded topics and elements for them
							List<Topic> newEmbeddedTopics = new List<Topic>(definitionListEntryCount);
							commentParser.ExtractEmbeddedTopics(mergedElement.Topic, newEmbeddedTopics);

							for (int i = 0; i < definitionListEntryCount; i++)
								{
								Element newEmbeddedElement = new Element(mergedElement.Topic.CommentLineNumber, 1, Element.Flags.InComments);
								newEmbeddedElement.Topic = newEmbeddedTopics[i];

								commentElements.Insert(commentIndex + i, newEmbeddedElement);
								commentCount++;
								}
							}

						if (MergeEnumValues((ParentElement)mergedElement, commentElements, commentIndex, codeElements, codeIndex,
														ref mergedElements,
														out int embeddedCommentElementsMerged, out int embeddedCodeElementsMerged))
							{
							commentIndex += embeddedCommentElementsMerged;
							commentCount -= embeddedCommentElementsMerged;
							codeIndex += embeddedCodeElementsMerged;
							codeCount -= embeddedCodeElementsMerged;
							}
						}

					// Add the remainder of the code elements as is.
					while (codeCount > 0)
						{
						mergedElements.Add(codeElements[codeIndex]);
						codeIndex++;
						codeCount--;
						}
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
								mergedElements[mergedElements.Count - 1].FilePosition < mergedElements[mergedElements.Count - 2].FilePosition)
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

								if (mergedElement.Topic.IsEnum &&
									MergeEnumValues((ParentElement)mergedElement, commentElements, commentIndex, codeElements, codeIndex,
																ref mergedElements,
																out int embeddedCommentElementsMerged, out int embeddedCodeElementsMerged))
									{
									commentIndex += embeddedCommentElementsMerged;
									commentCount -= embeddedCommentElementsMerged;
									codeIndex += embeddedCodeElementsMerged;
									codeCount -= embeddedCodeElementsMerged;
									}
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

									if (RemoveChildElements(parentElement,
																		 commentElements, commentIndex, commentIndex + commentCount,
																		 codeElements, matchingCodeIndex + 1, codeIndex + codeCount,
																		 out int commentElementsRemovedFromBlock, out int commentElementsRemovedAfterBlock,
																		 out int codeElementsRemovedFromBlock, out int codeElementsRemovedAfterBlock))
										{
										commentCount -= commentElementsRemovedFromBlock;
										codeCount -= codeElementsRemovedFromBlock;
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


			// Now we may have to fix up the ranges of any comment-only elements.

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

						if (element.EndingFilePosition > parentElement.EndingFilePosition)
							{
							element.EndingLineNumber = parentElement.EndingLineNumber;
							element.EndingCharNumber = parentElement.EndingCharNumber;
							}
						}

					// Next, if it has a topic that starts or ends scope, that scope only extends until the next code element.   Basically if
					// you declare a comment-only class, you can add comment-only members to it but once an actual code element is
					// reached the scope reverts back to the code.  This behavior doesn't apply to groups since we want to be able to use
					// them with code elements.

					if (element.Topic != null && element.Topic.CommentTypeID != 0)
						{
						var commentType = EngineInstance.CommentTypes.FromID(element.Topic.CommentTypeID);

						if (commentType.Scope == CommentType.ScopeValue.Start ||
							commentType.Scope == CommentType.ScopeValue.End)
							{
							int nextCodeIndex = i + 1;

							while (nextCodeIndex < mergedElements.Count && mergedElements[nextCodeIndex].InCode == false)
								{  nextCodeIndex++;  }

							if (nextCodeIndex < mergedElements.Count)
								{
								var nextCodeElement = mergedElements[nextCodeIndex];

								if (element.EndingFilePosition > nextCodeElement.FilePosition)
									{
									element.EndingLineNumber = nextCodeElement.LineNumber;
									element.EndingCharNumber = nextCodeElement.CharNumber;
									}
								}
							}
						}

					// Finally, if it's a group, make sure it doesn't encompass any other ParentElements except enums.

					if (element.Topic != null && element.Topic.IsGroup)
						{
						for (int j = i + 1; j < mergedElements.Count && element.Contains(mergedElements[j]); j++)
							{
							if (mergedElements[j] is ParentElement &&
								(mergedElements[j].Topic == null || mergedElements[j].Topic.IsEnum == false))
								{
								element.EndingLineNumber = mergedElements[j].LineNumber;
								element.EndingCharNumber = mergedElements[j].CharNumber;
								break;
								}
							}
						}
					}
				}


			return mergedElements;
			}


		/* Function: MergeEnumValues
		 *
		 * If you've just merged an enum, this will merge the code elements for the values with any embedded topics from the
		 * comment.  It will return how many elements were merged from each list.
		 *
		 * - commentElementIndex should be an index into commentElements pointing to the first potential embedded topic,
		 *   not to parent element.  Likewise for codeElementIndex and codeElements.
		 *
		 *	- The merged embedded elements will be added to the end of the mergedElements list.  As such the merged parent
		 *	  enum should already be on the list as the last element prior to calling.
		 *
		 *	- As with <MergeElements()> the original <Elements> and/or <Topics> may be reused so don't use them after
		 *	  calling this function.
		 *
		 *	- It will return the amount of elements merged from each list in embeddedCommentElementsMerged and
		 *	  embeddedCodeElementsMerged.  The calling code should skip ahead that many in its list processing.
		 */
		protected bool MergeEnumValues (ParentElement enumElement,
														  List<Element> commentElements, int commentElementIndex,
														  List<Element> codeElements, int codeElementIndex,
														  ref List<Element> mergedElements,
														  out int embeddedCommentElementsMerged,
														  out int embeddedCodeElementsMerged)
			{
			// See how many embedded topics there are in each list.

			int commentEmbeddedTopicCount = CountConsecutiveEmbeddedTopics(commentElements, commentElementIndex);
			int codeEmbeddedTopicCount = CountChildElements(enumElement, codeElements, codeElementIndex);


			// Merge the elements.

			if (commentEmbeddedTopicCount == 0)
				{
				// If there are no embedded comment or code topics we can just return.
				if (codeEmbeddedTopicCount == 0)
					{
					embeddedCommentElementsMerged = 0;
					embeddedCodeElementsMerged = 0;
					return false;
					}

				// If there are no embedded comment topics but we do have code topics, we can add them as is.
				else
					{
					for (int i = codeElementIndex; i < codeElementIndex + codeEmbeddedTopicCount; i++)
						{  mergedElements.Add(codeElements[i]);  }

					embeddedCommentElementsMerged = 0;
					embeddedCodeElementsMerged = codeEmbeddedTopicCount;
					return true;
					}
				}

			// If there are embedded comment topics but no embedded code topics we can add them as is.
			else if (codeEmbeddedTopicCount == 0)
				{
				for (int i = commentElementIndex; i < commentElementIndex + commentEmbeddedTopicCount; i++)
					{  mergedElements.Add(commentElements[i]);  }

				embeddedCommentElementsMerged = commentEmbeddedTopicCount;
				embeddedCodeElementsMerged = 0;
				return true;
				}

			// If both have embedded topics, we must compare the lists.
			else
				{
				// Start by adding every code element.  If there is a matching comment element we'll use its Topic since its description
				// should be used.

				StringComparison comparisonType =(this.Language.CaseSensitive ?
																	  StringComparison.InvariantCulture :
																	  StringComparison.InvariantCultureIgnoreCase);

				for (int codeEmbeddedElementIndex = codeElementIndex;
					  codeEmbeddedElementIndex < codeElementIndex + codeEmbeddedTopicCount;
					  codeEmbeddedElementIndex++)
					{
					Element codeEmbeddedElement = codeElements[codeEmbeddedElementIndex];
					Topic codeEmbeddedTopic = codeEmbeddedElement.Topic;

					for (int commentEmbeddedElementIndex = commentElementIndex;
						  commentEmbeddedElementIndex < commentElementIndex + commentEmbeddedTopicCount;
						  commentEmbeddedElementIndex++)
						{
						Element commentEmbeddedElement = commentElements[commentEmbeddedElementIndex];
						Topic commentEmbeddedTopic = commentEmbeddedElement.Topic;

						if (commentEmbeddedTopic != null &&
							string.Compare(codeEmbeddedTopic.Title, commentEmbeddedTopic.Title, comparisonType) == 0)
							{
							// Replace the code topic with the comment's
							codeEmbeddedElement.Topic = commentEmbeddedTopic;

							// Set the comment's to null so we don't reuse it and we can tell which ones weren't used later
							commentEmbeddedElement.Topic = null;

							break;
							}
						}

					// Add the code element whether there was a comment match or not
					mergedElements.Add(codeEmbeddedElement);
					}


				// Now go through all the comment elements to add any that didn't have a match, which we'll recognize because
				// they didn't have their Topics set to null in the last stage.

				// Reuse the last added element's file position because the elements must appear in source order.
				FilePosition lastFilePosition = mergedElements[mergedElements.Count - 1].FilePosition;

				for (int commentEmbeddedElementIndex = commentElementIndex;
					  commentEmbeddedElementIndex < commentElementIndex + commentEmbeddedTopicCount;
					  commentEmbeddedElementIndex++)
					{
					Element commentEmbeddedElement = commentElements[commentEmbeddedElementIndex];

					if (commentEmbeddedElement.Topic != null)
						{
						commentEmbeddedElement.FilePosition = lastFilePosition;
						mergedElements.Add(commentEmbeddedElement);
						}
					}

				embeddedCommentElementsMerged = commentEmbeddedTopicCount;
				embeddedCodeElementsMerged = codeEmbeddedTopicCount;
				return true;
				}
			}


		/* Function: RemoveChildElements
		 *
		 * Removes all the <Elements> from both lists which are a child of the passed <ParentElement>.  Returns whether it
		 * removed anything, with specific counts in separate variables.
		 *
		 * Since this is called from code that processes <Elements> in blocks, you can specify the index of the end of each block
		 * to separate the counts into those removed from inside the block and after.  This is useful because only the ones removed
		 * from inside it will shrink the size of the block.
		 */
		protected bool RemoveChildElements (ParentElement parent,
																List<Element> commentElements, int firstCommentChildIndex, int endOfCommentBlockIndex,
																List<Element> codeElements, int firstCodeChildIndex, int endOfCodeBlockIndex,
																out int commentElementsRemovedFromBlock, out int commentElementsRemovedAfterBlock,
																out int codeElementsRemovedFromBlock, out int codeElementsRemovedAfterBlock)
			{
			commentElementsRemovedFromBlock = 0;
			commentElementsRemovedAfterBlock = 0;

			while (firstCommentChildIndex < commentElements.Count &&
					  parent.Contains(commentElements[firstCommentChildIndex]))
				{
				if (firstCommentChildIndex < endOfCommentBlockIndex)
					{
					commentElementsRemovedFromBlock++;
					endOfCommentBlockIndex--;
					}
				else
					{  commentElementsRemovedAfterBlock++;  }

				commentElements.RemoveAt(firstCommentChildIndex);
				}


			codeElementsRemovedFromBlock = 0;
			codeElementsRemovedAfterBlock = 0;

			while (firstCodeChildIndex < codeElements.Count &&
					  parent.Contains(codeElements[firstCodeChildIndex]))
				{
				if (firstCodeChildIndex < endOfCodeBlockIndex)
					{
					codeElementsRemovedFromBlock++;
					endOfCodeBlockIndex--;
					}
				else
					{  codeElementsRemovedAfterBlock++;  }

				codeElements.RemoveAt(firstCodeChildIndex);
				}


			return (commentElementsRemovedFromBlock > 0 ||
					   commentElementsRemovedAfterBlock > 0 ||
					   codeElementsRemovedFromBlock > 0 ||
					   codeElementsRemovedAfterBlock > 0);
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
				if (commentTopic.CommentTypeID == 0)
					{  throw new Exception ("All comment topics with titles must have comment type IDs before calling CanMergeTopics().");  }
				#endif

				// Documentation and file topics should not be merged with code.  Headerless topics are assumed to be code.
				if (EngineInstance.CommentTypes.FromID(commentTopic.CommentTypeID).IsCode == false)
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

			Topic mergedTopic = new Topic(EngineInstance.CommentTypes);

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

			// Prototype - If one was specified in the comment it overrides the code.
			if (commentTopic.Prototype != null)
				{  mergedTopic.Prototype = commentTopic.Prototype;  }
			else
				{  mergedTopic.Prototype = codeTopic.Prototype;  }

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

			// CommentTypeID - If the user specified one, we always want to use that.
			if (commentTopic.CommentTypeID != 0)
				{
				mergedTopic.CommentTypeID = commentTopic.CommentTypeID;

				// If this changed whether the topic defines a class, reset the symbols to the comment's
				if (commentTopic.DefinesClass != codeTopic.DefinesClass)
					{
					mergedTopic.Symbol = commentTopic.Symbol;
					mergedTopic.ClassString = commentTopic.ClassString;
					}
				}
			else
				{  mergedTopic.CommentTypeID = codeTopic.CommentTypeID;  }

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


		/* Function: CountConsecutiveEmbeddedTopics
		 * Returns the number of consecutive embedded <Topics> that are present in the list starting at the passed index, or zero if
		 * none.  Each <Element> must have a <Topic> present with <Topic.IsEmbedded> set to be included.
		 */
		protected int CountConsecutiveEmbeddedTopics (List<Element> elements, int startingIndex)
			{
			int endingIndex = startingIndex;

			while (endingIndex < elements.Count &&
					 elements[endingIndex].Topic != null &&
					 elements[endingIndex].Topic.IsEmbedded)
				{  endingIndex++;  }

			return (endingIndex - startingIndex);
			}


		/* Function: CountChildElements
		 * Returns the number of consecutive child <Elements> that follow the passed <ParentElement>.
		 */
		protected int CountChildElements (ParentElement parentElement, List<Element> elements, int startingIndex)
			{
			int endingIndex = startingIndex;

			while (endingIndex < elements.Count &&
					 parentElement.Contains(elements[endingIndex]))
				{  endingIndex++;  }

			return (endingIndex - startingIndex);
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


		/* Function: AddEnumValuesToBodies
		 * Goes through all the enum <Topics> in the list of <Elements> and makes sure all embedded value <Topics> are also
		 * represented in the enum's body via a definition list.  If any are missing it will add them.
		 */
		protected void AddEnumValuesToBodies (List<Element> elements)
			{
			int elementIndex = 0;
			while (elementIndex < elements.Count)
				{
				// If the topic is an enum...
				if (elements[elementIndex].Topic != null &&
					elements[elementIndex].Topic.IsList == false &&
					elements[elementIndex].Topic.IsEnum)
					{
					var enumElement = (ParentElement)elements[elementIndex];
					var enumTopic = enumElement.Topic;
					var enumBody = enumTopic.Body;
					elementIndex++;

					int firstValueElementIndex = elementIndex;
					int valueCount = CountChildElements(enumElement, elements, firstValueElementIndex);
					elementIndex += valueCount;

					// If the enum topic has values...
					if (valueCount > 0)
						{

						// If there's no existing body just add them all in order
						if (enumBody == null)
							{
							StringBuilder newBody = new StringBuilder();

							newBody.Append("<h>" + Locale.Get("NaturalDocs.Engine", "Heading.EnumValues") + "</h>");
							newBody.Append("<dl>");

							for (int i = 0; i < valueCount; i++)
								{
								var topic = elements[firstValueElementIndex + i].Topic;

								AddAsDefinitionListSymbol(topic, newBody);
								topic.IsEmbedded = true;
								}

							newBody.Append("</dl>");
							enumTopic.Body = newBody.ToString();
							}

						// If the enum element already has a body...
						else
							{

							// For each value, find the index of where it appears in the enum topic's body.

							// The array values are initialized to zero, and it's okay to use that to mean "undefined" instead of -1
							// because no value will ever have a body index of zero.  The index is to the "<ds>" which must appear
							// after a "<dl>".
							int[] bodyIndexOfValue = new int[valueCount];

							int bodyIndex = 0;

							while (GetNextDefinitionListSymbol(enumBody, bodyIndex, out int valueTagIndex, out int valueNameIndex,
																				out int valueNameLength, out int valueEndIndex))
								{
								// Find a matching unused value element
								for (int i = 0; i < valueCount; i++)
									{
									var valueElement = elements[firstValueElementIndex + i];
									var valueTopic = valueElement.Topic;

									if (bodyIndexOfValue[i] == 0 &&
										valueTopic.Title.Length == valueNameLength &&
										string.Compare(enumBody, valueNameIndex, valueTopic.Title, 0, valueNameLength) == 0)
										{
										bodyIndexOfValue[i] = valueTagIndex;
										break;
										}
									}

								bodyIndex = valueEndIndex;
								}


							// Now that bodyIndexOfValue is filled in, get some summary information about it such as whether all
							// the values are represented in the body and if they're in the same order.

							bool allValuesInBody = true;
							bool anyValuesInBody = false;
							bool allValuesInOrder = true;
							int bodyIndexOfLastValueInBody = 0;
							int lastDefinedBodyIndex = 0;

							for (int i = 0; i < valueCount; i++)
								{
								if (bodyIndexOfValue[i] == 0)
									{
									allValuesInBody = false;
									}
								else
									{
									anyValuesInBody = true;

									if (bodyIndexOfValue[i] < lastDefinedBodyIndex)
										{  allValuesInOrder = false;  }

									lastDefinedBodyIndex = bodyIndexOfValue[i];

									if (bodyIndexOfValue[i] > bodyIndexOfLastValueInBody)
										{  bodyIndexOfLastValueInBody = bodyIndexOfValue[i];  }
									}
								}


							// If there are no values in the body, we can just add them to the end in a new section.

							if (!anyValuesInBody)
								{
								StringBuilder newBody = new StringBuilder(enumBody);

								newBody.Append("<h>" + Locale.Get("NaturalDocs.Engine", "Heading.EnumValues") + "</h>");
								newBody.Append("<dl>");

								for (int i = 0; i < valueCount; i++)
									{
									var topic = elements[firstValueElementIndex + i].Topic;

									AddAsDefinitionListSymbol(topic, newBody);
									topic.IsEmbedded = true;
									}

								newBody.Append("</dl>");
								enumElement.Topic.Body = newBody.ToString();
								}


							// Otherwise if there's some values in the body but not all, we have to add the rest in.

							else if (!allValuesInBody)
								{

								// If the existing ones aren't in any discernable order, just add the missing ones to the end of the list.

								if (!allValuesInOrder)
									{
									int insertionPoint = GetEndOfDefinitionListEntry(enumBody, bodyIndexOfLastValueInBody);

									// Create the new body.
									StringBuilder newBody = new StringBuilder();

									newBody.Append(enumBody, 0, insertionPoint);

									for (int i = 0; i < valueCount; i++)
										{
										if (bodyIndexOfValue[i] == 0)
											{
											var topic = elements[firstValueElementIndex + i].Topic;

											AddAsDefinitionListSymbol(topic, newBody);
											topic.IsEmbedded = true;
											}
										}

									newBody.Append(enumBody, insertionPoint, enumBody.Length - insertionPoint);
									enumTopic.Body = newBody.ToString();
									}

								else // allValuesInOrder
									{
									// Create the new body.
									StringBuilder newBody = new StringBuilder();

									int valueIndex = 0;
									int valueInBodyIndex = 0;
									int lastBodyIndex = 0;

									for (;;)
										{
										// Skip past any values that don't appear in the body.
										while (valueInBodyIndex < valueCount &&
												 bodyIndexOfValue[valueInBodyIndex] == 0)
											{  valueInBodyIndex++;  }

										if (valueInBodyIndex < valueCount)
											{
											// Add the body between the last point we added and the beginning of this value to the
											// new body.
											if (bodyIndexOfValue[valueInBodyIndex] > lastBodyIndex)
												{
												newBody.Append(enumBody, lastBodyIndex, bodyIndexOfValue[valueInBodyIndex] - lastBodyIndex);
												lastBodyIndex = bodyIndexOfValue[valueInBodyIndex];
												}

											// Add the values that don't appear in the body to it.
											while (valueIndex < valueInBodyIndex)
												{
												var topic = elements[firstValueElementIndex + valueIndex].Topic;

												AddAsDefinitionListSymbol(topic, newBody);
												topic.IsEmbedded = true;

												valueIndex++;
												}

											// Increment both to start this process again on the next one.
											valueIndex++;
											valueInBodyIndex++;
											}

										else // there's no more values that appear in the body
											{
											// Add all the body between the last point we added and the end of the last value entry.
											int afterLastValueInBodyIndex = GetEndOfDefinitionListEntry(enumBody, bodyIndexOfLastValueInBody);

											if (afterLastValueInBodyIndex > lastBodyIndex)
												{
												newBody.Append(enumBody, lastBodyIndex, afterLastValueInBodyIndex - lastBodyIndex);
												lastBodyIndex = afterLastValueInBodyIndex;
												}

											// Add all remaining values that don't appear in the body to it.
											while (valueIndex < valueCount)
												{
												var topic = elements[firstValueElementIndex + valueIndex].Topic;

												AddAsDefinitionListSymbol(topic, newBody);
												topic.IsEmbedded = true;

												valueIndex++;
												}

											// Add the last of the original body to it.
											if (lastBodyIndex < enumBody.Length)
												{
												newBody.Append(enumBody, lastBodyIndex, enumBody.Length - lastBodyIndex);
												lastBodyIndex = enumBody.Length;
												}

											break;
											}
										}

									enumTopic.Body = newBody.ToString();
									}
								}
							}
						}

					// elementIndex was already incremented
					}

				else // not an enum
					{  elementIndex++;  }
				}
			}


		/* Function: GetNextDefinitionListSymbol
		 * Walks through the passed <NDMarkup>, stopping on definition list symbols enclosed in "<ds>" tags.  Returns whether
		 * it found one, and if so, also returns its tag index (where the "<ds>" is), the name index and length, and the ending
		 * index (after the "</ds>".)  You can pass an index into the <NDMarkup> where the search should start from.
		 */
		protected bool GetNextDefinitionListSymbol (string ndMarkup, int searchStartIndex, out int tagIndex, out int nameIndex,
																		out int nameLength, out int endIndex)
			{
			tagIndex = ndMarkup.IndexOf("<ds>", searchStartIndex);

			if (tagIndex == -1)
				{
				nameIndex = -1;
				nameLength = 0;
				endIndex = -1;
				return false;
				}

			nameIndex = tagIndex + 4;  // move past "<ds>"
			endIndex = ndMarkup.IndexOf("</ds>", nameIndex);

			if (endIndex == -1)
				{
				tagIndex = -1;
				nameIndex = -1;
				nameLength = 0;
				return false;
				}

			nameLength = endIndex - nameIndex;
			endIndex += 5;  // move past "</ds>"

			return true;
			}


		/* Function: AddAsDefinitionListSymbol
		 * Adds the passed <Topic> as a definition list entry to the end of the StringBuilder.
		 */
		protected void AddAsDefinitionListSymbol (Topic topic, StringBuilder stringBuilder)
			{
			stringBuilder.Append("<ds>" + topic.Title + "</ds>");
			stringBuilder.Append("<dd>");

			if (topic.Body != null)
				{  stringBuilder.Append(topic.Body);  }

			stringBuilder.Append("</dd>");
			}


		/* Function: GetEndOfDefinitionListEntry
		 * Assuming the index is on the beginning of a definition list entry (the "<de>" or "<ds>" tag), returns the index of the
		 * end of the entry (after the "</dd>" tag.)
		 */
		protected int GetEndOfDefinitionListEntry (string ndMarkup, int beginningOfDefinitionListEntry)
			{
			#if DEBUG
			if (string.Compare(ndMarkup, beginningOfDefinitionListEntry, "<de>", 0, 4) != 0 &&
				string.Compare(ndMarkup, beginningOfDefinitionListEntry, "<ds>", 0, 4) != 0)
				{  throw new Exception("beginningOfDefinitionListEntry must be on a <de> or a <ds>.");  }
			#endif

			// Find the following "</dd>".  +13 since we have to go at least that far to skip the "<ds>", "</ds>", and "<dd>".
			int endOfDefinitionListEntry = ndMarkup.IndexOf("</dd>", beginningOfDefinitionListEntry + 13);

			#if DEBUG
			if (endOfDefinitionListEntry == -1)
				{  throw new Exception("endOfDefinitionListEntry shouldn't be -1 because there should always be a </dd> following it.");  }
			#endif

			// Now move past the "</dd>"
			endOfDefinitionListEntry += 5;

			return endOfDefinitionListEntry;
			}


		/* Function: AddAutomaticGrouping
		 * Adds automatic grouping to the <Elements>.  Returns whether anything was changed.
		 */
		protected bool AddAutomaticGrouping (List<Element> elements)
			{
			// For the first pass determine if we already have groups and recurse into sub groups.

			bool hasGroups = false;
			int groupsAdded = 0;

			int i = 0;
			while (i < elements.Count)
				{
				if (elements[i] is ParentElement)
					{
					ParentElement subParent = (ParentElement)elements[i];

					if (subParent.Topic != null && subParent.Topic.IsGroup)
						{  hasGroups = true;  }

					if (subParent.Topic == null || (subParent.Topic.IsEnum == false && subParent.Topic.IsList == false))
						{
						// We do this even if the element is a group because it may hold an embedded class that needs to be grouped.
						groupsAdded += AddAutomaticGroupingToParent(elements, i);
						}

					do
						{  i++;  }
					while (i < elements.Count && subParent.Contains(elements[i]));
					}
				else
					{  i++;  }
				}


			// For the second pass add groups to each stretch of non-parent topics in this scope.

			if (hasGroups == false)
				{
				i = 0;
				while (i < elements.Count)
					{
					if ( (elements[i] is ParentElement) == false ||
						 (elements[i].Topic != null && (elements[i].Topic.IsEnum || elements[i].Topic.IsList)) )
						{
						int startIndex = i;

						do
							{
							if (elements[i] is ParentElement)
								{
								ParentElement subParent = (ParentElement)elements[i];

								if (subParent.Topic != null && (subParent.Topic.IsEnum || subParent.Topic.IsList))
									{
									do
										{  i++;  }
									while (i < elements.Count && subParent.Contains(elements[i]));
									}
								else
									{  break;  }
								}
							else // not a ParentElement
								{  i++;  }
							}
						while (i < elements.Count);

						int newGroups = AddAutomaticGroupingToRange(elements, startIndex, i);

						groupsAdded += newGroups;
						i += newGroups;
						}

					else // element is a non-enum ParentElement
						{
						ParentElement subParent = (ParentElement)elements[i];

						do
							{  i++;  }
						while (i < elements.Count && subParent.Contains(elements[i]));
						}
					}
				}

			return (groupsAdded > 0);
			}


		/* Function: AddAutomaticGroupingToParent
		 * Adds automatic grouping to the <Elements> that belong to the passed parent.  It will return the number of elements
		 * added to the list, or zero if none.
		 */
		protected int AddAutomaticGroupingToParent (List<Element> elements, int parentIndex)
			{
			#if DEBUG
			if ((elements[parentIndex] is ParentElement) == false)
				{  throw new Exception ("Called AddAutomaticGroupingToParent() with an element that wasn't a ParentElement.");  }
			#endif


			// For the first pass determine if we already have groups and recurse into sub groups.  We also have to deal with the
			// possibility that the parent is itself a group.

			ParentElement parent = (ParentElement)elements[parentIndex];

			bool hasGroups = (parent.Topic != null && parent.Topic.IsGroup);
			int groupsAdded = 0;

			int i = parentIndex + 1;
			while (i < elements.Count && parent.Contains(elements[i]))
				{
				if (elements[i] is ParentElement)
					{
					ParentElement subParent = (ParentElement)elements[i];

					if (subParent.Topic != null && subParent.Topic.IsGroup)
						{  hasGroups = true;  }

					if (subParent.Topic == null || (subParent.Topic.IsEnum == false && subParent.Topic.IsList == false))
						{
						// We do this even if the element is a group because it may hold an embedded class that needs to be grouped.
						groupsAdded += AddAutomaticGroupingToParent(elements, i);
						}

					do
						{  i++;  }
					while (i < elements.Count && subParent.Contains(elements[i]));
					}
				else
					{  i++;  }
				}


			// For the second pass add groups to each stretch of non-parent topics in this scope.

			if (hasGroups == false)
				{
				i = parentIndex + 1;
				while (i < elements.Count && parent.Contains(elements[i]))
					{
					if ( (elements[i] is ParentElement) == false ||
						 (elements[i].Topic != null && (elements[i].Topic.IsEnum || elements[i].Topic.IsList)) )
						{
						int startIndex = i;

						do
							{
							if (elements[i] is ParentElement)
								{
								ParentElement subParent = (ParentElement)elements[i];

								if (subParent.Topic != null && (subParent.Topic.IsEnum || subParent.Topic.IsList))
									{
									do
										{  i++;  }
									while (i < elements.Count && subParent.Contains(elements[i]));
									}
								else
									{  break;  }
								}
							else // not a ParentElement
								{  i++;  }
							}
						while (i < elements.Count && parent.Contains(elements[i]));

						int newGroups = AddAutomaticGroupingToRange(elements, startIndex, i);

						groupsAdded += newGroups;
						i += newGroups;
						}

					else // element is a non-enum ParentElement
						{
						ParentElement subParent = (ParentElement)elements[i];

						do
							{  i++;  }
						while (i < elements.Count && subParent.Contains(elements[i]));
						}
					}
				}

			return groupsAdded;
			}


		/* Function: AddAutomaticGroupingToRange
		 * Adds automatic grouping to the <Elements> between the indexes.  It assumes they are all in the same scope and there are
		 * no <ParentElements> in this range.  It will return the number of elements added to the list, or zero if none.
		 */
		protected int AddAutomaticGroupingToRange (List<Element> elements, int startIndex, int endIndex)
			{
			if (endIndex <= startIndex)
				{  return 0;  }

			#if DEBUG
			ParentElement firstParent = null;
			int firstParentIndex = FindElementParent(elements, startIndex);

			if (firstParentIndex != -1)
				{  firstParent = (ParentElement)elements[firstParentIndex];  }

			for (int j = startIndex; j < endIndex; j++)
				{
				if (elements[j] is ParentElement && (elements[j].Topic == null || (elements[j].Topic.IsEnum == false && elements[j].Topic.IsList == false)))
					{  throw new Exception("Cannot use AddAutomaticGroupingToRange() with ranges that contain ParentElements other than enums and list topics.");  }
				if (firstParent != null && firstParent.Contains(elements[j]) == false)
					{  throw new Exception("All elements passed to AddAutomaticGroupingToRange() must be part of the same scope.");  }
				}
			#endif

			if (EngineInstance.CommentTypes.GroupCommentTypeID == 0)
				{  return 0;  }

			CommentType lastCommentType = null;
			int groupsAdded = 0;
			ParentElement lastGroupAdded = null;
			int lastGroupAddedIndex = -1;

			int i = startIndex;
			while (i < endIndex)
				{
				if (elements[i].Topic == null)
					{
					i++;
					continue;
					}

				int effectiveCommentTypeID = elements[i].Topic.CommentTypeID;

				if (elements[i].Topic.IsEnum)
					{
					int typeCommentTypeID = EngineInstance.CommentTypes.IDFromKeyword("type", language.ID);

					if (typeCommentTypeID != 0)
						{  effectiveCommentTypeID = typeCommentTypeID;  }
					}

				if (lastCommentType == null || lastCommentType.ID != effectiveCommentTypeID)
					{
					if (lastGroupAdded != null)
						{
						lastGroupAdded.EndingLineNumber = elements[i].LineNumber;
						lastGroupAdded.EndingCharNumber = elements[i].CharNumber;
						}

					lastCommentType = EngineInstance.CommentTypes.FromID(effectiveCommentTypeID);
					bool addGroup = true;

					// Don't group on files if they're the first topic in the file.
					if (lastCommentType.IsFile)
						{
						addGroup = false;

						for (int j = 0; j < i; j++)
							{
							if (elements[j].Topic != null)
								{
								addGroup = true;
								break;
								}
							}
						}

					if (addGroup)
						{
						Topic newGroupTopic = new Topic(EngineInstance.CommentTypes);
						newGroupTopic.CommentTypeID = EngineInstance.CommentTypes.GroupCommentTypeID;
						newGroupTopic.Title = lastCommentType.PluralDisplayName;

						ParentElement newGroupElement = new ParentElement(elements[i].LineNumber, elements[i].CharNumber, 0);
						newGroupElement.Topic = newGroupTopic;

						elements.Insert(i, newGroupElement);
						lastGroupAdded = newGroupElement;
						lastGroupAddedIndex = i;

						groupsAdded++;
						i++;
						endIndex++;
						}
					}

				if (elements[i] is ParentElement)
					{
					ParentElement subParent = (ParentElement)elements[i];

					do
						{  i++;  }
					while (i < elements.Count && subParent.Contains(elements[i]));
					}
				else
					{  i++;  }
				}

			if (lastGroupAdded != null)
				{
				if (endIndex < elements.Count)
					{
					lastGroupAdded.EndingLineNumber = elements[endIndex].LineNumber;
					lastGroupAdded.EndingCharNumber = elements[endIndex].CharNumber;
					}
				else
					{
					lastGroupAdded.EndingLineNumber = int.MaxValue;
					lastGroupAdded.EndingCharNumber = int.MaxValue;
					}

				int parentIndex = FindElementParent(elements, lastGroupAddedIndex);

				if (parentIndex != -1 && (elements[parentIndex] as ParentElement).EndingFilePosition < lastGroupAdded.EndingFilePosition)
					{
					lastGroupAdded.EndingLineNumber = (elements[parentIndex] as ParentElement).EndingLineNumber;
					lastGroupAdded.EndingCharNumber = (elements[parentIndex] as ParentElement).EndingCharNumber;
					}
				}

			return groupsAdded;
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

					CommentType commentType = EngineInstance.CommentTypes.FromID(topic.CommentTypeID);

					string ignore;
					SymbolString topicSymbol = SymbolString.FromPlainText(topic.Title, out ignore);

					// This could happen with something like "Topic: ."
					if (topicSymbol == null)
						{
						element.Topic = null;
						continue;
						}

					int parentIndex = FindElementParent(elements, i);
					while (parentIndex != -1 && (elements[parentIndex] as ParentElement).ChildContextStringSet == false)
						{  parentIndex = FindElementParent(elements, parentIndex);  }

					ContextString parentContext;
					if (parentIndex == -1)
						{  parentContext = new ContextString();  }
					else
						{  parentContext = (elements[parentIndex] as ParentElement).ChildContextString;  }


					// Set Topic.Symbol

					if (commentType.Scope == CommentType.ScopeValue.Normal)
						{  topic.Symbol = parentContext.Scope + topicSymbol;  }
					else // Scope is Start, End, or AlwaysGlobal
						{  topic.Symbol = topicSymbol;  }


					// Set Topic.ClassString and ParentElement.DefaultChildClassString if appropriate

					if (commentType.Scope == CommentType.ScopeValue.Start && commentType.InHierarchy)
						{
						var hierarchy = EngineInstance.Hierarchies.FromID(commentType.HierarchyID);
						Language language = EngineInstance.Languages.FromID(topic.LanguageID);

						ClassString classString = ClassString.FromParameters(hierarchy.ID, language.ID, language.CaseSensitive, topicSymbol);

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
						Language.EnumValues enumValue = 0;
						if (commentType.IsEnum == true)
							{  enumValue = Manager.FromID(topic.LanguageID).EnumValue;  }

						if (commentType.Scope == CommentType.ScopeValue.Start ||
						    (commentType.IsEnum == true && enumValue == Language.EnumValues.UnderType))
							{
							ContextString newContext = new ContextString();
							newContext.Scope = topic.Symbol;
							(element as ParentElement).ChildContextString = newContext;
							}
						else if (commentType.Scope == CommentType.ScopeValue.End ||
								  (commentType.IsEnum == true && enumValue == Language.EnumValues.Global))
							{
							(element as ParentElement).ChildContextString = new ContextString();
							}
						else if (commentType.IsEnum == true && enumValue == Language.EnumValues.UnderParent)
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


		/* Function: ExtractClassParentLinks
		 * Fills in <Element.ClassParentLinks> for any relevant <Topics>.  It is assumed that <Topics> already have all <ClassStrings>,
		 * <ContextStrings>, language ID, and file ID set.
		 */
		protected void ExtractClassParentLinks (List<Element> elements)
			{
			foreach (var element in elements)
				{
				if (element.Topic == null)
					{  continue;  }

				var topic = element.Topic;
				var parsedClassPrototype = topic.ParsedClassPrototype;

				if (parsedClassPrototype == null)
					{  continue;  }

				int parentCount = parsedClassPrototype.NumberOfParents;

				for (int i = 0; i < parentCount; i++)
					{
					Link link = new Link();
					link.Type = LinkType.ClassParent;

					TokenIterator start, end;
					parsedClassPrototype.GetParentName(i, out start, out end);

					link.Symbol = SymbolString.FromPlainText_NoParameters( start.TextBetween(end) );

					// Use BodyContext instead of PrototypeContext so that the parent's scope is included.  So for this:
					//
					//    // Class: X.Y.MyClass
					//    class MyClass : public MyParent
					//
					// "MyParent" could be interpreted as X.Y.MyParent in addition to global.
					//
					link.Context = topic.BodyContext;

					link.FileID = topic.FileID;
					link.ClassString = topic.ClassString;
					link.LanguageID = topic.LanguageID;
					// Don't need to fill in EndingSymbol

					if (element.ClassParentLinks == null)
						{  element.ClassParentLinks = new List<Link>();  }

					element.ClassParentLinks.Add(link);
					}
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


		/* Function: DetectParameterStyle
		 * Determines whether the *single* parameter between the iterators uses the C or Pascal style.  Note that a Pascal prototype
		 * may contain individual parameters that look like C style parameters, but it should always have at least one that looks like
		 * a Pascal style parameter.
		 */
		protected ParameterStyle DetectParameterStyle (TokenIterator start, TokenIterator end)
			{
			TokenIterator iterator = start;

			while (iterator < end)
				{
				// Quit early if we found a default value expression
				if (iterator.Character == '=' || iterator.MatchesAcrossTokens(":="))
					{  break;  }

				// Skip double colons so we're not confused by C++ ::Globals
				else if (iterator.MatchesAcrossTokens("::"))
					{  iterator.Next(2);  }

				// Can only check for a colon after checking for := and ::
				else if (iterator.Character == ':')
					{  return ParameterStyle.Pascal;  }

				else if (TryToSkipTypeOrVarName(ref iterator, end) ||
						   TryToSkipComment(ref iterator) ||
						   TryToSkipString(ref iterator) ||
						   TryToSkipBlock(ref iterator, true))
					{
					// There may be comments in the prototype if it's something we allowed there like a Splint comment or /*out*/.

					// Strings don't really make sense in the prototype until the default value, but we need the parser to handle it
					// anyway just so it doesn't lose its mind if one occurs.

					// If we come across a block that doesn't immediately follow an identifier, it may be something like a C# property.
					}

				// Skip over whitespace plus any unexpected random symbols that appear.
				else
					{  iterator.Next();  }
				}

			// If we didn't find anything Pascal, then we're C.
			return ParameterStyle.C;
			}


		/* Function: DetectParameterStyle
		 * Determines whether the parameters in this prototype use the C or Pascal style.
		 */
		protected ParameterStyle DetectParameterStyle (ParsedPrototype prototype)
			{
			if (prototype.NumberOfParameters == 0)
				{  return ParameterStyle.Unknown;  }

			// We have to go through all the parameters to see if any are Pascal-style since some may appear as C-style.  For
			// example:
			//
			// Function FunctionName (const a, b: string): integer;
			//
			// "const a" could be seen as a C-style parameter with type "const" and name "a".  It's only when we get to the second
			// parameter that we see it's Pascal.

			TokenIterator start, end;

			for (int i = 0; i < prototype.NumberOfParameters; i++)
				{
				prototype.GetParameter(i, out start, out end);

				if (DetectParameterStyle(start, end) == ParameterStyle.Pascal)
					{  return ParameterStyle.Pascal;  }
				}

			// If we didn't find anything Pascal then we're C.
			return ParameterStyle.C;
			}


		/* Function: MarkCParameter
		 * Marks the tokens in the C-style parameter specified by the bounds with <PrototypeParsingTypes>.
		 */
		protected void MarkCParameter (TokenIterator start, TokenIterator end)
			{
			// Pass 1: Count the number of "words" in the parameter prior to the default value and mark the default value
			// separator.  We'll figure out how to interpret the words in the second pass.

			int words = 0;
			TokenIterator iterator = start;

			while (iterator < end)
				{

				// Default values

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

					TokenIterator lookbehind = endOfDefaultValue;
					lookbehind.Previous();

					while (lookbehind >= iterator && lookbehind.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
						{
						endOfDefaultValue = lookbehind;
						lookbehind.Previous();
						}

					endOfDefaultValue.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, iterator);

					if (iterator < endOfDefaultValue)
						{  iterator.SetPrototypeParsingTypeBetween(endOfDefaultValue, PrototypeParsingType.DefaultValue);  }

					break;
					}


				// Param separator

				else if (iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
					{  break;  }


				// "Words" we're interested in

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


				// Whitespace and any unexpected random symbols

				else
					{  iterator.Next();  }
				}


			// Pass 2: Mark the "words" we counted from the first pass.  The order of words goes [modifier] [modifier] [type] [name],
			// starting from the right.  Typeless languages that only have one word will have it correctly interpreted as the name.

			iterator = start;
			TokenIterator wordStart, wordEnd;

			while (iterator < end)
				{
				wordStart = iterator;
				bool foundWord = false;
				bool foundBlock = false;

				if (iterator.PrototypeParsingType == PrototypeParsingType.DefaultValueSeparator ||
					iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
					{
					break;
					}
				else if (TryToSkipTypeOrVarName(ref iterator, end))
					{
					foundWord = true;
					}
				else if (TryToSkipComment(ref iterator) ||
						  TryToSkipString(ref iterator) ||
						  TryToSkipBlock(ref iterator, true))
					{
					foundWord = true;
					foundBlock = true;
					}
				else
					{
					iterator.Next();
					}

				// Process the word we found
				if (foundWord)
					{
					wordEnd = iterator;

					if (words >= 3)
						{
						if (foundBlock && wordEnd.TokenIndex - wordStart.TokenIndex >= 2)
							{
							wordStart.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;

							TokenIterator lookbehind = wordEnd;
							lookbehind.Previous();
							lookbehind.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
							}
						else
							{
							wordStart.SetPrototypeParsingTypeBetween(wordEnd, PrototypeParsingType.TypeModifier);
							}
						}
					else if (words == 2)
						{
						MarkType(wordStart, wordEnd);

						// Go back and change any trailing * or & to parameter modifiers because even if they're textually attached to the type
						// (int* x) they're actually part of the parameter in C++ (int *x).

						TokenIterator lookbehind = wordEnd;
						lookbehind.Previous();

						while (lookbehind >= wordStart &&
								 (lookbehind.Character == '*' || lookbehind.Character == '&' || lookbehind.Character == '^') )
							{
							lookbehind.PrototypeParsingType = PrototypeParsingType.ParamModifier;
							lookbehind.Previous();
							lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator, wordStart);
							}
						}
					else if (words == 1)
						{  MarkName(wordStart, wordEnd);  }

					words--;
					}
				}
			}


		/* Function: MarkPascalParameter
		 * Marks the tokens in the Pascal-style parameter specified by the bounds with <PrototypeParsingTypes>.
		 */
		protected void MarkPascalParameter (TokenIterator start, TokenIterator end)
			{
			// Pass 1: Count the number of "words" in the parameter prior to the default value and mark the default value.
			// We'll figure out how to interpret the words in the second pass.  Also mark the colon as the name/type separator
			// if it exists.

			int words = 0;
			int wordsBeforeColon = 0;

			TokenIterator iterator = start;

			while (iterator < end)
				{

				// Default values

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

					TokenIterator lookbehind = end;
					lookbehind.Previous();

					while (lookbehind >= iterator && lookbehind.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
						{
						endOfDefaultValue = lookbehind;
						lookbehind.Previous();
						}

					endOfDefaultValue.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, iterator);

					if (iterator < endOfDefaultValue)
						{  iterator.SetPrototypeParsingTypeBetween(endOfDefaultValue, PrototypeParsingType.DefaultValue);  }

					break;
					}


				// Colon.  Can only check for this after checking for :=

				else if (iterator.Character == ':')
					{
					wordsBeforeColon = words;
					iterator.PrototypeParsingType = PrototypeParsingType.NameTypeSeparator;
					iterator.Next();
					}


				// Param separator

				else if (iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
					{  break;  }


				// "Words" we're interested in

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


				// Whitespace and any unexpected random symbols

				else
					{  iterator.Next();  }
				}


			// Pass 2: Mark the "words" we counted from the first pass.  Before the colon the order of the words goes
			// [modifier] [modifier] [name].  After the colon it goes [modifier] [modifier] [type].  Not every parameter line will have
			// a colon as they could be sharing a type declaration.  An example of modifiers on each side is "const a: array of string".


			// Fix up the word counts.  wordsBeforeColon will be zero if we never found a colon.

			int wordsAfterColon;

			if (wordsBeforeColon == 0)
				{
				wordsBeforeColon = words;
				wordsAfterColon = 0;
				}
			else
				{  wordsAfterColon = words - wordsBeforeColon;  }


			// Before the colon: [modifier] [modifier] [name]

			iterator = start;
			TokenIterator wordStart, wordEnd;

			while (iterator < end)
				{
				wordStart = iterator;
				bool foundWord = false;
				bool foundBlock = false;

				if (iterator.PrototypeParsingType == PrototypeParsingType.DefaultValueSeparator ||
					iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator ||
					iterator.PrototypeParsingType == PrototypeParsingType.NameTypeSeparator)
					{
					break;
					}
				else if (TryToSkipTypeOrVarName(ref iterator, end))
					{
					foundWord = true;
					}
				else if (TryToSkipComment(ref iterator) ||
						  TryToSkipString(ref iterator) ||
						  TryToSkipBlock(ref iterator, true))
					{
					foundWord = true;
					foundBlock = true;
					}
				else
					{  iterator.Next();  }

				// Process the word we found
				if (foundWord)
					{
					wordEnd = iterator;

					if (wordsBeforeColon >= 2)
						{
						if (foundBlock && wordEnd.TokenIndex - wordStart.TokenIndex >= 2)
							{
							wordStart.PrototypeParsingType = PrototypeParsingType.OpeningParamModifier;

							TokenIterator lookbehind = wordEnd;
							lookbehind.Previous();
							lookbehind.PrototypeParsingType = PrototypeParsingType.ClosingParamModifier;
							}
						else
							{
							wordStart.SetPrototypeParsingTypeBetween(wordEnd, PrototypeParsingType.ParamModifier);
							}
						}
					else if (wordsBeforeColon == 1)
						{  MarkName(wordStart, wordEnd);  }

					wordsBeforeColon--;
					}
				}


			// After the colon: [modifier] [modifier] [type]

			if (wordsAfterColon > 0)
				{
				while (iterator.PrototypeParsingType == PrototypeParsingType.NameTypeSeparator)
					{  iterator.Next();  }

				while (iterator < end)
					{
					wordStart = iterator;
					bool foundWord = false;
					bool foundBlock = false;

					if (iterator.PrototypeParsingType == PrototypeParsingType.DefaultValueSeparator ||
						iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator)
						{
						break;
						}
					else if (TryToSkipTypeOrVarName(ref iterator, end))
						{
						foundWord = true;
						}
					else if (TryToSkipComment(ref iterator) ||
							  TryToSkipString(ref iterator) ||
							  TryToSkipBlock(ref iterator, true))
						{
						foundWord = true;
						foundBlock = true;
						}
					else
						{
						iterator.Next();
						}

					// Process the block we found
					if (foundWord)
						{
						wordEnd = iterator;

						if (wordsAfterColon >= 2)
							{
							if (foundBlock && wordEnd.TokenIndex - wordStart.TokenIndex >= 2)
								{
								wordStart.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;

								TokenIterator lookbehind = wordEnd;
								lookbehind.Previous();
								lookbehind.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
								}
							else
								{
								wordStart.SetPrototypeParsingTypeBetween(wordEnd, PrototypeParsingType.TypeModifier);
								}
							}
						else if (wordsAfterColon == 1)
							{  MarkType(wordStart, wordEnd);  }

						wordsAfterColon--;
						}
					}
				}
			}


		/* Function: MarkTypeAndModifiers
		 * Marks the passed stretch of tokens with <PrototypeParsingTypes> for variable types and preceding modifiers.
		 */
		protected void MarkTypeAndModifiers (TokenIterator start, TokenIterator end)
			{
			// Pass 1: Count the number of "words" in the segment.

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
				else
					{  iterator.Next();  }
				}


			// Pass 2: Mark the "words" we counted from the first pass in the order of [modifier] [modifier] [type].

			iterator = start;
			TokenIterator wordStart, wordEnd;

			while (iterator < end)
				{
				wordStart = iterator;

				if (iterator.PrototypeParsingType == PrototypeParsingType.DefaultValueSeparator ||
					iterator.PrototypeParsingType == PrototypeParsingType.ParamSeparator ||
					iterator.PrototypeParsingType == PrototypeParsingType.NameTypeSeparator)
					{
					break;
					}
				else if (TryToSkipTypeOrVarName(ref iterator, end) ||
						   TryToSkipComment(ref iterator) ||
						   TryToSkipString(ref iterator) ||
						   TryToSkipBlock(ref iterator, true))
					{
					wordEnd = iterator;

					if (words >= 2)
						{  wordStart.SetPrototypeParsingTypeBetween(wordEnd, PrototypeParsingType.TypeModifier);  }
					else if (words == 1)
						{  MarkType(wordStart, wordEnd);  }

					words--;
					}
				else
					{  iterator.Next();  }
				}
			}


		/* Function: MarkType
		 * Marks the passed stretch of tokens with <PrototypeParsingTypes> for variable types.
		 */
		protected void MarkType (TokenIterator start, TokenIterator end)
			{
			// Mark all pre-text symbols as type modifiers.  This includes :: on ::globals.
			while (start < end &&
					 start.FundamentalType != FundamentalType.Text &&
					 start.Character != '_')
				{
				start.PrototypeParsingType = PrototypeParsingType.TypeModifier;
				start.Next();
				}

			TokenIterator iterator = start;
			TokenIterator qualifierEnd = start;

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
				{  start.SetPrototypeParsingTypeBetween(qualifierEnd, PrototypeParsingType.TypeQualifier);  }
			if (iterator > qualifierEnd)
				{  qualifierEnd.SetPrototypeParsingTypeBetween(iterator, PrototypeParsingType.Type);  }
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
					prevIterator.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;
					prevIterator.Next();

					iterator.Previous();
					iterator.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;

					MarkTypeSuffixParamList(prevIterator, iterator);

					iterator.Next();
					}
				else
					{
					iterator.PrototypeParsingType = PrototypeParsingType.TypeModifier;
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
						{  startWord.SetPrototypeParsingTypeBetween(endWord, PrototypeParsingType.TypeModifier);  }
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
			// Mark all pre-text symbols as param modifiers.  This includes :: on ::globals.
			while (start < end &&
					 start.FundamentalType != FundamentalType.Text &&
					 start.Character != '_')
				{
				start.PrototypeParsingType = PrototypeParsingType.ParamModifier;
				start.Next();
				}

			// Mark the name.  Qualifiers get included as part of the name.
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

			// Mark all following symbols as modifiers, detecting any blocks found
			if (start < end)
				{
				TokenIterator blockEnd = start;

				if (TryToSkipBlock(ref blockEnd, true) && blockEnd <= end)
					{
					start.PrototypeParsingType = PrototypeParsingType.OpeningParamModifier;

					blockEnd.Previous();
					blockEnd.PrototypeParsingType = PrototypeParsingType.ClosingParamModifier;

					start = blockEnd;
					start.Next();
					}
				else
					{
					start.PrototypeParsingType = PrototypeParsingType.ParamModifier;
					start.Next();
					}
				}
			}



		// Group: General Parsing Support Functions
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


		/* Function: TryToGetBlockComment
		 *
		 * If the iterator is on the opening symbol of a block comment, this function moves the iterator past the entire
		 * comment and returns true.  It will also return it as an <InlineDocumentationComment> and mark the symbols
		 * as <CommentParsingType.CommentSymbol>.  If the line does not start with an opening comment symbol it will
		 * return false and leave the iterator where it is.
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
		protected bool TryToGetBlockComment (ref TokenIterator iterator,
																 string openingSymbol, string closingSymbol, bool openingMustBeAlone,
																 out InlineDocumentationComment comment)
			{
			if (iterator.MatchesAcrossTokens(openingSymbol) == false)
				{
				comment = null;
				return false;
				}

			TokenIterator openingSymbolIterator = iterator;
			TokenIterator lookahead = iterator;

			// Advance past the opening symbol because it's possible for it to be the same as the closing one, such as with
			// Python's ''' and """ strings.
			lookahead.NextByCharacters(openingSymbol.Length);

			if (openingMustBeAlone && lookahead.FundamentalType == FundamentalType.Symbol)
				{
				comment = null;
				return false;
				}

			comment = new InlineDocumentationComment();
			comment.Start = openingSymbolIterator;

			var tokenizer = iterator.Tokenizer;
			TokenIterator closingSymbolIterator, endOfCommentIterator;

			if (tokenizer.FindTokensBetween(closingSymbol, false, lookahead, tokenizer.EndOfTokens, out closingSymbolIterator) == true)
				{
				endOfCommentIterator = closingSymbolIterator;
				endOfCommentIterator.NextByCharacters(closingSymbol.Length);

				// Make sure nothing appears after the closing symbol on the line
				lookahead = endOfCommentIterator;
				lookahead.NextPastWhitespace();

				if (lookahead.FundamentalType != FundamentalType.LineBreak &&
					lookahead.FundamentalType != FundamentalType.Null)
					{  comment = null;  }
				else
					{  comment.End = endOfCommentIterator;  }
				}
			else // no closing symbol
				{
				// If we're here that means there was an unclosed comment at the end of the file.  Skip it but don't treat
				// it as a documentation candidate.
				comment = null;
				endOfCommentIterator = iterator.Tokenizer.EndOfTokens;
				}

			if (comment != null)
				{
				// Mark the symbols before returning
				openingSymbolIterator.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, openingSymbol.Length);
				closingSymbolIterator.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, closingSymbol.Length);
				}

			// If we made it this far that means we found a comment and can move the iterator and return true.  Whether that
			// comment was suitable for documentation will be determined by the comment variable, but we are moving the
			// iterator and returning true either way.
			iterator = endOfCommentIterator;
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


		/* Function: TryToGetLineComment
		 *
		 * If the iterator is on a line comment symbol, this function moves the iterator past the entire comment and returns
		 * true.  If the comment is a candidate for documentation it will also return it as an <InlineDocumentationComment>
		 * and mark the symbols as <CommentParsingType.CommentSymbol>.  If the iterator is not on a line comment symbol
		 * it will return false and leave the iterator where it is.
		 *
		 * This function takes a separate comment symbol for the first line and all remaining lines, allowing you to detect
		 * Javadoc line comments that start with ## and the remaining lines use #.  Both symbols can be the same if this isn't
		 * required.
		 *
		 * If openingMustBeAlone is set, no symbol can appear immediately after the first line symbol.  If it does the function
		 * will return false and not move past the comment.  This allows you to specifically detect something like ## without
		 * also matching #######.
		 */
		protected bool TryToGetLineComment (ref TokenIterator iterator,
																string firstSymbol, string remainderSymbol, bool openingMustBeAlone,
																out InlineDocumentationComment comment)
			{
			if (iterator.MatchesAcrossTokens(firstSymbol) == false)
				{
				comment = null;
				return false;
				}

			if (openingMustBeAlone)
				{
				TokenIterator lookahead = iterator;
				lookahead.NextByCharacters(firstSymbol.Length);

				if (lookahead.FundamentalType == FundamentalType.Symbol)
					{
					comment = null;
					return false;
					}
				}

			comment = new InlineDocumentationComment();
			comment.Start = iterator;

			// Since we're definitely returning a comment we can mark the comment symbols as we go rather than waiting until
			// the end.
			iterator.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, firstSymbol.Length);
			iterator.NextByCharacters(firstSymbol.Length);

			for (;;)
				{
				// Skip the rest of the line
				while (iterator.IsInBounds &&
						  iterator.FundamentalType != FundamentalType.LineBreak)
					{  iterator.Next();  }

				// Skip the line break too if it's there
				if (iterator.FundamentalType == FundamentalType.LineBreak)
					{  iterator.Next();  }

				comment.End = iterator;

				// Look ahead to see if the next line continues the comment or we stop here
				TokenIterator lookahead = iterator;
				lookahead.NextPastWhitespace();

				if (lookahead.MatchesAcrossTokens(remainderSymbol))
					{
					lookahead.SetCommentParsingTypeByCharacters(CommentParsingType.CommentSymbol, remainderSymbol.Length);
					lookahead.NextByCharacters(remainderSymbol.Length);

					iterator = lookahead;
					}
				else
					{  break;  }
				}

			return true;
			}


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
		 * about the specific type of comment it was, you need to call <TryToSkipLineComment()> and <TryToSkipBlockComment()>
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
			if (!language.HasLineCommentSymbols)
				{
				commentSymbol = null;
				return false;
				}

			int commentSymbolIndex = iterator.MatchesAnyAcrossTokens(language.LineCommentSymbols);

			if (commentSymbolIndex == -1)
				{
				commentSymbol = null;
				return false;
				}

			commentSymbol = language.LineCommentSymbols[commentSymbolIndex];

			TokenIterator startOfComment = iterator;
			iterator.NextByCharacters(commentSymbol.Length);

			while (iterator.IsInBounds && iterator.FundamentalType != FundamentalType.LineBreak)
				{  iterator.Next();  }

			if (mode == ParseMode.SyntaxHighlight)
				{  startOfComment.SetSyntaxHighlightingTypeBetween(iterator, SyntaxHighlightingType.Comment);  }

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


		/* Function: IsOpeningBlockCommentSymbol
		 * Returns whether the iterator is on an opening block comment symbol, and if so, also returns the opening and closing symbols.
		 */
		protected bool IsOpeningBlockCommentSymbol (TokenIterator iterator, out string openingSymbol, out string closingSymbol)
			{
			if (language.HasBlockCommentSymbols)
				{
				foreach (var blockCommentSymbols in language.BlockCommentSymbols)
					{
					if (iterator.MatchesAcrossTokens(blockCommentSymbols.OpeningSymbol))
						{
						openingSymbol = blockCommentSymbols.OpeningSymbol;
						closingSymbol = blockCommentSymbols.ClosingSymbol;
						return true;
						}
					}
				}

			openingSymbol = null;
			closingSymbol = null;
			return false;
			}


		/* Function: TryToSkipBlockComment
		 *
		 * If the iterator is on an opening block comment symbol, moves it past the entire comment, provides the comment symbols that
		 * were used, and returns true.  If the language allows comments to nest it will only return the outermost symbols.
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
			if (!IsOpeningBlockCommentSymbol(iterator, out openingSymbol, out closingSymbol))
				{
				openingSymbol = null;
				closingSymbol = null;
				return false;
				}

			TokenIterator lookahead = iterator;
			lookahead.NextByCharacters(openingSymbol.Length);

			// Since most languages don't allow block comments to nest we don't want to create a stack on every call.  Instead we create
			// a stack of closing symbols that aren't the current one we're looking for and create the object on demand.
			string currentClosingSymbol = closingSymbol;
			SafeStack<string> closingSymbolStack = null;

			string nestedOpeningSymbol, nestedClosingSymbol;

			while (lookahead.IsInBounds)
				{
				if (lookahead.MatchesAcrossTokens(currentClosingSymbol))
					{
					lookahead.NextByCharacters(currentClosingSymbol.Length);

					if (closingSymbolStack == null || closingSymbolStack.Count == 0)
						{  break;  }
					else
						{  currentClosingSymbol = closingSymbolStack.Pop();  }
					}
				else if (language.BlockCommentsNest &&
						   IsOpeningBlockCommentSymbol(lookahead, out nestedOpeningSymbol, out nestedClosingSymbol))
					{
					lookahead.NextByCharacters(nestedOpeningSymbol.Length);

					if (closingSymbolStack == null)
						{  closingSymbolStack = new SafeStack<string>();  }

					closingSymbolStack.Push(currentClosingSymbol);
					currentClosingSymbol = nestedClosingSymbol;
					}
				else
					{  lookahead.Next();  }
				}

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.Comment);  }

			// Return true even if the iterator reached the end of the content before finding a closing symbol.
			iterator = lookahead;
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
		virtual protected bool TryToSkipString (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
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
				{  startOfString.SetSyntaxHighlightingTypeBetween(iterator, SyntaxHighlightingType.String);  }

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
		virtual protected bool TryToSkipNumber (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if ( ((iterator.Character >= '0' && iterator.Character <= '9') ||
				   iterator.Character == '-' || iterator.Character == '+' || iterator.Character == '.') == false)
				{  return false;  }

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			// Check that we're not following an underscore or letter.  This prevents "_12" from being seen as a number.

			// We do this check before the tests for +, -, or . because if any of them are following a letter they aren't part of
			// the number.  For example, in "x+2", "x-2", and "x.2" the symbols are not part of the number the way they are
			// in "+2", "-2", or ".2".

			if (lookbehind.FundamentalType == FundamentalType.Text ||
				lookbehind.Character == '_')
				{  return false;  }

			TokenIterator lookahead = iterator;
			bool passedPeriod = false;
			bool lastCharWasE = false;
			bool isHex = false;

			if (lookahead.Character == '-' || lookahead.Character == '+')
				{
				// Distinguish between -1 and x-1

				lookbehind = iterator;
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
					iterator = lookahead;

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
				{  startOfNumber.SetSyntaxHighlightingTypeBetween(iterator, SyntaxHighlightingType.Number);  }

			return true;
			}


		/* Function: TryToSkipKeyword
		 *
		 * If the iterator is on a keyword, moves the iterator past it and returns true.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		virtual protected bool TryToSkipKeyword (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			// All the default keywords are a single text token, no underscores or other symbols

			if (iterator.FundamentalType != FundamentalType.Text)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (lookahead.Character == '_' ||
				lookahead.FundamentalType == FundamentalType.Text)
				{  return false;  }

			TokenIterator lookbehind = iterator;
			lookbehind.Previous();

			if (lookbehind.Character == '_' ||
				lookbehind.FundamentalType == FundamentalType.Text)
				{  return false;  }

			if (!defaultKeywords.Contains(iterator.String))
				{  return false;  }

			if (mode == ParseMode.SyntaxHighlight)
				{  iterator.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }

			iterator.Next();
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
		 * genuinely don't need a limit, set it to <Tokenizer.EndOfTokens>.
		 */
		protected bool TryToSkipTypeOrVarName (ref TokenIterator iterator, TokenIterator limit)
			{
			if (iterator < limit &&
				 (iterator.FundamentalType == FundamentalType.Text ||
				  iterator.Character == '_' || iterator.Character == '*' || iterator.Character == '&' || iterator.Character == '^' ||
				  iterator.Character == '$' || iterator.Character == '@' || iterator.Character == '%') )
				{
				iterator.Next();

				while (iterator < limit)
					{
					// Add dot to our previous list.  Also add ? for C# nullable types.
					if (iterator.FundamentalType == FundamentalType.Text ||
						 iterator.Character == '_' || iterator.Character == '*' || iterator.Character == '&' || iterator.Character == '^' ||
						 iterator.Character == '$' || iterator.Character == '@' || iterator.Character == '%' ||
						 iterator.Character == '.' || iterator.Character == '?')
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


		/* Function: ResetTokensBetween
		 * If the mode is <ParseMode.SyntaxHighlight>, <ParseMode.ParsePrototype>, or <ParseMode.ParseClassPrototype>, this will
		 * reset the relevant tokens between the iterators back to null.  For other modes it has no effect.
		 */
		protected void ResetTokensBetween (TokenIterator start, TokenIterator end, ParseMode mode)
			{
			if (mode == ParseMode.SyntaxHighlight)
				{  start.SetSyntaxHighlightingTypeBetween(end, SyntaxHighlightingType.Null);  }
			else if (mode == ParseMode.ParsePrototype)
				{  start.SetPrototypeParsingTypeBetween(end, PrototypeParsingType.Null);  }
			else if (mode == ParseMode.ParseClassPrototype)
				{  start.SetClassPrototypeParsingTypeBetween(end, ClassPrototypeParsingType.Null);  }
			}



		// Group: Validation Support Functions
		// __________________________________________________________________________


		#if DEBUG

		/* Function: ValidateElements
		 * Validates a list of <Elements> to make sure all the properties are set correctly, throwing an exception if not.  This
		 * does nothing in non-debug builds.
		 */
		protected void ValidateElements (List<Element> elements, ValidateElementsMode mode)
			{
			int lastLineNumber = 0;
			int lastCharNumber = 0;

			for (int i = 0; i < elements.Count; i++)
				{
				Element element = elements[i];


				// Make sure they have either InCode or InComments set.  We don't do this form ValidateElementsMode.Final because
				// automatic grouping will be applied which doesn't appear in either.  However, prior to that point every Element needs
				// one or the other set.

				if (mode != ValidateElementsMode.Final)
					{
					if (element.InCode == false && element.InComments == false)
						{  throw new Exception("Element " + ElementName(element) + " doesn't have the InComments or InCode flag set.");  }
					}


				// Make sure they're in order

				if (element.LineNumber < lastLineNumber ||
					 (element.LineNumber == lastLineNumber && element.CharNumber < lastCharNumber))
					{
					throw new Exception("Element " + ElementName(element) + " doesn't appear in order.  " +
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
						throw new Exception("IsRootElement was set on " + ElementName(element) + " which is at position " + i + " in the list.  " +
																 "IsRootElement can only be set on the first member of a list.");
						}


					// Make sure parents have their ending values set.

					if (elementAsParent.EndingLineNumber == -1 || elementAsParent.EndingCharNumber == -1)
						{  throw new Exception(ElementName(element) + " did not have its ending position properties set.");  }

					if (elementAsParent.EndingLineNumber < elementAsParent.LineNumber ||
						 (elementAsParent.EndingLineNumber == elementAsParent.LineNumber &&
						  elementAsParent.EndingCharNumber < elementAsParent.CharNumber))
						{  throw new Exception(ElementName(element) + "'s ending position was before its starting position.");  }


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
									throw new Exception(ElementName(element) + " starts in " + ElementName(previousParent) + "'s range but extends past it.");
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
					if (element.Topic.CommentTypeID == 0)
						{  throw new Exception("All topics returned by GetCodeElements() must have comment type IDs.");  }
					if (element.Topic.IsList)
						{  throw new Exception("GetCodeElements() cannot return list topics.");  }
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

						if (element.Topic.IsList)
							{
							int elementEmbeddedTopics = CountConsecutiveEmbeddedTopics(elements, i + 1);

							if (bodyEmbeddedTopics != elementEmbeddedTopics)
								{
								throw new Exception("Element " + ElementName(element) + " had " + bodyEmbeddedTopics + " embedded topics in its body but " +
															   elementEmbeddedTopics + " embedded topics following it in the elements.");
								}
							}
						else // element.Topic.IsEnum
							{
							int elementChildren = CountChildElements((ParentElement)element, elements, i + 1);

							if (bodyEmbeddedTopics != elementChildren)
								{
								throw new Exception("Element " + ElementName(element) + " had " + bodyEmbeddedTopics + " embedded topics in its body but " +
															   elementChildren + " child elements.");
								}
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
							{  throw new Exception("Embedded element " + ElementName(element) + " did not follow a list or enum topic.");  }
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
					if (topic.CommentTypeID == 0)
						{  missingProperties += " CommentTypeID";  }

					if (missingProperties != "")
						{  throw new Exception("Generated Topic is missing properties:" + missingProperties);  }
					}
				}
			}


		/* Function: ElementName
		 * Generates a name for the passed <Element> to use in debugging messages.
		 */
		protected string ElementName (Element element)
			{
			string elementName = "(line " + element.LineNumber + ", char " + element.CharNumber + ")";

			if (element.Topic != null && element.Topic.Title != null)
				{  elementName = element.Topic.Title + " " + elementName;  }

			return elementName;
			}

		#endif



		// Group: Properties
		// __________________________________________________________________________


		/* Property: EngineInstance
		 * The <Engine.Instance> associated with this parser.
		 */
		public Engine.Instance EngineInstance
			{
			get
				{  return engineInstance;  }
			}

		/* Propetry: Language
		 * The <Languages.Language> associated with this parser.
		 */
		public Languages.Language Language
			{
			get
				{  return language;  }
			}

		/* Property: Manager
		 * The <Languages.Manager> associated with this parser.
		 */
		public Languages.Manager Manager
			{
			get
				{  return engineInstance.Languages;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		protected Engine.Instance engineInstance;

		protected Language language;



		// Group: Static Variables
		// __________________________________________________________________________

		/* var: defaultKeywords
		 * A set of the default keywords for basic language support across all languages.
		 */
		static protected StringSet defaultKeywords = new StringSet (KeySettings.Literal, new string[] {

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


		/* var: typeKeywords
		 * A set of the default keywords used as types across all languages.
		 */
		static protected StringSet typeKeywords = new StringSet (KeySettings.Literal, new string[] {
			"int", "int8", "int16", "int32", "int64", "uint", "uint8", "uint16", "uint32", "uint64",
			"signed", "unsigned", "integer", "long", "ulong", "short", "ushort", "real", "float", "double", "decimal",
			"float32", "float64", "float80", "void", "char", "string", "wchar", "wchar_t", "byte", "ubyte", "sbyte",
			"bool", "boolean"
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



		// Group: Regular Expressions
		// __________________________________________________________________________


		// Regex: IsPreservedPrototypeCommentRegex
		// Will match if the string is the content of a comment that should not be removed from prototypes in the output,
		// such as Splint comments.  This will only match against the comment content, such as "@out@" in "/*@out@*/",
		// so the surrounding comment symbols should not be included.
		//
		[GeneratedRegex("""^@.*@$|^[ \t]*(?:in|out|in[-/ ]?out|ref)[ \t]*$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex IsPreservedPrototypeCommentRegex();


		/* Regex: FindExtraOperatorWhitespaceRegex
		 * Will match parts of the string representing extra spaces between the "operator" keyword and any extra symbols.
		 * This allows the whitespace to be removed so "operator +" can match "operator+".
		 */
		[GeneratedRegex("""(?<=operator)[ \t]+(?=[^a-z0-9_])""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex FindExtraOperatorWhitespaceRegex();

		}
	}
