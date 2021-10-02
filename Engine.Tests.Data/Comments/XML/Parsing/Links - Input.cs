
/// <summary><a href="http://www.example.com">Named URL link</a></summary>
void URLTest1 () { }

/// <summary><a href="http://www.example.com" target="_blank">Named URL link with properties</a></summary>
void URLTest2 () { }

/// <summary><a href="http://www.example.com" /> Standalone URL link</summary>
void URLTest3 () { }

/// <summary>http://www.example.com Raw URL</summary>
void URLTest4 () { }

/// <summary><a href="http://www.example.com">http://www.example.com</a> Literal URL link</summary>
void URLTest5 () { }



/// <summary><a href="mailto:a@example.com">Named mailto link</a></summary>
void EMailTest1 () { }

/// <summary><a href="mailto:a@example.com" /> Standalone mailto link</summary>
void EMailTest2 () { }

/// <summary>a@example.com Raw e-mail address</summary>
void EMailTest3 () { }

/// <summary>mailto:a@example.com Raw mailto link</summary>
void EMailTest4 () { }

/// <summary><a href="mailto:a@example.com">a@example.com</a> Literal mailto/e-mail address link</summary>
void EMailTest5 () { }




/// <summary><see href="http://www.example.com">Named see href link</see></summary>
void SeeHRefTest1 () { }

/// <summary><see href="http://www.example.com" target="_blank">Named see href link with properties</see></summary>
void SeeHRefTest2 () { }

/// <summary><see href="mailto:a@example.com">Named see href link with mailto target</see></summary>
void SeeHRefTest3 () { }

/// <summary><see href="http://www.example.com" /> Standalone see href link</summary>
void SeeHRefTest4 () { }




/// <see href="http://www.example.com">Named top level see href link</see>
void TopLevelSeeHRefTest1 () { }

/// <see href="http://www.example.com" target="_blank">Named top level see href link with properties</see>
void TopLevelSeeHRefTest2 () { }

/// <see href="mailto:a@example.com">Named top level see href link with mailto target</see>
void TopLevelSeeHRefTest3 () { }

/// <see href="http://www.example.com" /><summary>Standalone top level see href link</summary>
void TopLevelSeeHRefTest4 () { }




/// <summary><a href="http://www.example.com&var=y">Named URL link & entity chars</a></summary>
void EntityTest1 () { }

/// <summary><a href="http://www.example.com&var=y" /> Standalone URL link & entity chars</summary>
void EntityTest2 () { }

/// <summary>http://www.example.com&var=y Raw URL link & entity chars</summary>
void EntityTest3 () { }

/// <summary><a href="mailto:a@example.com">Named e-mail link & entity chars</a></summary>
void EntityTest4 () { }
