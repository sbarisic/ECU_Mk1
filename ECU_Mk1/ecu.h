#pragma once

#ifdef WINDOWS_TEST
#define ECU_API __declspec(dllexport)
#endif

#ifndef ECU_API
#define ECU_API
#endif

ECU_API void init();