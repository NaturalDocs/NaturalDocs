/* 
 * Struct: GregValure.NaturalDocs.Engine.Symbols.EndingSymbol
 * ____________________________________________________________________________
 * 
 * A struct encapsulating the ending symbol from a symbol string, which is a normalized way of representing 
 * the last part of a hierarchal code element or topic, such as "functionc" in "PackageA.PackageB.FunctionC".
 * Unlike <SymbolStrings>, ending symbols are case-insensitive.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Symbols
	{
	public struct EndingSymbol : IComparable
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: EndingSymbol
		 */
		private EndingSymbol (string newEndingSymbol)
			{
			endingSymbol = newEndingSymbol;
			}

			
		/* Function: FromExportedString
		 * Creates an EndingSymbol from the passed string which originally came from another EndingSymbol object.  This skips 
		 * the normalization stage because it should already be in the proper format.  Only use this when retrieving EndingSymbols
		 * that were stored as plain text in a database or other data file.
		 */
		static public EndingSymbol FromExportedString (string exportedEndingSymbol)
		   {
		   EndingSymbol endingSymbol = new EndingSymbol(exportedEndingSymbol);
		   return endingSymbol;
		   }
			
		
		/* Function: FromSymbolStringSegment
		 * Creates an EndingSymbol from a segment of text split off from a <SymbolString>.  This function should only be called by
		 * <SymbolString>, other code should use <SymbolString.EndingSymbol> instead.  This assumes the string segment has
		 * already been normalized by <SymbolString>.
		 */
		static internal EndingSymbol FromSymbolStringSegment (string symbolStringSegment)
		   {
			// The only difference in normalization between this and SymbolString is that EndingStrings are case-insensitive.
		   if (symbolStringSegment != null)
				{  symbolStringSegment = symbolStringSegment.ToLower();  }

			EndingSymbol endingSymbol = new EndingSymbol(symbolStringSegment);
		   return endingSymbol;
		   }
			
		
		
		// Group: Operators
		// __________________________________________________________________________
		
		
		/* operator: operator string
		 * A cast operator to covert the symbol to a string.
		 */
		public static implicit operator string (EndingSymbol endingSymbolObject)
		   {
		   return endingSymbolObject.endingSymbol;
		   }
						
		/* Operator: operator ==
		 */
		public static bool operator== (EndingSymbol a, object b)
		   {
			// We need to make the operator compare against object intead of another EndingSymbol in order to support
			// directly comparing against null.
		   return a.Equals(b);
		   }

		/* Operator: operator !=
		 */
		public static bool operator!= (EndingSymbol a, object b)
		   {
		   return !(a.Equals(b));
		   }

		/* Function: ToString
		 * Returns the SymbolString as a string.
		 */
		public override string ToString ()
		   {
		   return endingSymbol;
		   }
			
		/* Function: GetHashCode
		 */
		public override int GetHashCode ()
		   {
			if (endingSymbol == null)
				{  return 0;  }
		   else
				{  return endingSymbol.GetHashCode();  }
		   }

		/* Function: Equals
		 */
		public override bool Equals (object other)
		   {
		   if (other == null)
		      {  return (endingSymbol == null);  }
		   else if (other is EndingSymbol)
		      {  return (endingSymbol == ((EndingSymbol)other).endingSymbol);  }
			else if (other is string)
				{  return (endingSymbol == (string)other);  }
		   else
		      {  return false;  }
		   }
			
		/* Function: CompareTo
		 */
		public int CompareTo (object other)
		   {
		   return endingSymbol.CompareTo(other);
		   }
		
			
			
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* string: endingSymbol
		 * The symbol, _always_ in normalized form.
		 */
		private string endingSymbol;
	
		}
	}