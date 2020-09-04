/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.Input
 * ____________________________________________________________________________
 * 
 * A base class for the configuration of all input targets.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Config.Targets
	{
	abstract public class Input : Target
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		public Input (PropertyLocation propertyLocation) : base (propertyLocation)
			{
			this.number = 0;
			numberPropertyLocation = PropertySource.NotDefined;
			}

		public Input (Input toCopy) : base (toCopy)
			{
			number = toCopy.number;
			numberPropertyLocation = toCopy.numberPropertyLocation;
			}

		abstract public Input Duplicate ();


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
		abstract public Files.InputType Type
			{  get;  }


		/* Property: TypePropertyLocation
		 * Where <Type> is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		abstract public PropertyLocation TypePropertyLocation
		    {  get;  }


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
		
		protected int number;
		protected PropertyLocation numberPropertyLocation;
		
		}
	}