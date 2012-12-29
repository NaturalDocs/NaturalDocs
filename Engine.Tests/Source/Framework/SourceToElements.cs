/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.Framework.SourceToElements
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

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using GregValure.NaturalDocs.Engine;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Tokenization;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Tests.Framework
	{
	public class SourceToElements
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Constructor: SourceToElements
		 */
		public SourceToElements ()
			{
			}


		/* Function: OutputOf
		 */
		public string OutputOf (List<Element> elements)
			{
			StringBuilder output = new StringBuilder();
			bool isFirst = true;

			foreach (var element in elements)
				{
				int indent = 0;
				ParentElement parent = element.Parent;

				while (parent != null)
					{
					indent += 2;
					parent = parent.Parent;
					}

				if (isFirst)
					{  isFirst = false;  }
				else
					{  
					output.Append(' ', indent);
					output.AppendLine("---------------");  
					}


				// Topic

				output.Append(' ', indent);
				if (element.Parent == null)
					{  output.AppendLine("[Root Element]");  }
				else if (element.Topic == null)
					{  output.AppendLine("(no topic)");  }
				else 
					{
					output.Append( Engine.Instance.TopicTypes.FromID(element.Topic.TopicTypeID).Name + ": " );

					if (element.Topic.Title == null)
						{  output.AppendLine("(untitled)");  }
					else
						{  output.AppendLine(element.Topic.Title);  }

					if (element.Topic.Prototype != null)
						{
						output.Append(' ', indent);
						output.AppendLine(element.Topic.Prototype);
						}
					}


				// Position

				if (element.Parent != null)
					{
					output.Append(' ', indent);
					output.Append("(line " + element.LineNumber + ", char " + element.CharNumber);
					
					// Check Parent.Parent because we don't want to add this if it's a child of the root element.
					if (element.Parent.Parent != null)
						{
						output.Append(", child of ");

						if (element.Parent.Topic != null && element.Parent.Topic.Title != null)
							{  output.Append(element.Parent.Topic.Title);  }
						else
							{  output.Append("line " + element.Parent.LineNumber);  }
						}

					output.AppendLine(")");
					}


				// ParentElement properties

				if (element is ParentElement)
					{
					ParentElement parentElement = (ParentElement)element;

					if (parentElement.DefaultChildLanguageID != 0)
						{
						output.Append(' ', indent);
						output.AppendLine("- Child Language: " + Engine.Instance.Languages.FromID(parentElement.DefaultChildLanguageID).Name);
						}
					if (parentElement.ParentAccessLevel != AccessLevel.Unknown)
						{
						output.Append(' ', indent);
						output.AppendLine("- Access Level: " + parentElement.ParentAccessLevel);
						}
					if (parentElement.DefaultChildAccessLevel != AccessLevel.Unknown)
						{
						output.Append(' ', indent);
						output.AppendLine("- Default Child Access Level: " + parentElement.DefaultChildAccessLevel);
						}
					if (parentElement.ChildContextStringSet)
						{
						output.Append(' ', indent);
						output.Append("- Child Scope: ");

						if (parentElement.ChildContextString.ScopeIsGlobal)
							{  output.AppendLine("(global)");  }
						else
							{  output.AppendLine(parentElement.ChildContextString.Scope.FormatWithSeparator('.'));  }

						var usingStatements = parentElement.ChildContextString.GetUsingStatements();

						if (usingStatements != null)
							{
							foreach (var usingStatement in usingStatements)
								{
								output.Append(' ', indent);
								output.AppendLine("- Using Statement: " + usingStatement.FormatWithSeparator('.'));
								}
							}
						}
					}
				}

			return output.ToString();
			}


		/* Function: TestFolder
		 * 
		 * Tests all the input files contained in this folder.
		 * 
		 * If the test data folder is relative it will take the executing assembly path, skip up until it finds "Components", move 
		 * into the "EngineTests\Test Data" subfolder, and then make the path relative to that.  This is because it assumes all 
		 * the Natural Docs components will be subfolders of a shared Components folder, and Visual Studio or any other IDE
		 * is running an executable found inside a component's subfolder.
		 */
		public void TestFolder (Path testFolder, Path projectConfigFolder = default(Path))
			{
			TestList allTests = new TestList();
			TestEngine.Start(testFolder, projectConfigFolder);

			try
				{
				// Build a test for each input file we find
				string[] files = System.IO.Directory.GetFiles(TestEngine.InputFolder);

				foreach (string file in files)
					{
					if (Test.IsInputFile(file))
						{
						Test test = Test.FromInputFile(file);

						try
							{
							Language language = Engine.Instance.Languages.FromExtension(test.InputFile.Extension);

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
				{  TestEngine.Dispose();  }


			if (allTests.Count == 0)
				{  Assert.Fail("There were no tests found in " + TestEngine.InputFolder);  }
			else if (allTests.Passed == false)
				{  Assert.Fail(allTests.BuildFailureMessage());  }
			}

		}

	}