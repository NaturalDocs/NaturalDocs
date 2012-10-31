/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.Components.HTMLTopicPage
 * ____________________________________________________________________________
 * 
 * A base class for components that build a page of <Topics> for <Output.Builders.HTML>.
 * 
 * 
 * Topic: Usage
 * 
 *		- Create a new HTMLTopicPage.
 *		- Call <Build()>.
 *		- Unlike other components, this object cannot be reused for other locations.
 *		
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.  It has an internal state that is used during a call to
 *		<Build()>, and another <Build()> should not be started until it's completed.  Instead each thread should create its 
 *		own object.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Output.Builders.Components
	{
	public abstract class HTMLTopicPage
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: HTMLTopicPage
		 * Creates a new HTMLTopicPage object.  The accessor may or may not already have a lock on the database.
		 */
		public HTMLTopicPage (Builders.HTML htmlBuilder, CodeDB.Accessor accessor)
			{
			this.htmlBuilder = htmlBuilder;
			this.accessor = accessor;
			}


		/* Function: Build
		 * 
		 * Builds the page and its supporting JSON files.  Returns whether there was any content.  It will also return false
		 * if it was interrupted by the <CancelDelegate>.
		 * 
		 * If the <CodeDB.Accessor> passed to the constructor didn't have a lock, this function will automatically acquire 
		 * and release a read-only lock.  This is the preferred way of using this function as the lock will only be held during
		 * the data querying stage and will be released before writing output to disk.  If a lock was already held it will use
		 * it and not release it.
		 */
		public bool Build (CancelDelegate cancelDelegate)
			{
			bool releaseDBLock = false;

			if (accessor.LockHeld == CodeDB.Accessor.LockType.None)
				{  
				accessor.GetReadOnlyLock();
				releaseDBLock = true;
				}

			try
				{
				List<Topic> topics = GetTopics(cancelDelegate);
				
				if (topics.Count == 0 || cancelDelegate())
					{  return false;  }
				
					
				// Get links and their targets

				// We can't skip looking up classes and contexts here.  Later code will be trying to compare generated 
				// links to the ones in this list and that requires them having all their properties.
				List<Link> links = GetLinks(cancelDelegate);

				if (cancelDelegate())
					{  return false;  }

				IDObjects.SparseNumberSet linkTargetIDs = new IDObjects.SparseNumberSet();

				foreach (Link link in links)
					{
					if (link.IsResolved)
						{  linkTargetIDs.Add(link.TargetTopicID);  }
					}

				IList<Topic> linkTargets = accessor.GetTopicsByID(linkTargetIDs, cancelDelegate);

				if (cancelDelegate())
					{  return false;  }

				// We also need to get any links appearing inside the link targets.  Wut?  When you have a resolved link, 
				// a tooltip shows up when you hover over it.  The tooltip is built from the link targets we just retrieved.  
				// However, if the summary appearing in the tooltip contains any Natural Docs links, we need to know if
				// they're resolved and how to know what text to show (originaltext, named links, etc.)  Links don't store
				// which topic they appear in, but they do store the file, so gather the file IDs of the link targets that
				// have Natural Docs links in the summaries and get all the links in those files.

				// Links also store which class they appear in, so why not do this by class instead of by file?  Because a 
				// link could be to something global, and the global scope could potentially have a whole hell of a lot of 
				// content, depending on the project and language.  While there can also be some really long files, the
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
					{  return false;  }

				if (releaseDBLock)
					{
					accessor.ReleaseLock();
					releaseDBLock = false;
					}


				// Build the HTML for the list of topics

				StringBuilder html = new StringBuilder("\r\n\r\n");
				HTMLTopic topicBuilder = new HTMLTopic(htmlBuilder);

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
						topicBuilder.Build(topics[i], links, linkTargets, html, topics, i + 1, extraClass);  
						html.Append("\r\n\r\n");
						}
					}
							

				// Build the full HTML file

				htmlBuilder.BuildFile(OutputFile, PageTitle, html.ToString(), Builders.HTML.PageType.Content);


				// Build the tooltips file

				using (System.IO.StreamWriter file = htmlBuilder.CreateTextFileAndPath(ToolTipsFile))
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


				// Build summary and summary tooltips files

				JSSummaryData summaryBuilder = new JSSummaryData(htmlBuilder);
				summaryBuilder.Build(topics, links, PageTitle, OutputFileHashPath, SummaryFile, SummaryToolTipsFile);

				return true;
				}
				
			finally
				{ 
				if (releaseDBLock)
					{  accessor.ReleaseLock();  }
				}
			}



		// Group: Abstract Functions
		// __________________________________________________________________________


		/* Function: GetTopics
		 * 
		 * Retrieves the <Topics> for the page's location.  If there are no topics it will return an empty list.
		 * 
		 * When implementing this function note that the class's accessor may or may not already have a lock.
		 */
		public abstract List<Topic> GetTopics (CancelDelegate cancelDelegate);


		/* Function: GetLinks
		 * 
		 * Retrieves the <Links> appearing in the page's location.  If there are no links it will return an empty list.
		 * 
		 * When implementing this function note that the class's accessor may or may not already have a lock.
		 */
		public abstract List<Link> GetLinks (CancelDelegate cancelDelegate);



		// Group: Abstract Properties
		// __________________________________________________________________________


		/* Property: PageTitle
		 */
		abstract public string PageTitle
			{  get;  }

		/* Property: OutputFile
		 */
		abstract public Path OutputFile
		   {  get;  }

		/* Property: OutputFileHashPath
		 */
		abstract public string OutputFileHashPath
			{  get;  }

		/* Property: ToolTipsFile
		 */
		abstract public Path ToolTipsFile
		   {  get;  }

		/* Property: SummaryFile
		 */
		abstract public Path SummaryFile
		   {  get;  }

		/* Property: SummaryToolTipsFile
		 */
		abstract public Path SummaryToolTipsFile
		   {  get;  }



		// Group: Variables
		// __________________________________________________________________________


		/* var: htmlBuilder
		 * The <Builders.HTML> associated with this object.
		 */
		protected Builders.HTML htmlBuilder;

		/* var: accessor
		 */
		protected CodeDB.Accessor accessor;

		}
	}

