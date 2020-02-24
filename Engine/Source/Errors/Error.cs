/* 
 * Class: CodeClear.NaturalDocs.Engine.Errors.Error
 * ____________________________________________________________________________
 * 
 * A class containing information about an error that occurred in the engine.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Errors
	{
	public class Error : IComparable
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Error
		 * A constructor for an error that occurs in a specific file.
		 */
		public Error (string message, Path file = default(Path), int lineNumber = 0, Config.Source configSource = Config.Source.NotDefined, 
						 string property = null)
			{
			this.message = message;

			this.file = file;
			this.lineNumber = lineNumber;
			this.configSource = configSource;
			this.property = property;
			}
			
			
		/* Constructor: Error
		 * A constructor for an error that occurs in a specific file.
		 */
		public Error (string message, Config.PropertyLocation propertyLocation, string property = null) 
			: this (message, propertyLocation.FileName, propertyLocation.LineNumber, propertyLocation.Source, property)
			{
			}


		/* Function: Matches
		 * Whether the error occurs in the passed location.
		 */
		public bool Matches (Path file = default(Path), int lineNumber = 0, Config.Source configSource = Config.Source.NotDefined, 
									string property = null)
			{
			return (this.file == file && this.lineNumber == lineNumber && this.configSource == configSource && this.property == property);
			}
			
			
		/* Function: Matches
		 * Whether the error occurs in the passed location.
		 */
		public bool Matches (Config.PropertyLocation propertyLocation, string property = null)
			{
			return Matches(propertyLocation.FileName, propertyLocation.LineNumber, propertyLocation.Source, property);
			}


		/* Function: CompareTo
		 * Implements IComparable.CompareTo() so that errors can be sorted.  They are sorted by <File>, then <LineNumber>, then 
		 * <ConfigSource>, then <Property>.
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
					
				if (lineNumber != other.lineNumber)
					{
					return lineNumber - other.lineNumber;
					}

				if (configSource != other.configSource)
					{
					return (int)configSource - (int)other.configSource;
					}

				if (property != other.property)
					{
					if (property == null)
						{  return -1;  }
					else
						{  return property.CompareTo(other.property);  }
					}

				return 0;
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

		/* Property: ConfigSource
		 * The config source the error occurs in, if appropriate.  Will be <Config.Source.NotDefined> otherwise.
		 */
		public Config.Source ConfigSource
			{
			get
				{  return configSource;  }
			set
				{  configSource = value;  }
			}

		/* Property: Property
		 * 
		 * The propery that the error occurs in, if appropriate.  Will be null otherwise.
		 * 
		 * The string will match the class property name.  For example, an error in <ProjectConfig.TabWidth> will have "TabWidth".  An error in the 
		 * global title will be "ProjectInfo.Title".  If it occurs in one of the targets, it will be something like "InputTargets[0].Folder" or 
		 * "OutputTargets[1].ProjectInfo.Title".
		 */
		public string Property
			{
			get
				{  return property;  }
			set
				{  property = value;  }
			}
		
		
		
		// Group: Variables
		// __________________________________________________________________________
		
		
		protected string message;

		protected Path file;
		protected int lineNumber;
		protected Config.Source configSource;
		protected string property;

		}
	}