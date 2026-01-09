/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.TopicPage
 * ____________________________________________________________________________
 *
 * Creates the HTML page for the <Topics> of a source file or class and all it's supporting JavaScript files.
 *
 *
 * Threading: Not Thread Safe
 *
 *		This class is only designed to be used by one thread at a time.  Each thread should create its own object.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class TopicPage : Page
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: TopicPage
		 */
		public TopicPage (Context context) : base (context)
			{
			}


		/* Function: BuildDataFiles
		 *
		 * Builds the content HTML file for the passed <Context's> <PageLocation> and its supporting <JSONSummary> and
		 * <JSONToolTips>.  Returns whether there was any content.  It will also return false if it was interrupted by the
		 * <CancelDelegate>.
		 *
		 * If the <CodeDB.Accessor> doesn't have a lock, this function will automatically acquire and release a read-only
		 * lock.  This is the preferred way of using this function as the lock will only be held during the data querying stage
		 * and will be  released before writing output to disk.
		 *
		 * If it already had a lock it will use it and not release it unless you set releaseExistingLocks.
		 */
		public bool BuildDataFiles (Context context, CodeDB.Accessor accessor, CancelDelegate cancelDelegate,
												bool releaseExistingLocks = false)
			{
			this.Context = context;
			var location = context.Page;

			List<Engine.Topics.Topic> topics;
			List<Engine.Links.Link> links;
			List<Engine.Links.ImageLink> imageLinks;

			bool releaseDBLock = false;

			if (accessor.LockHeld == CodeDB.Accessor.LockType.None)
				{
				accessor.GetReadOnlyLock();
				releaseDBLock = true;
				}
			else if (releaseExistingLocks)
				{
				releaseDBLock = true;
				}

			try
				{

				// Get the topics from the database.

				if (location.IsSourceFile)
					{
					topics = accessor.GetTopicsInFile(location.FileID, cancelDelegate);
					}
				else if (location.IsClass)
					{
					topics = accessor.GetTopicsInClass(location.ClassID, cancelDelegate);
					}
				else
					{  throw new NotImplementedException();  }

				if (topics == null || topics.Count == 0 || cancelDelegate())
					{  return false;  }


				// Create the class view if appropriate

				if (location.IsClass)
					{
					ClassView.Merge(ref topics, EngineInstance);

					// It's possible for ClassView to reduce the number of topics to zero, so check for that and treat it as if the
					// class page has no content.
					if (topics.Count == 0)
						{  return false;  }
					}


				// Get the links from the database.

				if (location.IsSourceFile)
					{
					links = accessor.GetLinksInFile(location.FileID, cancelDelegate) ?? new List<Engine.Links.Link>();
					imageLinks = accessor.GetImageLinksInFile(location.FileID, cancelDelegate) ?? new List<Engine.Links.ImageLink>();
					}
				else if (location.IsClass)
					{
					links = accessor.GetLinksInClass(location.ClassID, cancelDelegate) ?? new List<Engine.Links.Link>();
					imageLinks = accessor.GetImageLinksInClass(location.ClassID, cancelDelegate) ?? new List<Engine.Links.ImageLink>();
					}
				else
					{  throw new NotImplementedException();  }

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

				if (location.IsClass == false && classIDsDefined.IsEmpty == false)
					{
					List<Engine.Links.Link> classParentLinks = accessor.GetClassParentLinksInClasses(classIDsDefined, cancelDelegate);

					if (classParentLinks != null && classParentLinks.Count > 0)
						{  links.AddRange(classParentLinks);  }
					}

				if (cancelDelegate())
					{  return false;  }


				// Now we need to find the children of all the classes defined on this page.  Get the class parent links that resolve to
				// any of the defined classes, but keep them separate for now.

				List<Engine.Links.Link> childLinks = null;

				if (classIDsDefined.IsEmpty == false)
					{  childLinks = accessor.GetClassParentLinksToClasses(classIDsDefined, cancelDelegate);  }

				if (cancelDelegate())
					{  return false;  }


				// Get link targets for everything but the children, since they would just resolve to classes already in this file.

				IDObjects.NumberSet linkTargetIDs = new IDObjects.NumberSet();

				foreach (var link in links)
					{
					if (link.IsResolved)
						{  linkTargetIDs.Add(link.TargetTopicID);  }
					}

				List<Engine.Topics.Topic> linkTargets = accessor.GetTopicsByID(linkTargetIDs, cancelDelegate) ??
																		  new List<Engine.Topics.Topic>();

				if (cancelDelegate())
					{  return false;  }


				// Now get targets for the children.

				List<Engine.Topics.Topic> childTargets = null;

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


				// Now we need to find any Natural Docs and image links appearing inside the summaries of link targets.
				// The tooltips that will be generated for them include their summaries, and even though we don't generate
				// HTML links inside tooltips, how and if they're resolved affects their appearance.  We need to know whether
				// to include the original text with angle brackets, the text without angle brackets if it's resolved, or only part
				// of the text if it's a resolved named link.

				// Links don't store which topic they appear in but they do store the file, so gather the file IDs of the link
				// targets that have Natural Docs or image links in the summaries and get all the links in those files.

				// Links also store which class they appear in, so why not do this by class instead of by file?  Because a
				// link could be to something global, and the global scope could potentially have a whole hell of a lot of
				// content, depending on the project and language.  While there can also be some really long files, the
				// chances of that are smaller so we stick with doing this by file.

				IDObjects.NumberSet summaryLinkFileIDs = new IDObjects.NumberSet();

				foreach (var linkTarget in linkTargets)
					{
					if (linkTarget.Summary != null &&
						(linkTarget.Summary.IndexOf("<link type=\"naturaldocs\"") != -1 || linkTarget.Summary.IndexOf("<image ") != -1))
						{  summaryLinkFileIDs.Add(linkTarget.FileID);  }
					}

				List<Engine.Links.Link> summaryLinks = null;
				List<Engine.Links.ImageLink> summaryImageLinks = null;

				if (!summaryLinkFileIDs.IsEmpty)
					{
					summaryLinks = accessor.GetNaturalDocsLinksInFiles(summaryLinkFileIDs, cancelDelegate);
					summaryImageLinks = accessor.GetImageLinksInFiles(summaryLinkFileIDs, cancelDelegate);
					}

				if (cancelDelegate())
					{  return false;  }


				// Finally done with the database.

				if (releaseDBLock)
					{
					accessor.ReleaseLock();
					releaseDBLock = false;
					}


				// Determine the page title

				string pageTitle;

				if (context.Page.IsSourceFile)
					{  pageTitle = EngineInstance.Files.FromID(context.Page.FileID).FileName.NameWithoutPath;  }
				else if (context.Page.IsClass)
					{  pageTitle = context.Page.ClassString.Symbol.LastSegment;  }
				else
					{  throw new NotImplementedException();  }


				// Build the HTML for the list of topics

				StringBuilder html = new StringBuilder("\r\n\r\n");

				HTML.Components.Topic topicBuilder = new HTML.Components.Topic(context);
				HTML.Components.Tooltip tooltipBuilder = new HTML.Components.Tooltip(context);

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
						topicBuilder.AppendTopic(topics[i], context, links, linkTargets, imageLinks, html, topics, i + 1, extraClass);
						html.Append("\r\n\r\n");
						}
					}


				// Build the full HTML file

				Build(context.OutputFile, pageTitle, html.ToString(), PageType.Content);


				// Build summary and tooltips files

				JSONSummary summaryBuilder = new JSONSummary(context);
				summaryBuilder.ConvertToJSON(topics, context);
				summaryBuilder.BuildDataFile(pageTitle);

				JSONToolTips toolTipsBuilder = new JSONToolTips(context);
				toolTipsBuilder.ConvertToJSON(topics, links, imageLinks, context, skipEmbeddedTopics: true);
				toolTipsBuilder.BuildDataFileForSummary();

				toolTipsBuilder.ConvertToJSON(linkTargets, summaryLinks, summaryImageLinks, context);
				toolTipsBuilder.BuildDataFileForContent();

				return true;
				}

			catch (Exception e)
				{
				try
					{  e.AddNaturalDocsTask("Building HTML file " + context.OutputFile);  }
				catch
					{  }

				throw;
				}

			finally
				{
				if (releaseDBLock)
					{  accessor.ReleaseLock();  }
				}
			}

		}
	}
