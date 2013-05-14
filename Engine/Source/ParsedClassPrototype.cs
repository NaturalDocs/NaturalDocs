/* 
 * Class: GregValure.NaturalDocs.Engine.ParsedClassPrototype
 * ____________________________________________________________________________
 * 
 * A class that wraps a <Tokenizer> for a prototype that's been marked with <ClassPrototypeParsingTypes>, providing easier 
 * access to things like parent lines.
 * 
 * Usage:
 * 
 *		The functions and properties obviously rely on the relevant tokens being set.  You cannot expect a proper result from
 *		<GetParent()> or <NumberOfParents> unless the tokens are marked with <ClassPrototypeParsingType.StartOfParents>
 *		and <ClassPrototypeParsingType.ParentSeparator>.  Likewise, you can't get anything from <GetParentName()> unless
 *		you also have tokens marked with <ClassPrototypeParsingType.Name>.  However, you can set the parent divider tokens,
 *		call <GetParent()>, and then use those bounds to further parse the parent and set tokens like 
 *		<ClassPrototypeParsingType.Name>.
 *		
 *		You can set multiple consecutive tokens to <ClassPrototypeParsingType.StartOfParents> and 
 *		<ClassPrototypeParsingType.ParentSeparator> and they will be counted as one separator.  However, all whitespace in
 *		between them must be marked as well.
 * 
 *		An important thing to remember though is that the parent divisions are calculated once and saved.  Only call functions like
 *		<GetParent()> after *ALL* the separator tokens (<ClassPrototypeParsingType.StartOfParents>,
 *		<ClassPrototypeParsingType.ParentSeparator>, and optionally <ClassPrototypeParsingType.StartOfBody>) are set and will 
 *		not change going forward.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Tokenization;


namespace GregValure.NaturalDocs.Engine
	{
	public class ParsedClassPrototype
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: SectionType
		 * 
		 * BeforeParents - The prototype prior to the parents.  If there are no parents, this will be the entire prototype.
		 *	Parent - An individual parent.  This will not include separators.
		 *	AfterParents - The prototype after the parents.
		 */
		public enum SectionType : byte
			{  BeforeParents, Parent, AfterParents  }



		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: ParsedClassPrototype
		 * Creates a new parsed prototype.
		 */
		public ParsedClassPrototype (Tokenizer prototype)
			{
			tokenizer = prototype;
			sections = null;
			}


		/* Function: GetName
		 * Gets the bounds of the class name, or returns false if it couldn't find it.
		 */
		public bool GetName (out TokenIterator start, out TokenIterator end)
			{
			return GetTokensInSection(SectionType.BeforeParents, 0, ClassPrototypeParsingType.Name,
												 out start, out end);
			}


		/* Function: GetKeyword
		 * Gets the bounds of the class keyword, such as "class", "struct", or "interface", or returns false if it couldn't find it.
		 */
		public bool GetKeyword (out TokenIterator start, out TokenIterator end)
			{
			return GetTokensInSection(SectionType.BeforeParents, 0, ClassPrototypeParsingType.Keyword,
												 out start, out end);
			}


		/* Function: GetModifiers
		 * Gets the bounds of any modifiers to the class, such as "static" or "public", or returns false if there aren't any.
		 */
		public bool GetModifiers (out TokenIterator start, out TokenIterator end)
			{
			return GetTokensInSection(SectionType.BeforeParents, 0, ClassPrototypeParsingType.Modifier,
												 out start, out end);
			}


		/* Function: GetAccessLevel
		 * Returns the <Languages.AccessLevel> if it can be determined by the prototype.  This should only be used with basic language
		 * support as it will not be as reliable as the dedicated language parser.
		 */
		public Languages.AccessLevel GetAccessLevel ()
			{
			Languages.AccessLevel accessLevel = Languages.AccessLevel.Unknown;

			TokenIterator iterator, end;
			if (GetModifiers(out iterator, out end) == false)
				{  return accessLevel;  }

			bool previousWasUnderscore = false;

			while (iterator < end)
				{
				if (iterator.FundamentalType == FundamentalType.Text &&
					 iterator.ClassPrototypeParsingType == ClassPrototypeParsingType.Modifier &&
					 previousWasUnderscore == false)
					{
					if (iterator.MatchesToken("public"))
						{  accessLevel = Languages.AccessLevel.Public;  }
					else if (iterator.MatchesToken("private"))
						{  accessLevel = Languages.AccessLevel.Private;  }
					else if (iterator.MatchesToken("protected"))
						{
						if (accessLevel == Languages.AccessLevel.Internal)
							{  accessLevel = Languages.AccessLevel.ProtectedInternal;  }
						else
							{  accessLevel = Languages.AccessLevel.Protected;  }
						}
					else if (iterator.MatchesToken("internal"))
						{
						if (accessLevel == Languages.AccessLevel.Protected)
							{  accessLevel = Languages.AccessLevel.ProtectedInternal;  }
						else
							{  accessLevel = Languages.AccessLevel.Internal;  }
						}
					}
				else
					{
					previousWasUnderscore = (iterator.Character == '_');
					}

				iterator.Next();
				}

			return accessLevel;
			}


		/* Function: GetTemplateSuffix
		 * Gets the bounds of the template suffix attached to the class name, such as "<T>" in "List<T>", or returns false if there isn't one.
		 */
		public bool GetTemplateSuffix (out TokenIterator start, out TokenIterator end)
			{
			return GetTokensInSection(SectionType.BeforeParents, 0, ClassPrototypeParsingType.TemplateSuffix,
												 out start, out end);
			}


		/* Function: GetPostModifiers
		 * Gets the bounds of any modifiers that appear after the class name and parents, or returns false if there aren't any.
		 */
		public bool GetPostModifiers (out TokenIterator start, out TokenIterator end)
			{
			return GetTokensInSection(SectionType.AfterParents, 0, ClassPrototypeParsingType.PostParentModifier,
												 out start, out end);
			}


		/* Function: GetParent
		 * Gets the bounds of the numbered parent, or returns false if it doesn't exist.  Numbers start at zero.
		 */
		public bool GetParent (int index, out TokenIterator start, out TokenIterator end)
			{
			return GetSectionBounds(SectionType.Parent, index, out start, out end);
			}


		/* Function: GetParentName
		 * Gets the bounds of the parent's name, or returns false if it couldn't find it.
		 */
		public bool GetParentName (int index, out TokenIterator start, out TokenIterator end)
			{
			return GetTokensInSection(SectionType.Parent, index, ClassPrototypeParsingType.Name,
												 out start, out end);
			}


		/* Function: GetParentModifiers
		 * Gets the bounds of the parent's modifiers, such as "public", or returns false if it couldn't find any.
		 */
		public bool GetParentModifiers (int index, out TokenIterator start, out TokenIterator end)
			{
			return GetTokensInSection(SectionType.Parent, index, ClassPrototypeParsingType.Modifier,
												 out start, out end);
			}


		/* Function: GetParentTemplateSuffix
		 * Gets the bounds of the parent's template suffix, or returns false if it couldn't find one.
		 */
		public bool GetParentTemplateSuffix (int index, out TokenIterator start, out TokenIterator end)
			{
			return GetTokensInSection(SectionType.Parent, index, ClassPrototypeParsingType.TemplateSuffix,
												 out start, out end);
			}


		/* Function: CalculateSections
		 */
		protected void CalculateSections ()
			{
			sections = new List<Section>();


			// Before Parents

			TokenIterator iterator = tokenizer.FirstToken;
			iterator.NextPastWhitespace();

			TokenIterator startOfSection = iterator;

			Section section = new Section();
			section.Type = SectionType.BeforeParents;
			section.StartIndex = startOfSection.TokenIndex;

			while (iterator.IsInBounds && 
					iterator.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfParents &&
					iterator.ClassPrototypeParsingType != ClassPrototypeParsingType.PostParentModifier &&
					iterator.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfBody)
				{  iterator.Next();  }

			TokenIterator lookbehind = iterator;
			lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfSection);

			section.EndIndex = lookbehind.TokenIndex;
			sections.Add(section);


			// Parents

			if (iterator.ClassPrototypeParsingType == ClassPrototypeParsingType.StartOfParents)
				{
				do
					{  iterator.Next();  }
				while (iterator.ClassPrototypeParsingType == ClassPrototypeParsingType.StartOfParents);

				iterator.NextPastWhitespace();

				while (iterator.IsInBounds &&
						 iterator.ClassPrototypeParsingType != ClassPrototypeParsingType.PostParentModifier &&
						 iterator.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfBody)
					{
					startOfSection = iterator;

					section = new Section();
					section.Type = SectionType.Parent;
					section.StartIndex = startOfSection.TokenIndex;

					while (iterator.IsInBounds &&
							 iterator.ClassPrototypeParsingType != ClassPrototypeParsingType.ParentSeparator &&
							 iterator.ClassPrototypeParsingType != ClassPrototypeParsingType.PostParentModifier &&
							 iterator.ClassPrototypeParsingType != ClassPrototypeParsingType.StartOfBody)
						{  iterator.Next();  }

					lookbehind = iterator;
					lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfSection);

					section.EndIndex = lookbehind.TokenIndex;
					sections.Add(section);

					if (iterator.ClassPrototypeParsingType == ClassPrototypeParsingType.ParentSeparator)
						{
						do
							{  iterator.Next();  }
						while (iterator.ClassPrototypeParsingType == ClassPrototypeParsingType.ParentSeparator);

						iterator.NextPastWhitespace();
						}
					}
				}


			// After Parents

			if (iterator.IsInBounds)
				{
				section = new Section();
				section.Type = SectionType.AfterParents;
				section.StartIndex = iterator.TokenIndex;

				startOfSection = iterator;
				iterator = tokenizer.LastToken;
				iterator.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, startOfSection);

				section.EndIndex = iterator.TokenIndex;
				sections.Add(section);
				}
			}


		/* Function: GetTokensInSection
		 * Returns the bounds of the first stretch of tokens of the specified type appearing within the passed section.
		 */
		 protected bool GetTokensInSection (SectionType sectionType, int sectionIndex, ClassPrototypeParsingType tokenType,
														  out TokenIterator start, out TokenIterator end)
			{
			TokenIterator sectionStart, sectionEnd;
			GetSectionBounds(sectionType, sectionIndex, out sectionStart, out sectionEnd);

			start = sectionStart;

			while (start.IsInBounds &&
					 start < sectionEnd &&
					 start.ClassPrototypeParsingType != tokenType)
				{  start.Next();  }

			end = start;

			while (end < sectionEnd &&
					 end.ClassPrototypeParsingType == tokenType)
				{  end.Next();  }

			return (end > start);
			}


		/* Function: GetSectionBounds
		 * Returns the bounds of the passed section and whether it exists.  An index of zero represents the first section of that
		 * type, 1 represents the second, etc.
		 */
		protected bool GetSectionBounds (SectionType type, int index, out TokenIterator start, out TokenIterator end)
			{
			Section section = FindSection(type, index);

			if (section == null)
				{
				start = tokenizer.LastToken;
				end = start;
				return false;
				}
			else
				{
				start = tokenizer.FirstToken;
				start.Next(section.StartIndex);

				end = start;
				end.Next(section.EndIndex - section.StartIndex);

				return true;
				}
			}


		/* Function: FindSection
		 * Returns the first section with the passed type, or if you passed an index, the nth section with that type.  If there are
		 * none it will return null.
		 */
		protected Section FindSection (SectionType type, int index = 0)
			{
			if (sections == null)
				{  CalculateSections();  }

			foreach (Section section in sections)
				{
				if (section.Type == type)
					{
					if (index == 0)
						{  return section;  }
					else
						{  index--;  }
					}
				}

			return null;
			}
			

		/* Function: CountSections
		 * Returns the number of sections with the passed type.
		 */
		protected int CountSections (SectionType type)
			{
			if (sections == null)
				{  CalculateSections();  }

			int count = 0;

			foreach (Section section in sections)
				{
				if (section.Type == type)
					{  count++;  }
				}

			return count;
			}



		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Tokenizer
		 * The tokenized prototype.
		 */
		public Tokenizer Tokenizer
			{
			get
				{  return tokenizer;  }
			}


		/* Property: NumberOfParents
		 */
		public int NumberOfParents
			{
			get
				{  
				return CountSections(SectionType.Parent);
				}
			}


		
		// Group: Variables
		// __________________________________________________________________________
		
		/* var: tokenizer
		 * The <Tokenizer> containing the full prototype.
		 */
		protected Tokenizer tokenizer;

		/* var: sections
		 * A list of <Sections> representing chunks of the prototype, or null if it hasn't been calculated yet.
		 */
		protected List<Section> sections;



		/* ___________________________________________________________________________
		 * 
		 * Class: GregValure.NaturalDocs.Engine.ParsedClassPrototype.Section
		 * ___________________________________________________________________________
		 */
		protected class Section
			{
			public Section ()
				{
				StartIndex = 0;
				EndIndex = 0;
				Type = SectionType.BeforeParents;
				}
			
			public int StartIndex;
			public int EndIndex;
			public SectionType Type;
			}
		}
	}