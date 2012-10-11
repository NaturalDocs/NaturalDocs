/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Symbols;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: BuildClassFile
		 * Builds an output file based on a class.  The accessor should NOT hold a lock on the database.  This will also
		 * build the metadata files.
		 */
		protected void BuildClassFile (int classID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			#if DEBUG
			if (accessor.LockHeld != CodeDB.Accessor.LockType.None)
				{  throw new Exception ("Shouldn't call BuildClassFile() when the accessor already holds a database lock.");  }
			#endif

			accessor.GetReadOnlyLock();
			bool haveDBLock = true;

			try
				{
				ClassString classString = accessor.GetClassByID(classID);
				IList<Topic> topics = accessor.GetTopicsInClass(classID, cancelDelegate);
				
				if (cancelDelegate())
					{  return;  }
				
					
				// Delete the file if there are no topics.

				if (topics.Count == 0)
				   {
				   accessor.ReleaseLock();
				   haveDBLock = false;
					
				   DeleteOutputFileIfExists(Class_OutputFile(classString));
				   DeleteOutputFileIfExists(Class_ToolTipsFile(classString));
				   DeleteOutputFileIfExists(Class_SummaryFile(classString));
				   DeleteOutputFileIfExists(Class_SummaryToolTipsFile(classString));

				   lock (writeLock)
				      {
				      if (classFilesWithContent.Remove(classID) == true)
				         {  buildFlags |= BuildFlags.BuildMenu;  }
				      }
				   }

				
				// Build the file if it has topics
				
				else
				   {

				   // Get links and their targets

					// We can't skip looking up classes and contexts here.  Later code will be trying to compare generated 
					// links to the ones in this list and that requires them having all their properties.
				   IList<Link> links = accessor.GetLinksInClass(classID, cancelDelegate);

				   if (cancelDelegate())
				      {  return;  }

				   IDObjects.SparseNumberSet linkTargetIDs = new IDObjects.SparseNumberSet();

				   foreach (Link link in links)
				      {
				      if (link.IsResolved)
				         {  linkTargetIDs.Add(link.TargetTopicID);  }
				      }

				   IList<Topic> linkTargets = accessor.GetTopicsByID(linkTargetIDs, cancelDelegate);

				   if (cancelDelegate())
				      {  return;  }

					// We also need to get any links appearing inside the link targets.  Wut?  When you have a resolved link, 
					// a tooltip shows up when you hover over it.  The tooltip is built from the link targets we just retrieved.  
					// However, if the summary appearing in the tooltip contains any Natural Docs links, we need to know if
					// they're resolved and how to know what text to show (originaltext, named links, etc.)  Links don't store
					// which topic they appear in, but they do store the file, so gather the file IDs of the link targets that
					// have Natural Docs links in the summaries and get all the links in those files.

					// Links also store which class they appear in, so why not do this by class instead of by file?  Because a 
					// link could be to something global, and the global scope could potentially have a whole hell of a lot of 
					// content, depending on the project and language.  While there can also be some epically long files, the
					// chances of that are less on average so we stick with doing this by file.

					IDObjects.SparseNumberSet inceptionFileIDs = new IDObjects.SparseNumberSet();

					foreach (Topic linkTarget in linkTargets)
					   {
					   if (linkTarget.Summary != null && linkTarget.Summary.IndexOf("<link type=\"naturaldocs\"") != -1)
					      {  inceptionFileIDs.Add(linkTarget.FileID);  }
					   }

					IList<Link> inceptionLinks = null;
					
					if (!inceptionFileIDs.IsEmpty)
					   {  
					   // Can't skip looking up classes and contexts here either.
					   inceptionLinks = accessor.GetNaturalDocsLinksInFiles(inceptionFileIDs, cancelDelegate);  
					   }

				   if (cancelDelegate())
				      {  return;  }

				   accessor.ReleaseLock();
				   haveDBLock = false;


					// xxx resort list of topics, since the file order is random
					// xxx also pick out the title topic
					// xxx also condense duplicates, like for partial classes and header/source files
					// xxx may need groups to separate where the files join


				   // Build the HTML for the list of topics

				   StringBuilder html = new StringBuilder("\r\n\r\n");
				   HTMLTopic topicBuilder = new HTMLTopic(this);

				   // We don't put embedded topics in the output, so we need to find the last non-embedded one so
				   // that the "last" CSS tag is correctly applied.
				   int lastNonEmbeddedTopic = topics.Count - 1;
				   while (lastNonEmbeddedTopic > 0 && topics[lastNonEmbeddedTopic].IsEmbedded == true)
				      {  lastNonEmbeddedTopic--;  }

				   for (int i = 0; i <= lastNonEmbeddedTopic; i++)
				      {  
				      string extraClass = null;

				      if (i == 0)
				         {  extraClass = "first";  }
				      else if (i == lastNonEmbeddedTopic)
				         {  extraClass = "last";  }

				      if (topics[i].IsEmbedded == false)
				         {
							// xxx needs to prefer class links if possible, since these will go to files
				         topicBuilder.Build(topics[i], links, linkTargets, html, topics, i + 1, extraClass);  
				         html.Append("\r\n\r\n");
				         }
				      }
							

				   // Build the full HTML files

				   Path outputPath = Class_OutputFile(classString);

				   // Can't get this from outputPath because it may have substituted characters to satisfy the path restrictions.
				   string title = classString.ToString(); // xxx get from title topic

				   BuildFile(outputPath, title, html.ToString(), PageType.Content);


				   // Build the tooltips file

				   using (System.IO.StreamWriter file = CreateTextFileAndPath(Class_ToolTipsFile(classString)))
				      {
				      file.Write("NDContentPage.OnToolTipsLoaded({");

				      #if DONT_SHRINK_FILES
				         file.WriteLine();
				      #endif

				      for (int i = 0; i < linkTargets.Count; i++)
				         {
				         Topic topic = linkTargets[i];
				         string toolTipHTML = topicBuilder.BuildToolTip(topic, inceptionLinks);

				         if (toolTipHTML != null)
				            {
				            #if DONT_SHRINK_FILES
				               file.Write("   ");
				            #endif

				            file.Write(topic.TopicID);
				            file.Write(":\"");
				            file.Write(toolTipHTML.StringEscape());
				            file.Write('"');

				            if (i != linkTargets.Count - 1)
				               {  file.Write(',');  }

					           #if DONT_SHRINK_FILES
				               file.WriteLine();
								#endif
								}
							}

						#if DONT_SHRINK_FILES
							file.Write("   ");
						#endif
				      file.Write("});");
				      }

				   // Build summary and summary tooltips metadata files

				   HTMLSummary summaryBuilder = new HTMLSummary(this);
				   summaryBuilder.Build(topics, links, title, 
				                               Class_OutputFileHashPath(classString), Class_SummaryFile(classString), 
														 Class_SummaryToolTipsFile(classString));

				   lock (writeLock)
				      {
				      if (classFilesWithContent.Add(classID) == true)
				         {  buildFlags |= BuildFlags.BuildMenu;  }
				      }
				   }
				}
				
			finally
				{ 
				if (haveDBLock)
					{  accessor.ReleaseLock();  }
				}
			}



		// Group: Path Functions
		// __________________________________________________________________________


		/* Function: Class_OutputFolder
		 * 
		 * Returns the output folder for class files, optionally for the passed language and partial symbol within it.
		 * 
		 * - If language isn't specified, it returns the output folder for all class files.
		 * - If language is specified but the symbol is not, it returns the output folder for all class files of that language.
		 * - If language and partial symbol are specified, it returns the output folder for that symbol.
		 */
		public Path Class_OutputFolder (Language language = null, SymbolString partialSymbol = default(SymbolString))
			{
			StringBuilder result = new StringBuilder(OutputFolder);
			result.Append("/classes");  

			if (language != null)
				{
				result.Append('/');
				result.Append(language.SimpleIdentifier);
					
				if (partialSymbol != null)
					{
					result.Append('/');
					string pathString = partialSymbol.FormatWithSeparator('/');
					result.Append(SanitizePath(pathString));
					}
				}

			return result.ToString();
			}


		/* Function: Class_OutputFolderHashPath
		 * Returns the hash path of the output folder for class files, optionally for the passed language and partial symbol 
		 * within.  The hash path will always include a trailing symbol so that the file name can simply be concatenated.
		 * 
		 * - If language isn't specified, it returns null since there is no common prefix for all class paths.
		 * - If language is specified but the symbol is not, it returns the prefix for all class paths of that language.
		 * - If language and partial symbol are specified, it returns the hash path for that symbol.
		 */
		public string Class_OutputFolderHashPath (Language language = null, SymbolString partialSymbol = default(SymbolString))
			{
			if (language == null)
				{  return null;  }

			StringBuilder result = new StringBuilder();

			result.Append(language.SimpleIdentifier);
			result.Append("Class:");

			if (partialSymbol != null)
				{
				string memberOperator = language.MemberOperator;

				// We only support :: and . in hash paths.  Default to . for anything else.
				if (memberOperator != "::")
					{  memberOperator = ".";  }

				string pathString = partialSymbol.FormatWithSeparator(memberOperator);
				result.Append(SanitizePath(pathString));
				result.Append(memberOperator);
				}

			return result.ToString();
			}


		/* Function: Class_OutputFile
		 * Returns the path of the output file generated for the passed class.
		 */
		public Path Class_OutputFile (ClassString classString)
			{
			var language = Engine.Instance.Languages.FromID(classString.LanguageID);

			return Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + 
						Class_OutputFileNameOnly(classString);
			}


		/* Function: Class_OutputFileHashPath
		 * Returns the hash path of the passed class.
		 */
		public string Class_OutputFileHashPath (ClassString classString)
			{
			var language = Engine.Instance.Languages.FromID(classString.LanguageID);

			// OutputFolderHashPath already includes the trailing separator so we can just concatenate them.
			return Class_OutputFolderHashPath(language, classString.Symbol.WithoutLastSegment) +
						Class_OutputFileNameOnlyHashPath(classString);
			}


		/* Function: Class_OutputFileNameOnly
		 * Returns the output file name of the passed class.  Any scope attached to it will be ignored and not included in 
		 * the result.
		 */
		public Path Class_OutputFileNameOnly (ClassString classString)
			{
			string nameString = classString.Symbol.LastSegment;
			return SanitizePath(nameString, true) + ".html";
			}


		/* Function: Class_OutputFileNameOnlyHashPath
		 * Returns the hash path of the passed class.  Any scope attached to it will be ignored and not included in the result.
		 */
		public string Class_OutputFileNameOnlyHashPath (ClassString classString)
			{
			string nameString = classString.Symbol.LastSegment;
			return SanitizePath(nameString);
			}


		/* Function: Class_ToolTipsFile
		 * Returns the tooltips file path of the output file generated for the passed class.
		 */
		public Path Class_ToolTipsFile (ClassString classString)
			{
			var language = Engine.Instance.Languages.FromID(classString.LanguageID);

			return Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + 
						Class_ToolTipsFileNameOnly(classString);
			}


		/* Function: Class_ToolTipsFileNameOnly
		 * Returns the tooltips file name of the passed class.  Any scope attached to it will be ignored and not included 
		 * in the result.
		 */
		public Path Class_ToolTipsFileNameOnly (ClassString classString)
			{
			string nameString = classString.Symbol.LastSegment;
			return SanitizePath(nameString, true) + "-ToolTips.js";
			}


		/* Function: Class_SummaryFile
		 * Returns the summary file path of the passed class.
		 */
		public Path Class_SummaryFile (ClassString classString)
			{
			var language = Engine.Instance.Languages.FromID(classString.LanguageID);

			return Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + 
						Class_SummaryFileNameOnly(classString);
			}


		/* Function: Class_SummaryFileNameOnly
		 * Returns the summary file name of the class.  Any scope attached to it will be ignored and not included in the result.
		 */
		public Path Class_SummaryFileNameOnly (ClassString classString)
			{
			string nameString = classString.Symbol.LastSegment;
			return SanitizePath(nameString, true) + "-Summary.js";
			}


		/* Function: Class_SummaryToolTipsFile
		 * Returns the summary tooltips file path of the passed class.
		 */
		public Path Class_SummaryToolTipsFile (ClassString classString)
			{
			var language = Engine.Instance.Languages.FromID(classString.LanguageID);

			return Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + 
						Class_SummaryToolTipsFileNameOnly(classString);
			}


		/* Function: Class_SummaryToolTipsFileNameOnly
		 * Returns the summary tooltips file name of the passed class.  Any scope attached to it will be ignored and not 
		 * included in the result.
		 */
		public Path Class_SummaryToolTipsFileNameOnly (ClassString classString)
			{
			string nameString = classString.Symbol.LastSegment;
			return SanitizePath(nameString, true) + "-SummaryToolTips.js";
			}

		}
	}

