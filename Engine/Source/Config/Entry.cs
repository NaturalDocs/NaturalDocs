/* 
 * Class: GregValure.NaturalDocs.Engine.Config.Entry
 * ____________________________________________________________________________
 * 
 * A base class for all entries found in <Project.txt> and <Project.nd>.  An entry is an entity like an input folder; it can have
 * its own properties and there can be more than one of the same type.  It does not include global settings like the project title.
 * 
 * Do not derive directly from this class.  Use subclasses like <InputEntry> instead.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Config
	{
	abstract public class Entry
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		protected Entry (Path configFile = default(Path), int lineNumber = -1)
			{
			this.configFile = configFile;
			this.lineNumber = lineNumber;
			}
			

		/* Function: Validate
		 * 
		 * Checks this entry's configuration to make sure it's valid, such as checking whether a path exists.  If it's not valid it will
		 * return false and place errors on the list.
		 * 
		 * Why not do this directly in <ConfigFileParser>?  Well, suppose you have <Project.txt> with an invalid input folder entry.
		 * However, the user specifies the input folders on the command line so it will be ignored.  We don't want to post an error in 
		 * this case.  If we did they would have to correct Project.txt every time they changed the paths on the command line, and 
		 * the whole point of keeping the command line options is so people don't have to use Project.txt if they don't want to.
		 */
		public virtual bool Validate (Errors.ErrorList errorList)
			{
			return true;
			}

			
		/* Function: IsSameFundamentalEntry
		 * Returns whether this entry is fundamentally the same as the passed one, meaning any identifying properties will be the
		 * same (i.e. both being HTML output folders with the same path) but secondary properties may be different (i.e. the folder 
		 * names and numbers being different.)
		 */
		public virtual bool IsSameFundamentalEntry (Entry other)
			{
			return false;
			}
			
			
		/* Function: CopyUnsetPropertiesFrom
		 * This entry will copy any properties that were not explicitly set from the passed one.  This will only be called for entries
		 * that return true with <IsSameFundamentalEntry>.
		 */
		public virtual void CopyUnsetPropertiesFrom (Entry other)
			{
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: ConfigFile
		 * The configuration file that defined this property, or null if none.
		 */
		public Path ConfigFile
			{
			get
				{  return configFile;  }
			}

		/* Property: LineNumber
		 * The line number of <ConfigFile> where this property was defined, or -1 if not relevant.
		 */
		public int LineNumber
			{
			get
				{  return lineNumber;  }
			}


		
		// Group: Variables
		// __________________________________________________________________________


		/* var: configFile
		 * The file that defined this property, or null if none.
		 */
		protected Path configFile;

		/* var: lineNumber
		 * The line number in <configFile> where this property was defined, or -1 if not relevant.
		 */
		protected int lineNumber;
		
		}
	}