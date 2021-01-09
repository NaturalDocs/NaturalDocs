/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.IgnoredSourceFolderPattern
 * ____________________________________________________________________________
 * 
 * The configuration of an ignored source folder pattern.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config.Targets
	{
	public class IgnoredSourceFolderPattern : Targets.Filter
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		public IgnoredSourceFolderPattern (PropertyLocation propertyLocation) : base (propertyLocation)
			{
			pattern = null;
			patternPropertyLocation = PropertySource.NotDefined;
			}

		public IgnoredSourceFolderPattern (IgnoredSourceFolderPattern toCopy) : base (toCopy)
			{
			pattern = toCopy.pattern;
			patternPropertyLocation = toCopy.patternPropertyLocation;
			}

		override public Filter Duplicate ()
			{
			return new IgnoredSourceFolderPattern(this);
			}

		public override bool Validate (ErrorList errorList, int targetIndex)
			{
			return true;
			}


		// Group: Properties
		// __________________________________________________________________________


		/* Property: Pattern
		 * The text pattern that should determine whether a folder has its contents ignored.
		 */
		public string Pattern
		    {
		    get
		        {  return pattern;  }
			set
				{  pattern = value;  }
		    }


		
		// Group: Property Locations
		// __________________________________________________________________________
		
					
		/* Property: PatternPropertyLocation
		 * Where <Pattern> is defined.
		 */
		public PropertyLocation PatternPropertyLocation
		    {
		    get
		        {  return patternPropertyLocation;  }
		    set
		        {  patternPropertyLocation = value;  }
		    }

	
		
		// Group: Variables
		// __________________________________________________________________________
		

		protected string pattern;

		protected PropertyLocation patternPropertyLocation;

		}
	}