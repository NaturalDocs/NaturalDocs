/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.PrototypeButtonPanel
 * ____________________________________________________________________________
 *
 * A reusable class for building HTML button panels attached to prototypes.
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


using System.Text;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class PrototypeButtonPanel : HTML.Components.FormattedText
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: PrototypeButtonPanel
		 */
		public PrototypeButtonPanel (Context context) : base (context)
			{
			repositoryLinksBuilder = new ButtonPanels.RepositoryLinks(context);
			}


		/* Function: IsNeededFor
		 * Whether the passed Topic's prototype needs a button panel.
		 */
		public bool IsNeededFor (Engine.Topics.Topic topic)
			{
			return repositoryLinksBuilder.HasRepositoryLinks(topic);
			}


		/* Function: AppendButtonPanel
		 *
		 * Builds the HTML for the passed Topic's prototype button panel and appends it to the passed StringBuilder.
		 *
		 * Requirements:
		 *
		 *		- The <Context>'s topic must be set.
		 */
		public void AppendButtonPanel (Engine.Topics.Topic topic, StringBuilder output)
			{
			if (!IsNeededFor(topic))
				{  return;  }

			output.Append(
				"<div id=\"NDPrototypeButtonPanel" + topic.TopicID + "\" class=\"NDPrototype PButtonPanel\" " +
					"onmouseenter=\"NDContentPage.OnPrototypeMouseEnter(event,'NDPrototypeButtonPanel" + topic.TopicID + "','NDPrototype" + topic.TopicID + "');\" " +
					"onmouseleave=\"NDContentPage.OnPrototypeMouseLeave(event,'NDPrototypeButtonPanel" + topic.TopicID + "','NDPrototype" + topic.TopicID + "');\">");

				repositoryLinksBuilder.AppendRepositoryLinks(topic, output);

			output.Append("</div>");
			}


		// Group: Variables
		// __________________________________________________________________________

		protected ButtonPanels.RepositoryLinks repositoryLinksBuilder;


		}
	}
