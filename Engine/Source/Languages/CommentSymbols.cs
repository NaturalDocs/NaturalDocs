
// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Languages
	{

	/* Struct: CodeClear.NaturalDocs.Engine.Languages.BlockCommentSymbols
	 * ____________________________________________________________________________
	 * 
	 * A struct representing a pair of strings which serve as the opening and closing symbols for a block comment.
	 */
	public struct BlockCommentSymbols
		{

		// Group: Functions
		// __________________________________________________________________________

		public BlockCommentSymbols (string openingSymbol, string closingSymbol)
			{
			this.openingSymbol = openingSymbol;
			this.closingSymbol = closingSymbol;
			}


		// Group: Operators
		// __________________________________________________________________________

		public static bool operator == (BlockCommentSymbols a, BlockCommentSymbols b)
			{
			return (a.openingSymbol == b.openingSymbol &&
					   a.closingSymbol == b.closingSymbol);
			}
			
		public static bool operator != (BlockCommentSymbols a, BlockCommentSymbols b)
			{
			return !(a == b);
			}
			
		public override bool Equals (object o)
			{
			if (o is BlockCommentSymbols)
				{  return (this == (BlockCommentSymbols)o);  }
			else
				{  return false;  }
			}

		public override int GetHashCode ()
			{
			return openingSymbol.GetHashCode();
			}


		// Group: Properties
		// __________________________________________________________________________

		public string OpeningSymbol
			{
			get
				{  return openingSymbol;  }
			}

		public string ClosingSymbol
			{
			get
				{  return closingSymbol;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		private string openingSymbol;
		private string closingSymbol;

		}


	/* Struct: CodeClear.NaturalDocs.Engine.Languages.LineCommentSymbols
	 * ____________________________________________________________________________
	 * 
	 * A struct representing a pair of strings which serve and the first and following line comment symbols for comments
	 * where that is significant, such as Javadoc comments.
	 */
	public struct LineCommentSymbols
		{

		// Group: Functions
		// __________________________________________________________________________

		public LineCommentSymbols (string firstLineSymbol, string followingLinesSymbol)
			{
			this.firstLineSymbol = firstLineSymbol;
			this.followingLinesSymbol = followingLinesSymbol;
			}


		// Group: Operators
		// __________________________________________________________________________

		public static bool operator == (LineCommentSymbols a, LineCommentSymbols b)
			{
			return (a.firstLineSymbol == b.firstLineSymbol &&
					   a.followingLinesSymbol == b.followingLinesSymbol);
			}
			
		public static bool operator != (LineCommentSymbols a, LineCommentSymbols b)
			{
			return !(a == b);
			}
			
		public override bool Equals (object o)
			{
			if (o is LineCommentSymbols)
				{  return (this == (LineCommentSymbols)o);  }
			else
				{  return false;  }
			}

		public override int GetHashCode ()
			{
			return firstLineSymbol.GetHashCode();
			}


		// Group: Properties
		// __________________________________________________________________________

		public string FirstLineSymbol
			{
			get
				{  return firstLineSymbol;  }
			}

		public string FollowingLinesSymbol
			{
			get
				{  return followingLinesSymbol;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		private string firstLineSymbol;
		private string followingLinesSymbol;

		}

	}