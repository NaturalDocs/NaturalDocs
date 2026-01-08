/*
 * Class: CodeClear.NaturalDocs.Tests.Test
 * ____________________________________________________________________________
 *
 * A class storing information about a single test.  This does not include the code for executing tests, that exists in
 * <TestRunners>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Symbols;


namespace CodeClear.NaturalDocs.Tests
	{
	public class Test
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: FailureReasons
		 *
		 * The reason why an individual test failed.
		 *
		 * OutputDoesntMatch - The expected output file doesn't match the actual output file.
		 * ExpectedOutputMissing - The expected output file doesn't exist.
		 * InvalidConfigurationFile - There are errors in <Test Folder.txt>.
		 * ExceptionThrown - An exception was thrown when attempting to run the test.
		 *
		 * Null - The test hasn't been run yet or hasn't failed.
		 */
		public enum FailureReasons
			{  OutputDoesntMatch, ExpectedOutputMissing, InvalidConfigurationFile, ExceptionThrown, Null  }


		/* Enum: Status
		 */
		protected enum Status
			{  NotRun, Passed, Failed  }



		// Group: Constructors
		// __________________________________________________________________________


		/* Function: CreateFromInputFile
		 * Creates a new Test object from an input file.
		 */
		public static Test CreateFromInputFile (AbsolutePath inputFile)
			{
			AbsolutePath testFolder = inputFile.ParentFolder;
			string name = NameFromInputFile(inputFile);

			return new Test(
				name: name,
				inputFile: inputFile,
				expectedOutputFile: ExpectedOutputFileOf(name, testFolder),
				actualOutputFile: ActualOutputFileOf(name, testFolder)
				);
			}


		/* Function: CreateFromExpectedOutputFile
		 * Creates a new Test object from an expected output file.  Use this function only for tests that do not have a corresponding
		 * input file.
		 */
		public static Test CreateFromExpectedOutputFile (AbsolutePath expectedOutputFile)
			{
			AbsolutePath testFolder = expectedOutputFile.ParentFolder;
			string name = NameFromExpectedOutputFile(expectedOutputFile);

			return new Test(
				name: name,
				inputFile: null,
				expectedOutputFile: expectedOutputFile,
				actualOutputFile: ActualOutputFileOf(name, testFolder)
				);
			}


		/* Function: CreateFromActualOutputFile
		 * Creates a new Test object from an actual output file.  Use this function only for tests that do not have a corresponding input
		 * file.
		 */
		public static Test CreateFromActualOutputFile (AbsolutePath actualOutputFile)
			{
			AbsolutePath testFolder = actualOutputFile.ParentFolder;
			string name = NameFromActualOutputFile(actualOutputFile);

			return new Test(
				name: name,
				inputFile: null,
				expectedOutputFile: ExpectedOutputFileOf(name, testFolder),
				actualOutputFile: actualOutputFile
				);
			}


		/* Constructor: Test
		 * This is a protected constructor.  Use one of the Create functions instead.
		 */
		protected Test (string name, AbsolutePath inputFile, AbsolutePath expectedOutputFile, AbsolutePath actualOutputFile)
			{
			this.name = name;

			status = Status.NotRun;
			failureReason = FailureReasons.Null;

			this.inputFile = inputFile;
			this.expectedOutputFile = expectedOutputFile;
			this.actualOutputFile = actualOutputFile;

			classID = 0;
			classString = default;
			}



		// Group: Path Functions
		// __________________________________________________________________________


		/* Function: IsInputFile
		 * Returns whether the passed file is a test input file.
		 */
		public static bool IsInputFile (Path file)
			{
			// Input files can have any extension
			return file.NameWithoutPathOrExtension.EndsWith(" - Input");
			}


		/* Function: IsExpectedOutputFile
		 * Returns whether the passed file is an expected output file for a test.
		 */
		public static bool IsExpectedOutputFile (Path file)
			{
			return file.NameWithoutPath.EndsWith(" - Expected Output.txt");
			}


		/* Function: IsActualOutputFile
		 *	Returns whether the passed file is an actual output file for a test.
		 */
		public static bool IsActualOutputFile (Path file)
			{
			return file.NameWithoutPath.EndsWith(" - Actual Output.txt");
			}


		/* Function: ExpectedOutputFileOf
		 * Returns the expected output file of the passed test name and folder.
		 */
		public static AbsolutePath ExpectedOutputFileOf (string testName, AbsolutePath testFolder)
			{
			return testFolder + '/' + testName + " - Expected Output.txt";
			}


		/* Function: ActualOutputFileOf
		 * Returns the actual output file of the passed test name and folder.
		 */
		public static AbsolutePath ActualOutputFileOf (string testName, AbsolutePath testFolder)
			{
			return testFolder + '/' + testName + " - Actual Output.txt";
			}


		/* Function: NameFromInputFile
		 * Extracts the name of the test from an input file path.
		 */
		public static string NameFromInputFile (Path inputFile)
			{
			string name = inputFile.NameWithoutPathOrExtension;

			if (name.EndsWith(" - Input") == false)
				{  throw new InvalidOperationException();  }
			else
				{  return name.Substring(0, name.Length - 8);  }
			}


		/* Function: NameFromExpectedOutputFile
		 * Extracts the name of the test from an expected output file path.
		 */
		public static string NameFromExpectedOutputFile (Path outputFile)
			{
			string name = outputFile.NameWithoutPath;

			if (name.EndsWith(" - Expected Output.txt") == false)
				{  throw new InvalidOperationException();  }
			else
				{  return name.Substring(0, name.Length - 22);  }
			}


		/* Function: NameFromActualOutputFile
		 * Extracts the name of the test from an actual output file path.
		 */
		public static string NameFromActualOutputFile (Path outputFile)
			{
			string name = outputFile.NameWithoutPath;

			if (name.EndsWith(" - Actual Output.txt") == false)
				{  throw new InvalidOperationException();  }
			else
				{  return name.Substring(0, name.Length - 20);  }
			}



		// Group: State Functions
		// __________________________________________________________________________


		/* Function: MarkAsPassed
		 * Sets the test to passed.
		 */
		internal void MarkAsPassed ()
			{
			status = Status.Passed;
			failureReason = FailureReasons.Null;
			}


		/* Function: MarkAsFailed
		 * Sets the test to failed.  If you call this multiple times only the first reason will be used.
		 */
		internal void MarkAsFailed (FailureReasons reason)
			{
			if (reason == FailureReasons.Null)
				{  throw new InvalidOperationException("Must provide a reason when marking a test as failed.");  }

			status = Status.Failed;

			if (this.failureReason == FailureReasons.Null)
				{  this.failureReason = reason;  }
			}



		// Group: Properties
		// _________________________________________________________________________


		/* Property: Name
		 * The name of the test.
		 */
		public string Name
			{
			get
				{  return name;  }
			}


		/* Property: Passed
		 * Whether the test succeeded.  If the test hasn't been run yet this will be false.  Use <MarkAsPassed()> to set this property.
		 */
		public bool Passed
			{
			get
				{  return (status == Status.Passed);  }
			}


		/* Property: Failed
		 * Whether the test failed.  If the test hasn't been run yet this will be false.  Use <MarkAsFailed()> to set this property.
		 */
		public bool Failed
			{
			get
				{  return (status == Status.Failed);  }
			}


		/* Property: FailureReason
		 * If the test failed, the reason as to why.  This will be <FailureReasons.Null> if the test hasn't failed or hasn't been run.
		 */
		public FailureReasons FailureReason
			{
			get
				{  return failureReason;  }
			}


		/* Property: InputFile
		 * The full path of the input file.
		 */
		public AbsolutePath InputFile
			{
			get
				{  return inputFile;  }
			}


		/* Property: ExpectedOutputFile
		 * The full path of the expected output file.
		 */
		public AbsolutePath ExpectedOutputFile
			{
			get
				{  return expectedOutputFile;  }
			}


		/* Property: ActualOutputFile
		 * The full path of the actual output file.
		 */
		public AbsolutePath ActualOutputFile
			{
			get
				{  return actualOutputFile;  }
			}


		/* Property: ClassID
		 * An optional class ID associated with the test.  This is useful for class-based tests that may not have a single
		 * input file.  Can be ignored for everything else.
		 */
		public int ClassID
			{
			get
				{  return classID;  }
			protected internal set
				{  classID = value;  }
			}


		/* Property: ClassString
		 * An optional <ClassString> associated with the test.  This is useful for class-based tests that may not have a
		 * single input file.  Can be ignored for everything else.
		 */
		public ClassString ClassString
			{
			get
				{  return classString;  }
			protected internal set
				{  classString = value;  }
			}



		// Group: Variables
		// _________________________________________________________________________


		/* var: name
		 * The name of the test.
		 */
		protected string name;

		/* var: status
		 * The current <Status> of the test.
		 */
		protected Status status;

		/* var: failureReason
		 * If <status> is <Status.Failed>, the reason associated with the failure.  <FailureReasons.Null> otherwise.
		 */
		protected FailureReasons failureReason;

		/* var: inputFile
		 * The full path of the input file.
		 */
		protected AbsolutePath inputFile;

		/* var: expectedOutputFile
		 * The full path of the expected output file.
		 */
		protected AbsolutePath expectedOutputFile;

		/* var: actualOutputFile
		 * The full path of the actual output file.
		 */
		protected AbsolutePath actualOutputFile;

		/* var: classID
		 * An optional class ID associated with the test.  This is useful for class-based tests that may not have a single
		 * input file.  Can be ignored for everything else.
		 */
		protected int classID;

		/* var: classString
		 * An optional <ClassString> associated with the test.  This is useful for class-based tests that may not have a
		 * single input file.  Can be ignored for everything else.
		 */
		protected ClassString classString;

		}
	}
