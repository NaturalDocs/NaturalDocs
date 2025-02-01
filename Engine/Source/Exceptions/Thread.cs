/*
 * Class: CodeClear.NaturalDocs.Engine.Exceptions.Thread
 * ____________________________________________________________________________
 *
 * An exception thrown when one of the program's worker threads has stopped because of an exception.
 * Preserves the stack trace of the thread and embeds it as the inner exception, which will always exist.  Also
 * includes the time when it was thrown, in case multiple threads have exceptions and it's important to know
 * which one came first.
 *
 *
 * Topic: Usage
 *
 *		- Code in <Engine.Thread> classes does not need to throw this directly.  If you throw a normal exception the class
 *		  will wrap it automatically.
 *
 *		- If you're walking through inner exceptions to provide a crash log, you can skip reporting <Message> from the inner
 *		  exception of this class since it will be contained in this one's.  InnerException will always exist so you don't need
 *		  to check it for null.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


namespace CodeClear.NaturalDocs.Engine.Exceptions
	{
	public class Thread : System.Exception
		{
		public Thread (System.Exception exception)
			: base ("The thread has ended due to an exception", exception)
			{
			threadStackTrace = exception.StackTrace;
			utcTime = System.DateTime.UtcNow;

			System.Threading.Thread currentThread = System.Threading.Thread.CurrentThread;

			if (!System.String.IsNullOrEmpty(currentThread.Name))
				{  threadName = currentThread.Name;  }
			else if (currentThread.IsThreadPoolThread == true)
				{  threadName = Engine.Locale.SafeGet("NaturalDocs.Engine", "Thread.ThreadPoolThread", "Thread Pool Thread");  }
			else
				{  threadName = Engine.Locale.SafeGet("NaturalDocs.Engine", "Thread.UnnamedThread", "Unnamed Thread");  }
			}


		public Thread (System.Exception exception, Engine.Thread thread)
			: base ("The thread has ended due to an exception", exception)
			{
			threadStackTrace = exception.StackTrace;
			utcTime = System.DateTime.UtcNow;

			threadName = thread.Name;
			}


		/* Property: Message
		 * Gets the message of the exception.  This is overridden to provide the wrapped exception's message.
		 */
		public override string Message
			{
			get
				{
				if (InnerException is Engine.Exceptions.UserFriendly)
					{  return InnerException.Message;  }
				else
					{
					return InnerException.Message + " (" + threadName + ")";
					}
				}
			}


		/* Property: StackTrace
		 * Gets the stack trace that cause the exception.  This is overridden to provide the stack trace from the
		 * thread as well.
		 */
		public override string StackTrace
			{
			get
				{
				return

				"   ----- " + threadName + " -----" +
				System.Environment.NewLine +
				threadStackTrace +
				System.Environment.NewLine +
				"   ----- " + Engine.Locale.SafeGet("NaturalDocs.Engine", "Thread.ParentThread", "Parent Thread") + " -----" +
				System.Environment.NewLine +
				base.StackTrace;
				}
			}


		/* Property: ThreadName
		 * The name of the thread that caused the exception.
		 */
		public string ThreadName
			{
			get
				{  return threadName;  }
			}

		/* Property: UTCTime
		 * The UTC time the exception was thrown.
		 */
		public System.DateTime UTCTime
			{
			get
				{  return utcTime;  }
			}


		/* Variable: threadName
		 * The name of the thread that caused the exception.
		 */
		protected string threadName;

		/* Variable: threadStackTrace
		 * The stack trace from the thread that caused the exception.
		 */
		protected string threadStackTrace;

		/* var: utcTime
		 * The UTC time when the exception was thrown.
		 */
		protected System.DateTime utcTime;
		}
	}
