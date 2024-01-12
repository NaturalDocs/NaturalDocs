/*
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.Input
 * ____________________________________________________________________________
 *
 * A base class for the configuration of all input targets.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Config.Targets
	{
	abstract public class Input : OverridableInputSettings
		{

		// Group: Functions
		// __________________________________________________________________________


		public Input (Files.InputType type, PropertyLocation propertyLocation) : base ()
			{
			this.type = type;
			this.number = 0;

			this.propertyLocation = propertyLocation;
			this.numberPropertyLocation = PropertySource.NotDefined;
			}


		public Input (Input toCopy) : base (toCopy)
			{
			this.type = toCopy.type;
			this.number = toCopy.number;

			this.propertyLocation = toCopy.propertyLocation;
			this.numberPropertyLocation = toCopy.numberPropertyLocation;
			}


		abstract public Input Duplicate ();


		/* Function: Validate
		 * Override to add errors if there are any problems with the target's properties, such as a folder not existing.
		 * TargetIndex is passed so that you may include it in the error's Property field, such as
		 * "InputTargets[0].Folder".
		 */
		public abstract bool Validate (Errors.ErrorList errorList, int targetIndex);


		/* Function: IsSameTarget
		 * Override to determine whether the two input targets are fundamentally the same.  Only primary identifying properties
		 * should be compared, so two <SourceFolders> should return true if they point to the same folder, and secondary
		 * properties such as <Name> and <Number> should be ignored.
		 */
		public abstract bool IsSameTarget (Input other);



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Type
		 * The type of file source this input target provides.
		 */
		public Files.InputType Type
			{
			get
				{  return type;  }
			}


		/* Property: Number
		 * The number of the input target, or zero if it isn't defined.  Numbers are used to distinguish multiple file sources in URLs,
		 * such as File:Folder/Source.cs versus File2:Folder/Source.cs, and must be unique for each <Type>.
		 */
		public int Number
			{
			get
				{  return number;  }
			set
				{  number = value;  }
			}



		// Group: Property Locations
		// __________________________________________________________________________


		/* Property: PropertyLocation
		 * Where the input target is defined.
		 */
		public PropertyLocation PropertyLocation
			{
			get
				{  return propertyLocation;  }
			}


		/* Property: NumberPropertyLocation
		 * Where <Number> is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation NumberPropertyLocation
		    {
		    get
		        {  return numberPropertyLocation;  }
		    set
		        {  numberPropertyLocation = value;  }
		    }



		// Group: Variables
		// __________________________________________________________________________

		protected Files.InputType type;
		protected int number;

		protected PropertyLocation propertyLocation;
		protected PropertyLocation numberPropertyLocation;

		}
	}
