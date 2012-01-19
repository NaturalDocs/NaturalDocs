/* 
 * Class: GregValure.NaturalDocs.EngineTests.Framework.TestResult
 * ____________________________________________________________________________
 * 
 * A class storing the result of a single <Test>.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.EngineTests.Framework
	{
	public class TestResult
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Constructor: TestResult
		 */
		public TestResult (string name, bool passed)
			{
			this.name = name;
			this.passed = passed;
			}



		// Group: Properties
		// _________________________________________________________________________


		/* Property: Passed
		 * Whether the test succeeded.
		 */
		public bool Passed
			{
			get
				{  return passed;  }
			}

		/* Property: Name
		 * The name of the test.
		 */
		public string Name
			{
			get
				{  return name;  }
			}



		// Group: Variables
		// _________________________________________________________________________


		/* var: name
		 * The name of the test.
		 */
		protected string name;

		/* var: passed
		 * Whether the test succeeded.
		 */
		protected bool passed;

		}

	}