/* 
 * Class: GregValure.NaturalDocs.Engine.Languages.Parsers.CSharp
 * ____________________________________________________________________________
 * 
 * Full language support parser for C#.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Tokenization;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Languages.Parsers
	{
	public class CSharp : Language
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
		public CSharp () : base ("C#")
			{
			Type = LanguageType.BasicSupport;  //xxx

			LineCommentStrings = new string[] { "//" };
			BlockCommentStringPairs = new string[] { "/*", "*/" };
			JavadocLineCommentStringPairs = new string[] { "/**", "*/" };
			XMLLineCommentStrings = new string[] { "///" };

			MemberOperator = ".";
			EnumValue = EnumValues.UnderParent;
			CaseSensitive = true;
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
		 *		TopicTypeID - Set.
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

			ParentElement rootElement = new ParentElement(0, 0, Element.Flags.InCode);
			rootElement.IsRootElement = true;
			rootElement.DefaultDeclaredChildAccessLevel = AccessLevel.Internal;
			rootElement.DefaultChildLanguageID = this.ID;
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

						  TryToGetNamespace(ref iterator, elements, scope) ||
						  TryToGetClass(ref iterator, elements, scope) ||
						  TryToGetFunction(ref iterator, elements, scope) ||
						  TryToGetVariable(ref iterator, elements, scope) ||
						  TryToGetProperty(ref iterator, elements, scope) ||
						  TryToGetConstructor(ref iterator, elements, scope) ||
						  TryToGetEnum(ref iterator, elements, scope) ||
						  TryToGetConversionOperator(ref iterator, elements, scope) ||

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



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToGetNamespace
		 * Attempts to retrieve a namespace element.  If it was successful it will move the iterator past it, add it and its children to the
		 * <Elements> list, and return true.  If it was not it will leave the iterator alone and return false;
		 */
		protected bool TryToGetNamespace (ref TokenIterator iterator, List<Element> elements, SymbolString scope)
			{
			// See [9] and [B.2.6]

			// Namespaces may not have attributes.  There may be global attributes above them in the file, but they do not apply to the 
			// namespace itself.  Namespaces embedded in other namespaces do not have attributes at all.

			if (iterator.MatchesToken("namespace") == false)
				{  return false;  }

			TokenIterator lookahead = iterator;
			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			string name = TryToGetIdentifier(ref lookahead);

			if (name == null)
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);

			if (lookahead.Character != '{')
				{  return false;  }

			lookahead.Next();


			// Create the element

			SymbolString symbol = scope + SymbolString.FromPlainText_ParenthesesAlreadyRemoved(name);

			ContextString childContext = new ContextString();
			childContext.Scope = symbol;

			ParentElement namespaceElement = new ParentElement(iterator, Element.Flags.InCode);
			namespaceElement.ChildContextString = childContext;
			namespaceElement.DefaultDeclaredChildAccessLevel = AccessLevel.Internal;

			// We don't create topics for namespaces.

			elements.Add(namespaceElement);


			iterator = lookahead;
			GetCodeElements(ref iterator, elements, symbol, '}');

			namespaceElement.EndingLineNumber = iterator.LineNumber;
			namespaceElement.EndingCharNumber = iterator.CharNumber;

			return true;
			}


		/* Function: TryToGetClass
		 * Attempts to retrieve a class, struct, or interface element.  If it was successful it will move the iterator past it, add it to the
		 * <Elements> list, and return true.  If it was not it will leave the iterator alone and return false.
		 */
		protected bool TryToGetClass (ref TokenIterator iterator, List<Element> elements, SymbolString scope)
			{
			// Classes - See [10] and [B.2.7]
			// Structs - See [11] and [B.2.8]
			// Interfaces - See [13] and [B.2.10]

			// While there are differences in the syntax of the three (classes have more possible modifiers, structs and interfaces can
			// only inherit interfaces, etc.) they are pretty small and for our purposes we can combine them into one parsing function.
			// It's okay to be over tolerant.

			TokenIterator lookahead = iterator;


			// Attributes
			
			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			AccessLevel accessLevel;

			// This covers "partial" as well, even though that's listed separately in the documentaton.
			if (TryToSkipModifiers(ref lookahead, out accessLevel))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword and name

			if (lookahead.MatchesToken("class") == false &&
				lookahead.MatchesToken("struct") == false &&
				lookahead.MatchesToken("interface") == false)
				{  return false;  }

			string keyword = lookahead.String;

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			string name = TryToGetIdentifier(ref lookahead);

			if (name == null)
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);


			// Template signature

			if (TryToSkipTemplateSignature(ref lookahead))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Base classes and interfaces

			if (lookahead.Character == ':')
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				for (;;)
					{
					TryToSkipIdentifier(ref lookahead);
					TryToSkipWhitespace(ref lookahead);

					if (TryToSkipTemplateSignature(ref lookahead))
						{  TryToSkipWhitespace(ref lookahead);  }

					if (lookahead.Character != ',')
						{  break;  }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				}


			// Constraint clauses

			if (lookahead.MatchesToken("where"))
				{
				// This is the last possible thing before the body, it can get somewhat elaborate, and we don't need to parse it for
				// anything so let's just plow through to the brace instead.
				lookahead.Next();

				while (lookahead.IsInBounds && lookahead.Character != '{')
					{  GenericSkip(ref lookahead, true);  }
				}


			// Start of body

			if (lookahead.Character != '{')
				{  return false;  }


			// Create element

			SymbolString symbol = scope + SymbolString.FromPlainText_ParenthesesAlreadyRemoved(name);

			ContextString childContext = new ContextString();
			childContext.Scope = symbol;

			ParentElement classElement = new ParentElement(iterator, Element.Flags.InCode);
			classElement.ChildContextString = childContext;
			classElement.MaximumEffectiveChildAccessLevel = accessLevel;

			if (keyword == "interface")
				{  classElement.DefaultDeclaredChildAccessLevel = AccessLevel.Public;  }
			else // "class" or "struct"
				{  classElement.DefaultDeclaredChildAccessLevel = AccessLevel.Private;  }

			int topicTypeID = Engine.Instance.TopicTypes.IDFromKeyword(keyword);

			if (topicTypeID != 0)
				{
				Topic classTopic = new Topic();
				classTopic.Title = symbol.FormatWithSeparator('.');  // so the title is fully resolved
				classTopic.Symbol = symbol;
				classTopic.Prototype = NormalizePrototype( iterator.Tokenizer.TextBetween(iterator, lookahead) );
				classTopic.TopicTypeID = topicTypeID;
				classTopic.DeclaredAccessLevel = accessLevel;
				classTopic.CodeLineNumber = iterator.LineNumber;

				classElement.Topic = classTopic;
				}

			elements.Add(classElement);


			// Body

			iterator = lookahead;
			iterator.Next();
			GetCodeElements(ref iterator, elements, symbol, '}');

			classElement.EndingLineNumber = iterator.LineNumber;
			classElement.EndingCharNumber = iterator.CharNumber;

			return true;
			}


		/* Function: TryToGetFunction
		 * Attempts to retrieve a function, delegate, or operator other than a conversion or indexer.  If successful it will add an <Element> 
		 * to the list, move the iterator past it, and return true.  If unsuccessful it will leave the iterator alone and return false.
		 */
		protected bool TryToGetFunction (ref TokenIterator iterator, List<Element> elements, SymbolString scope)
			{
			// Functions (methods) - See [10.6] and [B.2.7]
			// Delegates - See [15] and [B.2.12]
			// Operators - See [10.10] and [B.2.7]

			TokenIterator lookahead = iterator;


			// Attributes
			
			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			AccessLevel accessLevel;

			// This covers "partial" as well, even though that's listed separately in the documentaton.
			if (TryToSkipModifiers(ref lookahead, out accessLevel))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword

			string keyword = null;

			if (lookahead.MatchesToken("delegate"))
				{  keyword = "delegate";  }
			else if (lookahead.MatchesToken("const") ||
					  lookahead.MatchesToken("event") ||
					  lookahead.MatchesToken("implicit") ||
					  lookahead.MatchesToken("explicit") ||
					  lookahead.MatchesToken("enum"))
				{  return false;  }

			if (keyword == null)
				{  keyword = "function";  }
			else
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Return type

			if (lookahead.MatchesToken("void"))
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}
			else if (TryToSkipType(ref lookahead) == false)
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);


			// Name

			string name = TryToGetIdentifier(ref lookahead);

			if (name == null)
				{  return false;  }

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

				TryToSkipWhitespace(ref lookahead);
				}


			// Template signature

			if (TryToSkipTemplateSignature(ref lookahead))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Parameters

			if (lookahead.Character != '(')
				{  return false;  }

			lookahead.Next();
			GenericSkipUntilAfter(ref lookahead, ')');
			TryToSkipWhitespace(ref lookahead);


			// Constraint clauses

			if (lookahead.MatchesToken("where"))
				{
				// This is the last possible thing before the body, it can get somewhat elaborate, and we don't need to parse it for
				// anything so let's just plow through to the brace instead.
				lookahead.Next();

				while (lookahead.IsInBounds && 
						 lookahead.Character != '{' &&
						 lookahead.Character != ';')
					{  GenericSkip(ref lookahead, true);  }
				}


			// Create element

			int topicTypeID = Engine.Instance.TopicTypes.IDFromKeyword(keyword);

			if (topicTypeID != 0)
				{
				Topic functionTopic = new Topic();
				functionTopic.Title = name;
				functionTopic.Symbol = scope + SymbolString.FromPlainText_ParenthesesAlreadyRemoved(name);
				functionTopic.Prototype = NormalizePrototype( iterator.Tokenizer.TextBetween(iterator, lookahead) );
				functionTopic.TopicTypeID = topicTypeID;
				functionTopic.DeclaredAccessLevel = accessLevel;
				functionTopic.CodeLineNumber = iterator.LineNumber;

				Element functionElement = new Element(iterator, Element.Flags.InCode);
				functionElement.Topic = functionTopic;

				elements.Add(functionElement);
				}


			// Body

			if (lookahead.Character == '{')
				{
				lookahead.Next();
				GenericSkipUntilAfter(ref lookahead, '}');
				}
			else if (lookahead.Character == ';')
				{  lookahead.Next();  }
			else
				{  return false;  }

			iterator = lookahead;
			return true;
			}


		/* Function: TryToGetConstructor
		 * Attempts to retrieve a constructor or destructor.  If successful it will add an <Element> to the list, move the iterator past
		 * it, and return true.  If unsuccessful it will leave the iterator alone and return false.
		 */
		protected bool TryToGetConstructor (ref TokenIterator iterator, List<Element> elements, SymbolString scope)
			{
			// Constructors - See [10.11] and [B.2.7]
			// Destructors - See [10.13] and [B.2.7]

			TokenIterator lookahead = iterator;


			// Attributes
			
			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			AccessLevel accessLevel;

			if (TryToSkipModifiers(ref lookahead, out accessLevel))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword

			string keyword;

			if (lookahead.Character == '~')
				{
				keyword = "destructor";
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}
			else
				{  keyword = "constructor";  }


			// Name

			string name = TryToGetIdentifier(ref lookahead);

			if (name == null ||
				name == "delegate" ||
				name == "const" ||
				name == "event" ||
				name == "implicit" ||
				name == "explicit" ||
				name == "enum" ||
				name == "void")
				{  return false;  }

			if (keyword == "destructor")
				{  name = "~" + name;  }

			TryToSkipWhitespace(ref lookahead);


			// Parameters

			if (lookahead.Character != '(')
				{  return false;  }

			lookahead.Next();
			GenericSkipUntilAfter(ref lookahead, ')');

			TokenIterator endOfPrototype = lookahead;

			TryToSkipWhitespace(ref lookahead);


			// Constructor initializer

			if (lookahead.Character == ':' && keyword == "constructor")
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.MatchesToken("base") == false &&
					lookahead.MatchesToken("this") == false)
					{  return false;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character != '(')
					{  return false;  }

				lookahead.Next();
				GenericSkipUntilAfter(ref lookahead, ')');
				TryToSkipWhitespace(ref lookahead);
				}

			if (lookahead.Character != '{' &&
				lookahead.Character != ';')
				{  return false;  }


			// Create element

			int topicTypeID = Engine.Instance.TopicTypes.IDFromKeyword(keyword);

			if (topicTypeID != 0)
				{
				Topic functionTopic = new Topic();
				functionTopic.Title = name;
				functionTopic.Symbol = scope + SymbolString.FromPlainText_ParenthesesAlreadyRemoved(name);
				functionTopic.Prototype = NormalizePrototype( iterator.Tokenizer.TextBetween(iterator, endOfPrototype) );
				functionTopic.TopicTypeID = topicTypeID;
				functionTopic.DeclaredAccessLevel = accessLevel;
				functionTopic.CodeLineNumber = iterator.LineNumber;

				Element functionElement = new Element(iterator, Element.Flags.InCode);
				functionElement.Topic = functionTopic;

				elements.Add(functionElement);
				}


			// Body

			if (lookahead.Character == '{')
				{
				lookahead.Next();
				GenericSkipUntilAfter(ref lookahead, '}');
				}
			else if (lookahead.Character == ';')
				{  lookahead.Next();  }
			else
				{  return false;  }

			iterator = lookahead;
			return true;
			}


		/* Function: TryToGetConversionOperator
		 * Attempts to retrieve a conversion operator.  If successful it will add an <Element> to the list, move the iterator past it, and 
		 * return true.  If unsuccessful it will leave the iterator alone and return false.
		 */
		protected bool TryToGetConversionOperator (ref TokenIterator iterator, List<Element> elements, SymbolString scope)
			{
			// Operators - See [10.10] and [B.2.7]

			TokenIterator lookahead = iterator;


			// Attributes
			
			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			AccessLevel accessLevel;

			// This covers "partial" as well, even though that's listed separately in the documentaton.
			if (TryToSkipModifiers(ref lookahead, out accessLevel))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Name and type

			if (lookahead.MatchesToken("implicit") == false &&
				lookahead.MatchesToken("explicit") == false)
				{  return false;  }

			System.Text.StringBuilder name = new System.Text.StringBuilder(lookahead.String);

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			if (lookahead.MatchesToken("operator") == false)
				{  return false;  }

			name.Append(" operator ");
			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);

			TokenIterator startOfType = lookahead;

			if (TryToSkipType(ref lookahead) == false)
				{  return false;  }

			iterator.Tokenizer.AppendTextBetweenTo(startOfType, lookahead, name);
			TryToSkipWhitespace(ref lookahead);


			// Parameters

			if (lookahead.Character != '(')
				{  return false;  }

			lookahead.Next();
			GenericSkipUntilAfter(ref lookahead, ')');
			TryToSkipWhitespace(ref lookahead);


			// Create element

			int topicTypeID = Engine.Instance.TopicTypes.IDFromKeyword("operator");

			if (topicTypeID != 0)
				{
				Topic operatorTopic = new Topic();
				operatorTopic.Title = name.ToString();
				operatorTopic.Symbol = scope + SymbolString.FromPlainText_ParenthesesAlreadyRemoved(operatorTopic.Title);
				operatorTopic.Prototype = NormalizePrototype( iterator.Tokenizer.TextBetween(iterator, lookahead) );
				operatorTopic.TopicTypeID = topicTypeID;
				operatorTopic.DeclaredAccessLevel = accessLevel;
				operatorTopic.CodeLineNumber = iterator.LineNumber;

				Element operatorElement = new Element(iterator, Element.Flags.InCode);
				operatorElement.Topic = operatorTopic;

				elements.Add(operatorElement);
				}


			// Body

			if (lookahead.Character == '{')
				{
				lookahead.Next();
				GenericSkipUntilAfter(ref lookahead, '}');
				}
			else if (lookahead.Character == ';')
				{  lookahead.Next();  }
			else
				{  return false;  }

			iterator = lookahead;
			return true;
			}


		/* Function: TryToGetVariable
		 * Attempts to retrieve a variable, constant, or event declared like a variable.  If successful it will add one or more <Elements> to
		 * the list, move the iterator past it, and return true.  If unsuccessful it will leave the iterator alone and return false.
		 */
		protected bool TryToGetVariable (ref TokenIterator iterator, List<Element> elements, SymbolString scope)
			{
			// Variables (fields) - See [10.5] and [B.2.7]
			// Constants - See [10.4] and [B.2.7]
			// Events - See [10.8] and [B.2.7]

			TokenIterator lookahead = iterator;


			// Attributes
			
			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			AccessLevel accessLevel;

			if (TryToSkipModifiers(ref lookahead, out accessLevel))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword

			string keyword = null;

			if (lookahead.MatchesToken("const"))
				{  keyword = "constant";  }
			else if (lookahead.MatchesToken("event"))
				{  keyword = "event";  }
			else if (lookahead.MatchesToken("void") ||
					  lookahead.MatchesToken("implicit") ||
					  lookahead.MatchesToken("explicit") ||
					  lookahead.MatchesToken("enum") ||
					  lookahead.MatchesToken("delegate"))
				{  return false;  }

			if (keyword == null)
				{  keyword = "variable";  }
			else
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Type

			if (TryToSkipType(ref lookahead) == false)
				{  return false;  }

			TokenIterator endOfType = lookahead;
			TryToSkipWhitespace(ref lookahead);


			// Name

			string name = TryToGetIdentifier(ref lookahead);

			if (name == null)
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);

			if (lookahead.Character != ';' &&
				lookahead.Character != ',' &&
				lookahead.Character != '=')
				{  return false;  }


			// Create element

			int topicTypeID = Engine.Instance.TopicTypes.IDFromKeyword(keyword);

			if (topicTypeID != 0)
				{
				Topic variableTopic = new Topic();
				variableTopic.Title = name;
				variableTopic.Symbol = scope + SymbolString.FromPlainText_ParenthesesAlreadyRemoved(name);
				variableTopic.Prototype = NormalizePrototype( iterator.Tokenizer.TextBetween(iterator, lookahead) );
				variableTopic.TopicTypeID = topicTypeID;
				variableTopic.DeclaredAccessLevel = accessLevel;
				variableTopic.CodeLineNumber = iterator.LineNumber;

				Element variableElement = new Element(iterator, Element.Flags.InCode);
				variableElement.Topic = variableTopic;

				elements.Add(variableElement);
				}

			
			// Multiple declarations

			while (lookahead.IsInBounds && lookahead.Character != ';')
				{
				if (lookahead.Character == '=')
					{
					lookahead.Next();
					while (lookahead.IsInBounds && 
							 lookahead.Character != ',' && 
							 lookahead.Character != ';')
						{  GenericSkip(ref lookahead);  }
					}
				else if (lookahead.Character == ',')
					{
					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);

					TokenIterator startOfNewName = lookahead;

					string newName = TryToGetIdentifier(ref lookahead);
					if (newName == null)
						{  break;  }

					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character != ';' && 
						lookahead.Character != ',' && 
						lookahead.Character != '=')
						{  break;  }

					if (topicTypeID != 0)
						{
						Topic newVariableTopic = new Topic();
						newVariableTopic.Title = newName;
						newVariableTopic.Symbol = scope + SymbolString.FromPlainText_ParenthesesAlreadyRemoved(newName);
						newVariableTopic.Prototype = NormalizePrototype( iterator.Tokenizer.TextBetween(iterator, endOfType) + " " + newName );
						newVariableTopic.TopicTypeID = topicTypeID;
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

			lookahead.Next();
			iterator = lookahead;
			return true;
			}


		/* Function: TryToGetProperty
		 * Attempts to retrieve a property, indexer, or event declared like a property.  If successful it will add it as an <Element> to the
		 * list, move the iterator past it, and return true.  If unsuccessful it will leave the iterator alone and return false.
		 */
		protected bool TryToGetProperty (ref TokenIterator iterator, List<Element> elements, SymbolString scope)
			{
			// Properties - See [10.7] and [B.2.7]
			// Indexers - See [10.9] and [B.2.7]
			// Events - See [10.8] and [B.2.7]

			TokenIterator lookahead = iterator;


			// Attributes
			
			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			AccessLevel accessLevel;

			if (TryToSkipModifiers(ref lookahead, out accessLevel))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword

			string keyword = null;

			if (lookahead.MatchesToken("event"))
				{  keyword = "event";  }
			else if (lookahead.MatchesToken("const") ||
					  lookahead.MatchesToken("void") ||
					  lookahead.MatchesToken("implicit") ||
					  lookahead.MatchesToken("explicit") ||
					  lookahead.MatchesToken("enum") ||
					  lookahead.MatchesToken("delegate"))
				{  return false;  }

			if (keyword == null)
				{  keyword = "property";  }
			else
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			// Type

			if (TryToSkipType(ref lookahead) == false)
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);


			// Name

			string name = TryToGetIdentifier(ref lookahead);

			if (name == null)
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);


			// Indexer

			if (name == "this" ||
				name.EndsWith(".this"))  // It may be InterfaceName.this[]
				{
				keyword = "operator";
				name += "[]";

				if (lookahead.Character != '[')
					{  return false;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (TryToSkipParameters(ref lookahead, ']') == false)
					{  return false;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}


			if (lookahead.Character != '{')
				{  return false;  }


			// Build prototype

			System.Text.StringBuilder prototype = new System.Text.StringBuilder();

			iterator.Tokenizer.AppendTextBetweenTo(iterator, lookahead, prototype);
			prototype.Append(" { ");

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

				if ( (keyword == "property" && lookahead.MatchesToken("get") == false && lookahead.MatchesToken("set") == false) ||
					 (keyword == "operator" && lookahead.MatchesToken("get") == false && lookahead.MatchesToken("set") == false) ||
					 (keyword == "event" && lookahead.MatchesToken("add") == false && lookahead.MatchesToken("remove") == false) )
					{  return false;  }

				lookahead.Next();

				if (!firstAccessor)
					{  prototype.Append("; ");  }
				else
					{  firstAccessor = false;  }

				iterator.Tokenizer.AppendTextBetweenTo(startOfAccessor, lookahead, prototype);

				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character == ';')
					{  lookahead.Next();  }
				else if (lookahead.Character == '{')
					{
					lookahead.Next();
					GenericSkipUntilAfter(ref lookahead, '}');
					}

				TryToSkipWhitespace(ref lookahead);
				}

			lookahead.Next();
			prototype.Append(" }");


			// Create element

			int topicTypeID = Engine.Instance.TopicTypes.IDFromKeyword(keyword);

			if (topicTypeID != 0)
				{
				Topic propertyTopic = new Topic();
				propertyTopic.Title = name;
				propertyTopic.Symbol = scope + SymbolString.FromPlainText_ParenthesesAlreadyRemoved(name);
				propertyTopic.Prototype = NormalizePrototype(prototype.ToString());
				propertyTopic.TopicTypeID = topicTypeID;
				propertyTopic.DeclaredAccessLevel = accessLevel;
				propertyTopic.CodeLineNumber = iterator.LineNumber;

				Element propertyElement = new Element(iterator, Element.Flags.InCode);
				propertyElement.Topic = propertyTopic;

				elements.Add(propertyElement);
				}


			iterator = lookahead;
			return true;
			}


		/* Function: TryToGetEnum
		 * Attempts to retrieve an enum.  If successful it will add an <Element> to the list, move the iterator past it, and return true.
		 * If unsuccessful it will leave the iterator alone and return false.
		 */
		protected bool TryToGetEnum (ref TokenIterator iterator, List<Element> elements, SymbolString scope)
			{
			// See [14] and [B.2.11]

			TokenIterator lookahead = iterator;


			// Attributes
			
			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Modifiers

			AccessLevel accessLevel;

			if (TryToSkipModifiers(ref lookahead, out accessLevel))
				{  TryToSkipWhitespace(ref lookahead);  }


			// Keyword

			if (lookahead.MatchesToken("enum") == false)
				{  return false;  }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Name

			string name = TryToGetIdentifier(ref lookahead);

			if (name == null)
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);


			// Type

			if (lookahead.Character == ':')
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (TryToSkipType(ref lookahead) == false)
					{  return false;  }

				TryToSkipWhitespace(ref lookahead);
				}

			if (lookahead.Character != '{')
				{  return false;  }


			// Create element

			int topicTypeID = Engine.Instance.TopicTypes.IDFromKeyword("enum");

			if (topicTypeID != 0)
				{
				Topic enumTopic = new Topic();
				enumTopic.Title = name;
				enumTopic.Symbol = scope + SymbolString.FromPlainText_ParenthesesAlreadyRemoved(name);
				enumTopic.Prototype = NormalizePrototype( iterator.Tokenizer.TextBetween(iterator, lookahead) );
				enumTopic.TopicTypeID = topicTypeID;
				enumTopic.DeclaredAccessLevel = accessLevel;
				enumTopic.CodeLineNumber = iterator.LineNumber;

				Element enumElement = new Element(iterator, Element.Flags.InCode);
				enumElement.Topic = enumTopic;

				elements.Add(enumElement);
				}


			//  Body

			lookahead.Next();
			GenericSkipUntilAfter(ref lookahead, '}');

			iterator = lookahead;
			return true;
			}



		// Group: Component Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToSkipParameters
		 * Tries to move the iterator past a comma-separated list of parameters ending at the closing symbol, which defaults to
		 * a closing parenthesis.
		 */
		protected bool TryToSkipParameters (ref TokenIterator iterator, char closingSymbol = ')')
			{
			TokenIterator lookahead = iterator;

			for (;;)
				{
				if (TryToSkipParameter(ref lookahead, closingSymbol) == false)
					{  return false;  }

				if (lookahead.Character == closingSymbol)
					{
					iterator = lookahead;
					return true;
					}
				else if (lookahead.Character == ',')
					{
					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{  return false;  }
				}
			}


		/* Function: TryToSkipParameter
		 * Tries to move the iterator past a parameter, such as "int x" or "IList<int> y = null".  The parameter ends at a comma or
		 * the closing symbol, which defaults to a closing parenthesis.
		 */
		protected bool TryToSkipParameter (ref TokenIterator iterator, char closingSymbol = ')')
			{
			TokenIterator lookahead = iterator;

			if (TryToSkipAttributes(ref lookahead, AttributeTarget.LocalOnly) == true)
				{  TryToSkipWhitespace(ref lookahead);  }

			if (lookahead.MatchesToken("ref") ||
				lookahead.MatchesToken("out") ||
				lookahead.MatchesToken("params") ||
				lookahead.MatchesToken("this"))
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}

			if (TryToSkipType(ref lookahead) == false)
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);

			if (TryToSkipIdentifier(ref lookahead) == false)
				{  return false;  }

			TryToSkipWhitespace(ref lookahead);

			if (lookahead.Character == '=')
				{
				lookahead.Next();

				while (lookahead.IsInBounds &&
						 lookahead.Character != ',' &&
						 lookahead.Character != closingSymbol)
					{  GenericSkip(ref lookahead);  }
				}

			if (lookahead.Character == ',' || 
				lookahead.Character == closingSymbol)
				{
				iterator = lookahead;
				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToSkipType
		 * Tries to move the iterator past a type, such as "int", "System.Collections.Generic.List<int>", or "int[]".
		 */
		protected bool TryToSkipType (ref TokenIterator iterator)
			{
			TokenIterator lookahead = iterator;

			if (TryToSkipIdentifier(ref lookahead) == false)
				{  return false;  }

			iterator = lookahead;
			TryToSkipWhitespace(ref lookahead);

			if (lookahead.Character == '?')
				{
				lookahead.Next();
				iterator = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}

			if (TryToSkipTemplateSignature(ref lookahead))
				{
				iterator = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}

			if (lookahead.Character == '*')
				{
				lookahead.Next();
				iterator = lookahead;
				TryToSkipWhitespace(ref lookahead);
				}

			while (lookahead.Character == '[')
				{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				while (lookahead.Character == ',')
					{
					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
					}

				if (lookahead.Character == ']')
					{
					lookahead.Next();
					iterator = lookahead;
					TryToSkipWhitespace(ref lookahead);
					}
				else
					{  break;  }
				}

			return true;
			}


		/* Function: TryToGetIdentifier
		 * Attempts to an identifier, such as "X.Y.Z".  If successful it will return it as a string and move the iterator past it.  If not it will return
		 * null and leave the iterator alone.  Use <TryToGetUnqualifiedIdentifier()> if you only want to retrieve a single segment.
		 */
		protected string TryToGetIdentifier (ref TokenIterator iterator)
			{
			TokenIterator start = iterator;

			if (TryToSkipIdentifier(ref iterator))
				{  return iterator.Tokenizer.TextBetween(start, iterator);  }
			else
				{  return null;  }
			}


		/* Function: TryToSkipIdentifier
		 * Tries to move the iterator past a qualified identifier, such as "X.Y.Z".  Use <SkipUnqualifiedIdentifier()> if you only want
		 * to skip a single segment.
		 */
		protected bool TryToSkipIdentifier (ref TokenIterator iterator)
			{
			TokenIterator lookahead = iterator;
			TokenIterator endOfIdentifier;

			for (;;)
				{
				if (TryToSkipUnqualifiedIdentifier(ref lookahead) == false)
					{  return false;  }

				endOfIdentifier = lookahead;
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character != '.')
					{  break;  }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);
				}

			iterator = endOfIdentifier;
			return true;
			}


		/* Function: TryToGetUnqualifiedIdentifier
		 * Attempts to retrieve a single unqualified identifier, which means only "X" would be returned for "X.Y.Z".  If successful it will return
		 * it as a string and move the iterator past it.  If not it will return null and leave the iterator alone.
		 */
		protected string TryToGetUnqualifiedIdentifier (ref TokenIterator iterator)
			{
			TokenIterator start = iterator;

			if (TryToSkipUnqualifiedIdentifier(ref iterator))
				{  return iterator.Tokenizer.TextBetween(start, iterator);  }
			else
				{  return null;  }
			}


		/* Function: TryToSkipUnqualifiedIdentifier
		 * Tries to move the iterator past a single unqualified identifier, which means only "X" in "X.Y.Z".
		 */
		protected bool TryToSkipUnqualifiedIdentifier (ref TokenIterator iterator)
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
		 * Tries to move the iterator past a template signature, such as "<int>" in "List<int>".  It can handle nested templates.
		 */
		protected bool TryToSkipTemplateSignature (ref TokenIterator iterator)
			{
			if (iterator.Character != '<')
				{  return false;  }

			// If this is ever implemented to actually parse the signature, note that in interfaces and delegates there is an additional in/out 
			// keyword that can optionally be applied to each one.  See [13.1.3]

			iterator.Next();
			GenericSkipUntilAfter(ref iterator, '>', true);
			
			return true;
			}


		/* Function: TryToGetAttributes
		 * Tries to retrieve a group of attributes which may be separated by whitespace.  If successful it will return the attributes as a
		 * list of strings and move the iterator past it.  If not it will return null and leave the iterator alone.
		 */
		protected List<string> TryToGetAttributes (ref TokenIterator iterator, AttributeTarget type = AttributeTarget.Any)
			{
			string attribute = TryToGetAttribute(ref iterator, type);

			if (attribute == null)
				{  return null;  }

			List<string> attributes = new List<string>();
			attributes.Add(attribute);

			for (;;)
				{
				TokenIterator lookahead = iterator;
				TryToSkipWhitespace(ref iterator);
				attribute = TryToGetAttribute(ref lookahead, type);

				if (attribute != null)
					{  
					attributes.Add(attribute);
					iterator = lookahead;  
					}
				else
					{  break;  }
				}

			return attributes;
			}


		/* Function: TryToSkipAttributes
		 * Tries to move the iterator past a group of attributes which may be separated by whitespace.
		 */
		protected bool TryToSkipAttributes (ref TokenIterator iterator, AttributeTarget type = AttributeTarget.Any)
			{
			if (TryToSkipAttribute(ref iterator, type) == false)
				{  return false;  }

			for (;;)
				{
				TokenIterator lookahead = iterator;
				TryToSkipWhitespace(ref lookahead);

				if (TryToSkipAttribute(ref lookahead, type) == true)
					{  iterator = lookahead;  }
				else
					{  break;  }
				}

			return true;
			}


		/* Function: TryToGetAttribute
		 * Tries to retrieve a single attribute.  If successful, it will move the iterator past the attribute and return it as a string.  If not, it will return
		 * null and leave the iterator alone.  Note that there may be more than one attribute in a row, so use <TryToGetAttributes()> if you need to
		 * get all of them.
		 */
		protected string TryToGetAttribute (ref TokenIterator iterator, AttributeTarget type = AttributeTarget.Any)
			{
			TokenIterator startOfAttribute = iterator;

			if (TryToSkipAttribute(ref iterator, type))
				{  return iterator.Tokenizer.TextBetween(startOfAttribute, iterator);  }
			else
				{  return null;  }
			}


		/* Function: TryToSkipAttribute
		 * Tries to move the iterator past a single attribute.  Note that there may be more than one attribute in a row, so use <TryToSkipAttributes()>
		 * if you need to move past all of them.
		 */
		protected bool TryToSkipAttribute (ref TokenIterator iterator, AttributeTarget type = AttributeTarget.Any)
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

			iterator.Next();
			GenericSkipUntilAfter(ref iterator, ']');

			return true;
			}


		/* Function: TryToSkipModifiers
		 * Attempts to skip one or more modifiers such as "public" or "static".
		 */
		protected bool TryToSkipModifiers (ref TokenIterator iterator)
			{
			AccessLevel ignore;
			return TryToSkipModifiers(ref iterator, out ignore);
			}


		/* Function: TryToSkipModifiers
		 * Attempts to skip one or more modifiers such as "public" or "static".  If they contained access modifiers it will return it, or <AccessLevel.Unknown>
		 * if not.
		 */
		protected bool TryToSkipModifiers (ref TokenIterator iterator, out AccessLevel accessLevel)
			{
			accessLevel = AccessLevel.Unknown;
			TokenIterator lookahead = iterator;
			bool foundAttributes = false;

			while (iterator.IsInBounds)
				{
				if (lookahead.MatchesToken("public"))
					{  accessLevel = AccessLevel.Public;  }

				else if (lookahead.MatchesToken("private"))
					{  accessLevel = AccessLevel.Private;  }

				else if (lookahead.MatchesToken("protected"))
					{  
					if (accessLevel == AccessLevel.Internal)
						{  accessLevel = AccessLevel.ProtectedInternal;  }
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

				lookahead.Next();
				iterator = lookahead;

				TryToSkipWhitespace(ref lookahead);
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
		 */
		protected bool TryToSkipPreprocessingDirective (ref TokenIterator iterator)
			{
			if (iterator.Character != '#')
				{  return false;  }

			// It's safe to use a lookbehind here because only actual whitespace may precede the hash character.  Comments are not allowed.
			TokenIterator lookbehind = iterator;
			lookbehind.Previous();
			lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator);

			if (lookbehind.IsInBounds && lookbehind.FundamentalType != FundamentalType.LineBreak)
				{  return false;  }

			// Technically only the enumerated preprocessing keywords are valid here, but we'll be tolerant and accept anything in the format. 
			do
				{  iterator.Next();  }
			while (iterator.IsInBounds && iterator.FundamentalType != FundamentalType.LineBreak);

			iterator.Next();
			return true;
			}


		/* Function: TryToSkipWhitespace
		 * Includes comments.
		 */
		protected bool TryToSkipWhitespace (ref TokenIterator iterator)
			{
			int originalTokenIndex = iterator.TokenIndex;

			for (;;)
				{
				if (iterator.FundamentalType == FundamentalType.Whitespace ||
					iterator.FundamentalType == FundamentalType.LineBreak)
					{  iterator.Next();  }

				else if (TryToSkipComment(ref iterator))
					{  }

				else
					{  break;  }
				}

			return (iterator.TokenIndex != originalTokenIndex);
			}


		/* Function: TryToSkipComment
		 */
		new protected bool TryToSkipComment (ref TokenIterator iterator)
			{
			return (TryToSkipLineComment(ref iterator) ||
					  TryToSkipBlockComment(ref iterator));
			}


		/* Function: TryToSkipLineComment
		 */
		new protected bool TryToSkipLineComment (ref TokenIterator iterator)
			{
			if (iterator.MatchesAcrossTokens("//"))
				{
				iterator.NextByCharacters(2);

				while (iterator.IsInBounds &&
						 iterator.FundamentalType != FundamentalType.LineBreak)
					{  iterator.Next();  }

				if (iterator.FundamentalType == FundamentalType.LineBreak)
					{  iterator.Next();  }

				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToSkipBlockComment
		 */
		new protected bool TryToSkipBlockComment (ref TokenIterator iterator)
			{
			if (iterator.MatchesAcrossTokens("/*"))
				{
				iterator.NextByCharacters(2);

				while (iterator.IsInBounds &&
						 iterator.MatchesAcrossTokens("*/") == false)
					{  iterator.Next();  }

				if (iterator.MatchesAcrossTokens("*/"))
					{  iterator.NextByCharacters(2);  }

				return true;
				}
			else
				{  return false;  }
			}


		/* Function: TryToSkipString
		 * This covers string, @string, and character constants.
		 */
		new protected bool TryToSkipString (ref TokenIterator iterator)
			{
			if (iterator.Character == '\"' || iterator.Character == '\'')
				{
				char closingChar = iterator.Character;
				iterator.Next();

				while (iterator.IsInBounds && iterator.Character != closingChar)
					{
					if (iterator.Character == '\\')
						{  iterator.Next(2);  }
					else
						{  iterator.Next();  }
					}

				iterator.Next();
				return true;
				}

			else if (iterator.MatchesAcrossTokens("@\""))
				{
				iterator.NextByCharacters(2);

				while (iterator.IsInBounds)
					{
					if (iterator.MatchesAcrossTokens("\"\""))
						{  iterator.NextByCharacters(2);  }
					else if (iterator.Character == '\"')
						{  break;  }
					else
						{  iterator.Next();  }
					}

				iterator.Next();
				return true;
				}

			else
				{  return false;  }
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: Keywords
		 */
		static protected StringSet Keywords = new StringSet(false, false, new string[] {

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
			"readonly", "ref	", "return", "sbyte", "sealed",
			"short	", "sizeof", "stackalloc", "static", "string",
			"struct", "switch", "this", "throw", "true",
			"try", "typeof", "uint", "ulong", "unchecked",
			"unsafe", "ushort", "using", "virtual", "void",
			"volatile", "while",

			// Additional keywords found in the syntax reference

			"get", "set", "var", "alias", "partial", "dynamic", "yield", "where", "add", "remove", "value",

			// Additional keywords for LINQ

			"from", "let", "where", "join", "on", "equals", "into", "orderby", "ascending", "descending",
			"select", "group", "by"

			});

		/* var: NonAccessModifiers
		 * A list of all possible modifiers (such as "static") excluding the access properties (such as "public".)  Not every modifier can apply
		 * to every code element (such as "sealed" not being revelant for constants) but it is okay for the parser to be over tolerant.
		 */
		static protected string[] NonAccessModifiers = new string[] {
			"new", "abstract", "sealed", "static", "partial", "readonly", "volatile", "virtual", "override", "extern"
			};

		}
	}