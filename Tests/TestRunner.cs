/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunner
 * ____________________________________________________________________________
 *
 * A base class for all test runners, which are responsible for executing the <Tests> in each <TestFolder>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Comments;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Tests
	{
	public abstract class TestRunner
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: InputMode
		 *
		 * How the test runner will format the input for the derived test to interpret.
		 *
		 * Values:
		 *
		 *		String - The test runner will read the input file contents as a flat string and send it to <RunTest(string)>.
		 *
		 *		Lines - The test runner will read the input file contents as an array of strings, one per line, and send them to
		 *				   <RunTest(string[])>.
		 *
		 *		Topics - The test runner will parse the input file as a source file and send the resulting <Topics> to
		 *					<RunTest(Topic)>.  Requires at least <EngineMode.InstanceOnly>.
		 *
		 *		CommentsAndTopics - The test runner will parse the input file as a source file and send the resulting
		 *										 <PossibleDocumentationComments> and <Topics> to
		 *										 <RunTest(PossibleDocumentationComment, Topic)>.  Requires at least
		 *										 <EngineMode.InstanceOnly>.
		 *
		 *		CodeElements - The test runner will parse the input file as a source file and send the resulting code
		 *								<Elements> to <RunTest(Element)>.  Requires at least <EngineMode.InstanceOnly>.
		 *
		 *		HTML - The test runner will read the contents of the input file's generated HTML output file and send it to
		 *				   <RunTest(string)>.  Requires at least <EngineMode.InstanceAndGeneratedDocs>.
		 *
		 */
		protected enum InputMode
			{  String, Lines, Topics, CommentsAndTopics, CodeElements, HTML  }


		/* Enum: EngineMode
		 *
		 * Whether the test runner needs a functioning <Engine.Instance> and also whether it needs fully built HTML documentation
		 * prior to running.  Test execution will be faster if you only use the minimum level necessary.
		 *
		 * Values:
		 *
		 *		NotNeeded - The test runner doesn't need an <Engine.Instance> at all.  This is good for tests that can be run against
		 *						   internal classes that don't rely on it, such as <Engine.IDObjects.NumberSet>.
		 *
		 *		InstanceOnly - The test runner needs an active <Engine.Instance> but it doesn't need to build a complete set of HTML
		 *							  documentation beforehand.  This is good for tests that rely on the settings in <Comments.txt> and
		 *							  <Languages.txt>, like prototype parsing.  HTML tests can also use this if the HTML can be generated on
		 *							  demand by directly calling internal functions.
		 *
		 *		InstanceAndGeneratedDocs - The test runner needs an active <Engine.Instance> and also to build a complete set of
		 *												   HTML documentation prior to running.  This is also good for tests where you want to keep
		 *												   the generated HTML so it can be opened and inspected manually.
		 */
		protected enum EngineMode
			{  NotNeeded, InstanceOnly, InstanceAndGeneratedDocs  }



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: TestRunner
		 * Constructor.  Derived classes need to specify the <InputMode> and <EngineMode> needed for the test.  Since it's intrinsic
		 * to how the tests function they should always be set by the derived class and not exposed as an option for general use.
		 */
		protected TestRunner (InputMode inputMode, EngineMode engineMode = EngineMode.NotNeeded)
			{
			this.testFolder = null;
			this.engineManager = null;
			this.engineMode = engineMode;
			this.inputMode = inputMode;
			}


		/* Function: RunAll
		 *
		 * Finds and runs all tests in the passed <TestFolder>.  <TestFolder.Tests> will be filled in (unless there was a failure prior to
		 * finding the tests, such as during engine initialization) and the folder will be marked as passed if all of them pass individually.
		 * If any fail the folder will be marked as failed and a failure message provided.  It will return whether the folder passed or
		 * failed as well.
		 *
		 * This function executes all the <overridable stages>, which is what derived classes should use.  This function is not itself
		 * overridable.
		 */
		public bool RunAll (TestFolder folder)
			{
			this.testFolder = folder;

			// It may have already failed due to folder configuration issues
			if (testFolder.Failed)
				{  return false;  }

			bool hasErrors = false;


			// Start the engine

			if (engineMode != EngineMode.NotNeeded)
				{
				try
					{
					if (!StartEngine())
						{
						testFolder.MarkAsFailed("Could not start the Natural Docs engine.");
						hasErrors = true;
						}
					}
				catch (Exception e)
					{
					testFolder.MarkAsFailed("Could not start the Natural Docs engine: " + e.Message + " (" + e.GetType().Name + ")");
					hasErrors = true;
					}
				}


			// Build the documentation, if necessary

			if (!hasErrors &&
				engineMode == EngineMode.InstanceAndGeneratedDocs)
				{
				try
					{
					if (!BuildDocumentation())
						{
						testFolder.MarkAsFailed("Could not build the HTML documentation.");
						hasErrors = true;
						}
					}
				catch (Exception e)
					{
					testFolder.MarkAsFailed("Could not build the HTML documentation: " + e.Message + " (" + e.GetType().Name + ")");
					hasErrors = true;
					}
				}


			// Look for tests

			List<Test> tests = null;

			if (!hasErrors)
				{
				try
					{
					if (FindTests(out tests) &&
						tests != null &&
						tests.Count > 0)
						{
						testFolder.Tests = tests;
						}
					else
						{
						testFolder.MarkAsFailed("Could not find tests in " + testFolder.Path);
						hasErrors = true;
						}
					}
				catch (Exception e)
					{
					testFolder.MarkAsFailed("Could not find tests in " + testFolder.Path + ": " + e.Message + " (" + e.GetType().Name + ")");
					hasErrors = true;
					}
				}


			// Run individual tests

			if (!hasErrors &&
				tests != null)
				{
				foreach (var test in tests)
					{
					try
						{
						// Individual tests probably can't fail ahead of time like the folder can, but let's check anyway to code defensively
						if (!test.Failed)
							{  RunTest(test);  }
						}
					catch (Exception e)
						{
						test.MarkAsFailed(Test.FailureReasons.ExceptionThrown);
						testFolder.MarkAsFailed("Exception thrown when running test \"" + test.Name + "\": " + e.Message + " (" + e.GetType().Name + ")");
						hasErrors = true;
						}
					}

				// Note that the passed/failed state of the folder is independent of the passed/failed state of individual tests.  One does
				// not automatically change the other.
				if (!hasErrors && !testFolder.Failed)
					{
					if (testFolder.TestsPassed == testFolder.TestCount)
						{  testFolder.MarkAsPassed();  }
					else
						{
						testFolder.MarkAsFailed(testFolder.TestsFailed.ToString() + " of " + testFolder.TestCount.ToString() +
															" test" + (testFolder.TestCount != 1 ? "s" : "") + " failed.");
						}
					}
				}


			// Dispose of the engine, if necessary

			if (engineMode != EngineMode.NotNeeded)
				{
				try
					{
					DisposeOfEngine();
					engineManager = null;
					}
				catch (Exception e)
					{
					testFolder.MarkAsFailed("Could not dispose of the Natural Docs engine: " + e.Message + " (" + e.GetType().Name + ")");
					hasErrors = true;
					}
				}


			return testFolder.Passed;
			}



		// Group: Overridable Stages
		// __________________________________________________________________________


		/* Function: StartEngine
		 *
		 * Creates and starts an <Engine.Instance>.  This will only be called if the <EngineMode> requires it.  Returns whether it was
		 * successful.  If it was not, the <TestFolder> will be marked as failed and a message will be included in
		 * <TestFolder.FailureMessage>.
		 *
		 * Default Implementation:
		 *
		 *		The default implementation will create an <EngineManager> which creates, configures, and starts an <Engine.Instance>
		 *		using the settings in <TestFolder.Config>.
		 *
		 */
		protected virtual bool StartEngine ()
			{
			// If there are errors with the engine configuration they will be thrown as exceptions which RunAll() will handle
			engineManager = new EngineManager();
			engineManager.Start(testFolder);

			return true;
			}


		/* Function: BuildDocumentation
		 *
		 * Uses the engine to build a complete set of HTML documentation.  This function will only be called if the <EngineMode>
		 * requires it, otherwise it will be skipped.  Returns whether it was successful.  If it was not, the <TestFolder> will be marked
		 * as failed and a message will be included in <TestFolder.FailureMessage>.
		 *
		 * Default Implementation:
		 *
		 *		The default implementation will build the documentation using the <EngineManager> that was created by <StartEngine()>.
		 *
		 */
		protected virtual bool BuildDocumentation ()
			{
			// If there are errors they will be thrown as exceptions which RunAll() will handle
			engineManager.BuildDocumentation();
			return true;
			}


		/* Function: FindTests
		 *
		 * Goes through the <TestFolder> and creates a list of <Tests> from its contents.  Returns whether it was successful.  If
		 * it was not the <TestFolder> will be marked as failed and a message will be included in <TestFolder.FailureMessage>.  Not
		 * finding any tests counts as a failure, so it should not return an empty list and true.
		 *
		 * If indicated by <EngineMode>, an <Engine.Instance> will be created before this is called so that you can use configuration
		 * settings such as those in <Languages.txt> to help find and interpret tests.  If also indicated by <EngineMode>, a full set of
		 * HTML documentation will be built before this is called so that you can use those files as well.
		 *
		 * Default Implementation:
		 *
		 *		The default implementation creates a list of all the files in the <TestFolder> which match against <Test.IsInputFile()>.
		 *		It does not recurse into subfolders.
		 *
		 */
		protected virtual bool FindTests (out List<Test> tests)
			{
			tests = new List<Test>();

			string[] files = System.IO.Directory.GetFiles(testFolder.Path);

			foreach (AbsolutePath file in files)
				{
				if (Test.IsInputFile(file))
					{
					tests.Add( Test.CreateFromInputFile(file) );
					}
				}

			if (tests.Count == 0)
				{
				testFolder.MarkAsFailed("There were no tests found in " + testFolder.Path);

				tests = null;
				return false;
				}

			return true;
			}


		/* Function: RunTest
		 *
		 * Executes a single <Test> from the <TestFolder>, sets <Test.MarkAsPassed()> or <Test.MarkAsFailed()>, and returns whether
		 * it was successful.
		 *
		 * Default Implementation:
		 *
		 *		The default implementation reads the <InputMode> and handles the contents of <Test.InputFile> according to that, passing
		 *		it to one of the other RunTest() functions such as <RunTest(string)>.  This allows derived classes to override one of those
		 *		functions which is simpler than overriding this one.
		 *
		 *		The results are saved to <Test.ActualOutputFile>.  It then compares the result to the contents of <Test.ExpectedOutputFile>
		 *		and if it's different the test fails.  If an exception was thrown by the RunTest() function it will be saved to <Test.ActualOutputFile>
		 *		along with a stack trace.
		 *
		 */
		protected virtual bool RunTest (Test test)
			{
			string actualOutput = null;

			try
				{

				// String

				if (inputMode == InputMode.String)
					{
					string input = System.IO.File.ReadAllText(test.InputFile);
					actualOutput = RunTest(input);
					}


				// Lines

				else if (inputMode == InputMode.Lines)
					{
					string[] input = System.IO.File.ReadAllLines(test.InputFile);
					actualOutput = RunTest(input);
					}


				// Topics

				else if (inputMode == InputMode.Topics)
					{
					Language language = EngineInstance.Languages.FromFileExtension(test.InputFile.Extension);

					if (language == null)
						{  throw new Exception("Extension " + test.InputFile.Extension + " did not resolve to a language.");  }

					IList<Topic> topics;
					LinkSet classParentLinks;
					language.Parser.Parse(test.InputFile, -1, Engine.Delegates.NeverCancel, out topics, out classParentLinks);

					actualOutput = RunTest(topics);
					}


				// Comments and Topics

				else if (inputMode == InputMode.CommentsAndTopics)
					{
					Language language = EngineInstance.Languages.FromFileExtension(test.InputFile.Extension);

					if (language == null)
						{  throw new Exception("Extension " + test.InputFile.Extension + " did not resolve to a language.");  }

					string code = System.IO.File.ReadAllText(test.InputFile);

					IList<PossibleDocumentationComment> comments = language.Parser.GetPossibleDocumentationComments(code);

					IList<Topic> topics;
					LinkSet classParentLinks;
					language.Parser.Parse(test.InputFile, -1, Engine.Delegates.NeverCancel, out topics, out classParentLinks);

					actualOutput = RunTest(comments, topics);
					}


				// Code Elements

				else if (inputMode == InputMode.CodeElements)
					{
					Language language = EngineInstance.Languages.FromFileExtension(test.InputFile.Extension);

					if (language == null)
						{  throw new Exception("Extension " + test.InputFile.Extension + " did not resolve to a language.");  }

					string code = System.IO.File.ReadAllText(test.InputFile);
					Tokenizer tokenizedCode = new Tokenizer(code);
					List<Element> codeElements = language.Parser.GetCodeElements(tokenizedCode);

					if (codeElements == null)
						{  throw new Exception("GetCodeElements() returned null.");  }

					actualOutput = RunTest(codeElements);
					}


				// HTML

				else if (inputMode == InputMode.HTML)
					{
					var fileInfo = EngineInstance.Files.FromPath(test.InputFile);

					if (fileInfo == null)
						{  throw new Exception("Could not get file info of " + test.InputFile);  }

					var fileContext = new Engine.Output.HTML.Context(engineManager.HTMLBuilder, fileInfo.ID);

					Path htmlFile = fileContext.OutputFile;

					string html = System.IO.File.ReadAllText(htmlFile);

					actualOutput = RunTest(html);
					}


				else
					{  throw new NotImplementedException();  }
				}
			catch (Exception e)
				{
				actualOutput =
					"An exception was thrown while executing the test:\n" +
					"\n" +
					e.Message + "\n" +
					"(" + e.GetType().Name + ")\n" +
					"\n" +
					e.StackTrace + "\n";

				SaveIfDifferent(test.ActualOutputFile, actualOutput);

				test.MarkAsFailed(Test.FailureReasons.ExceptionThrown);
				return false;
				}

			SaveIfDifferent(test.ActualOutputFile, actualOutput);

			if (!System.IO.File.Exists(test.ExpectedOutputFile))
				{
				test.MarkAsFailed(Test.FailureReasons.ExpectedOutputMissing);
				return false;
				}

			string expectedOutput = System.IO.File.ReadAllText(test.ExpectedOutputFile);

			if (actualOutput.NormalizeLineBreaks() == expectedOutput.NormalizeLineBreaks())
				{
				test.MarkAsPassed();
				return true;
				}
			else
				{
				test.MarkAsFailed(Test.FailureReasons.OutputDoesntMatch);
				return false;
				}
			}


		/* Function: RunTest (string)
		 *
		 * Converts the test input to output and returns it.  When using <InputMode.String>, the input will be a string with the contents of
		 * <Test.InputFile>.  When using <InputMode.HTML>, the input will be the contents of the input file's generated HTML file.
		 *
		 * This is only relevant if you're using the default implementation of <RunTest(Test)> with <InputMode.String> or <InputMode.HTML>.
		 * It will not be called otherwise, unless your implementation of <RunTest(Test)> also calls it.
		 *
		 * Default Implementation:
		 *
		 *		The default implementation throws a NotImplementException because you need to define it if you're not overriding
		 *		<RunTest(Test)>.  We do this instead of making it abstract so that if you do override <RunTest(Test)> you're not forced
		 *		to define this as well.
		 *
		 */
		protected virtual string RunTest (string input)
			{
			throw new NotImplementedException();
			}


		/* Function: RunTest (string[])
		 *
		 * Converts the test input to output and returns it.  The input will be an array of strings with the contents of <Test.InputFile>,
		 * one line per string.
		 *
		 * This is only relevant if you're using the default implementation of <RunTest(Test)> with <InputMode.Lines>.  It will not be
		 * called otherwise, unless your implementation of <RunTest(Test)> also calls it.
		 *
		 * Default Implementation:
		 *
		 *		The default implementation throws a NotImplementException because you need to define it if you're not overriding
		 *		<RunTest(Test)>.  We do this instead of making it abstract so that if you do override <RunTest(Test)> you're not forced
		 *		to define this as well.
		 *
		 */
		protected virtual string RunTest (string[] input)
			{
			throw new NotImplementedException();
			}


		/* Function: RunTest (Topic)
		 *
		 * Generates output from the test input <Topics> and returns it.  The input files will be parsed as source files to generate the
		 * <Topics>.  The output will be whatever properties from them are relevant to the test.
		 *
		 * This function is only relevant if you're using the default implementation of <RunTest(Test)> with <InputMode.Topics>.  It will
		 * not be called otherwise, unless your implementation of <RunTest(Test)> also calls it.
		 *
		 * Default Implementation:
		 *
		 *		The default implementation throws a NotImplementException because you need to define it if you're not overriding
		 *		<RunTest(Test)>.  We do this instead of making it abstract so that if you do override <RunTest(Test)> you're not forced
		 *		to define this as well.
		 *
		 */
		protected virtual string RunTest (IList<Topic> topics)
			{
			throw new NotImplementedException();
			}


		/* Function: RunTest (PossibleDocumentationComment, Topic)
		 *
		 * Generates output from the test input <PossibleDocumentationComments> and <Topics> and returns it.  The input files will be
		 * parsed as source files to generate the <PossibleDocumentationComments> and <Topics>.  The output will be whatever properties
		 * from them are relevant to the test.
		 *
		 * This function is only relevant if you're using the default implementation of <RunTest(Test)> with <InputMode.CommentsAndTopics>.
		 * It will not be called otherwise, unless your implementation of <RunTest(Test)> also calls it.
		 *
		 * Default Implementation:
		 *
		 *		The default implementation throws a NotImplementException because you need to define it if you're not overriding
		 *		<RunTest(Test)>.  We do this instead of making it abstract so that if you do override <RunTest(Test)> you're not forced
		 *		to define this as well.
		 *
		 */
		protected virtual string RunTest (IList<PossibleDocumentationComment> comments, IList<Topic> topics)
			{
			throw new NotImplementedException();
			}


		/* Function: RunTest (Element)
		 *
		 * Generates output from the test input code <Elements> and returns it.  The input files will be parsed as source files to generate
		 * the code <Elements>.  The output will be whatever properties from them are relevant to the test.
		 *
		 * This function is only relevant if you're using the default implementation of <RunTest(Test)> with <InputMode.CodeElements>.  It
		 * will not be called otherwise, unless your implementation of <RunTest(Test)> also calls it.
		 *
		 * Default Implementation:
		 *
		 *		The default implementation throws a NotImplementException because you need to define it if you're not overriding
		 *		<RunTest(Test)>.  We do this instead of making it abstract so that if you do override <RunTest(Test)> you're not forced
		 *		to define this as well.
		 *
		 */
		protected virtual string RunTest (IList<Element> elements)
			{
			throw new NotImplementedException();
			}


		/* Function: DisposeOfEngine
		 *
		 * Disposes of the <Engine.Instance> previously created.
		 *
		 * Default Implementation:
		 *
		 *		The default implementation disposes of the <EngineManager> that was created by <StartEngine()>.
		 *
		 */
		protected virtual void DisposeOfEngine ()
			{
			if (engineManager != null)
				{
				engineManager.Dispose();
				engineManager = null;
				}
			}



		// Group: Support Functions
		// _________________________________________________________________________


		/* Function: SaveIfDifferent
		 * Replaces the contents of the passed file, but only if it's different.  This prevents the file timestamp from changing if
		 * the actual contents of the file hasn't.
		 */
		protected void SaveIfDifferent (AbsolutePath path, string contents)
			{
			string oldContents = null;

			try
				{  oldContents = System.IO.File.ReadAllText(path);  }
			catch
				{  }

			if (oldContents == null ||
				contents != oldContents)
				{
				System.IO.File.WriteAllText(path, contents);
				}
			}



		// Group: Properties
		// _________________________________________________________________________


		/* Property: EngineInstance
		 * The <Engine.Instance> managed by <EngineManager>, or null if it hasn't been created yet.  This is a convenience
		 * property to access it directly.
		 */
		public Engine.Instance EngineInstance
			{
			get
				{
				if (engineManager != null)
					{  return engineManager.EngineInstance;  }
				else
					{  return null;  }
				}
			}



		// Group: Variables
		// _________________________________________________________________________


		/* var: testFolder
		 * The <TestFolder> currently being run with <RunAll()>.  Null otherwise.
		 */
		protected TestFolder testFolder;

		/* var: engineManager
		 * An <EngineManager> to handle the <Engine.Instance> for the tests.
		 */
		protected EngineManager engineManager;

		/* var: engineMode
		 * Engine options such as whether the test runner requires an <Engine.Instance> and the full HTML documentation
		 * to be built.
		 */
		protected EngineMode engineMode;

		/* var: inputMode
		 * Input options on how the input will be formatted and which <RunTest()> function is called.
		 */
		protected InputMode inputMode;

		}
	}
