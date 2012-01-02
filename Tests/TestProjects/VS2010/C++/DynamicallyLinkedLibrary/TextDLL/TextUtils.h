#pragma once

#include <string>

#ifdef WIN32
	#undef DLL_EXPORT
	#define DLL_EXPORT __declspec(dllexport)
#else
	#undef DLL_EXPORT
	#define DLL_EXPORT 
#endif

class TextUtils
{
public:
	static DLL_EXPORT std::string getHello();
	static DLL_EXPORT std::string getWorld();
};

