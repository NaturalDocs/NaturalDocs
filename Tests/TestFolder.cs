/*
 * Class: CodeClear.NaturalDocs.Tests.TestFolder
 * ____________________________________________________________________________
 *
 * A class for managing a folder of tests.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using CodeClear.NaturalDocs.Engine;
using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Tests
	{
	public class TestFolder
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: Status
		 */
		protected enum Status
			{  NotRun, Passed, Failed  }



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: TestFolder
		 */
		public TestFolder (AbsolutePath path, TestFolderConfig config)
			{
			this.path = path;
			this.config = config;
			this.tests = null;
			this.status = Status.NotRun;
			this.failureMessage = null;
			}



		// Group: Path Functions
		// __________________________________________________________________________


		/* Function: IsTestFolder
		 * Whether the passed path is a test folder, as determined by whether it contains a recognized configuration file.
		 */
		public static bool IsTestFolder (AbsolutePath folderPath, out AbsolutePath configFilePath)
			{
			foreach (var configFileName in PossibleConfigFileNames)
				{
				configFilePath = folderPath + "/" + configFileName;

				if (System.IO.File.Exists(configFilePath))
					{  return true;  }
				}

			configFilePath = null;
			return false;
			}


		/* Function: IsHTMLOutputFolder
		 * Whether the passed path is a HTML output folder, in which case it and its subfolders can be skipped when looking
		 * for tests.
		 */
		public static bool IsHTMLOutputFolder (Path folder)
			{
			return (folder.NameWithoutPath == "HTML Output");
			}



		// Group: State Functions
		// __________________________________________________________________________


		/* Function: MarkAsPassed
		 *
		 * Sets the test folder to passed.
		 *
		 * Note that this is independent of whether any individual <Test> passed or failed.  You must manually mark a folder
		 * as passed if all its <Tests> passed, and marking the folder as passed does not affect any of its individual <Tests>.
		 */
		public void MarkAsPassed ()
			{
			status = Status.Passed;
			failureMessage = null;
			}


		/* Function: MarkAsFailed
		 *
		 * Sets the test folder to failed.  If you call this multiple times only the first reason will be used.
		 *
		 * Note that this is independent of whether any individual <Test> passed or failed.  You must manually mark a folder
		 * as failed if any of its <Tests> failed, and marking the folder as failed does not affect any of its individual <Tests>.
		 */
		public void MarkAsFailed (string reason)
			{
			if (reason == null)
				{  throw new InvalidOperationException("Must provide a reason when marking a test folder as failed.");  }

			status = Status.Failed;

			if (failureMessage == null)
				{  failureMessage = reason;  }
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Path
		 * The path to the test folder.
		 */
		public AbsolutePath Path
			{
			get
				{  return path;  }
			}


		/* Property: Config
		 * The <TestFolderConfig> associated with this folder.
		 */
		public TestFolderConfig Config
			{
			get
				{  return config;  }
			}


		/* Property: Passed
		 *
		 * Whether this test folder has passed.  If the tests haven't been run yet this will be false.  Use <MarkAsPassed()> to
		 * set this property.
		 *
		 * Note that this is independent of whether any individual <Test> passed or failed.  You must manually mark a folder
		 * as passed if all its <Tests> passed, and marking the folder as passed does not affect any of its individual <Tests>.
		 */
		public bool Passed
			{
			get
				{  return (status == Status.Passed);  }
			}


		/* Property: Failed
		 *
		 * Whether this test folder failed.  If the tests haven't been run yet this will be false.  Use <MarkAsFailed()> to set this
		 * property.
		 *
		 * Note that this is independent of whether any individual <Test> passed or failed.  You must manually mark a folder
		 * as failed if any of its <Tests> failed, and marking the folder as failed does not affect any of its individual <Tests>.
		 */
		public bool Failed
			{
			get
				{  return (status == Status.Failed);  }
			}


		/* Property: FailureMessage
		 * If the test folder failed, a message explaining why.  If the folder passed or the tests haven't been run yet this will be
		 * null.
		 */
		public string FailureMessage
			{
			get
				{  return failureMessage;  }
			}



		// Group: Individual Test Properties
		// _________________________________________________________________________


		/* Property: Tests
		 * A list of <Tests> associated with this folder, or null if it hasn't been generated yet.
		 */
		public List<Test> Tests
			{
			get
				{  return tests;  }
			internal set
				{  tests = value;  }
			}


		/* Property: TestsPassed
		 * The number of <Tests> which passed.  Note that this is independent of whether the folder itself passed.
		 */
		public int TestsPassed
			{
			get
				{
				if (tests == null)
					{  return 0;  }
				else
					{
					int count = 0;

					foreach (var test in tests)
						{
						if (test.Passed)
							{  count++;  }
						}

					return count;
					}
				}
			}


		/* Property: TestsFailed
		 * The number of <Tests> which failed.  Note that this is independent of whether the folder itself failed.
		 */
		public int TestsFailed
			{
			get
				{
				if (tests == null)
					{  return 0;  }
				else
					{
					int count = 0;

					foreach (var test in tests)
						{
						if (test.Failed)
							{  count++;  }
						}

					return count;
					}
				}
			}


		/* Property: TestCount
		 * The number of <Tests> associated with this folder.
		 */
		public int TestCount
			{
			get
				{
				if (tests == null)
					{  return 0;  }
				else
					{  return tests.Count;  }
				}
			}



		// Group: Variables
		// _________________________________________________________________________


		/* var: path
		 * The path of the test folder.
		 */
		protected AbsolutePath path;

		/* var: config
		 * The configuration of the test folder.
		 */
		protected TestFolderConfig config;

		/* var: tests
		 * A list of <Tests> found in the folder, or null if it hasn't been created yet.
		 */
		protected List<Test> tests;

		/* var: status
		 * The current <Status> of the test folder.
		 */
		protected Status status;

		/* var: failureMessage
		 * If <status> is <Status.Failed>, a message associated with the failure.  Null otherwise.
		 */
		protected string failureMessage;



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: PossibleConfigFileNames
		 * A list of all the possible test folder configuration file names.
		 */
		static public string[] PossibleConfigFileNames = new string[] {
			"Test Folder.txt",
			"- Test Folder.txt",
			"~ Test Folder.txt",
			".Test Folder.txt" };

		}
	}
