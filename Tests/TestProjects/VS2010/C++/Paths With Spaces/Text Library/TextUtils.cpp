#include "TextUtils.h"
#include <Hello.h>

std::string TextUtils::getText()
{
	return Hello::getText() + ", World!";
}
