/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.ButtonPanels.RepositoryLinks
 * ____________________________________________________________________________
 *
 * A reusable class for building repository links for HTML button panels.
 *
 *
 * Threading: Not Thread Safe
 *
 *		This class is only designed to be used by one thread at a time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.ButtonPanels
	{
	public class RepositoryLinks : HTML.Components.FormattedText
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: RepositoryLinks
		 */
		public RepositoryLinks (Context context) : base (context)
			{
			}


		/* Function: HasRepositoryLinks
		 * Returns whether the passed Topic has any repository links.
		 */
		public bool HasRepositoryLinks (Engine.Topics.Topic topic)
			{
			// First check the topic's main definition

			var definitionFile = EngineInstance.Files.FromID(topic.FileID);

			// If the FileSource isn't a SourceFolder this will be null without throwing an exception
			var definitionFileSource = EngineInstance.Files.FileSourceOf(definitionFile) as Files.FileSources.SourceFolder;

			if (definitionFileSource != null &&
				definitionFileSource.RepositorySourceURLTemplate != null)
				{  return true;  }


			// Now check its other definitions, if any

			if (topic.HasOtherDefinitions)
				{
				foreach (var definitionTopic in topic.OtherDefinitions)
					{
					definitionFile = EngineInstance.Files.FromID(definitionTopic.FileID);

					// If the FileSource isn't a SourceFolder it will be null without throwing an exception
					definitionFileSource = EngineInstance.Files.FileSourceOf(definitionFile) as Files.FileSources.SourceFolder;

					if (definitionFileSource != null &&
						definitionFileSource.RepositorySourceURLTemplate != null)
						{  return true;  }
					}
				}

			return false;
			}


		/* Function: AppendRepositoryLinks
		 *
		 * Builds the HTML for the passed Topic's prototype button panel and appends it to the passed StringBuilder.
		 *
		 * Requirements:
		 *
		 *		- The <Context>'s topic must be set.
		 */
		public void AppendRepositoryLinks (Engine.Topics.Topic topic, StringBuilder output)
			{
			List<RepositoryLink> repositoryLinks = BuildRepositoryLinks(topic);

			if (repositoryLinks == null || repositoryLinks.Count == 0)
				{  return;  }

			else if (repositoryLinks.Count == 1)
				{
				output.Append(
					"<div class=\"BPRepositoryLinks Single\">" +

						"<a href=\"" + repositoryLinks[0].URL + "\" target=\"_blank\">" +
							Locale.Get("NaturalDocs.Engine", "HTML.ViewSourceInRepository").ToHTML() +
							"<span class=\"RLLinkIcon\"></span>" +
						"</a>" +

					"</div>");
				}

			else
				{
				output.Append(
					"<div class=\"BPRepositoryLinks Multiple\">" +

						"<div class=\"RLHeader\">" +
							Locale.Get("NaturalDocs.Engine", "HTML.ViewSourceInRepository").ToHTML() + ":" +
						"</div>" +

						"<div class=\"RLLinks\">");

						foreach (var repositoryLink in repositoryLinks)
							{
							output.Append(
								"<a href=\"" + repositoryLink.URL + "\" target=\"_blank\">" +
									repositoryLink.Title.ToHTML() + "<span class=\"RLLinkIcon\"></span>" +
								"</a>");
							}

					output.Append(
						"</div>" +
					"</div>");
				}
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: BuildRepositoryLinks
		 * Returns a list of <RepositoryLinks> defining the passed <Topic>, or null if there aren't any.  Note that <RepositoryLink.Title>
		 * will only be defined if there's more than one, since it's not needed otherwise.
		 */
		protected List<RepositoryLink> BuildRepositoryLinks (Engine.Topics.Topic topic)
			{
			List<RepositoryLink> links = null;


			// First check the topic's main definition

			var definitionFile = EngineInstance.Files.FromID(topic.FileID);

			// If the FileSource isn't a SourceFolder this will be null without throwing an exception
			var definitionFileSource = EngineInstance.Files.FileSourceOf(definitionFile) as Files.FileSources.SourceFolder;

			if (definitionFileSource != null &&
				definitionFileSource.RepositorySourceURLTemplate != null)
				{
				var link = new RepositoryLink();

				link.FileID = topic.FileID;
				link.FileSourceName = definitionFileSource.Name;
				link.RelativeFilePath = definitionFileSource.MakeRelative(definitionFile.FileName);
				link.LineNumber = HTML.RepositoryLinks.EffectiveLineNumber(topic);

				link.URL = HTML.RepositoryLinks.ToSourceFile(definitionFileSource.RepositorySourceURLTemplate, link.RelativeFilePath, link.LineNumber);
				// link.Title stays null for now

				links = new List<RepositoryLink>();
				links.Add(link);
				}


			// Now check its other definitions, if any

			if (topic.HasOtherDefinitions)
				{
				foreach (var definitionTopic in topic.OtherDefinitions)
					{
					definitionFile = EngineInstance.Files.FromID(definitionTopic.FileID);

					// If the FileSource isn't a SourceFolder it will be null without throwing an exception
					definitionFileSource = EngineInstance.Files.FileSourceOf(definitionFile) as Files.FileSources.SourceFolder;

					if (definitionFileSource != null &&
						definitionFileSource.RepositorySourceURLTemplate != null)
						{
						var definitionLink = new RepositoryLink();

						definitionLink.FileID = definitionTopic.FileID;
						definitionLink.FileSourceName = definitionFileSource.Name;
						definitionLink.RelativeFilePath = definitionFileSource.MakeRelative(definitionFile.FileName);
						definitionLink.LineNumber = HTML.RepositoryLinks.EffectiveLineNumber(definitionTopic);

						definitionLink.URL = HTML.RepositoryLinks.ToSourceFile(definitionFileSource.RepositorySourceURLTemplate,
																										  definitionLink.RelativeFilePath, definitionLink.LineNumber);
						// definitionLink.Title stays null for now

						if (links == null)
							{  links = new List<RepositoryLink>();  }

						links.Add(definitionLink);
						}
					}
				}


			// If there's more than one we have to sort the list and possibly change some titles

			if (links != null && links.Count > 1)
				{

				// First generate titles for each link that are just the file name without the path

				for (int i = 0; i < links.Count; i++)
					{
					links[i].Title = links[i].RelativeFilePath.NameWithoutPath;
					}


				// Next see if there's any duplicate titles or file IDs

				bool hasDuplicateTitles = false;
				bool hasDuplicateFileIDs = false;

				for (int i = 0; i < links.Count - 1; i++)
					{
					for (int j = i + 1; j < links.Count; j++)
						{
						if (string.Compare(links[i].Title, links[j].Title, StringComparison.InvariantCultureIgnoreCase) == 0)
							{
							hasDuplicateTitles = true;

							if (links[i].FileID == links[j].FileID)
								{
								hasDuplicateFileIDs = true;
								break;
								}
							}

						if (hasDuplicateFileIDs)
							{  break;  }
						}
					}


				// If there's duplicate titles, replace each title with the full path relative to the file source, and include the file source
				// name as well in case there's more than one.

				if (hasDuplicateTitles)
					{
					for (int i = 0; i < links.Count; i++)
						{
						links[i].Title = links[i].RelativeFilePath.ToString('/');

						if (links[i].FileSourceName != null)
							{  links[i].Title = links[i].FileSourceName + '/' + links[i].Title;  }
						}


					// Now find the longest common substring ending in a slash.

					int lastCommonSlashIndex = links[0].Title.LastIndexOf('/');

					for (int i = 1; i < links.Count && lastCommonSlashIndex != -1; i++)
						{
						while (links[i].Title.Length < lastCommonSlashIndex + 2 ||
								 string.Compare(links[0].Title, 0, links[i].Title, 0, lastCommonSlashIndex + 1, StringComparison.InvariantCultureIgnoreCase) != 0)
							{
							if (lastCommonSlashIndex == 0)
								{  lastCommonSlashIndex = -1;  }
							else
								{  lastCommonSlashIndex = links[0].Title.LastIndexOf('/', lastCommonSlashIndex - 1);  }

							if (lastCommonSlashIndex == -1)
								{  break;  }
							}
						}


					// If there's a common section of string, cut all the titles down to exclude it.

					for (int i = 0; i < links.Count; i++)
						{
						links[i].Title = links[i].Title.Substring(lastCommonSlashIndex + 1);
						}
					}


				// Sort by title, using file ID and line number as backup options.  This keeps definitions from the same file together
				// and we'll add the line numbers later.

				links.Sort( (a,b) =>
					{
					int result = string.Compare(a.Title, b.Title, StringComparison.InvariantCultureIgnoreCase);

					if (result != 0)
						{  return result;  }

					// Try again but case-sensitive to break ties.
					result = string.Compare(a.Title, b.Title, StringComparison.InvariantCulture);

					if (result != 0)
						{  return result;  }

					// Then by file ID just to keep definitions from the same file together.
					result = a.FileID - b.FileID;

					if (result != 0)
						{  return result;  }

					// And finally by line number for those in the same file.
					return (a.LineNumber - b.LineNumber);
					});


				// Finally, add line numbers to definitions in the same file.

				if (hasDuplicateFileIDs)
					{
					for (int i = 0; i < links.Count; i++)
						{
						if ( (i + 1 < links.Count && links[i+1].FileID == links[i].FileID) ||
							 (i > 0 && links[i-1].FileID == links[i].FileID) )
							{
							links[i].Title += " (" + links[i].LineNumber + ")";
							}
						}
					}
				}


			return links;
			}


		/* ___________________________________________________________________________
		 *
		 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.ButtonPanels.RepositoryLinks.RepositoryLink
		 * ____________________________________________________________________________
		 *
		 * A simple class to handle gathering and sorting repository links.
		 *
		 */

		protected class RepositoryLink
			{

			// Group: Functions
			// __________________________________________________________________________


			/* Constructor: RepositoryLink
			 */
			public RepositoryLink ()
				{
				URL = null;
				Title = null;

				FileID = 0;
				FileSourceName = null;
				RelativeFilePath = default;
				LineNumber = 0;
				}



			// Group: Variables
			// __________________________________________________________________________


			public string Title;
			public string URL;

			public int FileID;
			public string FileSourceName;
			public RelativePath RelativeFilePath;
			public int LineNumber;

			}

		}
	}
