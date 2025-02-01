/*
 * Struct: CodeClear.NaturalDocs.Engine.FilePosition
 * ____________________________________________________________________________
 *
 * A position within a source file, which encapsulates both a line number and character number.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine
	{
	public struct FilePosition
		{

		// Group: Functions
		// __________________________________________________________________________


		public FilePosition (int lineNumber, int charNumber)
			{
			this.lineNumber = lineNumber;
			this.charNumber = charNumber;
			}



		// Group: Operators
		// __________________________________________________________________________


		/* Operator: operator ==
		 */
		public static bool operator== (FilePosition a, FilePosition b)
			{
			return (a.lineNumber == b.lineNumber &&
					  a.charNumber == b.charNumber);
			}

		/* Operator: operator !=
		 */
		public static bool operator!= (FilePosition a, FilePosition b)
			{
			return !(a == b);
			}

		/* Operator: operator >
		 */
		public static bool operator> (FilePosition a, FilePosition b)
			{
			return (a.lineNumber > b.lineNumber ||
					  (a.lineNumber == b.lineNumber &&
					   a.charNumber > b.charNumber));
			}

		/* Operator: operator >=
		 */
		public static bool operator>= (FilePosition a, FilePosition b)
			{
			return (a.lineNumber > b.lineNumber ||
					  (a.lineNumber == b.lineNumber &&
					   a.charNumber >= b.charNumber));
			}

		/* Operator: operator <
		 */
		public static bool operator< (FilePosition a, FilePosition b)
			{
			return !(a >= b);
			}

		/* Operator: operator <=
		 */
		public static bool operator<= (FilePosition a, FilePosition b)
			{
			return !(a > b);
			}

		/* Function: GetHashCode
		 */
		public override int GetHashCode ()
			{
			return lineNumber.GetHashCode();
			}

		/* Function: Equals
		 */
		public override bool Equals (object obj)
			{
			if (obj is FilePosition)
				{  return (this == (FilePosition)obj);  }
			else
				{  return false;  }
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: LineNumber
		 * The line number in the file.  Line numbers start at one.
		 */
		public int LineNumber
			{
			get
				{  return lineNumber;  }
			set
				{  lineNumber = value;  }
			}


		/* Property: CharNumber
		 * The character number relative to the line.  Character numbers start at one.
		 */
		public int CharNumber
			{
			get
				{  return charNumber;  }
			set
				{  charNumber = value;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: lineNumber
		 * The line number in the file.  Line numbers start at one.
		 */
		private int lineNumber;

		/* var: charNumber
		 * The character number relative to the line.  Character numbers start at one.
		 */
		private int charNumber;

		}
	}
