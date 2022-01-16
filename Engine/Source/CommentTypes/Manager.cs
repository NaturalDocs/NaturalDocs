/*
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.Manager
 * ____________________________________________________________________________
 *
 * A module to handle <Comments.txt> and all the comment type settings within Natural Docs.
 *
 *
 * Topic: Usage
 *
 *		- Call <Engine.Instance.Start()> which will start this module.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.CommentTypes
	{
	public partial class Manager : Module
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Manager
		 */
		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			config = null;
			groupCommentTypeID = 0;

			systemTextConfig = null;
			projectTextConfig = null;
			mergedTextConfig = null;
			lastRunConfig = null;
			}


		protected override void Dispose (bool strictRulesApply)
			{
			}



		// Group: Information Functions
		// __________________________________________________________________________


		/* Function: FromKeyword
		 * Returns the <CommentType> associated with the passed keyword, or null if none.  The language ID should be set to
		 * the language the keyword appears in, though it can also be zero to only return comment types from language agnostic
		 * keywords.
		 */
		public CommentType FromKeyword (string keyword, int languageID)
			{
			return config.CommentTypeFromKeyword(keyword, languageID);
			}

		/* Function: FromKeyword
		 * Returns the <CommentType> associated with the passed keyword, or null if none.  Also returns whether it was singular
		 * or plural.  The language ID should be set to the language the keyword appears in, though it can also be zero to only return
		 * comment types from language agnostic keywords.
		 */
		public CommentType FromKeyword (string keyword, int languageID, out bool plural)
			{
			return config.CommentTypeFromKeyword(keyword, languageID, out plural);
			}

		/* Function: FromName
		 * Returns the <CommentType> associated with the passed name, or null if none.
		 */
		public CommentType FromName (string name)
			{
			return config.CommentTypeFromName(name);
			}

		/* Function: FromID
		 * Returns the <CommentType> associated with the passed ID, or null if none.
		 */
		public CommentType FromID (int commentTypeID)
			{
			return config.CommentTypeFromID(commentTypeID);
			}

		/* Function: IDFromKeyword
		 * Returns the comment type ID associated with the passed keyword, or zero if none.
		 */
		public int IDFromKeyword (string keyword, int languageID)
			{
			var keywordDefinition = config.KeywordDefinition(keyword, languageID);

			if (keywordDefinition == null)
				{  return 0;  }
			else
				{  return keywordDefinition.CommentTypeID;  }
			}

		/* Function: TagFromName
		 * Returns the <Tag> associated with the passed name, or null if none.
		 */
		public Tag TagFromName (string name)
			{
			return config.TagFromName(name);
			}

		/* Function: TagFromID
		 * Returns the <Tag> associated with the passed ID, or null if none.
		 */
		public Tag TagFromID (int tagID)
			{
			return config.TagFromID(tagID);
			}



		// Group: Convenience Functions
		// __________________________________________________________________________


		/* Function: InClassHierarchy
		 * Whether the passed comment type is part of the class hierarchy.
		 */
		public bool InClassHierarchy (CommentType commentType)
			{
			return (commentType.HierarchyID == EngineInstance.Hierarchies.ClassHierarchyID);
			}

		/* Function: InClassHierarchy
		 * Whether the passed comment type ID is part of the class hierarchy.
		 */
		public bool InClassHierarchy (int commentTypeID)
			{
			return InClassHierarchy( FromID(commentTypeID) );
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: GroupCommentTypeID
		 * The ID of the "group" keyword, or zero if it isn't defined.
		 */
		public int GroupCommentTypeID
			{
			get
				{  return groupCommentTypeID;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: config
		 * The final configuration to use.
		 */
		protected Config config;

		/* var: groupCommentTypeID
		 * The ID of the "group" keyword, or zero if it's not defined.
		 */
		protected int groupCommentTypeID;



		// Group: Temporary Initialization Variables
		// __________________________________________________________________________
		//
		// These variables are used to store data between <Start_Stage1()> and <Start_Stage2()>.  They are not used
		// afterwards.
		//


		/* var: systemTextConfig
		 * The <ConfigFiles.TextFile> representing the system <Comments.txt>.  This is only stored between <Start_Stage1()>
		 * and <Start_Stage2()>.  It will be null afterwards.
		 */
		protected ConfigFiles.TextFile systemTextConfig;

		/* var: projectTextConfig
		 * The <ConfigFiles.TextFile> representing the project <Comments.txt>.  This is only stored between <Start_Stage1()>
		 * and <Start_Stage2()>.  It will be null afterwards.
		 */
		protected ConfigFiles.TextFile projectTextConfig;

		/* var: mergedTextConfig
		 * A <ConfigFiles.TextFile> representing the merger of <systemTextConfig> and <projectTextConfig>, sans keywords.
		 * This is only stored between <Start_Stage1()> and <Start_Stage2()>.  It will be null afterwards.
		 */
		protected ConfigFiles.TextFile mergedTextConfig;

		/* var: lastRunConfig
		 * The <Config> representing the contents of <Comments.nd>.  This is only stored between <Start_Stage1()> and
		 * <Start_Stage2()>.  It will be null afterwards.
		 */
		protected Config lastRunConfig;

		}
	}
