#include "Hello.h"

#include <Utils.h>

// Returns "Hello, " using the Utils library...
std::string Hello::getText()
{
	return Utils::addStrings("Hel", "lo, ");
}

