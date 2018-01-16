
/*
	Topic: Keywords

		(code)

		async bool Function (string x)
			{
			if (x == null)
				{  return true;  }
			else
				{  return false;  }
			}

		(end)


	Topic: @Identifiers

		Should not be highlighted as keywords.

		(code)

		int Function (string x)
			{
			int @true = 1;
			int @false = 0;
			string @null = "";

			if (x == @null)
				{  return @true;  }
			else
				{  return @false;  }
			}

		(end)

*/
