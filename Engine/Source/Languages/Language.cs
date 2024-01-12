/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Language
 * ____________________________________________________________________________
 *
 * A class encapsulating information about a language.
 *
 *
 * Multithreading: Thread Safety Notes
 *
 *		Once the object is set up, meaning there will be no further changes to properties like <LineCommentSymbols>,
 *		the object is read-only and can be used by multiple threads simultaneously.  <Parser> stores no parsing state
 *		information so it is also okay to be used by multiple threads simulaneously.
 *
 *
 * Topic: Language-Specific Parsers
 *
 *		The <Parser> property can be set to a language-specific one instead of the default generalized one.  In order to
 *		have it used automatically you must also create a predefined language entry in <Languages.Manager>'s
 *		constructor.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Languages
	{
	public class Language : IDObjects.IDObject
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: EnumValues
		 * Can be Global, UnderType, or UnderParent.
		 */
		public enum EnumValues : byte
			{  Global, UnderType, UnderParent  };


		/* Enum: LanguageType
		 *
		 * The type of language or file this is.
		 *
		 * FullSupport - The language is fully supported.
		 * BasicSupport - The language has basic support.
		 * TextFile - The file is a text file.
		 * Container - The file is a container, meaning it's contents may contain code from other languages, such as .cgi and .asp files.
		 */
		public enum LanguageType : byte
			{  FullSupport, BasicSupport, TextFile, Container  };



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Language
		 * Creates a new language object.
		 */
		public Language (string name) : base ()
			{
			this.name = name;

			simpleIdentifier = null;
			type = LanguageType.BasicSupport;
			parser = null;
			lineCommentSymbols = null;
			blockCommentSymbols = null;
			javadocLineCommentSymbols = null;
			javadocBlockCommentSymbols = null;
			xmlLineCommentSymbols = null;
			memberOperator = ".";
			prototypeEnders = null;
			lineExtender = null;
			enumValue = EnumValues.UnderType;
			caseSensitive = true;
			blockCommentsNest = false;
			}

		/* Function: HasPrototypeEndersFor
		 * Returns whether prototype enders are defined for the passed comment type.
		 */
		public bool HasPrototypeEndersFor (int commentTypeID)
			{
			return (PrototypeEndersIndex(commentTypeID) != -1);
			}

		/* Function: GetPrototypeEndersFor
		 * Returns the <Languages.PrototypeEnders> for the passed comment type, or null if there are none.
		 */
		public PrototypeEnders GetPrototypeEndersFor (int commentTypeID)
			{
			int index = PrototypeEndersIndex(commentTypeID);

			if (index == -1)
				{  return null;  }
			else
				{  return prototypeEnders[index];  }
			}

		/* Function: AddPrototypeEnders
		 * Sets the <PrototypeEnders> for the passed comment type.  If enders already existed for that type they will be replaced.
		 */
		public void AddPrototypeEnders (PrototypeEnders prototypeEnders)
			{
			bool delete = (prototypeEnders == null ||
								 (!prototypeEnders.HasSymbols && !prototypeEnders.IncludeLineBreaks));

			int index = PrototypeEndersIndex(prototypeEnders.CommentTypeID);

			if (index == -1)
				{
				if (!delete)
					{
					if (this.prototypeEnders == null)
						{  this.prototypeEnders = new List<PrototypeEnders>();  }

					this.prototypeEnders.Add(prototypeEnders);
					}
				}
			else
				{
				if (!delete)
					{  this.prototypeEnders[index] = prototypeEnders;  }
				else
					{
					if (this.prototypeEnders.Count == 1)
						{  this.prototypeEnders = null;  }
					else
						{  this.prototypeEnders.RemoveAt(index);  }
					}
				}
			}

		/* Function: PrototypeEndersIndex
		 * Returns the index into <prototypeEnders> the passed comment ID exists at, or -1 if it's not in the list.
		 */
		protected int PrototypeEndersIndex (int commentTypeID)
			{
			if (prototypeEnders == null)
				{  return -1;  }

			for (int i = 0; i < prototypeEnders.Count; i++)
				{
				if (prototypeEnders[i].CommentTypeID == commentTypeID)
					{  return i;  }
				}

			return -1;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Name
		 * The name of the language.
		 */
		override public string Name
			{
			get
				{  return name;  }
			}

		/* Property: SimpleIdentifier
		 * The name of the language using only the letters A to Z, or null if it hasn't been set yet.
		 */
		public string SimpleIdentifier
			{
			get
				{  return simpleIdentifier;  }
			set
				{  simpleIdentifier = value;  }
			}

		/* Property: Type
		 * The <LanguageType> of the language or file.
		 */
		public LanguageType Type
			{
			get
				{  return type;  }
			set
				{  type = value;  }
			}

		/* Property: HasParser
		 * Whether <Parser> has been set.
		 */
		public bool HasParser
			{
			get
				{  return (parser != null);  }
			}

		/* Property: Parser
		 * The <Languages.Parser> associated with this language, or null if it hasn't been set yet.
		 */
		public Languages.Parser Parser
			{
			get
				{  return parser;  }
			set
				{  parser = value;  }
			}

		/* Property: HasLineCommentSymbols
		 * Whether there are any <LineCommentSymbols> defined.
		 */
		public bool HasLineCommentSymbols
			{
			get
				{  return (lineCommentSymbols != null);  }
			}

		/* Property: LineCommentSymbols
		 * A list of strings representing line comment symbols, or null if none are defined.
		 */
		public IList<string> LineCommentSymbols
			{
			get
				{  return lineCommentSymbols;  }
			set
				{
				if (value == null || value.Count == 0)
					{  lineCommentSymbols = null;  }
				else
					{
					if (lineCommentSymbols == null)
						{  lineCommentSymbols = new List<string>(value.Count);  }
					else
						{  lineCommentSymbols.Clear();  }

					lineCommentSymbols.AddRange(value);
					}
				}
			}

		/* Property: HasBlockCommentSymbols
		 * Whether there are any <BlockCommentSymbols> defined.
		 */
		public bool HasBlockCommentSymbols
			{
			get
				{  return (blockCommentSymbols != null);  }
			}

		/* Property: BlockCommentSymbols
		 * A list of <Languages.BlockCommentSymbols> that start and end block comments, or null if none are defined.
		 */
		public IList<BlockCommentSymbols> BlockCommentSymbols
			{
			get
				{  return blockCommentSymbols;  }
			set
				{
				if (value == null || value.Count == 0)
					{  blockCommentSymbols = null;  }
				else
					{
					if (blockCommentSymbols == null)
						{  blockCommentSymbols = new List<BlockCommentSymbols>(value.Count);  }
					else
						{  blockCommentSymbols.Clear();  }

					// This works because BlockCommentSymbols are structs
					blockCommentSymbols.AddRange(value);
					}
				}
			}

		/* Property: HasJavadocLineCommentSymbols
		 * Whether there are any <JavadocLineCommentSymbols> defined.
		 */
		public bool HasJavadocLineCommentSymbols
			{
			get
				{  return (javadocLineCommentSymbols != null);  }
			}

		/* Property: JavadocLineCommentSymbols
		 * A list of <Languages.LineCommentSymbols> that start Javadoc line comments, or null if there aren't any.
		 */
		public IList<LineCommentSymbols> JavadocLineCommentSymbols
			{
			get
				{  return javadocLineCommentSymbols;  }
			set
				{
				if (value == null || value.Count == 0)
					{  javadocLineCommentSymbols = null;  }
				else
					{
					if (javadocLineCommentSymbols == null)
						{  javadocLineCommentSymbols = new List<LineCommentSymbols>(value.Count);  }
					else
						{  javadocLineCommentSymbols.Clear();  }

					// This works because LineCommentSymbols are structs
					javadocLineCommentSymbols.AddRange(value);
					}
				}
			}

		/* Property: HasJavadocBlockCommentSymbols
		 * Whether there are any <JavadocBlockCommentSymbols> defined.
		 */
		public bool HasJavadocBlockCommentSymbols
			{
			get
				{  return (javadocBlockCommentSymbols != null);  }
			}

		/* Property: JavadocBlockCommentSymbols
		 * A list of <Javadoc.BlockCommentSymbols> that start and end Javadoc block comments, or null if there aren't any.
		 */
		public IList<BlockCommentSymbols> JavadocBlockCommentSymbols
			{
			get
				{  return javadocBlockCommentSymbols;  }
			set
				{
				if (value == null || value.Count == 0)
					{  javadocBlockCommentSymbols = null;  }
				else
					{
					if (javadocBlockCommentSymbols == null)
						{  javadocBlockCommentSymbols = new List<BlockCommentSymbols>(value.Count);  }
					else
						{  javadocBlockCommentSymbols.Clear();  }

					// This works because BlockCommentSymbols are structs
					javadocBlockCommentSymbols.AddRange(value);
					}
				}
			}

		/* Property: HasXMLLineCommentSymbols
		 * Whether there are any <XMLLineCommentSymbols> defined.
		 */
		public bool HasXMLLineCommentSymbols
			{
			get
				{  return (xmlLineCommentSymbols != null);  }
			}

		/* Property: XMLLineCommentSymbols
		 * A list of strings that start XML line comments, or null if there aren't any.
		 */
		public IList<string> XMLLineCommentSymbols
			{
			get
				{  return xmlLineCommentSymbols;  }
			set
				{
				if (value == null || value.Count == 0)
					{  xmlLineCommentSymbols = null;  }
				else
					{
					if (xmlLineCommentSymbols == null)
						{  xmlLineCommentSymbols = new List<string>(value.Count);  }
					else
						{  xmlLineCommentSymbols.Clear();  }

					xmlLineCommentSymbols.AddRange(value);
					}
				}
			}

		/* Property: MemberOperator
		 * A string representing the default member operator symbol.
		 */
		public string MemberOperator
			{
			get
				{  return memberOperator;  }
			set
				{  memberOperator = value;  }
			}

		/* Property: HasPrototypeEnders
		 * Whether there are any <PrototypeEnders> defined.
		 */
		public bool HasPrototypeEnders
			{
			get
				{  return (prototypeEnders != null);  }
			}

		/* Property: PrototypeEnders
		 * A list of all the <Languages.PrototypeEnders> associated with this language, or null if none.
		 */
		public IList<PrototypeEnders> PrototypeEnders
			{
			get
				{  return prototypeEnders;  }
			}

		/* Property: HasLineExtender
		 * Whether <LineExtender> is defined.
		 */
		public bool HasLineExtender
			{
			get
				{  return (lineExtender != null);  }
			}

		/* Property: LineExtender
		 * A string representing the line extender symbol if line breaks are significant to the language, or null if none.
		 */
		 public string LineExtender
			{
			get
				{  return lineExtender;  }
			set
				{  lineExtender = value;  }
			}

		/* Property: EnumValue
		 * How enum values are referenced.
		 */
		public EnumValues EnumValue
			{
			get
				{  return enumValue;  }
			set
				{  enumValue = value;  }
			}

		/* Property: CaseSensitive
		 * Whether the language's identifiers are case sensitive.
		 */
		public bool CaseSensitive
			{
			get
				{  return caseSensitive;  }
			set
				{  caseSensitive = value;  }
			}

		/* Property: BlockCommentsNest
		 * Whether the language's block comments can nest.
		 */
		public bool BlockCommentsNest
			{
			get
				{  return blockCommentsNest;  }
			set
				{  blockCommentsNest = value;  }
			}



		// Group: Operators
		// __________________________________________________________________________


		/* Function: operator ==
		 * Returns whether all the properties of the two languages are equal.
		 */
		public static bool operator == (Language language1, Language language2)
			{
			if ((object)language1 == null && (object)language2 == null)
				{  return true;  }
			else if ((object)language1 == null || (object)language2 == null)
				{  return false;  }

			if (language1.ID != language2.ID ||
				language1.name != language2.name ||
				language1.simpleIdentifier != language2.simpleIdentifier ||
				language1.type != language2.type ||
				// ignore parser
				// do comment strings later
				language1.memberOperator != language2.memberOperator ||
				// do prototype enders later
				language1.lineExtender != language2.lineExtender ||
				language1.enumValue != language2.enumValue ||
				language1.caseSensitive != language2.caseSensitive ||
				language1.blockCommentsNest != language2.blockCommentsNest)
				{  return false;  }

			int lineCommentSymbols1Count = (language1.lineCommentSymbols != null ? language1.lineCommentSymbols.Count : 0);
			int lineCommentSymbols2Count = (language2.lineCommentSymbols != null ? language2.lineCommentSymbols.Count : 0);
			int blockCommentSymbols1Count = (language1.blockCommentSymbols != null ? language1.blockCommentSymbols.Count : 0);
			int blockCommentSymbols2Count = (language2.blockCommentSymbols != null ? language2.blockCommentSymbols.Count : 0);
			int javadocLineCommentSymbols1Count = (language1.javadocLineCommentSymbols != null ? language1.javadocLineCommentSymbols.Count : 0);
			int javadocLineCommentSymbols2Count = (language2.javadocLineCommentSymbols != null ? language2.javadocLineCommentSymbols.Count : 0);
			int javadocBlockCommentSymbols1Count = (language1.javadocBlockCommentSymbols != null ? language1.javadocBlockCommentSymbols.Count : 0);
			int javadocBlockCommentSymbols2Count = (language2.javadocBlockCommentSymbols != null ? language2.javadocBlockCommentSymbols.Count : 0);
			int xmlLineCommentSymbols1Count = (language1.xmlLineCommentSymbols != null ? language1.xmlLineCommentSymbols.Count : 0);
			int xmlLineCommentSymbols2Count = (language2.xmlLineCommentSymbols != null ? language2.xmlLineCommentSymbols.Count : 0);
			int prototypeEnders1Count = (language1.prototypeEnders != null ? language1.prototypeEnders.Count : 0);
			int prototypeEnders2Count = (language2.prototypeEnders != null ? language2.prototypeEnders.Count : 0);

			if (lineCommentSymbols1Count != lineCommentSymbols2Count ||
				blockCommentSymbols1Count != blockCommentSymbols2Count ||
				javadocLineCommentSymbols1Count != javadocLineCommentSymbols2Count ||
				javadocBlockCommentSymbols1Count != javadocBlockCommentSymbols2Count ||
				xmlLineCommentSymbols1Count != xmlLineCommentSymbols2Count ||
				prototypeEnders1Count != prototypeEnders2Count)
				{  return false;  }


			// The order these properties appear in doesn't matter, so use Contains().  This makes each list comparison an O(n²)
			// operation, but these should be very short lists so we don't care.

			if (lineCommentSymbols1Count > 0)
				{
				foreach (var lineCommentSymbols1 in language1.lineCommentSymbols)
					{
					if (!language2.lineCommentSymbols.Contains(lineCommentSymbols1))
						{  return false;  }
					}
				}

			if (blockCommentSymbols1Count > 0)
				{
				foreach (var blockCommentSymbols1 in language1.blockCommentSymbols)
					{
					if (!language2.blockCommentSymbols.Contains(blockCommentSymbols1))
						{  return false;  }
					}
				}

			if (javadocLineCommentSymbols1Count > 0)
				{
				foreach (var javadocLineCommentSymbols1 in language1.javadocLineCommentSymbols)
					{
					if (!language2.javadocLineCommentSymbols.Contains(javadocLineCommentSymbols1))
						{  return false;  }
					}
				}

			if (javadocBlockCommentSymbols1Count > 0)
				{
				foreach (var javadocBlockCommentSymbols1 in language1.javadocBlockCommentSymbols)
					{
					if (!language2.javadocBlockCommentSymbols.Contains(javadocBlockCommentSymbols1))
						{  return false;  }
					}
				}

			if (xmlLineCommentSymbols1Count > 0)
				{
				foreach (var xmlLineCommentSymbols1 in language1.xmlLineCommentSymbols)
					{
					if (!language2.xmlLineCommentSymbols.Contains(xmlLineCommentSymbols1))
						{  return false;  }
					}
				}

			if (prototypeEnders1Count > 0)
				{
				foreach (var prototypeEnders1 in language1.prototypeEnders)
					{
					if (!language2.prototypeEnders.Contains(prototypeEnders1))
						{  return false;  }
					}
				}

			return true;
			}

		/* Function: operator !=
		 * Returns if any of the properties of the two languages are different.
		 */
		public static bool operator != (Language language1, Language language2)
			{
			return !(language1 == language2);
			}

		public override bool Equals (object o)
			{
			if (o is Language)
				{  return (this == (Language)o);  }
			else
				{  return false;  }
			}

		public override int GetHashCode ()
			{
			return Name.GetHashCode();
			}



		// Group: Variables
		// __________________________________________________________________________

		/* var: name
		 * The language name.
		 */
		protected string name;

		/* var: simpleIdentifier
		 * The language's name using only the letters A to Z, or null if it's not defined.
		 */
		protected string simpleIdentifier;

		/* var: type
		 * The <LanguageType> of the language or file.
		 */
		protected LanguageType type;

		/* var: parser
		 * The <Languages.Parser> associated with this language, or null if it hasn't been defined.
		 */
		protected Languages.Parser parser;

		/* var: lineCommentSymbols
		 * A list of strings that start line comments, or null if there aren't any.
		 */
		protected List<string> lineCommentSymbols;

		/* var: blockCommentSymbols
		 * A list of <Languages.BlockCommentSymbols> that start and end block comments, or null if there aren't any.
		 */
		protected List<BlockCommentSymbols> blockCommentSymbols;

		/* var: javadocLineCommentSymbols
		 * A list of <Languages.LineCommentSymbols> that start Javadoc line comments, or null if there aren't any.
		 */
		protected List<LineCommentSymbols> javadocLineCommentSymbols;

		/* var: javadocBlockCommentSymbols
		 * A list of <Javadoc.BlockCommentSymbols> that start and end Javadoc block comments, or null if there aren't any.
		 */
		protected List<BlockCommentSymbols> javadocBlockCommentSymbols;

		/* var: xmlLineCommentSymbols
		 * A list of strings that start XML line comments, or null if there aren't any.
		 */
		protected List<string> xmlLineCommentSymbols;

		/* string: memberOperator
		 * A string representing the default member operator symbol.
		 */
		protected string memberOperator;

		/* var: prototypeEnders
		 * A list of the <Languages.PrototypeEnders> associated with this language.  Since there shouldn't be very many entries
		 * it is more efficient to have a list and walk through them than to use a Dictionary.
		 */
		protected List<PrototypeEnders> prototypeEnders;

		/* var: lineExtender
		 * A string representing the line extender symbol if line breaks are significant to the language, or null if ithere is
		 * none.
		 */
		protected string lineExtender;

		/* var: enumValue
		 * How the language handles enum values.
		 */
		protected EnumValues enumValue;

		/* var: caseSensitive
		 * Whether the language is case sensitive or not.
		 */
		protected bool caseSensitive;

		/* var: blockCommentsNest
		 * Whether the language's block comments can nest.
		 */
		protected bool blockCommentsNest;

		}
	}
