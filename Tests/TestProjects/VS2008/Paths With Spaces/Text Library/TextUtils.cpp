#include "TextUtils.h"
#include <Hello.h>

std::string TextUtils::getText()
{
	return Hello::getHello() + ", World!";
}

