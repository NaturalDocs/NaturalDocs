/*
 * Class: CodeClear.NaturalDocs.Engine.Comments.Components.SectionedComment
 * ____________________________________________________________________________
 *
 * A class to manage the generated output for comments that can be divided into named sections.  This is used primarily
 * for Javadoc and XML comments.  For example, all @param lines in Javadoc will be condensed into a single "Parameters"
 * section.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Comments.Components
	{
	public class SectionedComment
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: SectionedComment
		 */
		public SectionedComment ()
			{
			sections = new List<Section>();
			}


		/* Function: GetOrCreateTextSection
		 * Returns a <TextSection> associated with the passed name.  If the last section in the comment has the same name
		 * it will be returned so new content can be appended to it.  If it doesn't this will create a new section, add it to the
		 * end of the list and return it.
		 */
		public TextSection GetOrCreateTextSection (string name)
			{
			if (sections.Count > 0 &&
				sections[sections.Count - 1] is TextSection &&
				sections[sections.Count - 1].Name == name)
				{  return (sections[sections.Count - 1] as TextSection);  }
			else
				{
				TextSection newBlock = new TextSection(name);
				sections.Add(newBlock);
				return newBlock;
				}
			}


		/* Function: GetOrCreateListSection
		 * Returns a <ListSection> associated with the passed name.  If a section exists with the same name it will be returned
		 * so new items can be added to it.  If one doesn't it will add a new list section to the end of the comment and return it.
		 */
		public ListSection GetOrCreateListSection (string name)
			{
			foreach (var section in sections)
				{
				if (section is ListSection &&
					section.Name == name)
					{  return (section as ListSection);  }
				}

			ListSection newSection = new ListSection(name);
			sections.Add(newSection);
			return newSection;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Sections
		 * The complete list of sections in the comment.
		 */
		public List<Section> Sections
			{
			get
				{  return sections;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		protected List<Section> sections;




		/* __________________________________________________________________________
		 *
		 * Class: CodeClear.NaturalDocs.Engine.Comments.Components.SectionedComment.Section
		 * __________________________________________________________________________
		 *
		 * A base class for all comment sections.
		 *
		 */
		public abstract class Section
			{
			public Section (string name)
				{
				this.Name = name;
				}

			public string Name;
			}


		/* __________________________________________________________________________
		 *
		 * Struct: CodeClear.NaturalDocs.Engine.Comments.Components.SectionedComment.TextSection
		 * __________________________________________________________________________
		 */
		public class TextSection : Section
			{
			public TextSection (string name) : base (name)
				{
				Content = new StringBuilder();
				}

			public void Append (string content)
				{
				Content.Append(content);
				}

			public StringBuilder Content;
			}


		/* __________________________________________________________________________
		 *
		 * Struct: CodeClear.NaturalDocs.Engine.Comments.Components.SectionedComment.ListSection
		 * __________________________________________________________________________
		 */
		public class ListSection : Section
			{
			public ListSection (string name) : base (name)
				{
				Members = new List<ListMember>();
				}

			public void AddMember (string name, string description)
				{
				Members.Add( new ListMember(name, description) );
				}

			public int MemberCount
				{
				get
					{  return Members.Count;  }
				}

			public bool MembersHaveNames
				{
				get
					{
					foreach (var member in Members)
						{
						if (member.Name != null)
							{  return true;  }
						}

					return false;
					}
				}

			public bool MembersHaveDescriptions
				{
				get
					{
					foreach (var member in Members)
						{
						if (member.Description != null)
							{  return true;  }
						}

					return false;
					}
				}

			public List<ListMember> Members;
			}


		/* __________________________________________________________________________
		 *
		 * Struct: CodeClear.NaturalDocs.Engine.Comments.Components.SectionedComment.ListMember
		 * __________________________________________________________________________
		 */
		public struct ListMember
			{
			public ListMember (string name, string description)
				{
				this.Name = name;
				this.Description = description;
				}

			public string Name;
			public string Description;
			}
		}
	}
