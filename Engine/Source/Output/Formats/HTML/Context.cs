/* 
 * Struct: CodeClear.NaturalDocs.Engine.Output.Formats.HTML.Context
 * ____________________________________________________________________________
 * 
 * A struct that contains the context in which a HTML component is being built, such as which <Topic> it's for and which
 * <HTMLTopicPage> it appears in.
 * 
 * 
 * Multithreading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output.Formats.HTML
	{
	public struct Context
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Context
		 * Creates a context with the passed parameters.
		 */
		public Context (Builders.HTML builder, Output.Components.HTMLTopicPage topicPage = null, Topic topic = null)
			{
			#if DEBUG
			if (topicPage != null)
				{
				if ((object)topicPage.HTMLBuilder != (object)builder)
					{  throw new Exception("Tried to create a Context with a topic page that doesn't match the builder.");  }
				}
			if (topic != null)
				{
				if (topicPage == null)
					{  throw new Exception("Tried to create a Context with a topic but not a topic page.");  }
				}
			#endif

			this.builder = builder;
			this.topicPage = topicPage;
			this.topic = topic;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Builder
		 * The <Builders.HTML> associated with this context.
		 */
		public Builders.HTML Builder
			{
			get
				{  return builder;  }
			}


		/* Property: TopicPage
		 * The <Output.Components.HTMLTopicPage> associated with this context, or null if it's not relevant.
		 */
		public Output.Components.HTMLTopicPage TopicPage
			{
			get
				{  return topicPage;  }
			}


		/* Property: Topic
		 * The <Topic> associated with this context, or null if it's not relevant.
		 */
		public Topic Topic
			{
			get
				{  return topic;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: builder
		 * The <Builders.HTML> being built for.
		 */
		private Builders.HTML builder;

		/* var: topicPage
		 * The <Output.Components.HTMLTopicPage> being built for, or null if it's not relevant.
		 */
		private Output.Components.HTMLTopicPage topicPage;

		/* var: topic
		 * The <Topic> being built for, or null if it's not relevant.
		 */
		private Topic topic;

		}
	}

