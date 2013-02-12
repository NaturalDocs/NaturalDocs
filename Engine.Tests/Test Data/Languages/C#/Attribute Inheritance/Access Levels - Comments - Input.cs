
// Access levels set in comments should not affect the access levels of actual code elements, even if they're
// unspecified in the code.  They should work on comment-only topics though.

public class PublicClass
	{

	// Public Function: Function_PublicTagUnspecifiedCode
	void Function_PublicTagUnspecifiedCode ()
		{  }

	// Public Function: Function_PublicTagPrivateCode
	private void Function_PublicTagPrivateCode ()
		{  }


	// Public Group: PublicGroup
	// ____________________________________________________

		void UnspecifiedFunction ()
			{  }

		private void PrivateFunction ()
			{  }

		// Function: UnspecifiedCommentFunction

		// Private Function: PrivateCommentFunction


	// Private Group: PrivateGroup
	// ____________________________________________________

		void UnspecifiedFunction2 ()
			{  }

		public void PublicFunction2 ()
			{  }

		// Function: UnspecifiedCommentFunction2

		// Public Function: PublicCommentFunction2

	}
