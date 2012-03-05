/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.Framework.Test
 * ____________________________________________________________________________
 * 
 * A class storing information about a single file-based test.
 * 
 * Usage:
 * 
 *		- When iterating through the test data folder, use <IsInputFile()> to find the test files.
 *		- Pass them to <Load()>.
 *		- External code should then run the test and either set <ActualOutput> or <TestException>.
 *		- Call <SaveOutput()> to write the result to disk.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine;


namespace GregValure.NaturalDocs.Engine.Tests.Framework
	{
	public class Test
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Constructor: Test
		 */
		public Test ()
			{
			name = null;

			inputFile = null;
			expectedOutputFile = null;
			actualOutputFile = null;

			expectedOutput = null;
			actualOutput = null;
			testException = null;
			}


		/* Function: IsInputFile
		 *	 A static function that determines whether the passed file is a test input file.
		 */
		public static bool IsInputFile (Path file)
			{
			return file.NameWithoutPathOrExtension.EndsWith(" - Input");
			}
		

		/* Function: Load
		 * Loads an input file, setting up all the <Properties> for the test.  You can use this to recycle a Test object that was
		 * previously used for another file.
		 */
		public void Load (Path inputFile)
			{
			this.inputFile = inputFile;

			name = inputFile.NameWithoutPathOrExtension;

			if (name.EndsWith(" - Input") == false || name.Length <= 8)
				{  throw new InvalidOperationException();  }
			else
				{  name = name.Substring(0, name.Length - 8);  }

			expectedOutputFile = inputFile.ParentFolder + '/' + name + " - Expected Output.txt";
			actualOutputFile = inputFile.ParentFolder + '/' + name + " - Actual Output.txt";

			try
				{  expectedOutput = System.IO.File.ReadAllText(expectedOutputFile);  }
			catch (System.IO.FileNotFoundException)
				{  expectedOutput = null;  }

			actualOutput = null;
			testException = null;
			}


		/* Function: SaveOutput
		 * Saves <ActualOutput> to the relevant file.  If <ActualOutput> is null or an exception was stored in <TestException>
		 * it will contain a note explaining that.
		 */
		 public void SaveOutput ()
			{
			string output;

			if (testException != null)
				{
				output =
					"Exception thrown:\n\n" +
					"   " + testException.Message + "\n" +
					"   (" + testException.GetType() + ")\n\n";

				Exception inner = testException.InnerException;
				while (inner != null)
					{
					output +=
					"Caused by:\n\n" +
					"   " + inner.Message + "\n" +
					"   (" + inner.GetType() + ")\n\n";

					inner = inner.InnerException;
					}

				output +=
					"Stack trace:\n\n" +
					testException.StackTrace + "\n";
				}

			else if (actualOutput == null || actualOutput == "")
				{
				output = "(No output generated)\n";
				}

			else
				{  output = actualOutput;  }

			string oldOutput = null;

			try
				{  
				// May not exist
				oldOutput = System.IO.File.ReadAllText(actualOutputFile);  
				}
			catch
				{  }

			// We don't want to change the time stamps every time, so only write when necessary.
			if (oldOutput == null || oldOutput.NormalizeLineBreaks() != output.NormalizeLineBreaks())
				{  System.IO.File.WriteAllText(actualOutputFile, output);  }
			}


		/* Function: ToTestResult
		 * Returns a <TestResult> object made from the current test.
		 */
		public TestResult ToTestResult ()
			{
			return new TestResult(name, Passed);
			}



		// Group: Properties
		// _________________________________________________________________________


		/* Property: Passed
		 * Whether the test succeeded, which means the actual output matches the expected output and no exceptions were 
		 * thrown.  Both the actual and expected output must exist.
		 */
		public bool Passed
			{
			get
				{
				return (testException == null && expectedOutput != null && actualOutput != null && expectedOutput == actualOutput);
				}
			}

		/* Property: Name
		 * The name of the test.
		 */
		public string Name
			{
			get
				{  return name;  }
			}

		/* Property: InputFile
		 * The full path of the input file.
		 */
		public Path InputFile
			{
			get
				{  return inputFile;  }
			}

		/* Property: ExpectedOutputFile
		 * The full path of the expected output file.
		 */
		public Path ExpectedOutputFile
			{
			get
				{  return expectedOutputFile;  }
			}

		/* Property: ActualOutputFile
		 * The full path of the actual output file where you put the generated output if it differs from <ExpectedOutput>.
		 */
		public Path ActualOutputFile
			{
			get
				{  return actualOutputFile;  }
			}

		/* Property: ExpectedOutput
		 * The contents of <ExpectedOutputFile>, or null if that file didn't exist.
		 */
		public string ExpectedOutput
			{
			get
				{  return expectedOutput;  }
			}

		/* Property: ActualOutput
		 * The actual output generated from <Input>.
		 */
		public string ActualOutput
			{
			get
				{  return actualOutput;  }
			set
				{  actualOutput = value;  }
			}

		/* Property: TestException
		 * The exception generated by trying to build the output, if any.
		 */
		public Exception TestException
			{
			get
				{  return testException;  }
			set
				{  testException = value;  }
			}



		// Group: Variables
		// _________________________________________________________________________


		/* var: name
		 * The name of the test.
		 */
		protected string name;

		/* var: inputFile
		 * The full path of the input file.
		 */
		protected Path inputFile;

		/* var: expectedOutputFile
		 * The full path of the expected output file.
		 */
		protected Path expectedOutputFile;

		/* var: actualOutputFile
		 * The full path of the actual output file where you put the generated output if it differs from <ExpectedOutput>.
		 */
		protected Path actualOutputFile;

		/* var: input
		 * The contents of <InputFile>, minus the description header if present.
		 */
		protected string input;

		/* var: expectedOutput
		 * The contents of <ExpectedOutputFile>, or null if that file didn't exist.
		 */
		protected string expectedOutput;

		/* var: actualOutput
		 * The actual output generated from <Input>.
		 */
		protected string actualOutput;

		/* var: testException
		 * The exception generated by trying to build the output, if any.
		 */
		protected Exception testException;

		}

	}