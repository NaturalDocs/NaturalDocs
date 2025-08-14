/*
 * Class: CodeClear.NaturalDocs.Tests.TestFolderConfig
 * ____________________________________________________________________________
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine;


namespace CodeClear.NaturalDocs.Tests
	{
	public partial class TestFolderConfig
		{

		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: GetTestRunner
		 * Creates a <TestRunner> object for the passed test type, as found in <Test Folder.txt>.  Returns null if it couldn't find one.
		 */
		static public TestRunner GetTestRunner (string testType)
			{
			string lcTestType = testType.ToLowerInvariant();

			switch (lcTestType)
				{
				case "numberset":
					return new TestRunners.NumberSet();
				case "version strings":
					return new TestRunners.VersionStrings();
				case "javadoc iterator":
					return new TestRunners.JavadocIterator();
				case "xml iterator":
					return new TestRunners.XMLIterator();
				case "link interpretations":
					return new TestRunners.LinkInterpretations();
				case "link scoring":
					return new TestRunners.LinkScoring();
				case "attribute inheritance":
					return new TestRunners.AttributeInheritance();
				case "class prototype parsing":
					return new TestRunners.ClassPrototypeParsing();
				case "comment merging":
					return new TestRunners.CommentMerging();
				case "comment type detection":
					return new TestRunners.CommentTypeDetection();
				case "comment types and symbols":
					return new TestRunners.CommentTypesAndSymbols();
				case "enums":
					return new TestRunners.Enums();
				case "grouping":
					return new TestRunners.Grouping();
				case "language detection":
					return new TestRunners.LanguageDetection();
				case "ndmarkup":
					return new TestRunners.NDMarkup();
				case "prototype detection":
					return new TestRunners.PrototypeDetection();
				case "prototype parsing":
					return new TestRunners.PrototypeParsing();
				case "search keywords":
					return new TestRunners.SearchKeywords();
				case "summaries":
					return new TestRunners.Summaries();
				case "symbols":
					return new TestRunners.Symbols();
				case "topic bodies":
					return new TestRunners.TopicBodies();
				case "language parsing":
					return new TestRunners.LanguageParsing();
				case "comment detection":
					return new TestRunners.CommentDetection();

				case "html anchors":
					return new TestRunners.HTMLAnchors();
				case "html class prototypes":
					return new TestRunners.HTMLClassPrototypes();
				case "html code sections":
					return new TestRunners.HTMLCodeSections();
				case "html links":
					return new TestRunners.HTMLLinks();

				default:
					return null;
				}
			}

		}
	}
