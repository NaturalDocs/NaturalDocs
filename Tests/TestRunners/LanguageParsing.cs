/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.LanguageParsing
 * ____________________________________________________________________________
 *
 * Tests to make sure Natural Docs can correctly extract <Elements> from a file.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Symbols;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class LanguageParsing : TestRunner
		{

		public LanguageParsing ()
			: base (InputMode.CodeElements, EngineMode.InstanceOnly)
			{  	}

		protected override string RunTest (IList<Element> elements)
			{
			StringBuilder output = new StringBuilder();

			for (int i = 0; i < elements.Count; i++)
				{
				Element element = elements[i];
				ParentElement parentElement = null;

				int indent = 0;

				for (int j = i - 1; j >= 0; j--)
					{
					if (elements[j] is ParentElement)
						{
						if ( (elements[j] as ParentElement).Contains(element) )
							{
							indent += 2;

							if (parentElement == null)
								{  parentElement = (ParentElement)elements[j];  }
							}
						}
					}

				if (i > 0)
					{
					output.Append(' ', indent);
					output.AppendLine("---------------");
					}


				// Topic

				output.Append(' ', indent);
				if (element is ParentElement && (element as ParentElement).IsRootElement == true)
					{  output.AppendLine("[Root Element]");  }
				else if (element.Topic == null)
					{  output.AppendLine("(no topic)");  }
				else
					{
					output.Append( EngineInstance.CommentTypes.FromID(element.Topic.CommentTypeID).Name + ": " );

					if (element.Topic.Title == null)
						{  output.AppendLine("(untitled)");  }
					else
						{  output.AppendLine(element.Topic.Title);  }

					if (element.Topic.Symbol != null)
						{
						output.Append(' ', indent);
						output.AppendLine("Symbol: " + element.Topic.Symbol.FormatWithSeparator('.'));
						}

					if (element.Topic.Prototype != null)
						{
						output.Append(' ', indent);
						output.AppendLine("Prototype: " + element.Topic.Prototype);
						}
					}


				// Position

				if (parentElement != null)
					{
					output.Append(' ', indent);
					output.Append("(line " + element.LineNumber + ", char " + element.CharNumber);

					if (parentElement.IsRootElement == false)
						{
						output.Append(", child of ");

						if (parentElement.Topic != null && parentElement.Topic.Title != null)
							{  output.Append(parentElement.Topic.Title);  }
						else
							{  output.Append("line " + parentElement.LineNumber);  }
						}

					output.AppendLine(")");
					}


				// ParentElement properties

				if (element is ParentElement)
					{
					ParentElement elementAsParent = (ParentElement)element;

					if (elementAsParent.DefaultChildLanguageID != 0)
						{
						output.Append(' ', indent);
						output.AppendLine("- Child Language: " + EngineInstance.Languages.FromID(elementAsParent.DefaultChildLanguageID).Name);
						}
					if (elementAsParent.MaximumEffectiveChildAccessLevel != AccessLevel.Unknown)
						{
						output.Append(' ', indent);
						output.AppendLine("- Maximum Effective Child Access Level: " + elementAsParent.MaximumEffectiveChildAccessLevel);
						}
					if (elementAsParent.DefaultDeclaredChildAccessLevel != AccessLevel.Unknown)
						{
						output.Append(' ', indent);
						output.AppendLine("- Default Declared Child Access Level: " + elementAsParent.DefaultDeclaredChildAccessLevel);
						}
					if (elementAsParent.ChildContextStringSet)
						{
						output.Append(' ', indent);
						output.Append("- Child Scope: ");

						if (elementAsParent.ChildContextString.ScopeIsGlobal)
							{  output.AppendLine("(global)");  }
						else
							{  output.AppendLine(elementAsParent.ChildContextString.Scope.FormatWithSeparator('.'));  }

						var usingStatements = elementAsParent.ChildContextString.GetUsingStatements();

						if (usingStatements != null)
							{
							foreach (var usingStatement in usingStatements)
								{
								output.Append(' ', indent);
								output.Append("- Child Using Statement: ");

								if (usingStatement.Type == UsingString.UsingType.AddPrefix)
									{
									output.AppendLine("Add " + usingStatement.PrefixToAdd.FormatWithSeparator('.'));
									}
								else if (usingStatement.Type == UsingString.UsingType.ReplacePrefix)
									{
									output.AppendLine("Replace " + usingStatement.PrefixToRemove.FormatWithSeparator('.') +
																		 " with " + usingStatement.PrefixToAdd.FormatWithSeparator('.'));
									}
								else
									{
									output.AppendLine("Unknown using type " + usingStatement.Type);
									}
								}
							}
						}
					}
				}

			return output.ToString();
			}

		}
	}
