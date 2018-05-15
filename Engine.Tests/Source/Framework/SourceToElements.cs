/* 
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.SourceToElements
 * ____________________________________________________________________________
 * 
 * A base class for automated tests where sample source files are loaded from a folder, converted to <Elements>, 
 * and the properties of those elements are saved to files and compared to other files containing the expected result.
 * 
 * The benefit of this approach is that you never have to hand code the output.  You can run the tests without
 *	an expected output file, look over the actual output file, and if it's acceptable rename it to become the
 *	expected output file.
 *
 * Usage:
 * 
 *		- Derive a class that has the [TestFixture] attribute.
 *		- Create a function with the [Test] attribute that calls <TestFolder()>, pointing it to the input files.
 *		- All files in the test folder in the format "[Test Name] - Input.[extension]" will be tested when NUnit runs.
 *		- A corresponding file "[Test Name] - Actual Output.txt" will be created for each one.
 *		- If it matches the contents of the file "[Test Name] - Expected Output.txt", the test will pass.  If it doesn't,
 *		  that file doesn't exist, or an exception was thrown, the test will fail.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework
	{
	public class SourceToElements
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Constructor: SourceToElements
		 */
		public SourceToElements ()
			{
			engineInstanceManager = null;
			}


		/* Function: OutputOf
		 */
		public string OutputOf (List<Element> elements)
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
						output.AppendLine(element.Topic.Symbol.FormatWithSeparator('.'));
						}

					if (element.Topic.Prototype != null)
						{
						output.Append(' ', indent);
						output.AppendLine(element.Topic.Prototype);
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


		/* Function: TestFolder
		 * Tests all the input files contained in this folder.  See <EngineInstanceManager.Start()> for how relative paths are handled.
		 */
		public void TestFolder (Path testDataFolder, Path projectConfigFolder = default(Path))
			{
			TestList allTests = new TestList();
			
			engineInstanceManager = new EngineInstanceManager();
			engineInstanceManager.Start(testDataFolder, projectConfigFolder);

			// Store this so we can still use it for error messages after the engine is disposed of.
			Path inputFolder = engineInstanceManager.InputFolder;

			try
				{
				// Build a test for each input file we find
				string[] files = System.IO.Directory.GetFiles(inputFolder);

				foreach (string file in files)
					{
					if (Test.IsInputFile(file))
						{
						Test test = Test.FromInputFile(file);

						try
							{
							Language language = EngineInstance.Languages.FromExtension(test.InputFile.Extension);

							if (language == null)
								{  throw new Exception("Extension " + test.InputFile.Extension + " did not resolve to a language.");  }

							string code = System.IO.File.ReadAllText(test.InputFile);
							Tokenizer tokenizedCode = new Tokenizer(code);
							List<Element> codeElements = language.GetCodeElements(tokenizedCode);

							if (codeElements == null)
								{  throw new Exception("GetCodeElements() returned null.");  }

							test.SetActualOutput( OutputOf(codeElements) );  
							}
						catch (Exception e)
							{  test.TestException = e;  }

						test.Run();
						allTests.Add(test);
						}
					}
				}

			finally
				{  
				engineInstanceManager.Dispose();  
				engineInstanceManager = null;
				}


			if (allTests.Count == 0)
				{  Assert.Fail("There were no tests found in " + inputFolder);  }
			else if (allTests.Passed == false)
				{  Assert.Fail(allTests.BuildFailureMessage());  }
			}


		// Group: Properties
		// __________________________________________________________________________

		public NaturalDocs.Engine.Instance EngineInstance
			{
			get
				{
				if (engineInstanceManager != null)
					{  return engineInstanceManager.EngineInstance;  }
				else
					{  return null;  }
				}
			}


		// Group: Variables
		// __________________________________________________________________________

		protected EngineInstanceManager engineInstanceManager;

		}
	}