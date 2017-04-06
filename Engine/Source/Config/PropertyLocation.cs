/* 
 * Struct: CodeClear.NaturalDocs.Engine.Config.PropertyLocation
 * ____________________________________________________________________________
 * 
 * A struct that stores where a particular property is defined.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Config
	{
	public struct PropertyLocation
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: PropertyLocation
		 */
		public PropertyLocation (Config.Source source, Path fileName = default(Path), int lineNumber = 0)
			{
			#if DEBUG
			if (IsFileBased(source))
				{
				if (fileName == null)
					{  throw new Exception ("Must provide a file name for " + source  + " PropertyLocations.");  }
				}
			else
				{
				if (fileName != null || lineNumber != 0)
					{  throw new Exception ("Cannot provide a file name or line number for " + source + " PropertyLocations.");  }
				}
			#endif

			this.source = source;
			this.fileName = fileName;
			this.lineNumber = lineNumber;
			}
		
		
		/* Operator: operator PropertyLocation
		 * Allows non-file based <Config.Sources> to be cast directly to a PropertyLocation.
		 */
		public static implicit operator PropertyLocation (Config.Source configSource)
			{
			return new PropertyLocation(configSource);
			}



		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: IsFileBased
		 * Returns whether the passed <Config.Source> is file-based.
		 */
		public static bool IsFileBased (Config.Source configSource)
			{
			return (configSource >= Source.LowestFileValue &&
					  configSource <= Source.HighestFileValue);
			}


		
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: IsDefined
		 * Whether the property is defined.  Is equivalent to testing <Source> for <Source.NotDefined>.
		 */
		public bool IsDefined
			{
			get
				{  return (source != Source.NotDefined);  }
			}


		/* Property: Source
		 * The <Config.Source> where this property is defined, or <Source.NotDefined> if it hasn't been set.
		 */
		public Config.Source Source
			{
			get
				{  return source;  }
			set
				{  source = value;  }
			}
			
			
		/* Property: FileName
		 * If the property is defined in a config file, the <Path> of the file.  Null otherwise.
		 */
		public Path FileName
			{
			get
				{  return fileName;  }
			set
				{
				#if DEBUG
				if (IsFileBased(source))
					{
					if (value == null)
						{  throw new Exception ("FileName cannot be null for " + source + " PropertyLocations.");  }
					}
				else
					{
					if (value != null)
						{  throw new Exception ("FileName must be null for " + source + " PropertyLocations.");  }
					}
				#endif

				fileName = value;  
				}
			}
			
		
		/* Property: LineNumber
		 * If the property is defined in a config file, the line number where it appears.  Zero otherwise.
		 */
		public int LineNumber
			{
			get
				{  return lineNumber;  }
			set
				{  
				#if DEBUG
				// 0 is valid for file-based config sources
				if (!IsFileBased(source))
					{
					if (value != 0)
						{  throw new Exception ("LineNumber must be 0 for " + source + " PropertyLocations.");  }
					}
				#endif

				lineNumber = value;  
				}
			}

			

		// Group: Variables
		// __________________________________________________________________________		
		
		private Source source;
		private Path fileName;
		private int lineNumber;
		
		}
	}