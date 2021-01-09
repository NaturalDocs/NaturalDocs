/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.Target
 * ____________________________________________________________________________
 * 
 * A shared base class for all input, output, and filter targets.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.Config
	{
	abstract public class Target
		{
		
		// Group: Functions
		// __________________________________________________________________________
		

		public Target (PropertyLocation propertyLocation)
			{
			this.propertyLocation = propertyLocation;
			}

		public Target (Target toCopy)
			{
			this.propertyLocation = toCopy.propertyLocation;
			}

		/* Function: Validate
		 * Override to add errors if there are any problems with the target's properties, such as a folder not existing.
		 * TargetIndex is passed so that you may include it in the error's Property field, such as
		 * "InputTargets[0].Folder".
		 */
		public abstract bool Validate (Errors.ErrorList errorList, int targetIndex);
			
	
		
		// Group: Properties
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