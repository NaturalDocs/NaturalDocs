/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.Targets.InputBase
 * ____________________________________________________________________________
 * 
 * A base class for the configuration of all input targets.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.Config.Targets
	{
	abstract public class InputBase : Base
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		public InputBase (PropertyLocation propertyLocation, Files.InputType type) : base (propertyLocation)
			{
			this.name = null;
			this.number = 0;
			this.type = type;

			namePropertyLocation = Source.NotDefined;
			numberPropertyLocation = Source.NotDefined;
			typePropertyLocation = propertyLocation;
			}

		public InputBase (InputBase toCopy) : base (toCopy)
			{
			name = toCopy.name;
			number = toCopy.number;
			type = toCopy.type;

			namePropertyLocation = toCopy.namePropertyLocation;
			numberPropertyLocation = toCopy.numberPropertyLocation;
			typePropertyLocation = toCopy.typePropertyLocation;
			}

		abstract public InputBase Duplicate ();

		abstract public void GenerateDefaultName ();


		/* Function: IsSameTarget
		 * Override to determine whether the two input targets are fundamentally the same.  Only primary identifying properties
		 * should be compared, so two <SourceFolders> should return true if they point to the same folder, and secondary
		 * properties such as <Name> and <Number> should be ignored.
		 */
		public abstract bool IsSameTarget (InputBase other);
			
	
		
		// Group: Properties
		// __________________________________________________________________________


		/* Property: Name
		 * The name of the input target, or null if it isn't defined.  Names are used to distinguish multiple file sources in user-visible
		 * places such as menus.  They should ideally be unique.
		 */
		public string Name
			{
			get
				{  return name;  }
			set
				{  name = value;  }
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


		/* Property: Type
		 * The type of file source this input target provides.
		 */
		public Files.InputType Type
			{
			get
				{  return type;  }
			set
				{  type = value;  }
			}


		
		// Group: Property Locations
		// __________________________________________________________________________
		
					
		/* Property: NamePropertyLocation
		 * Where <Name> is defined, or <Source.NotDefined> if it isn't.
		 */
		public PropertyLocation NamePropertyLocation
		    {
		    get
		        {  return namePropertyLocation;  }
		    set
		        {  namePropertyLocation = value;  }
		    }


		/* Property: NumberPropertyLocation
		 * Where <Number> is defined, or <Source.NotDefined> if it isn't.
		 */
		public PropertyLocation NumberPropertyLocation
		    {
		    get
		        {  return numberPropertyLocation;  }
		    set
		        {  numberPropertyLocation = value;  }
		    }


		/* Property: TypePropertyLocation
		 * Where <Type> is defined, or <Source.NotDefined> if it isn't.
		 */
		public PropertyLocation TypePropertyLocation
		    {
		    get
		        {  return typePropertyLocation;  }
		    set
		        {  typePropertyLocation = value;  }
		    }


		
		// Group: Variables
		// __________________________________________________________________________
		
		protected string name;
		protected int number;
		protected Files.InputType type;

		protected PropertyLocation namePropertyLocation;
		protected PropertyLocation numberPropertyLocation;
		protected PropertyLocation typePropertyLocation;
		
		}
	}