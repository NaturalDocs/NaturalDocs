/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.CSharp
 * ____________________________________________________________________________
 *
 * Full language support parser for C#.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
	{
	public class CSharp : Parser
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: AttributeTarget
		 * GlobalOnly - Only attributes for assemblies and modules.
		 * LocalOnly - All attributes other than global.
		 * Any - All attributes.
		 */
		public enum AttributeTarget : byte
			{  GlobalOnly, LocalOnly, Any  }



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: CSharp
		 */
		public CSharp (Engine.Instance engineInstance, Language language) : base (engineInstance, language)
			{
			}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */
		public override ParsedPrototype ParsePrototype (string stringPrototype, int commentTypeID)
			{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator startOfPrototype = tokenizedPrototype.FirstToken;
			bool parsed = false;

			if (commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("function", language.ID) ||
				commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("delegate", language.ID) ||
				commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("operator", language.ID))
			    {
				parsed = TryToSkipFunction(ref startOfPrototype, ParseMode.ParsePrototype);
			    }

			if (!parsed &&
				(commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("variable", language.ID) ||
				 commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("constant", language.ID) ||
				 commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("event", language.ID)) )
			    {
				parsed = TryToSkipVariable(ref startOfPrototype, ParseMode.ParsePrototype);
			    }

			if (!parsed &&
				(commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("property", language.ID) ||
				 commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("operator", language.ID) ||
				 commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("function", language.ID) ||
				 commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("event", language.ID)) )
			    {
				parsed = TryToSkipProperty(ref startOfPrototype, ParseMode.ParsePrototype);
			    }

			if (!parsed &&
				(commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("function", language.ID) ||
				 commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("constructor", language.ID) ||
				 commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("destructor", language.ID)) )
			    {
				parsed = TryToSkipConstructor(ref startOfPrototype, ParseMode.ParsePrototype);
			    }

			if (!parsed &&
				(commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("enum", language.ID) ||
				 commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("type", language.ID)) )
			    {
				parsed = TryToSkipEnum(ref startOfPrototype, ParseMode.ParsePrototype);
			    }

			if (!parsed &&
				(commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("operator", language.ID) ||
				 commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("function", language.ID)) )
			    {
				parsed = TryToSkipConversionOperator(ref startOfPrototype, ParseMode.ParsePrototype);
			    }

			if (parsed)
				{
				return new ParsedPrototype(tokenizedPrototype, this.Language.ID, commentTypeID,
														 parameterStyle: ParameterStyle.C, supportsImpliedTypes: true);
				}
			else
			    {
				return base.ParsePrototype(stringPrototype, commentTypeID);
				}
			}


		/* Function: ParseClassPrototype
		 * Converts a raw text prototype into a <ParsedClassPrototype>.  Will return null if it is not an appropriate prototype.
		 */
		override public ParsedClassPrototype ParseClassPrototype (string stringPrototype, int commentTypeID)
			{
			if (EngineInstance.CommentTypes.InClassHierarchy(commentTypeID) == false)
				{  return null;  }

			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator startOfPrototype = tokenizedPrototype.FirstToken;
			ParsedClassPrototype parsedPrototype = new ParsedClassPrototype(tokenizedPrototype);
			bool parsed = false;

			if (commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("class", language.ID) ||
				commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("struct", language.ID) ||
				commentTypeID == EngineInstance.CommentTypes.IDFromKeyword("interface", language.ID))
			    {
				parsed = TryToSkipClass(ref startOfPrototype, ParseMode.ParseClassPrototype);
			    }

			if (parsed)
				{  return parsedPrototype;  }
			else
			    {  return base.ParseClassPrototype(stringPrototype, commentTypeID);  }
			}


		/* Function: GetCodeElements
		 *
		 * Topic Properties:
		 *
		 *		TopicID - Not set.
		 *		Title - Set.
		 *		Body - Not applicable.
		 *		Summary - Not applicable.
		 *		Prototype - Set.
		 *		Symbol - Not set.
		 *		SymbolDefinitionNumber - Not set.
		 *		ClassString - Not set.
		 *		ClassID - Not set.
		 *		IsEmbedded - Not applicable.
		 *		CommentTypeID - Set.
		 *		AccessLevel - Set if directly defined on the element.  Not set to defaults or adjusted for inheritance.
		 *		Tags - Not applicable.
		 *		FileID - Not set.
		 *		CommentLineNumber - Not applicable.
		 *		CodeLineNumber - Set.
		 *		LanguageID - Set only on root element.
		 *		PrototypeContext - Not set.
		 *		BodyContext - Not set.
		 *
		 */
		override public List<Element> GetCodeElements (Tokenizer source)
			{
			List<Element> elements = new List<Element>();

			// Having a root element is important for setting the default child access level and to provide a target for top-level
			// using statements.
			ParentElement rootElement = new ParentElement(0, 0, Element.Flags.InCode);
			rootElement.IsRootElement = true;
			rootElement.MaximumEffectiveChildAccessLevel = AccessLevel.Public;
			rootElement.DefaultDeclaredChildAccessLevel = AccessLevel.Internal;
			rootElement.DefaultChildLanguageID = language.ID;
			rootElement.ChildContextString = new ContextString();
			rootElement.EndingLineNumber = int.MaxValue;
			rootElement.EndingCharNumber = int.MaxValue;

			elements.Add(rootElement);

			TokenIterator iterator = source.FirstToken;
			GetCodeElements(ref iterator, elements, new SymbolString());

			return elements;
			}


		/* Function: GetCodeElements
		 *
		 * Adds code elements to the list until it reaches the end of the file or optionally passes a specific character.  This will
		 * recursively go into nested classes and namespaces.  The stop character must appear on its own and not inside a
		 * block, string, or comment, so to use '}' you must start past the opening brace.  The iterator will be left past the stop
		 * character or at the end of the file.
		 *
		 * If you want to skip a block without searching for elements within it, use <GenericSkipUntilAfter()> instead.
		 */
		protected void GetCodeElements (ref TokenIterator iterator, List<Element> elements, SymbolString scope,
																		 char untilAfterChar = '\0')
			{
			while (iterator.IsInBounds)
				{
				if (iterator.Character == untilAfterChar)
					{
					iterator.Next();
					break;
					}

				else if (TryToSkipWhitespace(ref iterator) ||
						  TryToSkipPreprocessingDirective(ref iterator) ||

						  TryToSkipUsingStatement(ref iterator, ParseMode.CreateElements, elements, scope) ||
						  TryToSkipNamespace(ref iterator, ParseMode.CreateElements, elements, scope) ||
						  TryToSkipClass(ref iterator, ParseMode.CreateElements, elements, scope) ||
						  TryToSkipFunction(ref iterator, ParseMode.CreateElements, elements, scope) ||
						  TryToSkipVariable(ref iterator, ParseMode.CreateElements, elements, scope) ||
						  TryToSkipProperty(ref iterator, ParseMode.CreateElements, elements, scope) ||
						  TryToSkipConstructor(ref iterator, ParseMode.CreateElements, elements, scope) ||
						  TryToSkipEnum(ref iterator, ParseMode.CreateElements, elements, scope) ||
						  TryToSkipConversionOperator(ref iterator, ParseMode.CreateElements, elements, scope) ||

						  // We skip attributes after trying to get language elements because they may be part of one.
						  // We have to skip attributes in this loop to begin with because they don't end like regular statements, so not having
						  // this here would mean SkipRestOfStatement() skips it and the next statement.
						  // We only skip a single attribute because a global one may be followed by a local one that's part of a language element.
						  TryToSkipAttribute(ref iterator))
					{  }

				else
					{  SkipRestOfStatement(ref iterator);  }
				}
			}


		/* Function: SyntaxHighlight
		 */
		override public void SyntaxHighlight (Tokenizer source)
			{
			TokenIterator iterator = source.FirstToken;

			TokenIterator lastCodeToken = iterator.Tokenizer.EndOfTokens;  // Default to out of bounds

			while (iterator.IsInBounds)
				{
				if (TryToSkipPreprocessingDirective(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipComment(ref iterator, ParseMode.SyntaxHighlight))
					{
					}
				else if (TryToSkipString(ref iterator, ParseMode.SyntaxHighlight) ||
						  TryToSkipNumber(ref iterator, ParseMode.SyntaxHighlight))
					{
					lastCodeToken = iterator;
					lastCodeToken.Previous();
					}

				// Determine if it's an attribute or an array bracket.  TryToSkipAttribute() won't check the context.
				else if (iterator.Character == '[')
				   {
					bool isAttribute;

					if (lastCodeToken.FundamentalType == FundamentalType.Null)
						{  isAttribute = true;  }
					else if (lastCodeToken.FundamentalType == FundamentalType.Text || lastCodeToken.Character == '_')
						{  isAttribute = false;  }
					else if (lastCodeToken.Character == ']')
						{
						// If it follows a ], copy what that symbol was already marked as since both arrays and attributes can
						// be chained.  int[][] and [Test][Category("x")].
						isAttribute = (lastCodeToken.SyntaxHighlightingType == SyntaxHighlightingType.Metadata);
						}
					else if (lastCodeToken.Character == '(' ||
							  lastCodeToken.Character == '[' ||
							  lastCodeToken.Character == '{' ||
							  lastCodeToken.Character == ',' ||
							  lastCodeToken.Character == ';' ||
							  lastCodeToken.Character == '}')
						{  isAttribute = true;  }
					else
						{  isAttribute = false;  }

					if (isAttribute && TryToSkipAttribute(ref iterator, mode: ParseMode.SyntaxHighlight))
						{
						}
					else
						{
						lastCodeToken = iterator;
						iterator.Next();
						}
				   }

				// Skip @ identifiers since things like @if shouldn't be highlighted as keywords.  We already covered @ strings above.
				else if (iterator.Character == '@')
					{
					do
						{  iterator.Next();  }
					while (iterator.FundamentalType == FundamentalType.Text ||
								iterator.Character == '_');

					lastCodeToken = iterator;
					lastCodeToken.Previous();
					}

				// Text.  Check for keywords.
				else if (iterator.FundamentalType == FundamentalType.Text || iterator.Character == '_')
					{
					TokenIterator endOfIdentifier = iterator;

					do
						{  endOfIdentifier.Next();  }
					while (endOfIdentifier.FundamentalType == FundamentalType.Text ||
							endOfIdentifier.Character == '_');

					string identifier = source.TextBetween(iterator, endOfIdentifier);

					if (Keywords.Contains(identifier))
						{  iterator.SetSyntaxHighlightingTypeByCharacters(SyntaxHighlightingType.Keyword, identifier.Length);  }

					iterator = endOfIdentifier;

					lastCodeToken = iterator;
					lastCodeToken.Previous();
					}

				else
					{
					if (iterator.FundamentalType != FundamentalType.Whitespace &&
						iterator.FundamentalType != FundamentalType.LineBreak)
						{  lastCodeToken = iterator;  }

					iterator.Next();
					}
				}
			}


		/* Function: IsBuiltInType
		 * Returns whether the type string is a built-in type such as "int" as opposed to a user-defined type.
		 */
		override public bool IsBuiltInType (string type)
			{
			return BuiltInTypes.Contains(type);
			}



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToSkipUsingStatement
		 *
		 * If the iterator is on a using statement, moves it past it and returns true.  If the mode is set to <ParseMode.CreateElements> it will
		 * add it to the most recent <ParentElement>.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.CreateElements>
		 *			- The elements and scope parameters must be set.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipUsingStatement (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly, List<Element> elements = null,
																SymbolString scope = default(SymbolString))
			{
			#if DEBUG
			if (mode == ParseMode.CreateElements && elements == null)
				{  throw new Exception("Elements and scope must be set when using ParseMode.CreateElements().");  }
			#endif

			if (iterator.MatchesToken("using") == false)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			TryToSkipWhitespace(ref lookahead);

			string firstIdentifier;
			if (TryToSkipIdentifier(ref lookahead, out firstIdentifier) == false)
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);

			UsingString usingString = default(UsingString);

			if (lookahead.Character == '=')
				{
				lookahead.Next();

				TryToSkipWhitespace(ref lookahead);

				string secondIdentifier;
				if (TryToSkipIdentifier(ref lookahead, out secondIdentifier) == false)
					{  return false;  }

				if (mode == ParseMode.CreateElements)
					{
					usingString = UsingString.FromParameters(UsingString.UsingType.ReplacePrefix,
																			 SymbolString.FromPlainText_NoParameters(secondIdentifier),
																			 SymbolString.FromPlainText_NoParameters(firstIdentifier));
					}
				}
			else // not on '='
				{
				if (mode == ParseMode.CreateElements)
					{
					usingString = UsingString.FromParameters(UsingString.UsingType.AddPrefix,
																			 SymbolString.FromPlainText_NoParameters(firstIdentifier));
					}
				}


			if (mode == ParseMode.CreateElements)
				{
				// Find the parent.  We can't use FindElementParent() because ending line/char numbers haven't been set yet.  Instead we'll
				// find it manually, treating -1 as meaning it contains the current position.

				int parentIndex = -1;

				for (int i = elements.Count - 1; i >= 0; i--)
					{
					if (elements[i] is ParentElement)
						{
						ParentElement parentElement = (ParentElement)elements[i];

						if (parentElement.FilePosition <= iterator.FilePosition &&
							(parentElement.EndingLineNumber == -1 ||
							 parentElement.EndingFilePosition > iterator.FilePosition))
							{
							parentIndex = i;
							break;
							}
						}
					}

				if (parentIndex != -1)
					{
					ParentElement parentElement = (ParentElement)elements[parentIndex];

					ContextString tempContext = parentElement.ChildContextString;
					tempContext.AddUsingStatement(usingString);
					parentElement.ChildContextString = tempContext;
					}
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipNamespace
		 *
		 * If the iterator is on a namespace element, moves it past it and returns true.  If the mode is set to <ParseMode.CreateElements>
		 * it will add it to the list of <Elements>.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.CreateElements>
		 *			- The elements and scope parameters must be set.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipNamespace (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly, List<Element> elements = null,
														  SymbolString scope = default(SymbolString))
			{
			#if DEBUG
			if (mode == ParseMode.CreateElements && elements == null)
				{  throw new Exception("Elements and scope must be set when using ParseMode.CreateElements.");  }
			#endif

			// Namespaces may not have attributes.  There may be global attributes above them in the file, but they do not apply to the
			// namespace itself.  Namespaces embedded in other namespaces do not have attributes at all.

			if (iterator.MatchesToken("namespace") == false)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			string name;
			if (TryToSkipIdentifier(ref lookahead, out name) == false)
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);

			bool hasBody;

			if (lookahead.Character == '{')
				{  hasBody = true;  }
			else if (lookahead.Character == ';')
				{  hasBody = false;  }
			else
				{  return false;  }

			lookahead.Next();


			// Create the element

			if (mode == ParseMode.CreateElements)
				{
				SymbolString symbol = scope + SymbolString.FromPlainText_NoParameters(name);

				ContextString childContext = new ContextString();
				childContext.Scope = symbol;

				ParentElement namespaceElement = new ParentElement(iterator, Element.Flags.InCode);
				namespaceElement.DefaultChildLanguageID = language.ID;
				namespaceElement.ChildContextString = childContext;
				namespaceElement.MaximumEffectiveChildAccessLevel = AccessLevel.Public;
				namespaceElement.DefaultDeclaredChildAccessLevel = AccessLevel.Internal;

				// We don't create topics for namespaces.

				elements.Add(namespaceElement);

				iterator = lookahead;
				GetCodeElements(ref iterator, elements, symbol, (hasBody ? '}' : '\0'));

				namespaceElement.EndingLineNumber = iterator.LineNumber;
				namespaceElement.EndingCharNumber = iterator.CharNumber;

				return true;
				}

			else // not ParseMode.CreateElements
				{
				iterator = lookahead;

				if (hasBody)
					{  GenericSkipUntilAfter(ref iterator, '}');  }

				return true;
				}
			}


		/* Function: TryToSkipClass
		 *
		 * If the iterator is on a class, struct, or interface element, moves it past it and returns true.  If the mode is set to
		 * <ParseMode.CreateElements> it will add it to the list of <Elements>.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParseClassPrototype>
		 *		- <ParseMode.CreateElements>
		 *			- The elements and scope parameters must be set.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipClass (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly, List<Element> elements = null,
												  SymbolString scope = default(SymbolString))
			{
			// Classes, Structs, Interfaces, Records

			// While there are differences in the syntax of the four (classes have more possible modifiers, structs and interfaces can
			// only inherit interfaces, etc.) they are pretty small and for our purposes we can combine them into one parsing function.
			// It's okay to be over tolerant.

			#if DEBUG
			if (mode == ParseMode.CreateElements && elements == null)
				{  throw new Exception("Elements and scope must be set when using ParseMode.CreateElements().");  }
			#endif

			TokenIterator lookahead = iterator;


			// Attributes

			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			TokenIterator startOfModifiers = lookahead;
			TokenIterator endOfModifiers = lookahead;

			AccessLevel accessLevel;

			// This covers "partial" as well, even though that's listed separately in the documentaton.
			if (TryToSkipModifiers(ref lookahead, out accessLevel, mode))
				{
				endOfModifiers = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}

			if (mode == ParseMode.ParseClassPrototype &&
				endOfModifiers > startOfModifiers)
				{  startOfModifiers.SetClassPrototypeParsingTypeBetween(endOfModifiers, ClassPrototypeParsingType.Modifier);  }


			// Keyword

			if (lookahead.MatchesToken("class") == false &&
				lookahead.MatchesToken("struct") == false &&
				lookahead.MatchesToken("interface") == false &&
				lookahead.MatchesToken("record") == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			string keyword = lookahead.String;

			if (mode == ParseMode.ParseClassPrototype)
				{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.Keyword;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Name

			string name;
			if (TryToSkipIdentifier(ref lookahead, out name, mode, PrototypeParsingType.Name) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			// Template signature

			if (TryToSkipTemplateSignature(ref lookahead, mode, false))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Record parameters

			TokenIterator startOfRecordParameters = lookahead;
			TokenIterator endOfRecordParameters = lookahead;
			bool hasRecordParameters = false;

			if (keyword == "record" && TryToSkipParameters(ref lookahead, mode))
				{
				hasRecordParameters = true;
				endOfRecordParameters = lookahead;

				TryToSkipWhitespace(ref lookahead, mode);
				}


			// Base classes and interfaces

			// If you have "record MyRec (int X, int Y) : ParentRec (X)", then X is defined in the parent record and inherited.  It will
			// not be defined as a new property in MyRec.  Therefore we need to collect the names that appear in the parent statements
			// so we can ignore them when creating new properties.
			StringSet ignoredRecordParameters = null;

			if (lookahead.Character == ':')
				{
				if (mode == ParseMode.ParseClassPrototype)
					{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfParents;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				for (;;)
					{

					// Parent identifier

					TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Null);
					TryToSkipWhitespace(ref lookahead);


					// Parent template signature

					if (TryToSkipTemplateSignature(ref lookahead, mode, true))
						{  TryToSkipWhitespace(ref lookahead);  }


					// Parent record parameters

					if (keyword == "record" && lookahead.Character == '(')
						{
						if (ignoredRecordParameters == null)
							{  ignoredRecordParameters = new StringSet();  }

						lookahead.Next();
						TryToSkipWhitespace(ref lookahead);

						while (lookahead.IsInBounds)
							{
							TokenIterator startOfIdentifier = lookahead;

							if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode))
								{
								string identifier = startOfIdentifier.TextBetween(lookahead);

								TryToSkipWhitespace(ref lookahead);

								// Only add it if it's the last word before a comma or parenthesis, so we skip modifiers like "in"
								if (lookahead.Character == ',' ||
									lookahead.Character == ')')
									{
									ignoredRecordParameters.Add(identifier);
									}
								}
							else if (lookahead.Character == ',')
								{
								lookahead.Next();
								TryToSkipWhitespace(ref lookahead);
								}
							else if (lookahead.Character == ')')
								{
								lookahead.Next();
								TryToSkipWhitespace(ref lookahead);
								break;
								}
							else
								{  lookahead.Next();  }
							}
						}

					if (lookahead.Character != ',')
						{  break;  }

					if (mode == ParseMode.ParseClassPrototype)
						{  lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.ParentSeparator;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				}


			// Constraint clauses

			while (TryToSkipWhereClause(ref lookahead, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Start of body

			if (lookahead.Character != '{' &&
				lookahead.Character != ';' &&
				lookahead.IsInBounds)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}


			// Create elements

			if (mode == ParseMode.CreateElements)
				{
				SymbolString symbol = scope + SymbolString.FromPlainText_NoParameters(name);

				ClassString classString = ClassString.FromParameters(EngineInstance.Hierarchies.ClassHierarchyID, language.ID, true, symbol);

				ContextString childContext = new ContextString();
				childContext.Scope = symbol;

				ParentElement classElement = new ParentElement(iterator, Element.Flags.InCode);
				classElement.DefaultChildLanguageID = language.ID;
				classElement.DefaultChildClassString = classString;
				classElement.ChildContextString = childContext;
				classElement.MaximumEffectiveChildAccessLevel = accessLevel;

				if (keyword == "interface" || keyword == "record")
					{  classElement.DefaultDeclaredChildAccessLevel = AccessLevel.Public;  }
				else // "class" or "struct"
					{  classElement.DefaultDeclaredChildAccessLevel = AccessLevel.Private;  }

				int commentTypeID = EngineInstance.CommentTypes.IDFromKeyword(keyword, language.ID);

				if (commentTypeID != 0)
					{
					Topic classTopic = new Topic(EngineInstance.CommentTypes);
					classTopic.Title = symbol.FormatWithSeparator('.');  // so the title is fully resolved
					classTopic.Symbol = symbol;
					classTopic.ClassString = classString;
					classTopic.Prototype = NormalizePrototype( iterator.TextBetween(lookahead) );
					classTopic.CommentTypeID = commentTypeID;
					classTopic.LanguageID = language.ID;
					classTopic.DeclaredAccessLevel = accessLevel;
					classTopic.CodeLineNumber = iterator.LineNumber;

					classElement.Topic = classTopic;
					}

				elements.Add(classElement);


				// Add properties from record parameters

				if (hasRecordParameters)
					{
					int propertyCommentTypeID = EngineInstance.CommentTypes.IDFromKeyword("property", language.ID);

					if (propertyCommentTypeID != 0)
						{
						// Extract the entire parameter parenthetical into its own Tokenizer.
						var parametersTokenizer = Tokenizer.CreateFromIterators(startOfRecordParameters, endOfRecordParameters);

						// Walk over it with ParseMode.ParsePrototype to set the PrototypeParsingType tokens.
						var tempIterator = parametersTokenizer.FirstToken;
						TryToSkipParameters(ref tempIterator, ParseMode.ParsePrototype);

						// Now we can throw it into a ParsedPrototype in order to easily iterate through the parameters and get their
						// properties.
						var parametersPrototype = new ParsedPrototype(parametersTokenizer, this.Language.ID, propertyCommentTypeID,
																							   parameterStyle: ParameterStyle.C, supportsImpliedTypes: true);

						for (int i = 0; i < parametersPrototype.NumberOfParameters; i++)
							{
							TokenIterator parameterNameStart, parameterNameEnd;

							if (parametersPrototype.GetParameterName(i, out parameterNameStart, out parameterNameEnd))
								{
								string parameterName = parameterNameStart.TextBetween(parameterNameEnd);

								if (ignoredRecordParameters == null ||
									ignoredRecordParameters.Contains(parameterName) == false)
									{

									// Construct a prototype

									// Start by getting the full parameter line.
									TokenIterator fullParameterStart, fullParameterEnd;
									parametersPrototype.GetParameter(i, out fullParameterStart, out fullParameterEnd);

									// Trim off the trailing comma and default value expression
									TokenIterator paramLookBehind = fullParameterEnd;
									paramLookBehind.Previous();

									while (paramLookBehind > fullParameterStart &&
											  (paramLookBehind.PrototypeParsingType == PrototypeParsingType.ParamSeparator ||
											   paramLookBehind.PrototypeParsingType == PrototypeParsingType.DefaultValue ||
											   paramLookBehind.PrototypeParsingType == PrototypeParsingType.DefaultValueSeparator ||
											   paramLookBehind.FundamentalType == FundamentalType.Whitespace ||
											   paramLookBehind.FundamentalType == FundamentalType.LineBreak))
										{
										fullParameterEnd.Previous();
										paramLookBehind.Previous();
										}

									// Trim off leading modifiers
									TryToSkipWhitespace(ref fullParameterStart);

									while (fullParameterStart.MatchesToken("in") ||
											  fullParameterStart.MatchesToken("params"))
										{
										TokenIterator paramLookahead = fullParameterStart;
										paramLookahead.Next();

										// Only accept it if there's whitespace following it, so it's not "in_object".
										if (TryToSkipWhitespace(ref paramLookahead))
											{  fullParameterStart = paramLookahead;  }
										else
											{  break;  }
										}

									// See if there's any attributes at the beginning, because if there are we want to place "public" after it so
									// we don't end up with "public [attributes] int X { get; init }"
									string propertyPrototype;
									TokenIterator afterAttributes = fullParameterStart;

									if (TryToSkipAttributes(ref afterAttributes))
										{
										propertyPrototype = fullParameterStart.TextBetween(afterAttributes) + " public ";
										TryToSkipWhitespace(ref afterAttributes);
										propertyPrototype += afterAttributes.TextBetween(fullParameterEnd) + " { get; init }";
										}
									else
										{
										propertyPrototype = "public " + fullParameterStart.TextBetween(fullParameterEnd) + " { get; init }";
										}

									// Find the parameter's location in the original text so we get the correct character number, since we
									// cut off the beginning of the line for our tokenizer.
									TokenIterator propertyLocation = startOfRecordParameters;
									propertyLocation.Next(fullParameterStart.TokenIndex);


									// Create the topic

									Topic propertyTopic = new Topic(EngineInstance.CommentTypes);
									propertyTopic.Title = parameterName;
									propertyTopic.Symbol = symbol + SymbolString.FromPlainText_NoParameters(parameterName);
									propertyTopic.Prototype = NormalizePrototype(propertyPrototype);
									propertyTopic.CommentTypeID = propertyCommentTypeID;
									propertyTopic.LanguageID = language.ID;
									propertyTopic.CodeLineNumber = propertyLocation.LineNumber;

									Element propertyElement = new Element(propertyLocation, Element.Flags.InCode);
									propertyElement.Topic = propertyTopic;

									elements.Add(propertyElement);
									}
								}
							}
						}
					}


				// Body

				iterator = lookahead;

				if (iterator.Character == '{')
					{
					iterator.Next();
					GetCodeElements(ref iterator, elements, symbol, '}');
					}

				classElement.EndingLineNumber = iterator.LineNumber;
				classElement.EndingCharNumber = iterator.CharNumber;

				return true;
				}

			else // mode isn't CreateElements
				{
				iterator = lookahead;

				if (iterator.Character == '{')
					{
					if (mode == ParseMode.ParseClassPrototype)
						{  iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfBody;  }

					iterator.Next();
					GenericSkipUntilAfter(ref iterator, '}');
					}

				return true;
				}
			}


		/* Function: TryToSkipFunction
		 *
		 * If the iterator is on a function, delegate, or operator other than a conversion or indexer, moves it past it and returns true.  If the
		 * mode is set to <ParseMode.CreateElements> it will add it to the list of <Elements>.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.CreateElements>
		 *			- The elements and scope parameters must be set.
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipFunction (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly, List<Element> elements = null,
													   SymbolString scope = default(SymbolString))
			{
			// Functions, Delegates, Operators

			#if DEBUG
			if (mode == ParseMode.CreateElements && elements == null)
				{  throw new Exception("Elements and scope must be set when using ParseMode.CreateElements().");  }
			#endif

			TokenIterator lookahead = iterator;


			// Attributes

			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly, mode, PrototypeParsingType.StartOfPrototypeSection))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			AccessLevel accessLevel;

			// This covers "partial" as well, even though that's listed separately in the documentaton.
			if (TryToSkipModifiers(ref lookahead, out accessLevel, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword

			string keyword = null;

			if (lookahead.MatchesToken("delegate"))
				{  keyword = "delegate";  }
			else if (lookahead.MatchesToken("const") ||
					  lookahead.MatchesToken("event") ||
					  lookahead.MatchesToken("implicit") ||
					  lookahead.MatchesToken("explicit") ||
					  lookahead.MatchesToken("enum") ||
					  lookahead.MatchesToken("using"))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (keyword == null)
				{  keyword = "function";  }
			else
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Return type

			if (TryToSkipType(ref lookahead, mode) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			// Name

			string name;
			TokenIterator startOfName = lookahead;

			if (TryToSkipIdentifier(ref lookahead, out name, mode, PrototypeParsingType.Name) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			if (name == "operator")
				{
				keyword = "operator";
				name += ' ';

				if (lookahead.MatchesToken("true") ||
					lookahead.MatchesToken("false"))
					{
					name += lookahead.String;
					lookahead.Next();
					}
				else
					{
					while (lookahead.FundamentalType == FundamentalType.Symbol &&
							 lookahead.Character != '(')
						{
						name += lookahead.Character;
						lookahead.Next();
						}
					}

				if (mode == ParseMode.ParsePrototype)
					{  startOfName.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.KeywordName);  }

				TryToSkipWhitespace(ref lookahead);
				}


			// Template signature

			if (TryToSkipTemplateSignature(ref lookahead, mode, false))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Parameters

			if (!TryToSkipParameters(ref lookahead, mode))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			// Constraint clauses

			while (TryToSkipWhereClause(ref lookahead, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			if (lookahead.IsInBounds &&
				lookahead.Character != '{' &&
				lookahead.Character != ';' &&
				lookahead.MatchesAcrossTokens("=>") == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}


			// Create element

			if (mode == ParseMode.CreateElements)
				{
				int commentTypeID = EngineInstance.CommentTypes.IDFromKeyword(keyword, language.ID);

				if (commentTypeID != 0)
					{
					Topic functionTopic = new Topic(EngineInstance.CommentTypes);
					functionTopic.Title = name;
					functionTopic.Symbol = scope + SymbolString.FromPlainText_NoParameters(name);
					functionTopic.Prototype = NormalizePrototype( iterator.TextBetween(lookahead) );
					functionTopic.CommentTypeID = commentTypeID;
					functionTopic.LanguageID = language.ID;
					functionTopic.DeclaredAccessLevel = accessLevel;
					functionTopic.CodeLineNumber = iterator.LineNumber;

					Element functionElement = new Element(iterator, Element.Flags.InCode);
					functionElement.Topic = functionTopic;

					elements.Add(functionElement);
					}
				}


			// Body

			if (lookahead.Character == '{')
				{
				lookahead.Next();
				GenericSkipUntilAfter(ref lookahead, '}');
				}
			else if (lookahead.Character == ';')
				{  lookahead.Next();  }
			else if (lookahead.MatchesAcrossTokens("=>"))
				{
				lookahead.Next(2);
				GenericSkipUntilAfter(ref lookahead, ';');
				}
			// Don't fail if it doesn't exist since we may be parsing a prototype.


			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipConstructor
		 *
		 * If the iterator is on a constructor or destructor, moves it past it and returns true.  If the mode is set to <ParseMode.CreateElements>
		 * it will add it to the list of <Elements>.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.CreateElements>
		 *			- The elements and scope parameters must be set.
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipConstructor (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly, List<Element> elements = null,
														   SymbolString scope = default(SymbolString))
			{
			// Constructors, Destructors

			#if DEBUG
			if (mode == ParseMode.CreateElements && elements == null)
				{  throw new Exception("Elements and scope must be set when using ParseMode.CreateElements().");  }
			#endif

			TokenIterator lookahead = iterator;


			// Attributes

			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly, mode, PrototypeParsingType.StartOfPrototypeSection))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			AccessLevel accessLevel;

			if (TryToSkipModifiers(ref lookahead, out accessLevel, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword

			string keyword;

			if (lookahead.Character == '~')
				{
				keyword = "destructor";

				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.Name;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}
			else
				{  keyword = "constructor";  }


			// Name

			string name;
			if (TryToSkipIdentifier(ref lookahead, out name, mode, PrototypeParsingType.Name) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (name == "delegate" ||
				name == "const" ||
				name == "event" ||
				name == "implicit" ||
				name == "explicit" ||
				name == "enum" ||
				name == "using")
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (keyword == "destructor")
				{  name = "~" + name;  }

			TryToSkipWhitespace(ref lookahead);


			// Parameters

			if (!TryToSkipParameters(ref lookahead, mode))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TokenIterator endOfPrototype = lookahead;
			TryToSkipWhitespace(ref lookahead);


			// Constructor initializer

			if (lookahead.Character == ':' && keyword == "constructor")
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.MatchesToken("base") == false &&
					lookahead.MatchesToken("this") == false)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character != '(')
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				lookahead.Next();
				GenericSkipUntilAfter(ref lookahead, ')');
				TryToSkipWhitespace(ref lookahead);
				}

			if (lookahead.IsInBounds &&
				lookahead.Character != '{' &&
				lookahead.Character != ';' &&
				lookahead.MatchesAcrossTokens("=>") == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}


			// Create element

			if (mode == ParseMode.CreateElements)
				{
				int commentTypeID = EngineInstance.CommentTypes.IDFromKeyword(keyword, language.ID);

				if (commentTypeID != 0)
					{
					Topic functionTopic = new Topic(EngineInstance.CommentTypes);
					functionTopic.Title = name;
					functionTopic.Symbol = scope + SymbolString.FromPlainText_NoParameters(name);
					functionTopic.Prototype = NormalizePrototype( iterator.TextBetween(endOfPrototype) );
					functionTopic.CommentTypeID = commentTypeID;
					functionTopic.LanguageID = language.ID;
					functionTopic.DeclaredAccessLevel = accessLevel;
					functionTopic.CodeLineNumber = iterator.LineNumber;

					Element functionElement = new Element(iterator, Element.Flags.InCode);
					functionElement.Topic = functionTopic;

					elements.Add(functionElement);
					}
				}


			// Body

			if (lookahead.Character == '{')
				{
				lookahead.Next();
				GenericSkipUntilAfter(ref lookahead, '}');
				}
			else if (lookahead.Character == ';')
				{  lookahead.Next();  }
			else if (lookahead.MatchesAcrossTokens("=>"))
				{
				lookahead.Next(2);
				GenericSkipUntilAfter(ref lookahead, ';');
				}
			// Don't fail if it doesn't exist since we may be parsing a prototype.


			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipConversionOperator
		 *
		 * If the iterator is on a conversion operator, moves it past it and returns true.  If the mode is set to <ParseMode.CreateElements>
		 * it will add it to the list of <Elements>.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.CreateElements>
		 *			- The elements and scope parameters must be set.
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipConversionOperator (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
																	 List<Element> elements = null, SymbolString scope = default(SymbolString))
			{
			#if DEBUG
			if (mode == ParseMode.CreateElements && elements == null)
				{  throw new Exception("Elements and scope must be set when using ParseMode.CreateElements().");  }
			#endif

			TokenIterator lookahead = iterator;


			// Attributes

			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly, mode, PrototypeParsingType.StartOfPrototypeSection))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			AccessLevel accessLevel;

			// This covers "partial" as well, even though that's listed separately in the documentaton.
			if (TryToSkipModifiers(ref lookahead, out accessLevel, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keywords

			if (lookahead.MatchesToken("implicit") == false &&
				lookahead.MatchesToken("explicit") == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			if (lookahead.MatchesToken("operator") == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.KeywordName;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Name

			System.Text.StringBuilder name = new System.Text.StringBuilder("operator ");
			TokenIterator startOfType = lookahead;

			// IterateOnly so we don't mark it as a type when using ParseMode.ParsePrototype
			if (TryToSkipType(ref lookahead, ParseMode.IterateOnly) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			startOfType.AppendTextBetweenTo(lookahead, name);

			if (mode == ParseMode.ParsePrototype)
				{  startOfType.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.KeywordName);  }

			TryToSkipWhitespace(ref lookahead);


			// Parameters

			if (!TryToSkipParameters(ref lookahead, mode))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);

			if (lookahead.IsInBounds &&
				lookahead.Character != '{' &&
				lookahead.Character != ';' &&
				lookahead.MatchesAcrossTokens("=>") == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}


			// Create element

			if (mode == ParseMode.CreateElements)
				{
				int commentTypeID = EngineInstance.CommentTypes.IDFromKeyword("operator", language.ID);

				if (commentTypeID != 0)
					{
					Topic operatorTopic = new Topic(EngineInstance.CommentTypes);
					operatorTopic.Title = name.ToString();
					operatorTopic.Symbol = scope + SymbolString.FromPlainText_NoParameters(operatorTopic.Title);
					operatorTopic.Prototype = NormalizePrototype( iterator.TextBetween(lookahead) );
					operatorTopic.CommentTypeID = commentTypeID;
					operatorTopic.LanguageID = language.ID;
					operatorTopic.DeclaredAccessLevel = accessLevel;
					operatorTopic.CodeLineNumber = iterator.LineNumber;

					Element operatorElement = new Element(iterator, Element.Flags.InCode);
					operatorElement.Topic = operatorTopic;

					elements.Add(operatorElement);
					}
				}


			// Body

			if (lookahead.Character == '{')
				{
				lookahead.Next();
				GenericSkipUntilAfter(ref lookahead, '}');
				}
			else if (lookahead.Character == ';')
				{  lookahead.Next();  }
			else if (lookahead.MatchesAcrossTokens("=>"))
				{
				lookahead.Next(2);
				GenericSkipUntilAfter(ref lookahead, ';');
				}
			// Don't fail if it doesn't exist since we may be parsing a prototype.


			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipVariable
		 *
		 * If the iterator is on a variable, constant, or event, moves it past it and returns true.  If the mode is set to <ParseMode.CreateElements>
		 * it will add it to the list of <Elements>.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.CreateElements>
		 *			- The elements and scope parameters must be set.
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipVariable (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly, List<Element> elements = null,
													 SymbolString scope = default(SymbolString))
			{
			// Variables, Constants, Events

			#if DEBUG
			if (mode == ParseMode.CreateElements && elements == null)
				{  throw new Exception("Elements and scope must be set when using ParseMode.CreateElements().");  }
			#endif

			TokenIterator lookahead = iterator;


			// Attributes

			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly, mode, PrototypeParsingType.StartOfPrototypeSection))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			AccessLevel accessLevel;

			if (TryToSkipModifiers(ref lookahead, out accessLevel, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword and Type

			string keyword = null;
			TokenIterator endOfType;

			if (lookahead.MatchesToken("delegate"))
				{
				// Only accept the "delegate" keyword if it's part of a function pointer.  If it's an actual delegate definition then
				// we want to fail.
				if (!TryToSkipFunctionPointer(ref lookahead, mode))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				keyword = "variable";
				endOfType = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}
			else
				{
				if (lookahead.MatchesToken("const"))
					{  keyword = "constant";  }
				else if (lookahead.MatchesToken("event"))
					{  keyword = "event";  }
				else if (lookahead.MatchesToken("implicit") ||
						  lookahead.MatchesToken("explicit") ||
						  lookahead.MatchesToken("enum") ||
						  lookahead.MatchesToken("using"))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				if (keyword == null)
					{  keyword = "variable";  }
				else
					{
					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}

				if (TryToSkipType(ref lookahead, mode) == false)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				endOfType = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}


			// Name

			string name;
			if (TryToSkipIdentifier(ref lookahead, out name, mode, PrototypeParsingType.Name) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			// Fixed size buffer

			// Only valid for unsafe structs, and can only be a single dimension
			if (lookahead.Character == '[')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.OpeningParamModifier;  }

				lookahead.Next();

				while (lookahead.IsInBounds && lookahead.Character != ']')
					{  GenericSkip(ref lookahead);  }

				if (lookahead.Character != ']')
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.ClosingParamModifier;  }

				lookahead.Next();
				lookahead.NextPastWhitespace();
				}

			if (lookahead.IsInBounds &&
				lookahead.Character != ';' &&
				lookahead.Character != ',' &&
				( lookahead.Character != '=') ||
				  lookahead.MatchesAcrossTokens("=>") )
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}


			// Create element

			int commentTypeID = EngineInstance.CommentTypes.IDFromKeyword(keyword, language.ID);

			if (mode == ParseMode.CreateElements && commentTypeID != 0)
				{
				Topic variableTopic = new Topic(EngineInstance.CommentTypes);
				variableTopic.Title = name;
				variableTopic.Symbol = scope + SymbolString.FromPlainText_NoParameters(name);
				variableTopic.Prototype = NormalizePrototype( iterator.TextBetween(lookahead) );
				variableTopic.CommentTypeID = commentTypeID;
				variableTopic.LanguageID = language.ID;
				variableTopic.DeclaredAccessLevel = accessLevel;
				variableTopic.CodeLineNumber = iterator.LineNumber;

				Element variableElement = new Element(iterator, Element.Flags.InCode);
				variableElement.Topic = variableTopic;

				elements.Add(variableElement);
				}


			// Multiple declarations and default values

			while (lookahead.IsInBounds && lookahead.Character != ';')
				{
				if (lookahead.Character == '=')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.DefaultValueSeparator;  }

					lookahead.Next();
					TokenIterator startOfDefaultValue = lookahead;

					while (lookahead.IsInBounds &&
							 lookahead.Character != ',' &&
							 lookahead.Character != ';')
						{  GenericSkip(ref lookahead);  }

					if (mode == ParseMode.ParsePrototype)
						{  startOfDefaultValue.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.DefaultValue);  }
					}
				else if (lookahead.Character == ',')
					{
					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);

					TokenIterator startOfNewName = lookahead;

					string newName;
					if (TryToSkipIdentifier(ref lookahead, out newName, mode, PrototypeParsingType.Name) == false)
						{  break;  }

					TryToSkipWhitespace(ref lookahead);

					if (lookahead.IsInBounds &&
						lookahead.Character != ';' &&
						lookahead.Character != ',' &&
						lookahead.Character != '=')
						{  break;  }

					if (mode == ParseMode.CreateElements && commentTypeID != 0)
						{
						Topic newVariableTopic = new Topic(EngineInstance.CommentTypes);
						newVariableTopic.Title = newName;
						newVariableTopic.Symbol = scope + SymbolString.FromPlainText_NoParameters(newName);
						newVariableTopic.Prototype = NormalizePrototype( iterator.TextBetween(endOfType) + " " + newName );
						newVariableTopic.CommentTypeID = commentTypeID;
						newVariableTopic.LanguageID = language.ID;
						newVariableTopic.DeclaredAccessLevel = accessLevel;
						newVariableTopic.CodeLineNumber = startOfNewName.LineNumber;

						Element newVariableElement = new Element(startOfNewName, Element.Flags.InCode);
						newVariableElement.Topic = newVariableTopic;

						elements.Add(newVariableElement);
						}
					}
				else // shouldn't get here, but just in case
					{  break;  }
				}

			if (lookahead.Character == ';')
				{  lookahead.Next();  }

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipProperty
		 *
		 * If the iterator is on a property, indexer, or event declared like a property, moves it past it and returns true.  If the mode is set to
		 * <ParseMode.CreateElements> it will add it to the list of <Elements>.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.CreateElements>
		 *			- The elements and scope parameters must be set.
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipProperty (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly, List<Element> elements = null,
													  SymbolString scope = default(SymbolString))
			{
			// Properties, Indexers, Events

			#if DEBUG
			if (mode == ParseMode.CreateElements && elements == null)
				{  throw new Exception("Elements and scope must be set when using ParseMode.CreateElements().");  }
			#endif

			TokenIterator lookahead = iterator;


			// Attributes

			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly, mode, PrototypeParsingType.StartOfPrototypeSection))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			AccessLevel accessLevel;

			if (TryToSkipModifiers(ref lookahead, out accessLevel, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword

			string keyword = null;

			if (lookahead.MatchesToken("event"))
				{  keyword = "event";  }
			else if (lookahead.MatchesToken("const") ||
					  lookahead.MatchesToken("implicit") ||
					  lookahead.MatchesToken("explicit") ||
					  lookahead.MatchesToken("enum") ||
					  lookahead.MatchesToken("delegate") ||
					  lookahead.MatchesToken("using"))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (keyword == null)
				{  keyword = "property";  }
			else
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Type

			if (TryToSkipType(ref lookahead, mode) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			// Name

			string name;
			TokenIterator startOfName = lookahead;

			if (TryToSkipIdentifier(ref lookahead, out name, mode, PrototypeParsingType.Name) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);


			// Indexer

			if (name == "this" ||
				name.EndsWith(".this"))  // It may be InterfaceName.this[]
				{
				keyword = "operator";

				if (mode == ParseMode.ParsePrototype)
					{  startOfName.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.KeywordName);  }

				if (!TryToSkipParameters(ref lookahead, mode, openingSymbol: '['))
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				TryToSkipWhitespace(ref lookahead);
				}


			if (lookahead.Character != '{' &&
				lookahead.MatchesAcrossTokens("=>") == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}


			// Build prototype

			if (mode == ParseMode.CreateElements)
				{
				System.Text.StringBuilder prototype = new System.Text.StringBuilder();

				iterator.AppendTextBetweenTo(lookahead, prototype);
				prototype.Append(" { ");

				if (lookahead.Character == '{')
					{
					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);

					bool firstAccessor = true;

					while (lookahead.IsInBounds && lookahead.Character != '}')
						{
						TokenIterator startOfAccessor = lookahead;

						if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly))
							{  TryToSkipWhitespace(ref lookahead);  }

						// Accessors may have their own access levels in properties (but not in events) but for the documentation we're always
						// going to use the overall property's access level.
						if (TryToSkipModifiers(ref lookahead))
							{  TryToSkipWhitespace(ref lookahead);  }

						bool validAccessor = false;

						if (keyword == "property" || keyword == "operator")
							{
							validAccessor = (lookahead.MatchesToken("get") ||
													 lookahead.MatchesToken("set") ||
													 lookahead.MatchesToken("init"));
							}
						else if (keyword == "event")
							{
							validAccessor = (lookahead.MatchesToken("add") ||
													 lookahead.MatchesToken("remove"));
							}

						if (!validAccessor)
							{
							ResetTokensBetween(iterator, lookahead, mode);
							return false;
							}

						lookahead.Next();

						if (!firstAccessor)
							{  prototype.Append("; ");  }
						else
							{  firstAccessor = false;  }

						startOfAccessor.AppendTextBetweenTo(lookahead, prototype);

						TryToSkipWhitespace(ref lookahead);

						if (lookahead.Character == ';')
							{  lookahead.Next();  }
						else if (lookahead.Character == '{')
							{
							lookahead.Next();
							GenericSkipUntilAfter(ref lookahead, '}');
							}
						else if (lookahead.MatchesAcrossTokens("=>"))
							{
							lookahead.Next(2);
							GenericSkipUntilAfter(ref lookahead, ';');
							}

						TryToSkipWhitespace(ref lookahead);
						}

					// Closing brace
					lookahead.Next();
					}

				else // lookahead is at "=>"
					{
					prototype.Append("get");

					lookahead.Next(2);
					GenericSkipUntilAfter(ref lookahead, ';');
					}

				prototype.Append(" }");


				// Create element

				int commentTypeID = EngineInstance.CommentTypes.IDFromKeyword(keyword, language.ID);

				if (commentTypeID != 0)
					{
					Topic propertyTopic = new Topic(EngineInstance.CommentTypes);
					propertyTopic.Title = name;

					// We don't attach it to the name variable earlier because we don't want it to be part of the symbol.
					if (keyword == "operator")
						{  propertyTopic.Title += " []";  }

					propertyTopic.Symbol = scope + SymbolString.FromPlainText_NoParameters(name);
					propertyTopic.Prototype = NormalizePrototype(prototype.ToString());
					propertyTopic.CommentTypeID = commentTypeID;
					propertyTopic.LanguageID = language.ID;
					propertyTopic.DeclaredAccessLevel = accessLevel;
					propertyTopic.CodeLineNumber = iterator.LineNumber;

					Element propertyElement = new Element(iterator, Element.Flags.InCode);
					propertyElement.Topic = propertyTopic;

					elements.Add(propertyElement);
					}
				}

			else // mode isn't CreateElements
				{
				if (lookahead.Character == '{')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfParams;  }

					lookahead.Next();
					TokenIterator startOfModifiers = lookahead;

					while (lookahead.IsInBounds)
						{
						if (lookahead.Character == '}')
							{
							if (mode == ParseMode.ParsePrototype)
								{  lookahead.PrototypeParsingType = PrototypeParsingType.EndOfParams;  }

							lookahead.Next();
							break;
							}
						else if (lookahead.Character == ';')
							{
							if (mode == ParseMode.ParsePrototype)
								{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;  }

							lookahead.Next();
							startOfModifiers = lookahead;
							}
						else if ( (
									  (keyword == "property" || keyword == "operator") &&
								 	  (lookahead.MatchesToken("get") ||
								       lookahead.MatchesToken("set") ||
								       lookahead.MatchesToken("init"))
									 ) ||

									(
									  keyword == "event" &&
								 	   (lookahead.MatchesToken("add") ||
								        lookahead.MatchesToken("remove"))
									) )
							{
							if (mode == ParseMode.ParsePrototype)
								{
								lookahead.PrototypeParsingType = PrototypeParsingType.KeywordName;

								TokenIterator endOfModifiers = lookahead;
								endOfModifiers.Previous();
								endOfModifiers.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds);

								startOfModifiers.NextPastWhitespace();

								if (endOfModifiers > startOfModifiers)
									{  startOfModifiers.SetPrototypeParsingTypeBetween(endOfModifiers, PrototypeParsingType.TypeModifier);  }
								}

							lookahead.Next();
							}
						else
							{
							GenericSkip(ref lookahead);
							}
						}
					}
				else // =>
					{
					lookahead.Next(2);
					GenericSkipUntilAfter(ref lookahead, ';');
					}
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipEnum
		 *
		 * If the iterator is on an enum, moves it past it and returns true.  If the mode is set to <ParseMode.CreateElements> it will add it
		 * to the list of <Elements>.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.CreateElements>
		 *			- The elements and scope parameters must be set.
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipEnum (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly, List<Element> elements = null,
												  SymbolString scope = default(SymbolString))
			{
			#if DEBUG
			if (mode == ParseMode.CreateElements && elements == null)
				{  throw new Exception("Elements and scope must be set when using ParseMode.CreateElements().");  }
			#endif

			TokenIterator lookahead = iterator;
			TokenIterator endOfPrototype;


			// Attributes

			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly, mode, PrototypeParsingType.StartOfPrototypeSection))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			AccessLevel accessLevel;

			if (TryToSkipModifiers(ref lookahead, out accessLevel, mode))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword

			if (lookahead.MatchesToken("enum") == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Name

			string name;
			if (TryToSkipIdentifier(ref lookahead, out name, mode, PrototypeParsingType.Name) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			endOfPrototype = lookahead;
			TryToSkipWhitespace(ref lookahead);


			// Type

			if (lookahead.Character == ':')
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (TryToSkipType(ref lookahead, mode) == false)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}

				endOfPrototype = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}


			// Create enum element

			int elementsAdded = 0;

			int enumCommentTypeID = 0;
			int constantCommentTypeID = 0;

			ParentElement enumElement = null;
			Topic enumTopic = null;

			if (mode == ParseMode.CreateElements)
				{
				enumCommentTypeID = EngineInstance.CommentTypes.IDFromKeyword("enum", language.ID);
				constantCommentTypeID = EngineInstance.CommentTypes.IDFromKeyword("constant", language.ID);

				SymbolString symbol = scope + SymbolString.FromPlainText_NoParameters(name);

				ContextString childContext = new ContextString();
				childContext.Scope = symbol;

				enumElement = new ParentElement(iterator, Element.Flags.InCode);
				enumElement.ChildContextString = childContext;
				enumElement.MaximumEffectiveChildAccessLevel = accessLevel;

				if (enumCommentTypeID != 0)
					{
					enumTopic = new Topic(EngineInstance.CommentTypes);
					enumTopic.Title = name;
					enumTopic.Symbol = symbol;
					enumTopic.Prototype = NormalizePrototype( iterator.TextBetween(endOfPrototype) );
					enumTopic.CommentTypeID = enumCommentTypeID;
					enumTopic.LanguageID = language.ID;
					enumTopic.DeclaredAccessLevel = accessLevel;
					enumTopic.CodeLineNumber = iterator.LineNumber;

					enumElement.Topic = enumTopic;
					}

				elements.Add(enumElement);
				elementsAdded++;
				}


			// Values

			// The body isn't included in prototypes so we shouldn't fail if it's missing
			if (lookahead.Character == '{')
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead, ParseMode.IterateOnly);

				// We check this first in case the body is empty
				if (lookahead.Character == '}')
					{  lookahead.Next();  }
				else
					{
					for (;;)
						{
						TokenIterator startOfValue = lookahead;


						// Value attributes

						if (TryToSkipAttributes(ref lookahead, mode: mode, prototypeParsingType: PrototypeParsingType.ParamModifier))
							{  TryToSkipWhitespace(ref lookahead, ParseMode.IterateOnly);  }


						// Value name

						string valueName;

						if (!TryToSkipIdentifier(ref lookahead, out valueName, mode, PrototypeParsingType.Name))
							{
							ResetTokensBetween(iterator, lookahead, mode);
							elements.RemoveRange(elements.Count - elementsAdded, elementsAdded);
							return false;
							}

						TokenIterator endOfValue = lookahead;


						// Create value element

						Element valueElement = null;
						Topic valueTopic = null;

						if (mode == ParseMode.CreateElements)
							{
							valueElement = new Element(startOfValue, Element.Flags.InCode);

							if (constantCommentTypeID != 0)
								{
								valueTopic = new Topic(EngineInstance.CommentTypes);
								valueTopic.Title = valueName;
								valueTopic.Symbol = enumElement.ChildContextString.Scope + SymbolString.FromPlainText_NoParameters(valueName);
								valueTopic.IsEmbedded = true;
								valueTopic.LanguageID = language.ID;
								valueTopic.CommentTypeID = constantCommentTypeID;

								valueElement.Topic = valueTopic;
								}

							elements.Add(valueElement);
							elementsAdded++;
							}

						TryToSkipWhitespace(ref lookahead, mode);


						// Value constant

						if (lookahead.Character == '=')
							{
							lookahead.Next();
							TryToSkipWhitespace(ref lookahead, mode);

							while (lookahead.IsInBounds &&
									 lookahead.Character != ',' &&
									 lookahead.Character != '}')
								{
								if (!TryToSkipWhitespace(ref lookahead, ParseMode.IterateOnly))
									{
									GenericSkip(ref lookahead);
									endOfValue = lookahead;
									}
								}
							}


						// End of value or body

						char endCharacter = lookahead.Character;

						if (endCharacter == ',' ||
							endCharacter == '}')
							{
							lookahead.Next();

							if (lookahead.LineNumber == endOfValue.LineNumber)
								{  endOfValue = lookahead;  }

							if (mode == ParseMode.CreateElements)
								{
								var inlineComment = GetInlineDocumentationComment(endOfValue);

								if (inlineComment != null &&
									valueTopic != null)
									{
									Topic inlineTopic = EngineInstance.Comments.Parse(inlineComment);
									valueTopic.Body = inlineTopic.Body;
									valueTopic.Summary = inlineTopic.Summary;
									}
								}

							if (endCharacter == ',')
								{  TryToSkipWhitespace(ref lookahead, mode);  }
							else // endCharacter == '}'
								{  break;  }
							}
						else
							{
							ResetTokensBetween(iterator, lookahead, mode);
							elements.RemoveRange(elements.Count - elementsAdded, elementsAdded);
							return false;
							}
						}
					}
				}


			// Finish up

			if (mode == ParseMode.CreateElements)
				{
				enumElement.EndingLineNumber = lookahead.LineNumber;
				enumElement.EndingCharNumber = lookahead.CharNumber;
				}

			iterator = lookahead;
			return true;
			}



		// Group: Component Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToSkipParameters
		 *
		 * Tries to move the iterator past a comma-separated list of parameters in parentheses or brackets.  The opening symbol
		 * defaults to ( but can be set to [ or <.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipParameters (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly, char openingSymbol = '(')
			{
			if (iterator.Character != openingSymbol)
				{  return false;  }


			// Setup

			TokenIterator lookahead = iterator;

			char closingSymbol;

			if (openingSymbol == '(')
				{  closingSymbol = ')';  }
			else if (openingSymbol == '[')
				{  closingSymbol = ']';  }
			else if (openingSymbol == '<')
				{  closingSymbol = '>';  }
			else
				{  throw new InvalidOperationException();  }


			// Opening paren

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfParams;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Parameter list

			while (lookahead.IsInBounds && lookahead.Character != closingSymbol)
				{
				if (lookahead.Character == ',')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				else if (TryToSkipParameter(ref lookahead, closingSymbol, mode))
					{
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{  break;  }
				}


			// Closing paren

			if (lookahead.Character == closingSymbol)
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.EndOfParams;  }

				lookahead.Next();
				iterator = lookahead;
				return true;
				}
			else
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}
			}


		/* Function: TryToSkipParameter
		 *
		 * Tries to move the iterator past a parameter, such as "int x" or "IList<int> y = null".  The parameter ends at a comma or
		 * the closing symbol, which defaults to a closing parenthesis.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipParameter (ref TokenIterator iterator, char closingSymbol = ')', ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;

			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly, mode, PrototypeParsingType.TypeModifier) == true)
				{  TryToSkipWhitespace(ref lookahead);  }

			while (lookahead.MatchesToken("ref") ||
					  lookahead.MatchesToken("in") ||
					  lookahead.MatchesToken("out") ||
					  lookahead.MatchesToken("params") ||
					  lookahead.MatchesToken("this"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}

			if (TryToSkipType(ref lookahead, mode) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);

			if (TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Name) == false)
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			TryToSkipWhitespace(ref lookahead);

			if (lookahead.Character == '=')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.DefaultValueSeparator;  }

				lookahead.Next();
				TokenIterator startOfDefaultValue = lookahead;

				while (lookahead.IsInBounds &&
						 lookahead.Character != ',' &&
						 lookahead.Character != closingSymbol)
					{  GenericSkip(ref lookahead);  }

				if (mode == ParseMode.ParsePrototype)
					{  startOfDefaultValue.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.DefaultValue);  }
				}

			if (lookahead.Character == ',' ||
				lookahead.Character == closingSymbol)
				{
				iterator = lookahead;
				return true;
				}
			else
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}
			}


		/* Function: TryToSkipTuple
		 *
		 * Tries to move the iterator past a tuple, which may or not have names, multiple members, or embedded tuples.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipTuple (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '(')
				{  return false;  }

			TokenIterator lookahead = iterator;

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.StartOfTuple;  }

			lookahead.Next();
			lookahead.NextPastWhitespace();

			for (;;)
				{
				if (lookahead.Character == ')')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.EndOfTuple;  }

					lookahead.Next();

					iterator = lookahead;
					return true;
					}
				else if (lookahead.Character == ',')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.TupleMemberSeparator;  }

					lookahead.Next();
					lookahead.NextPastWhitespace();
					}
				else if (TryToSkipTupleMember(ref lookahead, mode))
					{
					lookahead.NextPastWhitespace();
					}
				else
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}
				}
			}


		/* Function: TryToSkipTupleMember
		 *
		 * Tries to move the iterator past a member of a tuple, which is a type and optionally a name.  The type can be another tuple.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipTupleMember (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			// Type, which may be another tuple

			if (!TryToSkipType(ref iterator, mode))
				{  return false;  }


			// Optional name

			TokenIterator lookahead = iterator;
			lookahead.NextPastWhitespace();

			if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode))
				{
				if (mode == ParseMode.ParsePrototype)
					{
					iterator.NextPastWhitespace();
					iterator.SetPrototypeParsingTypeBetween(lookahead, PrototypeParsingType.TupleMemberName);
					}

				iterator = lookahead;
				}

			return true;
			}


		/* Function: TryToSkipFunctionPointer
		 *
		 * Tries to move the iterator past a function pointer, such as "delegate*<int>" or "delegate unmanaged[Cdecl]<float>".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipFunctionPointer (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			// Delegate keyword

			if (!iterator.MatchesToken("delegate"))
				{  return false;  }

			TokenIterator lookahead = iterator;

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.Type;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Asterisk

			if (lookahead.Character != '*')
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			if (mode == ParseMode.ParsePrototype)
				{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Managed or Unmanaged modifier, optional

			if (lookahead.MatchesToken("managed"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}
			else if (lookahead.MatchesToken("unmanaged"))
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);


				// Declaration type modifier, optional

				if (lookahead.Character == '[')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;  }

					if (!TryToSkipBlock(ref lookahead, false))
						{
						ResetTokensBetween(iterator, lookahead, mode);
						return false;
						}

					if (mode == ParseMode.ParsePrototype)
						{
						lookahead.Previous();
						lookahead.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
						lookahead.Next();
						}

					TryToSkipWhitespace(ref lookahead);
					}
				}


			// Parameters as template signature

			if (!TryToSkipTemplateSignature(ref lookahead, mode, true))
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}

			iterator = lookahead;
			return true;
			}


		/* Function: TryToSkipType
		 *
		 * Tries to move the iterator past a type, such as "int", "(int, string)", "System.Collections.Generic.List<int>", or "int[]".  This can
		 * handle tuples and accepts "void" as a valid type.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipType (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			// Dedicated functions for more complicated types

			if (TryToSkipTuple(ref iterator, mode) ||
				TryToSkipFunctionPointer(ref iterator, mode))
				{  return true;  }


			// Base type

			TokenIterator lookahead = iterator;

			if (TryToSkipIdentifier(ref lookahead, mode, PrototypeParsingType.Type) == false)
				{  return false;  }

			iterator = lookahead;
			TryToSkipWhitespace(ref lookahead);


			// Template Signature

			if (TryToSkipTemplateSignature(ref lookahead, mode, true))
				{
				iterator = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}


			// Nullable

			if (lookahead.Character == '?')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

				lookahead.Next();
				iterator = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}


			// Pointer

			if (lookahead.Character == '*')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

				lookahead.Next();
				iterator = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}


			// Array

			while (lookahead.Character == '[')
				{
				if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				while (lookahead.Character == ',')
					{
					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}

				if (lookahead.Character == ']')
					{
					if (mode == ParseMode.ParsePrototype)
						{  lookahead.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;  }

					lookahead.Next();
					iterator = lookahead;
					TryToSkipWhitespace(ref lookahead);
					}
				else if (lookahead.IsInBounds == false)
					{
					ResetTokensBetween(iterator, lookahead, mode);
					return false;
					}
				else
					{  break;  }
				}

			return true;
			}


		/* Function: TryToSkipIdentifier
		 *
		 * Attempts to skip past and retrieve an identifier, such as "A.B.C".  Use <TryToSkipUnqualifiedIdentifier()> if you only want to
		 * retrieve a single segment.
		 *
		 * Note that this function will handle internal template signatures ("A.B<X>.C") but not the one at the end ("A.B.C<X>") since it
		 * is assumed that you would want to handle that one manually.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.Name> or
		 *			  <PrototypeParsingType.Type>.  If set to Type, it will use both <PrototypeParsingType.Type> and
		 *			  <PrototypeParsingType.TypeQualifier>.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- The tokens will be marked with <ClassPrototypeParsingType.Name>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipIdentifier (ref TokenIterator iterator, out string identifier, ParseMode mode = ParseMode.IterateOnly,
													   PrototypeParsingType prototypeParsingType = PrototypeParsingType.Name)
			{
			TokenIterator start = iterator;

			if (TryToSkipIdentifier(ref iterator, mode, prototypeParsingType))
				{
				identifier = start.TextBetween(iterator);
				return true;
				}
			else
				{
				identifier = null;
				return false;
				}
			}


		/* Function: TryToSkipIdentifier
		 *
		 * Tries to move the iterator past a qualified identifier, such as "A.B.C".  Use <TryToSkipUnqualifiedIdentifier()> if you only want
		 * to skip a single segment.
		 *
		 * Note that this function will handle internal template signatures ("A.B<X>.C") but not the one at the end ("A.B.C<X>") since it
		 * is assumed that you would want to handle that one manually.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.Name> or
		 *			  <PrototypeParsingType.Type>.  If set to Type, it will use both <PrototypeParsingType.Type> and
		 *			  <PrototypeParsingType.TypeQualifier>.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- The tokens will be marked with <ClassPrototypeParsingType.Name>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly,
													   PrototypeParsingType prototypeParsingType = PrototypeParsingType.Name)
			{
			TokenIterator lookahead = iterator;
			TokenIterator endOfIdentifier;
			TokenIterator endOfQualifier = iterator;

			for (;;)
				{
				if (TryToSkipUnqualifiedIdentifier(ref lookahead) == false)
					{  return false;  }

				endOfIdentifier = lookahead;
				TryToSkipWhitespace(ref lookahead);

				if (TryToSkipTemplateSignature(ref lookahead, mode, (prototypeParsingType == PrototypeParsingType.Type)))
					{
					TryToSkipWhitespace(ref lookahead);
					}

				if (lookahead.Character == '.')
					{
					if (lookahead.MatchesAcrossTokens("..") && iterator.MatchesToken("operator", true))
						{  break;  }

					lookahead.Next();
					}
				else if (lookahead.MatchesAcrossTokens("::"))
					{
					// :: can be used with "extern alias" identifiers.
					lookahead.Next(2);
					}
				else
					{  break;  }

				TryToSkipWhitespace(ref lookahead);
				endOfQualifier = lookahead;
				}

			if (mode == ParseMode.ParsePrototype)
				{
				if (prototypeParsingType == PrototypeParsingType.Type)
					{
					if (endOfQualifier > iterator)
						{  iterator.SetPrototypeParsingTypeBetween(endOfQualifier, PrototypeParsingType.TypeQualifier);  }

					endOfQualifier.SetPrototypeParsingTypeBetween(endOfIdentifier, PrototypeParsingType.Type);
					}
				else
					{  iterator.SetPrototypeParsingTypeBetween(endOfIdentifier, prototypeParsingType);  }
				}
			else if (mode == ParseMode.ParseClassPrototype)
				{  iterator.SetClassPrototypeParsingTypeBetween(endOfIdentifier, ClassPrototypeParsingType.Name);  }

			iterator = endOfIdentifier;
			return true;
			}


		/* Function: TryToSkipUnqualifiedIdentifier
		 *
		 * Tries to move the iterator past a single unqualified identifier, which means only "A" in "A.B.C".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipUnqualifiedIdentifier (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			TokenIterator lookahead = iterator;

			if (lookahead.Character == '@')
				{  lookahead.Next();  }

			if (lookahead.FundamentalType == FundamentalType.Text)
				{
				if (lookahead.Character >= '0' && lookahead.Character <= '9')
					{  return false;  }
				}
			else if (lookahead.FundamentalType == FundamentalType.Symbol)
				{
				if (lookahead.Character != '_')
					{  return false;  }
				}
			else
				{  return false;  }

			iterator = lookahead;

			do
				{  iterator.Next();  }
			while (iterator.FundamentalType == FundamentalType.Text || iterator.Character == '_');

			return true;
			}


		/* Function: TryToSkipTemplateSignature
		 *
		 * Tries to move the iterator past a template signature, such as "<int>" in "List<int>".  It can handle nested templates.  If isType is
		 * false, it will move past the signature in template declarations, such as "<X, in Y>" in "class Template<X, in Y>".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- If isType is true, it will mark tokens with these types, including in nested templates:
		 *				- <PrototypeParsingType.OpeningTypeSuffix>
		 *				- <PrototypeParsingType.ClosingTypeSuffix>
		 *				- <PrototypeParsingType.Type>
		 *				- <PrototypeParsingType.TypeQualifier>
		 *				- <PrototypeParsingType.TypeModifier>
		 *			- If isType is false, it will mark everything with <PrototypeParsingType.NameSuffix_PartOfType>.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- All tokens will be marked with <ClassPrototypeParsingType.TemplateSuffix>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipTemplateSignature (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly, bool isType = true)
			{
			if (iterator.Character != '<')
				{  return false;  }

			TokenIterator lookahead = iterator;

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			while (lookahead.IsInBounds && lookahead.Character != '>')
				{
				for (;;)
					{
					if (isType)
						{
						if (TryToSkipType(ref lookahead, mode) == false)
							{
							ResetTokensBetween(iterator, lookahead, mode);
							return false;
							}
						}
					else // not a type
						{
						// In interfaces and delegates there is an additional in/out modifier that can be applied to each one.
						if (lookahead.MatchesToken("in") ||
							lookahead.MatchesToken("out"))
							{
							lookahead.Next();
							TryToSkipWhitespace(ref lookahead);
							}

						if (TryToSkipUnqualifiedIdentifier(ref lookahead, mode) == false)
							{
							ResetTokensBetween(iterator, lookahead, mode);
							return false;
							}
						}

					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == ',')
						{
						lookahead.Next();
						TryToSkipWhitespace(ref lookahead);
						}
					else
						{  break;  }
					}
				}

			if (lookahead.Character == '>')
				{
				if (mode == ParseMode.ParsePrototype)
					{
					if (isType)
						{
						iterator.PrototypeParsingType = PrototypeParsingType.OpeningTypeModifier;
						lookahead.PrototypeParsingType = PrototypeParsingType.ClosingTypeModifier;
						lookahead.Next();
						}
					else
						{
						iterator.PrototypeParsingType = PrototypeParsingType.OpeningParamModifier;
						lookahead.PrototypeParsingType = PrototypeParsingType.ClosingParamModifier;
						lookahead.Next();
						}
					}
				else if (mode == ParseMode.ParseClassPrototype)
					{
					lookahead.Next();
					iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.TemplateSuffix);
					}
				else
					{  lookahead.Next();  }

				iterator = lookahead;
				return true;
				}
			else
				{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
				}
			}


		/* Function: TryToSkipWhereClause
		 *
		 * Tries to move the iterator past a where clause, such as "where struct, new()".  This only covers a single where clause, so you
		 * may have to call this in a loop to get them all.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- It will be marked with <PrototypeParsingType.StartOfPostPrototypeLine> and <PrototypeParsingType.PostPrototypeLine>.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- It will be marked with <ClassPrototypeParsing.StartOfPostPrototypeLine> and <ClassPrototypeParsingType.PostPrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipWhereClause (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.MatchesToken("where") == false)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			if (TryToSkipUnqualifiedIdentifier(ref lookahead) == false)
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);

			if (lookahead.Character != ':')
				{  return false;  }

			lookahead.Next();
			TokenIterator endOfClause = lookahead;
			TryToSkipWhitespace(ref lookahead);

			for (;;)
				{
				if (lookahead.MatchesToken("struct") ||
					lookahead.MatchesToken("notnull") ||
					lookahead.MatchesToken("unmanaged") ||
					lookahead.MatchesToken("default"))
					{
					lookahead.Next();
					endOfClause = lookahead;

					TryToSkipWhitespace(ref lookahead);
					}
				else if (lookahead.MatchesToken("class"))
					{
					lookahead.Next();
					endOfClause = lookahead;

					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == '?')
						{
						lookahead.Next();
						endOfClause = lookahead;

						TryToSkipWhitespace(ref lookahead);
						}
					}
				else if (lookahead.MatchesToken("new"))
					{
					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character != '(')
						{  return false;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character != ')')
						{  return false;  }

					lookahead.Next();
					endOfClause = lookahead;

					TryToSkipWhitespace(ref lookahead);
					}
				else if (TryToSkipType(ref lookahead))
					{
					endOfClause = lookahead;
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{  return false;  }

				if (lookahead.Character == ',')
					{
					lookahead.Next();
					endOfClause = lookahead;
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{  break;  }
				}

			if (mode == ParseMode.ParsePrototype)
				{
				iterator.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;
				}
			else if (mode == ParseMode.ParseClassPrototype)
				{
				iterator.SetClassPrototypeParsingTypeBetween(endOfClause, ClassPrototypeParsingType.PostPrototypeLine);
				iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPostPrototypeLine;
				}

			iterator = endOfClause;
			return true;
			}


		/* Function: TryToSkipAttributes
		 *
		 * Tries to move the iterator past a group of attributes which may be separated by whitespace.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.TypeModifier>,
		 *			  <PrototypeParsingType.ParamModifier>, or <PrototypeParsingType.StartOfPrototypeSection>.  It will actually set the brackets
		 *			  to the opening and closing types.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark the first one with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and the rest with <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttributes (ref TokenIterator iterator, AttributeTarget type = AttributeTarget.Any,
														   ParseMode mode = ParseMode.IterateOnly,
														   PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier)
			{
			if (TryToSkipAttribute(ref iterator, type, mode, prototypeParsingType) == false)
				{  return false;  }

			for (;;)
				{
				TokenIterator lookahead = iterator;
				TryToSkipWhitespace(ref lookahead, mode);

				if (TryToSkipAttribute(ref lookahead, type, mode, prototypeParsingType) == true)
					{  iterator = lookahead;  }
				else
					{  break;  }
				}

			return true;
			}


		/* Function: TryToSkipAttribute
		 *
		 * Tries to move the iterator past a single attribute.  Note that there may be more than one attribute in a row, so use <TryToSkipAttributes()>
		 * if you need to move past all of them.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- Set prototypeParsingType to the type you would like them to be marked as, such as <PrototypeParsingType.TypeModifier>,
		 *			  <PrototypeParsingType.ParamModifier>, or <PrototypeParsingType.StartOfPrototypeSection>.  It will actually set the brackets
		 *			  to the opening and closing types.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark the first one with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and the rest with <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipAttribute (ref TokenIterator iterator, AttributeTarget type = AttributeTarget.Any,
														 ParseMode mode = ParseMode.IterateOnly,
														 PrototypeParsingType prototypeParsingType = PrototypeParsingType.TypeModifier)
			{
			if (iterator.Character != '[')
				{  return false;  }

			if (type != AttributeTarget.Any)
				{
				TokenIterator lookahead = iterator;
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.MatchesToken("assembly") || lookahead.MatchesToken("module"))
					{
					if (type == AttributeTarget.LocalOnly)
						{  return false;  }
					}
				else
					{
					if (type == AttributeTarget.GlobalOnly)
						{  return false;  }
					}
				}

			TokenIterator startOfAttribute = iterator;

			iterator.Next();
			GenericSkipUntilAfter(ref iterator, ']');

			if (mode == ParseMode.SyntaxHighlight)
				{  startOfAttribute.SetSyntaxHighlightingTypeBetween(iterator, SyntaxHighlightingType.Metadata);  }
			else if (mode == ParseMode.ParsePrototype)
				{
				if (prototypeParsingType == PrototypeParsingType.StartOfPrototypeSection ||
					prototypeParsingType == PrototypeParsingType.TypeModifier ||
					prototypeParsingType == PrototypeParsingType.OpeningTypeModifier ||
					prototypeParsingType == PrototypeParsingType.ParamModifier ||
					prototypeParsingType == PrototypeParsingType.OpeningParamModifier)
					{
					PrototypeParsingType openingType, closingType;

					if (prototypeParsingType == PrototypeParsingType.StartOfPrototypeSection)
						{
						openingType = PrototypeParsingType.StartOfPrototypeSection;
						closingType = PrototypeParsingType.EndOfPrototypeSection;
						}
					else if (prototypeParsingType == PrototypeParsingType.TypeModifier ||
							   prototypeParsingType == PrototypeParsingType.OpeningTypeModifier)
						{
						openingType = PrototypeParsingType.OpeningTypeModifier;
						closingType = PrototypeParsingType.ClosingTypeModifier;
						}
					else // ParamModifier, OpeningParamModifier
						{
						openingType = PrototypeParsingType.OpeningParamModifier;
						closingType = PrototypeParsingType.ClosingParamModifier;
						}

					startOfAttribute.PrototypeParsingType = openingType;

					TokenIterator lookbehind = iterator;
					lookbehind.Previous();

					if (lookbehind.Character == ']')
						{  lookbehind.PrototypeParsingType = closingType;  }
					}
				else
					{  startOfAttribute.SetPrototypeParsingTypeBetween(iterator, prototypeParsingType);  }
				}
			else if (mode == ParseMode.ParseClassPrototype)
				{
				startOfAttribute.SetClassPrototypeParsingTypeBetween(iterator, ClassPrototypeParsingType.PrePrototypeLine);
				startOfAttribute.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;
				}

			return true;
			}


		/* Function: TryToSkipModifiers
		 *
		 * Attempts to skip one or more modifiers such as "public" or "static".
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- All modifiers will be set to <PrototypeParsingType.TypeModifier>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipModifiers (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			AccessLevel ignore;
			return TryToSkipModifiers(ref iterator, out ignore, mode);
			}


		/* Function: TryToSkipModifiers
		 *
		 * Attempts to skip one or more modifiers such as "public" or "static".  If they contained access modifiers it will return it, or <AccessLevel.Unknown>
		 * if not.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- All modifiers will be set to <PrototypeParsingType.TypeModifier>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipModifiers (ref TokenIterator iterator, out AccessLevel accessLevel, ParseMode mode = ParseMode.IterateOnly)
			{
			accessLevel = AccessLevel.Unknown;
			TokenIterator lookahead = iterator;
			bool foundAttributes = false;

			while (lookahead.IsInBounds)
				{
				if (lookahead.MatchesToken("public"))
					{  accessLevel = AccessLevel.Public;  }

				else if (lookahead.MatchesToken("private"))
					{
					if (accessLevel == AccessLevel.Protected)
						{  accessLevel = AccessLevel.PrivateProtected;  }
					else
						{  accessLevel = AccessLevel.Private;  }
					}

				else if (lookahead.MatchesToken("protected"))
					{
					if (accessLevel == AccessLevel.Internal)
						{  accessLevel = AccessLevel.ProtectedInternal;  }
					else if (accessLevel == AccessLevel.Private)
						{  accessLevel = AccessLevel.PrivateProtected;  }
					else
						{  accessLevel = AccessLevel.Protected;  }
					}

				else if (lookahead.MatchesToken("internal"))
					{
					if (accessLevel == AccessLevel.Protected)
						{  accessLevel = AccessLevel.ProtectedInternal;  }
					else
						{  accessLevel = AccessLevel.Internal;  }
					}

				else if (lookahead.MatchesAnyToken(NonAccessModifiers) == -1)
					{  break;  }

				foundAttributes = true;

				if (mode == ParseMode.SyntaxHighlight)
					{  lookahead.SyntaxHighlightingType = SyntaxHighlightingType.Keyword;  }
				else if (mode == ParseMode.ParsePrototype)
					{  lookahead.PrototypeParsingType = PrototypeParsingType.TypeModifier;  }

				lookahead.Next();
				iterator = lookahead;

				TryToSkipWhitespace(ref lookahead, mode);
				}

			return foundAttributes;
			}



		// Group: Base Parsing Functions
		// __________________________________________________________________________


		/* Function: GenericSkip
		 *
		 * Advances the iterator one place through general code.
		 *
		 * - If the position is on a string, it will skip it completely.
		 * - If the position is on an opening brace, parenthesis, or bracket it will skip until the past the closing symbol.
		 *	  It can optionally do this for angle brackets but won't by default.
		 * - If the position is on whitespace, comments, or a preprocessing directives it will skip it completely.
		 * - Otherwise it skips one token.
		 */
		protected void GenericSkip (ref TokenIterator iterator, bool angleBracketsAsBlocks = false)
			{
			if (iterator.Character == '(')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, ')');
				}
			else if (iterator.Character == '[')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, ']');
				}
			else if (iterator.Character == '{')
				{
				iterator.Next();
				GenericSkipUntilAfter(ref iterator, '}');
				}
			else if (iterator.Character == '<')
				{
				iterator.Next();

				if (angleBracketsAsBlocks)
					{  GenericSkipUntilAfter(ref iterator, '>', true);  }
				}

			else if (TryToSkipString(ref iterator) ||
					  TryToSkipWhitespace(ref iterator) ||
					  TryToSkipPreprocessingDirective(ref iterator))
					  // Attributes are covered by the opening bracket
				{  }

			else
				{  iterator.Next();  }
			}


		/* Function: GenericSkipUntilAfter
		 * Advances the iterator via <GenericSkip()> until a specific symbol is reached and passed.
		 */
		protected void GenericSkipUntilAfter (ref TokenIterator iterator, char symbol, bool angleBracketsAsBlocks = false)
			{
			while (iterator.IsInBounds)
				{
				if (iterator.Character == symbol)
					{
					iterator.Next();
					break;
					}
				else
					{  GenericSkip(ref iterator, angleBracketsAsBlocks);  }
				}
			}


		/* Function: SkipRestOfStatement
		 * Advances the iterator via <GenericSkip()> until after the end of the current statement, which is defined as a semicolon or
		 * a brace group.  Of course, either of those appearing inside parenthesis, a nested brace group, etc. don't count.
		 */
		protected void SkipRestOfStatement (ref TokenIterator iterator, bool angleBracketsAsBlocks = false)
			{
			while (iterator.IsInBounds)
				{
				if (iterator.Character == ';')
					{
					iterator.Next();
					break;
					}
				else if (iterator.Character == '{')
					{
					iterator.Next();
					GenericSkipUntilAfter(ref iterator, '}', angleBracketsAsBlocks);
					break;
					}
				else
					{  GenericSkip(ref iterator, angleBracketsAsBlocks);  }
				}
			}


		/* Function: TryToSkipPreprocessingDirective
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipPreprocessingDirective (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '#')
				{  return false;  }

			// It's safe to use a lookbehind here because only actual whitespace may precede the hash character.  Comments are not allowed.
			TokenIterator lookbehind = iterator;
			lookbehind.Previous();
			lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator);

			if (lookbehind.IsInBounds && lookbehind.FundamentalType != FundamentalType.LineBreak)
				{  return false;  }

			TokenIterator startOfDirective = iterator;

			// Technically only the enumerated preprocessing keywords are valid here, but we'll be tolerant and accept anything in the format.
			// Line comments (and only line comments) are allowed after directives.
			do
				{  iterator.Next();  }
			while (iterator.IsInBounds &&
					 iterator.FundamentalType != FundamentalType.LineBreak &&
					 iterator.MatchesAcrossTokens("//") == false);

			if (mode == ParseMode.SyntaxHighlight)
				{  startOfDirective.SetSyntaxHighlightingTypeBetween(iterator, SyntaxHighlightingType.PreprocessingDirective);  }

			return true;
			}


		/* Function: TryToSkipWhitespace
		 *
		 * Includes comments.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipWhitespace (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			int originalTokenIndex = iterator.TokenIndex;

			for (;;)
				{
				if (iterator.FundamentalType == FundamentalType.Whitespace ||
					iterator.FundamentalType == FundamentalType.LineBreak)
					{  iterator.Next();  }

				else if (TryToSkipComment(ref iterator, mode))
					{  }

				else
					{  break;  }
				}

			return (iterator.TokenIndex != originalTokenIndex);
			}


		/* Function: TryToSkipString
		 *
		 * This covers string, $string, @string, $@string, and character constants.
		 *
		 * Supported Modes:
		 *
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		override protected bool TryToSkipString (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if (iterator.Character != '\'' && iterator.Character != '\"' &&
				iterator.Character != '@' && iterator.Character != '$')
				{  return false;  }

			TokenIterator lookahead = iterator;


			// String opening

			bool interpolated = false;
			bool literal = false;

			if (lookahead.MatchesAcrossTokens("$@") ||
				lookahead.MatchesAcrossTokens("@$"))
				{
				interpolated = true;
				literal = true;
				lookahead.Next(2);
				}
			else if (lookahead.Character == '@')
				{
				literal = true;
				lookahead.Next();
				}
			else if (lookahead.Character == '$')
				{
				interpolated = true;
				lookahead.Next();
				}

			if (lookahead.Character != '\"' && lookahead.Character != '\'')
				{  return false;  }

			char closingChar = lookahead.Character;
			lookahead.Next();


			// String body

			while (lookahead.IsInBounds)
				{
				if ( (literal && lookahead.MatchesAcrossTokens("\"\"")) ||
					 (!literal && lookahead.Character == '\\') ||
					 (interpolated && lookahead.MatchesAcrossTokens("{{")) )
					{
					// Two characters for "" and {{, two tokens for \x
					lookahead.Next(2);
					}

				else if (interpolated && lookahead.Character == '{')
					{
					lookahead.Next();
					GenericSkipUntilAfter(ref lookahead, '}');
					}

				else if (lookahead.Character == closingChar)
					{
					lookahead.Next();
					break;
					}

				else
					{  lookahead.Next();  }
				}


			if (lookahead.IsInBounds)
				{
				if (mode == ParseMode.SyntaxHighlight)
					{  iterator.SetSyntaxHighlightingTypeBetween(lookahead, SyntaxHighlightingType.String);  }

				iterator = lookahead;
				return true;
				}
			else
				{  return false;  }
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
		override protected bool TryToSkipNumber (ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
			{
			if ( ((iterator.Character >= '0' && iterator.Character <= '9') ||
				   iterator.Character == '-' || iterator.Character == '+' || iterator.Character == '.') == false)
				{  return false;  }

			TokenIterator lookahead = iterator;
			bool passedPeriod = false;
			bool lastCharWasE = false;
			bool isHex = false;
			bool isBinary = false;

			if (lookahead.Character == '-' || lookahead.Character == '+')
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
					isBinary = (secondChar == 'b' || secondChar == 'B');
					}

				lookahead.Next();

				while ( (lookahead.Character >= '0' && lookahead.Character <= '9') ||
						  (isHex && lookahead.Character >= 'a' && lookahead.Character <= 'f') ||
						  (isHex && lookahead.Character >= 'A' && lookahead.Character <= 'F') ||
						  lookahead.Character == '_')
					{  lookahead.Next();  }

				char lastChar = iterator.Tokenizer.RawText[ lookahead.RawTextIndex - 1 ];
				lastCharWasE = (lastChar == 'e' || lastChar == 'E');
				}
			else
				{  return false;  }

			// We're definitely on a number, so apply the position in case the later lookaheads fail.
			TokenIterator startOfNumber = iterator;
			iterator = lookahead;

			if (lookahead.Character == '.' && !passedPeriod && !isHex && !isBinary)
				{
				lookahead.Next();

				if (lookahead.Character >= '0' && lookahead.Character <= '9')
					{
					passedPeriod = true;

					do
						{  lookahead.Next();  }
					while ( (lookahead.Character >= '0' && lookahead.Character <= '9') ||
							   lookahead.Character == '_');

					iterator = lookahead;

					char lastChar = iterator.Tokenizer.RawText[ lookahead.RawTextIndex - 1 ];
					lastCharWasE = (lastChar == 'e' || lastChar == 'E');
					}
				else
					{  lookahead = iterator;  }
				}

			if (lastCharWasE && !isHex && !isBinary && (lookahead.Character == '-' || lookahead.Character == '+'))
				{
				lookahead.Next();

				if (lookahead.Character >= '0' && lookahead.Character <= '9')
					{
					do
						{  lookahead.Next();  }
					while ( (lookahead.Character >= '0' && lookahead.Character <= '9') || lookahead.Character == '_');

					iterator = lookahead;
					}
				else
					{  lookahead = iterator;  }
				}

			if (mode == ParseMode.SyntaxHighlight)
				{  startOfNumber.SetSyntaxHighlightingTypeBetween(iterator, SyntaxHighlightingType.Number);  }

			return true;
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: Keywords
		 */
		static protected StringSet Keywords = new StringSet (KeySettings.Literal, new string[] {

			// Listed in the C# reference's keywords section

			"abstract", "as", "base", "bool", "break",
			"byte", "case", "catch", "char", "checked",
			"class", "const", "continue", "decimal", "default",
			"delegate", "do", "double", "else", "enum",
			"event", "explicit", "extern", "false", "finally",
			"fixed", "float", "for", "foreach", "goto",
			"if", "implicit", "in", "int", "interface",
			"internal", "is", "lock", "long", "namespace",
			"new", "null", "object", "operator", "out",
			"override", "params", "private", "protected", "public",
			"readonly", "ref", "return", "sbyte", "sealed",
			"short", "sizeof", "stackalloc", "static", "string",
			"struct", "switch", "this", "throw", "true",
			"try", "typeof", "uint", "ulong", "unchecked",
			"unsafe", "ushort", "using", "virtual", "void",
			"volatile", "while",

			// Additional keywords found in the syntax reference

			"get", "set", "var", "alias", "partial", "dynamic", "yield", "where", "add", "remove", "value", "async", "await", "nameof",
			"when", "unmanaged", "notnull", "global", "with", "init", "record", "and", "or", "not", "nint", "nuint", "managed",

			// Additional keywords for LINQ

			"from", "let", "where", "join", "on", "equals", "into", "orderby", "ascending", "descending",
			"select", "group", "by"

			});

		/* var: NonAccessModifiers
		 * A list of all possible modifiers (such as "static") excluding the access properties (such as "public".)  Not every modifier can apply
		 * to every code element (such as "sealed" not being revelant for constants) but it is okay for the parser to be over tolerant.
		 */
		static protected string[] NonAccessModifiers = new string[] {
			"new", "abstract", "sealed", "static", "partial", "readonly", "volatile", "virtual", "override", "extern", "unsafe", "async", "ref", "fixed"
			};

		/* var: BuiltInTypes
		 */
		static protected StringSet BuiltInTypes = new StringSet (KeySettings.Literal, new string[] {

			"byte", "sbyte", "int", "uint", "short", "ushort", "long", "ulong", "float", "double", "decimal",
			"char", "string", "bool", "void", "object", "dynamic", "nint", "nuint", "delegate", "managed", "unmanaged"

			});
		}
	}
