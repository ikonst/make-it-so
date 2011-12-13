#pragma once

#ifdef WIN32
	#undef DLL_EXPORT
	#define DLL_EXPORT __declspec(dllexport)
#else
	#undef DLL_EXPORT
	#define DLL_EXPORT 
#endif

class Addition
{
public:
	static DLL_EXPORT double add(double a, double b);
};
