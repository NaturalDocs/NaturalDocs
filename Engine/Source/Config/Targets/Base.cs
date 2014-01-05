/* 
 * Class: GregValure.NaturalDocs.Engine.Config.Targets.Base
 * ____________________________________________________________________________
 * 
 * A shared base class for all <InputTargets>, <OutputTargets>, and <FilterTargets>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine.Config;


namespace GregValure.NaturalDocs.Engine.Config.Targets
	{
	abstract public class Base
		{
		
		// Group: Functions
		// __________________________________________________________________________
		

		public Base (PropertyLocation propertyLocation)
			{
			this.propertyLocation = propertyLocation;
			}

		public Base (Base toCopy)
			{
			this.propertyLocation = toCopy.propertyLocation;
			}

		/* Function: Validate
		 * Override to add errors if there are any problems with the target's properties, such as a folder not existing.
		 * TargetIndex is passed so that you may include it in the error's Property field, such as
		 * "InputTargets[0].Folder".
		 */
		public abstract bool Validate (Errors.ErrorList errorList, int targetIndex);
			
	
		
		// Group: Property Locations
		// __________________________________________________________________________
		
					
		/* Property: PropertyLocation
		 * Where the entire entry is defined.
		 */
		public PropertyLocation PropertyLocation
			{
			get
				{  return propertyLocation;  }
			set
				{  propertyLocation = value;  }
			}
			
	
		
		// Group: Variables
		// __________________________________________________________________________
		

		protected PropertyLocation propertyLocation;
		
		}
	}