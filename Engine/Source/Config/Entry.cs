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
		
		
		protected Entry ()
			{
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
		
		}
	}