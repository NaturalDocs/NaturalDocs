/* 
 * Struct: CodeClear.NaturalDocs.Engine.CommentTypes.CommentTypeFlags
 * ____________________________________________________________________________
 * 
 * A class encapsulating the properties and validation logic behind comment type flags, since it must be shared between
 * <CommentType> and <ConfigFileCommentType>.
 * 
 * When adding this to another class you should make it a public variable instead of a property.  Properties and structs do 
 * not behave predictably when used together.  If you were to use a property and write "commentType.Flags.Code = true", the
 * property will return a copy, you'd set Code to true on the copy, and then it would never be written back to the original
 * location.  To make it work you'd have to make a copy, edit it in a temporary variable, and then overwrite the original in 
 * a separate step.  Using a public variable instead lets it behave more predictably.
 * 
 * 
 * Usage:
 * 
 *		- Set individual flags and/or use <AllConfigurationProperties> to load and save a binary representation.
 *		- After reading a "Flags:" line from a user editable config file, call <Validate()> with strict mode off.
 *		- After reading a full "Comment Type:" section from a user editable config file, call <AddImpliedFlags()> and then
 *		  <Validate()> again with strict mode on.
 *		- You MUST call <Validate()> with strict mode on at least once before starting the engine.
 *		
 * 
 * Code, File, and Documentation:
 * 
 *		- One and only one must be defined.
 *		- Used to determine case sensitivity in linking.
 *		- Used to determine where long HTML titles should be broken.
 * 
 * Variable Type:
 * 
 *		- Implies Code.
 *		- Used to determine which comments can be used for links in prototypes.
 *		
 * Class Hierarchy and Database Hierarchy:
 * 
 *		- Only one may be defined.
 *		- Scope must be set to Start.
 *		- Implies Code.
 *		- Class Hierarchy implies Variable Type.
 *		- Used to determine which classes appear in the respective menus.
 *		- Used to determine which comments can be targets of and have their prototypes seached for class parent links.
 *		
 * Enum:
 * 
 *		- Implies Code and Variable Type.
 *		- Used to invoke special scope and embedded topic handling.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.CommentTypes
	{
	public struct CommentTypeFlags
		{
		
		// Group: Types
		// __________________________________________________________________________

		
		/* Enum: FlagValues
		 * 
		 * Configuration Properties:
		 * 
		 *		Code - Set if the comment type describes a code element.
		 *		File - Set if the comment type describes a file.
		 *		Documentation - Set if the comment type is for standalone documentation.
		 * 
		 *		VariableType - Set if the comment type can be used as a type for a variable.
		 * 
		 *		ClassHierarchy - Set if the comment type should appear in the class hierarchy.
		 *		DatabaseHierarchy - Set if the comment type should appear in the database hierarchy.
		 * 
		 *		Enum - Set if the comment type describes an enum.
		 *		
		 *		ConfigurationPropertiesMask - A combination of all the configuration properties so they can be filtered en masse.
		 *		
		 * Location Properties:
		 * 
		 *		InSystemFile - Set if the comment type was defined in the system config file <Topics.txt>.
		 *		InProjectFile - Set if the comment type was defined in the project config file <Topics.txt>.  Not set for Alter Comment Type.
		 *		InBinaryFile - Set if the comment type appears in <Topics.nd>.
		 * 
		 *		InConfigFiles - A combination of <InSystemFile> and <InProjectFile> used for testing if either are set.
		 *		
		 *		LocationPropertiesMask - A combination of all the location properties so they can be filtered en masse.
		 * 
		 */
		public enum FlagValues : ushort
			{
			Code = 0x0001,
			File = 0x0002,
			Documentation = 0x0004,

			VariableType = 0x0008,

			ClassHierarchy = 0x0010,
			DatabaseHierarchy = 0x0020,

			Enum = 0x0040,

			ConfigurationPropertiesMask = Code | File | Documentation | VariableType |
																		ClassHierarchy | DatabaseHierarchy | Enum,

			InSystemFile = 0x1000,
			InProjectFile = 0x2000,
			InBinaryFile = 0x4000,
			
			InConfigFiles = InSystemFile | InProjectFile,

			LocationPropertiesMask = InSystemFile | InProjectFile | InBinaryFile
			}
		
		
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Validate
		 * 
		 * Checks the flags for problems.  If there aren't any it will return null.  If there are it will return a list of error messages.
		 * 
		 * The error messages are retrieved from <Locale> using the module passed to the function and these identifiers.  You
		 * must implement them all in the translation file.
		 * 
		 * - CommentTypeFlags.CantCombine(a,b)
		 * - CommentTypeFlags.CantCombine(a,b,c)
		 * - CommentTypeFlags.MustDefineOneOf(a,b)
		 * - CommentTypeFlags.MustDefineOneOf(a,b,c)
		 * - CommentTypeFlags.MustDefineAWithB(a,b)
		 * - CommentTypeFlags.FlagRequiresScope(flag,scope)
		 * 
		 * If strict is false it only tests for contradictions, such as defining both <Code> and <File>.  You can use this for testing
		 * validity before the scope is set.  You should also use this to test validity on what the user entered before calling 
		 * <AddImpliedFlags()>.  This prevents potentially confusing error messages such as "Flags: File, Class Hierarchy" 
		 * complaining about both <Code> and <File> being set when <Code> wasn't explicitly set, it was added by 
		 * <AddImpliedFlags()>.
		 * 
		 * If strict is true it performs a more rigorous check, which should be done before allowing the engine to start.
		 * <AddImpliedFlags()> should be called beforehand.
		 * 
		 */
		public List<string> Validate (bool strict, CommentType.ScopeValue? scope, string localeModule)
			{
			List<string> errors = new List<string>();


			// Code, File, and Documentation

			if (Code)
				{
				if (File && Documentation)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.CantCombine(a,b,c)", "Code", "File", "Documentation"));  }
				else if (File)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.CantCombine(a,b)", "Code", "File"));  }
				else if (Documentation)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.CantCombine(a,b)", "Code", "Documentation"));  }
				}
			else if (File)
				{
				if (Documentation)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.CantCombine(a,b)", "File", "Documentation"));  }
				}
			else if (Documentation)
				{
				}
			else
				{
				if (strict)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.MustDefineOneOf(a,b,c)", "Code", "File", "Documentation"));  }
				}


			// Variable Type

			if (VariableType)
				{
				if (strict && !Code)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.MustDefineAWithB(a,b)", "Code", "Variable Type"));  }
				if (File)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.CantCombine(a,b)", "File", "Variable Type"));  }
				if (Documentation)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.CantCombine(a,b)", "Documentation", "Variable Type"));  }
				}


			// Class Hierarchy, Database Hierarchy

			if (ClassHierarchy)
				{
				if (DatabaseHierarchy)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.CantCombine(a,b)", "Class Hierarchy", "Database Hierarchy"));  }
				if (strict && !Code)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.MustDefineAWithB(a,b)", "Code", "Class Hierarchy"));  }
				if (strict && !VariableType)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.MustDefineAWithB(a,b)", "Variable Type", "Class Hierarchy"));  }
				if (File)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.CantCombine(a,b)", "File", "Class Hierarchy"));  }
				if (Documentation)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.CantCombine(a,b)", "Documentation", "Class Hierarchy"));  }
				if ( (scope == null && strict) || 
					  (scope != null && (CommentType.ScopeValue)scope != CommentType.ScopeValue.Start) )
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.FlagRequiresScope(flag,scope)", "Class Hierarchy", "Start"));  }
				}
			else if (DatabaseHierarchy)
				{
				if (strict && !Code)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.MustDefineAWithB(a,b)", "Code", "Database Hierarchy"));  }
				if (File)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.CantCombine(a,b)", "File", "Database Hierarchy"));  }
				if (Documentation)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.CantCombine(a,b)", "Documentation", "Database Hierarchy"));  }
				if ( (scope == null && strict) || 
					  (scope != null && (CommentType.ScopeValue)scope != CommentType.ScopeValue.Start) )
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.FlagRequiresScope(flag,scope)", "Database Hierarchy", "Start"));  }
				}


			// Enum

			if (Enum)
				{
				if (strict && !Code)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.MustDefineAWithB(a,b)", "Code", "Enum"));  }
				if (strict && !VariableType)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.MustDefineAWithB(a,b)", "Variable Type", "Enum"));  }
				if (File)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.CantCombine(a,b)", "File", "Class Hierarchy"));  }
				if (Documentation)
					{  errors.Add(Locale.Get(localeModule, "TopicTypeFlags.CantCombine(a,b)", "Documentation", "Class Hierarchy"));  }
				}


			if (errors.Count == 0)
				{  return null;  }
			else
				{  return errors;  }
			}


		/* Function: AddImpliedFlags
		 * Adds flags that are implied by other flags, such as how <Enum> implies <Code> and <VariableType>.  Also sets
		 * <Code> if <File> or <Documentation> isn't set.
		 */
		public void AddImpliedFlags ()
			{
			if ((flags & (FlagValues.ClassHierarchy | FlagValues.DatabaseHierarchy | 
								 FlagValues.Enum | FlagValues.VariableType)) != 0)
				{  flags |= FlagValues.Code;  }

			if ((flags & (FlagValues.Enum | FlagValues.ClassHierarchy)) != 0)
				{  flags |= FlagValues.VariableType;  }

			if ((flags & (FlagValues.File | FlagValues.Documentation)) == 0)
				{  flags |= FlagValues.Code;  }
			}



		// Group: Configuration Properties
		// __________________________________________________________________________
		

		/* Property: AllConfigurationProperties
		 * All of the properties below combined into one <FlagValues> except for the <location properties>.  This allows
		 * them all to be compared at once, or see if any are set by comparing against zero, or read or write them to disk
		 * as one value.  Setting this will not affect the <location properties>.
		 */
		public FlagValues AllConfigurationProperties
			{
			get
				{  return (flags & FlagValues.ConfigurationPropertiesMask);  }
			set
				{
				flags &= ~FlagValues.ConfigurationPropertiesMask;
				flags |= (value & FlagValues.ConfigurationPropertiesMask);
				}
			}

		/* Property: Code
		 * Whether the comment type describes a code element.
		 */
		public bool Code
			{
			get
				{  return ( (flags & FlagValues.Code) != 0);  }
			set
				{  
				if (value == true)
					{  flags |= FlagValues.Code;  }
				else
					{  flags &= ~FlagValues.Code;  }
				}
			}

		/* Property: File
		 * Whether the comment type describes a file.
		 */
		public bool File
			{
			get
				{  return ( (flags & FlagValues.File) != 0);  }
			set
				{  
				if (value == true)
					{  flags |= FlagValues.File;  }
				else
					{  flags &= ~FlagValues.File;  }
				}
			}

		/* Property: Documentation
		 * Whether the comment type is used for standalone documentation.
		 */
		public bool Documentation
			{
			get
				{  return ( (flags & FlagValues.Documentation) != 0);  }
			set
				{  
				if (value == true)
					{  flags |= FlagValues.Documentation;  }
				else
					{  flags &= ~FlagValues.Documentation;  }
				}
			}

		/* Property: VariableType
		 * Whether the comment type describes a code element that can be used as the type of a variable.
		 */
		public bool VariableType
			{
			get
				{  return ( (flags & FlagValues.VariableType) != 0);  }
			set
				{  
				if (value == true)
					{  flags |= FlagValues.VariableType;  }
				else
					{  flags &= ~FlagValues.VariableType;  }
				}
			}

		/* Property: ClassHierarchy
		 * Whether the comment type is part of the class hierarchy.
		 */
		public bool ClassHierarchy
			{
			get
				{  return ( (flags & FlagValues.ClassHierarchy) != 0);  }
			set
				{  
				if (value == true)
					{  flags |= FlagValues.ClassHierarchy;  }
				else
					{  flags &= ~FlagValues.ClassHierarchy;  }
				}
			}

		/* Property: DatabaseHierarchy
		 * Whether the comment type is part of the database hierarchy.
		 */
		public bool DatabaseHierarchy
			{
			get
				{  return ( (flags & FlagValues.DatabaseHierarchy) != 0);  }
			set
				{  
				if (value == true)
					{  flags |= FlagValues.DatabaseHierarchy;  }
				else
					{  flags &= ~FlagValues.DatabaseHierarchy;  }
				}
			}

		/* Property: Enum
		 * Whether the comment type describes an enum.
		 */
		public bool Enum
			{
			get
				{  return ( (flags & FlagValues.Enum) != 0);  }
			set
				{  
				if (value == true)
					{  flags |= FlagValues.Enum;  }
				else
					{  flags &= ~FlagValues.Enum;  }
				}
			}



		// Group: Location Properties
		// __________________________________________________________________________


		/* Property: InSystemFile
		 * Whether this comment type was defined in the system <Topics.txt> file.  Does not affect equality comparisons.
		 */
		public bool InSystemFile
			{
			get
				{  return ( (flags & FlagValues.InSystemFile) != 0);  }
			set
				{  
				if (value == true)
					{  flags |= FlagValues.InSystemFile;  }
				else
					{  flags &= ~FlagValues.InSystemFile;  }
				}
			}
			
		/* Property: InProjectFile
		 * Whether this comment type was defined in the project <Topics.txt> file.  Does not affect equality comparisons.
		 */
		public bool InProjectFile
			{
			get
				{  return ( (flags & FlagValues.InProjectFile) != 0);  }
			set
				{  
				if (value == true)
					{  flags |= FlagValues.InProjectFile;  }
				else
					{  flags &= ~FlagValues.InProjectFile;  }
				}
			}
			
		/* Property: InConfigFiles
		 * Whether this comment type was defined in either of the <Topics.txt> files.  Does not affect equality comparisons.
		 */
		public bool InConfigFiles
			{
			get
				{  return ( (flags & FlagValues.InConfigFiles) != 0);  }
			}
			
		/* Property: InBinaryFile
		 * Whether this comment type appears in <Topics.nd>.  Does not affect equality comparisons.
		 */
		public bool InBinaryFile
			{
			get
				{  return ( (flags & FlagValues.InBinaryFile) != 0);  }
			set
				{
				if (value == true)
					{  flags |= FlagValues.InBinaryFile;  }
				else
					{  flags &= ~FlagValues.InBinaryFile;  }
				}
			}

			
			
		// Group: Variables
		// __________________________________________________________________________
		

		/* var: flags
		 */
		private FlagValues flags;

		}
	}