/* 
 * Struct: CodeClear.NaturalDocs.Engine.Symbols.UsingString
 * ____________________________________________________________________________
 * 
 * A struct encapsulating a using string, which is a normalized way of representing a single "using" statement.
 * 
 * The encoding uses <SeparatorChars.Level2> since it encapsulates a <SymbolString> which uses 
 * <SeparatorChars.Level1>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Symbols
	{
	public struct UsingString
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: UsingType
		 * 
		 * Which effectthe using statement has.
		 * 
		 * AddPrefix - The statement adds a prefix to symbols.  An AddPrefix statement for "PackageA.PackageB" means
		 *							"Symbol" can be interpreted as "PackageA.PackageB.Symbol".
		 * ReplacePrefix - The statement can replace a prefix on symbols.  A ReplacePrefix statement for "PackageA"
		 *									to "PackageB" means "PackageA.Symbol" can be interpreted as "PackageB.Symbol".
		 */
		public enum UsingType : byte
			{  
			AddPrefix = 1,
			ReplacePrefix = 2
			}


		
		// Group: Constants
		// __________________________________________________________________________

		/* Constant: SeparatorChar
		 * The character used to separate string components.
		 */
		public const char SeparatorChar = SeparatorChars.Level2;



		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: UsingString
		 */
		private UsingString (string newUsingString)
			{
			usingString = newUsingString;
			}

			
		/* Function: FromParameters
		 * Creates a UsingString from the passed parameters.
		 */
		static public UsingString FromParameters (UsingType type, SymbolString prefixToAdd, SymbolString prefixToRemove = default(SymbolString))
			{
			StringBuilder stringBuilder = new System.Text.StringBuilder(prefixToAdd.ToString().Length + 1);

			if (type == UsingType.AddPrefix)
				{  
				stringBuilder.Append('A');

				if (prefixToAdd == null)
					{  throw new InvalidOperationException();  }

				stringBuilder.Append(prefixToAdd.ToString());
				}

			else if (type == UsingType.ReplacePrefix)
				{  
				stringBuilder.Append('R');

				if (prefixToAdd == null || prefixToRemove == null)
					{  throw new InvalidOperationException();  }

				stringBuilder.Append(prefixToRemove);
				stringBuilder.Append(SeparatorChar);
				stringBuilder.Append(prefixToAdd);
				}

			else
				{  throw new InvalidOperationException();  }

			return new UsingString(stringBuilder.ToString());
			}


		/* Function: FromExportedString
		 * Creates a UsingString from the passed string which originally came from another UsingString object.  This assumes the
		 * string is already be in the proper format.  Only use this when retrieving UsingStrings that were stored as plain text 
		 * in a database or other data file.
		 */
		static public UsingString FromExportedString (string exportedUsingString)
			{
			if (exportedUsingString != null && exportedUsingString.Length == 0)
				{  exportedUsingString = null;  }

			return new UsingString(exportedUsingString);
			}


		
		// Group: Operators
		// __________________________________________________________________________
		
		
		/* operator: operator string
		 * A cast operator to covert the using string to a string.
		 */
		public static implicit operator string (UsingString usingString)
			{
			return usingString.usingString;
			}



		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: PrefixToAdd
		 * The <SymbolString> to add to the beginning of the symbol.  This is applicable to both <UsingType.AddPrefix>
		 * and <UsingType.ReplacePrefix>.
		 */
		public SymbolString PrefixToAdd
			{
			get
				{
				if (Type == UsingType.AddPrefix)
					{  
					return SymbolString.FromExportedString(usingString.Substring(1));  
					}
				else if (Type == UsingType.ReplacePrefix)
					{
					int separatorIndex = usingString.IndexOf(SeparatorChar, 1);
					return SymbolString.FromExportedString(usingString.Substring(separatorIndex + 1));
					}
				else
					{  throw new InvalidOperationException();  }
				}
			}


		/* Property: PrefixToRemove
		 * The <SymbolString> to remove from the beginning of the symbol.  This is only applicable with 
		 * <UsingType.ReplacePrefix>.
		 */
		public SymbolString PrefixToRemove
			{
			get
				{
				if (Type == UsingType.ReplacePrefix)
					{
					int separatorIndex = usingString.IndexOf(SeparatorChar, 1);
					return SymbolString.FromExportedString(usingString.Substring(1, separatorIndex - 1));
					}
				else
					{  throw new InvalidOperationException();  }
				}
			}


		/* Property: Type
		 * Which effect the using statement has.
		 */
		public UsingType Type
			{
			get
				{
				if (usingString[0] == 'A')
					{  return UsingType.AddPrefix;  }
				else if (usingString[0] == 'R')
					{  return UsingType.ReplacePrefix;  }
				else
					{  throw new InvalidOperationException();  }
				}
			}
					
			
		
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* string: usingString
		 * 
		 * The combined using string.
		 * 
		 * - The first character will be 'A' or 'R' for <UsingType.AddPrefix> or <UsingType.ReplacePrefix>.
		 * - For <UsingType.AddPrefix>, the rest of the string will be a <SymbolString> of the prefix to add.
		 * - For <UsingType.ReplacePrefix>, the first character will be followed by the <SymbolString> of the
		 *   prefix to remove, then a <SeparatorChar>, then the <SymbolString> of the prefix to replace it.
		 */
		private string usingString;
	
		}
	}