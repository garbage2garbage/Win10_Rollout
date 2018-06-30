#dynamiclinkfile kernel32.dll
#dynamiclinkfile user32.dll
#dynamiclinkfile shell32.dll

data
ARGC DD ? ;argc for CommandLineToArgvW

code
START:
call GetCommandLineW
;eax = addr of command line

push addr ARGC, eax
call CommandLineToArgvW
;eax = argv[], [ARGC] = argc

;setup for later call to SystemParametersInfoW
push 3, [eax+4], 0, 20
;setup for later call to GetFileAttributesW
push [eax+4]

;if argc == 1, no picture filename provided- nothing to do
mov eax,d[ARGC]
cmp eax,1
je >

;already setup, check if file exists and not a directory
call GetFileAttributesW
;if -1 (not found) or bit 4 set (directory)
and eax,0x10
jne >

;already setup, try to set wallpaper
call SystemParametersInfoW
:
push eax
call ExitProcess
