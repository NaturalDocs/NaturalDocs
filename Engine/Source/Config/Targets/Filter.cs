/*
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.Filter
 * ____________________________________________________________________________
 *
 * A base class for the configuration of all filter targets.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Config.Targets
	{
	abstract public class Filter
		{

		// Group: Functions
		// __________________________________________________________________________


		public Filter (PropertyLocation propertyLocation)
			{
			this.propertyLocation = propertyLocation;
			}

		public Filter (Filter toCopy)
			{
			this.propertyLocation = toCopy.propertyLocation;
			}

		abstract public Filter Duplicate ();

		/* Function: Validate
		 * Override to add errors if there are any problems with the target's properties, such as a folder not existing.
		 * TargetIndex is passed so that you may include it in the error's Property field, such as
		 * "InputTargets[0].Folder".
		 */
		public abstract bool Validate (Errors.ErrorList errorList, int targetIndex);


		// Group: Property Locations
		// __________________________________________________________________________

		/* Property: PropertyLocation
		 * Where the filter target is defined.
		 */
		public PropertyLocation PropertyLocation
			{
			get
				{  return propertyLocation;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected PropertyLocation propertyLocation;

		}
	}
