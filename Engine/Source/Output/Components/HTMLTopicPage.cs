/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Components.HTMLTopicPage
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

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output.Components
	{
	public abstract class HTMLTopicPage
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: HTMLTopicPage
		 * Creates a new HTMLTopicPage object.
		 */
		public HTMLTopicPage (Builders.HTML htmlBuilder)
			{
			this.htmlBuilder = htmlBuilder;
			}


		/* Function: Build
		 * 
		 * Builds the page and its supporting JSON files.  Returns whether there was any content.  It will also return false
		 * if it was interrupted by the <CancelDelegate>.
		 * 
		 * If the <CodeDB.Accessor> doesn't have a lock, this function will automatically acquire and release a read-only lock.
		 * This is the preferred way of using this function as the lock will only be held during the data querying stage and will be 
		 * released before writing output to disk.  If it already has a lock it will use it and not release it.
		 */
		public bool Build (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			bool releaseDBLock = false;

			if (accessor.LockHeld == CodeDB.Accessor.LockType.None)
				{  
				accessor.GetReadOnlyLock();
				releaseDBLock = true;
				}

			try
				{
				// DEPENDENCY: HTMLTopicPages.Class assumes that this function will call a database function before using any path
				// properties.

				List<Topic> topics = GetTopics(accessor, cancelDelegate) ?? new List<Topic>();
				
				if (topics.Count == 0 || cancelDelegate())
					{  return false;  }
				
				List<Link> links = GetLinks(accessor, cancelDelegate) ?? new List<Link>();

				if (cancelDelegate())
					{  return false;  }


				// Find all the classes that are defined in this page, since we have to do additional lookups for class prototypes.

				IDObjects.NumberSet classIDsDefined = new IDObjects.NumberSet();

				foreach (var topic in topics)
					{
					if (topic.DefinesClass)
						{  classIDsDefined.Add(topic.ClassID);  }
					}

				if (cancelDelegate())
					{  return false;  }


				// We need the class parent links of all the classes defined on this page so the class prototypes can show the parents.
				// If this is a class page then we can skip this step since all the links should already be included.  However, for any 
				// other type of page this may not be the case.  A source file page would return all the links in that file, but the class 
				// may be defined across multiple files and we need the class parent links in all of them.  In this case we need to look 
				// up the class parent links separately by class ID.

				if ((this is HTMLTopicPages.Class) == false && classIDsDefined.IsEmpty == false)
					{
					List<Link> classParentLinks = accessor.GetClassParentLinksInClasses(classIDsDefined, cancelDelegate);

					if (classParentLinks != null && classParentLinks.Count > 0)
						{  links.AddRange(classParentLinks);  }
					}

				if (cancelDelegate())
					{  return false;  }


				// Now we need to find the children of all the classes defined on this page.  Get the class parent links that resolve to 
				// any of the defined classes, but keep them separate for now.

				List<Link> childLinks = null;

				if (classIDsDefined.IsEmpty == false)
					{  childLinks = accessor.GetClassParentLinksToClasses(classIDsDefined, cancelDelegate);  }

				if (cancelDelegate())
					{  return false;  }


				// Get link targets for everything but the children, since they would just resolve to classes already in this file.

				IDObjects.NumberSet linkTargetIDs = new IDObjects.NumberSet();

				foreach (Link link in links)
					{
					if (link.IsResolved)
						{  linkTargetIDs.Add(link.TargetTopicID);  }
					}

				List<Topic> linkTargets = accessor.GetTopicsByID(linkTargetIDs, cancelDelegate) ?? new List<Topic>();

				if (cancelDelegate())
					{  return false;  }


				// Now get targets for the children.

				List<Topic> childTargets = null;

				if (childLinks != null && childLinks.Count > 0)
					{
					IDObjects.NumberSet childClassIDs = new IDObjects.NumberSet();

					foreach (var childLink in childLinks)
						{  childClassIDs.Add(childLink.ClassID);  }

					childTargets = accessor.GetBestClassDefinitionTopics(childClassIDs, cancelDelegate);
					}

				if (cancelDelegate())
					{  return false;  }


				// We can merge the child links and targets into the main lists now.

				if (childLinks != null)
					{  links.AddRange(childLinks);  }

				if (childTargets != null)
					{  linkTargets.AddRange(childTargets);  }

				if (cancelDelegate())
					{  return false;  }


				// Now we need to find any Natural Docs links appearing inside the summaries of link targets.  The tooltips
				// that will be generated for them include their summaries, and even though we don't generate HTML links 
				// inside tooltips, how and if they're resolved affects the appearance of Natural Docs links.  We need to know 
				// whether to include the original text with angle brackets, the text without angle brackets if it's resolved, or 
				// only part of the text if it's a resolved named link.
				
				// Links don't store which topic they appear in but they do store the file, so gather the file IDs of the link 
				// targets that have Natural Docs links in the summaries and get all the links in those files.

				// Links also store which class they appear in, so why not do this by class instead of by file?  Because a 
				// link could be to something global, and the global scope could potentially have a whole hell of a lot of 
				// content, depending on the project and language.  While there can also be some really long files, the
				// chances of that are smaller so we stick with doing this by file.

				IDObjects.NumberSet summaryLinkFileIDs = new IDObjects.NumberSet();

				foreach (Topic linkTarget in linkTargets)
					{
					if (linkTarget.Summary != null && linkTarget.Summary.IndexOf("<link type=\"naturaldocs\"") != -1)
						{  summaryLinkFileIDs.Add(linkTarget.FileID);  }
					}

				List<Link> summaryLinks = null;
					
				if (!summaryLinkFileIDs.IsEmpty)
					{  summaryLinks = accessor.GetNaturalDocsLinksInFiles(summaryLinkFileIDs, cancelDelegate);  }

				if (cancelDelegate())
					{  return false;  }


				// Finally done with the database.

				if (releaseDBLock)
					{
					accessor.ReleaseLock();
					releaseDBLock = false;
					}


				try
					{

					// Build the HTML for the list of topics

					StringBuilder html = new StringBuilder("\r\n\r\n");
					HTMLTopic topicBuilder = new HTMLTopic(this);

					// We don't put embedded topics in the output, so we need to find the last non-embedded one to make
					// sure that the "last" CSS tag is correctly applied.
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

						if (!EngineInstance.Config.ShrinkFiles)
							{  file.WriteLine();  }

						for (int i = 0; i < linkTargets.Count; i++)
							{
							Topic topic = linkTargets[i];
							string toolTipHTML = topicBuilder.BuildToolTip(topic, summaryLinks);

							if (toolTipHTML != null)
								{
								if (!EngineInstance.Config.ShrinkFiles)
									{  file.Write("   ");  }

								file.Write(topic.TopicID);
								file.Write(":\"");
								file.Write(toolTipHTML.StringEscape());
								file.Write('"');

								if (i != linkTargets.Count - 1)
									{  file.Write(',');  }

								if (!EngineInstance.Config.ShrinkFiles)
									{  file.WriteLine();  }
								}
							}

						if (!EngineInstance.Config.ShrinkFiles)
							{  file.Write("   ");  }

						file.Write("});");
						}


					// Build summary and summary tooltips files

					JSSummaryData summaryBuilder = new JSSummaryData(this);
					summaryBuilder.Build(topics, links, PageTitle, OutputFileHashPath, SummaryFile, SummaryToolTipsFile);

					return true;
					}
				catch (Exception e)
					{
					try
						{  e.AddNaturalDocsTask("Building File: " + OutputFile);  }
					catch
						{  }

					throw;
					}
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
		 * Retrieves the <Topics> for the page's location.
		 * 
		 * When implementing this function note that the <CodeDB.Accessor> may or may not already have a lock.
		 */
		public abstract List<Topic> GetTopics (CodeDB.Accessor accessor, CancelDelegate cancelDelegate);


		/* Function: GetLinks
		 * 
		 * Retrieves the <Links> appearing in the page's location.
		 * 
		 * When implementing this function note that the <CodeDB.Accessor> may or may not already have a lock.
		 */
		public abstract List<Link> GetLinks (CodeDB.Accessor accessor, CancelDelegate cancelDelegate);


		/* Function: GetLinkTarget
		 * Returns a <HTMLTopicPage> for the target of a link which resolves to the passed <Topic>.
		 */
		public abstract HTMLTopicPage GetLinkTarget (Topic targetTopic);



		// Group: Properties
		// __________________________________________________________________________


		/* Property: HTMLBuilder
		 * The <Builders.HTML> associated with this topic page.
		 */
		public Builders.HTML HTMLBuilder
			{
			get
				{  return htmlBuilder;  }
			}


		/* Property: EngineInstance
		 * The <Engine.Instance> associated with this topic page.
		 */
		public Engine.Instance EngineInstance
			{
			get
				{  return HTMLBuilder.EngineInstance;  }
			}



		// Group: Abstract Properties
		// __________________________________________________________________________


		/* Property: PageTitle
		 */
		abstract public string PageTitle
			{  get;  }

		/* Function: IncludeClassInTopicHashPaths
		 * Whether to include the class in the topic part of hash paths.  For example, you would want "#Class:MyClass:Member" 
		 * instead of "#Class:MyClass:MyClass.Member".  However, you would want "#File:MyFile.cs:MyClass.Member" instead of
		 * "#File:MyFile.cs:Member".
		 */
		public abstract bool IncludeClassInTopicHashPaths
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

		}
	}

