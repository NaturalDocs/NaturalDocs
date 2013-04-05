/* 
 * Class: GregValure.NaturalDocs.Engine.Errors.Error
 * ____________________________________________________________________________
 * 
 * A class containing information about an error that occurred in the engine.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Errors
	{
	public class Error : IComparable
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Error
		 * A constructor for an error that occurs in a specific file.
		 */
		public Error (string newMessage, Path newFile = default(Path), int newLineNumber = 0)
			{
			message = newMessage;
			file = newFile;
			lineNumber = newLineNumber;
			}
			
			
		/* Function: CompareTo
		 * Implements IComparable.CompareTo() so that errors can be sorted.  They are sorted by <File>, then <LineNumber>.
		 */
		public int CompareTo (object otherObject)
			{
			if (otherObject is Error)
				{
				Error other = (Error)otherObject;
				
				if (file != other.file)
					{  
					if (file == null)
						{  return -1;  }
					else
						{  return file.CompareTo(other.file);  }
					}
					
				return lineNumber - other.lineNumber;
				}
			else
				{  throw new InvalidOperationException();  }
			}
		
			
			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Message
		 * The error message itself.
		 */
		public string Message
			{
			get
				{  return message;  }
			}
			
		/* Property: File
		 * The <Path> the error occurs in, if appropriate.  Will be null otherwise.
		 */
		public Path File
			{
			get
				{  return file;  }
			}
			
		/* Property: LineNumber
		 * The line number in the <File> the error occurs in, if appropriate.  Will be zero otherwise.
		 */
		public int LineNumber
			{
			get
				{  return lineNumber;  }
			set
				{  lineNumber = value;  }
			}
		
		
		
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* string: message
		 * The error message itself.
		 */
		protected string message;
		
		/* var: file
		 * The file the error appears in, if appropriate.  Will be null otherwise.
		 */
		protected Path file;
		
		/* var: lineNumber
		 * The line number of the <file> the error appears in, if appropriate.  Will be zero otherwise.
		 */
		protected int lineNumber;

		}
	}