
// Class: CPP_Plain
class CPP_Plain
	{  }

// Class: CPP_Public_Inheritance
class CPP_Public_Inheritance : public CPP_Base
	{  }

// Class: CPP_Protected_Inheritance
class CPP_Protected_Inheritance : protected CPP_Base
	{  }

// Class: CPP_Private_Inheritance
class CPP_Private_Inheritance : private CPP_Base
	{  }

// Class: CPP_Multiple_Inheritance
class CPP_Multiple_Inheritance : public CPP_BaseA, public CPP_BaseB
	{  }

// Class: CPP_Mixed_Multiple_Inheritance
class CPP_Mixed_Multiple_Inheritance : private CPP_BaseA, public CPP_BaseB, protected CPP_BaseC
	{  }
