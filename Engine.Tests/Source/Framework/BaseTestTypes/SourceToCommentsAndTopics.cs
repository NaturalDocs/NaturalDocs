﻿/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.BaseTestTypes.SourceToCommentsAndTopics
 * ____________________________________________________________________________
 *
 * A base class for automated tests where sample source files are loaded from a folder and converted to
 * <PossibleDocumentationComments> and <Topics>.  The results of <OutputOf()> are saved to files and
 * compared to other files containing the expected result.
 *
 *	The benefit of this approach is that you never have to hand code the output.  You can run the tests without
 *	an expected output file, look over the actual output file, and if it's acceptable rename it to become the
 * expected output file.
 *
 * Deriving a Test Type:
 *
 *		- Derive a class in the TestTypes namespace.
 *
 *		- Define <OutputOf()> to convert some facet of the <Topics> and <PossibleDocumentationComments> to
 *		  string output.
 *
 *
 * Input and Output Files:
 *
 *		- All files in the test folder in the format "[Test Name] - Input.[extension]" will be tested when NUnit runs.
 *
 *		- A corresponding file "[Test Name] - Actual Output.txt" will be created for each one.
 *
 *		- If it matches the contents of the file "[Test Name] - Expected Output.txt", the test will pass.  If it doesn't,
 *		  that file doesn't exist, or an exception was thrown, the test will fail.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using NUnit.Framework;
using CodeClear.NaturalDocs.Engine.Comments;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.BaseTestTypes
	{
	public abstract class SourceToCommentsAndTopics
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Constructor: SourceToCommentsAndTopics
		 */
		public SourceToCommentsAndTopics ()
			{
			engineInstanceManager = null;
			}


		/* Function: OutputOf
		 *
		 * Override this function to generate the output for the passed data.  The output should be whatever you're
		 * testing, so if you want to test prototype detection, return the prototype.  You have to account for the
		 * possibility of there being more than one topic in an input file, or none at all.
		 *
		 * You do not need to worry about catching exceptions unless the test is supposed to trigger them.  Uncaught
		 * exceptions will be handled automatically and cause the test to fail.  If the exception was intended as part
		 * of correct operation then you must catch it to prevent this.
		 *
		 * This function should not return null or an empty string as part of a successful test.  Doing so will cause the
		 * test to fail.  If a test is supposed to generated no output, return a string such as "test successful" instead.
		 */
		public abstract string OutputOf (IList<PossibleDocumentationComment> comments, IList<Topic> topics);


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
							Language language = EngineInstance.Languages.FromFileExtension(test.InputFile.Extension);

							if (language == null)
								{  throw new Exception("Extension " + test.InputFile.Extension + " did not resolve to a language.");  }

							string code = System.IO.File.ReadAllText(test.InputFile);

							IList<PossibleDocumentationComment> comments = language.Parser.GetPossibleDocumentationComments(code);

							IList<Topic> topics;
							LinkSet classParentLinks;
							language.Parser.Parse(test.InputFile, -1, Engine.Delegates.NeverCancel, out topics, out classParentLinks);

							test.SetActualOutput( OutputOf(comments, topics) );
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
