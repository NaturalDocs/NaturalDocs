/*
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.ConfigFiles.TextFile
 * ____________________________________________________________________________
 *
 * A class representing the contents of <Comments.txt>.
 *
 *
 * Multithreading: Not Thread Safe, Supports Multiple Readers
 *
 *		This object doesn't have any locking built in, and so it is up to the class managing it to provide thread safety if needed.
 *		However, it does support multiple concurrent readers.  This means it can be used in read-only mode with no locking or
 *		in read/write mode with a ReaderWriterLock.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.CommentTypes.ConfigFiles
	{
	public class TextFile
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: TextFile
		 */
		public TextFile ()
			{
			ignoredKeywordGroups = null;

			tags = null;
			tagsPropertyLocation = default;

			commentTypes = null;
			}



		// Group: Functions
		// __________________________________________________________________________


		/* Function: AddIgnoredKeywordGroup
		 * Adds a set of ignored keyword to the file.  There can be more than one.
		 */
		public void AddIgnoredKeywordGroup (TextFileKeywordGroup keywordGroup)
			{
			if (ignoredKeywordGroups == null)
				{  ignoredKeywordGroups = new List<TextFileKeywordGroup>();  }

			ignoredKeywordGroups.Add(keywordGroup);
			}


		/* Function: AddTag
		 * Adds a tag to the file.
		 */
		public void AddTag (string tag, PropertyLocation propertyLocation)
			{
			if (tags == null)
				{  tags = new List<string>();  }

			tags.Add(tag);

			// Only add the first one, ignore subsequent ones
			if (!tagsPropertyLocation.IsDefined)
				{  tagsPropertyLocation = propertyLocation;  }
			}


		/* Function: AddCommentType
		 * Adds a comment type to the file.
		 */
		public void AddCommentType (TextFileCommentType commentType)
			{
			if (commentTypes == null)
				{  commentTypes = new List<TextFileCommentType>();  }

			commentTypes.Add(commentType);
			}


		/* Function: FindCommentType
		 * Returns the comment type associated with the passed name if it's defined in this file, or null if it's not.
		 */
		public TextFileCommentType FindCommentType (string name)
			{
			if (commentTypes == null)
				{  return null;  }

			string normalizedName = name.NormalizeKey(Config.KeySettingsForCommentTypes);

			foreach (var commentType in commentTypes)
				{
				if (normalizedName == commentType.Name.NormalizeKey(Config.KeySettingsForCommentTypes))
					{  return commentType;  }
				}

			return null;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: HasIgnoredKeywords
		 * Whether any ignored keyword groups are defined.
		 */
		public bool HasIgnoredKeywords
			{
			get
				{  return (ignoredKeywordGroups != null);  }
			}


		/* Property: IgnoredKeywordGroups
		 * A list of the ignored keyword groups in the order they appear in the text file, or null if there aren't any.
		 */
		public IList<TextFileKeywordGroup> IgnoredKeywordGroups
			{
			get
				{  return ignoredKeywordGroups;  }
			}


		/* Property: HasTags
		 * Returns whether any tags are defined in this file.
		 */
		public bool HasTags
			{
			get
				{  return (tags != null);  }
			}


		/* Property: Tags
		 * A list of the tags in the order and case they appear in in the text file, or null if there aren't any.
		 */
		public IList<string> Tags
			{
			get
				{  return tags;  }
			}


		/* Property: TagsPropertyLocation
		 * The <PropertyLocation> where the tags are defined.
		 */
		public PropertyLocation TagsPropertyLocation
			{
			get
				{  return tagsPropertyLocation;  }
			}


		/* Property: HasCommentTypes
		 * Returns whether this file has any comment types defined.
		 */
		public bool HasCommentTypes
			{
			get
				{  return (commentTypes != null);  }
			}


		/* Property: CommentTypes
		 * A list <TextConfigFileCommentTypes> in the order they appear in the text file, or null if there aren't any.
		 */
		public IList<TextFileCommentType> CommentTypes
			{
			get
				{  return commentTypes;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: ignoredKeywordGroups
		 * A list of the ignored keyword groups in the order they appear in the text file, or null if there aren't any.
		 */
		protected List<TextFileKeywordGroup> ignoredKeywordGroups;

		/* var: tags
		 * A list of the tags in the order and case they appear in in the text file, or null if there aren't any.
		 */
		protected List<string> tags;

		/* var: tagsPropertyLocation
		 * The <PropertyLocation> where <tags> is defined.
		 */
		protected PropertyLocation tagsPropertyLocation;

		/* var: commentTypes
		 * A list of <TextFileCommentTypes> in the order they appear in the text file, or null if there aren't any.
		 */
		protected List<TextFileCommentType> commentTypes;

		}
	}
