/*
 * Class: CodeClear.NaturalDocs.Engine.Errors.Error
 * ____________________________________________________________________________
 *
 * A class containing information about an error that occurred in the engine.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
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
		public Error (string message, Path file = default(Path), int lineNumber = 0,
						   Config.PropertySource configSource = Config.PropertySource.NotDefined, string property = null)
			{
			this.message = message;

			this.file = file;
			this.lineNumber = lineNumber;
			this.propertySource = configSource;
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
		public bool Matches (Path file = default(Path), int lineNumber = 0,
									  Config.PropertySource propertySource = Config.PropertySource.NotDefined, string property = null)
			{
			return (this.file == file && this.lineNumber == lineNumber &&
					   this.propertySource == propertySource && this.property == property);
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

				if (propertySource != other.propertySource)
					{
					return (int)propertySource - (int)other.propertySource;
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

		/* Property: PropertySource
		 * The <Config.PropertySource> the error occurs in, if appropriate.  Will be <Config.PropertySource.NotDefined> otherwise.
		 */
		public Config.PropertySource PropertySource
			{
			get
				{  return propertySource;  }
			set
				{  propertySource = value;  }
			}

		/* Property: PropertyLocation
		 * The <Config.PropertyLocation> the error occurs in.
		 */
		public Config.PropertyLocation PropertyLocation
			{
			get
				{  return new Config.PropertyLocation(propertySource, file, lineNumber);  }
			}

		/* Property: Property
		 *
		 * The propery that the error occurs in, if appropriate.  Will be null otherwise.
		 *
		 * The string will match the class property name.  For example, an error in <ProjectConfig.TabWidth> will have "TabWidth".
		 * An error in the global title will be "ProjectInfo.Title".  If it occurs in one of the targets, it will be something like
		 * "InputTargets[0].Folder" or "OutputTargets[1].ProjectInfo.Title".
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
		protected Config.PropertySource propertySource;
		protected string property;

		}
	}
